using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Services;

public abstract class RemoteServiceHealthCheck(
    IHttpClientFactory httpClientFactory,
    string clientName,
    string serviceName,
    string healthPath) : IHealthCheck
{
    public const string SharedServiceClientName = "shared-service-health";
    public const string AiGatewayClientName = "ai-gateway-health";
    public const string DatabaseServiceClientName = "database-service-health";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(clientName);
            using var response = await client.GetAsync(
                healthPath,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{serviceName} is reachable.")
                : HealthCheckResult.Unhealthy(
                    $"{serviceName} returned HTTP {(int)response.StatusCode}.");
        }
        catch (HttpRequestException exception)
        {
            return HealthCheckResult.Unhealthy(
                $"{serviceName} could not be reached.",
                exception);
        }
        catch (TaskCanceledException exception)
        {
            return HealthCheckResult.Unhealthy(
                $"{serviceName} health check timed out.",
                exception);
        }
    }
}

public sealed class SharedServiceHealthCheck(IHttpClientFactory httpClientFactory)
    : RemoteServiceHealthCheck(
        httpClientFactory,
        SharedServiceClientName,
        "The shared service",
        "health/ready");

public sealed class AiGatewayHealthCheck(IHttpClientFactory httpClientFactory)
    : RemoteServiceHealthCheck(
        httpClientFactory,
        AiGatewayClientName,
        "The AI gateway",
        "health/ready");

public sealed class DatabaseServiceHealthCheck(IHttpClientFactory httpClientFactory)
    : RemoteServiceHealthCheck(
        httpClientFactory,
        DatabaseServiceClientName,
        "The database service",
        "health/ready");
