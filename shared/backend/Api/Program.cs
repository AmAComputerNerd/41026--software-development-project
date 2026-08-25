using Api.Configuration;
using Api.Data;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCanvasEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.UseApiExceptionHandling();
await app.InitialiseDatabaseAsync();

app.Run();
