namespace Api.Configuration;

public sealed class McpServerOptions
{
    public const string SectionName = "McpServer";

    public bool Enabled { get; init; }

    public string BaseUrl { get; init; } = "http://mcp-server:8080/mcp";
}
