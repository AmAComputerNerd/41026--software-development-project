namespace Api.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    string Priority,
    string Status,
    Guid? CourseId,
    string? CourseName,
    Guid? ParentTaskId,
    string? ParentTaskTitle
);