using Api.Configuration;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Endpoints;

public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/integrations/mcp/automation-health",
            GetAutomationHealthThroughMcp);
        endpoints.MapPost("/api/integrations/rag/query", QueryRag);
        endpoints.MapGet(
            "/internal/ai-context/automation-health",
            GetAutomationHealthContext);
        return endpoints;
    }

    private static async Task<IResult> GetAutomationHealthThroughMcp(
        AutomationHealthRequestDto request,
        IMcpAutomationClient mcpClient,
        IOptions<McpServerOptions> options,
        CancellationToken cancellationToken)
    {
        if (request.Days is < 1 or > 90)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["days"] = ["Days must be between 1 and 90."]
            });
        }

        if (options.Value.Enabled is not true)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "MCP integration is disabled");
        }

        try
        {
            var result = await mcpClient.GetAutomationHealthAsync(
                request.Days,
                cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The MCP server could not complete the request.");
        }
    }

    private static async Task<IResult> QueryRag(
        RagQuestionRequestDto request,
        IRagClient ragClient,
        IOptions<RagServerOptions> options,
        CancellationToken cancellationToken)
    {
        var question = request.Question.Trim();
        if (question.Length is < 3 or > 1000)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["question"] = ["Question must contain between 3 and 1000 characters."]
            });
        }

        if (options.Value.Enabled is not true)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "RAG integration is disabled");
        }

        try
        {
            return Results.Ok(await ragClient.AnswerAsync(question, cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The RAG server could not complete the request.");
        }
    }

    private static async Task<IResult> GetAutomationHealthContext(
        [FromQuery] int days,
        IOptions<McpServerOptions> options,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (options.Value.Enabled is not true)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "MCP integration is disabled");
        }

        if (days is < 1 or > 90)
        {
            return Results.BadRequest(new { error = "days must be between 1 and 90." });
        }

        var generatedAt = DateTime.UtcNow;
        var cutoff = generatedAt.AddDays(-days);
        var enabledAutomations = await db.Automations
            .AsNoTracking()
            .Where(automation => automation.Enabled && !automation.Deleted)
            .CountAsync(cancellationToken);
        var runs = await db.AutomationRuns
            .AsNoTracking()
            .Where(run => run.ExecutionTimeStamp >= cutoff)
            .Select(run => new { run.Result, run.ExecutionTimeStamp })
            .ToListAsync(cancellationToken);
        var successfulRuns = runs.Count(run => run.Result == AutomationRunResult.Success);
        var failedRuns = runs.Count(run => run.Result == AutomationRunResult.Failed);
        var runningRuns = runs.Count(run => run.Result == AutomationRunResult.Running);
        var completedRuns = successfulRuns + failedRuns;
        double? successRate = completedRuns == 0
            ? null
            : Math.Round(successfulRuns * 100d / completedRuns, 1);

        return Results.Ok(new AutomationHealthMetricsDto(
            days,
            enabledAutomations,
            runs.Count,
            successfulRuns,
            failedRuns,
            runningRuns,
            successRate,
            runs.Count == 0 ? null : runs.Max(run => run.ExecutionTimeStamp),
            generatedAt));
    }
}