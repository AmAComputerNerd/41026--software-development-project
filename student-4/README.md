# Account & Profile Microservice (`student-4`)

- **Owner**: Student 4 (Tristan Huang)
- **Status**: Scaffold / Planned for Release 1
- **Domain**: User settings, Canvas profile synchronisation, notification channels, theme customisation.

---

## 1. Planned Architecture

- **Backend**: ASP.NET Core (.NET 10) public API (port `5104`).
- **Database**: Internal ASP.NET Core + EF Core SQLite service (`5204` standalone).
- **Contracts**: Shared internal API/database transport records.
- **Frontend**: Vue 3 + TypeScript + Vite + `@better-canvas/ui-kit` (proxied at `/account/`).
- **Integration Points**:
  - Fetches authenticated user info from Canvas via `shared-backend` (`GET /api/canvas/users/self`).
  - Coordinates profile preferences with `student-1-backend` (`/notifications/preferences`).

---

## 2. Implementation Playbooks

- To implement the backend slice, follow [Playbook: New Backend Slice](../docs/playbooks/new-backend-microservice.md).
- To implement the frontend, follow [Playbook: New Frontend Microservice](../docs/playbooks/new-frontend-microservice.md).
