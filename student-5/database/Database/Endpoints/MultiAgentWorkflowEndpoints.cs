using GradesManager.Contracts;
using GradesManager.Data;
using GradesManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints;

public static class MultiAgentWorkflowEndpoints
{
    public static IEndpointRouteBuilder MapMultiAgentWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/internal/multi-agent-workflows");

        group.MapGet("/student/{studentId:guid}", GetMultiAgentWorkflowByStudent);
        group.MapGet("/{workflowId:guid}", GetMultiAgentWorkflow);
        group.MapPost("/", CreateMultiAgentWorkflow);
        group.MapPut("/{workflowId:guid}", UpdateMultiAgentWorkflow);

        return endpoints;
    }

    private static async Task<IResult> GetMultiAgentWorkflowByStudent(
        [FromRoute] Guid studentId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty)
        {
            return Results.BadRequest("Student ID cannot be empty.");
        }

        var workflow = await db.MultiAgentWorkflows
            .AsNoTracking()
            .Where(w => w.StudentId == studentId)
            .OrderByDescending(w => w.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return workflow == null
            ? Results.NotFound()
            : Results.Ok(new MultiAgentWorkflowRecord(
                workflow.WorkflowId,
                workflow.StudentId,
                workflow.Goal,
                workflow.ResultJson,
                workflow.CreatedAt,
                workflow.UpdatedAt,
                workflow.Revision));
    }

    private static async Task<IResult> GetMultiAgentWorkflow(
        [FromRoute] Guid workflowId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (workflowId == Guid.Empty)
        {
            return Results.BadRequest("Workflow ID cannot be empty.");
        }

        var workflow = await db.MultiAgentWorkflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId, cancellationToken);

        return workflow == null
            ? Results.NotFound()
            : Results.Ok(new MultiAgentWorkflowRecord(
                workflow.WorkflowId,
                workflow.StudentId,
                workflow.Goal,
                workflow.ResultJson,
                workflow.CreatedAt,
                workflow.UpdatedAt,
                workflow.Revision));
    }

    private static async Task<IResult> CreateMultiAgentWorkflow(
        CreateMultiAgentWorkflowCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var workflow = new MultiAgentWorkflow
        {
            WorkflowId = command.WorkflowId,
            StudentId = command.StudentId,
            Goal = command.Goal,
            ResultJson = command.ResultJson,
            CreatedAt = command.CreatedAt,
            UpdatedAt = command.UpdatedAt,
            Revision = command.Revision
        };

        db.MultiAgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);

        var record = new MultiAgentWorkflowRecord(
            workflow.WorkflowId,
            workflow.StudentId,
            workflow.Goal,
            workflow.ResultJson,
            workflow.CreatedAt,
            workflow.UpdatedAt,
            workflow.Revision);
        return Results.Created($"/internal/multi-agent-workflows/{workflow.WorkflowId}", record);
    }

    private static async Task<IResult> UpdateMultiAgentWorkflow(
        [FromRoute] Guid workflowId,
        UpdateMultiAgentWorkflowCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (workflowId != command.WorkflowId)
        {
            return Results.BadRequest("Workflow ID mismatch.");
        }

        var workflow = await db.MultiAgentWorkflows.FindAsync([workflowId], cancellationToken);
        if (workflow == null)
        {
            return Results.NotFound();
        }

        workflow.StudentId = command.StudentId;
        workflow.Goal = command.Goal;
        workflow.ResultJson = command.ResultJson;
        workflow.UpdatedAt = command.UpdatedAt;
        workflow.Revision = command.Revision;

        await db.SaveChangesAsync(cancellationToken);

        var record = new MultiAgentWorkflowRecord(
            workflow.WorkflowId,
            workflow.StudentId,
            workflow.Goal,
            workflow.ResultJson,
            workflow.CreatedAt,
            workflow.UpdatedAt,
            workflow.Revision);
        return Results.Ok(record);
    }
}