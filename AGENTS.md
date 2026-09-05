# AGENTS.md — Universal Agent Instruction Manual

> **Authoritative Specification & Operating Guide for AI Coding Agents and Developers**  
> Workspace: `41026--software-development-project`  
> Architecture: Microservices over Docker Compose with Nginx Reverse Proxy & ASP.NET Core / Vue 3

---

## 1. System Overview

This repository is an **LLM-enhanced microservices web platform** built on top of the **Canvas Infrastructure LMS API**. The project is architected as a set of autonomous vertical slices owned by individual team members, unified by a shared frontend shell and backed by shared infrastructure services.

### Core Architectural Paradigm
1. **Vertical Slice Microservices**: Each student owns a bounded context (`student-1` through `student-5`) containing its own backend and frontend.
2. **Shared Gateway Services**:
   - `shared-backend`: Exclusive owner of Canvas LMS API communications, authentication, caching, and HTML sanitization.
   - `ai-mode`: Exclusive holder of the OpenRouter LLM API key, serving as a unified chat-completions gateway for all backends.
   - `shared-shell`: Vue 3 dashboard shell and Nginx reverse proxy mapping all frontend routes and backend `/api/*` routes under a single origin (`http://localhost:8080`).
   - `shared/ui-kit`: `@better-canvas/ui-kit` npm workspace package delivering the canonical Neobrutalism design system tokens, CSS primitives, fonts, and shared Vue components.

---

## 2. The Five Golden Architectural Rules

Any AI agent modifying or extending this codebase **must strictly follow these 5 rules**:

### Rule 1: Database Isolation (No Cross-Database Queries)
- Each bounded context owns an independent SQLite database (`*.db`) via Entity Framework Core.
- **NEVER** query another service's SQLite file or EF Core DbContext directly.
- **All cross-service data exchange must occur over HTTP APIs** using defined DTO contracts.

### Rule 2: Canvas Gateway Boundary & HTML Sanitization
- Microservices **must not** make direct requests to the Canvas LMS API or store Canvas API tokens.
- All Canvas communication is routed through `shared-backend` (`http://shared-backend:8080/api/canvas/*`).
- Canvas assignment descriptions are treated as **untrusted HTML** and sanitized to plain text by `shared-backend` before delivery to downstream services.

### Rule 3: Centralized AI Gateway (`ai-mode`)
- Microservices **must not** call OpenRouter or external LLM providers directly.
- All AI features call the internal `ai-mode` gateway (`http://ai-mode:8080/v1/chat/completions`) inside the Docker network.
- Only the `ai-mode` container receives the `OPENROUTER_API_KEY` secret from the root `.env`.

### Rule 4: Neobrutalism Design System (`@better-canvas/ui-kit`)
- Frontends are built with **Vue 3 `<script setup lang="ts">` and plain SCSS**.
- All styles must use design tokens from `@better-canvas/ui-kit` (`--color-surface`, `--color-primary`, `--border-width-md`, `--shadow-offset-md`, `--font-mono`, etc.).
- **Design rules**: Zero border-radius (`border-radius: 0`), prominent solid borders (`border: 4px solid var(--border-color)`), hard drop shadows without blur (`box-shadow: 4px 4px 0 var(--shadow-color)`), high contrast, and brutalist badges/buttons.
- **Do NOT introduce Vuetify or heavy external UI component libraries** into new frontend services.

### Rule 5: Git & Contribution Workflow
- Always branch off `main` for new features or fixes (`feat/<feature-name>`, `fix/<fix-name>`, `docs/<doc-name>`).
- Use **Conventional Commits** (`feat(...)`, `fix(...)`, `docs(...)`, `chore(...)`, `refactor(...)`).
- Never push directly to `main`; submit a Pull Request targeting `main`.

---

## 3. Microservice Inventory & Port Map

