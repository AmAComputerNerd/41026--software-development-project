using GradesManager.DTOs;

namespace GradesManager.Services
{
    public interface IRagClient
    {
        Task<RagAnswerResponseDto> AnswerAsync(
            string question,
            CancellationToken cancellationToken);
    }
}
