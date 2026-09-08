namespace Api.DTOs;

public record UpdateUserRequestDto(
    string Email,
    string FirstName,
    string? MiddleNames,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    string? UserProfile
);