| Service Name | Directory | Stack | Host Port | Docker Internal DNS | Responsibilities / Routes |
|---|---|---|---|---|---|
| **`shared-shell`** | `shared/frontend` | Vue 3 + Nginx | `8080` | `http://shared-shell:80` | Dashboard shell (`/`), Nginx reverse proxy for all frontends & APIs |
| **`shared-backend`** | `shared/backend` | ASP.NET Core (.NET 10) + SQLite | `5110` | `http://shared-backend:8080` | Canvas API gateway (`/api/canvas/*`), in-memory caching (3-min TTL), audit log |
| **`ai-mode`** | `ai-services/ai-mode` | ASP.NET Core (.NET 10) | *Internal only* | `http://ai-mode:8080` | OpenRouter LLM gateway (`/v1/chat/completions`), health checks (`/health/live`, `/health/ready`) |
| **`student-1-backend`** | `student-1/backend` | ASP.NET Core (.NET 10) + SQLite | `5101` | `http://student-1-backend:8080` | Notifications, delivery preferences, AI digests & chat, SSE stream (`/notifications/*`, `/digest/*`, `/preferences/*`) |
| **`student-1-frontend`** | `student-1/frontend` | Vue 3 + TypeScript + Vite | *Proxied* | `http://student-1-frontend:80` | Notifications UI, SSE toast alerts, AI digest assistant (`/notifications/`, `/api/notifications/`) |
| **`student-2-backend`** | `student-2/backend` | ASP.NET Core (.NET 10) + SQLite | `5102` | `http://student-2-backend:8080` | Automations service (planned / placeholder) |
| **`student-2-frontend`** | `student-2/frontend` | Vue 3 + TypeScript + Vite | *Proxied* | `http://student-2-frontend:80` | Automations UI (`/automations/`, `/api/automations/`) |
| **`student-3-backend`** | `student-3/backend` | ASP.NET Core (.NET 10) | `5103` | `http://student-3-backend:8080` | Public deadline/task API, Canvas and AI orchestration, due-soon reminders (`/api/deadlines/*`, `/api/tasks/*`, `/api/canvas-sync`) |
| **`student-3-database`** | `student-3/database` | ASP.NET Core (.NET 10) + EF Core SQLite | *Internal only* (`5203` standalone) | `http://student-3-database:8080` | Exclusive owner of Student 3 persistence, migrations, seeding, and atomic task operations |
| **`student-3-frontend`** | `student-3/frontend` | Vue 3 + TypeScript + Vite | *Proxied* | `http://student-3-frontend:80` | Deadlines & task management UI, calendar view (`/deadlines/`, `/api/deadlines/`) |
| **`student-4-backend`** | `student-4/backend` | ASP.NET Core (.NET 10) + SQLite | `5104` | `http://student-4-backend:8080` | Account & profile service (planned / placeholder) |
| **`student-4-frontend`** | `student-4/frontend` | Vue 3 + TypeScript + Vite | *Proxied* | `http://student-4-frontend:80` | Account UI (`/account/`, `/api/account/`) |
| **`student-5-backend`** | `student-5/backend` | ASP.NET Core (.NET 10) + SQLite | `5105` | `http://student-5-backend:8080` | Grades & progress API, what-if marks calculation (`/api/grades/*`) |
| **`student-5-frontend`** | `student-5/frontend` | Vue 3 + TypeScript + Vite | *Proxied* | `http://student-5-frontend:80` | Grades & progress UI (`/grades/`, `/api/grades/`) |

---

## 4. Repository Structure

