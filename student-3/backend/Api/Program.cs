using Api.Configuration;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
// Validation checks and binding for service options.
builder.Services
    .AddOptions<DatabaseServiceOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseServiceOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "DatabaseService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddOptions<SharedServiceOptions>()
    .Bind(builder.Configuration.GetSection(SharedServiceOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "SharedService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddOptions<AiGatewayOptions>()
    .Bind(builder.Configuration.GetSection(AiGatewayOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "AiGateway:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddOptions<NotificationServiceOptions>()
    .Bind(builder.Configuration.GetSection(NotificationServiceOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "NotificationService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddOptions<McpServerOptions>()
    .Bind(builder.Configuration.GetSection(McpServerOptions.SectionName))
    .Validate(
        options => !options.Enabled || IsAbsoluteHttpUrl(options.BaseUrl),
        "McpServer:BaseUrl must be an absolute HTTP or HTTPS URL when MCP is enabled.")
    .ValidateOnStart();
builder.Services
    .AddOptions<RagServerOptions>()
    .Bind(builder.Configuration.GetSection(RagServerOptions.SectionName))
    .Validate(
        options => !options.Enabled || IsAbsoluteHttpUrl(options.BaseUrl),
        "RagServer:BaseUrl must be an absolute HTTP or HTTPS URL when RAG is enabled.")
    .ValidateOnStart();
// Http clients and retry behaviour
builder.Services
    .AddHttpClient<IDatabaseClient, DatabaseClient>((services, client) =>
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
    .AddHttpClient<ISharedCanvasClient, SharedCanvasClient>((services, client) =>
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<SharedServiceOptions>>().Value.BaseUrl))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
    });
builder.Services
    .AddHttpClient<IAiTaskService, AiTaskService>((services, client) =>
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<AiGatewayOptions>>().Value.BaseUrl))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.DisableForUnsafeHttpMethods();
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(190);
    });
builder.Services
    .AddHttpClient<INotificationClient, NotificationClient>((services, client) =>
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<NotificationServiceOptions>>().Value.BaseUrl))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
    });
builder.Services.AddSingleton<IMcpDeadlineClient, McpDeadlineClient>();
builder.Services
    .AddHttpClient<IRagClient, RagClient>((services, client) =>
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<RagServerOptions>>().Value.BaseUrl))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 1;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.DisableForUnsafeHttpMethods();
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(95);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(200);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(100);
    });
// Health check services
builder.Services.AddHttpClient(
    RemoteServiceHealthCheck.DatabaseServiceClientName,
    (services, client) =>
    {
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<DatabaseServiceOptions>>().Value.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(3);
    });
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<DatabaseServiceHealthCheck>(
        "database-service",
        tags: ["ready"]);
// Remote services
builder.Services.AddScoped<CanvasSyncOrchestrator>();
builder.Services.AddScoped<DueSoonReminderService>();
builder.Services.AddHostedService<DueSoonReminderBackgroundService>();
// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:3003", "http://localhost:8080"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseApiExceptionHandling();
app.UseHttpsRedirection();

// API
app.MapCourseEndpoints();
app.MapTaskEndpoints();
app.MapCanvasSyncEndpoints();
app.MapMcpIntegrationEndpoints();
app.MapRagIntegrationEndpoints();
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