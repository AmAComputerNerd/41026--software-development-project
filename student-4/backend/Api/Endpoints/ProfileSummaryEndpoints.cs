using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class ProfileSummaryEndpoints
{
    public static IEndpointRouteBuilder MapProfileSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");

        group.MapPost("/{userId:guid}/profile-summary", GenerateProfileSummary);

        return endpoints;
    }

    private static async Task<IResult> GenerateProfileSummary(
        Guid userId,
        IAiProfileSummaryService aiService,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Results.NotFound();
        }

        // Load role-specific data for the prompt
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        var teacher = await db.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);

        // Generate the AI summary
        var summary = await aiService.GenerateSummaryAsync(user, student, teacher, cancellationToken);

        // Persist the summary
        user.UserProfile = summary;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { summary });
    }
}