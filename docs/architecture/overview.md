# Architecture overview

Current state as of this document's last update. This is source material
for the technical report's architecture diagrams, and orientation for
anyone (or any AI agent) new to the repo.

## Services

| Service | Path | Stack | Port (host) | Owns | Depends on |
|---|---|---|---|---|---|
| shared-shell | shared/frontend | Vue 3 + nginx | 8080 | Unified dashboard, routing to all features | student-1, student-2, student-3 frontends and backends |
| shared-backend | shared/backend | ASP.NET Core + SQLite | 5110 | Canvas API integration (courses, assignments, users), caching | — |
| ai-mode | ai-services/ai-mode | ASP.NET Core | (internal only) | OpenRouter proxy, shared LLM access | — |
| student-1-frontend | student-1/frontend | Vue 3 | (internal only, proxied at /notifications) | Notifications UI | student-1-backend |
| student-1-backend | student-1/backend | ASP.NET Core + SQLite | 5101 | Notifications, preferences, AI digest | ai-mode |
| student-2-frontend | student-2/frontend | Vue 3 + TypeScript | (internal only, proxied at /automations) | Automation configuration and run-history UI | student-2-backend |
| student-2-backend | student-2/backend | ASP.NET Core + EF Core + SQLite | 5102 | Assignment extension and scheduled post configurations and run records | — |
| student-3-frontend | student-3/frontend | Vue 3 + TypeScript | (internal only, proxied at /deadlines) | Deadline and task-tracker UI | student-3-backend |
| student-3-backend | student-3/backend | ASP.NET Core + SQLite | 5103 | Deadlines & task tracker, Canvas assignment sync, AI task planning | shared-backend, ai-mode |

Students 4 and 5 (Accounts and Settings, Grades and Progress) have no code yet — just
`backend/` and `frontend/` placeholder directories.

## Shared infrastructure

Owned by student-1 (Bryan), per team assignment:
- `shared/frontend` — the dashboard shell and reverse proxy
- `shared/ui-kit` — design tokens, fonts, shared Vue components, consumed
  as an npm workspace package by every frontend
- `ai-services/ai-mode` — shared OpenRouter gateway

Owned by student-3 (Jonathon), used by the whole team:
- `shared/backend` — Canvas API integration, other services call its
  HTTP API and don't read its database directly

Canvas assignment descriptions are treated as untrusted HTML. The shared
backend parses them into compact plain text before returning its API DTOs, so
downstream services neither render raw Canvas markup nor send it to AI models.

## Reverse proxy routing

`shared-shell`'s nginx config maps:
- `/` — the dashboard itself
- `/notifications/` → student-1-frontend
- `/api/notifications/` → student-1-backend
- `/automations/` → student-2-frontend
- `/api/automations/` → student-2-backend
- `/deadlines/` → student-3-frontend
- `/api/deadlines/` → student-3-backend
- `/grades`, `/account` — stubbed but commented out, waiting on students 5 and 4

## AI-mode

Using OpenRouter (`nvidia/nemotron-3-ultra-550b-a55b:free`), approved by
the tutor as a substitute for the spec's suggested Ollama runtime. One
shared API key held only by the `ai-mode` gateway service, other backends
call the gateway rather than OpenRouter directly.

The deadline tracker uses its backend as the AI boundary. It resolves course
and assignment context from its own database, submits bounded prompts to
`ai-mode`, validates structured responses, and persists generated subtasks
atomically. The browser never calls `ai-mode` directly.

## Database boundary

Each backend owns its own SQLite file. No service reads another's
database directly. `shared-backend`'s SQLite stores only a Canvas request
audit log, not cached Canvas data (course/assignment/user data is fetched
live with a short in-memory cache, not persisted).
