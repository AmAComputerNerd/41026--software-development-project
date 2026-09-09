# Grades & Progress Backend (`student-5/backend`)

ASP.NET Core (.NET 10) Minimal API microservice for course grade calculation, mark aggregation, and what-if simulation.

---

## 1. Setup & Run

The backend delegates persistence over HTTP to the internal `student-5-database` service.

```bash
# Terminal 1: Run Database Service
dotnet run --project student-5/database/Database/Database.csproj

# Terminal 2: Run Public Backend
dotnet run --project student-5/backend/GradesManager/GradesManager/GradesManager.csproj
```

Runs on host port `5105` (inside Docker: port `8080`).

---

## 2. Key Capabilities & Endpoints

- `GET /api/grades` — Returns current enrolled course grades and weight breakdown.
- `PUT /api/grades/api/assignment/marks/` — Updates or simulates assignment marks.

---

## 3. Database & EF Core

Persistence is owned by `student-5/database` using Entity Framework Core with SQLite (`grades.db`). To add migrations:

```bash
dotnet ef migrations add <MigrationName> --project student-5/database/Database/Database.csproj
```

