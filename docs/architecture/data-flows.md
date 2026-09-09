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
        │ 7. POST /internal/canvas-snapshots
        ▼
[ student-3-database ]
        │
        │ 8. Transactional Upsert Tasks (EF Core SQLite)
        │    (Existing tasks updated; missing marked inactive)
        ▼
[ Database: /app/Data/app.db ]
```

### Steps:
1. Client calls `POST /api/canvas-sync` on `student-3-backend`.
2. `student-3-backend` issues internal HTTP requests to `shared-backend` (`http://shared-backend:8080/api/canvas/*`).
3. `shared-backend` forwards request with `CANVAS_API_TOKEN` to Canvas.
4. Canvas returns course and assignment entities containing HTML descriptions.
5. `shared-backend` cleans untrusted markup, producing clean plain text.
6. `student-3-backend` validates and submits one complete snapshot to `student-3-database`.
7. `student-3-database` performs atomic database upserts:
   - New assignments become active tasks.
   - Deleted assignments in Canvas receive `CanvasIsActive = false` (never hard-deleted).
   - Submissions graded/submitted in Canvas mark the task `IsCompleted = true`.

---

## 2. Proactive Deadline Reminders & Task Push Notifications

```
[ student-3-backend (Task Creation / Worker) ]
        │
        │ 1. Task created or due reminder detected
        │ 2. POST /notifications/push
        │    Payload: { Title, Type: "Deadline", Source: "deadlines", RelatedEntityId: task.Id }
        ▼
[ student-1-backend ]
        │
        │ 3. Insert notification into PostgreSQL (notifications_db)
        │ 4. Publish Event to NotificationStreamBroker
        ▼
[ NotificationStreamBroker ] ───(SSE Event: "notification")───► [ student-1-frontend ]
                                                                      │
                                                                      │ 5. Render Action Buttons
                                                                      │    [MARK COMPLETE] [AI BREAK DOWN]
                                                                      ▼
                                                              [ User clicks MARK COMPLETE ]
                                                                      │
                                                                      │ 6. PUT /api/deadlines/tasks/{id}
                                                                      ▼
                                                              [ student-3-backend ]
                                                                      │
                                                                      │ 7. PUT /internal/tasks/{id}
                                                                      ▼
                                                              [ student-3-database ]
```

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
        │ 3. Query active notifications & user preferences from PostgreSQL
        │ 4. Format prompt with dynamic notification grounding context
        │ 5. POST /v1/chat/completions
        ▼
[ ai-mode Gateway ]
        │
        │ 6. Inject OpenRouter API Key & Call LLM
        ▼
[ OpenRouter (MiniMax-M3) ]
        │
        │ 7. Return AI Completion
        ▼
[ student-1-backend ] ──► Returns JSON response to frontend chat interface
```

---

## 5. Automated Execution Flow (Scheduled Canvas Posts & AI Quiz Filling)

```
[ student-2-backend Periodic Worker (every 30s) ]
        │
        │ 1. Check enabled automations from SQLite
        │ 2. Generate immutable candidate execution key
        │ 3. Atomically record run state as RUN
        ▼
┌───────────────────────────────────────┴───────────────────────────────────────┐
│ Scheduled Post Flow                           Quiz Filler Flow                │
│                                                                               │
│ 4a. POST /api/canvas/conversations            4b. POST /api/canvas/.../quiz   │
│     (via shared-backend)                          submission start            │
│ 5a. Canvas Conversation Created               5b. GET quiz questions          │
│                                               6b. Call ai-mode for answers    │
│                                               7b. POST draft answers to quiz  │
│                                                   (submission left in draft)  │
└───────────────────────────────────────┬───────────────────────────────────────┘
                                        │
                                        │ 8. Mark run state SUC / FAI in SQLite
                                        ▼
                               [ student-2-frontend ]
                               (Displays updated run history)
```

---

## 6. Account Password Reset Flow (MailHog SMTP)

```
[ User on /account/forgot-password ]
        │
        │ 1. POST /api/auth/forgot-password { email: "student@example.edu" }
        ▼
[ student-4-authentication ]
        │
        │ 2. Generate cryptographic reset token
        │ 3. Dispatch SMTP message to mailhog:1025
        ▼
[ MailHog (SMTP Server) ] ──► Email available at http://localhost:8025
        │
        │ 4. User copies reset token or clicks email link
        ▼
[ User on /account/reset-password ]
        │
        │ 5. POST /api/auth/reset-password { token: "...", newPassword: "..." }
        ▼
[ student-4-authentication ]
        │
        │ 6. Verify token & update password hash in student-4-database
        ▼
[ student-4-database (EF Core SQLite) ]
```

---

## 7. Cross-Service Action Triggers (`AI BREAK DOWN` & `GRADE IMPACT`)

### AI Subtask Breakdown Flow
1. User clicks `AI BREAK DOWN` on a Deadline notification.
2. `student-1-frontend` opens `BreakdownDialog.vue`.
3. Modal calls `POST /api/deadlines/tasks/{id}/ai-breakdown` on `student-3-backend`.
4. `student-3-backend` loads assignment context through `student-3-database`, calls `ai-mode`, validates generated subtasks, and sends one bulk command back to the database service for atomic persistence.

### Grade Impact Simulation Flow
1. User clicks `GRADE IMPACT` on a Grade notification.
2. `student-1-frontend` opens `GradeImpactDialog.vue`.
3. Modal retrieves assignment mark weightings and submits simulated scores to `PUT /api/grades/api/assignment/marks/` on `student-5-backend` (which persists via `student-5-database`).
4. Student visualizes live GPA / course percentage impact.

