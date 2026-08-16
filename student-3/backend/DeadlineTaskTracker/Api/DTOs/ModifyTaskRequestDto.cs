using Api.Models;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.DTOs;

public record ModifyTaskRequestDto(
    string? NewTitle,
    bool UpdateDescription,
    string? NewDescription,
    DateTimeOffset? NewDueDate,
    TaskPriority? NewPriority,
    TaskStatus? NewStatus,
    bool? UpdateCourseId,
    Guid? NewCourseId
);