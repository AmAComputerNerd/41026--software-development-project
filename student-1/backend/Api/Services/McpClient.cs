using System.Text.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Api.Services;

public sealed class McpClient(IOptions<McpServerOptions> options) : IMcpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly McpServerOptions _options = options.Value;

    public async Task<McpBroadcastResponseDto> BroadcastAlertAsync(
        Guid studentId,
        string title,
        string message,
        string urgency = "Medium",
        CancellationToken cancellationToken = default)
    {
        if (_options.Enabled is not true)
        {
            return new McpBroadcastResponseDto(
                Success: false,
                Status: "disabled",
                Tool: "notifications_broadcast_alert",
                Data: null,
                Error: "MCP integration is disabled in configuration.");
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_options.BaseUrl, UriKind.Absolute),
            Name = "student-1-mcp-client",
            TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false
        });

        try
        {
            await using var client = await ModelContextProtocol.Client.McpClient.CreateAsync(
                transport,
                cancellationToken: cancellationToken);

            var result = await client.CallToolAsync(
                "notifications_broadcast_alert",
                new Dictionary<string, object?>
                {
                    ["studentId"] = studentId.ToString(),
                    ["title"] = title,
                    ["message"] = message,
                    ["urgency"] = urgency
                },
                cancellationToken: cancellationToken);

            var structuredContent = result.StructuredContent;
            if (result.IsError is true || structuredContent is null)
            {
                return new McpBroadcastResponseDto(
                    Success: false,
                    Status: "error",
                    Tool: "notifications_broadcast_alert",
                    Data: null,
                    Error: "The MCP server did not return a structured result.");
            }

            var jsonElement = structuredContent.Value;
            var status = jsonElement.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString() ?? "unknown"
                : "unknown";

            return new McpBroadcastResponseDto(
                Success: status == "success",
                Status: status,
                Tool: "notifications_broadcast_alert",
                Data: jsonElement,
                Error: status == "error" && jsonElement.TryGetProperty("error", out var errProp)
                    ? errProp.ToString()
                    : null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new McpBroadcastResponseDto(
                Success: false,
                Status: "timeout",
                Tool: "notifications_broadcast_alert",
                Data: null,
                Error: "The MCP request timed out.");
        }
        catch (Exception ex)
        {
            return new McpBroadcastResponseDto(
                Success: false,
                Status: "unreachable",
                Tool: "notifications_broadcast_alert",
                Data: null,
                Error: $"The MCP server could not be reached: {ex.Message}");
        }
    }
}
