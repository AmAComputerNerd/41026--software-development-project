# Playbook: Splitting a Backend into API and Database Services

Use this playbook when an existing ASP.NET Core backend directly owns Entity
Framework Core and must be separated into:

- A public API and orchestration service.
- An internal database service that exclusively owns EF Core and the database.
- A shared contracts project for communication between them.

The Student 3 implementation is the reference example:

```text
student-3/
├── backend/Api/
├── contracts/Contracts/
├── database/Database/
└── frontend/
```

> [!CAUTION]
> A database-service split changes a local method boundary into a distributed
> systems boundary. Do not replace atomic EF operations with several dependent
> HTTP requests. Move transactional behavior with the database.

---

## 1. Define the Target Boundary

The public backend owns:

- Public routes and public DTOs.
- Authentication, authorization, and CORS.
- Canvas, AI, notification, and other external integrations.
- Background workflow orchestration.
- Translation of internal failures into public responses.

The database service owns:

- Entity Framework Core packages and `DbContext`.
- Entity models, relationships, constraints, and converters.
- Migrations and model snapshots.
- Database seeding and startup migration.
- Connection strings, database files, and Docker volumes.
- Transactional domain operations.
- Internal persistence endpoints.

The database service should not call Canvas, AI, notification, or other
application services. The public backend obtains external data and sends
validated persistence commands to the database service.

---

## 2. Inventory Existing Database Access

Before editing, find:

- Every `AddDbContext`, `UseSqlite`, and connection string.
- Every endpoint, service, and worker that injects the `DbContext`.
- All migrations, seeders, entities, indexes, and relationships.
- Startup calls to `Database.Migrate()` or equivalent helpers.
- Docker volume mounts and database filenames.
- Operations that use explicit or implicit transactions.

Useful searches:

```bash
rg "AddDbContext|UseSqlite|DbContext|Database\\.Migrate|MigrateAsync" student-N/backend
rg "ConnectionStrings|Data Source|\\.db" student-N/backend
rg "SaveChanges|BeginTransaction" student-N/backend
```

Create a table of readers and writers:

| Operation | Current owner | Reads | Writes | Must be atomic? |
| --- | --- | --- | --- | ---: |
| List records | API endpoint | Records | — | No |
| Complete parent | API endpoint | Parent and descendants | Parent and descendants | Yes |
| External sync | Sync service | Existing remote records | Upserts and deactivation | Yes |
| Reminder delivery | Background worker | Due records | Delivery timestamp | Partially |

---

## 3. Record Compatibility Requirements

Capture the existing public contract before moving persistence:

- Routes and HTTP methods.
- Request and response JSON.
- Success and failure status codes.
- Validation messages.
- Filtering and sorting behavior.
- Not-found behavior.
- CORS behavior.
- Liveness and readiness behavior.

The frontend and external clients should not need to know that the persistence
implementation moved.

Do not combine this split with:

- Database schema redesign.
- Database filename changes.
- Public endpoint renames.
- DTO redesign.
- Unrelated feature work.

Handle those as later changes after the service boundary is stable.

---

## 4. Create the Shared Contracts Project

Create a dependency-light class library:

```text
student-N/contracts/Contracts/
├── Contracts.csproj
└── PersistenceContracts.cs
```

The contracts project may contain:

- Read records.
- Create and update commands.
- Query/filter contracts.
- Bulk-operation commands.
- Synchronization snapshots.
- Result summaries.

It must not contain:

- EF Core references.
- `DbContext`.
- Database entities or navigation properties.
- Service implementations.
- Database-provider types.

Reference it from both services:

```xml
<ProjectReference Include="..\..\contracts\Contracts\Contracts.csproj" />
```

Keep public API DTOs in the public backend unless the public and internal
contracts are deliberately identical.

---

## 5. Create the Database Web Service

Create an internal ASP.NET Core project:

