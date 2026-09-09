using System.ComponentModel.DataAnnotations;

namespace GradesManager.Contracts;

public record CourseRecord(
    Guid CourseId,
    string Code,
    string Name,
    long? CanvasCourseId,
    bool? CanvasIsActive,
    DateTime? LastCanvasSyncAt
);

public record StudentRecord(
    Guid StudentId,
    string? Name,
    double? IdealMark
);

public record AssignmentRecord(
    Guid AssignmentId,
    Guid CourseId,
    string Name,
    double? Weight,
    int? MaxMark,
    bool? Completed
);

public record StudentAssignmentRecord(
    Guid StudentId,
    Guid AssignmentId,
    int? TempMark,
    int? FinalMark
);

public record StudentCourseRecord(
    Guid StudentId,
    Guid CourseId
);

public record CreateCourseCommand(
    [Required] string Code,
    [Required] string Name,
    long? CanvasCourseId,
    bool? CanvasIsActive,
    DateTime? LastCanvasSyncAt
);

public record UpdateCourseCommand(
    [Required] Guid CourseId,
    [Required] string Code,
    [Required] string Name,
    long? CanvasCourseId,
    bool? CanvasIsActive,
    DateTime? LastCanvasSyncAt
);

public record CreateStudentCommand(
    [Required] string? Name,
    double? IdealMark
);

public record UpdateStudentCommand(
    [Required] Guid StudentId,
    string? Name,
    double? IdealMark
);

public record CreateAssignmentCommand(
    [Required] Guid CourseId,
    [Required] string Name,
    double? Weight,
    int? MaxMark,
    bool? Completed
);

public record UpdateAssignmentCommand(
    [Required] Guid AssignmentId,
    [Required] Guid CourseId,
    [Required] string Name,
    double? Weight,
    int? MaxMark,
    bool? Completed
);

public record CreateStudentAssignmentCommand(
    [Required] Guid StudentId,
    [Required] Guid AssignmentId,
    int? TempMark,
    int? FinalMark
);

public record UpdateStudentAssignmentCommand(
    [Required] Guid StudentId,
    [Required] Guid AssignmentId,
    int? TempMark,
    int? FinalMark
);

public record CreateStudentCourseCommand(
    [Required] Guid StudentId,
    [Required] Guid CourseId
);

public record GetCoursesQuery(
    bool IncludeInactiveCanvas = false
);

public record GetAssignmentsByStudentQuery(
    [Required] Guid StudentId
);

public record GetAssignmentsByCourseQuery(
    [Required] Guid CourseId
);

public record GetStudentMarksQuery(
    [Required] Guid StudentId
);

public record OperationResult(
    bool Success,
    string? ErrorMessage = null
);

public record BulkOperationResult(
    int AffectedCount,
    string? ErrorMessage = null
);