# Project Notes for Claude & AI Agents

This is a 5-student microservices project (ASD unit, Release 0).
Keep `docs/architecture/overview.md` up to date with what's actually built.

> **Universal Agent Guide**: Refer to [`AGENTS.md`](AGENTS.md) for full system specifications, port matrices, and CLI commands.
> **Documentation Hub**: Refer to [`docs/README.md`](docs/README.md) for architecture, runbooks, and playbooks.

---

## 1. Golden Architectural Rules

1. **Database Boundaries**: Each student's `student-N/backend` owns its own isolated SQLite database. **Zero cross-database queries**; all communication is via HTTP APIs.
2. **Canvas Gateway**: `shared/backend` exclusively owns Canvas API communication (`courses`, `assignments`, `users`). Assignments' untrusted HTML is sanitized into plain text at the gateway.
3. **AI Gateway (`ai-mode`)**: Only `ai-mode` holds `OPENROUTER_API_KEY`. All microservices call `http://ai-mode:8080/v1/chat/completions`.
4. **Design System**: Use `@better-canvas/ui-kit` (workspace package) with Vue 3 `<script setup>` and plain SCSS. Follow Neobrutalism tokens (`0px` radius, thick borders, raw drop shadows). No Vuetify in new slices.
5. **Git Workflow**: Branch off `main` per feature (`feat/`, `fix/`, `docs/`), use Conventional Commits, and open PRs into `main`.

---

## 2. Ports & Routing Cheatsheet

- `http://localhost:8080` — Shared Shell (Nginx Dashboard + Reverse Proxy)
- `http://localhost:8080/notifications/` / API: `5101` (`/api/notifications/`) — Student 1 (Notifications)
- `http://localhost:8080/deadlines/` / API: `5103` (`/api/deadlines/`) — Student 3 (Deadlines & Tasks)
- `http://localhost:8080/grades/` / API: `5105` (`/api/grades/`) — Student 5 (Grades & Progress)
- API: `5110` (`/api/canvas/*`) — Shared Backend (Canvas Gateway)
- Internal `8080` (`/v1/chat/completions`) — AI Mode (OpenRouter Gateway)

---

## 3. Key CLI Commands

```bash
# Docker Stack
docker compose up --build
docker compose down -v

# Backend (.NET 10)
dotnet build
dotnet test
dotnet format --verify-no-changes
dotnet ef migrations add <Name> --project Api/Api.csproj

# Frontend (npm workspaces)
npm run dev --workspace=shared-frontend
npm run dev --workspace=student-1-frontend
npm run build --workspaces
```

## 4. Frontend Conventions

Follow `docs/playbooks/new-frontend-microservice.md`.

Short version: Vue 3, no Vuetify, plain SCSS. Depend on
`@better-canvas/ui-kit` (workspace package) for tokens, fonts, and shared
components (Navbar, etc.) instead of writing your own. Add an nginx
proxy block in `shared/frontend/nginx.conf`, add your service to the root
`docker-compose.yml`, and enable its tile in `shared/ui-kit/src/services.ts`.

## 5. Git Conventions

- Branch off `main` per feature, don't stack feature branches on other
  open feature branches.
- Commit after each logical step, not one giant commit at the end, push
  as you go.
- Open a PR into `main` when done, don't push directly to main.
