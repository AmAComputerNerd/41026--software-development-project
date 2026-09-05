using Api.DTOs;

namespace Api.Services;

public interface ISharedCanvasClient
{
    Task<IReadOnlyList<SharedCanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedCanvasRecipientDto>> GetRecipientsAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task CreateConversationAsync(
        CreateSharedCanvasConversationDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedCanvasQuizDto>> GetQuizzesAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<SharedCanvasQuizSubmissionDto> StartQuizSubmissionAsync(
        long courseId,
        long quizId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedCanvasQuizQuestionDto>> GetQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        CancellationToken cancellationToken);

    Task AnswerQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        AnswerSharedCanvasQuizQuestionsDto request,
        CancellationToken cancellationToken);
}