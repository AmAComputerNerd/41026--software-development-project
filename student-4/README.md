# Account & Profile Microservice (`student-4`)

- **Owner**: Student 4 (Tristan Huang)
- **Status**: Scaffold / Planned for Release 1
- **Domain**: User settings, Canvas profile synchronisation, notification channels, theme customisation.

---

## 1. Planned Architecture

- **Backend**: ASP.NET Core (.NET 10) Minimal API + EF Core SQLite (port `5104`).
- **Frontend**: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit` (proxied at `/account/`).
- **Integration Points**:
  - Fetches authenticated user info from Canvas via `shared-backend` (`GET /api/canvas/users/self`).
  - Coordinates profile preferences with `student-1-backend` (`/notifications/preferences`).

---

## 2. Implementation Playbooks

- To implement the backend, follow [Playbook: New Backend Microservice](../docs/playbooks/new-backend-microservice.md).
- To implement the frontend, follow [Playbook: New Frontend Microservice](../docs/playbooks/new-frontend-microservice.md).
