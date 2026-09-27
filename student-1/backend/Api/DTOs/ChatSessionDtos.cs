namespace Api.DTOs;

public sealed record ChatSessionHeaderDto(
    Guid Id,
    Guid StudentId,
    string Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int MessageCount,
    string? LastMessage);

public sealed record ChatMessageDetailDto(
    Guid Id,
    Guid ChatSessionId,
    string Role,
    string Content,
    DateTime CreatedAtUtc,
    string? CitationsJson = null,
    string? Confidence = null);

public sealed record ChatSessionDetailDto(
    Guid Id,
    Guid StudentId,
    string Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<ChatMessageDetailDto> Messages);

public sealed record CreateChatSessionRequestDto(
    Guid StudentId,
    string? Title = null);

public sealed record SendChatMessageRequestDto(
    string Content,
    bool IncludeRag = true);
