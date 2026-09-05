# Documentation Index & Architecture Hub

Welcome to the documentation suite for the **41026 Advanced Software Development Project**. This directory contains detailed guides for both human engineers and AI coding agents to navigate, develop, test, and contribute to the microservices ecosystem.

---

## 🧭 Navigation Map

### 1. Universal Agent Entry Points
- [**`AGENTS.md`**](../AGENTS.md) — The universal AI agent specification, golden rules, CLI cheat-sheet, and port maps.
- [**`CLAUDE.md`**](../CLAUDE.md) — High-density quick-reference guide for Claude / prompt-based tools.
- [**`CONTRIBUTING.md`**](../CONTRIBUTING.md) — Team git conventions, branch workflows, and PR standards.

---

### 2. Architecture & Design
- [**Architecture Overview**](architecture/overview.md) — High-level system architecture, service topology, reverse proxy routing, and technology stack.
- [**Microservices Catalog**](architecture/services.md) — In-depth breakdown of every service (`shared-shell`, `shared-backend`, `ai-mode`, and `student-1` through `student-5`), endpoints, data models, and configurations.
- [**Data Flows & Sequences**](architecture/data-flows.md) — Sequence diagrams and trace walkthroughs for Canvas sync, SSE real-time streaming, due-soon reminders, AI digests, and cross-service actions.

---

### 3. Development Runbooks
- [**Getting Started & Local Setup**](development/getting-started.md) — Step-by-step setup for Docker Compose, standalone .NET backends, standalone Vue frontends, and environment variables.
- [**Testing & CI Guide**](development/testing-and-ci.md) — How to run unit tests, type-checks, linters, format verification, and understand GitHub Actions CI workflows.
- [**Database & Migrations Guide**](development/database-and-migrations.md) — Entity Framework Core SQLite database isolation, migrations lifecycle, schema seeding, and queries.

---

### 4. Playbooks & Workflows
- [**Playbook: New Frontend Microservice**](playbooks/new-frontend-microservice.md) — How to scaffold a new Vue 3 frontend, integrate `@better-canvas/ui-kit`, configure Nginx proxying, and register dashboard tiles.
- [**Playbook: New Backend Microservice**](playbooks/new-backend-microservice.md) — How to scaffold a new ASP.NET Core minimal API microservice, configure SQLite EF Core, CORS, Dockerfile, and docker-compose.
- [**Playbook: Agentic Review Loop**](playbooks/agentic-loop-guide.md) — Guide to using `tools/agentic_loop`, adding custom feature context prompts, and running automated code evaluations.

---

### 5. Microservice Subsystem Documentation
- [**`shared/ui-kit/README.md`**](../shared/ui-kit/README.md) — `@better-canvas/ui-kit` design tokens, Neobrutalism CSS primitives, and components.
- [**`shared/README.md`**](../shared/README.md) — Shared backend Canvas gateway and dashboard shell.
- [**`ai-services/ai-mode/README.md`**](../ai-services/ai-mode/README.md) — Shared OpenRouter AI gateway.
- [**`student-1/backend/README.md`**](../student-1/backend/README.md) & [**`student-1/frontend/README.md`**](../student-1/frontend/README.md) — Notifications microservice.
- [**`student-2/README.md`**](../student-2/README.md) — Automations microservice blueprint.
- [**`student-3/README.md`**](../student-3/README.md) — Deadlines & Task Tracker microservice.
- [**`student-4/README.md`**](../student-4/README.md) — Account microservice blueprint.
- [**`student-5/backend/README.md`**](../student-5/backend/README.md) & [**`student-5/frontend/README.md`**](../student-5/frontend/README.md) — Grades & Progress microservice.
