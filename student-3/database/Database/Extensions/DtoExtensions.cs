using Database.Models;
using Student3.Contracts;

namespace Database.Extensions;

public static class DtoExtensions
{
    public static TaskRecord ToRecord(this TaskEntity task)
    {
        return new TaskRecord(
            task.Id,
            task.Title,
            task.Description,
            task.DueDate,
            task.Priority.ToString(),
            task.Status.ToString(),
            task.CourseId,
            task.Course?.Name,
            task.ParentTaskId,
            task.ParentTask?.Title,
            task.CanvasAssignmentId,
            task.CanvasUpdatedAt,
            task.CanvasWorkflowState,
            task.CanvasSubmissionState,
            task.CanvasIsActive,
            task.DueSoonReminderSentAtUtc);
    }

    public static CourseRecord ToRecord(this Course course)
    {
        return new CourseRecord(
            course.Id,
            course.Code,
            course.Name,
            course.CanvasCourseId,
            course.CanvasWorkflowState,
            course.CanvasIsActive,
            course.LastCanvasSyncAt);
    }
}
