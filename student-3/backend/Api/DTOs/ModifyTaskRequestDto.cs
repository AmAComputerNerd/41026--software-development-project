using Api.Models;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.DTOs;

public record ModifyTaskRequestDto(
    string? NewTitle,
    bool UpdateDescription,
    string? NewDescription,
    DateTime? NewDueDate,
    string? NewPriority,
    string? NewStatus
);