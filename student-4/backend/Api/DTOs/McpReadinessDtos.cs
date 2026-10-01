namespace Api.DTOs;

public sealed record AccountReadinessDto(
    Guid UserId,
    string OverallStatus,
    DateTime CheckedAtUtc,
    IReadOnlyList<AccountReadinessCheckDto> Checks);

public sealed record AccountReadinessCheckDto(
    string Code,
    string Label,
    string Status,
    string Message,
    string? Action);

public sealed record McpReadinessToolResultDto(
    string Status,
    string Tool,
    AccountReadinessDto? Data,
    McpReadinessToolErrorDto? Error);

public sealed record McpReadinessToolErrorDto(string Code, string Message);
