using Api.Data;
using Api.Extensions;
using Api.Models;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class AiDigestEndpoints
{
    public static IEndpointRouteBuilder MapAiDigestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/digest");
        group.MapPost("/generate", GenerateDigest);
        group.MapGet("/", GetDigests);
        return endpoints;
    }

    private static async Task<IResult> GenerateDigest(
        Guid studentId,
        AppDbContext db,
        IAiDigestService aiDigestService,
        ILoggerFactory loggerFactory)
    {
        var unreadNotifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.StudentId == studentId && !n.IsRead)
            .OrderBy(n => n.CreatedAtUtc)
            .ToListAsync();

        var summary = await aiDigestService.GenerateDigestAsync(studentId, unreadNotifications);

        var digest = new AiDigest
        {
            StudentId = studentId,
            Summary = summary,
            GeneratedAtUtc = DateTime.UtcNow
        };

        db.AiDigests.Add(digest);
        await db.SaveChangesAsync();
        
        return Results.Created($"/digest/{digest.Id}", digest.ToDto());
    }

    private static async Task<IResult> GetDigests(Guid studentId, AppDbContext db)
    {
        var digestDtos = await db.AiDigests
            .AsNoTracking()
            .Where(d => d.StudentId == studentId)
            .OrderByDescending(d => d.GeneratedAtUtc)
            .Select(d => d.ToDto())
            .ToListAsync();

        return Results.Ok(digestDtos);
    }
}
