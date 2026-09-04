namespace Api.Services;

public interface IAiQuizAnswerService
{
    Task<IReadOnlyList<GeneratedQuizAnswer>> AnswerQuestionsAsync(
        AiQuizContext context,
        CancellationToken cancellationToken);
}

public sealed record AiQuizContext(
    string QuizTitle,
    IReadOnlyList<AiQuizQuestion> Questions);

public sealed record AiQuizQuestion(
    long Id,
    string QuestionText,
    IReadOnlyList<AiQuizAnswerOption> Options);

public sealed record AiQuizAnswerOption(long Id, string Text);

public sealed record GeneratedQuizAnswer(long QuestionId, long? AnswerId, string? Text);
