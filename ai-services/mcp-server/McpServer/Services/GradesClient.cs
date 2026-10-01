using System.Net.Http.Json;

namespace McpServer.Services
{
    public sealed class GradesClient(HttpClient httpClient) : IGradesClient
    {
        public async Task<GradeWeightingsData> GetGradesWeightingsAsync(
            double weight,
            int limit,
            CancellationToken cancellationToken)
        {
            using var response = await httpClient.GetAsync(
                $"internal/ai-context/grades-weightings?weight={weight}&limit={limit}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new GradeClientException(
                    $"Student 5 returned HTTP {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<GradeWeightingsData>(
                cancellationToken)
                ?? throw new GradeClientException(
                    "Grade service returned an empty response.");
        }
    }

    public sealed class GradeClientException(
        string message,
        Exception? innerException = null) : Exception(message, innerException);
}
