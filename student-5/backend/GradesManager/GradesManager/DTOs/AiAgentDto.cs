using GradesManager.Models;

namespace GradesManager.DTOs
{
    public record GenerateRecommendationRequestDto
    (
        List<Assignment> Assignments
    );

    public record GeneratedRecommendationDto(
        string Recommendation
    );
}
