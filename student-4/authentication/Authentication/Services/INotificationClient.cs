using Student4.Contracts;

namespace Authentication.Services;

public interface INotificationClient
{
    Task PushAsync(PushNotificationDto notification, CancellationToken cancellationToken = default);
}
