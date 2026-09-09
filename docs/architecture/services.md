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
- Consumes `@better-canvas/ui-kit` for the global navigation bar (`Navbar.vue`), theme switching, and persistent unread badge count via SSE.

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
- Maintains an audit database of Canvas API requests (`CanvasRequestLog`, stored in `shared.db`).

### Key Endpoints
- `GET /api/canvas/courses` — Lists enrolled courses.
- `GET /api/canvas/courses/{courseId}/assignments` — Lists assignments for a given course.
- `GET /api/canvas/courses/{courseId}/users` — Lists users enrolled in a course.
- `GET /api/canvas/courses/{courseId}/recipients` — Lists messageable recipients for a course.
- `POST /api/canvas/conversations` — Creates a Canvas conversation (scheduled posts).
- `GET /api/canvas/courses/{courseId}/quizzes` — Lists Classic Quizzes for a course.
- Classic Quiz submission helpers for starting a submission, reading its
  questions, and saving answers. There is deliberately no endpoint that
  completes/turns in a submission.

---

## 3. `ai-mode` (OpenRouter LLM Gateway)

- **Path**: `ai-services/ai-mode/`
- **Stack**: ASP.NET Core (.NET 10 Minimal API)
- **Port (Host)**: *Internal Docker only* (`http://ai-mode:8080`)
- **Owner**: Student 1 (Bryan Lee)

### Responsibilities
- Centralized proxy for OpenRouter LLM completions (`https://openrouter.ai/api/v1/chat/completions`).
- Holds the repository's sole `OPENROUTER_API_KEY`.
- Defaults to `nvidia/nemotron-3.5-lightning:free` if no model is explicitly specified in the request payload. The default can be overridden gateway-wide with `OPENROUTER_MODEL` (or the `OpenRouter:Model` configuration key).
- Converts embedded error payloads into proper HTTP status codes for robust client-side retry handling.
- Provides health check endpoints (`/health/live` for process liveness, `/health/ready` for API key validation).

### Key Endpoints
- `POST /v1/chat/completions` — OpenAI-compatible chat completions proxy.
- `GET /health/live` — Liveness probe.
- `GET /health/ready` — Readiness probe.

---

## 4. `student-1` (Notifications Microservice)

- **Path**: `student-1/backend/`, `student-1/database/`, and `student-1/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) + EF Core (Npgsql/PostgreSQL) + SSE Pub/Sub Broker
  - Database: PostgreSQL 16 Alpine container (`student-1/database`) seeded by `init.sql`
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5101` (Backend), `5432` (PostgreSQL), Proxied at `/notifications/` (Frontend)
- **Owner**: Student 1 (Bryan Lee)

### Responsibilities
- Stores student notifications (categories: `Deadline`, `Grade`, `Automation`, `Account`, `AI`).
- Manages per-type delivery preferences (channels: `InApp`, `Email`).
- Provides real-time Server-Sent Events stream (`GET /notifications/stream`) via `NotificationStreamBroker`.
- Generates AI digests summarizing unread activity (`POST /digest/generate`).
- Provides conversational AI assistant grounded in notifications (`POST /digest/chat`).
- Supports cross-service action triggers (`AI BREAK DOWN`, `GRADE IMPACT`, `MARK COMPLETE`).
- Delegates persistence to the dedicated `student-1-database` PostgreSQL
  container, which exclusively owns the `student-1-postgres-data` volume.

### Key Endpoints
- `GET /notifications` — Query notifications with pagination, filtering, and sorting.
- `POST /notifications/push` — Ingest notification from internal services (e.g. deadline reminders).
- `PUT /notifications/{id}/read`, `PUT /notifications/{id}/unread` & `PUT /notifications/read-all` — Mark read status.
- `DELETE /notifications/{id}` — Delete notification.
- `GET /notifications/stream` — Real-time SSE stream (`text/event-stream`).
- `GET /preferences`, `POST /preferences`, `PUT /preferences/{id}` & `DELETE /preferences/{id}` — Delivery preferences.
- `POST /api/canvas-sync` — Sync Canvas assignments into notifications through `shared-backend`.
- `GET /digest` — List previously generated digests.
- `POST /digest/generate` — Generate AI summary of recent activity.
- `POST /digest/chat` — Conversational Q&A with AI assistant.

### Tests
- `student-1/backend/Api.Tests` holds xUnit unit tests (stream broker, DTO
  extensions, Canvas sync, OpenRouter digest service) and integration tests
  driven by `Microsoft.AspNetCore.Mvc.Testing`.
- `student-1/frontend/e2e` holds Playwright end-to-end specs
  (`npm run test:e2e --workspace=student-1-frontend`).

---

## 5. `student-3` (Deadlines & Task Tracker Microservice)

- **Path**: `student-3/backend/`, `student-3/database/`,
  `student-3/contracts/`, and `student-3/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10), public API and orchestration
  - Database: ASP.NET Core (.NET 10) + EF Core SQLite, internal-only
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5103` (Backend), `5203` (database when run standalone),
  Proxied at `/deadlines/` (Frontend)
- **Owner**: Student 3 (Jonathon Thomson)

