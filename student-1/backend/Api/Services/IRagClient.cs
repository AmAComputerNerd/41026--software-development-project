using Api.DTOs;

namespace Api.Services;

public interface IRagClient
{
    Task<RagQueryResponseDto> QueryAsync(
        string question,
        string scope = "student-1",
        CancellationToken cancellationToken = default);
}