```text
student-N/database/Database/
├── Data/
├── Endpoints/
├── Extensions/
├── Migrations/
├── Models/
├── Services/
├── Database.csproj
├── Dockerfile
├── Program.cs
└── appsettings.json
```

Use `Microsoft.NET.Sdk.Web`, target the repository's .NET version, and add:

- `Microsoft.EntityFrameworkCore.Design`
- The existing EF Core database provider.
- EF Core health checks.
- The shared contracts project reference.

The service startup should:

1. Register the existing `DbContext`.
2. Preserve converters, seeding, and provider settings.
3. Register transactional persistence services.
4. Map internal endpoints.
5. Map `/health/live`.
6. Map `/health/ready` using a real database check.
7. Apply existing migrations before accepting traffic.

Do not enable browser CORS or expose public feature routes.

---

## 6. Move Files with Git History

Move tracked files before editing them:

```bash
git mv student-N/backend/Api/Data \
  student-N/database/Database/Data
git mv student-N/backend/Api/Models \
  student-N/database/Database/Models
git mv student-N/backend/Api/Migrations \
  student-N/database/Database/Migrations
```

Move database-owned services individually:

```bash
git mv \
  student-N/backend/Api/Services/TransactionalService.cs \
  student-N/database/Database/Services/TransactionalService.cs
```

After moving:

1. Update namespaces.
2. Update project references.
3. Avoid unrelated rewrites.
4. Confirm rename detection:

```bash
git diff --summary
git log --follow -- student-N/database/Database/Data/AppDbContext.cs
```

Git infers renames from similarity. Smaller edits preserve history more
reliably.

---

## 7. Design Internal Persistence Endpoints

Prefer domain-oriented endpoints over a generic repository API:

```text
GET  /internal/tasks
GET  /internal/tasks/{id}
POST /internal/tasks
PUT  /internal/tasks/{id}
POST /internal/tasks/{id}/subtasks
POST /internal/canvas-snapshots
GET  /internal/reminders/due
PUT  /internal/reminders/{id}/sent
```

Keep these operations inside one database-service request when they must be
atomic:

- Updating a parent and all descendants.
- Inserting a generated batch.
- Applying a remote-system snapshot.
- Updating several related entities.
- Validating relationships and saving the result.

The database service should return explicit:

- `200`/`201` success responses.
- `400` validation responses.
- `404` missing-resource responses.
- `409` conflict responses where appropriate.
- `500` only for unexpected server failures.

---

## 8. Add a Typed Database Client

Create an interface in the public backend:

```csharp
public interface IDatabaseClient
{
    Task<TaskRecord?> GetTaskAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<TaskRecord> CreateTaskAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken);
}
```

The HTTP implementation must:

- Use `HttpClient`.
- Propagate cancellation tokens.
- Apply bounded timeouts.
- Distinguish `404` from service failure.
- Reject malformed or empty success responses.
- Preserve expected validation responses.
- Convert connectivity failures into `503 Service Unavailable`.
- Avoid automatically retrying unsafe writes.

Register and validate its base URL:

```text
DatabaseService__BaseUrl=http://student-N-database:8080
```

---

## 9. Refactor the Public Backend

Replace direct EF access in this order:

1. Read-only endpoints.
2. Simple create, update, and delete endpoints.
3. AI or external-service workflows.
4. Transactional operations.
5. Background workers.
6. Startup migration and seeding.

Once no callers remain:

- Remove EF Core packages from the API project.
- Remove the connection string.
- Remove `DbContext` registration.
- Remove migration startup.
- Remove the database volume from the API container.

Verify with:

```bash
rg "DbContext|EntityFrameworkCore|ConnectionStrings|Data Source" \
  student-N/backend
```

---

## 10. Preserve External Orchestration

External calls stay in the public backend:

```text
Client
  -> Public backend
  -> External gateway
  -> Public backend validates result
  -> Database service persists result
```

Examples:

```text
Backend -> Canvas gateway -> Backend -> Database service
Backend -> Database service for context -> AI gateway -> Database service
Backend -> Database service for due work -> Notification service
        -> Database service records successful delivery
```

