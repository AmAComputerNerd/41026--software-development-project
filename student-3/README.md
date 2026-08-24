# Deadline and task-tracker service

The backend manages courses, tasks, subtasks, priorities, completion states,
due dates, and task filtering. Persisted timestamps use `DateTime` normalized
to UTC.

## Standalone development

The task and course APIs can run without other services:

```powershell
dotnet run --project student-3\backend\Api
```

Cross-service features are intentionally unavailable in standalone mode.

## Canvas sync

The service obtains Canvas data exclusively through the shared backend.
Docker Compose supplies `SharedService:BaseUrl` as
`http://shared-backend:8080`, using the shared service's Compose DNS name.
Canvas sync is intentionally unavailable when this service is run standalone.

Start the integrated services and trigger a sync:

```powershell
docker compose up --build
```

```http
POST /api/canvas-sync
```

Canvas course and assignment IDs have unique database constraints. Repeated
syncs update the same course/task, including task completion when Canvas reports
a submission as submitted or graded. Other submission states do not overwrite
the task's local status. Assignments no longer returned by Canvas are retained
with `canvasIsActive: false`. Normal task and course list requests hide inactive
Canvas records unless `includeInactiveCanvas=true` is provided.
