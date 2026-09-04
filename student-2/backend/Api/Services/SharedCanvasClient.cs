using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;

namespace Api.Services;

public sealed class SharedCanvasClient(HttpClient httpClient) : ISharedCanvasClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<SharedCanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasCourseDto>("api/canvas/courses", cancellationToken);
    }

    public Task<IReadOnlyList<SharedCanvasRecipientDto>> GetRecipientsAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasRecipientDto>(
            $"api/canvas/courses/{courseId}/recipients",
            cancellationToken);
    }

    public async Task CreateConversationAsync(
        CreateSharedCanvasConversationDto request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/canvas/conversations",
            request,
            JsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<IReadOnlyList<SharedCanvasQuizDto>> GetQuizzesAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasQuizDto>(
            $"api/canvas/courses/{courseId}/quizzes",
            cancellationToken);
    }

    public async Task<SharedCanvasQuizSubmissionDto> StartQuizSubmissionAsync(
        long courseId,
        long quizId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            $"api/canvas/courses/{courseId}/quizzes/{quizId}/submissions",
            null,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SharedCanvasQuizSubmissionDto>(
            JsonOptions,
            cancellationToken)
            ?? throw new HttpRequestException(
                "The shared service did not return a quiz submission.");
    }

    public Task<IReadOnlyList<SharedCanvasQuizQuestionDto>> GetQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasQuizQuestionDto>(
            $"api/canvas/quiz-submissions/{quizSubmissionId}/questions",
            cancellationToken);
    }

    public async Task AnswerQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        AnswerSharedCanvasQuizQuestionsDto request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/canvas/quiz-submissions/{quizSubmissionId}/answers",
            request,
            JsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<IReadOnlyList<T>> GetAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<T>>(
            JsonOptions,
            cancellationToken) ?? [];
    }
}