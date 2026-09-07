namespace Authentication.DTOs;

// Body for POST /api/auth/reset-password. The token is the raw
// value from the email link's `?token=` query parameter; the
// service hashes it (SHA-256) before sending it to the database.
public record ResetPasswordRequestDto(
    string Token,
    string NewPassword
);
