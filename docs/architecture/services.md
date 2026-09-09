# Microservices Catalog & Service Deep Dive

This document details every service in the repository, including responsibilities, technology stack, endpoints, database schemas, and configuration parameters.

---

## 1. `shared-shell` (Dashboard & Reverse Proxy)

- **Path**: `shared/frontend/`
- **Stack**: Vue 3 + TypeScript + Vite + Nginx
- **Port (Host)**: `8080` (Internal Docker: `80`)
- **Owner**: Student 1 (Bryan Lee)

### Responsibilities
- Serves the unified dashboard homepage (`/`) with quick status widgets (upcoming tasks, recent notifications, grades overview).
- Implements Nginx reverse proxy configuration (`nginx.conf`) routing requests to appropriate microservices.
- Consumes `@better-canvas/ui-kit` for global navigation bar (`TopNav.vue`), theme switching, and persistent unread badge count via SSE.

---

## 2. `shared-backend` (Canvas LMS API Gateway)

- **Path**: `shared/backend/`
- **Stack**: ASP.NET Core (.NET 10 Minimal API) + Entity Framework Core SQLite
- **Port (Host)**: `5110` (Internal Docker: `8080`)
- **Owner**: Student 3 (Jonathon Thomson)

### Responsibilities
- Exclusive client for the Canvas Infrastructure API.
- Authenticates using `CANVAS_API_TOKEN` and `CANVAS_BASE_URL`.
- Sanitizes Canvas HTML assignment descriptions into plain text (preserves headers, lists, code blocks; strips unsafe scripts, styles, and tags).
- Implements an in-memory 3-minute TTL cache (`IMemoryCache`) for courses, assignments, and user profile data.
- Maintains an audit database of Canvas API requests (`CanvasAuditLog`).
- Provides messageable recipient search (`/api/v1/search/recipients`) and Classic Quizzes endpoints for the Automations slice.

### Key Endpoints
- `GET /api/canvas/courses` — Lists enrolled courses.
- `GET /api/canvas/courses/{courseId}/assignments` — Lists assignments for a given course.
- `GET /api/canvas/users/self` — Returns the authenticated user's Canvas profile.
- `GET /api/canvas/search/recipients` — Search messageable Canvas users by context.
- `GET /api/canvas/courses/{courseId}/quizzes` — Lists course Classic Quizzes.
- `POST /api/canvas/courses/{courseId}/quizzes/{quizId}/submissions` — Create or resume quiz submission.
- `GET /api/canvas/quiz_submissions/{submissionId}/questions` — Load quiz questions.
- `POST /api/canvas/quiz_submissions/{submissionId}/questions` — Save draft answers against submission.

---

## 3. `ai-mode` (OpenRouter LLM Gateway)

- **Path**: `ai-services/ai-mode/`
- **Stack**: ASP.NET Core (.NET 10 Minimal API)
- **Port (Host)**: *Internal Docker only* (`http://ai-mode:8080`)
- **Owner**: Student 1 (Bryan Lee)

### Responsibilities
- Centralized proxy for OpenRouter LLM completions (`https://openrouter.ai/api/v1/chat/completions`).
- Holds the repository's sole `OPENROUTER_API_KEY`.
- Defaults to `minimax/minimax-m3:free` if no model is explicitly specified in the request payload.
- Converts embedded error payloads into proper HTTP status codes for robust client-side retry handling.
- Provides health check endpoints (`/health/live` for process liveness, `/health/ready` for API key validation).

### Key Endpoints
- `POST /v1/chat/completions` — OpenAI-compatible chat completions proxy.
- `GET /health/live` — Liveness probe.
- `GET /health/ready` — Readiness probe.

---

## 4. `student-1` (Notifications Microservice)

- **Path**: `student-1/backend/`, `student-1/database/`, `student-1/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) + EF Core PostgreSQL + SSE Pub/Sub Broker
  - Database: PostgreSQL 16 Alpine (`student-1-database`, port `5432`)
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit` + Playwright
- **Port (Host)**: `5101` (Backend), `5432` (Database), Proxied at `/notifications/` (Frontend)
- **Owner**: Student 1 (Bryan Lee)

### Responsibilities
- Stores student notifications (categories: `Deadline`, `Grade`, `Automation`, `Account`, `AI`).
- Manages per-type delivery preferences (channels: `InApp`, `Email`).
- Provides real-time Server-Sent Events stream (`GET /notifications/stream`) via `NotificationStreamBroker`.
- Generates AI digests summarizing unread activity (`POST /digest/generate`).
- Provides conversational AI assistant grounded in notifications (`POST /digest/chat`).
- Supports cross-service action triggers (`AI BREAK DOWN`, `GRADE IMPACT`, `MARK COMPLETE`).
- Unit and integration tests in `Api.Tests`, Playwright end-to-end tests in `frontend/e2e`.

### Key Endpoints
- `GET /notifications` — Query notifications with pagination, filtering, and sorting.
- `POST /notifications/push` — Ingest notification from internal services (e.g. deadline reminders).
- `PUT /notifications/{id}/read` & `PUT /notifications/read-all` — Mark read status.
- `DELETE /notifications/{id}` — Delete notification.
- `GET /notifications/stream` — Real-time SSE stream (`text/event-stream`).
- `GET /notifications/preferences` & `PUT /notifications/preferences` — Delivery preferences.
- `POST /digest/generate` — Generate AI summary of recent activity.
- `POST /digest/chat` — Conversational Q&A with AI assistant.

