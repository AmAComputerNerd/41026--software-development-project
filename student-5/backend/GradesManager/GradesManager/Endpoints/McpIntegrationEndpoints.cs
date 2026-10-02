using GradesManager.DTOs;
using GradesManager.Configuration;
using GradesManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GradesManager.Endpoints
{
    public static class McpIntegrationEndpoints
    {
        public static IEndpointRouteBuilder MapMcpIntegrationEndpoints(
            this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/integrations/mcp/assignment-weightings", GetWeightingsThroughMcp);
            endpoints.MapGet("/internal/ai-context/grades-weightings", GetWeightingsContext);
            return endpoints;
        }

        private static async Task<IResult> GetWeightingsThroughMcp(
            WeightingsRequestDto request,
            IMcpGradesClient mcpClient,
            CancellationToken cancellationToken)
        {
            if (request.Weight is < 1 or > 100)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["weight"] = ["Weight must be between 1 and 100."]
                });
            }

            if (request.Limit is < 1 or > 10)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["limit"] = ["Limit must be between 1 and 10."]
                });
            }

            var result = await mcpClient.GetWeightingsAsync(request.Weight, request.Limit, cancellationToken);
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

        private static async Task<IResult> GetWeightingsContext(
            [FromQuery] double weight,
            [FromQuery] int limit,
            IOptions<McpServerOptions> options,
            IDatabaseClient database,
            CancellationToken cancellationToken)
        {
            if (options.Value.Enabled is not true)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "The MCP integration is disabled.");
            }

            if (weight is < 1 or > 100 || limit is < 1 or > 10)
            {
                return Results.BadRequest(new
                {
                    error = "Weight must be between 1 and 100, and limit must be between 1 and 10."
                });
            }

            var now = DateTime.UtcNow;
            var assignments = await database.GetAssignmentsAsync(cancellationToken);
            var highestWeightAssignments = assignments
                .Where(a => a.Weight >= weight && a.Completed != true)
                .OrderByDescending(a => a.Weight)
                .Take(limit)
                .Select(a => new WeightingDto(
                    a.AssignmentId,
                    a.Name,
                    a.Weight,
                    a.MaxMark))
                .ToList();
            return Results.Ok(new WeightingsDto(
                weight,
                highestWeightAssignments.Count,
                now,
                highestWeightAssignments));
        }
    }
}
