namespace Api.Configuration;

public sealed class RagServerOptions
{
    public const string SectionName = "RagServer";

    public bool Enabled { get; init; }

    public string BaseUrl { get; init; } = "http://rag-server:8080";
}
