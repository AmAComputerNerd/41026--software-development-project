namespace Api.DTOs;

public record CanvasSyncResultDto(
    int CoursesCreated,
    int CoursesUpdated,
    int CoursesDeactivated,
    int TasksCreated,
    int TasksUpdated,
    int TasksDeactivated
);

public record SharedCanvasCourseDto(
    long Id,
    string Name,
    string? CourseCode,
    string WorkflowState
);

public record SharedCanvasAssignmentDto(
    long Id,
    long CourseId,
    string Name,
    string? Description,
    DateTimeOffset? DueAt,
    DateTimeOffset? UpdatedAt,
    string WorkflowState,
    bool Published,
    SharedCanvasSubmissionDto? Submission
);

public record SharedCanvasSubmissionDto(
    string WorkflowState,
    DateTimeOffset? SubmittedAt,
    bool Late,
    bool Missing
);
