using Api.Services;

namespace Api.Endpoints;

public static class CanvasSyncEndpoints
{
    public static IEndpointRouteBuilder MapCanvasSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/canvas-sync", Sync);
        return endpoints;
    }

    private static async Task<IResult> Sync(
        CanvasTaskSyncService syncService,
        CancellationToken cancellationToken)
    {
        var result = await syncService.SyncAsync(cancellationToken);
        return Results.Ok(result);
    }
}
