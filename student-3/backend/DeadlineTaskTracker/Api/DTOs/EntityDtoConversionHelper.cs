using Api.Models;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.DTOs;

public static class EntityDtoConversionHelper
{
    public static TaskDto ToDto(this TaskEntity task)
    {
        return new TaskDto(
            Id: task.Id,
            Title: task.Title,
            Description: task.Description,
            DueDate: task.DueDate,
            Priority: task.Priority.ToString(),
            Status: task.Status.ToString(),
            CourseId: task.CourseId,
            CourseName: task.Course?.Name,
            ParentTaskId: task.ParentTaskId,
            ParentTaskTitle: task.ParentTask?.Title
        );
    }

    public static CourseDto ToDto(this Course course)
    {
        // Currently a copy of Course. Useful to keep as its own DTO for now to make things easier for when the Canvas API integration happens.
        return new CourseDto(
            Id: course.Id,
            Code: course.Code,
            Name: course.Name
        );
    }
}