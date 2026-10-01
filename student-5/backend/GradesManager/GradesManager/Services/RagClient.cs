using System.Net.Http.Json;
using System.Text.Json;
using GradesManager.Configuration;
using GradesManager.DTOs;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace GradesManager.Services
{
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
                throw new RagIntegrationDisabledException();
            }

            try
            {
                using var response = await httpClient.PostAsJsonAsync(
                    "api/answers",
                    new RagAnswerRequestDto(question, "student-5"),
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
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new RagServiceException("The RAG request timed out.");
            }
            catch (RagServiceException)
            {
                throw;
            }
            catch (HttpRequestException exception)
            {
                throw new RagServiceException("The RAG server could not be reached.", exception);
            }
            catch (TimeoutRejectedException exception)
            {
                throw new RagServiceException("The RAG request timed out.", exception);
            }
            catch (BrokenCircuitException exception)
            {
                throw new RagServiceException(
                    "The RAG server is temporarily unavailable.",
                    exception);
            }
            catch (JsonException exception)
            {
                throw new RagServiceException(
                    "The RAG server returned an unreadable response.",
                    exception);
            }
        }
    }

    public sealed class RagIntegrationDisabledException()
        : Exception("RAG integration is disabled.");

    public sealed class RagServiceException(
        string message,
        Exception? innerException = null) : Exception(message, innerException);
}
