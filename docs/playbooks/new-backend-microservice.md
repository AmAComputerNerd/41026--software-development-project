# Playbook: Adding a New Backend Microservice

This step-by-step playbook guides developers and AI agents through bootstrapping and integrating a new ASP.NET Core backend microservice (e.g. for Student 2: Automations or Student 4: Account).

> [!NOTE]
> This guide describes the existing single-service backend pattern. If the
> slice requires a separately deployed persistence service, follow
> [Splitting a Backend into API and Database Services](split-database-service.md)
> after establishing the initial API behavior and data model.

---

## 1. Directory Structure

Create your service under `student-N/backend/`:

```
student-N/
├── backend/
│   ├── Api/
│   │   ├── Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Data/
│   │   │   └── AppDbContext.cs
│   │   ├── Endpoints/
│   │   │   └── FeatureEndpoints.cs
│   │   ├── Models/
│   │   └── Migrations/
│   ├── StudentN.sln
│   ├── Dockerfile
│   └── README.md
```

---

## 2. Project File Configuration (`Api.csproj`)

Target .NET 10 and include the standard EF Core SQLite dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

---

## 3. Minimal API Program Configuration (`Program.cs`)

Follow the minimal API endpoint pattern:

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=app.db"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors();

// Auto-migrate on startup in Development
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Map endpoints
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
// app.MapGroup("/api/...").MapEndpoints();

app.Run();
```

---

## 4. Dockerfile

Create `student-N/backend/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Api/Api.csproj", "Api/"]
RUN dotnet restore "Api/Api.csproj"

COPY . .
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
```

---

## 5. Docker Compose & Reverse Proxy Integration

1. Add your backend service to `docker-compose.yml`:
   ```yaml
   student-N-backend:
     build:
       context: ./student-N/backend
       dockerfile: Dockerfile
     ports:
       - "510N:8080"
     environment:
       - ASPNETCORE_ENVIRONMENT=Development
       - SharedService__BaseUrl=http://shared-backend:8080
       - AiGateway__BaseUrl=http://ai-mode:8080
     volumes:
       - studentN-data:/app/data
   ```
2. Uncomment/add the API proxy route in `shared/frontend/nginx.conf`:
   ```nginx
   location /api/feature/ {
       proxy_pass http://student-N-backend:8080/;
       proxy_set_header Host $host;
       proxy_set_header X-Real-IP $remote_addr;
   }
   ```
3. Add `student-N-backend` to `shared-shell`'s `depends_on` list.
