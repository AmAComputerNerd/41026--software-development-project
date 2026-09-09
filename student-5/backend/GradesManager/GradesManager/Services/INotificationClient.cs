using GradesManager.DTOs;

namespace GradesManager.Services
{
    public interface INotificationClient
    {
        Task PushAsync(PushNotificationDto notification, CancellationToken cancellationToken = default);
    }
}
