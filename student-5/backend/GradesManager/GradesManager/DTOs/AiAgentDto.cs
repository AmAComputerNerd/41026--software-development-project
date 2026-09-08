using GradesManager.Contracts;

namespace GradesManager.DTOs
{
    public record GenerateRecommendationRequestDto
    (
        List<AssignmentRecord> Assignments
    );

    public record GeneratedRecommendationDto(
        string Recommendation
    );
}
