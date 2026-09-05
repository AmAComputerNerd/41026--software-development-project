namespace Api.DTOs;

public record ChatMessageDto(string Role, string Content);

public record AskAssistantRequestDto(
    Guid StudentId,
    string Prompt,
    List<ChatMessageDto>? History = null
);

public record AskAssistantResponseDto(
    string Reply,
    DateTime RepliedAtUtc
);
