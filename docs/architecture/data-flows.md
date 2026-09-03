# Cross-Service Data Flows & Sequence Walkthroughs

This document traces the primary end-to-end data flows and lifecycle sequences across the microservices ecosystem.

---

## 1. Canvas Assignment Sync & Task Ingestion

```
[ User / Scheduler ]
        │
        │ 1. POST /api/canvas-sync
        ▼
[ student-3-backend ]
        │
        │ 2. GET /api/canvas/courses & assignments
        ▼
[ shared-backend ]
        │
        │ 3. Fetch from Canvas LMS API
        ▼
[ Canvas LMS ]
        │
        │ 4. Return Raw JSON + HTML Descriptions
        ▼
[ shared-backend ]
        │
        │ 5. Parse & Sanitize HTML -> Plain Text
        │ 6. Cache in-memory (3-min TTL) & Write Audit Record
        ▼
[ student-3-backend ]
        │
        │ 7. Transactional Upsert Tasks (EF Core SQLite)
        │    (Existing tasks updated; missing marked inactive)
        ▼
[ Database: tasks.db ]
```

### Steps:
1. Client calls `POST /api/canvas-sync` on `student-3-backend`.
2. `student-3-backend` issues internal HTTP requests to `shared-backend` (`http://shared-backend:8080/api/canvas/*`).
3. `shared-backend` forwards request with `CANVAS_API_TOKEN` to Canvas.
4. Canvas returns course and assignment entities containing HTML descriptions.
5. `shared-backend` cleans untrusted markup, producing clean plain text.
6. `student-3-backend` performs atomic database upserts:
   - New assignments become active tasks.
   - Deleted assignments in Canvas receive `CanvasIsActive = false` (never hard-deleted).
   - Submissions graded/submitted in Canvas mark the task `IsCompleted = true`.

---

## 2. Proactive Deadline Reminder & Inline Completion

```
[ student-3 Background Service ]
        │
        │ 1. Poll tasks due within reminder window
        ▼
[ Database: tasks.db ]
        │
        │ 2. POST /notifications/push
        │    Payload: { Title, DueDate, RelatedEntityType: "Task", RelatedEntityId: 42 }
        ▼
[ student-1-backend ]
        │
        │ 3. Persist Notification (Type: Deadline)
        │ 4. Publish Event to Stream Broker
        ▼
[ NotificationStreamBroker ] ───(SSE Event: "notification")───► [ student-1-frontend ]
                                                                      │
                                                                      │ 5. Render Action Buttons
                                                                      │    [MARK COMPLETE] [AI BREAK DOWN]
                                                                      ▼
                                                              [ User clicks MARK COMPLETE ]
                                                                      │
                                                                      │ 6. PUT /api/deadlines/tasks/42
                                                                      ▼
                                                              [ student-3-backend ]
```

### Steps:
1. `DueSoonReminderBackgroundService` in `student-3` evaluates unreminded tasks due within the reminder window (e.g. 24h).
2. It sends an HTTP push request to `student-1-backend` (`POST /notifications/push`) with `RelatedEntityType = "Task"` and `RelatedEntityId = <id>`.
3. `student-1-backend` persists the notification in SQLite and stamps `DueSoonReminderSentAtUtc` on the task.
4. `student-1-backend` emits a real-time event to `NotificationStreamBroker`.
5. The user sees a toast alert and interactive action buttons on `/notifications/`.
6. Clicking `MARK COMPLETE` executes an inline `PUT /api/deadlines/tasks/{id}` request directly through Nginx to `student-3-backend`.

---

## 3. Real-Time SSE Notification Streaming

```
[ Client Browser ]
        │
        │ 1. GET /notifications/stream (Accept: text/event-stream)
        ▼
[ shared-shell (Nginx) ]
        │ (X-Accel-Buffering: no)
        ▼
[ student-1-backend (SSE Endpoint) ]
        │
        │ 2. Register Client in NotificationStreamBroker
        │ 3. Emit initial "connected" event
        ▼
[ NotificationStreamBroker ]
        │
        │ 4. New notification created / pushed
        ▼
[ Client Receives Event ] ──► Updates Bell Badge in Shell + Shows Floating Toast
```

---

## 4. Conversational AI Digest Assistant

```
[ User on /notifications/ ]
        │
        │ 1. User asks: "What assignments are due this Friday?"
        ▼
[ student-1-frontend ]
        │
        │ 2. POST /digest/chat { message: "...", history: [...] }
        ▼
[ student-1-backend ]
        │
        │ 3. Query active notifications & user preferences from SQLite
        │ 4. Format prompt with dynamic notification grounding context
        │ 5. POST /v1/chat/completions
        ▼
[ ai-mode Gateway ]
        │
        │ 6. Inject OpenRouter API Key & Call LLM
        ▼
[ OpenRouter (Nemotron) ]
        │
        │ 7. Return AI Completion
        ▼
[ student-1-backend ] ──► Returns JSON response to frontend chat interface
```

---

## 5. Cross-Service Action Triggers (`AI BREAK DOWN` & `GRADE IMPACT`)

### AI Subtask Breakdown Flow
1. User clicks `AI BREAK DOWN` on a Deadline notification.
2. `student-1-frontend` opens `BreakdownDialog.vue`.
3. Modal calls `POST /api/deadlines/tasks/{id}/ai-breakdown` on `student-3-backend`.
4. `student-3-backend` builds prompt using assignment context from `tasks.db`, calls `ai-mode`, validates generated subtasks, and saves them atomically to the task.

### Grade Impact Simulation Flow
1. User clicks `GRADE IMPACT` on a Grade notification.
2. `student-1-frontend` opens `GradeImpactDialog.vue`.
3. Modal retrieves assignment mark weightings and submits simulated scores to `PUT /api/grades/api/assignment/marks/` on `student-5-backend`.
4. Student visualizes live GPA / course percentage impact.
