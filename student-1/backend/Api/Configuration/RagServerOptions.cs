namespace Api.Configuration;

public sealed class RagServerOptions
{
    public const string SectionName = "RagServer";

    public bool Enabled { get; init; } = true;

    public string BaseUrl { get; init; } = "http://localhost:5003";
}
