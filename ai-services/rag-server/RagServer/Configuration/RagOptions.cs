namespace RagServer.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string CorpusPath { get; init; } = "Corpus";

    public int MaximumRetrievedChunks { get; init; } = 4;

    public double MinimumRelevanceScore { get; init; } = 0.2;
}

public sealed class AiGatewayOptions
{
    public const string SectionName = "AiGateway";

    public string BaseUrl { get; init; } = "http://ai-mode:8080";
}