For external side effects, record success only after the external operation
succeeds. Make retryable operations idempotent or use durable claim keys where
duplicate execution would be harmful.

---

## 11. Add Docker Images and Preserve Data

Create separate Dockerfiles for the API and database services.

The API image should:

- Build the API and contracts projects.
- Contain no database directory setup.
- Run without a database volume.

The database image should:

- Build the database and contracts projects.
- Create and own `/app/Data`.
- Run as the repository's standard non-root user.
- Include the health-check client used by Compose.

Use a shared parent build context when both projects reference the contracts
project:

```yaml
student-N-backend:
  build:
    context: ./student-N
    dockerfile: backend/Api/Dockerfile

student-N-database:
  build:
    context: ./student-N
    dockerfile: database/Database/Dockerfile
```

Move the existing named volume rather than creating a replacement:

```yaml
student-N-database:
  volumes:
    - student-N-db:/app/Data

student-N-backend:
  # No database volume.
```

Keep the existing mount path and database filename. Never run the old backend
and new database service against the same SQLite file simultaneously.

---

## 12. Isolate the Database Network

Create an internal network shared only by the API and database services:

```yaml
services:
  student-N-database:
    networks:
      - student-N-data

  student-N-backend:
    networks:
      - default
      - student-N-data

networks:
  default:
  student-N-data:
    internal: true
```

Do not add a Compose `ports` mapping to the database service.

An optional standalone development port belongs in `launchSettings.json`, not
the production-like Compose topology.

Use `depends_on` only for required startup dependencies:

```yaml
student-N-backend:
  depends_on:
    student-N-database:
      condition: service_healthy
```

Optional runtime integrations should not block API startup.

---

## 13. Update CI

Add the database and contracts paths to the owning slice's CI workflow.

The slice workflow should:

- Build and format the combined solution.
- Run public API contract checks with both processes.
- Run EF migration drift checks against the database project.
- Scan both services for vulnerable dependencies.
- Confirm database unavailability makes API readiness return `503`.

The Docker workflow should:

- Discover buildable images from `docker compose config`.
- Build them through the dynamic image matrix.
- Assert that the database has no published ports.
- Assert that only the API joins the database network.
- Assert that the API no longer mounts the database volume.
- Run a focused Compose integration test for the migrated slice.

---

## 14. Roll Out Safely

Before switching containers:

1. Stop the old backend.
2. Back up the database volume while no process is writing.
3. Start only the new database service.
4. Allow it to apply the existing migrations.
5. Verify database readiness and expected record counts.
6. Start the refactored API.
7. Exercise reads, writes, transactional commands, and background workflows.
8. Confirm only the database container mounts the data volume.

Do not introduce a new schema migration merely to split service ownership.

---

## 15. Roll Back

If the migration must be reversed:

1. Stop the new API and database services.
2. Restore the backup only if data changed incorrectly.
3. Start the previous backend image.
4. Reattach the original volume at its original path.
5. Verify readiness and basic reads and writes.

Keep schema changes separate from the service split so the previous backend
remains compatible with the database.

---

## 16. Acceptance Checklist

The migration is complete when:

- [ ] Public routes and JSON contracts remain compatible.
- [ ] The public backend contains no EF Core references.
- [ ] The public backend contains no database connection string.
- [ ] The public backend does not mount the database volume.
- [ ] The database service exclusively owns entities and migrations.
- [ ] Existing data opens without a destructive migration.
- [ ] Transactional workflows remain atomic.
- [ ] Unsafe writes are not automatically retried.
- [ ] Database failures return explicit API failures.
- [ ] API readiness reflects database availability.
- [ ] The database service has no host-published port.
- [ ] Only the owning API can reach the database network.
- [ ] Optional integrations do not create unnecessary startup coupling.
- [ ] CI builds both services and checks migration drift.
- [ ] Git detects moved persistence files as renames.
- [ ] The rollback path has been documented and exercised.
