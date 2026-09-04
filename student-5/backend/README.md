# Grades & Progress Backend (`student-5/backend`)

ASP.NET Core (.NET 10) Minimal API microservice for course grade calculation, mark aggregation, and what-if simulation.

---

## 1. Setup & Run

```bash
cd GradesManager
dotnet restore
dotnet run
```

Runs on host port `5105` (inside Docker: port `8080`).

---

## 2. Key Capabilities & Endpoints

- `GET /api/grades` — Returns current enrolled course grades and weight breakdown.
- `PUT /api/grades/api/assignment/marks/` — Updates or simulates assignment marks.

---

## 3. Database & EF Core

Uses Entity Framework Core with SQLite (`grades.db`). To add migrations:

```bash
dotnet ef migrations add <MigrationName> --project GradesManager.csproj
```
