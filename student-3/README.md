# Deadline and task-tracker service

The service manages courses, tasks, subtasks, priorities, completion states,
due dates, and task filtering. Its Vue frontend provides task-list,
monthly calendar, upcoming-task, and Canvas assignment planning views using
the shared Better Canvas UI kit. Persisted timestamps use `DateTime` normalized
to UTC. Due dates can be changed or removed explicitly; completing a parent
task also completes all of its descendants.

AI planning calls the internal `ai-mode` gateway through the task-tracker
backend. Assignment prompt templates remain editable, but trusted assignment
and course context is loaded from the tracker database. Generated plans are
validated and saved as one operation. Task descriptions can also be drafted
from a title and the selected course or parent assessment, then edited before
saving.

## Standalone development

The task and course APIs can run without other services:

```powershell
dotnet run --project student-3\backend\Api
```

In a second terminal, run the frontend at
`http://localhost:3003/deadlines/`:

```powershell
npm run dev --workspace=student-3-frontend
```

The frontend calls the published standalone API on port 5103. Canvas sync is
unavailable unless the shared backend is also configured.

## Canvas sync

The service obtains Canvas data exclusively through the shared backend.
Docker Compose supplies `SharedService:BaseUrl` as
`http://shared-backend:8080`, using the shared service's Compose DNS name.
Canvas sync is intentionally unavailable when this service is run standalone.
AI generation is also unavailable unless `AiGateway:BaseUrl` points to a
running `ai-mode` gateway.

Start the integrated services and trigger a sync:

```powershell
docker compose up --build
```

The shared shell is then available at `http://localhost:8080/`, the task
tracker at `http://localhost:8080/deadlines/`, and its proxied API under
`/api/deadlines/`. The shell dashboard also shows the next five incomplete
tasks by due date and priority.

```http
POST /api/canvas-sync
```

Canvas course and assignment IDs have unique database constraints. Repeated
syncs update the same course/task, including task completion when Canvas reports
a submission as submitted or graded. Other submission states do not overwrite
the task's local status. Assignments no longer returned by Canvas are retained
with `canvasIsActive: false`. Normal task and course list requests hide inactive
Canvas records unless `includeInactiveCanvas=true` is provided.

Canvas assignment descriptions are converted from untrusted HTML to readable
plain text by the shared Canvas service before they reach the tracker. Paragraph,
line-break, list, table, and image-alt text is preserved; executable and embedded
content is discarded. The tracker never renders Canvas HTML.
