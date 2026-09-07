using System.Text.Json.Serialization;
using Api.Configuration;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

var builder = WebApplication.CreateBuilder(args);

#region Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
    .AddOptions<DatabaseServiceOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseServiceOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "DatabaseService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddOptions<AiGatewayOptions>()
    .Bind(builder.Configuration.GetSection(AiGatewayOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "AiGateway:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();

builder.Services
    .AddHttpClient<IAccountDatabaseClient, AccountDatabaseClient>((services, client) =>
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<DatabaseServiceOptions>>().Value.BaseUrl))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(250);
        options.Retry.DisableForUnsafeHttpMethods();
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
    });
builder.Services
    .AddHttpClient(RemoteServiceHealthCheck.DatabaseServiceClientName, (services, client) =>
    {
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<DatabaseServiceOptions>>().Value.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(3);
    });

builder.Services
    .AddHttpClient(OpenRouterProfileSummaryService.HttpClientName, (services, client) =>
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["AiGateway:BaseUrl"] ?? "http://ai-mode:8080";
        ConfigureClient(client, baseUrl);
    })
    .AddStandardResilienceHandler(options =>
    {
        // Retry transient 5xx from the AI gateway, but pass 429 (rate limit)
        // through so the user sees the real upstream message instead of
        // having us mask it as a retry.
        options.Retry.ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => (int)r.StatusCode >= 500);
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
    });

builder.Services.AddScoped<IAiProfileSummaryService, OpenRouterProfileSummaryService>();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseServiceHealthCheck>("database-service", tags: ["ready"]);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5199"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});
#endregion

var app = builder.Build();

if (string.IsNullOrWhiteSpace(builder.Configuration["AiGateway:BaseUrl"]))
{
    Log.AiGatewayBaseUrlNotSet(app.Logger);
}

#region Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapUserEndpoints();
app.MapStudentEndpoints();
app.MapTeacherEndpoints();
app.MapProfileSummaryEndpoints();

app.UseApiExceptionHandling();
app.UseHttpsRedirection();

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
#endregion

app.Run();

static bool IsAbsoluteHttpUrl(string value)
{
    return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

static void ConfigureClient(HttpClient client, string baseUrl)
{
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
    client.Timeout = Timeout.InfiniteTimeSpan;
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message =
        "AiGateway:BaseUrl is not set. AI generation will fail until you set it " +
        "(see student-4/backend/README.md).")]
    public static partial void AiGatewayBaseUrlNotSet(ILogger logger);
}
