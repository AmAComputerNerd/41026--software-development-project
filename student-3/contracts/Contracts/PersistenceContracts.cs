namespace Student3.Contracts;

public sealed record CourseRecord(
    Guid Id,
    string Code,
    string Name,
    long? CanvasCourseId,
    string? CanvasWorkflowState,
    bool? CanvasIsActive,
    DateTime? LastCanvasSyncAt);

public sealed record TaskRecord(
    Guid Id,
    string Title,
    string? Description,
    DateTime? DueDate,
    string Priority,
    string Status,
    Guid? CourseId,
    string? CourseName,
    Guid? ParentTaskId,
    string? ParentTaskTitle,
    long? CanvasAssignmentId,
    DateTime? CanvasUpdatedAt,
    string? CanvasWorkflowState,
    string? CanvasSubmissionState,
    bool? CanvasIsActive,
    DateTime? DueSoonReminderSentAtUtc);

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    DateTime? DueDate,
    string Priority,
    Guid? CourseId,
    Guid? ParentTaskId);

public sealed record UpdateTaskCommand(
    string? NewTitle,
    bool UpdateDescription,
    string? NewDescription,
    bool UpdateDueDate,
    DateTime? NewDueDate,
    string? NewPriority,
    string? NewStatus);

public sealed record GeneratedSubtaskRecord(string Title, string? Description);

public sealed record CreateSubtasksCommand(
    string Priority,
    IReadOnlyList<GeneratedSubtaskRecord> Tasks);

public sealed record CanvasSnapshotCommand(IReadOnlyList<CanvasCourseSnapshot> Courses);

public sealed record CanvasCourseSnapshot(
    long Id,
    string Name,
    string? CourseCode,
    string WorkflowState,
    IReadOnlyList<CanvasAssignmentSnapshot> Assignments);

public sealed record CanvasAssignmentSnapshot(
    long Id,
    long CourseId,
    string Name,
    string? Description,
    DateTime? DueAt,
    DateTime? UpdatedAt,
    string WorkflowState,
    string? SubmissionState,
    bool IsSubmitted);

public sealed record CanvasSyncResultRecord(
    int CoursesCreated,
    int CoursesUpdated,
    int CoursesDeactivated,
    int TasksCreated,
    int TasksUpdated,
    int TasksDeactivated);

public sealed record MarkReminderSentCommand(DateTime SentAtUtc);
