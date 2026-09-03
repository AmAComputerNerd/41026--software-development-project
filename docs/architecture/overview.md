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
                         │   • /deadlines     -> student-3-frontend  │
                         │   • /grades        -> student-5-frontend  │
                         │   • /api/*         -> Proxied to backends │
                         └─────────────────┬───┬───┬─────────────────┘
                                           │   │   │
                  ┌────────────────────────┘   │   └────────────────────────┐
                  ▼                            ▼                            ▼
      ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
      │   student-1-frontend  │   │   student-3-frontend  │   │   student-5-frontend  │
      │   (Notifications UI)  │   │   (Deadlines & Tasks) │   │   (Grades & Progress) │
      └───────────┬───────────┘   └───────────┬───────────┘   └───────────┬───────────┘
                  │                           │                           │
                  │ /api/notifications/*      │ /api/deadlines/*          │ /api/grades/*
                  ▼                           ▼                           ▼
      ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
      │   student-1-backend   │   │   student-3-backend   │   │   student-5-backend   │
      │   (Port 5101)         │◄──┤   (Port 5103)         │   │   (Port 5105)         │
      │   [SQLite: notifs.db] │push│   [SQLite: tasks.db]  │   │   [SQLite: grades.db] │
      └───────────┬───────────┘   └───────────┬───────────┘   └───────────────────────┘
                  │                           │
                  │ Chat / Digest             │ Canvas sync
                  ▼                           ▼
      ┌───────────────────────┐   ┌───────────────────────┐
      │     ai-mode (8080)    │   │  shared-backend(5110) │
      │  (OpenRouter Gateway) │   │  (Canvas LMS Gateway) │
      │  [Holds API Key]      │   │  [SQLite: audit.db]   │
      └───────────┬───────────┘   └───────────┬───────────┘
                  │                           │
                  ▼                           ▼
          [ OpenRouter API ]          [ Canvas LMS API ]
```

---

## 2. Microservice Directory & Port Matrix

| Service | Directory | Stack | Host Port | Internal Docker URL | Core Responsibilities | Dependencies |
|---|---|---|---|---|---|---|
| **`shared-shell`** | `shared/frontend` | Vue 3 + Nginx | `8080` | `http://shared-shell:80` | Host entrypoint, dashboard UI, reverse proxy routing | All frontends & backends |
| **`shared-backend`** | `shared/backend` | ASP.NET Core + SQLite | `5110` | `http://shared-backend:8080` | Canvas LMS API client, HTML sanitizer, 3-min in-memory cache, audit log | Canvas LMS |
| **`ai-mode`** | `ai-services/ai-mode` | ASP.NET Core | *Internal only* | `http://ai-mode:8080` | Central OpenRouter LLM gateway, status error normalization, health checks | OpenRouter API |
| **`student-1-backend`** | `student-1/backend` | ASP.NET Core + SQLite | `5101` | `http://student-1-backend:8080` | Notification management, delivery preferences, AI digests & chat, SSE stream broker | `ai-mode`, `shared-backend` |
| **`student-1-frontend`** | `student-1/frontend` | Vue 3 + Vite | *Proxied* | `http://student-1-frontend:80` | Notifications page, real-time toast alerts, AI digest chat panel | `student-1-backend` |
| **`student-2-backend`** | `student-2/backend` | ASP.NET Core + SQLite | `5102` | `http://student-2-backend:8080` | Automations service (placeholder/stub) | — |
| **`student-2-frontend`** | `student-2/frontend` | Vue 3 + Vite | *Proxied* | `http://student-2-frontend:80` | Automations UI (placeholder/stub) | `student-2-backend` |
| **`student-3-backend`** | `student-3/backend` | ASP.NET Core + SQLite | `5103` | `http://student-3-backend:8080` | Task tracker, course linkage, Canvas sync, due-soon reminder worker, AI subtask planning | `shared-backend`, `ai-mode`, `student-1-backend` |
| **`student-3-frontend`** | `student-3/frontend` | Vue 3 + Vite | *Proxied* | `http://student-3-frontend:80` | Task manager, calendar view, upcoming task view, AI breakdown modal | `student-3-backend` |
| **`student-4-backend`** | `student-4/backend` | ASP.NET Core + SQLite | `5104` | `http://student-4-backend:8080` | Account/profile service (placeholder/stub) | — |
| **`student-4-frontend`** | `student-4/frontend` | Vue 3 + Vite | *Proxied* | `http://student-4-frontend:80` | Account UI (placeholder/stub) | `student-4-backend` |
| **`student-5-backend`** | `student-5/backend` | ASP.NET Core + SQLite | `5105` | `http://student-5-backend:8080` | Grades & progress calculation, what-if marks endpoints | — |
| **`student-5-frontend`** | `student-5/frontend` | Vue 3 + Vite | *Proxied* | `http://student-5-frontend:80` | Grades list, grade breakdown, what-if simulator | `student-5-backend` |

---

## 3. Reverse Proxy Routing (shared-shell)

The Nginx server running inside `shared-shell` is the sole entry point exposed on port `8080`. It routes requests dynamically:

- `/` → Serves the dashboard shell (`shared/frontend/dist`).
- `/notifications/` → Proxies to `http://student-1-frontend:80/`.
- `/api/notifications/` → Proxies to `http://student-1-backend:8080/`.
- `/deadlines/` → Proxies to `http://student-3-frontend:80/`.
- `/api/deadlines/` → Proxies to `http://student-3-backend:8080/`.
- `/grades/` → Proxies to `http://student-5-frontend:80/`.
- `/api/grades/` → Proxies to `http://student-5-backend:8080/`.
- `/automations/`, `/account/` → Stubs present in `nginx.conf`, waiting for students 2 and 4.

---

## 4. Cross-Service Boundaries & Communication Rules

### Database Isolation
Each microservice uses Entity Framework Core mapped to its own isolated SQLite file in a persistent Docker volume (`student1-data`, `student3-data`, `student5-data`, `shared-data`).
- No service reads or writes another service's database file.
- Direct database connection strings point only to the local service database.

### Canvas Data Boundary
- `shared-backend` holds the `CANVAS_BASE_URL` and `CANVAS_API_TOKEN`.
- Canvas assignment descriptions are converted from raw, untrusted HTML into clean plain text before returning DTOs to caller backends.
- Course, assignment, and user responses are cached in-memory with a 3-minute TTL to minimize redundant Canvas requests.

### Centralized AI Mode Boundary
- `ai-services/ai-mode` is the only service that reads `OPENROUTER_API_KEY`.
- Downstream services send chat completion requests to `http://ai-mode:8080/v1/chat/completions`.
- Standard model: `nvidia/nemotron-3-ultra-550b-a55b:free`.

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
  - Border radius: `0px` (strict sharp corners).
  - Borders: `4px solid var(--border-color)` (high-contrast ink borders).
  - Shadows: `4px 4px 0 var(--shadow-color)` (hard unblurred drop shadows).
  - Palette: High-contrast parchment surface, hazard yellow accents, retro status indicators.
- **Animations & Micro-interactions**:
  - Standard duration and easing tokens (`--anim-duration-base`, `--anim-ease-out`).
  - Subtle spring transforms on hover and active states (`transform: translate(-2px, -2px)` with expanded shadow).
