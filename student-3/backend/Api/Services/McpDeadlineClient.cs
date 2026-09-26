using System.Text.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Api.Services;

public sealed class McpDeadlineClient(IOptions<McpServerOptions> options)
    : IMcpDeadlineClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly McpServerOptions _options = options.Value;

    public async Task<McpDeadlineToolResultDto> GetUpcomingDeadlinesAsync(
        int days,
        int limit,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new McpIntegrationDisabledException();
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_options.BaseUrl, UriKind.Absolute),
            Name = "student-3-mcp-client",
            TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false
        });

        try
        {
            await using var client = await McpClient.CreateAsync(
                transport,
                cancellationToken: cancellationToken);
            var result = await client.CallToolAsync(
                "deadlines_list_upcoming",
                new Dictionary<string, object?>
                {
                    ["days"] = days,
                    ["limit"] = limit
                },
                cancellationToken: cancellationToken);

            var structuredContent = result.StructuredContent;
            if (result.IsError is true || structuredContent is null)
            {
                throw new McpServiceException(
                    "The MCP server did not return a structured deadline result.");
            }

            return structuredContent.Value.Deserialize<McpDeadlineToolResultDto>(JsonOptions)
                ?? throw new McpServiceException(
                    "The MCP server returned an empty deadline result.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new McpServiceException("The MCP request timed out.");
        }
        catch (McpServiceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new McpServiceException(
                "The MCP server could not complete the deadline request.",
                exception);
        }
    }
}

public sealed class McpIntegrationDisabledException()
    : Exception("MCP integration is disabled.");

public sealed class McpServiceException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
