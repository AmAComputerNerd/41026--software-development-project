namespace Api.DTOs;

public record AiDigestDto(
    Guid Id,
    Guid StudentId,
    string Summary,
    DateTime GeneratedAtUtc
);
