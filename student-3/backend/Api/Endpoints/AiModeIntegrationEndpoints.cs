using Api.Configuration;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Endpoints;

public static class AiModeIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapAiModeIntegrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/integrations/capabilities",
            GetAiModeCapabilities);
        return endpoints;
    }

    private static async Task<IResult> GetAiModeCapabilities(
        IOptions<AiGatewayOptions> aiGatewayOptions,
        CancellationToken cancellationToken)
    {
        var response = Results.Ok(new { AiModeEnabled = aiGatewayOptions.Value.Enabled });
        response.Headers.CacheControl = "no-store";
        return response;
    }
}
