# Database & Migrations Guide

> Database architecture, Entity Framework Core workflow, PostgreSQL container setup, and migrations management.

---

## 1. Database Architecture & Ownership Matrix

Each vertical slice microservice strictly owns its own dedicated database. **Cross-database queries are forbidden.** All cross-service data queries occur via HTTP APIs.

| Service | Database Engine | Database Name / File | Migration Location | Network / Visibility |
|---|---|---|---|---|
| **`student-1`** | PostgreSQL 16 (Alpine) | `notifications_db` | `student-1/database/init.sql` & EF Core | Public port `5432` / internal `student-1-database` |
| **`student-2`** | SQLite | `student-2-db` (`app.db`) | `student-2/backend/Api/Migrations/` | Persistent named volume |
| **`student-3`** | SQLite | `student-3-db` (`app.db`) | `student-3/database/Database/Migrations/` | Private network `student-3-data` (`student-3-database`) |
| **`student-4`** | SQLite | `student-4-db` (`app.db`) | `student-4/database/Database/Migrations/` | Private network `student-4-data` (`student-4-database`) |
| **`student-5`** | SQLite | `student-5-db` (`grades.db`) | `student-5/database/Database/Migrations/` | Private network `student-5-data` (`student-5-database`) |
| **`shared-backend`** | SQLite | `shared-db` (`audit.db`) | `shared/backend/Api/Migrations/` | Persistent named volume |

---

## 2. EF Core Migrations Lifecycle

When modifying entity models in any slice, you must create and apply a migration.

### Step 1: Generate Migration
Run `dotnet ef migrations add` targeting the specific project that owns persistence:

```bash
# Example: Student 1 (PostgreSQL)
dotnet ef migrations add <DescriptiveMigrationName>   --project student-1/backend/Api/Api.csproj

# Example: Student 2 (SQLite)
dotnet ef migrations add <DescriptiveMigrationName>   --project student-2/backend/Api/Api.csproj

# Example: Student 3 (Internal Database Service)
dotnet ef migrations add <DescriptiveMigrationName>   --project student-3/database/Database/Database.csproj

# Example: Student 4 (Internal Database Service)
dotnet ef migrations add <DescriptiveMigrationName>   --project student-4/database/Database/Database.csproj

# Example: Student 5 (Internal Database Service)
dotnet ef migrations add <DescriptiveMigrationName>   --project student-5/database/Database/Database.csproj

# Example: Shared Backend
dotnet ef migrations add <DescriptiveMigrationName>   --project shared/backend/Api/Api.csproj
```

### Step 2: Review Generated Migration
Check the newly generated migration file in the owning project's `Migrations/` directory. Verify:
- Up and Down methods are symmetric and reversible.
- Column types, foreign keys, and indexes match the intended design.
- No unintended drops or schema truncations occurred.

### Step 3: Apply Migration Locally
```bash
dotnet ef database update   --project <persistence-owning-project.csproj>
```

In Docker Compose mode, migrations are applied automatically during application startup via `context.Database.Migrate()` or `DatabaseMigrator`.

For Students 3, 4, and 5, only the `student-N-database` service may mount `student-N-db` or apply migrations. The matching public backend accesses persistence exclusively through the internal HTTP API.

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
  - Outside Docker: delete the local `.db` file (or drop the PostgreSQL volume for Student 1) and run `dotnet ef database update`.
