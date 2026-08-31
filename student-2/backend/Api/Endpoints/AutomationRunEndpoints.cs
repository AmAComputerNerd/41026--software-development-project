using Api.Data;
using Api.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class AutomationRunEndpoints
{
    public static IEndpointRouteBuilder MapAutomationRunEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/automation-runs");
        group.MapGet("/", GetAutomationRuns);
        group.MapGet("/{id:guid}", GetAutomationRun);
        return endpoints;
    }

    private static async Task<IResult> GetAutomationRuns(
        AppDbContext db,
        Guid? automationId,
        Guid? studentId,
        string? result)
    {
        var query = db.AutomationRuns.AsNoTracking().AsQueryable();

        if (automationId.HasValue)
        {
            query = query.Where(run => run.AutomationId == automationId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(run => run.Automation.StudentId == studentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            var normalizedResult = result.ToUpperInvariant();
            query = query.Where(run => run.Result == normalizedResult);
        }

        var runs = await query
            .OrderByDescending(run => run.ExecutionTimeStamp)
            .ToListAsync();

        return Results.Ok(runs.Select(run => run.ToDto()));
    }

    private static async Task<IResult> GetAutomationRun(Guid id, AppDbContext db)
    {
        var run = await db.AutomationRuns.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return run is null ? Results.NotFound() : Results.Ok(run.ToDto());
    }
}