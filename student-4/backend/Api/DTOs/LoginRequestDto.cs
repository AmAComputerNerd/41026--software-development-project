namespace Api.DTOs;

// Request body for POST /api/auth/login.
// The password is compared against the user record's PasswordHash field using
// a constant-time string comparison. This is a stopgap until we swap to a
// real hashing library (BCrypt, etc.) — the field is named PasswordHash for
// forward-compatibility.
public record LoginRequestDto(
    string Email,
    string Password
);
