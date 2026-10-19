using Api.Data;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class MultiAgentIntegrationEndpoints
{
    private static readonly string[] Agents = ["Planner", "Worker", "Reviewer", "HumanReview"];

    private static readonly HashSet<string> ValidDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Approve",
        "Correct",
        "PartialAccept",
        "Reject"
    };

    public static IEndpointRouteBuilder MapMultiAgentIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notifications/multi-agent");

        group.MapGet("/status", GetStatus);
        group.MapGet("/workflows", ListWorkflows);
        group.MapGet("/workflows/{id:guid}", GetWorkflowById);
        group.MapPost("/run", RunWorkflow);
        group.MapPost("/workflows/{id:guid}/review", SubmitHumanReview);

        return endpoints;
    }

    private static IResult GetStatus(IMultiAgentClient multiAgentClient) =>
        Results.Ok(new
        {
            enabled = multiAgentClient.IsEnabled,
            service = "multi-agent-server",
            agents = Agents
        });

    private static async Task<IResult> ListWorkflows(
        [FromQuery] Guid? studentId,
        IMultiAgentClient multiAgentClient,
        CancellationToken cancellationToken)
    {
        var workflows = await multiAgentClient.ListWorkflowsAsync(studentId, cancellationToken);
        return Results.Ok(workflows);
    }

    private static async Task<IResult> GetWorkflowById(
        [FromRoute] Guid id,
        IMultiAgentClient multiAgentClient,
        CancellationToken cancellationToken)
    {
        if (!multiAgentClient.IsEnabled)
        {
            return Results.Problem(
                title: "Multi-Agent Service Disabled",
                detail: "Multi-Agent Server integration is disabled for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var workflow = await multiAgentClient.GetWorkflowByIdAsync(id, cancellationToken);
        return workflow is null
            ? Results.NotFound(new { error = $"Workflow {id} not found." })
            : Results.Ok(workflow);
    }

    private static async Task<IResult> RunWorkflow(
        [FromBody] StartMultiAgentWorkflowRequestDto request,
        AppDbContext db,
        IMultiAgentClient multiAgentClient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goal) || request.Goal.Trim().Length < 5)
        {
            return Results.BadRequest(new { error = "Goal must be at least 5 characters." });
        }

        if (!multiAgentClient.IsEnabled)
        {
            return Results.Problem(
                title: "Multi-Agent Service Disabled",
                detail: "Multi-Agent Server integration is disabled for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var targetStudentId = request.StudentId is { } sid && sid != Guid.Empty
            ? sid
            : Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Gather live Student 1 PostgreSQL notification snapshot for the Worker agent
        var recentNotifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.StudentId == targetStudentId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(8)
            .Select(n => $"{n.Type} [{(n.IsRead ? "Read" : "Unread")}]: {n.Message} ({n.SourceMicroservice})")
            .ToListAsync(cancellationToken);

        var evidenceSnapshot = recentNotifications.Count > 0
            ? $"PostgreSQL notifications_db ({recentNotifications.Count} items): " + string.Join("; ", recentNotifications)
            : "PostgreSQL notifications_db: No unread notifications for student.";

        try
        {
            var workflow = await multiAgentClient.RunWorkflowAsync(
                targetStudentId,
                request.Goal.Trim(),
                evidenceSnapshot,
                cancellationToken);
            return Results.Ok(workflow);
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                title: "Multi-Agent Server Unreachable",
                detail: $"Failed to contact multi-agent-server: {ex.Message}",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> SubmitHumanReview(
        [FromRoute] Guid id,
        [FromBody] SubmitHumanReviewRequestDto request,
        IMultiAgentClient multiAgentClient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Decision) || !ValidDecisions.Contains(request.Decision.Trim()))
        {
            return Results.BadRequest(new
            {
                error = "Decision must be one of: Approve, Correct, PartialAccept, Reject."
            });
        }

        if (!multiAgentClient.IsEnabled)
        {
            return Results.Problem(
                title: "Multi-Agent Service Disabled",
                detail: "Multi-Agent Server integration is disabled for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            var updated = await multiAgentClient.SubmitHumanReviewAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Workflow {id} not found." });
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                title: "Multi-Agent Server Unreachable",
                detail: $"Failed to contact multi-agent-server: {ex.Message}",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
