namespace Api.DTOs;

public record CanvasAssignmentDto(
    long Id,
    long CourseId,
    long AssignmentGroupId,
    string Name,
    string? Description,
    DateTime? DueAt,
    DateTime? UpdatedAt,
    string WorkflowState,
    bool Published,
    double MaxMarks,
    CanvasSubmissionDto? Submission
);

public record CanvasSubmissionDto(
    double? FinalMark,
    string WorkflowState,
    DateTime? SubmittedAt,
    bool Late,
    bool Missing
);

public record CanvasAssignmentGroupDto(
    long Id,
    string Name,
    double Weight
);
