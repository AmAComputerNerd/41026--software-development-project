# Architecture overview

Current state as of this document's last update. This is source material
for the technical report's architecture diagrams, and orientation for
anyone (or any AI agent) new to the repo.

## Services

| Service | Path | Stack | Port (host) | Owns | Depends on |
|---|---|---|---|---|---|
| shared-shell | shared/frontend | Vue 3 + nginx | 8080 | Unified dashboard, reverse proxy to every feature | every frontend + backend |
| shared-backend | shared/backend | ASP.NET Core + SQLite | 5110 | Canvas API integration (courses, assignments, users), in-memory cache, audit log | — |
| ai-mode | ai-services/ai-mode | ASP.NET Core | (internal only) | OpenRouter proxy, shared LLM access | — |
| student-1-frontend | student-1/frontend | Vue 3 | (proxied at /notifications/) | Notifications UI | student-1-backend |
| student-1-backend | student-1/backend | ASP.NET Core + SQLite | 5101 | Notifications, preferences, AI digest | ai-mode |
| student-3-frontend | student-3/frontend | Vue 3 | (proxied at /deadlines/) | Deadlines & tasks UI | student-3-backend |
| student-3-backend | student-3/backend | ASP.NET Core + SQLite | 5103 | Deadlines & task tracker, Canvas assignment sync, AI task planning | shared-backend, ai-mode |
| student-5-frontend | student-5/frontend | Vue 3 | (proxied at /grades/) | Grades & progress UI | student-5-backend |
| student-5-backend | student-5/backend | ASP.NET Core + SQLite | 5105 | Grades & progress API | (no cross-service calls yet) |

Students 2 (Automations) and 4 (Account) have placeholder
`backend/` and `frontend/` directories only — no code yet. The
nginx stubs for `/account/` and `/automations/` are still commented
out in `shared/frontend/nginx.conf`; their dashboard tiles render
as "coming soon" until those slices ship.

## Shared infrastructure

Owned by student-1 (Bryan), per team assignment:
- `shared/frontend` — the dashboard shell and reverse proxy.
- `shared/ui-kit` — design tokens, fonts, shared Vue components,
  consumed as an npm workspace package by every frontend.
- `ai-services/ai-mode` — shared OpenRouter gateway.

Owned by student-3 (Jonathon), used by the whole team:
- `shared/backend` — Canvas API integration, other services call its
  HTTP API and don't read its database directly.

Canvas assignment descriptions are treated as untrusted HTML. The shared
backend parses them into compact plain text before returning its API DTOs,
so downstream services neither render raw Canvas markup nor send it to AI
models.

## Reverse proxy routing

`shared-shell`'s nginx config maps:
- `/` — the dashboard itself.
- `/notifications/` → student-1-frontend; `/api/notifications/` → student-1-backend.
- `/deadlines/` → student-3-frontend; `/api/deadlines/` → student-3-backend.
- `/grades/` → student-5-frontend; `/api/grades/` → student-5-backend.
- `/automations/`, `/account/` — stubbed but commented out, waiting
  on students 2 and 4. Note: the in-file comment in
  `shared/frontend/nginx.conf` labels the `/account/` stub as
  "student-5"; that is a leftover from an earlier reassignment. The
  current student-5 route is `/grades/`, and `/account/` is unowned.

## Data flow

Two concrete paths to know:

**Canvas assignment → task in the tracker**

1. The user (or a scheduled job) calls
   `POST /api/canvas-sync` on student-3-backend.
2. student-3-backend calls `GET /courses`, `GET /assignments`, etc.
   on shared-backend over the internal Docker network.
3. shared-backend hits Canvas with the user's `CANVAS_API_TOKEN`,
   caches results briefly in memory, parses assignment HTML into
   plain text, and writes an audit row to its own SQLite.
4. student-3-backend upserts one task per stable Canvas assignment
   ID into its own SQLite. Removed assignments are flagged inactive,
   not deleted. Submitted/graded assignments mark the task complete.

