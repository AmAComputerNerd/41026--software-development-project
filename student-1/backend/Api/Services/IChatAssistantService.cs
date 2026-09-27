using Api.DTOs;

namespace Api.Services;

public interface IChatAssistantService
{
    Task<ChatMessageDetailDto> GenerateInitialDigestAsync(
        Guid studentId,
        Guid chatSessionId,
        CancellationToken cancellationToken = default);

    Task<ChatMessageDetailDto> SendMessageAsync(
        Guid studentId,
        Guid chatSessionId,
        string prompt,
        bool includeRag = true,
        CancellationToken cancellationToken = default);
}
