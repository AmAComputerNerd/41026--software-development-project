using GradesManager.Configuration;
using GradesManager.Data;
using GradesManager.Endpoints;
using GradesManager.Extensions;
using GradesManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services
    .AddOptions<SharedServiceOptions>()
    .Bind(builder.Configuration.GetSection(SharedServiceOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "SharedService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
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
builder.Services.AddHttpClient<ISharedCanvasClient, SharedCanvasClient>(
    (services, client) => ConfigureClient(
        client,
        services.GetRequiredService<IOptions<SharedServiceOptions>>().Value.BaseUrl));
builder.Services.AddScoped<CanvasTaskSyncService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

//map endpoints
app.MapCourseEndpoints();
app.MapStudentEndpoints();
app.MapAssignmentEndpoints();
app.MapCanvasSyncEndpoints();

app.UseHttpsRedirection();
await app.InitialiseDatabaseAsync();

app.Run();

static bool IsAbsoluteHttpUrl(string value)
{
    return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

static void ConfigureClient(HttpClient client, string baseUrl)
{
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
}
