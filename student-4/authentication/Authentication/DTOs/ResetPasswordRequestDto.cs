namespace Authentication.DTOs;

public record ResetPasswordRequestDto(
    string Token,
    string NewPassword
);
