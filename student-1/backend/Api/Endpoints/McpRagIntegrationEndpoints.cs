using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class McpRagIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapMcpRagIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notifications");

        group.MapPost("/mcp/broadcast", BroadcastMcpAlert);
        group.MapPost("/rag/query", QueryRagKnowledge);

        return endpoints;
    }

    private static async Task<IResult> BroadcastMcpAlert(
        [FromBody] McpBroadcastRequestDto request,
        IMcpClient mcpClient,
        CancellationToken cancellationToken)
    {
        if (request.StudentId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Valid studentId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length is < 3 or > 100)
        {
            return Results.BadRequest(new { error = "Title must be between 3 and 100 characters." });
        }

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length is < 5 or > 500)
        {
            return Results.BadRequest(new { error = "Message must be between 5 and 500 characters." });
        }

        var result = await mcpClient.BroadcastAlertAsync(
            request.StudentId,
            request.Title.Trim(),
            request.Message.Trim(),
            request.Urgency,
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> QueryRagKnowledge(
        [FromBody] RagQueryRequestDto request,
        IRagClient ragClient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Trim().Length < 3)
        {
            return Results.BadRequest(new { error = "Query must be at least 3 characters." });
        }

        var result = await ragClient.QueryAsync(
            request.Query.Trim(),
            request.Scope ?? "student-1",
            cancellationToken);

        return Results.Ok(result);
    }
}
