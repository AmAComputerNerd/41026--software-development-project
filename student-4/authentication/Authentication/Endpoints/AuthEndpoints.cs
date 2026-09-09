using Authentication.DTOs;
using Authentication.Extensions;
using Authentication.Services;
using Microsoft.AspNetCore.Mvc;
using Student4.Contracts;

namespace Authentication.Endpoints;

public static partial class AuthEndpoints
{
    [LoggerMessage(LogLevel.Information, "forgot-password requested for unknown email '{Email}' - silently returning success.")]
    private static partial void LogUnknownEmail(ILogger logger, string email);

    [LoggerMessage(LogLevel.Error, "forgot-password: database service unavailable.")]
    private static partial void LogDatabaseUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "forgot-password: email send failed for {Email}.")]
    private static partial void LogEmailSendFailed(ILogger logger, Exception exception, string email);

    [LoggerMessage(LogLevel.Warning, "Failed to push notification for {Email}.")]
    private static partial void LogNotificationFailed(ILogger logger, Exception exception, string email);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/login", Login);
        group.MapPost("/change-password", ChangePassword);
        group.MapDelete("/delete-account", DeleteAccount);

        // Password Reset - forgot-password returns 200 OK and reset-password returns 400
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
        INotificationClient notificationClient,
        ILogger<PasswordResetTokenGenerator> logger,
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
            if (!success)
            {
                return Results.Unauthorized();
            }

            try
            {
                var users = await db.GetUsersAsync(cancellationToken);
                var user = users.FirstOrDefault(u => string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));
                var userId = user?.Id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: userId,
                    Type: "Account",
                    SourceMicroservice: "account",
                    Message: "Your account password has been changed successfully.",
                    RelatedEntityType: "User",
                    RelatedEntityId: userId), cancellationToken);
            }
            catch (Exception ex)
            {
                LogNotificationFailed(logger, ex, request.Email);
            }

            return Results.Ok(new { message = "Password changed successfully." });
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
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
        }

        try
        {
            var users = await db.GetUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u =>
                string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                LogUnknownEmail(logger, request.Email);
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
            LogDatabaseUnavailable(logger, ex);
            return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
        }
        catch (EmailSendException ex)
        {
            LogEmailSendFailed(logger, ex, request.Email);
            return Results.Ok(new { message = "If that email is registered, a reset link has been sent." });
        }
    }

    private static async Task<IResult> ResetPassword(
        IAccountDatabaseClient db,
        INotificationClient notificationClient,
        ILogger<PasswordResetTokenGenerator> logger,
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

            try
            {
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: updatedUser.Id,
                    Type: "Account",
                    SourceMicroservice: "account",
                    Message: "Your account password has been reset successfully.",
                    RelatedEntityType: "User",
                    RelatedEntityId: updatedUser.Id), cancellationToken);
            }
            catch (Exception ex)
            {
                LogNotificationFailed(logger, ex, updatedUser.Email);
            }

            return Results.Ok(new { message = "Password reset successfully. You can now log in with your new password." });
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }
}
