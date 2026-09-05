# Playbook: Adding a New Backend Slice

Use this playbook when adding a new backend capability to the repository.

The default architecture is a three-project slice:

- A public ASP.NET Core API for routes and orchestration.
- An internal ASP.NET Core database service that exclusively owns EF Core.
- A shared class library containing internal HTTP contracts.

Do not place EF Core, migrations, connection strings, or a database volume in
the public API. The Student 3 slice is the reference implementation.

> [!NOTE]
> If converting an existing single-process backend, use
> [Splitting a Backend into API and Database Services](split-database-service.md).
> This playbook describes the greenfield default.

---

## 1. Choose Names and Ports

Replace these placeholders throughout the examples:

| Placeholder | Example |
| --- | --- |
| `student-N` | `student-4` |
| `feature` | `account` |
| `510N` | Public API host port, such as `5104` |
| `520N` | Standalone database-service port, such as `5204` |
| `StudentN` | Solution name |

The database service is internal-only in Docker Compose. `520N` is for
standalone `dotnet run` development and is not published by Compose.

---

## 2. Create the Slice Structure

Create:

```text
student-N/
├── .dockerignore
├── backend/
│   ├── Api/
│   │   ├── Configuration/
│   │   ├── DTOs/
│   │   ├── Endpoints/
│   │   ├── Extensions/
│   │   ├── Services/
│   │   ├── Api.csproj
│   │   ├── Dockerfile
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── StudentN.sln
├── contracts/
│   └── Contracts/
│       ├── Contracts.csproj
│       └── PersistenceContracts.cs
└── database/
    └── Database/
        ├── Data/
        ├── Endpoints/
        ├── Extensions/
        ├── Migrations/
        ├── Models/
        ├── Properties/
        ├── Services/
        ├── Database.csproj
        ├── Dockerfile
        ├── Program.cs
        └── appsettings.json
```

Scaffold the projects:

```bash
dotnet new web -n Api -o student-N/backend/Api
dotnet new classlib -n Contracts -o student-N/contracts/Contracts
dotnet new web -n Database -o student-N/database/Database
dotnet new sln --format sln -n StudentN -o student-N/backend

dotnet sln student-N/backend/StudentN.sln add \
  student-N/backend/Api/Api.csproj \
  student-N/contracts/Contracts/Contracts.csproj \
  student-N/database/Database/Database.csproj

dotnet add student-N/backend/Api/Api.csproj reference \
  student-N/contracts/Contracts/Contracts.csproj
dotnet add student-N/database/Database/Database.csproj reference \
  student-N/contracts/Contracts/Contracts.csproj
```

Add `Properties/launchSettings.json` to both web projects so standalone
development uses predictable ports.

API:

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:510N",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Database:

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:520N",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

## 3. Define Service Ownership

The public API owns:

- Public `/api/*` routes.
- Public request and response DTOs.
- Authentication, authorization, and CORS.
- Canvas access through `shared-backend`.
- AI access through `ai-mode`.
- Calls to notification and other application services.
- Background workflow orchestration.
- Translation of internal failures into public responses.

The database service owns:

- EF Core packages and `DbContext`.
- Entity models and relationships.
- Migrations and seeding.
- SQLite connection strings and files.
- The Docker database volume.
- Transactional domain operations.
- Internal `/internal/*` persistence endpoints.

The contracts project owns only transport records shared between those two
services.

The database service must not call Canvas, AI, notification, or another
bounded context. External data flows through the public API:

```text
Client -> API -> external service -> API -> database service
```

---

## 4. Configure the Contracts Project

`Contracts.csproj` remains dependency-light:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

Add records for internal reads and commands:

```csharp
namespace StudentN.Contracts;

public sealed record FeatureRecord(
    Guid Id,
    string Name,
    DateTime CreatedAt);

public sealed record CreateFeatureCommand(string Name);

public sealed record UpdateFeatureCommand(string? Name);
```

Contracts may include:

- Read records.
- Create and update commands.
- Query/filter contracts.
- Bulk-operation commands.
- Synchronization snapshots.
- Result summaries.

Contracts must not include:

- EF Core references.
- Entity classes or navigation properties.
- `DbContext`.
- Database-provider types.
- Service implementations.

Keep public API DTOs in `backend/Api/DTOs`. This prevents persistence details
from becoming part of the browser-facing contract.

---

## 5. Configure the Database Project

Use `Microsoft.NET.Sdk.Web`, target .NET 10, enable repository analyzers, and
add the current repository versions of:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.5" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.11" />
  <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.11" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="10.2.3" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\contracts\Contracts\Contracts.csproj" />
</ItemGroup>
```

Only this project references EF Core.

Configure its connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/app.db"
  }
}
```

Create `AppDbContext`, entities, relationships, converters, and idempotent
seeding under the database project.

---

## 6. Configure the Database Application

Register EF Core and database-backed readiness:

```csharp
using Database.Data;
using Database.Endpoints;
using Database.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapFeaturePersistenceEndpoints();
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
```

