namespace Api.Services;

public sealed class DueSoonReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DueSoonReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = configuration.GetValue("DueSoonReminder:IntervalMinutes", 15);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        do
        {
            await RunAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<DueSoonReminderService>();
            var remindersSent = await reminderService.SendDueSoonRemindersAsync(cancellationToken);

            DueSoonReminderLog.DueSoonReminderRunCompleted(logger, remindersSent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            DueSoonReminderLog.DueSoonReminderRunFailed(logger, ex);
        }
    }
}

internal static partial class DueSoonReminderLog
{
    [LoggerMessage(Level = LogLevel.Information, Message =
        "Due-soon reminder run completed: {RemindersSent} reminder(s) sent.")]
    public static partial void DueSoonReminderRunCompleted(ILogger logger, int remindersSent);

    [LoggerMessage(Level = LogLevel.Error, Message = "Due-soon reminder run failed.")]
    public static partial void DueSoonReminderRunFailed(ILogger logger, Exception ex);
}
