namespace Api.Configuration;

public sealed class McpServerOptions
{
    public const string SectionName = "McpServer";

    public bool Enabled { get; init; } = true;

    public string BaseUrl { get; init; } = "http://localhost:5002/mcp";
}
