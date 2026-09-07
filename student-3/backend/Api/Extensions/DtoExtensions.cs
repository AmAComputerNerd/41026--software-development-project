using Api.DTOs;
using Student3.Contracts;

namespace Api.Extensions;

public static class DtoExtensions
{
    public static TaskDto ToDto(this TaskRecord task)
    {
        return new TaskDto(
            Id: task.Id,
            Title: task.Title,
            Description: task.Description,
            DueDate: task.DueDate,
            Priority: task.Priority.ToString(),
            Status: task.Status.ToString(),
            CourseId: task.CourseId,
            CourseName: task.CourseName,
            ParentTaskId: task.ParentTaskId,
            ParentTaskTitle: task.ParentTaskTitle,
            CanvasAssignmentId: task.CanvasAssignmentId,
            CanvasUpdatedAt: task.CanvasUpdatedAt,
            CanvasWorkflowState: task.CanvasWorkflowState,
            CanvasSubmissionState: task.CanvasSubmissionState,
            CanvasIsActive: task.CanvasIsActive);
    }

    public static CourseDto ToDto(this CourseRecord course)
    {
        return new CourseDto(
            Id: course.Id,
            Code: course.Code,
            Name: course.Name,
            CanvasCourseId: course.CanvasCourseId,
            CanvasWorkflowState: course.CanvasWorkflowState,
            CanvasIsActive: course.CanvasIsActive,
            LastCanvasSyncAt: course.LastCanvasSyncAt);
    }
}