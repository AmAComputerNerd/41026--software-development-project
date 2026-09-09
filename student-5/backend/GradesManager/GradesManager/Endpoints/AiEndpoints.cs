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
            INotificationClient notificationClient,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            // Get assignments from database service
            var assignments = await databaseClient.GetAssignmentsAsync(cancellationToken);

            var recommendation = await aiTaskService.GenerateRecommendationAsync(
                new AiRecommendationContext((assignments ?? new List<AssignmentRecord>()).ToList()),
                cancellationToken);

            try
            {
                var studentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: studentId,
                    Type: "Grade",
                    SourceMicroservice: "grades",
                    Message: "AI study recommendation generated for your grades."), cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("GradesManager.Endpoints.AiEndpoints");
                AiEndpointsLog.PushNotificationFailed(logger, ex);
            }

            return Results.Ok(new GeneratedRecommendationDto(recommendation));
        }
    }

    internal static partial class AiEndpointsLog
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push notification for AI grade recommendation.")]
        public static partial void PushNotificationFailed(ILogger logger, Exception ex);
    }
}
