namespace Api.DTOs;

public record NotificationDto(
    Guid Id,
    Guid StudentId,
    string Type,
    string SourceMicroservice,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string? ActionPayload = null
);
