namespace Api.DTOs;

public record NotificationPreferenceDto(
    Guid Id,
    Guid StudentId,
    string Type,
    string Channel,
    bool Enabled,
    DateTime UpdatedAtUtc
);
