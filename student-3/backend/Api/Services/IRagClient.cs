using Api.DTOs;

namespace Api.Services;

public interface IRagClient
{
    Task<RagAnswerResponseDto> AnswerAsync(
        string question,
        CancellationToken cancellationToken);
}
