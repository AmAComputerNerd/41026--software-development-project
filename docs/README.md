# Better Canvas — System Documentation Library

Welcome to the documentation suite for the **Better Canvas** microservices web platform (41026 Software Development Project).

---

## Documentation Index

### 1. Architecture Specifications
- [**Architecture Overview**](architecture/overview.md) — System boundaries, microservices topology, network architecture, and security policies.
- [**Microservices Catalog**](architecture/services.md) — Service-by-service specification, technology stacks, endpoints, ownership, and port allocations.
- [**Data Flows & Sequences**](architecture/data-flows.md) — Sequence diagrams detailing Canvas ingestion, AI processing, SSE notifications, auth workflows, and cross-service actions.

### 2. Implementation Playbooks
- [**Creating a New Frontend Microservice**](playbooks/new-frontend-microservice.md) — Step-by-step guide for scaffolding, styling with `@better-canvas/ui-kit`, configuring Vite proxying, and registering routes in Nginx.
- [**Building a New Backend Microservice**](playbooks/new-backend-microservice.md) — Guide for ASP.NET Core minimal APIs, database configuration, Dockerfile setup, and health check endpoints.
- [**Splitting API and Database Services**](playbooks/split-database-service.md) — Architectural pattern and step-by-step instructions for separating public APIs from private database persistence microservices.
- [**Agentic Loop Evaluation Guide**](playbooks/agentic-loop-guide.md) — Running and interpreting automated multi-agent code analysis and architectural compliance checks.

### 3. Development Runbooks
- [**Getting Started & Local Setup**](development/getting-started.md) — Step-by-step setup for Docker Compose, standalone .NET backends, standalone Vue frontends, and environment variables.
- [**Testing & CI Guide**](development/testing-and-ci.md) — How to run unit tests, type-checks, linters, format verification, and understand GitHub Actions CI workflows.
- [**Database & Migrations Guide**](development/database-and-migrations.md) — Multi-database architecture (PostgreSQL and dedicated internal SQLite database services), migrations lifecycle, and seeding.

---

## Team & Slice Quick Links

- [**`shared/backend/README.md`**](../shared/backend/README.md) — Canvas LMS API Gateway & Cache.
- [**`shared/frontend/README.md`**](../shared/frontend/README.md) — Dashboard Shell & Nginx Reverse Proxy.
- [**`shared/ui-kit/README.md`**](../shared/ui-kit/README.md) — `@better-canvas/ui-kit` Design Tokens & Components.
- [**`ai-services/ai-mode/README.md`**](../ai-services/ai-mode/README.md) — OpenRouter AI Gateway.
- [**`student-1/backend/README.md`**](../student-1/backend/README.md), [**`student-1/database/README.md`**](../student-1/database/README.md) & [**`student-1/frontend/README.md`**](../student-1/frontend/README.md) — Notifications microservice.
- [**`student-2/README.md`**](../student-2/README.md) — Automations microservice.
- [**`student-3/README.md`**](../student-3/README.md) — Deadlines & Task Tracker microservice.
- [**`student-4/README.md`**](../student-4/README.md) — Account & Authentication microservice.
- [**`student-5/backend/README.md`**](../student-5/backend/README.md) & [**`student-5/frontend/README.md`**](../student-5/frontend/README.md) — Grades & Progress microservice.
