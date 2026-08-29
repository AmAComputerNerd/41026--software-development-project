using Api.Configuration;
using Api.Data;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

EnvironmentFile.LoadFromCurrentDirectory();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["Canvas:ApiToken"] =
    Environment.GetEnvironmentVariable("CANVAS_API_TOKEN") ??
    builder.Configuration["Canvas:ApiToken"];
builder.Configuration["Canvas:BaseUrl"] =
    Environment.GetEnvironmentVariable("CANVAS_BASE_URL") ??
    builder.Configuration["Canvas:BaseUrl"];

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.Configure<CanvasOptions>(
    builder.Configuration.GetSection(CanvasOptions.SectionName));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<ICanvasApiClient, CanvasApiClient>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CanvasFacade>();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck(
        "canvas-configuration",
        () => IsCanvasConfigured(builder.Configuration)
            ? HealthCheckResult.Healthy("Canvas is configured.")
            : HealthCheckResult.Unhealthy("Canvas configuration is missing or invalid."),
        tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCanvasEndpoints();
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
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });

app.UseApiExceptionHandling();
await app.InitialiseDatabaseAsync();

app.Run();

static bool IsCanvasConfigured(IConfiguration configuration)
{
    return Uri.TryCreate(
            configuration["Canvas:BaseUrl"],
            UriKind.Absolute,
            out var baseUri) &&
        (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrWhiteSpace(configuration["Canvas:ApiToken"]);
}
