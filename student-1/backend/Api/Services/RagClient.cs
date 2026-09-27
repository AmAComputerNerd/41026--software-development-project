using System.Net.Http.Json;
using System.Text.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class RagClient(
    HttpClient httpClient,
    IOptions<RagServerOptions> options) : IRagClient
{
    private sealed record RagAnswerPayload(string Question, string Scope);

    private sealed record RagCitationData(
        string SourceId,
        string Title,
        string Heading,
        double Score);

    private sealed record RagAnswerResponseData(
        string Status,
        string Answer,
        string Confidence,
        IReadOnlyList<RagCitationData> Citations);

    private readonly RagServerOptions _options = options.Value;

    public async Task<RagQueryResponseDto> QueryAsync(
        string question,
        string scope = "student-1",
        CancellationToken cancellationToken = default)
    {
        if (_options.Enabled is not true)
        {
            return new RagQueryResponseDto(
                Question: question,
                Answer: "RAG integration is currently disabled in configuration.",
                Citations: [],
                Confidence: "DISABLED",
                HasSufficientContext: false);
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/answers",
                new RagAnswerPayload(question, scope),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new RagQueryResponseDto(
                    Question: question,
                    Answer: $"RAG query returned HTTP {(int)response.StatusCode}: {errorBody}",
                    Citations: [],
                    Confidence: "LOW",
                    HasSufficientContext: false);
            }

            var data = await response.Content.ReadFromJsonAsync<RagAnswerResponseData>(
                cancellationToken: cancellationToken);

            if (data is null)
            {
                return new RagQueryResponseDto(
                    Question: question,
                    Answer: "The RAG server returned an empty response.",
                    Citations: [],
                    Confidence: "LOW",
                    HasSufficientContext: false);
            }

            var citations = data.Citations?
                .Select(c => $"{c.Title} ({c.SourceId})")
                .Distinct()
                .ToList() ?? [];

            var isGrounded = string.Equals(data.Status, "grounded", StringComparison.OrdinalIgnoreCase);

            return new RagQueryResponseDto(
                Question: question,
                Answer: data.Answer,
                Citations: citations,
                Confidence: data.Confidence ?? "LOW",
                HasSufficientContext: isGrounded);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RagQueryResponseDto(
                Question: question,
                Answer: "The RAG query timed out.",
                Citations: [],
                Confidence: "LOW",
                HasSufficientContext: false);
        }
        catch (Exception ex)
        {
            return new RagQueryResponseDto(
                Question: question,
                Answer: $"The RAG server could not be reached: {ex.Message}",
                Citations: [],
                Confidence: "LOW",
                HasSufficientContext: false);
        }
    }
}
