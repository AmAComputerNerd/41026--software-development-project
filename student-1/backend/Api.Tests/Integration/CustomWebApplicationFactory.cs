using Api.Data;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Api.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    public Mock<IAiDigestService> AiDigestServiceMock { get; } = new();
    public Mock<ISharedCanvasClient> SharedCanvasClientMock { get; } = new();
    public Mock<IMcpClient> McpClientMock { get; } = new();
    public Mock<IRagClient> RagClientMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("AiGateway:BaseUrl", "http://localhost");

        builder.ConfigureServices(services =>
        {
            // Remove background hosted services so they don't block tests
            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }

            // Remove existing EF Core and Npgsql descriptors
            var efDescriptors = services.Where(d =>
                d.ServiceType.Namespace?.StartsWith("Npgsql", StringComparison.Ordinal) == true ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType.Name.Contains("DbContext", StringComparison.Ordinal)
            ).ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            // Create in-memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Register fresh DbContext with SQLite provider using a new internal service provider
            services.AddEntityFrameworkSqlite();
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection)
                    .UseInternalServiceProvider(serviceProvider);
            });

            // Replace external services with mocks
            var aiServiceDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IAiDigestService));
            if (aiServiceDescriptor != null)
            {
                services.Remove(aiServiceDescriptor);
            }
            services.AddSingleton(AiDigestServiceMock.Object);

            var canvasClientDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(ISharedCanvasClient));
            if (canvasClientDescriptor != null)
            {
                services.Remove(canvasClientDescriptor);
            }
            services.AddSingleton(SharedCanvasClientMock.Object);

            var mcpClientDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IMcpClient));
            if (mcpClientDescriptor != null)
            {
                services.Remove(mcpClientDescriptor);
            }
            services.AddSingleton(McpClientMock.Object);

            var ragClientDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IRagClient));
            if (ragClientDescriptor != null)
            {
                services.Remove(ragClientDescriptor);
            }
            services.AddSingleton(RagClientMock.Object);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
