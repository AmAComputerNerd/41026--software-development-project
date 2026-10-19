# Getting Started & Local Development Runbook

This guide covers local environment setup, configuration, and running the microservices stack in Docker Compose or standalone development mode.

---

## 1. Prerequisites

Ensure you have the following installed locally:

- **Docker Desktop** (or equivalent container runtime supporting Docker Compose v2)
- **Node.js 22 LTS** (for standalone frontend development and npm workspace tooling)
- **.NET 10 SDK** (for standalone ASP.NET Core backend development)
- **Python 3.11+** (for environment setup and the agentic loop)
- **Git**

---

## 2. Environment Configuration

1. Run the environment setup from the repository root:
   ```bash
   python tools/setup_env.py
   ```
2. Enter the requested OpenRouter API key, Canvas base URL, Canvas API
   token, and PostgreSQL password. Press Enter at the PostgreSQL prompt to
   generate a random password for a new database. Secret inputs are hidden.
   Passwords must use letters, digits, underscores, or hyphens so they can
   be safely embedded in the backend connection string. Other values are copied from
   `.env.example`.

The command refuses to modify an existing `.env`. Run
`python tools/setup_env.py --force` only when you intentionally want to
replace it.
When replacing the file, Enter at the PostgreSQL prompt preserves its
existing `POSTGRES_PASSWORD` value.

> [!TIP]
> If you do not have a Canvas API token during development, backend and frontend services will still run, but Canvas sync endpoints (`/api/canvas-sync`) will return error responses.
> For AI features, a valid `OPENROUTER_API_KEY` and `OPENROUTER_MODEL` are required; otherwise, AI Mode readiness fails.

---

## 3. Option A: Running with Docker Compose (Recommended)

Docker Compose builds and networks the core application. AI Mode, MCP, RAG
and Multi-Agent run as non-containerised host processes in a second terminal:

```bash
python tools/run_ai_services.py
```

The launcher requires `AI_MODE_HOST_PORT`, `MCP_HOST_PORT`, `RAG_HOST_PORT`
and `MULTI_AGENT_HOST_PORT`; `.env.example` supplies `5001`, `5002`, `5003`
and `5004`. Add missing entries to an existing `.env` without replacing its
secrets. It loads the OpenRouter key only into AI Mode and stages a temporary
RAG corpus. It listens on all host interfaces for Docker access; restrict
these development ports with your host firewall.

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
> - If connecting to `shared-backend`, provide its URL via `appsettings.Development.json` or environment variables (e.g. `SharedService__BaseUrl=http://127.0.0.1:5110`). Use loopback URLs on the configured host ports for AI Mode, MCP and RAG when the backend also runs outside Docker.
> - The standalone default for `student-3-database` is `http://localhost:5203` (matching `DatabaseService:BaseUrl` in `student-3/backend/Api/appsettings.json`), `student-4-database` is `http://localhost:5204`, and `student-5-database` is `http://localhost:5205`.

### AI services

Run `python tools/run_ai_services.py` outside Docker. Compose supplies
`host.docker.internal` URLs and Linux host-gateway mappings to all five
calling backends. MCP calls those backends through published ports
`5101`-`5105`. Inspect host readiness with
`curl --fail http://127.0.0.1:5001/health/ready` (use the configured MCP,
RAG or Multi-Agent port as appropriate). Restart the launcher after corpus
edits to refresh RAG's temporary documents and startup index.

To migrate an existing local stack, stop the old launcher with Ctrl+C and
stop/remove the old project's containers without deleting database volumes:
`docker compose down --remove-orphans` (**do not add `--volumes`**).
Then run `docker compose up --build -d` and start the host launcher in
another terminal. No database migration or volume reset is needed.
Rollback consists of stopping the host launcher, restoring the previous
Compose/launcher configuration and recreating the old containers, retaining
database volumes. Dockerfiles and Azure support remain for optional cloud
bonus deployment; they are not part of the required local stack.
