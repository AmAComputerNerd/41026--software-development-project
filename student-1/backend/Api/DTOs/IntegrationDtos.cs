namespace Api.DTOs;

public sealed record McpBroadcastRequestDto(
    Guid StudentId,
    string Title,
    string Message,
    string Urgency = "Medium");

public sealed record McpBroadcastResponseDto(
    bool Success,
    string Status,
    string Tool,
    object? Data,
    string? Error = null);

public sealed record RagQueryRequestDto(
    string Query,
    string? Scope = "all");

public sealed record RagQueryResponseDto(
    string Question,
    string Answer,
    List<string> Citations,
    string Confidence,
    bool HasSufficientContext);