### Responsibilities
- Manages coursework tasks, subtasks, priorities, completion states, and due dates.
- Ingests Canvas assignments via `POST /api/canvas-sync` through `shared-backend`.
- Periodically runs `DueSoonReminderBackgroundService` to dispatch reminders to `student-1-backend`.
- Provides AI-assisted subtask planning (`POST /api/deadlines/tasks/{id}/ai-breakdown`).
- Delegates all persistence over HTTP to `student-3-database`, which
  exclusively owns the `student-3-db` volume, migrations, and atomic writes.
- Uses a private internal Docker network shared only by the Student 3 API and
  database services. Notification delivery remains a runtime integration and
  does not block `student-3-backend` startup.
- Frontend offers list view, monthly calendar view, and upcoming deadlines widget.

### Key Endpoints
- `GET /api/deadlines/tasks` & `POST /api/deadlines/tasks` — Task CRUD.
- `PUT /api/deadlines/tasks/{id}` & `DELETE /api/deadlines/tasks/{id}` — Task update/delete.
- `POST /api/canvas-sync` — Trigger sync with Canvas gateway.
- `POST /api/deadlines/tasks/{id}/ai-breakdown` — Generate AI task breakdown subtasks.

---

## 6. `student-5` (Grades & Progress Microservice)

- **Path**: `student-5/backend/`, `student-5/database/`,
  `student-5/contracts/`, and `student-5/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10), public API and orchestration
  - Database: ASP.NET Core (.NET 10) + EF Core SQLite, internal-only
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5105` (Backend), Proxied at `/grades/` (Frontend)
- **Owner**: Student 5 (William Hannah)

### Responsibilities
- Calculates grade progress, course weights, and cumulative marks.
- Provides "What-If" grade simulation allowing students to forecast target GPAs or marks.
- Exposes mark update endpoints used by cross-service interactive notifications.
- Generates AI study recommendations through the `ai-mode` gateway.
- Delegates all persistence over HTTP to `student-5-database`, which
  exclusively owns the `student-5-db` volume and its EF Core migrations, on the
  private `student-5-data` Docker network.

### Key Endpoints
The shell proxies `/api/grades/` to the backend root, so each route below is
reachable externally as `/api/grades<route>`.

- `GET /api/courses` & `GET /api/courses/{id}` — Course listings.
- `GET /api/students` & `GET /api/students/{id}` — Student records and ideal marks.
- `POST /api/students`, `PUT /api/students` & `DELETE /api/students/{studentId}` — Ideal-mark management.
- `GET /api/assignment/{id}`, `GET /api/assignment/student/{studentId}` & `GET /api/assignment/course/{courseId}` — Assignment lookups.
- `POST /api/assignment/marks/`, `PUT /api/assignment/marks/`, `DELETE /api/assignment/marks/{studentId}/{assignmentId}` & `GET /api/assignment/marks/{studentId}` — Temporary ("what-if") marks.
- `POST /api/ai/generate-recommendation` — AI study recommendation.

---

## 7. `student-2` (Automations Microservice)

- **Path**: `student-2/backend/` and `student-2/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) + EF Core SQLite + periodic execution worker
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5102` (Backend), Proxied at `/automations/` (Frontend)
- **Owner**: Student 2 (Isaac Thomas)

### Responsibilities
- Stores assignment-extension, scheduled-post, and quiz-filler automation
  configurations in polymorphic EF Core tables.
- Runs a background worker (default every 30 seconds) that claims due execution
  candidates with durable execution keys before contacting Canvas.
- Executes scheduled Canvas conversations and AI-assisted Classic Quiz filling
  through `shared-backend` and `ai-mode`; it never turns a quiz in.
- Exposes read-only run history.

### Key Endpoints
- `/api/automations` — Automation CRUD (polymorphic `$type` payloads).
- `/api/automation-runs` — Read-only run history.
- Swagger UI in Development at `http://localhost:5102/swagger`.

See [`student-2/README.md`](../../student-2/README.md) for the full contract.

---

## 8. `student-4` (Account Microservice)

- **Path**: `student-4/backend/`, `student-4/authentication/`,
  `student-4/database/`, `student-4/contracts/`, and `student-4/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10), profile API and AI profile summaries
  - Authentication: ASP.NET Core (.NET 10), login, password and account management, SMTP password reset
  - Database: ASP.NET Core (.NET 10) + EF Core SQLite, internal-only
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5104` (Backend), `5114` (Authentication), Proxied at `/account/` (Frontend)
- **Owner**: Student 4 (Tristan Huang)

### Responsibilities
- Stores user, student, and teacher profile records with hashed passwords.
- Issues password-reset emails through SMTP; development uses the `mailhog`
  container (SMTP `1025`, web UI `http://localhost:8025`).
- Generates AI profile summaries through the `ai-mode` gateway.
- Delegates all persistence over HTTP to `student-4-database`, which
  exclusively owns the `student-4-db` volume on the private `student-4-data`
  Docker network.

### Key Endpoints
- `GET|POST /api/users`, `GET|PUT|DELETE /api/users/{userId}` — User records.
- `POST /api/users/{userId}/profile-summary` — AI profile summary.
- `GET|PUT /api/students/{userId}` & `GET|PUT /api/teachers/{userId}` — Role profiles.
- `POST /api/auth/login`, `POST /api/auth/change-password`, `DELETE /api/auth/delete-account` — Account management.
- `POST /api/auth/forgot-password` & `POST /api/auth/reset-password` — Password reset.
