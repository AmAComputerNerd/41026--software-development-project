using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;

namespace Api.Services;

public sealed class NotificationClient(HttpClient httpClient) : INotificationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PushAsync(NotificationPushDto dto, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/notifications/push",
            dto,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SharedServiceException(
                $"The notification service returned HTTP {(int)response.StatusCode}.");
        }
    }
}
