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

### Key Endpoints
- `GET /api/canvas/courses` — Lists enrolled courses.
- `GET /api/canvas/courses/{courseId}/assignments` — Lists assignments for a given course.
- `GET /api/canvas/users/self` — Returns the authenticated user's Canvas profile.

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

- **Path**: `student-1/backend/` and `student-1/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) + EF Core SQLite + SSE Pub/Sub Broker
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5101` (Backend), Proxied at `/notifications/` (Frontend)
- **Owner**: Student 1 (Bryan Lee)

### Responsibilities
- Stores student notifications (categories: `Deadline`, `Grade`, `System`, `Announcement`, `Ai`).
- Manages per-type delivery preferences (channels: `InApp`, `Email`).
- Provides real-time Server-Sent Events stream (`GET /notifications/stream`) via `NotificationStreamBroker`.
- Generates AI digests summarizing unread activity (`POST /digest/generate`).
- Provides conversational AI assistant grounded in notifications (`POST /digest/chat`).
- Supports cross-service action triggers (`AI BREAK DOWN`, `GRADE IMPACT`, `MARK COMPLETE`).

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

- **Path**: `student-5/backend/` and `student-5/frontend/`
- **Stack**:
  - Backend: ASP.NET Core (.NET 10) + EF Core SQLite
  - Frontend: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit`
- **Port (Host)**: `5105` (Backend), Proxied at `/grades/` (Frontend)
- **Owner**: Student 5 (William Hannah)

### Responsibilities
- Calculates grade progress, course weights, and cumulative marks.
- Provides "What-If" grade simulation allowing students to forecast target GPAs or marks.
- Exposes mark update endpoints used by cross-service interactive notifications.

### Key Endpoints
- `GET /api/grades` — List grades and marks across enrolled courses.
- `PUT /api/grades/api/assignment/marks/` — Update or simulate assignment marks.

---

## 7. `student-2` (Automations) & `student-4` (Account)

- **Path**: `student-2/` and `student-4/`
- **Owners**: Student 2 (Isaac Thomas) and Student 4 (Tristan Huang)
- **Status**: Scaffolding / planned for Release 1.
- **Integration Roadmap**: Follow [Playbook: New Backend Microservice](../playbooks/new-backend-microservice.md) and [Playbook: New Frontend Microservice](../playbooks/new-frontend-microservice.md).
