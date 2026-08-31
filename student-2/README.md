# Automations service

The service stores configurable assignment extension and scheduled post
automations. It provides full CRUD for automation configurations and read-only
access to records of previous runs. Release 0 does not schedule or execute
automations.

The backend uses ASP.NET Core, Entity Framework Core, and its own SQLite
database. The Vue 3 and TypeScript frontend uses the shared Better Canvas UI
kit and is available through the shared shell at `/automations/`.

## Standalone development

Run the backend at `http://localhost:5102`:

```powershell
dotnet run --project student-2\backend\Api
```

Run the frontend at `http://localhost:3002/automations/`:

```powershell
npm run dev --workspace=student-2-frontend
```

Until the Accounts service is implemented, the frontend uses the fixed demo
student ID declared in `frontend/src/config.ts`.

## API

Automation CRUD is available under `/api/automations`. Read-only run history is
available under `/api/automation-runs`. Swagger is available in Development at
`http://localhost:5102/swagger`.

The initial database contains ten assignment extension configurations, ten
scheduled post configurations, and ten run records for each type.

## Integrated development

```powershell
docker compose up --build
```

The shared shell serves the frontend at `http://localhost:8080/automations/`
and proxies the API under `http://localhost:8080/api/automations/`.