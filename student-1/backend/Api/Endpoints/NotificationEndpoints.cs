using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/notifications");
        group.MapGet("/", GetNotifications);
        group.MapGet("/{id:guid}", GetNotification);
        group.MapPut("/{id:guid}/read", MarkAsRead);
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

    private static async Task<IResult> PushNotification(PushNotificationRequestDto requestDto, AppDbContext db)
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
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return Results.Created($"/notifications/{notification.Id}", notification.ToDto());
    }
}
