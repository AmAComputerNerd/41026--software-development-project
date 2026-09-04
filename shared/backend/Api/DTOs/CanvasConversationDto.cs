namespace Api.DTOs;

public sealed record CreateCanvasConversationDto(
    IReadOnlyList<string> Recipients,
    string Subject,
    string Body,
    string ContextCode,
    bool GroupConversation
);

public sealed record CanvasConversationDto(string Id);