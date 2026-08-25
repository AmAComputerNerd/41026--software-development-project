using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
                assignment.Description,
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
