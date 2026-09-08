using GradesManager.Contracts;
using GradesManager.DTOs;
using GradesManager.Services;
using Microsoft.AspNetCore.Mvc;

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
            IDatabaseClient databaseClient,
            IAiTaskService aiTaskService,
            CancellationToken cancellationToken)
        {
            // Get assignments from database service
            var assignments = await databaseClient.GetAssignmentsAsync(cancellationToken);
            
            var recommendation = await aiTaskService.GenerateRecommendationAsync(
                new AiRecommendationContext((assignments ?? new List<AssignmentRecord>()).ToList()),
                cancellationToken);
            
            return Results.Ok(new GeneratedRecommendationDto(recommendation));
        }
    }
}
