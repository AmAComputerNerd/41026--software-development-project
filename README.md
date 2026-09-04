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

**Student 4: Tristan Huang (STUDENT-NUM).**  
Working directory: `student-4/`  
TODO: Short summary of microservice, other info

**Student 5: William Hannah (25494675).**  
Working directory: `student-5/`  
Grades and progress service: aggregates Canvas grade data and renders
progress views. Backend listens on host port `5105`; frontend is
proxied through the shared shell at `/grades/`.

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

To stop: `docker compose down`. To wipe the SQLite volumes (forces a
clean re-seed on next start): `docker compose down -v`.

## What runs where

Host ports are set in `docker-compose.yml`. Backends are exposed
directly so you can hit them with `curl` or a REST client; frontends
sit behind the shared shell and aren't reachable on their own host
port.

| Host port | Service                          | Notes |
|----------:|----------------------------------|-------|
| `8080`    | `shared-shell` (nginx)           | Dashboard entry point. Proxies `/notifications`, `/deadlines`, `/grades`, and `/api/*` to the right microservice. |
| `5101`    | `student-1-backend`              | Notifications API. |
| `5103`    | `student-3-backend`              | Deadlines & tasks API. |
| `5105`    | `student-5-backend`              | Grades & progress API. |
| `5110`    | `shared-backend`                 | Canvas gateway. CORS is locked down; only other backends call it. |

`ai-mode` is internal-only (no host port). It fronts OpenRouter and is
the only service that needs the OpenRouter key. See
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
│   ├── backend/         # ASP.NET Core + SQLite
│   └── frontend/        # Vue 3 + plain SCSS
├── docs/
│   ├── architecture/overview.md
│   └── playbooks/new-frontend-microservice.md
├── .github/workflows/   # one CI workflow per service group
├── docker-compose.yml
└── .env.example
```

## Service communication

Microservices communicate over HTTP and own separate SQLite databases.
They must not query another service's Entity Framework database. The
shared backend owns Canvas authentication and API pagination. The
deadline and task-tracker backend receives `SharedService:BaseUrl`
through standard ASP.NET configuration. Docker Compose supplies
`http://shared-backend:8080`, where `shared-backend` is resolved by
Compose's internal DNS. Cross-service integration is intentionally
available only through Docker Compose; standalone services do not
receive addresses for other services.

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

Each backend's `Api/` directory is a standalone ASP.NET project. The
easiest way to iterate is still `docker compose up`, but a backend
will run with `dotnet run` from its own `Api/` directory provided
you supply the env vars it needs (notably `OPENROUTER_API_KEY` for AI
features and `SharedService__BaseUrl` for any service that calls into
the Canvas gateway). See `docs/architecture/overview.md` for the
per-service env-var reference.

Each frontend is a Vite + Vue 3 project. Run it with `npm run dev`
from the frontend's directory; it expects a backend reachable on the
URL baked in at build time (set via `VITE_*_API_BASE_URL` in the
frontend's Dockerfile). In Docker the gateway is `nginx`, outside
Docker you'll be hitting `localhost:<backend-port>` directly.

## Continuous integration

One workflow per service group under `.github/workflows/`:

- `docker-ci.yml` — Docker image build for the full stack (fires on
  any change under `ai-services/`, `shared/`, `student-*/`,
  `docker-compose.yml`, or the root `package.json`).
- `shared-ci.yml` — frontend type-check + build, .NET build + format
  check, EF migrations drift check, NuGet vulnerability scan.
- `student-1-ci.yml`, `student-3-ci.yml`, `student-5-ci.yml` — same
  shape as `shared-ci.yml`, scoped to that student's slice.

Workflows run on PRs into `main` and on pushes to `main`. Path filters
keep each workflow scoped to the directories it owns, so unrelated
changes don't trigger unrelated builds.

## Release 0: Summary

Working branch: `main`  
Feature set:
- Shared dashboard shell and UI kit.
- Notification preferences, notification management, and AI digests.
- Assignment extension configuration, scheduled Canvas posts, AI quiz filling, and automation run history.
- Deadline/task CRUD, course linkage, filtering, and Canvas synchronization.
- Shared Canvas API gateway, audit database, Docker image, and CI workflow.
- Grades & progress slice (student 5) — backend API and frontend
  shell, integrated into the shared dashboard.

Heading into Release 1: student 4's Account slice is the next planned
service.
