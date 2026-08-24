namespace Api.DTOs;

public record NotificationFilterDto(
    Guid? StudentId,
    string? Type,
    bool? IsRead
);
