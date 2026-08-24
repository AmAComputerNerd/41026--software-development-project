using Api.Models;

namespace Api.Services;

public interface IAiDigestService
{
    Task<string> GenerateDigestAsync(Guid studentId, IReadOnlyList<Notification> unreadNotifications, CancellationToken cancellationToken = default);
}
