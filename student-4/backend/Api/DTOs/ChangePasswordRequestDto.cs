namespace Api.DTOs;

// Body for POST /api/auth/change-password.
public record ChangePasswordRequestDto(
    string Email,
    string CurrentPassword,
    string NewPassword
);