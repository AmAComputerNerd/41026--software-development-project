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
        group.MapPost("/{id:guid}/revise", ReviseWorkflow);

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
        catch (WorkflowConflictException exception)
        {
            return Results.Conflict(new { error = exception.Message });
        }
    }

    private static async Task<IResult> ReviseWorkflow(
        Guid id, ReviseWorkflowRequest request, MultiAgentCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        if (request.RequestId == Guid.Empty || request.ExpectedRevision < 0 ||
            string.IsNullOrWhiteSpace(request.ReviewerName) || request.ReviewerName.Length > 100 ||
            string.IsNullOrWhiteSpace(request.Feedback) || request.Feedback.Length > 2000)
        {
            return Results.BadRequest(new { error = "Supply a request ID, current revision, reviewer name (1-100 characters) and feedback (1-2000 characters)." });
        }
        try
        {
            return Results.Ok(await coordinator.ReviseWorkflowAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Workflow {id} not found." });
        }
        catch (WorkflowConflictException exception)
        {
            return Results.Conflict(new { error = exception.Message });
        }
        catch (WorkflowExecutionException exception)
        {
            return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: exception.Message);
        }
    }
}
