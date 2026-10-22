using GradesManager.Configuration;
using Microsoft.Extensions.Options;

namespace GradesManager.Endpoints;

public static class MultiAgentIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapMultiAgentIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/integrations/capabilities",
            GetMultiAgentCapabilities);
        return endpoints;
    }

    private static IResult GetMultiAgentCapabilities(
        IOptions<AiGatewayOptions> aiGatewayOptions,
        IOptions<MultiAgentServerOptions> multiAgentOptions,
        HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        return Results.Ok(new
        {
            AiModeEnabled = aiGatewayOptions.Value.Enabled is true,
            MultiAgentEnabled = multiAgentOptions.Value.Enabled is true
        });
    }
}