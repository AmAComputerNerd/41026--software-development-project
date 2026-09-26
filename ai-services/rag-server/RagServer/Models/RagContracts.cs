namespace RagServer.Models;

public sealed record RagAnswerRequest(string Question, string Scope = "student-3");

public sealed record RagAnswerResponse(
    string Status,
    string Answer,
    string Confidence,
    IReadOnlyList<RagCitation> Citations,
    RagRetrievalSummary Retrieval);

public sealed record RagCitation(
    string SourceId,
    string Title,
    string Heading,
    double Score);

public sealed record RagRetrievalSummary(
    int MatchedChunks,
    int ConsideredChunks);

public sealed record RetrievedChunk(
    string SourceId,
    string Title,
    string Heading,
    string Content,
    double Score);
