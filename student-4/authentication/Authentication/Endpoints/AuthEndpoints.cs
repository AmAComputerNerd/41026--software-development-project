using Authentication.DTOs;
using Authentication.Extensions;
using Authentication.Services;
using Microsoft.AspNetCore.Mvc;
using Student4.Contracts;

namespace Authentication.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/login", Login);
        group.MapPost("/change-password", ChangePassword);
        group.MapDelete("/delete-account", DeleteAccount);

        // Password reset. Both endpoints are designed to be
        // non-enumerating: /forgot-password always returns 200 OK
        // even if the email is unknown, and /reset-password always
        // returns 400 if the token is invalid (never 404, to avoid
        // confirming token existence).
        group.MapPost("/forgot-password", ForgotPassword);
        group.MapPost("/reset-password", ResetPassword);

        return endpoints;
    }

    private static async Task<IResult> Login(
        IAccountDatabaseClient db,
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Email and password are required.");
        }

        try
        {
            var command = new LoginCommand(
                Email: request.Email,
                Password: request.Password
            );

            // Use the status-aware variant so a 401 from the database
            // (bad password) maps to our own 401, not 503.
            var user = await db.LoginWithStatusAsync(command, cancellationToken);
            return user is null ? Results.Unauthorized() : Results.Ok(user.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> ChangePassword(
        IAccountDatabaseClient db,
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest("Email, current password, and new password are required.");
        }

        try
        {
            var command = new ChangePasswordCommand(
                Email: request.Email,
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword
            );

            var success = await db.ChangePasswordAsync(command, cancellationToken);
            return success
                ? Results.Ok(new { message = "Password changed successfully." })
                : Results.Unauthorized();
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> DeleteAccount(
        IAccountDatabaseClient db,
        [FromBody] DeleteAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Email and password are required to delete account.");
        }

        try
        {
            var command = new DeleteAccountCommand(
                Email: request.Email,
                Password: request.Password
            );

            var success = await db.DeleteAccountAsync(command, cancellationToken);
            return success
                ? Results.Ok(new { message = "Account deleted successfully." })
                : Results.Unauthorized();
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> ForgotPassword(
        IAccountDatabaseClient db,
        IEmailSender email,
        PasswordResetTokenGenerator tokenGenerator,
        ILogger<PasswordResetTokenGenerator> logger,
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        // Always return 200 with the same body - we don't want to
        // leak whether the email is registered.
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
        }

        try
        {
            // Look up the user by email. We need their Id to create a
            // token. If they don't exist, we silently return success.
            var users = await db.GetUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u =>
                string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                logger.LogInformation(
                    "forgot-password requested for unknown email '{Email}' - silently returning success.",
                    request.Email);
                return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
            }

            var (rawToken, tokenHash) = tokenGenerator.Generate();
            var expiresAt = DateTime.UtcNow.Add(tokenGenerator.TokenLifetime);

            await db.CreatePasswordResetTokenAsync(
                new CreatePasswordResetTokenCommand(user.Id, tokenHash, expiresAt),
                cancellationToken);

            var link = tokenGenerator.BuildResetLink(rawToken);
            var subject = "Reset your Student 4 password";
            var text = $"""
                Hi {user.FirstName},

                Someone (hopefully you) requested a password reset for your Student 4 account.

                Open this link to choose a new password (it expires in {tokenGenerator.TokenLifetime.TotalMinutes:0} minutes):

                {link}

                If you didn't request this, you can safely ignore the email - your password will stay the same.

                - Student 4 Account Service
                """;

            await email.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}".Trim(),
                subject,
                text,
                htmlBody: null,
                cancellationToken);

            return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
        }
        catch (DatabaseServiceException ex)
        {
            // Even on a database blip, don't surface the error to the
            // caller - log and return success.
            logger.LogError(ex, "forgot-password: database service unavailable.");
            return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
        }
        catch (EmailSendException ex)
        {
            // Email failure: the user expects success, but operators
            // need to know it failed. Log and still return success.
            logger.LogError(ex, "forgot-password: email send failed for {Email}.", request.Email);
            return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
        }
    }

    private static async Task<IResult> ResetPassword(
        IAccountDatabaseClient db,
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Results.BadRequest("Token is required.");
        }
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return Results.BadRequest("New password must be at least 8 characters.");
        }

        try
        {
            // Hash the raw token exactly the same way the database
            // service stored it (and the same way we hashed on
            // generation). The database service never sees the raw
            // token, only the SHA-256 hash.
            var tokenHash = PasswordResetTokenGenerator.Hash(request.Token);

            var updatedUser = await db.ResetPasswordWithTokenAsync(
                new ResetPasswordWithTokenCommand(tokenHash, request.NewPassword),
                cancellationToken);

            // Don't reveal whether the token was unknown vs expired vs
            // used. From the user's perspective: "this link doesn't
            // work" is one error.
            if (updatedUser is null)
            {
                return Results.BadRequest("This reset link is invalid or has expired. Please request a new one.");
            }

            return Results.Ok(new { message = "Password reset successfully. You can now log in with your new password." });
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }
}
