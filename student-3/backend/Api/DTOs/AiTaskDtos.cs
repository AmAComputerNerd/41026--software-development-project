namespace Api.DTOs;

public record GenerateTaskBreakdownRequestDto(
    string Prompt,
    string Priority
);

public record GenerateTaskDescriptionRequestDto(
    string Title,
    Guid? CourseId,
    Guid? ParentTaskId
);

public record GeneratedTaskDescriptionDto(string Description);

public sealed record BreakdownProposalDto(
    MultiAgentWorkflowDto Workflow,
    IReadOnlyList<Services.GeneratedSubtask> Tasks);

public sealed record ReviewedSubtaskDto(int Index, string Title, string? Description);

public sealed record SubmitBreakdownReviewRequestDto(
    Guid WorkflowId,
    bool Reject,
    string ReviewerName,
    string Comments,
    IReadOnlyList<ReviewedSubtaskDto> Tasks);

public sealed record BreakdownReviewResultDto(
    MultiAgentWorkflowDto Workflow,
    IReadOnlyList<TaskDto> Tasks);
