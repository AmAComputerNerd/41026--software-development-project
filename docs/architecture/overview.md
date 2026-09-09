# Architecture Overview

Current architectural state of the **41026 Advanced Software Development Project**. This document serves as the authoritative source material for technical reports, architecture diagrams, and onboarding for developers and AI agents.

---

## 1. System Topology & Microservices

The application is structured as a decentralized microservices architecture composed of independent vertical slices running inside a Docker network behind an Nginx reverse proxy shell (`shared-shell`).

```
                                      [ Client Browser ]
                                               │
                                      (HTTP: Port 8080)
                                               ▼
                         ┌───────────────────────────────────────────┐
                         │           shared-shell (Nginx)            │
                         │   • /              -> Dashboard (Vue 3)   │
                         │   • /notifications -> student-1-frontend  │
                         │   • /automations   -> student-2-frontend  │
                         │   • /deadlines     -> student-3-frontend  │
                         │   • /account       -> student-4-frontend  │
                         │   • /grades        -> student-5-frontend  │
                         │   • /api/*         -> Proxied to backends │
                         └─────────────────┬───┬───┬─────────────────┘
                                           │   │   │
                  ┌────────────────────────┘   │   └────────────────────────┐
                  ▼                            ▼                            ▼
      ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
      │  student-1-frontend   │   │  student-3-frontend   │   │  student-5-frontend   │
      │  (Notifications UI)   │   │  (Deadlines & Tasks)  │   │  (Grades & Progress)  │
      └───────────┬───────────┘   └───────────┬───────────┘   └───────────┬───────────┘
                  │ /api/notifications/*      │ /api/deadlines/*          │ /api/grades/*
                  ▼                           ▼                           ▼
      ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
      │   student-1-backend   │   │   student-3-backend   │   │   student-5-backend   │
      │      (Port 5101)      │   │      (Port 5103)      │   │      (Port 5105)      │
      │     [Public API]      │   │     [Public API]      │   │     [Public API]      │
      └───────────┬───────────┘   └───────────┬───────────┘   └───────────┬───────────┘
                  │ HTTP                      │ HTTP                      │ HTTP
                  ▼                           ▼                           ▼
      ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
      │  student-1-database   │   │  student-3-database   │   │  student-5-database   │
      │    [PostgreSQL 16]    │   │   [EF Core: app.db]   │   │   [EF Core: app.db]   │
      └───────────────────────┘   └───────────────────────┘   └───────────────────────┘

  student-3-backend also pushes due-soon reminders to student-1-backend
  (POST /notifications/push). Every AI-enabled backend calls ai-mode, and
  every Canvas-consuming backend calls shared-backend:

      ┌───────────────────────┐             ┌───────────────────────┐
      │     ai-mode (8080)    │             │  shared-backend(5110) │
      │  (OpenRouter Gateway) │             │  (Canvas LMS Gateway) │
      │  [Holds API Key]      │             │  [SQLite: shared.db]  │
      └───────────┬───────────┘             └───────────┬───────────┘
                  ▼                                     ▼
          [ OpenRouter API ]                    [ Canvas LMS API ]
```

---

## 2. Microservice Directory & Port Matrix

