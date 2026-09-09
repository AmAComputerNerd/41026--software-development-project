# 41026 — Advanced Software Development; Project

An LLM-enhanced web application built on top of the Canvas Infrastructure
API. The system is a 5-student microservices project (ASD unit): each
student owns a vertical slice, services talk to each other over HTTP,
and one shared Canvas gateway is the only thing that talks to Canvas
directly. A shared dashboard shell routes users to each microservice.

For AI agent guidance, see [`AGENTS.md`](AGENTS.md). For the complete
documentation hub, see [`docs/README.md`](docs/README.md) and
[`docs/architecture/overview.md`](docs/architecture/overview.md). For how to
contribute, see [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Team

**Student 1: Bryan Lee (25495108).**  
Working directory: `student-1/`  
Notifications service: manages student notifications (deadlines, grades,
automation, account, and AI-sourced) with read/unread state, per-student
delivery preferences by notification type and channel (in-app or email),
real-time SSE streaming (`NotificationStreamBroker`), and AI-generated digests
and chat assistant summarising recent notification activity. Persistence is
backed by an isolated PostgreSQL 16 container (`notifications_db`). Includes
comprehensive xUnit backend tests and Playwright frontend e2e tests.

**Student 2: Isaac Thomas (25341708).**  
Working directory: `student-2/`  
Automations service: configures assignment extension, scheduled post, and quiz
filler automations, stores each type in its own Entity Framework table, and
provides read-only records of previous runs. A periodic worker executes due
scheduled posts and fills eligible Canvas quizzes through the shared backend's
Canvas gateway and `ai-mode`, using durable execution keys to prevent duplicate runs.

**Student 3: Jonathon Thomson (25488154).**  
Working directory: `student-3/`  
Deadline and task-tracker service: manages courses and coursework tasks,
including priorities, completion states, due dates, subtasks, filtering, and
Canvas assignment imports through the shared backend. Canvas sync keeps one
primary task per assignment and updates it on later imports. AI task breakdowns
generate structured subtasks. Persistence is exclusively delegated to the internal
`student-3-database` service on a private Docker network, and task events push
real-time notifications to Student 1.

**Student 4: Tristan Huang (25322025).**  
Working directory: `student-4/`  
Account & Authentication service: manages user profiles, roles, and settings.
Includes secure password hashing, profile AI summaries via `ai-mode`, and a
"forgot password" workflow dispatching password reset emails via MailHog SMTP.
Persistence is exclusively delegated to the internal `student-4-database` service
on a private Docker network.

**Student 5: William Hannah (25494675).**  
Working directory: `student-5/`  
Grades and progress service: aggregates Canvas grade data, course weightings,
and cumulative marks with an interactive "What-If" grade simulator. Persistence
is exclusively delegated to the internal `student-5-database` service on a private
Docker network. Backend listens on host port `5105`; frontend is proxied through
the shared shell at `/grades/`.

## Quickstart

You need: Docker Desktop (or compatible), Node 22 (only if you intend to
run a frontend or the ui-kit standalone outside Docker), and the .NET
10 SDK (only if you intend to run a backend standalone outside Docker).

1. Copy the env template and fill in the values you need:

   ```bash
   cp .env.example .env
   # edit .env and paste your OpenRouter and Canvas credentials
   ```

   The only required keys are `OPENROUTER_API_KEY` (for AI features),
   `CANVAS_BASE_URL` (the root URL of your Canvas instance), and
   `CANVAS_API_TOKEN` (a personal Canvas access token). `docker-compose.yml`
   injects these into the services that own each integration. Without an
   OpenRouter key, AI features will return 500s; without Canvas
   credentials, the sync endpoints will fail.

2. Bring the whole stack up:

   ```bash
   docker compose up --build
   ```

3. Open the dashboard at <http://localhost:8080>.

To stop: `docker compose down`. To wipe database volumes (forces a
clean re-seed on next start): `docker compose down -v`.

## What runs where

Host ports are set in `docker-compose.yml`. Backends are exposed
directly so you can hit them with `curl` or a REST client; frontends
sit behind the shared shell and are accessed through Nginx reverse proxy routes.

| Host port | Service                          | Notes |
|----------:|----------------------------------|-------|
| `8080`    | `shared-shell` (nginx)           | Dashboard entry point. Proxies `/notifications`, `/automations`, `/deadlines`, `/account`, `/grades`, and `/api/*` to the right microservice. |
| `5101`    | `student-1-backend`              | Notifications API. |
| `5432`    | `student-1-database`             | PostgreSQL database (`notifications_db`). |
| `5102`    | `student-2-backend`              | Automations API. |
| `5103`    | `student-3-backend`              | Deadlines & tasks API. |
| `5203`    | `student-3-database` (standalone)| Internal Student 3 persistence API on private network `student-3-data`. |
| `5104`    | `student-4-backend`              | Account API (`/api/users/*`, `/api/students/*`, `/api/teachers/*`). |
| `5114`    | `student-4-authentication`       | Authentication API (`/api/auth/*`). |
| `5204`    | `student-4-database` (standalone)| Internal Student 4 persistence API on private network `student-4-data`. |
| `5105`    | `student-5-backend`              | Grades & progress API (`/api/grades/*`). |
| `5205`    | `student-5-database` (standalone)| Internal Student 5 persistence API on private network `student-5-data`. |
| `5110`    | `shared-backend`                 | Canvas gateway. CORS is locked down; only other backends call it. |
| `1025`    | `mailhog` (SMTP)                 | Local mock SMTP server for password reset emails. |
| `8025`    | `mailhog` (Web UI)               | Web inbox to view test emails dispatched by authentication service. |

`ai-mode` is internal-only (no host port). It fronts OpenRouter and is
the only service that needs the OpenRouter key. See
[`docs/architecture/overview.md`](docs/architecture/overview.md) for the full service table.

## Project layout

```
.
├── ai-services/         # ai-mode gateway (OpenRouter proxy)
├── shared/
│   ├── backend/         # Canvas API integration + SQLite audit log
│   ├── frontend/        # dashboard shell + nginx reverse proxy
│   └── ui-kit/          # @better-canvas/ui-kit workspace package
│                        #   (tokens, fonts, shared Vue components)
├── student-1/           # Notifications vertical slice (Bryan Lee)
│   ├── backend/         # ASP.NET Core API + EF Core PostgreSQL
│   ├── database/        # Dedicated PostgreSQL 16 container
│   └── frontend/        # Vue 3 + TypeScript + Playwright e2e
├── student-2/           # Automations vertical slice (Isaac Thomas)
│   ├── backend/         # ASP.NET Core API + EF Core SQLite + periodic runner
│   └── frontend/        # Vue 3 + TypeScript
├── student-3/           # Deadlines & Tasks vertical slice (Jonathon Thomson)
│   ├── backend/         # ASP.NET Core public API & orchestration
│   ├── database/        # Internal EF Core/SQLite persistence service
│   ├── contracts/       # Internal HTTP persistence contracts
│   └── frontend/        # Vue 3 + TypeScript
├── student-4/           # Account & Auth vertical slice (Tristan Huang)
│   ├── backend/         # ASP.NET Core Account API
│   ├── authentication/  # ASP.NET Core Auth & Password Reset API
│   ├── database/        # Internal EF Core/SQLite persistence service
│   ├── contracts/       # Internal HTTP persistence contracts
│   └── frontend/        # Vue 3 + TypeScript
├── student-5/           # Grades & Progress vertical slice (William Hannah)
│   ├── backend/         # ASP.NET Core GradesManager API
│   ├── database/        # Internal EF Core/SQLite persistence service
│   ├── contracts/       # Internal HTTP persistence contracts
│   └── frontend/        # Vue 3 + TypeScript
├── docs/                # Full architecture, runbooks, and playbooks
├── .github/workflows/   # CI workflows per service group
├── docker-compose.yml
└── .env.example
```

## Service communication

Microservices communicate over HTTP and own strictly isolated databases.
They must not query another service's database directly. The shared backend
owns Canvas authentication and API pagination.

- **Student 1**: NotificationService connects directly to its dedicated PostgreSQL container `student-1-database`.
- **Student 2**: Automations backend stores data in its dedicated SQLite database `student-2-db`.
- **Student 3**: Deadlines backend delegates all persistence to `student-3-database` over an internal private network (`student-3-data`).
- **Student 4**: Account and Authentication backends delegate persistence to `student-4-database` over an internal private network (`student-4-data`), and auth sends emails via MailHog.
- **Student 5**: Grades backend delegates persistence to `student-5-database` over an internal private network (`student-5-data`).

To import Canvas data, start the services and call:

```http
POST http://localhost:5103/api/canvas-sync
```

The sync fetches active courses and their assignments, then
transactionally upserts one task per stable Canvas assignment ID.
Removed assignments are marked inactive rather than deleted. Canvas
data remains live in the shared service; only the source IDs and
fields needed by courses/tasks are persisted by the task tracker. A
submitted or graded assignment marks its task as completed; other
Canvas submission states do not overwrite the task's local status.

The shared Canvas and task-tracker databases persist timestamps as
`DateTime` normalized to UTC.

## Running a single service outside Docker

Each backend is a standalone ASP.NET project. The easiest way to iterate is
still `docker compose up`, but a backend will run with `dotnet run` provided
you supply the env vars it needs (notably `OPENROUTER_API_KEY` for AI features,
`SharedService__BaseUrl` for Canvas calls, and `DatabaseService__BaseUrl` for
services that delegate to an internal database service).

Each frontend is a Vite + Vue 3 project. Run it with `npm run dev --workspace=<workspace-name>`
from the repository root.

## Continuous integration

Workflows under `.github/workflows/`:

- `docker-ci.yml` — Full-stack Compose contract validation, dynamic parallel image builds, and integration tests.
- `shared-ci.yml` — Shared shell and backend build, tests, format check, and audit.
- `student-1-ci.yml` — Notifications frontend typecheck/build, .NET build/tests, format check, and migration verification.
- `student-2-ci.yml` — Automations frontend typecheck/build, .NET build, format check, and migration verification.
- `student-3-ci.yml` — Deadlines frontend typecheck/build, .NET build/tests, format check, and migration verification.
- `student-4-ci.yml` — Account & Auth frontend typecheck/build, .NET build, format check, and migration verification.
- `student-5-ci.yml` — Grades frontend typecheck/build, .NET build, format check, and migration verification.

Workflows run on PRs targeting `main` and pushes to `main`.

## Release 0: Summary

Working branch: `main`  
Feature set:
- **Shared infrastructure**: Dashboard shell, `@better-canvas/ui-kit` Neobrutalism design system, Canvas LMS API gateway with caching/sanitization, OpenRouter AI mode gateway, and MailHog mock SMTP.
- **Student 1 (Notifications)**: Delivery preferences, notification management, real-time SSE streaming broker, and AI digests and chat assistant backed by PostgreSQL.
- **Student 2 (Automations)**: Assignment extension requests, scheduled Canvas posts via Canvas Conversations, AI quiz filling via `ai-mode`, and execution run history.
- **Student 3 (Deadlines & Tasks)**: Deadline/task CRUD, course linkage, filtering, Canvas synchronization, AI subtask breakdown, internal database microservice, and real-time push notification integration.
- **Student 4 (Account & Authentication)**: Account management, secure password hashing, AI profile summaries, authentication API, password reset workflow via MailHog, and internal database microservice.
- **Student 5 (Grades & Progress)**: Grades aggregation, what-if marks simulator, marks update endpoints, and internal database microservice.
