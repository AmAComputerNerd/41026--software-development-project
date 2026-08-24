namespace Api.DTOs;

public record PushNotificationRequestDto(
    Guid StudentId,
    string Type,
    string SourceMicroservice,
    string Message
);
