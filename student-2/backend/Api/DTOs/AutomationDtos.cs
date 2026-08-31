namespace Api.DTOs;

public sealed record AutomationDto(
    Guid Id,
    Guid StudentId,
    string Type,
    bool Enabled,
    int? BufferMinutes,
    string? Reason,
    string? FurtherDetails,
    DateTime? PostTime,
    IReadOnlyList<string>? Recipients,
    string? Subject,
    string? Body);

public sealed record SaveAutomationRequestDto(
    Guid StudentId,
    string Type,
    bool Enabled,
    int? BufferMinutes,
    string? Reason,
    string? FurtherDetails,
    DateTime? PostTime,
    IReadOnlyList<string>? Recipients,
    string? Subject,
    string? Body);

public sealed record AutomationRunDto(
    Guid Id,
    Guid AutomationId,
    string Type,
    DateTime ExecutionTimeStamp,
    string Result,
    string? AssignmentId,
    IReadOnlyList<string>? Recipients,
    string? Subject,
    string? Body);