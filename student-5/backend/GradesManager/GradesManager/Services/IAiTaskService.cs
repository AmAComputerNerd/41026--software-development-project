using GradesManager.Models;

namespace GradesManager.Services
{
    public interface IAiTaskService
    {
        Task<string> GenerateRecommendationAsync(
            AiRecommendationContext context,
            CancellationToken cancellationToken);
    }

    public sealed record AiRecommendationContext(
        List<Assignment> Assignments);
}
