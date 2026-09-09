# System Architecture Overview

> **Authoritative Architectural Specification for Better Canvas**  
> Workspace: `41026--software-development-project`  
> Topology: Microservices over Docker Compose, Nginx Reverse Proxy, ASP.NET Core & Vue 3

---

## 1. High-Level System Architecture

Better Canvas is an **LLM-enhanced microservices web platform** designed around autonomous vertical slices. Each student owns a bounded business context, backed by dedicated infrastructure services for Canvas LMS connectivity, OpenRouter AI completions, and unified dashboard proxying.

```
                                  [ Browser / Client ]
                                           │
                                  (HTTP: Port 8080)
                                           ▼
                    ┌─────────────────────────────────────────────┐
                    │          shared-shell (Nginx)               │
                    │  - /             -> Dashboard (Vue 3)       │
                    │  - /notifications-> student-1-frontend      │
                    │  - /automations  -> student-2-frontend      │
                    │  - /deadlines    -> student-3-frontend      │
                    │  - /account      -> student-4-frontend      │
                    │  - /grades       -> student-5-frontend      │
                    │  - /api/*        -> Proxied to backends     │
                    └───────┬───────────────────────────────┬─────┘
                            │ (internal HTTP network)       │
            ┌───────────────┴──────────────┬────────────────┴──────────────┐
            ▼                              ▼                               ▼
┌───────────────────────┐      ┌───────────────────────┐      ┌───────────────────────┐
│   student-1-backend   │      │   student-3-backend   │      │   student-5-backend   │
│  (Notifications +     │◄─────┤ (Deadlines, Tasks,    │      │  (Grades & Progress)  │
│   SSE + AI Digest)    │ push │  Sync, AI Subtasks)   │      └───────────┬───────────┘
└───────────┬───────────┘      └───────────┬───────────┘                  │
            │                              │                              │
            │ PostgreSQL                   │ HTTP (private net)           │ HTTP (private net)
            ▼                              ▼                              ▼
┌───────────────────────┐      ┌───────────────────────┐      ┌───────────────────────┐
│   student-1-database  │      │   student-3-database  │      │   student-5-database  │
│   (PostgreSQL 16)     │      │  [EF Core: app.db]    │      │  [EF Core: grades.db] │
└───────────────────────┘      └───────────┬───────────┘      └───────────────────────┘
                                           │
                                           │ HTTP
                                           ▼
┌───────────────────────┐      ┌───────────────────────┐
│     ai-mode (8080)    │      │  shared-backend(8080) │
│  (OpenRouter Gateway) │      │  (Canvas LMS Gateway) │
└───────────┬───────────┘      └───────────┬───────────┘
            │ HTTPS                        │ HTTPS
            ▼                              ▼
     [ OpenRouter API ]             [ Canvas LMS API ]
```

---

## 2. Core Service Topology

```
[ Browser / Client ] ──HTTP :8080──→ shared-shell (Nginx)
                                      │
                                      ├── /notifications, /api/notifications
                                      │     → student-1-backend
                                      │       → student-1-database (PostgreSQL)
                                      │       → shared-backend and ai-mode
                                      │
                                      ├── /automations, /api/automations
                                      │     → student-2-backend
                                      │       → owned SQLite database
                                      │       → shared-backend and ai-mode
                                      │
                                      ├── /deadlines, /api/deadlines
                                      │     → student-3-backend
                                      │       → student-3-database
                                      │       → shared-backend and ai-mode
                                      │       → student-1-backend (notification push)
                                      │
                                      ├── /account, /api/auth|users|students|teachers
                                      │     → student-4-backend / authentication
                                      │       → student-4-database
                                      │       → ai-mode (profile summaries)
                                      │       → MailHog (password-reset email)
                                      │
                                      └── /grades, /api/grades
                                            → student-5-backend
                                              → student-5-database and ai-mode

shared-backend ──HTTPS──→ [ Canvas LMS API ]
ai-mode ────────HTTPS──→ [ OpenRouter API ]
```

### Infrastructure & Shared Services
- **`shared-shell`** (Port 8080): Vue 3 shell dashboard and Nginx reverse proxy routing web requests and API paths.
- **`shared-backend`** (Port 5110): Exclusive Canvas LMS API client with 3-minute in-memory caching and HTML sanitization.
- **`ai-mode`** (Internal 8080): Unified chat completions gateway proxying requests to OpenRouter LLMs.
- **`mailhog`** (SMTP: 1025 / Web UI: 8025): Local SMTP mock service for developer email verification.

### Vertical Microservice Slices
- **`student-1` (Notifications)**: Notification management, delivery preferences, SSE stream broker, AI digest generation and conversational assistant (`student-1-database` PostgreSQL 16 on port 5432).
- **`student-2` (Automations)**: Assignment extension requests, scheduled Canvas posts, AI quiz answering with `ai-mode`, and periodic execution runner.
- **`student-3` (Deadlines & Tasks)**: Coursework deadlines, subtask hierarchies, Canvas synchronization, AI breakdown planning, and push notifications to `student-1-backend` (`student-3-database` internal SQLite service on `student-3-data`).
- **`student-4` (Account & Authentication)**: User registration, login authentication, MailHog password reset workflows, profile AI summaries (`student-4-database` internal SQLite service on `student-4-data`).
- **`student-5` (Grades & Progress)**: Marks calculation, target GPA simulation ("what-if" marks), and progress tracking (`student-5-database` internal SQLite service on `student-5-data`).

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
- `/api/auth/` → Proxies to `http://student-4-authentication:8080/`.
- `/api/users/`, `/api/students/`, `/api/teachers/` → Proxy to `http://student-4-backend:8080/`.
- `/grades/` → Proxies to `http://student-5-frontend:80/`.
- `/api/grades/` → Proxies to `http://student-5-backend:8080/`.

Bare paths (`/automations`, `/deadlines`, `/account`, `/grades`) redirect
permanently to their trailing-slash form.

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
Direct database connection strings point only to the local service database.
`student-3-database`, `student-4-database`, and `student-5-database` exclusively mount their volumes; the matching public APIs use internal HTTP contracts and contain no EF Core dependency.
Each of those database services is attached only to its internal `student-N-data` Docker network. The matching public services join both that private network and the default application network; no other service can directly reach a private persistence API.

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

### D. Scheduled Canvas Automations
- `student-2-backend` stores assignment-extension, scheduled-post, and quiz-filler automations in its owned SQLite database.
- A periodic execution worker durably claims due work, accesses Canvas only through `shared-backend`, and records execution history.
- Quiz-filler automations send eligible Canvas quiz questions to `ai-mode`, validate the structured answers, and save draft attempts through the Canvas gateway without automatically submitting.

### E. Account, Authentication, and AI Profiles
- `student-4-authentication` provides login, password change, account deletion, and email password-reset workflows; both authentication and profile APIs access `student-4-database` exclusively over HTTP.
- Development password-reset messages are delivered to MailHog, whose web inbox is exposed on port `8025`.
- `student-4-backend` can generate a replacement profile summary through `ai-mode` using the stored user and student/teacher profile context.

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
