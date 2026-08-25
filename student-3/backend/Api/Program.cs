using Api.Configuration;
using Api.Data;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.Configure<SharedServiceOptions>(
    builder.Configuration.GetSection(SharedServiceOptions.SectionName));
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
builder.Services.AddHttpClient<ISharedCanvasClient, SharedCanvasClient>();
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Notifications:BaseUrl"]!);
});
builder.Services.AddScoped<CanvasTaskSyncService>();
builder.Services.AddScoped<TaskHierarchyService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:3003", "http://localhost:8080"])
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

// API
app.MapCourseEndpoints();
app.MapTaskEndpoints();
app.MapCanvasSyncEndpoints();

// Infrastructure
app.UseApiExceptionHandling();
app.UseHttpsRedirection();
await app.InitialiseDatabaseAsync();

app.Run();