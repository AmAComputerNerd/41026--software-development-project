using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class AutomationEndpoints
{
    public static IEndpointRouteBuilder MapAutomationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/automations");
        group.MapGet("/", GetAutomations);
        group.MapGet("/{id:guid}", GetAutomation);
        group.MapPost("/", AddAutomation);
        group.MapPut("/{id:guid}", UpdateAutomation);
        group.MapDelete("/{id:guid}", DeleteAutomation);
        return endpoints;
    }

    private static async Task<IResult> GetAutomations(
        AppDbContext db,
        Guid? studentId,
        bool includeDeleted = false)
    {
        var query = db.Automations.AsNoTracking().AsQueryable();

        if (studentId.HasValue)
        {
            query = query.Where(automation => automation.StudentId == studentId.Value);
        }

        if (!includeDeleted)
        {
            query = query.Where(automation => !automation.Deleted);
        }

        var automations = await query
            .OrderByDescending(automation => automation.Enabled)
            .ThenBy(automation => automation.Id)
            .ToListAsync();

        return Results.Ok(automations.Select(automation => automation.ToDto()));
    }

    private static async Task<IResult> GetAutomation([FromRoute] Guid id, AppDbContext db)
    {
        var automation = await db.Automations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && !item.Deleted);

        return automation is null ? Results.NotFound() : Results.Ok(automation.ToDto());
    }

    private static async Task<IResult> AddAutomation(SaveAutomationRequestDto request, AppDbContext db)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var automation = CreateAutomation(request);
        db.Automations.Add(automation);
        await db.SaveChangesAsync();

        return Results.Created($"/api/automations/{automation.Id}", automation.ToDto());
    }

    private static async Task<IResult> UpdateAutomation(
        [FromRoute] Guid id,
        SaveAutomationRequestDto request,
        AppDbContext db)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var automation = await db.Automations.FirstOrDefaultAsync(item => item.Id == id && !item.Deleted);
        if (automation is null)
        {
            return Results.NotFound();
        }

        if (!MatchesType(automation, request.Type))
        {
            return Results.BadRequest("An automation's type cannot be changed.");
        }

        automation.StudentId = request.StudentId;
        automation.Enabled = request.Enabled;

        switch (automation)
        {
            case AssignmentExtensionAutomation extension:
                extension.BufferMinutes = request.BufferMinutes!.Value;
                extension.Reason = request.Reason!.Trim();
                extension.FurtherDetails = request.FurtherDetails?.Trim() ?? string.Empty;
                break;
            case ScheduledPostAutomation post:
                post.PostTime = request.PostTime!.Value;
                post.Recipients = DtoExtensions.SerializeRecipients(request.Recipients!);
                post.Subject = request.Subject!.Trim();
                post.Body = request.Body!.Trim();
                break;
        }

        await db.SaveChangesAsync();
        return Results.Ok(automation.ToDto());
    }

    private static async Task<IResult> DeleteAutomation([FromRoute] Guid id, AppDbContext db)
    {
        var automation = await db.Automations.FirstOrDefaultAsync(item => item.Id == id && !item.Deleted);
        if (automation is null)
        {
            return Results.NotFound();
        }

        automation.Deleted = true;
        automation.Enabled = false;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static Automation CreateAutomation(SaveAutomationRequestDto request)
    {
        return request.Type.Trim() switch
        {
            "AssignmentExtension" => new AssignmentExtensionAutomation
            {
                StudentId = request.StudentId,
                Enabled = request.Enabled,
                BufferMinutes = request.BufferMinutes!.Value,
                Reason = request.Reason!.Trim(),
                FurtherDetails = request.FurtherDetails?.Trim() ?? string.Empty
            },
            "ScheduledPost" => new ScheduledPostAutomation
            {
                StudentId = request.StudentId,
                Enabled = request.Enabled,
                PostTime = request.PostTime!.Value,
                Recipients = DtoExtensions.SerializeRecipients(request.Recipients!),
                Subject = request.Subject!.Trim(),
                Body = request.Body!.Trim()
            },
            _ => throw new InvalidOperationException("Unsupported automation type.")
        };
    }

    private static string? Validate(SaveAutomationRequestDto request)
    {
        if (request.StudentId == Guid.Empty)
        {
            return "`studentId` is required.";
        }

        if (request.Type == "AssignmentExtension")
        {
            if (request.BufferMinutes is null or < 0)
            {
                return "`bufferMinutes` must be zero or greater.";
            }

            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            {
                return "`reason` must contain between 1 and 500 characters.";
            }

            if (request.FurtherDetails?.Length > 2000)
            {
                return "`furtherDetails` cannot exceed 2000 characters.";
            }

            return null;
        }

        if (request.Type == "ScheduledPost")
        {
            if (!request.PostTime.HasValue)
            {
                return "`postTime` is required.";
            }

            if (request.Recipients is null || request.Recipients.Count == 0 ||
                request.Recipients.Any(string.IsNullOrWhiteSpace))
            {
                return "At least one recipient is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 200)
            {
                return "`subject` must contain between 1 and 200 characters.";
            }

            if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 10000)
            {
                return "`body` must contain between 1 and 10000 characters.";
            }

            return null;
        }

        return "`type` must be AssignmentExtension or ScheduledPost.";
    }

    private static bool MatchesType(Automation automation, string type)
    {
        return automation is AssignmentExtensionAutomation && type == "AssignmentExtension" ||
            automation is ScheduledPostAutomation && type == "ScheduledPost";
    }
}