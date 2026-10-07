using Api.Configuration;
using Api.Data;
using Api.DTOs;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Api.Services.Executors;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.UseAllOfForInheritance();
    options.UseOneOfForPolymorphism();
    options.SelectSubTypesUsing(AutomationDtoRegistry.GetDerivedTypes);
    options.SelectDiscriminatorNameUsing(_ => "$type");
    options.SelectDiscriminatorValueUsing(AutomationDtoRegistry.GetDiscriminator);
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    var resolver = new DefaultJsonTypeInfoResolver();
    resolver.Modifiers.Add(AutomationDtoRegistry.ConfigureJsonTypeInfo);
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, resolver);
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<ISharedCanvasClient, SharedCanvasClient>(client =>
{
    var baseUrl = builder.Configuration["SharedService:BaseUrl"]
        ?? throw new InvalidOperationException("SharedService:BaseUrl is not configured.");
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services
    .AddOptions<AutomationExecutionOptions>()
    .Bind(builder.Configuration.GetSection(AutomationExecutionOptions.SectionName))
    .Validate(options => options.IntervalSeconds > 0, "IntervalSeconds must be greater than zero.")
    .ValidateOnStart();
builder.Services
    .AddOptions<AiGatewayOptions>()
    .Bind(builder.Configuration.GetSection(AiGatewayOptions.SectionName))
    .Validate(options => options.Enabled.HasValue, "AiGateway:Enabled must be configured.")
    .Validate(
        options => options.Enabled is false || IsAbsoluteHttpUrl(options.BaseUrl),
        "AiGateway:BaseUrl must be an absolute HTTP or HTTPS URL when AI Mode is enabled.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IAiQuizAnswerService, AiQuizAnswerService>((services, client) =>
{
    var baseUrl = services.GetRequiredService<IOptions<AiGatewayOptions>>().Value.BaseUrl;
    client.BaseAddress = string.IsNullOrWhiteSpace(baseUrl)
        ? null
        : new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services
    .AddOptions<NotificationServiceOptions>()
    .Bind(builder.Configuration.GetSection(NotificationServiceOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "NotificationService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services.AddHttpClient<INotificationClient, NotificationClient>((services, client) =>
{
    var baseUrl = services.GetRequiredService<IOptions<NotificationServiceOptions>>().Value.BaseUrl;
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services
    .AddOptions<McpServerOptions>()
    .Bind(builder.Configuration.GetSection(McpServerOptions.SectionName))
    .Validate(options => options.Enabled.HasValue, "McpServer:Enabled must be configured.")
    .Validate(
        options => options.Enabled is false || IsAbsoluteHttpUrl(options.BaseUrl),
        "McpServer:BaseUrl must be an absolute HTTP or HTTPS URL when MCP is enabled.")
    .ValidateOnStart();
builder.Services
    .AddOptions<RagServerOptions>()
    .Bind(builder.Configuration.GetSection(RagServerOptions.SectionName))
    .Validate(options => options.Enabled.HasValue, "RagServer:Enabled must be configured.")
    .Validate(
        options => options.Enabled is false || IsAbsoluteHttpUrl(options.BaseUrl),
        "RagServer:BaseUrl must be an absolute HTTP or HTTPS URL when RAG is enabled.")
    .ValidateOnStart();
builder.Services.AddSingleton<IMcpAutomationClient, McpAutomationClient>();
builder.Services.AddHttpClient<IRagClient, RagClient>((services, client) =>
{
    var baseUrl = services.GetRequiredService<IOptions<RagServerOptions>>().Value.BaseUrl;
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(190);
});
var executorTypes = typeof(IAutomationExecutor).Assembly
    .GetTypes()
    .Where(type => !type.IsAbstract && typeof(IAutomationExecutor).IsAssignableFrom(type));
foreach (var executorType in executorTypes)
{
    builder.Services.AddScoped(typeof(IAutomationExecutor), executorType);
}
builder.Services.AddScoped<AutomationExecutorRegistry>();
builder.Services.AddHostedService<AutomationExecutionBackgroundService>();
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3002", "http://localhost:8080"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapAutomationEndpoints();
app.MapAutomationRunEndpoints();
app.MapCanvasOptionEndpoints();
app.MapIntegrationEndpoints();
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

await app.InitialiseDatabaseAsync();

app.Run();

static bool IsAbsoluteHttpUrl(string value) =>
    Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);