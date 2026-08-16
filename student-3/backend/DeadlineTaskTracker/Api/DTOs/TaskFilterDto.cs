using Api.Models;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.DTOs;

public record TaskFilterDto(
    TaskStatus? Status,
    TaskPriority? Priority,
    Guid? CourseId,
    bool? Overdue
);