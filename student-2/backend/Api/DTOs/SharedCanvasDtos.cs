namespace Api.DTOs;

public sealed record SharedCanvasCourseDto(
    long Id,
    string Name,
    string? CourseCode,
    string WorkflowState
);

public sealed record SharedCanvasRecipientDto(
    string Id,
    string Name,
    string Category,
    string? AvatarUrl
);

public sealed record CreateSharedCanvasConversationDto(
    IReadOnlyList<string> Recipients,
    string Subject,
    string Body,
    string ContextCode,
    bool GroupConversation
);

public sealed record SharedCanvasQuizDto(
    long Id,
    long CourseId,
    string Title,
    string? QuizType,
    int? TimeLimit,
    int AllowedAttempts,
    int QuestionCount,
    bool Published,
    bool LockedForUser,
    bool HasSubmitted
);

public sealed record SharedCanvasQuizSubmissionDto(
    long Id,
    long QuizId,
    int Attempt,
    string ValidationToken,
    string? WorkflowState
);

public sealed record SharedCanvasQuizQuestionDto(
    long Id,
    string QuestionType,
    string QuestionText,
    IReadOnlyList<SharedCanvasQuizAnswerOptionDto> Answers
);

public sealed record SharedCanvasQuizAnswerOptionDto(long Id, string Text);

public sealed record SharedCanvasQuizAnswerDto(long QuestionId, long? AnswerId, string? Text);

public sealed record AnswerSharedCanvasQuizQuestionsDto(
    int Attempt,
    string ValidationToken,
    IReadOnlyList<SharedCanvasQuizAnswerDto> Answers
);