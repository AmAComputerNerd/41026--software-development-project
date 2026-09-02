using Api.DTOs;
using Api.Models;

namespace Api.Services;

public interface IAiDigestService
{
    Task<string> GenerateDigestAsync(Guid studentId, IReadOnlyList<Notification> unreadNotifications, CancellationToken cancellationToken = default);

    Task<string> AskAssistantAsync(
        Guid studentId,
        string prompt,
        IReadOnlyList<ChatMessageDto>? history,
        IReadOnlyList<Notification> unreadNotifications,
        CancellationToken cancellationToken = default);
}
