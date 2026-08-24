namespace Api.DTOs;

public record NotificationPreferenceRequestDto(
    Guid StudentId,
    string Type,
    string Channel,
    bool Enabled
);
