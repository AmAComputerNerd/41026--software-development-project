using Api.Configuration;
using Api.Data;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSeeding((db, _) => DbSeeder.SeedData((AppDbContext)db))
        .UseAsyncSeeding((db, _, _) =>
        {
            DbSeeder.SeedData((AppDbContext)db);
            return Task.CompletedTask;
        });
});
builder.Services.AddHttpClient();
var sharedServiceBaseUrl = builder.Configuration["SharedService:BaseUrl"] ?? "http://shared-backend:8080";
builder.Services.AddHttpClient<ISharedCanvasClient, SharedCanvasClient>(client =>
{
    client.BaseAddress = new Uri(sharedServiceBaseUrl);
});
var aiGatewayBaseUrl = builder.Configuration["AiGateway:BaseUrl"];
if (!Uri.TryCreate(aiGatewayBaseUrl, UriKind.Absolute, out var aiGatewayUri) ||
    (aiGatewayUri.Scheme != Uri.UriSchemeHttp &&
     aiGatewayUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "AiGateway:BaseUrl must be configured as an absolute HTTP or HTTPS URL.");
}

builder.Services.AddHttpClient<IAiDigestService, OpenRouterDigestService>(client =>
{
    client.BaseAddress = aiGatewayUri;
    client.Timeout = TimeSpan.FromSeconds(90);
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(45);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(100);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddScoped<CanvasNotificationSyncService>();
builder.Services.AddSingleton<INotificationStreamBroker, NotificationStreamBroker>();
builder.Services.AddHostedService<CanvasSyncBackgroundService>();

builder.Services
    .AddOptions<McpServerOptions>()
    .Bind(builder.Configuration.GetSection(McpServerOptions.SectionName));
builder.Services.AddSingleton<IMcpClient, McpClient>();

builder.Services
    .AddOptions<RagServerOptions>()
    .Bind(builder.Configuration.GetSection(RagServerOptions.SectionName));
builder.Services.AddHttpClient<IRagClient, RagClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<RagServerOptions>>().Value;
    if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
    {
        client.BaseAddress = uri;
    }
    client.Timeout = TimeSpan.FromSeconds(90);
});

builder.Services.AddScoped<IChatAssistantService, ChatAssistantService>();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live", "ready"]);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5199"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Endpoints
app.MapNotificationEndpoints();
app.MapPreferenceEndpoints();
app.MapAiDigestEndpoints();
app.MapCanvasSyncEndpoints();
app.MapChatEndpoints();
app.MapMcpRagIntegrationEndpoints();
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });
app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
    });

// Infrastructure
app.UseApiExceptionHandling();
app.UseHttpsRedirection();
await app.InitialiseDatabaseAsync();

app.Run();

public partial class Program { }
