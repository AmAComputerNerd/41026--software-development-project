using GradesManager.DTOs;

namespace GradesManager.Services
{
    public interface IMcpGradesClient
    {
        Task<McpGradesToolResultDto> GetWeightingsAsync(
            double weight,
            int limit,
            CancellationToken cancellationToken);
    }
}
