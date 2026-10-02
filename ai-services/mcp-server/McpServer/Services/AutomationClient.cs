using System.Net.Http.Json;

namespace McpServer.Services;

public sealed class AutomationClient(HttpClient httpClient) : IAutomationClient
{
    public async Task<AutomationHealthMetrics> GetAutomationHealthAsync(
        int days,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"internal/ai-context/automation-health?days={days}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AutomationClientException(
                $"Student 2 returned HTTP {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<AutomationHealthMetrics>(
            cancellationToken)
            ?? throw new AutomationClientException(
            "Automation service returned an empty health response.");
    }
}

public sealed class AutomationClientException(string message) : Exception(message);