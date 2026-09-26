using System.Net.Http.Json;

namespace McpServer.Services;

public sealed class DeadlineClient(HttpClient httpClient) : IDeadlineClient
{
    public async Task<UpcomingDeadlinesData> GetUpcomingDeadlinesAsync(
        int days,
        int limit,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"internal/ai-context/upcoming-deadlines?days={days}&limit={limit}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new DeadlineClientException(
                $"Student 3 returned HTTP {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<UpcomingDeadlinesData>(
            cancellationToken)
            ?? throw new DeadlineClientException(
                "Deadline service returned an empty response.");
    }
}

public sealed class DeadlineClientException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
