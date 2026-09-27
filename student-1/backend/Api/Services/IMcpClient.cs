using Api.DTOs;

namespace Api.Services;

public interface IMcpClient
{
    Task<McpBroadcastResponseDto> BroadcastAlertAsync(
        Guid studentId,
        string title,
        string message,
        string urgency = "Medium",
        CancellationToken cancellationToken = default);
}
