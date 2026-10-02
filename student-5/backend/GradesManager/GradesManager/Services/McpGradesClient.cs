using System.Text.Json;
using GradesManager.Configuration;
using GradesManager.DTOs;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace GradesManager.Services
{
    public sealed class McpGradesClient(IOptions<McpServerOptions> options)
    : IMcpGradesClient
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly McpServerOptions _options = options.Value;

        public async Task<McpGradesToolResultDto> GetWeightingsAsync(
            double weight,
            int limit,
            CancellationToken cancellationToken)
        {
            if (_options.Enabled is not true)
            {
                throw new McpIntegrationDisabledException();
            }

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(_options.BaseUrl, UriKind.Absolute),
                Name = "student-5-mcp-client",
                TransportMode = HttpTransportMode.StreamableHttp,
                EnableStandaloneGetStream = false
            });

            try
            {
                await using var client = await McpClient.CreateAsync(
                    transport,
                    cancellationToken: cancellationToken);
                var result = await client.CallToolAsync(
                    "grades_list_weightings",
                    new Dictionary<string, object?>
                    {
                        ["weight"] = weight,
                        ["limit"] = limit
                    },
                    cancellationToken: cancellationToken);

                var structuredContent = result.StructuredContent;
                if (result.IsError is true || structuredContent is null)
                {
                    throw new McpServiceException(
                        "The MCP server did not return a structured grades result.");
                }

                return structuredContent.Value.Deserialize<McpGradesToolResultDto>(JsonOptions)
                    ?? throw new McpServiceException(
                        "The MCP server returned an empty grades result.");
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
}
