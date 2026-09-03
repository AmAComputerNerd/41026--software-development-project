# Database & Entity Framework Core Migrations Guide

This document details the database architecture, schema management, and Entity Framework Core migration workflows for the microservices in this repository.

---

## 1. Database Architecture & Boundaries

### Strict Isolation Rule
Each backend microservice maintains an **independent SQLite database**:
- `student-1/backend/Api`: `notifications.db` (or configured database name)
- `student-3/backend/Api`: `deadlines.db`
- `student-5/backend/Api`: `grades.db`
- `shared/backend/Api`: `canvas_audit.db`

> [!CAUTION]
> **Zero Cross-Database Access**: Microservices must never open another service's SQLite file directly or attach to another database context. Cross-service data requests must always proceed via HTTP API endpoints.

---

## 2. Managing EF Core Migrations

Whenever you modify an entity class or `DbContext` model configuration:

### Step 1: Create a Migration
Navigate to the microservice's `backend` or `Api` directory:

```bash
# Example: Adding a migration to student-1
cd student-1/backend
dotnet ef migrations add <DescriptiveMigrationName> --project Api/Api.csproj
```

### Step 2: Review Generated Migration
Check the newly generated migration file in `Api/Migrations/`. Verify:
- Up and Down methods are symmetric and reversible.
- Column types, foreign keys, and indexes match the intended design.
- No unintended drops or schema truncations occurred.

### Step 3: Apply Migration Locally
```bash
dotnet ef database update --project Api/Api.csproj
```

In Docker Compose mode, migrations are typically applied automatically during application startup via `context.Database.Migrate()` or `DatabaseMigrator`.

---

## 3. Database Seeding Conventions

- Seeding logic resides in dedicated seeder classes (e.g. `DbInitializer.cs` or `DatabaseSeeder.cs`).
- Seeding should be idempotent (e.g. check `!context.Notifications.Any()` before adding seed data).
- Timestamps must always be stored in **UTC** (`DateTime.UtcNow`).

---

## 4. Troubleshooting Common Database Issues

- **Pending Model Changes Error (`InvalidOperationException`)**:
  - Cause: A model was modified without generating a matching migration.
  - Fix: Run `dotnet ef migrations add <Name> --project Api/Api.csproj`.
- **Database Lock / Busy Errors (`SQLite Error 5: 'database is locked'`)**:
  - Cause: Multiple processes accessing the SQLite file simultaneously without write-ahead logging (WAL).
  - Fix: Enable WAL mode in DbContext setup (`PRAGMA journal_mode=WAL;`).
- **Resetting Database to Fresh State**:
  - In Docker: `docker compose down -v && docker compose up --build`
  - Outside Docker: Delete the local `.db` file and run `dotnet ef database update`.
