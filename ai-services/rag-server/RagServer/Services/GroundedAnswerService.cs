using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RagServer.Models;

namespace RagServer.Services;

public sealed class GroundedAnswerService(HttpClient httpClient, ProjectCorpus corpus)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<RagAnswerResponse> AnswerAsync(
        string question,
        CancellationToken cancellationToken)
    {
        var retrieved = corpus.Retrieve(question);
        if (retrieved.Count == 0)
        {
            return new RagAnswerResponse(
                "insufficient_context",
                "Insufficient project context was found to answer this question.",
                "insufficient",
                [],
                new RagRetrievalSummary(0, corpus.ChunkCount));
        }

        var context = string.Join(
            "\n\n",
            retrieved.Select((chunk, index) =>
                $"[SOURCE {index + 1}: {chunk.SourceId} / {chunk.Heading}]\n{chunk.Content}"));
        var request = new ChatCompletionRequest(
            [
                new ChatMessage(
                    "system",
                    "Answer using only the supplied project excerpts. " +
                    "Do not add facts that are absent from them. " +
                    "If the excerpts do not answer the question, respond exactly: " +
                    "Insufficient project context was found to answer this question."),
                new ChatMessage(
                    "user",
                    $"Question:\n{question}\n\nProject excerpts:\n{context}")
            ],
            0.1,
            350);

        using var response = await httpClient.PostAsJsonAsync(
            "v1/chat/completions",
            request,
            JsonOptions,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new RagGenerationException(
                $"The AI gateway returned HTTP {(int)response.StatusCode}.");
        }

        ChatCompletionResponse? completion;
        try
        {
            completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new RagGenerationException(
                "The AI gateway returned an unreadable response.",
                exception);
        }

        var answer = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new RagGenerationException("The AI gateway returned an empty answer.");
        }

        if (answer.Equals(
                "Insufficient project context was found to answer this question.",
                StringComparison.OrdinalIgnoreCase))
        {
            return new RagAnswerResponse(
                "insufficient_context",
                answer,
                "insufficient",
                [],
                new RagRetrievalSummary(retrieved.Count, corpus.ChunkCount));
        }

        var confidence = CalculateConfidence(retrieved);
        var citations = retrieved
            .GroupBy(chunk => new { chunk.SourceId, chunk.Heading })
            .Select(group => group.First())
            .Select(chunk => new RagCitation(
                chunk.SourceId,
                chunk.Title,
                chunk.Heading,
                chunk.Score))
            .ToList();
        return new RagAnswerResponse(
            "success",
            answer,
            confidence,
            citations,
            new RagRetrievalSummary(retrieved.Count, corpus.ChunkCount));
    }

    private static string CalculateConfidence(IReadOnlyList<RetrievedChunk> retrieved)
    {
        var bestScore = retrieved[0].Score;
        var distinctSources = retrieved
            .Select(chunk => chunk.SourceId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (bestScore >= 0.7 && distinctSources >= 2)
        {
            return "high";
        }

        return bestScore >= 0.45 ? "medium" : "low";
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice>? Choices { get; init; }
    }

    private sealed class ChatCompletionChoice
    {
        [JsonPropertyName("message")]
        public ChatCompletionMessage? Message { get; init; }
    }

    private sealed class ChatCompletionMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
}

public sealed class RagGenerationException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
