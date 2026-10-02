# Shared Frontend & Dashboard Shell (`shared/frontend`)

> **Role**: Unified Application Shell & Nginx Reverse Proxy  
> **Tech Stack**: Vue 3, TypeScript, Vite, Nginx (Alpine), `@better-canvas/ui-kit`  
> **Host Port**: `8080` (maps to port `80` in container)

---

## 1. Overview & Routing Architecture

`shared-shell` serves as the single public entry point for the Better Canvas platform. It hosts the dashboard overview at `/` and reverse-proxies all microservice frontends and APIs through an embedded Nginx configuration.

```mermaid
flowchart TD
    Client["Browser / Client"] -- "HTTP :8080" --> Nginx["Nginx Reverse Proxy (shared-shell)"]

    subgraph StaticAndShell["Shell SPA"]
        Dashboard["Dashboard Shell (Vue 3)<br>/"]
        Bell["Live SSE Unread Bell Badge"]
    end

    subgraph Frontends["Proxied Frontends"]
        F1["/notifications/ -> student-1-frontend:80"]
        F2["/automations/ -> student-2-frontend:80"]
        F3["/deadlines/ -> student-3-frontend:80"]
        F4["/account/ -> student-4-frontend:80"]
        F5["/grades/ -> student-5-frontend:80"]
    end

    subgraph Backends["Proxied APIs"]
        B1["/api/notifications/ -> student-1-backend:8080"]
        B2["/api/automations/ -> student-2-backend:8080/api/"]
        B3["/api/deadlines/ -> student-3-backend:8080/api/"]
        B4A["/api/auth/ -> student-4-authentication:8080/"]
        B4B["/api/users/, students, teachers -> student-4-backend:8080/"]
        B5["/api/grades/ -> student-5-backend:8080/"]
    end

    Nginx --> Dashboard
    Dashboard --> Bell
    Bell -. "SSE: /notifications/stream" .-> B1
    Nginx --> F1 & F2 & F3 & F4 & F5
    Nginx --> B1 & B2 & B3 & B4A & B4B & B5
```

---

## 2. Running Locally

```bash
# Start frontend shell dev server (standalone)
npm run dev --workspace=shared-frontend

# Build for production (embedded in Nginx container)
npm run build --workspace=shared-frontend
```
