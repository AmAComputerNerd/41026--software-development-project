using Api.DTOs;
using Student3.Contracts;

namespace Api.Services;

public interface IStudent3DatabaseClient
{
    Task<IReadOnlyList<TaskRecord>> GetTasksAsync(
        TaskFilterDto filter,
        CancellationToken cancellationToken);

    Task<TaskRecord?> GetTaskAsync(Guid id, CancellationToken cancellationToken);

    Task<TaskRecord> CreateTaskAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken);

    Task<TaskRecord?> UpdateTaskAsync(
        Guid id,
        UpdateTaskCommand command,
        CancellationToken cancellationToken);

    Task<bool> DeleteTaskAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskRecord>?> CreateSubtasksAsync(
        Guid parentId,
        CreateSubtasksCommand command,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseRecord>> GetCoursesAsync(
        bool includeInactiveCanvas,
        CancellationToken cancellationToken);

    Task<CourseRecord?> GetCourseAsync(Guid id, CancellationToken cancellationToken);

    Task<CanvasSyncResultRecord> ApplyCanvasSnapshotAsync(
        CanvasSnapshotCommand command,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskRecord>> GetDueRemindersAsync(
        int hoursBeforeDue,
        int finalHoursBeforeDue,
        CancellationToken cancellationToken);

    Task MarkReminderSentAsync(
        Guid id,
        DateTime sentAtUtc,
        CancellationToken cancellationToken);
}
