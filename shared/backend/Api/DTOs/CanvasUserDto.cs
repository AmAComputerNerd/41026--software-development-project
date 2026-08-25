namespace Api.DTOs;

public record CanvasUserDto(
    long Id,
    string Name,
    string? Email,
    string? SisUserId,
    string? LoginId
);
