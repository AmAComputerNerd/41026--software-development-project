namespace Api.Services;

public sealed class CanvasSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CanvasSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = configuration.GetValue("CanvasSync:IntervalMinutes", 15);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        do
        {
            await RunSyncAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<CanvasNotificationSyncService>();
            var result = await syncService.SyncAsync(cancellationToken);

            Log.CanvasSyncCompleted(logger, result.NotificationsCreated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.CanvasSyncFailed(logger, ex);
        }
    }
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message =
        "Canvas sync completed: {NotificationsCreated} notification(s) created.")]
    public static partial void CanvasSyncCompleted(ILogger logger, int notificationsCreated);

    [LoggerMessage(Level = LogLevel.Error, Message = "Canvas sync run failed.")]
    public static partial void CanvasSyncFailed(ILogger logger, Exception ex);
}
