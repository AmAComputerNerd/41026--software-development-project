using Student4.Contracts;

namespace Api.Services;

public interface INotificationClient
{
    Task PushAsync(PushNotificationDto notification, CancellationToken cancellationToken = default);
}
