namespace Api.DTOs;

// Body for PUT /api/users/{userId}. Intentionally omits PasswordHash and
// UserType — those shouldn't be changeable through the profile-edit flow.
// UserProfile IS included so users can edit their AI-generated (or
// manually-written) profile summary when they click "change details".
public record UpdateUserRequestDto(
    string Email,
    string FirstName,
    string? MiddleNames,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    string? UserProfile
);
