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
and AI-generated digests summarising a student's recent notification
activity.

**Student 2: Isaac Thomas (25341708).**

Working directory: `student-2/`

Automations service: configures assignment extension, scheduled post, and quiz
filler automations, stores each type in its own Entity Framework table, and
provides read-only records of previous runs. A periodic worker executes due
scheduled posts and fills eligible Canvas quizzes through the shared backend's
Canvas gateway, and uses durable execution keys to prevent duplicate runs.

**Student 3: Jonathon Thomson (25488154).**  
Working directory: `student-3/`  
Deadline and task-tracker service: manages courses and coursework tasks,
including priorities, completion states, due dates, subtasks, filtering, and
Canvas assignment imports through the shared backend. Canvas sync keeps one
primary task per assignment and updates it on later imports without storing a
separate assessment table.

**Student 4: Tristan Huang (25322025).**  
Working directory: `student-4/`  
Account service: Account management and storage. Saves account details and 
allows the creation and editing of account. Saves passwords securely with a hash,
and can generate helpful AI Summaries for an account (to be expanded in future 
releases). Additionally, a 'forgot password' prompt which allows users to receive 
an email to change password.

**Student 5: William Hannah (25494675).**  
Working directory: `student-5/`  
Grades and progress service: aggregates course, assignment, and mark
data through its own private database service, renders progress views,
and simulates what-if marks. Backend listens on host port `5105`;
frontend is proxied through the shared shell at `/grades/`.

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
   docker compose up
   ```

3. Open the dashboard at <http://localhost:8080>.

To stop: `docker compose down`. To wipe the database volumes (forces a
clean re-seed on next start): `docker compose down -v`.

## What runs where

Host ports are set in `docker-compose.yml`. Backends are exposed
directly so you can hit them with `curl` or a REST client; frontends
sit behind the shared shell and aren't reachable on their own host
port.

| Host port | Service                          | Notes |
|----------:|----------------------------------|-------|
| `8080`    | `shared-shell` (nginx)           | Dashboard entry point. Proxies `/notifications`, `/automations`, `/deadlines`, `/account`, `/grades`, and `/api/*` to the right microservice. |
| `5101`    | `student-1-backend`              | Notifications API. |
| `5432`    | `student-1-database`             | PostgreSQL 16 container owning `notifications_db`. |
| `5102`    | `student-2-backend`              | Automations API. |
| `5103`    | `student-3-backend`              | Deadlines & tasks API. |
| `5203`    | `student-3-database` (standalone)| Internal Student 3 persistence API; not host-published by Docker Compose. |
| `5104`    | `student-4-backend`              | Account/profile API. |
| `5114`    | `student-4-authentication`       | Login, password management, and password reset API. |
| `5105`    | `student-5-backend`              | Grades & progress API. |
| `5110`    | `shared-backend`                 | Canvas gateway. CORS is locked down; only other backends call it. |
| `1025` / `8025` | `mailhog`                  | Development SMTP sink and web inbox for Student 4 password-reset emails. |

`ai-mode` is internal-only (no host port). It fronts OpenRouter and is
the only service that needs the OpenRouter key. The Student 4 and
Student 5 persistence services are internal-only too. See
`docs/architecture/overview.md` for the full service table.

## Project layout

```
.
├── ai-services/         # ai-mode gateway (OpenRouter proxy)
├── shared/
│   ├── backend/         # Canvas API integration
│   ├── frontend/        # dashboard shell + nginx reverse proxy
│   └── ui-kit/          # @better-canvas/ui-kit workspace package
│                        #   (tokens, fonts, shared Vue components)
├── student-N/           # one slice per student
│   ├── backend/         # ASP.NET Core public API
│   ├── authentication/  # Student 4 login / password-reset API
│   ├── database/        # private persistence service
│   │                    #   (PostgreSQL for student 1; internal
│   │                    #    EF Core/SQLite services for 3, 4, 5)
│   ├── contracts/       # internal HTTP contracts (students 3, 4, 5)
│   └── frontend/        # Vue 3 + plain SCSS
├── docs/
│   ├── architecture/overview.md
│   └── playbooks/new-frontend-microservice.md
├── .github/workflows/   # one CI workflow per service group
├── docker-compose.yml
└── .env.example
```

## Service communication

Microservices communicate over HTTP and own separate databases. They
must not query another service's Entity Framework database. The
shared backend owns Canvas authentication and API pagination. The
deadline and task-tracker backend receives `SharedService:BaseUrl`
and `DatabaseService:BaseUrl` through standard ASP.NET configuration.
Its EF Core context and SQLite volume are exclusively owned by the
internal `student-3-database` service; students 4 and 5 follow the same
split. Docker Compose supplies `http://shared-backend:8080` and
`http://student-3-database:8080`, resolved through Compose's internal
DNS. Each database service is isolated on a private network shared only
with the public services of its own slice; notification service
availability does not block the Student 3 API from starting.

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

Each backend is a standalone ASP.NET project (`Api/` for most slices,
`backend/GradesManager/GradesManager/` for student 5). The easiest way
to iterate is still `docker compose up`, but a backend will run with
`dotnet run` from its own project directory provided you supply the env
vars it needs (notably `OPENROUTER_API_KEY` for AI features,
`SharedService__BaseUrl` for any service that calls into the Canvas
gateway, and `DatabaseService__BaseUrl` for students 3, 4, and 5). See
`docs/architecture/overview.md` for the per-service env-var reference.

Each frontend is a Vite + Vue 3 project. Run it with `npm run dev`
from the frontend's directory; it expects a backend reachable on the
URL baked in at build time (set via `VITE_*_API_BASE_URL` in the
frontend's Dockerfile). In Docker the gateway is `nginx`, outside
Docker you'll be hitting `localhost:<backend-port>` directly.

## Continuous integration

One workflow per service group under `.github/workflows/`:

- `docker-ci.yml` — Docker image build for the full stack (fires on
  any change under `ai-services/`, `shared/`, `student-*/`,
  `docker-compose.yml`, the root `package.json`/`package-lock.json`, or
  `.env.example`).
- `shared-ci.yml` — frontend type-check + build, .NET build + format
  check, EF migrations drift check, NuGet vulnerability scan.
- `student-1-ci.yml` through `student-5-ci.yml` — same shape as
  `shared-ci.yml`, scoped to that student's slice. `student-1-ci.yml`
  additionally runs the .NET test suite and the Playwright E2E tests;
  `student-3-ci.yml` and `student-5-ci.yml` add API contract smoke tests.

Workflows run on PRs into `main` and on pushes to `main`. Path filters
keep each workflow scoped to the directories it owns, so unrelated
changes don't trigger unrelated builds.

## Release 0: Summary

Working branch: `main`  
Feature set:
- Shared dashboard shell and UI kit.
- Notification preferences, notification management, AI digests, and a
  dedicated PostgreSQL database microservice.
- Assignment extension configuration, scheduled Canvas posts, AI quiz filling, and automation run history.
- Deadline/task CRUD, course linkage, filtering, and Canvas synchronization.
- Shared Canvas API gateway, audit database, Docker image, and CI workflow.
- Account creation, management, authentication, password reset, and AI
  summary of account.
- Grades & progress slice (student 5) — backend API, private database
  service, and frontend, integrated into the shared dashboard.

All five slices are wired into the shared shell. Release 1 work builds on
this baseline.
