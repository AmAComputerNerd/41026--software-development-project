using Api.Configuration;
using Api.DTOs;
using Api.Services;
using Microsoft.Extensions.Options;

namespace Api.Endpoints;

public static class McpIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapMcpIntegrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/users/{userId:guid}/mcp-readiness", CheckReadinessThroughMcp);
        endpoints.MapGet("/internal/ai-context/accounts/{userId:guid}/readiness", GetReadinessContext);
        return endpoints;
    }

    private static async Task<IResult> CheckReadinessThroughMcp(
        Guid userId,
        McpReadinessClient client,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["userId"] = ["A non-empty account ID is required."]
            });
        }

        var result = await client.CheckReadinessAsync(userId, cancellationToken);
        return result.Status == "success"
            ? Results.Ok(result)
            : Results.Problem(
                statusCode: result.Error?.Code == "account_not_found"
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status502BadGateway,
                title: "Account readiness check unavailable",
                detail: result.Error?.Message ?? "The MCP account tool failed.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error?.Code ?? "mcp_tool_error"
                });
    }

    private static async Task<IResult> GetReadinessContext(
        Guid userId,
        IOptions<McpServerOptions> options,
        AccountReadinessService readinessService,
        CancellationToken cancellationToken)
    {
        if (options.Value.Enabled is not true)
        {
            throw new McpIntegrationDisabledException();
        }

        if (userId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "A non-empty account ID is required." });
        }

        try
        {
            var readiness = await readinessService.CheckAsync(userId, cancellationToken);
            return readiness is null ? Results.NotFound() : Results.Ok(readiness);
        }
        catch (DatabaseServiceException exception)
        {
            return Results.Problem(detail: exception.Message, statusCode: 503);
        }
    }
}
