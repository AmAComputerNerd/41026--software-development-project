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
                         └─┬─────────┬─────────┬─────────┬─────────┬─┘
                           │         │         │         │         │
          ┌────────────────┘         │         │         │         └────────────────┐
          ▼                          ▼         ▼         ▼                          ▼
┌───────────────────┐      ┌───────────────┐ ┌───────────────────┐      ┌───────────────────┐
│student-1-frontend │      │student-2-front│ │student-3-frontend │      │student-5-frontend │
│(Notifications UI) │      │(Automations)  │ │(Deadlines & Tasks)│      │(Grades & Progress)│
└─────────┬─────────┘      └───────┬───────┘ └─────────┬─────────┘      └─────────┬─────────┘
          │                        │                   │                          │
          │ /api/notifications/*   │ /api/automations/*│ /api/deadlines/*         │ /api/grades/*
          ▼                        ▼                   ▼                          ▼
┌───────────────────┐      ┌───────────────┐ ┌───────────────────┐      ┌───────────────────┐
│ student-1-backend │      │student-2-back │ │ student-3-backend │      │ student-5-backend │
│ (Port 5101)       │◄─────┤(Port 5102)    │ │ (Port 5103)       │      │ (Port 5105)       │
│ [PostgreSQL 16]   │ push │[SQLite]       │ │ [Public API]      │      │ [Public API]      │
└─────────┬─────────┘      └───────┬───────┘ └─────────┬─────────┘      └─────────┬─────────┘
          │                        │                   │ HTTP (private net)       │ HTTP (private net)
          │                        │                   ▼                          ▼
          │                        │         ┌───────────────────┐      ┌───────────────────┐
          │                        │         │student-3-database │      │student-5-database │
          │                        │         │[EF Core: app.db]  │      │[EF Core: grades.db│
          │                        │         └─────────┬─────────┘      └───────────────────┘
          │ Chat / Digest          │ Exec / AI         │ Canvas sync
          ▼                        ▼                   ▼
┌───────────────────┐      ┌───────────────────┐ ┌───────────────────┐
│   ai-mode (8080)  │      │shared-back (5110) │ │student-4-services │
│(OpenRouter Gateway│      │(Canvas LMS Gateway│ │(Backend, Auth 5114│
│[Holds API Key]    │      │[SQLite: audit.db] │ │ DB, MailHog 8025) │
└─────────┬─────────┘      └─────────┬─────────┘ └───────────────────┘
          │                          │
          ▼                          ▼
  [ OpenRouter API ]         [ Canvas LMS API ]
```

---

## 2. Microservice Directory & Port Matrix

| Service | Directory | Stack | Host Port | Internal Docker URL | Core Responsibilities | Dependencies |
|---|---|---|---|---|---|---|
| **`shared-shell`** | `shared/frontend` | Vue 3 + Nginx | `8080` | `http://shared-shell:80` | Host entrypoint, dashboard UI, reverse proxy routing | All frontends & backends |
| **`shared-backend`** | `shared/backend` | ASP.NET Core + SQLite | `5110` | `http://shared-backend:8080` | Canvas LMS API client, HTML sanitizer, 3-min in-memory cache, audit log | Canvas LMS |
| **`ai-mode`** | `ai-services/ai-mode` | ASP.NET Core | *Internal only* | `http://ai-mode:8080` | Central OpenRouter LLM gateway, status error normalization, health checks | OpenRouter API |
| **`student-1-database`** | `student-1/database` | PostgreSQL 16 (Alpine) | `5432` | `student-1-database:5432` | Isolated PostgreSQL database (`notifications_db`) for notifications and digests | — |
| **`student-1-backend`** | `student-1/backend` | ASP.NET Core + EF Core PostgreSQL | `5101` | `http://student-1-backend:8080` | Notification management, delivery preferences, AI digests & chat, SSE stream broker | `student-1-database`, `ai-mode`, `shared-backend` |
| **`student-1-frontend`** | `student-1/frontend` | Vue 3 + TypeScript + Vite | *Proxied* | `http://student-1-frontend:80` | Notifications page, real-time toast alerts, AI digest chat panel | `student-1-backend` |
| **`student-2-backend`** | `student-2/backend` | ASP.NET Core + SQLite | `5102` | `http://student-2-backend:8080` | Assignment extensions, scheduled Canvas posts, AI quiz filling, periodic execution, run history | `shared-backend`, `ai-mode` |
| **`student-2-frontend`** | `student-2/frontend` | Vue 3 + Vite | *Proxied* | `http://student-2-frontend:80` | Automation configuration and run-history UI | `student-2-backend` |
| **`student-3-backend`** | `student-3/backend` | ASP.NET Core | `5103` | `http://student-3-backend:8080` | Public task API, Canvas/AI orchestration, due-soon reminder worker | `student-3-database`, `shared-backend`, `ai-mode`, `student-1-backend` |
| **`student-3-database`** | `student-3/database` | ASP.NET Core + EF Core SQLite | *Internal only* (`5203` standalone) | `http://student-3-database:8080` | Student 3 persistence API, migrations, seeding, transactional task operations | — |
| **`student-3-frontend`** | `student-3/frontend` | Vue 3 + Vite | *Proxied* | `http://student-3-frontend:80` | Task manager, calendar view, upcoming task view, AI breakdown modal | `student-3-backend` |
| **`student-4-backend`** | `student-4/backend` | ASP.NET Core | `5104` | `http://student-4-backend:8080` | Account management API, user roles, profile AI summaries | `student-4-database`, `ai-mode` |
| **`student-4-authentication`** | `student-4/authentication` | ASP.NET Core | `5114` | `http://student-4-authentication:8080` | User auth, login, registration, password reset token emails via MailHog | `student-4-database`, `mailhog` |
| **`student-4-database`** | `student-4/database` | ASP.NET Core + EF Core SQLite | *Internal only* (`5204` standalone) | `http://student-4-database:8080` | Student 4 persistence API, user and authentication records | — |
| **`student-4-frontend`** | `student-4/frontend` | Vue 3 + Vite | *Proxied* | `http://student-4-frontend:80` | Account management UI, profile editing, password reset flow | `student-4-backend`, `student-4-authentication` |
| **`student-5-backend`** | `student-5/backend` | ASP.NET Core | `5105` | `http://student-5-backend:8080` | Grades & progress calculation, what-if marks endpoints | `student-5-database`, `ai-mode` |
| **`student-5-database`** | `student-5/database` | ASP.NET Core + EF Core SQLite | *Internal only* (`5205` standalone) | `http://student-5-database:8080` | Student 5 persistence API, migrations, and grade records | — |
| **`student-5-frontend`** | `student-5/frontend` | Vue 3 + Vite | *Proxied* | `http://student-5-frontend:80` | Grades list, grade breakdown, what-if simulator | `student-5-backend` |
| **`mailhog`** | External Docker image | Go (`mailhog/mailhog:latest`) | `1025` (SMTP), `8025` (UI) | `mailhog:1025` | Mock SMTP server and developer web mailbox UI | — |

---

## 3. Reverse Proxy Routing (shared-shell)

The Nginx server running inside `shared-shell` is the sole entry point exposed on port `8080`. It routes requests dynamically:

- `/` → Serves the dashboard shell (`shared/frontend/dist`).
- `/notifications/` → Proxies to `http://student-1-frontend:80/`.
- `/api/notifications/` → Proxies to `http://student-1-backend:8080/`.
- `/automations/` → Proxies to `http://student-2-frontend:80/`.
- `/api/automations/` → Proxies to `http://student-2-backend:8080/api/`.
- `/deadlines/` → Proxies to `http://student-3-frontend:80/`.
- `/api/deadlines/` → Proxies to `http://student-3-backend:8080/api/`.
- `/account/` → Proxies to `http://student-4-frontend:80/`.
- `/api/auth/` → Proxies to `http://student-4-authentication:8080/`.
- `/api/users/`, `/api/students/`, `/api/teachers/` → Proxies to `http://student-4-backend:8080/`.
- `/grades/` → Proxies to `http://student-5-frontend:80/`.
- `/api/grades/` → Proxies to `http://student-5-backend:8080/`.

---

## 4. Cross-Service Boundaries & Communication Rules

### Database Isolation
Each bounded context maintains its own strictly isolated persistence mechanism:
- **`student-1`**: Runs a dedicated PostgreSQL 16 container (`student-1-database`) mounting named volume `student-1-postgres-data`.
- **`student-2`**: Runs SQLite with persistent volume `student-2-db`.
- **`student-3`**: Dedicated internal database service `student-3-database` exclusively mounts `student-3-db` on the private `student-3-data` network.
- **`student-4`**: Dedicated internal database service `student-4-database` exclusively mounts `student-4-db` on the private `student-4-data` network.
- **`student-5`**: Dedicated internal database service `student-5-database` exclusively mounts `student-5-db` on the private `student-5-data` network.
- **`shared-backend`**: Dedicated SQLite audit log mounting `shared-db`.

No service reads or writes another service's database directly; all cross-service communication occurs over HTTP.

### Canvas Data Boundary
- `shared-backend` holds the `CANVAS_BASE_URL` and `CANVAS_API_TOKEN`.
- Canvas assignment descriptions are converted from raw, untrusted HTML into clean plain text before returning DTOs to caller backends.
- Course, assignment, and user responses are cached in-memory with a 3-minute TTL to minimize redundant Canvas requests.

### Centralized AI Mode Boundary
- `ai-services/ai-mode` is the only service that reads `OPENROUTER_API_KEY`.
- Downstream services send chat completion requests to `http://ai-mode:8080/v1/chat/completions`.
- Standard model: `minimax/minimax-m3:free`.

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

### D. Automated Canvas Actions & AI Quiz Filling
- `student-2-backend` executes scheduled posts to Canvas Conversations when due.
- Automatically answers and saves draft attempts on Classic Quizzes via `ai-mode` and Canvas Quiz Submission Questions API without submitting them automatically.

### E. Account Management & Email Verification
- `student-4-authentication` and `student-4-backend` handle account creation, hashed credentials, and password resets via MailHog SMTP.

---

## 6. Shared Design System: `@better-canvas/ui-kit`

All frontends share the workspace package `@better-canvas/ui-kit` (`shared/ui-kit`):
- **Neobrutalism Aesthetics**:
  - Border radius: `0px` (strict sharp corners).
  - Borders: `4px solid var(--border-color)` (high-contrast ink borders).
  - Shadows: `4px 4px 0 var(--shadow-color)` (hard unblurred drop shadows).
  - Palette: High-contrast parchment surface, hazard yellow accents, retro status indicators.
- **Animations & Micro-interactions**:
  - Standard duration and easing tokens (`--anim-duration-base`, `--anim-ease-out`).
  - Subtle spring transforms on hover and active states (`transform: translate(-2px, -2px)` with expanded shadow).

