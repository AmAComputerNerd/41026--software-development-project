using Api.Data;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options
        .UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSeeding((db, _) => DbSeeder.SeedData((AppDbContext)db))
        .UseAsyncSeeding((db, _, _) =>
        {
            DbSeeder.SeedData((AppDbContext)db);
            return Task.CompletedTask;
        });
});
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ISharedCanvasClient, SharedCanvasClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SharedService:BaseUrl"]!);
});
var aiGatewayBaseUrl = builder.Configuration["AiGateway:BaseUrl"] ?? "http://ai-mode:8080";
builder.Services.AddHttpClient<IAiDigestService, OpenRouterDigestService>(client =>
{
    client.BaseAddress = new Uri(aiGatewayBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();
builder.Services.AddScoped<CanvasNotificationSyncService>();
builder.Services.AddHostedService<CanvasSyncBackgroundService>();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"]);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5199"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(builder.Configuration["AiGateway:BaseUrl"]))
{
    Log.AiGatewayBaseUrlNotSet(app.Logger);
}

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
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
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

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message =
        "AiGateway:BaseUrl is not set. AI digest generation will fail until you set it " +
        "(see student-1/backend/README.md).")]
    public static partial void AiGatewayBaseUrlNotSet(ILogger logger);
}
