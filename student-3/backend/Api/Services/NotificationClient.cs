using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;

namespace Api.Services;

public sealed class NotificationClient(HttpClient httpClient) : INotificationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PushAsync(PushNotificationDto notification, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "notifications/push",
            notification,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new NotificationServiceException(
                $"The notification service returned HTTP {(int)response.StatusCode}.");
        }
    }
}
