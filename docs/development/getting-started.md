# Getting Started & Local Development Runbook

This guide covers local environment setup, configuration, and running the microservices stack in Docker Compose or standalone development mode.

---

## 1. Prerequisites

Ensure you have the following installed locally:

- **Docker Desktop** (or equivalent container runtime supporting Docker Compose v2)
- **Node.js 22 LTS** (for standalone frontend development and npm workspace tooling)
- **.NET 10 SDK** (for standalone ASP.NET Core backend development)
- **Git**

---

## 2. Environment Configuration

1. Copy the `.env.example` template to `.env` in the repository root:
   ```bash
   cp .env.example .env
   ```
2. Edit `.env` and configure your API credentials:
   ```dotenv
   # OpenRouter API Key for AI features (ai-mode gateway)
   OPENROUTER_API_KEY=sk-or-v1-xxxxxxxxxxxxxxxxxxxx

   # Canvas LMS credentials (shared-backend)
   CANVAS_BASE_URL=https://your-institution.instructure.com
   CANVAS_API_TOKEN=your-personal-canvas-access-token
   ```

> [!TIP]
> If you do not have a Canvas API token during development, backend and frontend services will still run, but Canvas sync endpoints (`/api/canvas-sync`) will return error responses.
> For AI features, a valid `OPENROUTER_API_KEY` is required; otherwise, AI calls will return HTTP 500.

---

## 3. Option A: Running with Docker Compose (Recommended)

Running via Docker Compose builds and networks all microservices together automatically, setting up internal DNS and volume persistence.

```bash
# Build and start all services
docker compose up --build

# Run in background (detached mode)
docker compose up -d

# View logs from all services or a specific service
docker compose logs -f
docker compose logs -f student-1-backend

# Stop all services
docker compose down

# Stop and wipe SQLite database volumes (forces fresh seeding)
docker compose down -v
```

Once running, access the services:
- **Shared Dashboard**: [http://localhost:8080](http://localhost:8080)
- **Notifications**: [http://localhost:8080/notifications/](http://localhost:8080/notifications/)
- **Deadlines & Tasks**: [http://localhost:8080/deadlines/](http://localhost:8080/deadlines/)
- **Grades & Progress**: [http://localhost:8080/grades/](http://localhost:8080/grades/)

---

## 4. Option B: Standalone Development (Iterative Mode)

You can run individual services outside Docker for rapid iteration and debugging.

### A. Installing Workspace Dependencies
From the repository root:
```bash
npm install
```

### B. Running a Frontend
To run any Vue frontend using Vite:
```bash
# Run shared shell
npm run dev --workspace=shared-frontend

# Run student frontends
npm run dev --workspace=student-1-frontend
npm run dev --workspace=student-3-frontend
npm run dev --workspace=student-5-frontend
```

### C. Running a Backend
Navigate to the backend project and run:
```bash
# Example: Running student-1 notification backend
cd student-1/backend/Api
dotnet run
```

Student 3 requires its database service to start first:

```bash
dotnet run --project student-3/database/Database/Database.csproj
# In another terminal:
dotnet run --project student-3/backend/Api/Api.csproj
```

The standalone defaults are `http://localhost:5203` for
`student-3-database` and `http://localhost:5103` for
`student-3-backend`. Docker Compose keeps the database service internal.

> [!IMPORTANT]
> When running a backend outside Docker:
> - Set environment variable `ASPNETCORE_ENVIRONMENT=Development`.
> - If connecting to `shared-backend` or `ai-mode`, you must provide their URLs via `appsettings.Development.json` or environment variables (e.g. `AiGateway__BaseUrl=http://localhost:8080`).
