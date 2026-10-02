# Cross-Service Data Flows & Sequence Walkthroughs

This document traces the primary end-to-end data flows and lifecycle sequences across the microservices ecosystem.

---

## 1. Canvas Assignment Sync & Task Ingestion

```mermaid
sequenceDiagram
    autonumber
    actor User as User / Scheduler
    participant S3B as student-3-backend
    participant SB as shared-backend
    participant Canvas as Canvas LMS API
    participant S3DB as student-3-database (SQLite)

    User->>S3B: POST /api/canvas-sync
    S3B->>SB: GET /api/canvas/courses & assignments
    SB->>Canvas: Fetch courses & assignments (Bearer token)
    Canvas-->>SB: Return raw JSON + HTML descriptions
    SB->>SB: Parse & sanitize HTML to plain text
    SB->>SB: Cache in-memory (3-min TTL) & log audit record
    SB-->>S3B: Return sanitized assignments & courses
    S3B->>S3DB: POST /internal/canvas-snapshots
    S3DB->>S3DB: Atomic transactional upsert (EF Core SQLite)
    S3DB-->>S3B: Snapshot persisted
    S3B-->>User: Sync complete (200 OK)
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

## 2. Cross-Microservice Push Notifications & Action Triggers

All vertical slice microservices link to `student-1-backend` (`NotificationService`) via resilient HTTP clients pushing event notifications to `POST /notifications/push`:

```mermaid
sequenceDiagram
    autonumber
    participant Sources as Microservice Backends<br>(S2, S3, S4, S5)
    participant S1B as student-1-backend
    participant DB as student-1-database (PostgreSQL)
    participant Broker as NotificationStreamBroker
    actor Client as Client / Frontends / Nav Bell

    Sources->>S1B: POST /notifications/push<br>{StudentId, Type, SourceMicroservice, Message, ActionPayload}
    S1B->>DB: Insert notification (notifications_db)
    S1B->>Broker: Publish event to active subscribers
    Broker-->>Client: SSE Event ("notification")
    Client->>Client: Render badge count & action buttons<br>[AI BREAK DOWN / GRADE IMPACT / MARK COMPLETE]
    Client->>Sources: User clicks action -> triggers target microservice API
```

---

## 3. Real-Time SSE Notification Streaming

```mermaid
sequenceDiagram
    autonumber
    actor Browser as Client Browser
    participant Shell as shared-shell (Nginx)
    participant S1B as student-1-backend
    participant Broker as NotificationStreamBroker

    Browser->>Shell: GET /notifications/stream (Accept: text/event-stream)
    Shell->>S1B: Proxy SSE request (X-Accel-Buffering: no)
    S1B->>Broker: Register client HTTP response stream
    Broker-->>Browser: Emit initial "connected" event
    Note over Broker,Browser: Live event broadcast loop
    Broker-->>Browser: SSE event: notification payload
    Browser->>Browser: Update bell badge in shell & render floating toast
```

---

## 4. Conversational AI Digest Assistant

```mermaid
sequenceDiagram
    autonumber
    actor User as User on /notifications/
    participant S1F as student-1-frontend
    participant S1B as student-1-backend
    participant DB as student-1-database (PostgreSQL)
    participant AIM as ai-mode Gateway (:5001)
    participant OpenRouter as OpenRouter API

    User->>S1F: Asks question ("What assignments are due this Friday?")
    S1F->>S1B: POST /digest/chat { message, history }
    S1B->>DB: Query active notifications & user preferences
    S1B->>S1B: Format system prompt with grounded context
    S1B->>AIM: POST /v1/chat/completions
    AIM->>OpenRouter: Forward request with OPENROUTER_API_KEY
    OpenRouter-->>AIM: Return AI completion
    AIM-->>S1B: Return completion JSON
    S1B-->>S1F: Return assistant response
    S1F-->>User: Display AI answer with citations
```

---

## 5. Automated Execution Flow (Scheduled Canvas Posts & AI Quiz Filling)

```mermaid
flowchart TD
    Worker["student-2-backend Periodic Worker (every 30s)"] --> Check["1. Check enabled automations in SQLite"]
    Check --> Key["2. Generate deterministic execution key"]
    Key --> Claim["3. Atomically record run state as RUN in SQLite"]
    Claim --> Split{"Automation Type?"}

    subgraph PostFlow["Scheduled Post Flow"]
        P1["4a. POST /api/canvas/conversations (via shared-backend)"] --> P2["5a. Canvas Conversation Created"]
    end

    subgraph QuizFlow["AI Quiz Filler Flow"]
        Q1["4b. POST /api/canvas/.../quiz submission start"] --> Q2["5b. GET quiz questions"]
        Q2 --> Q3["6b. Call ai-mode for structured answers"]
        Q3 --> Q4["7b. POST draft answers (submission kept in draft)"]
    end

    Split -- "scheduledPost" --> PostFlow
    Split -- "quizFiller" --> QuizFlow

    PostFlow --> Result["8. Record result (SUC / FAI) in SQLite"]
    QuizFlow --> Result
    Result --> Frontend["student-2-frontend displays updated run history"]
```

---

## 6. Account Password Reset Flow (MailHog SMTP)

```mermaid
sequenceDiagram
    autonumber
    actor User as User on /account/forgot-password
    participant S4A as student-4-authentication
    participant MailHog as MailHog (SMTP :1025 / Web :8025)
    participant S4DB as student-4-database (SQLite)

    User->>S4A: POST /api/auth/forgot-password { email }
    S4A->>S4A: Generate cryptographic reset token
    S4A->>MailHog: Dispatch SMTP message with token link
    MailHog-->>User: Email arrives in MailHog web inbox (:8025)
    User->>S4A: POST /api/auth/reset-password { token, newPassword }
    S4A->>S4A: Verify token validity
    S4A->>S4DB: Update password hash in database
    S4DB-->>S4A: Password updated
    S4A-->>User: Reset successful (200 OK)
```

---

## 7. Cross-Service Action Triggers (`AI BREAK DOWN` & `GRADE IMPACT`)

```mermaid
sequenceDiagram
    autonumber
    actor User as Student
    participant S1 as student-1-frontend
    participant S3 as student-3-backend (:5103)
    participant S3DB as student-3-database
    participant AIM as ai-mode (:5001)
    participant S5 as student-5-backend (:5105)
    participant S5DB as student-5-database

    rect rgb(240, 248, 255)
        note over User,AIM: AI Subtask Breakdown Flow
        User->>S1: Click "AI BREAK DOWN" on deadline notification
        S1->>S3: POST /api/deadlines/tasks/{id}/ai-breakdown
        S3->>S3DB: Load assignment & course context
        S3->>AIM: POST /v1/chat/completions (generate breakdown)
        AIM-->>S3: Subtask plan JSON
        S3->>S3DB: Persist subtasks atomically
        S3-->>S1: Return created subtasks
    end

    rect rgb(255, 250, 240)
        note over User,S5DB: Grade Impact Simulation Flow
        User->>S1: Click "GRADE IMPACT" on grade notification
        S1->>S5: PUT /api/grades/api/assignment/marks/ (simulated score)
        S5->>S5DB: Record / calculate mark adjustments
        S5-->>S1: Return projected course grade & GPA impact
        S1-->>User: Render visual impact in modal
    end
```

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

