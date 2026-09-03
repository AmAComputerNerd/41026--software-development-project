# Automations Microservice (`student-2`)

- **Owner**: Student 2 (Isaac Thomas)
- **Status**: Scaffold / Planned for Release 1
- **Domain**: Workflow automation, trigger-action rules, recurring Canvas sync jobs, scheduled actions.

---

## 1. Planned Architecture

- **Backend**: ASP.NET Core (.NET 10) Minimal API + EF Core SQLite (port `5102`).
- **Frontend**: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit` (proxied at `/automations/`).
- **Integration Points**:
  - Triggers on Canvas events via `shared-backend`.
  - Dispatches automated notifications to `student-1-backend` (`POST /notifications/push`).
  - Calls `ai-mode` (`http://ai-mode:8080/v1/chat/completions`) for intelligent rule matching.

---

## 2. Implementation Playbooks

- To implement the backend, follow [Playbook: New Backend Microservice](../docs/playbooks/new-backend-microservice.md).
- To implement the frontend, follow [Playbook: New Frontend Microservice](../docs/playbooks/new-frontend-microservice.md).