**Canvas notification → notification row in student-1**

1. student-1-backend polls shared-backend for new notifications.
2. shared-backend proxies the call to Canvas.
3. student-1-backend persists the notification, applies the user's
   per-type delivery preferences, and (on demand) asks ai-mode for
   an LLM-generated digest of recent activity.

The browser only talks to the shared shell, never directly to
ai-mode or any backend.

## AI-mode

Using OpenRouter (`nvidia/nemotron-3-ultra-550b-a55b:free`), approved by
the tutor as a substitute for the spec's suggested Ollama runtime. One
shared API key held only by the `ai-mode` gateway service, other backends
call the gateway rather than OpenRouter directly.

The deadline tracker uses its backend as the AI boundary. It resolves
course and assignment context from its own database, submits bounded
prompts to `ai-mode`, validates structured responses, and persists
generated subtasks atomically. The browser never calls `ai-mode`
directly.

## Database boundary

Each backend owns its own SQLite file. No service reads another's
database directly. `shared-backend`'s SQLite stores only a Canvas
request audit log, not cached Canvas data (course/assignment/user
data is fetched live with a short in-memory cache, not persisted).

## Environment variables

The root `.env` (gitignored, copy from `.env.example`) holds shared
secrets. `docker-compose.yml` injects them into the services that need
them. Per-service `environment:` blocks in compose set the rest.

| Variable | Read by | Notes |
|---|---|---|
| `OPENROUTER_API_KEY` | ai-mode | The only service that needs the OpenRouter key. |
| `CANVAS_BASE_URL` | shared-backend | Root URL of the Canvas instance (e.g. `https://your-institution.instructure.com`). |
| `CANVAS_API_TOKEN` | shared-backend | Personal Canvas access token. |
| `ASPNETCORE_ENVIRONMENT` | every .NET service | `Development` in compose; required for `dotnet user-secrets` to load. |
| `SharedService__BaseUrl` | student-1-backend, student-3-backend | URL of shared-backend, in compose's DNS: `http://shared-backend:8080`. |
| `AiGateway__BaseUrl` | student-3-backend | URL of ai-mode, in compose's DNS: `http://ai-mode:8080`. |
| `CanvasSync__IntervalMinutes` | student-1-backend | Polling cadence for Canvas-native notifications. |
| `Cors__AllowedOrigins__*` | student-1-backend | Allow-list of dev origins. CORS is locked down; new dev URLs need an entry here. |

Running a .NET backend outside Docker: set these as actual
environment variables, or via `dotnet user-secrets set` in that
service's `Api/` directory. user-secrets only load under
`ASPNETCORE_ENVIRONMENT=Development`; `dotnet run --no-launch-profile`
silently skips them.

## Troubleshooting

**AI feature returns 500.** `OPENROUTER_API_KEY` is unset or wrong.
Check root `.env`, then `docker compose up -d ai-mode` to restart
the gateway with the new value.

**Service can't reach another service.** Confirm both are running
under `docker compose`. Standalone `dotnet run` won't get the
internal DNS names; cross-service calls only work in compose.

**CORS error in the browser.** Add the dev origin to
`Cors__AllowedOrigins__*` in `docker-compose.yml` and restart
student-1-backend.

**Frontend shows the dashboard but the feature is blank.** The
nginx route is commented out (only happens for unbuilt slices like
`/automations/` and `/account/`). Confirm the feature actually has
code; if it does, the route block in `shared/frontend/nginx.conf`
needs uncommenting and the `shared-shell` service needs to
`depends_on` the new frontend/backend.

**EF Core complains about pending migrations.** A model change was
made without a corresponding migration. Run
`dotnet ef migrations add <Name> --project Api/Api.csproj` from the
service's backend directory.

## CI

Each service group has its own GitHub Actions workflow under
`.github/workflows/`. Path filters keep them scoped: `shared-ci.yml`
only fires on changes to `shared/`, `student-3-ci.yml` only on
`student-3/`, etc. All run on PRs into `main` and on pushes to
`main`.
