namespace GradesManager.DTOs;

public record CanvasSyncResultDto(
    int CoursesCreated,
    int CoursesUpdated,
    int CoursesDeactivated,
    int AssignmentGroupsCreated,
    int AssignmentGroupsUpdated,
    int AssignmentGroupsDeactivated,
    int AssignmentsCreated,
    int AssignmentsUpdated,
    int AssignmentsDeactivated,
    int StudentsCreated,
    int StudentCoursesCreated,
    int StudentAssignmentsCreated
);

public record SharedCanvasCourseDto(
    long CanvasCourseId,
    string Name,
    string? CourseCode,
    string WorkflowState
);

public record SharedCanvasAssignmentDto(
    long CanvasAssignmentId,
    long CanvasCourseId,
    long CanvasAssignmentGroupId,
    string Name,
    string? Description,
    DateTime? DueAt,
    DateTime? UpdatedAt,
    string WorkflowState,
    bool Published,
    double? MaxMarks,
    SharedCanvasSubmissionDto? Submission
);

public record SharedCanvasAssignmentGroupDto(
    long CanvasAssignmentGroupId,
    string Name,
    double Weight
);

public record SharedCanvasSubmissionDto(
    double? FinalMark,
    string WorkflowState,
    DateTime? SubmittedAt,
    bool Late,
    bool Missing
);

public record SharedCanvasUserDto(
    long CanvasUserId,
    string Name
);