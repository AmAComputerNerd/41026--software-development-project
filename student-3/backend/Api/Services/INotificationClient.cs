using Api.DTOs;

namespace Api.Services;

public interface INotificationClient
{
    Task PushAsync(NotificationPushDto dto, CancellationToken cancellationToken);
}
