using System.Net;
using System.Net.Http.Json;

namespace McpServer.Services;

public interface IAccountClient
{
    Task<AccountReadinessData?> CheckReadinessAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class AccountClient(HttpClient httpClient) : IAccountClient
{
    public async Task<AccountReadinessData?> CheckReadinessAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"internal/ai-context/accounts/{userId}/readiness",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new AccountClientException($"Account service returned HTTP {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<AccountReadinessData>(cancellationToken)
            ?? throw new AccountClientException("Account service returned an empty response.");
    }
}

public sealed record AccountReadinessData(
    Guid UserId,
    string OverallStatus,
    DateTime CheckedAtUtc,
    IReadOnlyList<AccountReadinessCheckData> Checks);

public sealed record AccountReadinessCheckData(
    string Code,
    string Label,
    string Status,
    string Message,
    string? Action);

public sealed class AccountClientException(string message) : Exception(message);
