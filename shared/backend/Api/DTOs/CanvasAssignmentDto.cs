namespace Api.DTOs;

public record CanvasAssignmentDto(
    long Id,
    long CourseId,
    string Name,
    string? Description,
    DateTime? DueAt,
    DateTime? UpdatedAt,
    string WorkflowState,
    bool Published,
    CanvasSubmissionDto? Submission
);

public record CanvasSubmissionDto(
    string WorkflowState,
    DateTime? SubmittedAt,
    bool Late,
    bool Missing
);
