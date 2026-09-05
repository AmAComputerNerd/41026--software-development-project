namespace Api.DTOs;

public sealed record CanvasRecipientDto(
    string Id,
    string Name,
    string Category,
    string? AvatarUrl
);