namespace Api.Configuration;

public sealed class MultiAgentServerOptions
{
    public const string SectionName = "MultiAgentServer";

    public bool Enabled { get; init; } = true;

    public string BaseUrl { get; init; } = "http://localhost:5004";
}
