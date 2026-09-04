namespace Api.DTOs;

public sealed record CanvasQuizDto(
    long Id,
    long CourseId,
    string Title,
    string? QuizType,
    int? TimeLimit,
    int AllowedAttempts,
    int QuestionCount,
    bool Published,
    bool LockedForUser
);

public sealed record CanvasQuizSubmissionDto(
    long Id,
    long QuizId,
    int Attempt,
    string ValidationToken,
    string? WorkflowState
);

public sealed record CanvasQuizQuestionDto(
    long Id,
    string QuestionType,
    string QuestionText,
    IReadOnlyList<CanvasQuizAnswerOptionDto> Answers
);

public sealed record CanvasQuizAnswerOptionDto(long Id, string Text);

public sealed record CanvasQuizAnswerDto(long QuestionId, long? AnswerId, string? Text);

public sealed record AnswerCanvasQuizQuestionsDto(
    int Attempt,
    string ValidationToken,
    IReadOnlyList<CanvasQuizAnswerDto> Answers
);
