using System.Security.Cryptography;
using System.Text;
using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        AppDbContext db,
        [FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Email and password are required.");
        }

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Email == request.Email)
            .Select(u => u.ToDto())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            // Don't reveal whether the email exists — return the same
            // 401 either way. Still do a dummy hash compare so the
            // response time is roughly constant.
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(string.Empty),
                Encoding.UTF8.GetBytes(string.Empty));
            return Results.Unauthorized();
        }

        // Constant-time comparison to avoid leaking info via timing.
        // Stored value is the raw password for now (see PasswordHash
        // field on UserDto — to be replaced with a real hash later).
        var storedBytes = Encoding.UTF8.GetBytes(user.PasswordHash);
        var providedBytes = Encoding.UTF8.GetBytes(request.Password);

        if (storedBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(user);
    }

    private static async Task<IResult> ChangePassword(
        AppDbContext db,
        [FromBody] ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest("Email, current password, and new password are required.");
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            // Don't reveal whether the email exists
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(string.Empty),
                Encoding.UTF8.GetBytes(string.Empty));
            return Results.Unauthorized();
        }

        // Verify current password
        var storedBytes = Encoding.UTF8.GetBytes(user.PasswordHash);
        var providedBytes = Encoding.UTF8.GetBytes(request.CurrentPassword);

        if (storedBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes))
        {
            return Results.Unauthorized();
        }

        // Update to new password (stored as-is for now; replace with BCrypt later)
        user.PasswordHash = request.NewPassword;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Password changed successfully." });
    }

    private static async Task<IResult> DeleteAccount(
        AppDbContext db,
        [FromBody] DeleteAccountRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Email and password are required to delete account.");
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            // Don't reveal whether the email exists
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(string.Empty),
                Encoding.UTF8.GetBytes(string.Empty));
            return Results.Unauthorized();
        }

        // Verify password
        var storedBytes = Encoding.UTF8.GetBytes(user.PasswordHash);
        var providedBytes = Encoding.UTF8.GetBytes(request.Password);

        if (storedBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes))
        {
            return Results.Unauthorized();
        }

        // Delete the user (cascades to Student/Teacher via EF Core relationships)
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Account deleted successfully." });
    }
}
