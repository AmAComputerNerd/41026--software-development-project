namespace Student4.Contracts;

public sealed record PushNotificationDto(
    Guid StudentId,
    string Type,
    string SourceMicroservice,
    string Message,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null,
    string? ActionPayload = null
);
