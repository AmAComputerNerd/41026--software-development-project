using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class AiDigestEndpoints
{
    public static IEndpointRouteBuilder MapAiDigestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/digest");
        group.MapPost("/generate", GenerateDigest);
        group.MapGet("/", GetDigests);
        group.MapPost("/chat", AskAssistant);
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

    private static async Task<IResult> AskAssistant(
        [FromBody] AskAssistantRequestDto request,
        AppDbContext db,
        IAiDigestService aiDigestService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Results.BadRequest("`prompt` cannot be empty.");
        }

        var unreadNotifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.StudentId == request.StudentId && !n.IsRead)
            .OrderBy(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var reply = await aiDigestService.AskAssistantAsync(
            request.StudentId,
            request.Prompt,
            request.History,
            unreadNotifications,
            cancellationToken);

        return Results.Ok(new AskAssistantResponseDto(reply, DateTime.UtcNow));
    }
}
