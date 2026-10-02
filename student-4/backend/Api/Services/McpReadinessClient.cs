using System.Text.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Client;

namespace Api.Services;

public sealed class McpReadinessClient(IOptions<McpServerOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<McpReadinessToolResultDto> CheckReadinessAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (options.Value.Enabled is not true)
        {
            throw new McpIntegrationDisabledException();
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(options.Value.BaseUrl, UriKind.Absolute),
            Name = "student-4-mcp-client",
            TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false
        });

        try
        {
            await using var client = await McpClient.CreateAsync(
                transport,
                cancellationToken: timeout.Token);
            var result = await client.CallToolAsync(
                "accounts_check_readiness",
                new Dictionary<string, object?> { ["userId"] = userId.ToString() },
                cancellationToken: timeout.Token);
            if (result.IsError is true || result.StructuredContent is null)
            {
                throw new McpServiceException("MCP returned no structured account result.");
            }

            var readiness = result.StructuredContent.Value
                .Deserialize<McpReadinessToolResultDto>(JsonOptions);
            if (readiness is null || readiness.Tool != "accounts_check_readiness" ||
                (readiness.Status == "success" &&
                    (readiness.Data?.UserId != userId || readiness.Data.Checks is null ||
                     readiness.Data.Checks.Count != 4 ||
                     !IsCheckStatus(readiness.Data.OverallStatus) ||
                     readiness.Data.Checks.Any(check => check is null || !IsCheckStatus(check.Status)))) ||
                (readiness.Status != "success" && (readiness.Status != "error" || readiness.Error is null)))
            {
                throw new McpServiceException("MCP returned an invalid account result.");
            }

            return readiness;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new McpServiceException("The MCP account request timed out.", exception);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or McpException or JsonException or IOException)
        {
            throw new McpServiceException("The MCP account request failed.", exception);
        }

    }

    private static bool IsCheckStatus(string? status) =>
        status is "ready" or "needs_attention" or "unavailable";
}

public sealed class McpIntegrationDisabledException()
    : Exception("MCP integration is disabled.");

public sealed class McpServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