`InitialiseDatabaseAsync` should apply migrations and run idempotent seeding.

Do not configure browser CORS. Do not map public feature routes.

---

## 7. Design Internal Persistence Endpoints

Map persistence routes under `/internal`:

```text
GET    /internal/features
GET    /internal/features/{id}
POST   /internal/features
PUT    /internal/features/{id}
DELETE /internal/features/{id}
```

Prefer coarse, domain-oriented commands for multi-record work:

```text
POST /internal/features/{id}/complete
POST /internal/features/import-snapshot
POST /internal/features/bulk
```

One logical transaction should be one database-service request. Do not make
the API perform several dependent HTTP calls to reproduce one EF transaction.

Return explicit:

- `200`/`201` for success.
- `400` for validation.
- `404` for missing records.
- `409` for domain conflicts.
- `500` only for unexpected failures.

---

## 8. Configure the Public API Project

The API project should reference:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.5" />
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="10.2.3" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\contracts\Contracts\Contracts.csproj" />
</ItemGroup>
```

It must not reference:

- `Microsoft.EntityFrameworkCore.*`
- The database project.
- Database entities.

Configure the database URL:

```json
{
  "DatabaseService": {
    "BaseUrl": "http://localhost:520N"
  }
}
```

Add validated options and a typed client:

```csharp
public interface IDatabaseClient
{
    Task<IReadOnlyList<FeatureRecord>> GetFeaturesAsync(
        CancellationToken cancellationToken);

    Task<FeatureRecord?> GetFeatureAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<FeatureRecord> CreateFeatureAsync(
        CreateFeatureCommand command,
        CancellationToken cancellationToken);
}
```

The HTTP implementation must:

- Propagate cancellation.
- Use bounded timeouts.
- Distinguish `404` from service failure.
- Reject empty or malformed successful responses.
- Preserve known validation failures.
- Convert connectivity, timeout, and open-circuit failures into `503`.
- Disable automatic retries for unsafe writes.

---

## 9. Configure the Public API Application

Register the typed database client:

```csharp
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

builder.Services
    .AddHttpClient<IDatabaseClient, DatabaseClient>((services, client) =>
    {
        var options = services
            .GetRequiredService<IOptions<DatabaseServiceOptions>>()
            .Value;
        client.BaseAddress = new Uri(
            $"{options.BaseUrl.TrimEnd('/')}/",
            UriKind.Absolute);
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.DisableForUnsafeHttpMethods();
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
    });
```

Add:

- `/health/live` for process liveness.
- `/health/ready` with a named HTTP check for the database service.
- CORS restricted to the shared shell and documented development origins.
- Public endpoint groups that delegate persistence through `IDatabaseClient`.
- Exception handling that maps database unavailability to `503`.

The API may remain live while a dependency is unavailable, but readiness must
report the dependency failure.

---

## 10. Add the Initial Migration

Install the EF Core command-line tool at the same version as the project:

```bash
dotnet tool install --global dotnet-ef --version 10.0.11
```

Then create the migration against the database project:

```bash
dotnet ef migrations add InitialCreate \
  --project student-N/database/Database/Database.csproj
```

Review:

- Tables and columns.
- Foreign keys and delete behavior.
- Unique indexes.
- Enum conversions.
- UTC date handling.
- Symmetry between `Up` and `Down`.

Check model drift:

```bash
dotnet ef migrations has-pending-model-changes \
  --project student-N/database/Database/Database.csproj
```

---

## 11. Create the Dockerfiles

Both images use `student-N/` as their build context so they can copy the shared
contracts project.

### API Dockerfile

Create `student-N/backend/Api/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/Api/Api.csproj", "backend/Api/"]
COPY ["contracts/Contracts/Contracts.csproj", "contracts/Contracts/"]
RUN dotnet restore "backend/Api/Api.csproj"
COPY backend/Api/ backend/Api/
COPY contracts/Contracts/ contracts/Contracts/
WORKDIR "/src/backend/Api"
RUN dotnet publish \
  "Api.csproj" \
  -c $BUILD_CONFIGURATION \
  -o /app/publish \
  /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*
