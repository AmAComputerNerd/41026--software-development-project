namespace Api.DTOs;

public sealed record RagQuestionRequestDto(string Question);

public sealed record RagAnswerRequestDto(string Question, string Scope);

public sealed record RagAnswerResponseDto(
    string Status,
    string Answer,
    string Confidence,
    IReadOnlyList<RagCitationDto> Citations,
    RagRetrievalSummaryDto Retrieval);

public sealed record RagCitationDto(string SourceId, string Title, string Heading, double Score);

public sealed record RagRetrievalSummaryDto(int MatchedChunks, int ConsideredChunks);
