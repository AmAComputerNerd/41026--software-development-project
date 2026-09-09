# Grades & Progress Backend (`student-5/backend`)

ASP.NET Core (.NET 10) Minimal API microservice for course grade calculation, mark aggregation, what-if simulation, and AI study recommendations.

Persistence is owned by the separate `student-5/database` service; this project
holds no EF Core context, connection string, or database volume.

---

## 1. Setup & Run

The backend delegates persistence over HTTP to the internal `student-5-database` service.

From the repository root:

```bash
dotnet restore student-5/backend/GradesManager/GradesManager.slnx
dotnet run --project student-5/backend/GradesManager/GradesManager/GradesManager.csproj
```

Docker Compose publishes the service on host port `5105` (inside Docker: port
`8080`). Standalone `dotnet run` uses the `http` launch profile on port `5272`,
so pass `--urls http://localhost:5105` if you want the frontend's default base
URL to resolve.

The private database service must be running first, in its own terminal:

```bash
dotnet run --project student-5/database/Database/Database.csproj --urls http://localhost:5205
```

Its URL comes from `DatabaseService:BaseUrl` (`http://localhost:5205` by
default in `appsettings.json`, `http://student-5-database:8080` in Compose).

---

## 2. Key Capabilities & Endpoints

The shared shell proxies `/api/grades/` to this service's root, so each route
below is reachable externally as `/api/grades<route>`.

- `GET /api/courses`, `GET /api/courses/{id}` — Course listings.
- `GET /api/students`, `GET /api/students/{id}` — Student records and ideal marks.
- `POST /api/students`, `PUT /api/students`, `DELETE /api/students/{studentId}` — Ideal-mark management.
- `GET /api/assignment/{id}`, `/student/{studentId}`, `/course/{courseId}` — Assignment lookups.
- `POST|PUT /api/assignment/marks/`, `DELETE /api/assignment/marks/{studentId}/{assignmentId}`, `GET /api/assignment/marks/{studentId}` — Temporary ("what-if") marks.
- `POST /api/ai/generate-recommendation` — AI study recommendation via the `ai-mode` gateway.

---

## 3. Database & EF Core

Entity Framework Core, SQLite, and all migrations live in
`student-5/database/Database`. To add a migration:

```bash
dotnet ef migrations add <MigrationName>   --project student-5/database/Database/Database.csproj
```
