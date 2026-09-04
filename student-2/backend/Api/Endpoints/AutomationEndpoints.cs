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
        var validationError = request.Validate();
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var automation = request.CreateAutomation();
        db.Automations.Add(automation);
        await db.SaveChangesAsync();

        return Results.Created($"/api/automations/{automation.Id}", automation.ToDto());
    }

    private static async Task<IResult> UpdateAutomation(
        [FromRoute] Guid id,
        SaveAutomationRequestDto request,
        AppDbContext db)
    {
        var validationError = request.Validate();
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var automation = await db.Automations.FirstOrDefaultAsync(item => item.Id == id && !item.Deleted);
        if (automation is null)
        {
            return Results.NotFound();
        }

        if (!request.CanApplyTo(automation))
        {
            return Results.BadRequest("An automation's type cannot be changed.");
        }

        request.ApplyTo(automation);

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

}