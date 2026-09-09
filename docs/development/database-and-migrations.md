# Database & Entity Framework Core Migrations Guide

This document details the database architecture, schema management, and Entity Framework Core migration workflows for the microservices in this repository.

For the complete extraction process, see
[Playbook: Splitting a Backend into API and Database Services](../playbooks/split-database-service.md).

---

## 1. Database Architecture & Boundaries

### Strict Isolation Rule
Each persistence-owning service maintains an **independent, isolated database**:
- `student-1`: **PostgreSQL 16** container (`student-1-database`, database: `notifications_db`). Initialized via `student-1/database/init.sql` and EF Core PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- `student-2`: **SQLite** database (`student-2-db`) managed directly by `student-2/backend/Api`.
- `student-3`: **SQLite** database (`student-3-db`) owned exclusively by the dedicated `student-3/database/Database` service on private network `student-3-data`.
- `student-4`: **SQLite** database (`student-4-db`) owned exclusively by the dedicated `student-4/database/Database` service on private network `student-4-data`.
- `student-5`: **SQLite** database (`student-5-db`) owned exclusively by the dedicated `student-5/database/Database` service on private network `student-5-data`.
- `shared-backend`: **SQLite** audit database (`shared-db`) managed by `shared/backend/Api`.

> [!CAUTION]
> **Zero Cross-Database Access**: Microservices must never open another service's database file/connection directly or attach to another database context. Cross-service data requests must always proceed via HTTP API endpoints.

---

## 2. Managing EF Core Migrations

Whenever you modify an entity class or `DbContext` model configuration:

### Step 1: Create a Migration
Run migrations against the dedicated persistence-owning project:

```bash
# Student 3
dotnet ef migrations add <DescriptiveMigrationName> \
  --project student-3/database/Database/Database.csproj

# Student 4
dotnet ef migrations add <DescriptiveMigrationName> \
  --project student-4/database/Database/Database.csproj

# Student 5
dotnet ef migrations add <DescriptiveMigrationName> \
  --project student-5/database/Database/Database.csproj

# Student 2
dotnet ef migrations add <DescriptiveMigrationName> \
  --project student-2/backend/Api/Api.csproj

# Shared Backend
dotnet ef migrations add <DescriptiveMigrationName> \
  --project shared/backend/Api/Api.csproj
```

### Step 2: Review Generated Migration
Check the newly generated migration file in the owning project's `Migrations/` directory. Verify:
- Up and Down methods are symmetric and reversible.
- Column types, foreign keys, and indexes match the intended design.
- No unintended drops or schema truncations occurred.

### Step 3: Apply Migration Locally
```bash
dotnet ef database update \
  --project student-N/database/Database/Database.csproj
```

In Docker Compose mode, migrations are applied automatically during application startup via `context.Database.Migrate()` or `DatabaseMigrator`.

Only the owning database service may mount its database volume or apply migrations. The public backend accesses persistence exclusively through its internal HTTP client.

---

## 3. Database Seeding Conventions

- Seeding logic resides in dedicated seeder classes (e.g. `DbInitializer.cs` or `DatabaseSeeder.cs`, or `init.sql` for PostgreSQL).
- Seeding should be idempotent (e.g. check `!context.Notifications.Any()` before adding seed data).
- Timestamps must always be stored in **UTC** (`DateTime.UtcNow`).

---

## 4. Troubleshooting Common Database Issues

- **Pending Model Changes Error (`InvalidOperationException`)**:
  - Cause: A model was modified without generating a matching migration.
  - Fix: Run `dotnet ef migrations add <Name> --project <OwningProject>.csproj`.
- **Database Lock / Busy Errors (`SQLite Error 5: 'database is locked'`)**:
  - Cause: Multiple processes accessing the SQLite file simultaneously without write-ahead logging (WAL).
  - Fix: Enable WAL mode in DbContext setup (`PRAGMA journal_mode=WAL;`).
- **Resetting Database to Fresh State**:
  - In Docker: `docker compose down -v && docker compose up --build`
  - Outside Docker: Delete the local `.db` file or recreate PostgreSQL container, then run `dotnet ef database update`.

