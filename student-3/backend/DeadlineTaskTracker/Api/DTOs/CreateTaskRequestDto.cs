using Api.Models;

namespace Api.DTOs;

public record CreateTaskRequestDto(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    TaskPriority Priority,
    Guid? CourseId
);