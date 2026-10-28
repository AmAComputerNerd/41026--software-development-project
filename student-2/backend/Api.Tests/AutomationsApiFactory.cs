using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Api.Tests;

public sealed class AutomationsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"student-2-tests-{Guid.NewGuid():N}.db");

    public Mock<ISharedCanvasClient> Canvas { get; } = new();
    public Mock<IAiQuizAnswerService> Ai { get; } = new();
    public Mock<IMultiAgentClient> MultiAgent { get; } = new();
    public Mock<INotificationClient> Notifications { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        builder.UseSetting("SharedService:BaseUrl", "http://localhost:5010");
        builder.UseSetting("NotificationService:BaseUrl", "http://localhost:5101");
        builder.UseSetting("AiGateway:Enabled", "false");
        builder.UseSetting("McpServer:Enabled", "false");
        builder.UseSetting("RagServer:Enabled", "false");
        builder.UseSetting("MultiAgentServer:Enabled", "true");
        builder.UseSetting("MultiAgentServer:BaseUrl", "http://localhost:5004");

        builder.ConfigureServices(services =>
        {
            foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
            {
                services.Remove(hosted);
            }

            services.AddSingleton(Canvas.Object);
            services.AddSingleton(Ai.Object);
            services.AddSingleton(MultiAgent.Object);
            services.AddSingleton(Notifications.Object);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
