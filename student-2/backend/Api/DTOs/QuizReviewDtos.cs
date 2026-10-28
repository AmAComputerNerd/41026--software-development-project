namespace Api.DTOs;

public sealed record QuizReviewOptionDto(long Id, string Text);

public sealed record QuizReviewQuestionDto(
    long Id,
    string QuestionType,
    string QuestionText,
    IReadOnlyList<QuizReviewOptionDto> Options,
    long? ProposedAnswerId,
    string? ProposedText);

public sealed record QuizReviewDto(
    Guid RunId,
    Guid AutomationId,
    string QuizTitle,
    long CourseId,
    long QuizId,
    string Result,
    Guid? WorkflowId,
    int WorkflowRevision,
    string ProposalSource,
    IReadOnlyList<QuizReviewQuestionDto> Questions,
    MultiAgentWorkflowDto? Workflow,
    string? WorkflowError,
    string? ReviewDecision,
    string? ReviewerName,
    DateTime? ReviewedAtUtc);

public sealed record QuizReviewAnswerDto(long QuestionId, long? AnswerId, string? Text);

public sealed record SubmitQuizReviewRequestDto(
    string Decision,
    string ReviewerName,
    string? Comments,
    IReadOnlyList<QuizReviewAnswerDto>? Answers);

public sealed record ReviseQuizReviewRequestDto(
    Guid RequestId,
    string ReviewerName,
    string Feedback);
