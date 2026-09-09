# Database & Entity Framework Core Migrations Guide

This document details the database architecture, schema management, and Entity Framework Core migration workflows for the microservices in this repository.

For the complete extraction process, see
[Playbook: Splitting a Backend into API and Database Services](../playbooks/split-database-service.md).

---

## 1. Database Architecture & Boundaries

### Strict Isolation Rule
Each persistence-owning service maintains an **independent database**:
- `student-1/database`: PostgreSQL 16 container (`notifications_db`), schema and
  seed data from `init.sql`; `student-1/backend/Api` connects to it with EF Core
  and `Npgsql`
- `student-2/backend/Api`: `automations.db` (SQLite)
- `student-3/database/Database`: `app.db` (SQLite) in Docker Compose
- `student-4/database/Database`: `app.db` (SQLite)
- `student-5/database/Database`: `app.db` (SQLite)
- `shared/backend/Api`: `shared.db` — Canvas request audit log (SQLite)

> [!CAUTION]
> **Zero Cross-Database Access**: Microservices must never open another service's database directly or attach to another database context. Cross-service data requests must always proceed via HTTP API endpoints.

---

## 2. Managing EF Core Migrations

Whenever you modify an entity class or `DbContext` model configuration:

### Step 1: Create a Migration
Slices with a dedicated database service place migrations in that service:

```bash
dotnet ef migrations add <DescriptiveMigrationName> \
  --project student-N/database/Database/Database.csproj
```

Services that still own EF Core inside their API project use that project
instead. For example:

```bash
dotnet ef migrations add <DescriptiveMigrationName> \
  --project student-1/backend/Api/Api.csproj

dotnet ef migrations add <DescriptiveMigrationName> \
  --project student-2/backend/Api/Api.csproj
```

### Step 2: Review Generated Migration
Check the newly generated migration file in the owning project's
`Migrations/` directory. Verify:
- Up and Down methods are symmetric and reversible.
- Column types, foreign keys, and indexes match the intended design.
- No unintended drops or schema truncations occurred.

### Step 3: Apply Migration Locally
```bash
dotnet ef database update \
  --project student-N/database/Database/Database.csproj
```

In Docker Compose mode, migrations are typically applied automatically during application startup via `context.Database.Migrate()` or `DatabaseMigrator`.

For Students 3, 4, and 5, only the `student-N-database` service may mount
`student-N-db` or apply migrations. The matching public backend accesses
persistence exclusively through the internal HTTP API.

---

## 3. Database Seeding Conventions

- Seeding logic resides in dedicated seeder classes (e.g. `DbInitializer.cs` or `DatabaseSeeder.cs`).
- Seeding should be idempotent (e.g. check `!context.Notifications.Any()` before adding seed data).
- Timestamps must always be stored in **UTC** (`DateTime.UtcNow`).

---

## 4. Troubleshooting Common Database Issues

- **Pending Model Changes Error (`InvalidOperationException`)**:
  - Cause: A model was modified without generating a matching migration.
  - Fix: Run `dotnet ef migrations add <Name> --project student-N/database/Database/Database.csproj`.
- **Database Lock / Busy Errors (`SQLite Error 5: 'database is locked'`)**:
  - Cause: Multiple processes accessing the SQLite file simultaneously without write-ahead logging (WAL).
  - Fix: Enable WAL mode in DbContext setup (`PRAGMA journal_mode=WAL;`).
- **Resetting Database to Fresh State**:
  - In Docker: `docker compose down -v && docker compose up --build`
  - Outside Docker: delete the local `.db` file (or drop the PostgreSQL volume
    for Student 1) and run `dotnet ef database update`.
