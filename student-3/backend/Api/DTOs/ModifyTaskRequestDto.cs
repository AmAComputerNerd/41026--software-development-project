namespace Api.DTOs;

public record ModifyTaskRequestDto(
    string? NewTitle,
    bool UpdateDescription,
    string? NewDescription,
    bool UpdateDueDate,
    DateTime? NewDueDate,
    string? NewPriority,
    string? NewStatus
);