USER $APP_UID
ENTRYPOINT ["dotnet", "Api.dll"]
```

### Database Dockerfile

Create `student-N/database/Database/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["database/Database/Database.csproj", "database/Database/"]
COPY ["contracts/Contracts/Contracts.csproj", "contracts/Contracts/"]
RUN dotnet restore "database/Database/Database.csproj"
COPY database/Database/ database/Database/
COPY contracts/Contracts/ contracts/Contracts/
WORKDIR "/src/database/Database"
RUN dotnet publish \
  "Database.csproj" \
  -c $BUILD_CONFIGURATION \
  -o /app/publish \
  /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/* \
  && mkdir -p /app/Data \
  && chown -R $APP_UID:$APP_UID /app/Data
USER $APP_UID
ENTRYPOINT ["dotnet", "Database.dll"]
```

Add a slice-level `.dockerignore` excluding Git metadata, IDE settings,
`bin/`, `obj/`, `node_modules/`, environment files, and secrets.

---

## 12. Register Both Services in Docker Compose

Create a private network and a named volume:

```yaml
services:
  student-N-database:
    build:
      context: ./student-N
      dockerfile: database/Database/Dockerfile
    volumes:
      - student-N-db:/app/Data
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    networks:
      - student-N-data
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "--fail", "--silent", "--show-error", "http://localhost:8080/health/ready"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 15s

  student-N-backend:
    build:
      context: ./student-N
      dockerfile: backend/Api/Dockerfile
    ports:
      - "510N:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      DatabaseService__BaseUrl: http://student-N-database:8080
    networks:
      - default
      - student-N-data
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "--fail", "--silent", "--show-error", "http://localhost:8080/health/ready"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 15s
    depends_on:
      student-N-database:
        condition: service_healthy

volumes:
  student-N-db:

networks:
  default:
  student-N-data:
    internal: true
```

Rules:

- Never publish a database-service host port in Compose.
- Only the database service mounts `student-N-db`.
- Only the API and database services join `student-N-data`.
- The API remains on `default` for Nginx and other service calls.
- Add only required startup dependencies to `depends_on`.
- Optional runtime integrations must not block API startup.
- Using `/health/ready` as the Compose probe intentionally gates dependants
  such as `shared-shell` on database availability. The API process remains
  independently observable through `/health/live`.

---

## 13. Add Reverse Proxy Routing

Only the public API is routed through Nginx:

```nginx
location /api/feature/ {
    proxy_pass http://student-N-backend:8080/api/;
    proxy_connect_timeout 5s;
    proxy_read_timeout 60s;
    proxy_next_upstream error timeout http_502 http_503 http_504;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
}
```

Never add an Nginx route for `student-N-database`.

Add the public backend to `shared-shell.depends_on` when the shell requires it
to be healthy before startup.

---

## 14. Add CI Coverage

Create or update the slice workflow to trigger on:

```yaml
paths:
  - "student-N/backend/**"
  - "student-N/contracts/**"
  - "student-N/database/**"
  - "student-N/frontend/**"
  - "shared/ui-kit/**"
  - "package.json"
  - "package-lock.json"
  - ".github/workflows/student-N-ci.yml"
```

The workflow should:

1. Restore and build the combined solution with warnings as errors.
2. Verify formatting.
3. Run unit and integration tests.
4. Start the database service on standalone port `520N`.
5. Start the API on `510N` with `DatabaseService__BaseUrl`.
6. Exercise the public API contract.
7. Check CORS from the frontend origin.
8. Run EF migration drift against `Database.csproj`.
9. Scan the complete solution for vulnerable packages.

The repository Docker workflow discovers both images automatically from
Compose. Extend its Compose-contract assertions to verify that:

- `student-N-data` is internal.
- `student-N-database` joins only `student-N-data`.
- `student-N-database` has no published ports.
- `student-N-backend` joins both `default` and `student-N-data`.
- Only `student-N-database` mounts `student-N-db`.

Add a focused integration scenario when the slice has meaningful startup,
network, or outage behavior to verify.

---

## 15. Validate the New Slice

Run:

```bash
dotnet restore student-N/backend/StudentN.sln
dotnet build \
  student-N/backend/StudentN.sln \
  --configuration Release \
  -warnaserror
dotnet format \
  student-N/backend/StudentN.sln \
  --verify-no-changes \
  --severity warn
dotnet tool install --global dotnet-ef --version 10.0.11
dotnet ef migrations has-pending-model-changes \
  --project student-N/database/Database/Database.csproj

docker compose config --quiet
docker compose build student-N-backend student-N-database
docker compose up -d student-N-database student-N-backend
```

Confirm:

- API liveness succeeds.
- API readiness succeeds when the database is healthy.
- API readiness returns `503` when the database is stopped.
- Public CRUD requests persist through the database service.
- Transactional domain commands remain atomic.
- The database has no host-published port.
- No unrelated service joins the private database network.
- The API image contains no SQLite file.
- Only the database container mounts the volume.

---

## 16. Completion Checklist

- [ ] API, contracts, and database projects exist.
- [ ] The solution builds all three projects.
- [ ] Only the database project references EF Core.
- [ ] Only the database service owns entities, migrations, and seeding.
- [ ] The API uses a typed HTTP database client.
- [ ] Internal contracts contain no EF types.
- [ ] Multi-record operations are one transactional database command.
- [ ] The database service has liveness and database-backed readiness.
- [ ] The API readiness checks the database service.
- [ ] Database failures become explicit `503` API responses.
- [ ] Unsafe writes are not automatically retried.
- [ ] Only the database container mounts the named volume.
- [ ] The database service has no Compose host port.
- [ ] A private internal database network is configured.
- [ ] Nginx routes only to the public API.
- [ ] Slice CI covers both processes and EF migration drift.
- [ ] Architecture and service documentation are updated.
