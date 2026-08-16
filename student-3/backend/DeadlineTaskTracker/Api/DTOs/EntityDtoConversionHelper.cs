using Api.Models;

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
            Priority: task.Priority,
            Status: task.Status,
            CourseId: task.CourseId,
            CourseName: task.Course?.Name
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