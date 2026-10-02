using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RagServer.Configuration;
using RagServer.Endpoints;
using RagServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<RagOptions>()
    .Bind(builder.Configuration.GetSection(RagOptions.SectionName))
    .Validate(
        options => Directory.Exists(options.CorpusPath),
        "Rag:CorpusPath must identify an existing directory.")
    .Validate(
        options => options.MaximumRetrievedChunks is >= 1 and <= 8,
        "Rag:MaximumRetrievedChunks must be between 1 and 8.")
    .Validate(
        options => options.MinimumRelevanceScore is > 0 and <= 1,
        "Rag:MinimumRelevanceScore must be greater than 0 and no more than 1.")
    .ValidateOnStart();
builder.Services
    .AddOptions<AiGatewayOptions>()
    .Bind(builder.Configuration.GetSection(AiGatewayOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "AiGateway:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services.AddSingleton<ProjectCorpus>();
builder.Services
    .AddHttpClient<GroundedAnswerService>((services, client) =>
    {
        var options = services.GetRequiredService<IOptions<AiGatewayOptions>>().Value;
        client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(180);
    });
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live", "ready"]);

var app = builder.Build();

_ = app.Services.GetRequiredService<ProjectCorpus>();

app.MapGet("/", (ProjectCorpus corpus) => Results.Ok(new
{
    service = "rag-server",
    status = "ready",
    sources = corpus.SourceCount,
    chunks = corpus.ChunkCount
}));
app.MapRagEndpoints();
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
