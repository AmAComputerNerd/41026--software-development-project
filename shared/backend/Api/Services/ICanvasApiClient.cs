using Api.DTOs;

namespace Api.Services;

public interface ICanvasApiClient
{
    Task<IReadOnlyList<CanvasCourseDto>> GetCoursesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasUserDto>> GetUsersForCourseAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasRecipientDto>> FindRecipientsAsync(
        long courseId,
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasConversationDto>> CreateConversationAsync(
        CreateCanvasConversationDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasQuizDto>> GetQuizzesAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<CanvasQuizSubmissionDto> StartQuizSubmissionAsync(
        long courseId,
        long quizId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasQuizQuestionDto>> GetQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        CancellationToken cancellationToken);

    Task AnswerQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        AnswerCanvasQuizQuestionsDto request,
        CancellationToken cancellationToken);
}
