using System.Text.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class RagClient(HttpClient httpClient, IOptions<RagServerOptions> options)
{
    public async Task<RagAnswerResponseDto> AnswerAsync(
        string question,
        CancellationToken cancellationToken)
    {
        if (options.Value.Enabled is not true)
        {
            throw new RagIntegrationDisabledException();
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/answers",
                new RagAnswerRequestDto(question, "student-4"),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new RagServiceException($"The RAG server returned HTTP {(int)response.StatusCode}.");
            }

            var answer = await response.Content.ReadFromJsonAsync<RagAnswerResponseDto>(cancellationToken);
            if (answer is null || string.IsNullOrWhiteSpace(answer.Answer) ||
                answer.Citations is null || answer.Retrieval is null ||
                (answer.Status == "success" &&
                    (answer.Confidence is not ("low" or "medium" or "high") ||
                     answer.Citations.Count == 0 ||
                     answer.Citations.Any(citation => citation is null ||
                         string.IsNullOrWhiteSpace(citation.SourceId) ||
                         !citation.SourceId.StartsWith("student-4/", StringComparison.Ordinal)))) ||
                (answer.Status == "insufficient_context" &&
                    (answer.Confidence != "insufficient" || answer.Citations.Count != 0)) ||
                answer.Status is not ("success" or "insufficient_context"))
            {
                throw new RagServiceException("The RAG server returned an invalid grounded answer.");
            }

            return answer;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RagServiceException("The RAG request timed out.", exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException)
        {
            throw new RagServiceException("The RAG server could not complete the request.", exception);
        }
    }
}

public sealed class RagIntegrationDisabledException() : Exception("RAG integration is disabled.");

public sealed class RagServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
