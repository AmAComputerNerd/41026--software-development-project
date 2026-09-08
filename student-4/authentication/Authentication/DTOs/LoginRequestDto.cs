namespace Authentication.DTOs;

// Request body for POST /api/auth/login.
public record LoginRequestDto(
    string Email,
    string Password
);
