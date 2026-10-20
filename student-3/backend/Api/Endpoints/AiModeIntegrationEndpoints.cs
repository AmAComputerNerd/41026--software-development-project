using Api.Configuration;
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

    private static IResult GetAiModeCapabilities(
        IOptions<AiGatewayOptions> aiGatewayOptions,
        IOptions<MultiAgentServerOptions> multiAgentOptions,
        HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        return Results.Ok(new
        {
            AiModeEnabled = aiGatewayOptions.Value.Enabled,
            AiBreakdownEnabled = aiGatewayOptions.Value.Enabled is true && multiAgentOptions.Value.Enabled is true
        });
    }
}
