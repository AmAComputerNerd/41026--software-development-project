using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class PreferenceEndpoints
{
    public static IEndpointRouteBuilder MapPreferenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/preferences");
        group.MapGet("/", GetPreferences);
        group.MapGet("/{id:guid}", GetPreference);
        group.MapPost("/", CreatePreference);
        group.MapPut("/{id:guid}", UpdatePreference);
        group.MapDelete("/{id:guid}", DeletePreference);
        return endpoints;
    }

    private static async Task<IResult> GetPreferences([FromQuery] Guid studentId, AppDbContext db)
    {
        var preferenceDtos = await db.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .Select(p => p.ToDto())
            .ToListAsync();

        return Results.Ok(preferenceDtos);
    }

    private static async Task<IResult> GetPreference([FromRoute] Guid id, AppDbContext db)
    {
        var preference = await db.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return preference == null ? Results.NotFound() : Results.Ok(preference.ToDto());
    }

    private static async Task<IResult> CreatePreference(NotificationPreferenceRequestDto requestDto, AppDbContext db)
    {
        if (!Enum.TryParse(requestDto.Type, out NotificationType resolvedType))
        {
            return Results.BadRequest("`type` could not be correlated with a valid NotificationType");
        }

        if (!Enum.TryParse(requestDto.Channel, out NotificationChannel resolvedChannel))
        {
            return Results.BadRequest("`channel` could not be correlated with a valid NotificationChannel");
        }

        var preference = new NotificationPreference
        {
            StudentId = requestDto.StudentId,
            Type = resolvedType,
            Channel = resolvedChannel,
            Enabled = requestDto.Enabled,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.NotificationPreferences.Add(preference);
        await db.SaveChangesAsync();

        return Results.Created($"/preferences/{preference.Id}", preference.ToDto());
    }

    private static async Task<IResult> UpdatePreference([FromRoute] Guid id, NotificationPreferenceRequestDto requestDto, AppDbContext db)
    {
        var preference = await db.NotificationPreferences.FindAsync(id);

        if (preference is null)
        {
            return Results.NotFound();
        }

        if (!Enum.TryParse(requestDto.Type, out NotificationType resolvedType))
        {
            return Results.BadRequest("`type` could not be correlated with a valid NotificationType");
        }

        if (!Enum.TryParse(requestDto.Channel, out NotificationChannel resolvedChannel))
        {
            return Results.BadRequest("`channel` could not be correlated with a valid NotificationChannel");
        }

        preference.StudentId = requestDto.StudentId;
        preference.Type = resolvedType;
        preference.Channel = resolvedChannel;
        preference.Enabled = requestDto.Enabled;
        preference.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(preference.ToDto());
    }

    private static async Task<IResult> DeletePreference([FromRoute] Guid id, AppDbContext db)
    {
        var preference = await db.NotificationPreferences.FindAsync(id);

        if (preference is null)
        {
            return Results.NotFound();
        }

        db.NotificationPreferences.Remove(preference);
        await db.SaveChangesAsync();

        return Results.Ok();
    }
}
