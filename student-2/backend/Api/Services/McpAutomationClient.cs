using System.Text.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Api.Services;

public sealed class McpAutomationClient(IOptions<McpServerOptions> options)
    : IMcpAutomationClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly McpServerOptions _options = options.Value;

    public async Task<McpAutomationResultDto> GetAutomationHealthAsync(
        int days,
        CancellationToken cancellationToken)
    {
        if (_options.Enabled is not true)
        {
            throw new InvalidOperationException("MCP integration is disabled.");
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_options.BaseUrl, UriKind.Absolute),
            Name = "student-2-mcp-client",
            TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false
        });

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: cancellationToken);
        var result = await client.CallToolAsync(
            "automations_review_health",
            new Dictionary<string, object?> { ["days"] = days },
            cancellationToken: cancellationToken);

        if (result.IsError is true || result.StructuredContent is null)
        {
            throw new InvalidOperationException(
                "The MCP server did not return a structured automation health report.");
        }

        return result.StructuredContent.Value.Deserialize<McpAutomationResultDto>(JsonOptions)
            ?? throw new InvalidOperationException("The MCP server returned an empty result.");
    }
}