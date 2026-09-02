namespace Api.DTOs;

public sealed record PushNotificationDto(
    Guid StudentId,
    string Type,
    string SourceMicroservice,
    string Message,
    string? RelatedEntityType,
    Guid? RelatedEntityId
);