| Service | Directory | Stack | Host Port | Internal Docker URL | Core Responsibilities | Dependencies |
|---|---|---|---|---|---|---|
| **`shared-shell`** | `shared/frontend` | Vue 3 + Nginx | `8080` | `http://shared-shell:80` | Host entrypoint, dashboard UI, reverse proxy routing | All frontends & backends |
| **`shared-backend`** | `shared/backend` | ASP.NET Core + SQLite | `5110` | `http://shared-backend:8080` | Canvas LMS API client, HTML sanitizer, 3-min in-memory cache, audit log | Canvas LMS |
| **`ai-mode`** | `ai-services/ai-mode` | ASP.NET Core | *Internal only* | `http://ai-mode:8080` | Central OpenRouter LLM gateway, status error normalization, health checks | OpenRouter API |
| **`student-1-backend`** | `student-1/backend` | ASP.NET Core + EF Core (Npgsql) | `5101` | `http://student-1-backend:8080` | Notification management, delivery preferences, AI digests & chat, SSE stream broker | `student-1-database`, `ai-mode`, `shared-backend` |
| **`student-1-database`** | `student-1/database` | PostgreSQL 16 Alpine | `5432` | `student-1-database:5432` | Student 1 persistence (`notifications_db`), `init.sql` seeding | — |
| **`student-1-frontend`** | `student-1/frontend` | Vue 3 + Vite | *Proxied* | `http://student-1-frontend:80` | Notifications page, real-time toast alerts, AI digest chat panel | `student-1-backend` |
| **`student-2-backend`** | `student-2/backend` | ASP.NET Core + SQLite | `5102` | `http://student-2-backend:8080` | Assignment extensions, scheduled Canvas posts, AI quiz filling, periodic execution, run history | `shared-backend`, `ai-mode` |
| **`student-2-frontend`** | `student-2/frontend` | Vue 3 + Vite | *Proxied* | `http://student-2-frontend:80` | Automation configuration and run-history UI | `student-2-backend` |
| **`student-3-backend`** | `student-3/backend` | ASP.NET Core | `5103` | `http://student-3-backend:8080` | Public task API, Canvas/AI orchestration, due-soon reminder worker | `student-3-database`, `shared-backend`, `ai-mode`, `student-1-backend` |
| **`student-3-database`** | `student-3/database` | ASP.NET Core + EF Core SQLite | *Internal only* (`5203` standalone) | `http://student-3-database:8080` | Student 3 persistence API, migrations, seeding, transactional task operations | — |
| **`student-3-frontend`** | `student-3/frontend` | Vue 3 + Vite | *Proxied* | `http://student-3-frontend:80` | Task manager, calendar view, upcoming task view, AI breakdown modal | `student-3-backend` |
| **`student-4-backend`** | `student-4/backend` | ASP.NET Core | `5104` | `http://student-4-backend:8080` | User/student/teacher profile API, AI profile summaries | `student-4-database`, `ai-mode` |
| **`student-4-authentication`** | `student-4/authentication` | ASP.NET Core | `5114` | `http://student-4-authentication:8080` | Login, password/account management, SMTP password reset | `student-4-database`, `mailhog` |
| **`student-4-database`** | `student-4/database` | ASP.NET Core + EF Core SQLite | *Internal only* | `http://student-4-database:8080` | Student 4 persistence API and migrations | — |
| **`student-4-frontend`** | `student-4/frontend` | Vue 3 + Vite | *Proxied* | `http://student-4-frontend:80` | Login, profile, and password-reset UI | `student-4-backend`, `student-4-authentication` |
| **`student-5-backend`** | `student-5/backend` | ASP.NET Core | `5105` | `http://student-5-backend:8080` | Grades & progress calculation, what-if marks endpoints, AI recommendations | `student-5-database`, `ai-mode` |
| **`student-5-database`** | `student-5/database` | ASP.NET Core + EF Core SQLite | *Internal only* | `http://student-5-database:8080` | Student 5 persistence API and migrations | — |
| **`student-5-frontend`** | `student-5/frontend` | Vue 3 + Vite | *Proxied* | `http://student-5-frontend:80` | Grades list, grade breakdown, what-if simulator | `student-5-backend` |
| **`mailhog`** | *(image `mailhog/mailhog`)* | Go SMTP test server | `1025` (SMTP), `8025` (UI) | `mailhog:1025` | Development inbox for Student 4 password-reset emails | — |

Canvas assignment descriptions are treated as untrusted HTML. The shared
backend parses them into compact plain text before returning its API DTOs, so
downstream services neither render raw Canvas markup nor send it to AI models.
The automations backend also obtains messageable Canvas recipient IDs and names
through shared-backend; only shared-backend receives the Canvas API token.
Its periodic executor asks each type-specific executor for zero or more due
execution candidates and durably claims each candidate before external work.
Candidate keys derive from their execution parameters; scheduled posts hash the
scheduled time and complete Canvas message configuration, while quiz filling
keys on the quiz ID so each quiz is attempted at most once. This prevents
duplicate sends across timer overlap or replicas while allowing edited or
multi-target automations to produce new logical executions.

---

## 3. Reverse Proxy Routing (shared-shell)

The Nginx server running inside `shared-shell` is the sole entry point exposed on port `8080`. It routes requests dynamically:

- `/` → Serves the dashboard shell (`shared/frontend/dist`).
- `/notifications/` → Proxies to `http://student-1-frontend:80/`.
- `/api/notifications/` → Proxies to `http://student-1-backend:8080/` (120s read timeout for SSE and AI calls).
- `/automations/` → Proxies to `http://student-2-frontend:80/`.
- `/api/automations/` → Proxies to `http://student-2-backend:8080/api/`.
- `/deadlines/` → Proxies to `http://student-3-frontend:80/`.
- `/api/deadlines/` → Proxies to `http://student-3-backend:8080/api/`.
- `/account/` → Proxies to `http://student-4-frontend:80/`.
- `/api/auth/` → Proxies to `http://student-4-authentication:8080`.
- `/api/users/`, `/api/students/`, `/api/teachers/` → Proxy to `http://student-4-backend:8080`.
- `/grades/` → Proxies to `http://student-5-frontend:80/`.
- `/api/grades/` → Proxies to `http://student-5-backend:8080/`.

