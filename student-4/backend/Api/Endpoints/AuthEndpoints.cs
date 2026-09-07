using Api.DTOs;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Student4.Contracts;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/login", Login);
        group.MapPost("/change-password", ChangePassword);
        group.MapDelete("/delete-account", DeleteAccount);

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

            var user = await db.LoginAsync(command, cancellationToken);
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
}
