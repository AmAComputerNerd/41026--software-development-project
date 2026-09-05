namespace Api.DTOs;

// Body for DELETE /api/auth/delete-account.
public record DeleteAccountRequestDto(
    string Email,
    string Password
);