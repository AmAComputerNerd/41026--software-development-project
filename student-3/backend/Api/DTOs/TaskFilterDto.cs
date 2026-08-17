using Api.Models;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.DTOs;

public record TaskFilterDto(
    string? Status,
    string? Priority,
    Guid? CourseId,
    Guid? ParentTaskId,
    bool? Overdue
);