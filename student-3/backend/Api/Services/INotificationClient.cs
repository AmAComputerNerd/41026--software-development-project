using Api.DTOs;

namespace Api.Services;

public interface INotificationClient
{
    Task PushAsync(PushNotificationDto notification, CancellationToken cancellationToken);
}
