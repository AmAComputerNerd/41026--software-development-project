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
        if (!_options.Enabled)
        {
            throw new RagIntegrationDisabledException();
        }

        using var response = await httpClient.PostAsJsonAsync(
            "api/answers",
            new RagAnswerRequestDto(question, "student-3"),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new RagServiceException(
                $"The RAG server returned HTTP {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<RagAnswerResponseDto>(
            cancellationToken)
            ?? throw new RagServiceException("The RAG server returned an empty response.");
    }
}

public sealed class RagIntegrationDisabledException()
    : Exception("RAG integration is disabled.");

public sealed class RagServiceException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