Bare paths (`/automations`, `/deadlines`, `/account`, `/grades`) redirect
permanently to their trailing-slash form.

---

## 4. Cross-Service Boundaries & Communication Rules

### Database Isolation
Each persistence-owning service uses its own isolated database in a persistent
Docker volume: EF Core over SQLite for `shared-backend` (`shared-db`),
`student-2-backend` (`student-2-db`), `student-3-database` (`student-3-db`),
`student-4-database` (`student-4-db`), and `student-5-database`
(`student-5-db`), plus a dedicated PostgreSQL container for Student 1
(`student-1-postgres-data`).
- No service reads or writes another service's database file.
- Direct database connection strings point only to the local service database.
- `student-3-database`, `student-4-database`, and `student-5-database`
  exclusively mount their volumes; the matching public APIs use internal HTTP
  contracts and contain no EF Core dependency.
- Each of those database services is attached only to its internal
  `student-N-data` Docker network. The matching public services (the backend,
  plus `student-4-authentication` for Student 4) join both that private network
  and the default application network; no other service can directly reach a
  private persistence API.

### Canvas Data Boundary
- `shared-backend` holds the `CANVAS_BASE_URL` and `CANVAS_API_TOKEN`.
- Canvas assignment descriptions are converted from raw, untrusted HTML into clean plain text before returning DTOs to caller backends.
- Course, assignment, and user responses are cached in-memory with a 3-minute TTL to minimize redundant Canvas requests.

### Centralized AI Mode Boundary
- `ai-services/ai-mode` is the only service that reads `OPENROUTER_API_KEY`.
- Downstream services send chat completion requests to `http://ai-mode:8080/v1/chat/completions`.
- Standard model: `nvidia/nemotron-3.5-lightning:free` (override per request with `model`, or gateway-wide with `OPENROUTER_MODEL`).

---

## 5. Key System Capabilities

### A. Real-Time SSE Notification Streaming
- `student-1-backend` exposes `GET /notifications/stream` emitting Server-Sent Events (`text/event-stream`).
- An in-memory broker (`NotificationStreamBroker`) publishes events whenever Canvas sync detects updates or external services push reminders.
- `shared-shell` and `student-1-frontend` maintain persistent SSE connections to update badge counts and display toast alerts without polling.

### B. Cross-Microservice Action Triggers
Notifications carry structured action metadata enabling cross-service operations directly from the notifications interface:
- **`AI BREAK DOWN`**: Triggers a modal calling `POST /api/deadlines/tasks/{id}/ai-breakdown` on `student-3-backend` to generate actionable subtasks with AI.
- **`GRADE IMPACT`**: Triggers a modal calling `PUT /api/grades/api/assignment/marks/` on `student-5-backend` to simulate grade changes.
- **`MARK COMPLETE`**: Calls `PUT /api/deadlines/tasks/{id}` on `student-3-backend` to mark a task done inline.

### C. Conversational AI Digest Assistant
- `student-1-backend` provides `POST /digest/chat` backed by `OpenRouterDigestService`.
- The assistant is dynamically grounded with the student's unread notifications and course context, allowing interactive multi-turn questions ("What deadlines do I have this week?", "Explain the feedback on Assignment 1").

---

## 6. Shared Design System: `@better-canvas/ui-kit`

All frontends share the workspace package `@better-canvas/ui-kit` (`shared/ui-kit`):
- **Neobrutalism Aesthetics**:
  - Border radius: `--nb-border-radius: 0` (strict sharp corners).
  - Borders: `var(--nb-border-width-md) solid var(--nb-color-ink)` (2/3/4px `sm`/`md`/`lg` high-contrast ink borders).
  - Shadows: `var(--nb-shadow)` — a hard, unblurred `6px 6px 0 var(--nb-color-shadow)` drop shadow.
  - Palette: High-contrast concrete surface (`--nb-color-bg`), safety-orange and hazard-yellow accents (`--nb-color-accent-orange`, `--nb-color-accent-yellow`), muted meta text (`--nb-color-muted`).
- **Animations & Micro-interactions**:
  - Standard duration and easing tokens (`--nb-duration-base`, `--nb-ease-out`, `--nb-ease-pop`).
  - Subtle spring transforms on hover and active states (`transform: translate(-2px, -2px)` with expanded shadow).
