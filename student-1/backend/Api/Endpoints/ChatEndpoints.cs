using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/chat");

        group.MapGet("/sessions", GetSessions);
        group.MapPost("/sessions", CreateSession);
        group.MapGet("/sessions/{id:guid}", GetSessionDetail);
        group.MapDelete("/sessions/{id:guid}", DeleteSession);
        group.MapPost("/sessions/{id:guid}/initial-digest", GenerateInitialDigest);
        group.MapPost("/sessions/{id:guid}/messages", SendMessage);

        return endpoints;
    }

    private static async Task<IResult> GetSessions(
        [FromQuery] Guid? studentId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var targetStudentId = studentId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

        var sessions = await db.ChatSessions
            .AsNoTracking()
            .Where(s => s.StudentId == targetStudentId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Select(s => new ChatSessionHeaderDto(
                s.Id,
                s.StudentId,
                s.Title,
                s.CreatedAtUtc,
                s.UpdatedAtUtc,
                s.Messages.Count,
                s.Messages.OrderByDescending(m => m.CreatedAtUtc).Select(m => m.Content).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Results.Ok(sessions);
    }

    private static async Task<IResult> CreateSession(
        [FromBody] CreateChatSessionRequestDto request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"Chat {DateTime.UtcNow:MMM d, HH:mm}"
            : request.Title.Trim();

        var session = new ChatSession
        {
            StudentId = request.StudentId,
            Title = title,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.ChatSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var header = new ChatSessionHeaderDto(
            session.Id,
            session.StudentId,
            session.Title,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            0,
            null);

        return Results.Created($"/api/chat/sessions/{session.Id}", header);
    }

    private static async Task<IResult> GetSessionDetail(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var session = await db.ChatSessions
            .AsNoTracking()
            .Include(s => s.Messages.OrderBy(m => m.CreatedAtUtc))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
        {
            return Results.NotFound(new { error = $"Session {id} not found." });
        }

        var detail = new ChatSessionDetailDto(
            session.Id,
            session.StudentId,
            session.Title,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            session.Messages.Select(m => new ChatMessageDetailDto(
                m.Id,
                m.ChatSessionId,
                m.Role,
                m.Content,
                m.CreatedAtUtc,
                m.CitationsJson,
                m.Confidence)).ToList());

        return Results.Ok(detail);
    }

    private static async Task<IResult> DeleteSession(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var session = await db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return Results.NotFound(new { error = $"Session {id} not found." });
        }

        db.ChatSessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GenerateInitialDigest(
        [FromRoute] Guid id,
        [FromQuery] Guid? studentId,
        AppDbContext db,
        IChatAssistantService assistantService,
        CancellationToken cancellationToken)
    {
        var session = await db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return Results.NotFound(new { error = $"Session {id} not found." });
        }

        var targetStudentId = studentId ?? session.StudentId;
        var message = await assistantService.GenerateInitialDigestAsync(targetStudentId, id, cancellationToken);

        return Results.Ok(message);
    }

    private static async Task<IResult> SendMessage(
        [FromRoute] Guid id,
        [FromBody] SendChatMessageRequestDto request,
        [FromQuery] Guid? studentId,
        AppDbContext db,
        IChatAssistantService assistantService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(new { error = "Message content cannot be empty." });
        }

        var session = await db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return Results.NotFound(new { error = $"Session {id} not found." });
        }

        var targetStudentId = studentId ?? session.StudentId;
        var reply = await assistantService.SendMessageAsync(
            targetStudentId,
            id,
            request.Content.Trim(),
            request.IncludeRag,
            cancellationToken);

        return Results.Ok(reply);
    }
}
