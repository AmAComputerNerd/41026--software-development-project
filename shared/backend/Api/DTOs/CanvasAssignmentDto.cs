namespace Api.DTOs;

public record CanvasAssignmentDto(
    long Id,
    long CourseId,
    string Name,
    string? Description,
    DateTimeOffset? DueAt,
    DateTimeOffset? UpdatedAt,
    string WorkflowState,
    bool Published,
    CanvasSubmissionDto? Submission
);

public record CanvasSubmissionDto(
    string WorkflowState,
    DateTimeOffset? SubmittedAt,
    bool Late,
    bool Missing
);
