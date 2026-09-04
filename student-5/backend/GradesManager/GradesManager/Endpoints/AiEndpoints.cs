using GradesManager.Data;
using GradesManager.DTOs;
using GradesManager.Models;
using GradesManager.Services;

namespace GradesManager.Endpoints
{
    public static class AiEndpoints
    {
        public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/ai");
            group.MapPost("/generate-recommendation", GenerateRecommendation);
            return endpoints;
        }

        private static async Task<IResult> GenerateRecommendation(
            GenerateRecommendationRequestDto requestDto,
            AppDbContext db,
            IAiTaskService aiTaskService,
            CancellationToken cancallationToken)
        {
            var recommendation = await aiTaskService.GenerateRecommendationAsync(
                new AiRecommendationContext(
                    requestDto.Assignments),
                cancallationToken);
            return Results.Ok(new GeneratedRecommendationDto(recommendation));
        }
    }
}
