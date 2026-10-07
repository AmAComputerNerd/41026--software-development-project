using System.Net.Http.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class RagClient(
    HttpClient httpClient,
    IOptions<RagServerOptions> options) : IRagClient
{
    private readonly RagServerOptions _options = options.Value;

    public async Task<RagAnswerResponseDto> AnswerAsync(
        string question,
        CancellationToken cancellationToken)
    {
        if (_options.Enabled is not true)
        {
            throw new InvalidOperationException("RAG integration is disabled.");
        }

        using var response = await httpClient.PostAsJsonAsync(
            "api/answers",
            new RagAnswerRequestDto(question, "student-2"),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RagAnswerResponseDto>(cancellationToken)
            ?? throw new InvalidOperationException("The RAG server returned an empty response.");
    }
}