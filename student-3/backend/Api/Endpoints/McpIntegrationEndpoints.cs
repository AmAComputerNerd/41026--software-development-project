using Api.Configuration;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Endpoints;

public static class McpIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapMcpIntegrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/integrations/mcp/upcoming-deadlines",
            GetUpcomingDeadlinesThroughMcp);
        endpoints.MapGet(
            "/internal/ai-context/upcoming-deadlines",
            GetUpcomingDeadlinesContext);
        return endpoints;
    }

    private static async Task<IResult> GetUpcomingDeadlinesThroughMcp(
        UpcomingDeadlinesRequestDto request,
        IMcpDeadlineClient mcpClient,
        CancellationToken cancellationToken)
    {
        if (request.Days is < 1 or > 90)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["days"] = ["Days must be between 1 and 90."]
            });
        }

        if (request.Limit is < 1 or > 50)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["limit"] = ["Limit must be between 1 and 50."]
            });
        }

        var result = await mcpClient.GetUpcomingDeadlinesAsync(
            request.Days,
            request.Limit,
            cancellationToken);
        return result.Status == "success"
            ? Results.Ok(result)
            : Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: result.Error?.Message ?? "The MCP tool failed.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error?.Code ?? "mcp_tool_error"
                });
    }

    private static async Task<IResult> GetUpcomingDeadlinesContext(
        [FromQuery] int days,
        [FromQuery] int limit,
        IOptions<McpServerOptions> options,
        IDatabaseClient database,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "MCP integration is disabled");
        }

        if (days is < 1 or > 90 || limit is < 1 or > 50)
        {
            return Results.BadRequest(new
            {
                error = "days must be 1-90 and limit must be 1-50."
            });
        }

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(days);
        var tasks = await database.GetTasksAsync(
            new TaskFilterDto(null, null, null, null, false, false),
            cancellationToken);
        var upcoming = tasks
            .Where(task =>
                task.Status != "Completed" &&
                task.DueDate.HasValue &&
                task.DueDate.Value >= now &&
                task.DueDate.Value <= cutoff)
            .OrderBy(task => task.DueDate)
            .ThenByDescending(task => PriorityRank(task.Priority))
            .Take(limit)
            .Select(task => new UpcomingDeadlineDto(
                task.Id,
                task.Title,
                task.DueDate!.Value,
                task.Priority,
                task.Status,
                task.CourseName))
            .ToList();

        return Results.Ok(new UpcomingDeadlinesDto(
            days,
            upcoming.Count,
            now,
            upcoming));
    }

    private static int PriorityRank(string priority) => priority switch
    {
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0
    };
}