```
.
├── AGENTS.md                          # [YOU ARE HERE] Canonical universal AI agent guide
├── CLAUDE.md                          # Streamlined agent cheat-sheet
├── README.md                          # Human-oriented project overview & team breakdown
├── CONTRIBUTING.md                    # Git workflows, PR guidelines, coding conventions
├── docker-compose.yml                 # Full stack multi-container orchestration
├── package.json                       # Root workspace declaration (@better-canvas/ui-kit + frontends)
├── .env.example                       # Environment variables template
│
├── ai-services/
│   └── ai-mode/                       # OpenRouter LLM proxy gateway (ASP.NET Core)
│       ├── Api/                       # Minimal API endpoints (/v1/chat/completions)
│       └── Dockerfile
│
├── shared/
│   ├── backend/                       # Canvas LMS API gateway + SQLite audit log
│   ├── frontend/                      # Shared dashboard shell + Nginx reverse proxy
│   └── ui-kit/                        # @better-canvas/ui-kit (tokens, primitives, components)
│
├── student-1/                         # Notifications vertical slice (Bryan Lee)
│   ├── backend/                       # NotificationService ASP.NET Core + SQLite + SSE broker
│   └── frontend/                      # Vue 3 Notifications UI + AI Digest Assistant + Toasts
│
├── student-2/                         # Automations vertical slice (Isaac Thomas)
│   ├── backend/                       # Placeholder / scaffold
│   └── frontend/                      # Placeholder / scaffold
│
├── student-3/                         # Deadlines & Tasks vertical slice (Jonathon Thomson)
│   ├── backend/                       # Public API, Canvas/AI orchestration, reminders
│   ├── database/                      # Internal EF Core/SQLite persistence service
│   ├── contracts/                     # Shared internal persistence contracts
│   └── frontend/                      # Vue 3 Deadlines & Task Tracker UI
│
├── student-4/                         # Account vertical slice (Tristan Huang)
│   ├── backend/                       # Placeholder / scaffold
│   └── frontend/                      # Placeholder / scaffold
│
├── student-5/                         # Grades & Progress vertical slice (William Hannah)
│   ├── backend/                       # GradesManager ASP.NET Core + SQLite
│   └── frontend/                      # Vue 3 Grades & Progress UI
│
├── tools/
│   ├── agentic_loop.py                # Multi-agent architecture & code evaluation runner
│   └── agentic_loop/                  # Collectors, pipelines, prompts for automated review
│
└── docs/                              # Comprehensive documentation library
    ├── README.md                      # Documentation index
    ├── architecture/                  # Architecture overview, services, data flows
    ├── development/                   # Getting started, testing/CI, database migrations
    └── playbooks/                     # Playbooks for new frontends, backends, agentic loop
```

---

## 5. Environment Variables & Secrets

The root `.env` file (copied from `.env.example`) supplies configuration:

| Variable | Target Service | Purpose |
|---|---|---|
| `OPENROUTER_API_KEY` | `ai-mode` | API key for OpenRouter LLM models (e.g. `minimax/minimax-m3:free`) |
| `CANVAS_BASE_URL` | `shared-backend` | Base URL of institution Canvas instance (`https://your-institution.instructure.com`) |
| `CANVAS_API_TOKEN` | `shared-backend` | Personal Canvas access token |
| `ASPNETCORE_ENVIRONMENT` | All .NET backends | Set to `Development` |
| `SharedService__BaseUrl` | `student-1`, `student-3` | Internal URL for Canvas gateway (`http://shared-backend:8080`) |
| `AiGateway__BaseUrl` | `student-1`, `student-3` | Internal URL for AI gateway (`http://ai-mode:8080`) |
| `NotificationService__BaseUrl`| `student-3` | Internal URL for notifications (`http://student-1-backend:8080`) |

---

## 6. CLI Cheatsheet for Agents

### Full Stack (Docker Compose)
```bash
# Start all services with rebuild
docker compose up --build

# Start specific services
docker compose up -d shared-shell shared-backend ai-mode student-1-backend student-1-frontend

# Stop all services and clean up volumes (forces fresh SQLite seeding)
docker compose down -v
```

### .NET Backend Operations (from microservice `backend/` or `Api/` directory)
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run standalone backend (e.g., student-1 on port 5101)
dotnet run --project Api/Api.csproj

# Run tests
dotnet test

# Format and check code standards
dotnet format --verify-no-changes

# Add an EF Core migration
dotnet ef migrations add <MigrationName> --project Api/Api.csproj

# Update database schema manually
dotnet ef database update --project Api/Api.csproj
```

### Frontend Operations (from root or workspace directory)
```bash
# Install all dependencies across all workspaces
npm install

# Run shared shell in development mode
npm run dev --workspace=shared-frontend

# Run student frontend in development mode
npm run dev --workspace=student-1-frontend
npm run dev --workspace=student-3-frontend
npm run dev --workspace=student-5-frontend

