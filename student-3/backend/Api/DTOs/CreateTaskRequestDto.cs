using Api.Models;

namespace Api.DTOs;

public record CreateTaskRequestDto(
    string Title,
    string? Description,
    DateTime? DueDate,
    string Priority,
    Guid? CourseId,
    Guid? ParentTaskId
);