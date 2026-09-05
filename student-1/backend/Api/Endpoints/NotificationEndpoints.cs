using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/notifications");
        group.MapGet("/", GetNotifications);
        group.MapGet("/stream", StreamNotifications);
        group.MapGet("/{id:guid}", GetNotification);
        group.MapPut("/{id:guid}/read", MarkAsRead);
        group.MapPut("/{id:guid}/unread", MarkAsUnread);
        group.MapPut("/read-all", MarkAllAsRead);
        group.MapDelete("/{id:guid}", DeleteNotification);
        group.MapPost("/push", PushNotification);
        return endpoints;
    }

    private static async Task<IResult> GetNotifications([AsParameters] NotificationFilterDto filter, AppDbContext db)
    {
        var query = db.Notifications.AsNoTracking().AsQueryable();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(n => n.StudentId == filter.StudentId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Type))
        {
            query = query.Where(n => n.Type.ToString() == filter.Type);
        }

        if (filter.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == filter.IsRead.Value);
        }

        var notificationDtos = await query.Select(n => n.ToDto()).ToListAsync();

        return Results.Ok(notificationDtos);
    }

    private static async Task<IResult> GetNotification([FromRoute] Guid id, AppDbContext db)
    {
        var notification = await db.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id);

        return notification == null ? Results.NotFound() : Results.Ok(notification.ToDto());
    }

    private static async Task<IResult> MarkAsRead([FromRoute] Guid id, AppDbContext db)
    {
        var notification = await db.Notifications.FindAsync(id);

        if (notification is null)
        {
            return Results.NotFound();
        }

        notification.IsRead = true;
        await db.SaveChangesAsync();

        return Results.Ok(notification.ToDto());
    }

    private static async Task<IResult> MarkAsUnread([FromRoute] Guid id, AppDbContext db)
    {
        var notification = await db.Notifications.FindAsync(id);

        if (notification is null)
        {
            return Results.NotFound();
        }

        notification.IsRead = false;
        await db.SaveChangesAsync();

        return Results.Ok(notification.ToDto());
    }

    private static async Task<IResult> MarkAllAsRead([FromQuery] Guid studentId, AppDbContext db)
    {
        await db.Notifications
            .Where(n => n.StudentId == studentId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

        return Results.Ok();
    }

    private static async Task<IResult> DeleteNotification([FromRoute] Guid id, AppDbContext db)
    {
        var notification = await db.Notifications.FindAsync(id);

        if (notification is null)
        {
            return Results.NotFound();
        }

        db.Notifications.Remove(notification);
        await db.SaveChangesAsync();

        return Results.Ok();
    }

    private static readonly System.Text.Json.JsonSerializerOptions SseJsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    private static async Task StreamNotifications(
        [FromQuery] Guid studentId,
        HttpContext httpContext,
        INotificationStreamBroker broker,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";
        httpContext.Response.Headers["X-Accel-Buffering"] = "no";

        var reader = broker.Subscribe(studentId, cancellationToken);

        await httpContext.Response.WriteAsync("event: connected\ndata: {}\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        var pingTask = Task.Run(async () =>
        {
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    await httpContext.Response.WriteAsync(": ping\n\n", cancellationToken);
                    await httpContext.Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored on disconnect
            }
        }, cancellationToken);

        try
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var notification))
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(notification, SseJsonOptions);
                    await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await httpContext.Response.Body.FlushAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on client disconnect
        }
    }

    private static async Task<IResult> PushNotification(
        PushNotificationRequestDto requestDto,
        AppDbContext db,
        INotificationStreamBroker broker,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(requestDto.Type, out NotificationType resolvedType))
        {
            return Results.BadRequest("`type` could not be correlated with a valid NotificationType");
        }

        if (string.IsNullOrWhiteSpace(requestDto.SourceMicroservice))
        {
            return Results.BadRequest("`sourceMicroservice` is a required argument");
        }

        if (string.IsNullOrWhiteSpace(requestDto.Message))
        {
            return Results.BadRequest("`message` is a required argument");
        }

        var notification = new Notification
        {
            StudentId = requestDto.StudentId,
            Type = resolvedType,
            SourceMicroservice = requestDto.SourceMicroservice,
            Message = requestDto.Message,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
            RelatedEntityType = requestDto.RelatedEntityType,
            RelatedEntityId = requestDto.RelatedEntityId,
            ActionPayload = requestDto.ActionPayload
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        var dto = notification.ToDto();
        await broker.PublishAsync(dto, cancellationToken);

        return Results.Created($"/notifications/{notification.Id}", dto);
    }
}
