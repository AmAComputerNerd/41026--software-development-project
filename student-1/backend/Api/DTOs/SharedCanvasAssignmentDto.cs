namespace Api.DTOs;

public record SharedCanvasAssignmentDto(
    long Id,
    long CourseId,
    string Name,
    string? Description,
    DateTime? DueAt,
    DateTime? UpdatedAt,
    string WorkflowState,
    bool Published,
    SharedCanvasSubmissionDto? Submission
);

public record SharedCanvasSubmissionDto(
    string WorkflowState,
    DateTime? SubmittedAt,
    bool Late,
    bool Missing
);
