using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class CanvasApiClient(
    HttpClient httpClient,
    IOptions<CanvasOptions> options) : ICanvasApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly CanvasOptions _options = options.Value;

    public async Task<IReadOnlyList<CanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken)
    {
        var courses = await GetAllPagesAsync<CanvasCourseResponse>(
            "api/v1/courses?enrollment_state=active&enrollment_type=student&per_page=100",
            cancellationToken);

        return courses
            .Select(course => new CanvasCourseDto(
                course.Id,
                course.Name,
                course.CourseCode,
                course.WorkflowState))
            .ToList();
    }

    public async Task<IReadOnlyList<CanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        var assignments = await GetAllPagesAsync<CanvasAssignmentResponse>(
            $"api/v1/courses/{courseId}/assignments?include[]=submission&per_page=100&order_by=due_at",
            cancellationToken);

        return assignments
            .Select(assignment => new CanvasAssignmentDto(
                assignment.Id,
                assignment.CourseId,
                assignment.Name,
                CanvasHtmlTextConverter.ToPlainText(assignment.Description),
                assignment.DueAt,
                assignment.UpdatedAt,
                assignment.WorkflowState,
                assignment.Published,
                assignment.Submission is null
                    ? null
                    : new CanvasSubmissionDto(
                        assignment.Submission.WorkflowState,
                        assignment.Submission.SubmittedAt,
                        assignment.Submission.Late,
                        assignment.Submission.Missing)))
            .ToList();
    }

    public async Task<IReadOnlyList<CanvasUserDto>> GetUsersForCourseAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        var users = await GetAllPagesAsync<CanvasUserResponse>(
            $"api/v1/courses/{courseId}/users?per_page=100",
            cancellationToken);

        return users
            .Select(user => new CanvasUserDto(
                user.Id,
                user.Name,
                user.Email,
                user.SisUserId,
                user.LoginId))
            .ToList();
    }

    public async Task<IReadOnlyList<CanvasRecipientDto>> FindRecipientsAsync(
        long courseId,
        string? search,
        CancellationToken cancellationToken)
    {
        var relativeUrl =
            $"api/v1/search/recipients?context=course_{courseId}&type=user&per_page=100";
        if (!string.IsNullOrWhiteSpace(search))
        {
            relativeUrl += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        var recipients = await GetAllPagesAsync<CanvasRecipientResponse>(
            relativeUrl,
            cancellationToken);

        return recipients
            .Select(recipient => new CanvasRecipientDto(
                GetRecipientId(recipient.Id),
                recipient.FullName ?? recipient.Name,
                GetRecipientCategory(recipient.CommonCourses, courseId),
                recipient.AvatarUrl))
            .ToList();
    }

    public async Task<IReadOnlyList<CanvasConversationDto>> CreateConversationAsync(
        CreateCanvasConversationDto request,
        CancellationToken cancellationToken)
    {
        ConfigureClient();

        var values = request.Recipients
            .Select(recipient => new KeyValuePair<string, string>("recipients[]", recipient))
            .Append(new("subject", request.Subject))
            .Append(new("body", request.Body))
            .Append(new("context_code", request.ContextCode))
            .Append(new(
                "group_conversation",
                request.GroupConversation ? "true" : "false"));

        using var content = new FormUrlEncodedContent(values);
        using var response = await httpClient.PostAsync(
            "api/v1/conversations",
            content,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CanvasApiException(
                response.StatusCode,
                $"Canvas returned HTTP {(int)response.StatusCode}.",
                response.Headers.RetryAfter?.Delta);
        }

        var conversations = await response.Content.ReadFromJsonAsync<List<CanvasConversationResponse>>(
            JsonOptions,
            cancellationToken) ?? [];

        return conversations
            .Select(conversation => new CanvasConversationDto(GetRecipientId(conversation.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<CanvasQuizDto>> GetQuizzesAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CanvasQuizResponse> quizzes;
        try
        {
            quizzes = await GetAllPagesAsync<CanvasQuizResponse>(
                $"api/v1/courses/{courseId}/quizzes?per_page=100",
                cancellationToken);
        }
        catch (CanvasApiException exception) when (
            exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            quizzes = await GetQuizzesFromAssignmentsAsync(courseId, cancellationToken);
        }

        var results = new List<CanvasQuizDto>();
        foreach (var quiz in quizzes)
        {
            var submission = await SendJsonAsync<CanvasQuizSubmissionListResponse>(
                HttpMethod.Get,
                $"api/v1/courses/{courseId}/quizzes/{quiz.Id}/submission",
                null,
                cancellationToken);
            results.Add(ToQuizDto(quiz, courseId, HasSubmitted(submission)));
        }

        return results;
    }

    private async Task<IReadOnlyList<CanvasQuizResponse>> GetQuizzesFromAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        var assignments = await GetAllPagesAsync<CanvasAssignmentResponse>(
            $"api/v1/courses/{courseId}/assignments?per_page=100",
            cancellationToken);
        var quizzes = new List<CanvasQuizResponse>();

        foreach (var quizId in assignments
            .Where(assignment => assignment.QuizId.HasValue)
            .Select(assignment => assignment.QuizId!.Value)
            .Distinct())
        {
            quizzes.Add(await SendJsonAsync<CanvasQuizResponse>(
                HttpMethod.Get,
                $"api/v1/courses/{courseId}/quizzes/{quizId}",
                null,
                cancellationToken));
        }

        return quizzes;
    }

    private static bool HasSubmitted(CanvasQuizSubmissionListResponse response)
    {
        return (response.QuizSubmissions ?? []).Any(submission =>
            string.Equals(
                submission.WorkflowState,
                "complete",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                submission.WorkflowState,
                "pending_review",
                StringComparison.OrdinalIgnoreCase));
    }

    private static CanvasQuizDto ToQuizDto(
        CanvasQuizResponse quiz,
        long courseId,
        bool hasSubmitted)
    {
        return new CanvasQuizDto(
            quiz.Id,
            courseId,
            quiz.Title ?? string.Empty,
            quiz.QuizType,
            quiz.TimeLimit,
            quiz.AllowedAttempts ?? 1,
            quiz.QuestionCount ?? 0,
            quiz.Published ?? false,
            quiz.LockedForUser ?? false,
            hasSubmitted);
    }

    public async Task<CanvasQuizSubmissionDto> StartQuizSubmissionAsync(
        long courseId,
        long quizId,
        CancellationToken cancellationToken)
    {
        CanvasQuizSubmissionListResponse payload;
        var resumedExistingSubmission = false;
        try
        {
            payload = await SendJsonAsync<CanvasQuizSubmissionListResponse>(
                HttpMethod.Post,
                $"api/v1/courses/{courseId}/quizzes/{quizId}/submissions",
                null,
                cancellationToken);
        }
        catch (CanvasApiException exception) when (
            exception.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            resumedExistingSubmission = true;
            payload = await SendJsonAsync<CanvasQuizSubmissionListResponse>(
                HttpMethod.Get,
                $"api/v1/courses/{courseId}/quizzes/{quizId}/submission",
                null,
                cancellationToken);
        }

        var submission = payload.QuizSubmissions?.FirstOrDefault()
            ?? throw new CanvasApiException(
                System.Net.HttpStatusCode.BadGateway,
                "Canvas did not return a quiz submission.");

        if (resumedExistingSubmission && !string.Equals(
            submission.WorkflowState,
            "untaken",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new CanvasApiException(
            System.Net.HttpStatusCode.Conflict,
            "The existing Canvas quiz submission cannot be resumed.");
        }

        if (string.IsNullOrWhiteSpace(submission.ValidationToken))
        {
            throw new CanvasApiException(
                System.Net.HttpStatusCode.BadGateway,
                "Canvas did not return a quiz submission validation token.");
        }

        return new CanvasQuizSubmissionDto(
            submission.Id,
            submission.QuizId,
            submission.Attempt ?? 1,
            submission.ValidationToken,
            submission.WorkflowState);
    }

    public async Task<IReadOnlyList<CanvasQuizQuestionDto>> GetQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        CancellationToken cancellationToken)
    {
        var payload = await SendJsonAsync<CanvasQuizSubmissionQuestionListResponse>(
            HttpMethod.Get,
            $"api/v1/quiz_submissions/{quizSubmissionId}/questions?include[]=quiz_question&per_page=100",
            null,
            cancellationToken);

        return (payload.QuizSubmissionQuestions ?? [])
            .Select(question => new CanvasQuizQuestionDto(
                question.Id,
                question.QuestionType ?? string.Empty,
                CanvasHtmlTextConverter.ToPlainText(question.QuestionText) ?? string.Empty,
                (question.Answers ?? [])
                    .Select(answer => new CanvasQuizAnswerOptionDto(
                        answer.Id,
                        CanvasHtmlTextConverter.ToPlainText(answer.Text ?? answer.Html)
                            ?? string.Empty))
                    .ToList()))
            .ToList();
    }

    public async Task AnswerQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        AnswerCanvasQuizQuestionsDto request,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["attempt"] = request.Attempt,
            ["validation_token"] = request.ValidationToken,
            ["quiz_questions"] = request.Answers
                .Select(answer => new Dictionary<string, object?>
                {
                    ["id"] = answer.QuestionId,
                    ["answer"] = answer.AnswerId is null
                        ? answer.Text
                        : answer.AnswerId.Value
                })
                .ToList()
        };

        await SendJsonAsync<JsonElement>(
            HttpMethod.Post,
            $"api/v1/quiz_submissions/{quizSubmissionId}/questions",
            body,
            cancellationToken);
    }

    private async Task<T> SendJsonAsync<T>(
        HttpMethod method,
        string relativeUrl,
        object? body,
        CancellationToken cancellationToken)
    {
        ConfigureClient();

        using var request = new HttpRequestMessage(method, relativeUrl);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CanvasApiException(
                response.StatusCode,
                $"Canvas returned HTTP {(int)response.StatusCode}.",
                response.Headers.RetryAfter?.Delta);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new JsonException("Canvas returned an empty response.");
    }

    private async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        ConfigureClient();

        var results = new List<T>();
        Uri? nextPage = new(relativeUrl, UriKind.Relative);

        while (nextPage is not null)
        {
            using var response = await httpClient.GetAsync(nextPage, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CanvasApiException(
                    response.StatusCode,
                    $"Canvas returned HTTP {(int)response.StatusCode}.",
                    response.Headers.RetryAfter?.Delta);
            }

            var page = await response.Content.ReadFromJsonAsync<List<T>>(
                JsonOptions,
                cancellationToken) ?? [];
            results.AddRange(page);
            nextPage = GetNextPage(response);
        }

        return results;
    }

    private static string GetRecipientId(JsonElement id)
    {
        return id.ValueKind switch
        {
            JsonValueKind.String => id.GetString() ?? string.Empty,
            JsonValueKind.Number => id.GetRawText(),
            _ => throw new JsonException("Canvas returned an invalid recipient ID.")
        };
    }

    private static string GetRecipientCategory(
        Dictionary<string, string[]>? commonCourses,
        long courseId)
    {
        var courseKey = courseId.ToString(CultureInfo.InvariantCulture);
        if (commonCourses is null || !commonCourses.TryGetValue(courseKey, out var enrollments))
        {
            return "People";
        }

        if (enrollments.Contains("StudentEnrollment", StringComparer.Ordinal))
        {
            return "Students";
        }

        if (enrollments.Contains("TeacherEnrollment", StringComparer.Ordinal))
        {
            return "Teachers";
        }

        if (enrollments.Contains("TaEnrollment", StringComparer.Ordinal))
        {
            return "Teaching assistants";
        }

        if (enrollments.Contains("DesignerEnrollment", StringComparer.Ordinal))
        {
            return "Designers";
        }

        return enrollments.Contains("ObserverEnrollment", StringComparer.Ordinal)
            ? "Observers"
            : "People";
    }

    private void ConfigureClient()
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new CanvasConfigurationException(
                "Canvas:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            throw new CanvasConfigurationException("Canvas:ApiToken is not configured.");
        }

        httpClient.BaseAddress ??= new Uri(
            baseUri.AbsoluteUri.EndsWith('/') ? baseUri.AbsoluteUri : $"{baseUri.AbsoluteUri}/");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiToken);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CanvasIntegrationService/1.0");
    }

    private Uri? GetNextPage(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
        {
            return null;
        }

        foreach (var link in values.SelectMany(value => value.Split(',')))
        {
            if (!link.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var start = link.IndexOf('<');
            var end = link.IndexOf('>');
            if (start < 0 || end <= start ||
                !Uri.TryCreate(link[(start + 1)..end], UriKind.Absolute, out var next))
            {
                throw new CanvasApiException(
                    System.Net.HttpStatusCode.BadGateway,
                    "Canvas returned an invalid pagination link.");
            }

            if (httpClient.BaseAddress is null ||
                !string.Equals(next.Scheme, httpClient.BaseAddress.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(next.Host, httpClient.BaseAddress.Host, StringComparison.OrdinalIgnoreCase) ||
                next.Port != httpClient.BaseAddress.Port)
            {
                throw new CanvasApiException(
                    System.Net.HttpStatusCode.BadGateway,
                    "Canvas returned a pagination link for a different origin.");
            }

            return next;
        }

        return null;
    }
}
