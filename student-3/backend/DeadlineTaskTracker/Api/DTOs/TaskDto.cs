using Api.Models;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    TaskPriority Priority,
    TaskStatus Status,
    Guid? CourseId,
    string? CourseName
);