# Typecheck and build all frontends
npm run build --workspaces
```

---

## 7. Cross-Service Interaction Patterns

```
                                  [ Browser / Client ]
                                           │
                                  (HTTP: Port 8080)
                                           ▼
                    ┌─────────────────────────────────────────────┐
                    │          shared-shell (Nginx)               │
                    │  - /             -> Dashboard (Vue 3)       │
                    │  - /notifications-> student-1-frontend      │
                    │  - /deadlines    -> student-3-frontend      │
                    │  - /grades       -> student-5-frontend      │
                    │  - /api/*        -> Proxied to backends     │
                    └───────┬───────────────────────────────┬─────┘
                            │ (internal HTTP network)       │
            ┌───────────────┴──────────────┐                │
            ▼                              ▼                ▼
┌───────────────────────┐      ┌───────────────────────┐ ┌───────────────────────┐
│   student-1-backend   │      │   student-3-backend   │ │   student-5-backend   │
│  (Notifications +     │◄─────┤ (Deadlines, Tasks,    │ │   (Grades & Progress) │
│   SSE + AI Digest)    │ push │  Sync, AI Subtasks)   │ └───────────────────────┘
└───────────┬───────────┘      └───────────┬───────────┘
            │                              │
            │ HTTP                         │ HTTP
            ▼                              ▼
┌───────────────────────┐      ┌───────────────────────┐
│     ai-mode (8080)    │      │  shared-backend(8080) │
│  (OpenRouter Gateway) │      │  (Canvas LMS Gateway) │
└───────────┬───────────┘      └───────────┬───────────┘
            │ HTTPS                        │ HTTPS
            ▼                              ▼
     [ OpenRouter API ]             [ Canvas LMS API ]
```

### Key Interactive Flows
1. **Canvas Assignment Ingestion**: User/Job invokes `POST /api/canvas-sync` on `student-3-backend` → calls `shared-backend` → fetches sanitized Canvas assignments → sends one snapshot command to `student-3-database` for an atomic SQLite upsert.
2. **Proactive Deadline Reminders**: `student-3-backend` queries due candidates from `student-3-database` → dispatches `POST /notifications/push` to `student-1-backend` → records successful delivery through the database service.
3. **Real-time SSE Notification Stream**: `student-1-frontend` establishes `GET /notifications/stream` (`text/event-stream`) → `NotificationStreamBroker` pushes live events to toasts and shell unread bell badge.
4. **Cross-Service Action Buttons**:
   - `AI BREAK DOWN`: Notification on `student-1` triggers dialog calling `POST /api/deadlines/tasks/{id}/ai-breakdown` on `student-3`.
   - `GRADE IMPACT`: Notification on `student-1` triggers dialog calling `student-5` what-if grade calculator.
   - `MARK COMPLETE`: Direct inline update via `PUT /api/deadlines/tasks/{id}`.
5. **AI Digest & Chat Assistant**: `student-1-backend` passes recent notifications to `ai-mode` (`POST /v1/chat/completions`) for summary and dynamic conversational Q&A.

---

## 8. Troubleshooting Guide for Agents

- **AI Feature Returns HTTP 500**:
  - Cause: `OPENROUTER_API_KEY` missing or invalid in root `.env`.
  - Fix: Check root `.env` and restart `ai-mode` (`docker compose up -d ai-mode`).
- **Cannot Connect to Another Service**:
  - Cause: Running standalone `dotnet run` without internal Docker DNS resolution.
  - Fix: Cross-service HTTP calls require Docker Compose network or explicit `localhost:<port>` overrides in development appsettings.
- **CORS Errors in Browser**:
  - Cause: Backend missing origin whitelist.
  - Fix: Add origin to `Cors:AllowedOrigins` in `appsettings.Development.json` or `docker-compose.yml`.
- **EF Core Database Drift / Missing Tables**:
  - Cause: Model entity changed without migration.
  - Fix: Run `dotnet ef migrations add <Name> --project Api/Api.csproj` and restart the backend.
- **Frontend Shows 404 / 502 for New Microservice**:
  - Cause: Nginx location block missing or commented out in `shared/frontend/nginx.conf`.
  - Fix: Uncomment/add the route block in `shared/frontend/nginx.conf` and ensure `shared-shell` has `depends_on` the new service.

---

## 9. Related Documentation Links

- [Documentation Index](docs/README.md)
- [Architecture Deep-Dive](docs/architecture/overview.md)
- [Microservices Catalog](docs/architecture/services.md)
- [Data Flows & Sequences](docs/architecture/data-flows.md)
- [Local Development Runbook](docs/development/getting-started.md)
- [Testing & CI Guide](docs/development/testing-and-ci.md)
- [Database & Migrations Guide](docs/development/database-and-migrations.md)
- [Playbook: New Frontend Microservice](docs/playbooks/new-frontend-microservice.md)
- [Playbook: New Backend Microservice](docs/playbooks/new-backend-microservice.md)
- [Playbook: Agentic Loop Evaluation](docs/playbooks/agentic-loop-guide.md)
