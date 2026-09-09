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

# Stop and wipe database volumes (forces fresh seeding)
docker compose down -v
```

Once running, access the services:
- **Shared Dashboard**: [http://localhost:8080](http://localhost:8080)
- **Notifications**: [http://localhost:8080/notifications/](http://localhost:8080/notifications/)
- **Automations**: [http://localhost:8080/automations/](http://localhost:8080/automations/)
- **Deadlines & Tasks**: [http://localhost:8080/deadlines/](http://localhost:8080/deadlines/)
- **Account & Auth**: [http://localhost:8080/account/](http://localhost:8080/account/)
- **Grades & Progress**: [http://localhost:8080/grades/](http://localhost:8080/grades/)
- **MailHog inbox** (Student 4 password-reset emails): [http://localhost:8025](http://localhost:8025)

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
npm run dev --workspace=student-2-frontend
npm run dev --workspace=student-3-frontend
npm run dev --workspace=student-4-frontend
npm run dev --workspace=student-5-frontend
```

### C. Running a Backend & Database

#### Student 1 (Notifications)
Student 1 needs its PostgreSQL container before the backend will start. Start just that service:
```bash
docker compose up -d student-1-database
```
Then run the backend:
```bash
dotnet run --project student-1/backend/Api/Api.csproj
```

#### Student 2 (Automations)
```bash
dotnet run --project student-2/backend/Api/Api.csproj
```

#### Student 3 (Deadlines & Tasks)
Start the internal database service first, then the backend:
```bash
dotnet run --project student-3/database/Database/Database.csproj
# In another terminal:
dotnet run --project student-3/backend/Api/Api.csproj
```

#### Student 4 (Account & Authentication)
Start the database service, auth service, and backend:
```bash
dotnet run --project student-4/database/Database/Database.csproj
# In additional terminals:
dotnet run --project student-4/authentication/Authentication/Authentication.csproj
dotnet run --project student-4/backend/Api/Api.csproj
```

#### Student 5 (Grades & Progress)
Start the database service, then the backend:
```bash
dotnet run --project student-5/database/Database/Database.csproj
# In another terminal:
dotnet run --project student-5/backend/GradesManager/GradesManager/GradesManager.csproj
```

> [!IMPORTANT]
> When running a backend outside Docker:
> - Set environment variable `ASPNETCORE_ENVIRONMENT=Development`.
> - If connecting to `shared-backend` or `ai-mode`, you must provide their URLs via `appsettings.Development.json` or environment variables (e.g. `AiGateway__BaseUrl=http://localhost:8080`).
> - The standalone default for `student-3-database` is `http://localhost:5203` (matching `DatabaseService:BaseUrl` in `student-3/backend/Api/appsettings.json`), `student-4-database` is `http://localhost:5204`, and `student-5-database` is `http://localhost:5205`.
