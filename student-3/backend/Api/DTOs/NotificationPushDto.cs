namespace Api.DTOs;

public record NotificationPushDto(
    Guid StudentId,
    string Type,
    string SourceMicroservice,
    string Message
);