---

## 5. `student-2` (Automations Microservice)

- **Path**: `student-2/backend/` and `student-2/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) + EF Core SQLite + Background Worker
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5102` (Backend), Proxied at `/automations/` (Frontend)
- **Owner**: Student 2 (Isaac Thomas)

### Responsibilities
- Configures assignment extensions, scheduled Canvas posts, and AI quiz filler automations.
- Background worker checks every 30 seconds for due executions.
- Dispatches Canvas messages via `shared-backend` (`POST /api/canvas/conversations`).
- Dispatches AI quiz answers using `ai-mode` and saves draft answers without auto-submitting.
- Tracks execution history with immutable parameter snapshots, deterministic SHA-256 claim keys, and run states (`RUN`, `SUC`, `FAI`).

### Key Endpoints
- `GET /api/automations` & `POST /api/automations` — Polymorphic automation CRUD (`assignmentExtension`, `scheduledPost`, `quizFiller`).
- `PUT /api/automations/{id}` & `DELETE /api/automations/{id}` — Update / delete automation.
- `GET /api/automation-runs` & `GET /api/automation-runs/{id}` — Read-only execution history.

---

## 6. `student-3` (Deadlines & Task Tracker Microservice)

- **Path**: `student-3/backend/`, `student-3/database/`, `student-3/contracts/`, and `student-3/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10), public API and orchestration
  - Database: ASP.NET Core (.NET 10) + EF Core SQLite, internal-only on `student-3-data`
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5103` (Backend), `5203` (database when run standalone), Proxied at `/deadlines/` (Frontend)
- **Owner**: Student 3 (Jonathon Thomson)

### Responsibilities
- Manages coursework tasks, subtasks, priorities, completion states, and due dates.
- Ingests Canvas assignments via `POST /api/canvas-sync` through `shared-backend`.
- Periodically runs `DueSoonReminderBackgroundService` to dispatch reminders to `student-1-backend`.
- Dispatches real-time push notifications to `student-1-backend` upon task creation and AI breakdown.
- Provides AI-assisted subtask planning (`POST /api/deadlines/tasks/{id}/ai-breakdown`).
- Delegates all persistence over HTTP to `student-3-database`.

### Key Endpoints
- `GET /api/deadlines/tasks` & `POST /api/deadlines/tasks` — Task CRUD.
- `PUT /api/deadlines/tasks/{id}` & `DELETE /api/deadlines/tasks/{id}` — Task update/delete.
- `POST /api/canvas-sync` — Trigger sync with Canvas gateway.
- `POST /api/deadlines/tasks/{id}/ai-breakdown` — Generate AI task breakdown subtasks.

---

## 7. `student-4` (Account & Authentication Microservice)

- **Path**: `student-4/backend/`, `student-4/authentication/`, `student-4/database/`, `student-4/contracts/`, and `student-4/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) Account API
  - Auth: ASP.NET Core (.NET 10) Authentication & Password Reset API
  - Database: ASP.NET Core (.NET 10) + EF Core SQLite, internal-only on `student-4-data`
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5104` (Backend), `5114` (Auth), `5204` (database when standalone), Proxied at `/account/` (Frontend)
- **Owner**: Student 4 (Tristan Huang)

### Responsibilities
- Manages user accounts, student/teacher profiles, role-based records, and profile AI summaries via `ai-mode`.
- Handles user registration, login authentication, and secure password hashing.
- Dispatches password reset emails via MailHog SMTP server (`mailhog:1025`).
- Delegates all persistence over HTTP to `student-4-database`.

### Key Endpoints
- `GET /api/users/{id}`, `PUT /api/users/{id}` — User profile management.
- `GET /api/students`, `GET /api/teachers` — Role-specific listings.
- `POST /api/auth/register`, `POST /api/auth/login` — Account registration and authentication.
- `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` — Password reset workflows.

---

## 8. `student-5` (Grades & Progress Microservice)

- **Path**: `student-5/backend/`, `student-5/database/`, `student-5/contracts/`, and `student-5/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) GradesManager API
  - Database: ASP.NET Core (.NET 10) + EF Core SQLite, internal-only on `student-5-data`
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5105` (Backend), `5205` (database when standalone), Proxied at `/grades/` (Frontend)
- **Owner**: Student 5 (William Hannah)

### Responsibilities
- Calculates grade progress, course weights, and cumulative marks.
- Provides "What-If" grade simulation allowing students to forecast target GPAs or marks.
- Exposes mark update endpoints used by cross-service interactive notifications.
- Delegates all persistence over HTTP to `student-5-database`.

### Key Endpoints
- `GET /api/grades` — List grades and marks across enrolled courses.
- `PUT /api/grades/api/assignment/marks/` — Update or simulate assignment marks.

---

## 9. `mailhog` (Mock SMTP Server & Web Mailbox)

- **Image**: `mailhog/mailhog:latest`
- **Port (Host)**: `1025` (SMTP), `8025` (Web UI)
- **Internal Docker DNS**: `mailhog:1025`

### Responsibilities
- Receives mock transactional emails dispatched by `student-4-authentication`.
- Provides an interactive developer web inbox at `http://localhost:8025` for validating email delivery, token links, and formatting during local development and testing.

