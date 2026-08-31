# 41026 - Advanced Software Development; Project
An LLM-enhanced web application utilising the Canvas Infrastructure API.

## Students
**Student 1: Bryan Lee (25495108).**  
Working directory: `student-1/`  
Notifications service: manages student notifications (deadlines, grades,
automation, account, and AI-sourced) with read/unread state, per-student
delivery preferences by notification type and channel (in-app or email),
and AI-generated digests summarising a student's recent notification
activity.

**Student 2: Isaac Thomas (25341708).**

Working directory: `student-2/`

Automations service: configures assignment extension and scheduled post
automations, stores each type in its own Entity Framework table, and provides
read-only records of previous runs. Release 0 stores configuration only and
does not execute automations.

**Student 3: Jonathon Thomson (25488154).**  
Working directory: `student-3/`  
Deadline and task-tracker service: manages courses and coursework tasks,
including priorities, completion states, due dates, subtasks, filtering, and
Canvas assignment imports through the shared backend. Canvas sync keeps one
primary task per assignment and updates it on later imports without storing a
separate assessment table.

**Student 4: Tristan Huang (STUDENT-NUM).**  
Working directory: `student-4/`  
TODO: Short summary of microservice, other info

**Student 5: William Hannah (STUDENT-NUM).**  
Working directory: `student-5/`  
TODO: Short summary of microservice, other info

## Setup

Copy `.env.example` to `.env`. Set `OPENROUTER_API_KEY` (get one at
https://openrouter.ai/keys), `CANVAS_BASE_URL` to the root URL of your Canvas
instance, and `CANVAS_API_TOKEN` to a personal Canvas access token.
`docker-compose.yml` injects these values into the services that own each
integration.

```bash
cp .env.example .env
# edit .env and paste your key
docker compose up
```

Individual services can run standalone for local development. Cross-service
features are intentionally supported only through Docker Compose; see each
service's README for its standalone limitations.

## Service communication

Microservices communicate over HTTP and own separate databases. They must not
query another service's Entity Framework database.

The shared backend owns Canvas authentication and API pagination. The deadline
and task-tracker backend receives `SharedService:BaseUrl` through standard
ASP.NET configuration. Docker Compose supplies `http://shared-backend:8080`,
where `shared-backend` is resolved by Compose's internal DNS. Cross-service
integration is intentionally available only through Docker Compose; standalone
services do not receive addresses for other services.

To import Canvas data, start the services and call:

```http
POST http://localhost:5103/api/canvas-sync
```

The sync fetches active courses and their assignments, then transactionally
upserts one task per stable Canvas assignment ID. Removed assignments are
marked inactive rather than deleted. Canvas data remains live in the shared
service; only the source IDs and fields needed by courses/tasks are persisted
by the task tracker. A submitted or graded assignment marks its task as
completed; other Canvas submission states do not overwrite the task's local
status.

The shared Canvas and task-tracker databases persist timestamps as `DateTime`
normalized to UTC.

## Release 0: Summary
Working branch: `main`  
Feature set:  
- Shared dashboard shell and UI kit.
- Notification preferences, notification management, and AI digests.
- Assignment extension and scheduled post automation configuration and run history.
- Deadline/task CRUD, course linkage, filtering, and Canvas synchronization.
- Shared Canvas API gateway, audit database, Docker image, and CI workflow.
