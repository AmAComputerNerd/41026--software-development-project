using Microsoft.AspNetCore.Mvc;
using MultiAgentServer.Models;
using MultiAgentServer.Services;

namespace MultiAgentServer.Endpoints;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workflows");

        group.MapGet("/", ListWorkflows);
        group.MapGet("/{id:guid}", GetWorkflowById);
        group.MapPost("/run", RunWorkflow);
        group.MapPost("/{id:guid}/human-review", SubmitHumanReview);

        return endpoints;
    }

    private static IResult ListWorkflows(
        [FromQuery] string? owner,
        [FromQuery] Guid? studentId,
        WorkflowStore store)
    {
        var workflows = store.List(owner, studentId);
        return Results.Ok(workflows);
    }

    private static IResult GetWorkflowById(
        [FromRoute] Guid id,
        WorkflowStore store)
    {
        var workflow = store.GetById(id);
        return workflow is null
            ? Results.NotFound(new { error = $"Workflow {id} not found." })
            : Results.Ok(workflow);
    }

    private static async Task<IResult> RunWorkflow(
        [FromBody] RunWorkflowRequest request,
        MultiAgentCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goal) || request.Goal.Trim().Length < 5)
        {
            return Results.BadRequest(new { error = "Goal must be at least 5 characters." });
        }

        var record = await coordinator.RunWorkflowAsync(request, cancellationToken);
        return Results.Ok(record);
    }

    private static IResult SubmitHumanReview(
        [FromRoute] Guid id,
        [FromBody] HumanReviewRequest request,
        MultiAgentCoordinator coordinator)
    {
        if (!MultiAgentCoordinator.IsValidDecision(request.Decision))
        {
            return Results.BadRequest(new
            {
                error = "Decision must be one of: Approve, Correct, PartialAccept, Reject."
            });
        }

        try
        {
            var updated = coordinator.SubmitHumanReview(id, request);
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Workflow {id} not found." });
        }
    }
}
