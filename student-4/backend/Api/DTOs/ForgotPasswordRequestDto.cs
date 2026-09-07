namespace Api.DTOs;

// Body for POST /api/auth/forgot-password. The response is always
// 200 (we don't want to leak whether an email is registered), so
// callers should treat the call as fire-and-forget from the user's
// perspective.
public record ForgotPasswordRequestDto(string Email);
