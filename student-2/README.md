# Automations service

The service stores configurable assignment extension, scheduled post, and quiz
filler automations. It provides full CRUD for automation configurations and
read-only access to records of previous runs. A background worker checks enabled
automations every 30 seconds and executes due scheduled posts and quiz fills.

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

The scheduled-post recipient picker requires the shared backend at
`http://localhost:5010`. Configure `CANVAS_BASE_URL` and `CANVAS_API_TOKEN` in
the repository root `.env`, then run `shared/backend/Api` before student-2.
Student-2 never receives or stores the Canvas token.

Quiz filling additionally requires the `ai-mode` gateway. Set
`AiGateway:BaseUrl` to its address; student-2 never receives or stores the
OpenRouter key.

## API

Automation CRUD is available under `/api/automations`. Read-only run history is
available under `/api/automation-runs`. Swagger is available in Development at
`http://localhost:5102/swagger`.

Each run-history row can be expanded to show its timestamp, result, and
type-specific stored fields. Scheduled-post details include context, delivery
mode, recipients, subject, and message body. Recipient IDs are resolved to
Canvas names when available, with the stored IDs retained as a fallback.

Automation request and response bodies are polymorphic. Use `$type` with
`assignmentExtension`, `scheduledPost`, or `quizFiller`; each object then
contains only the fields belonging to that automation type. Run-history
responses use the same discriminator values.

Scheduled posts mirror the Canvas Conversations create request. They store the
selected `course_<id>` context code, recipient user IDs as a JSON string array,
subject, body, and `groupConversation`, plus the local execution time. The
frontend loads messageable recipients through shared-backend's Canvas Search
API gateway and groups their names by enrollment category. When `postTime` is
due, the worker asks shared-backend to create the Canvas Conversation; only
shared-backend holds the Canvas token.

Each executor returns zero or more due execution candidates. A candidate owns
the exact immutable input it will execute, its deterministic execution key, its
run snapshot, and its execution action. Before contacting Canvas, the worker
commits a run with result `RUN` and a unique `(automationId, executionKey)`
value. The result becomes `SUC` or `FAI` after the request.

Scheduled-post keys are a versioned SHA-256 hash of the automation ID,
normalized scheduled time, course context, sorted recipient IDs, subject, body,
and conversation mode. Unchanged parameters cannot execute twice, while editing
the post time or message configuration creates a new logical execution. A
future date remains not due until that date passes. A claim left as `RUN` after
process termination is intentionally not replayed, because Canvas and SQLite
cannot participate in one atomic transaction and an automatic retry could
duplicate a message.

Assignment-extension reasons are stored as enum codes: `UNW` (unwell), `ACL`
(assignment clashes), `NMT` (more time needed), `FAM` (family commitments),
`CAR` (carer responsibilities), `REL` (religious commitments), `WRK` (work
priority), `TEC` (technical problem), `BRV` (bereavement), and `OTH` (other or
prefer not to say).

Assignment-extension automations can optionally store a Canvas course ID in
`subjectId`. A null value means the automation applies to any subject; the
frontend displays Canvas course names while persisting only the numeric ID.

Quiz filler automations answer Canvas Classic Quizzes. `subjectId` restricts
them to one course or, when null, covers every enrolled course.
`multipleChoice` and `shortAnswer` select which question types are answered; at
least one is required. A quiz is eligible when it is published, not locked, has
questions, and either allows at least `numberOfAttemptsRequired` attempts
(unlimited counts as enough) or has no time limit while `allowForNoTimeLimit`
is set.

Canvas cannot record answers through the Quiz Questions resource, which only
authors questions. Filling a quiz therefore uses the Quiz Submission Questions
resource: the worker starts a quiz submission, reads its questions, asks
`ai-mode` to choose an option ID for each multiple choice question and to write
text for each short answer question, then saves those answers against the
submission using its attempt and validation token.

The automation never turns the quiz in. The submission is deliberately left in
progress with the answers saved, so the student opens the quiz, reviews what was
filled in, and submits it themselves. Question and option text is converted to
plain text by shared-backend before it reaches the model, and the model's reply
is rejected unless every question is answered with a valid option ID or
non-empty text. Each quiz is keyed by its quiz ID, so a quiz is filled at most
once regardless of how many attempts it allows.

The initial database contains one assignment extension configuration, one
scheduled post configuration, one quiz filler configuration, and one run record
for each type.

## Adding an automation type

Add the backend entity, polymorphic DTO classes marked with
`AutomationDiscriminator`, and an `IEntityTypeConfiguration` for its derived
tables. JSON, Swagger, and Entity Framework discover these classes from the
assembly; shared endpoints and startup do not need type-specific changes.
Add an `AutomationExecutor<TAutomation>` implementation that returns its due
execution candidates. Executors are discovered automatically. Repeating types
can return multiple candidates in one check; for example, assignment-extension
execution can return one candidate per assignment with a key containing the
assignment ID and the relevant request parameters. Its current stub returns no
candidates.

On the frontend, add the discriminated TypeScript types and a definition module
under `src/automations/definitions` with its field component. Vite discovers the
definition automatically, and the dashboard, form selector, counters, updates,
and run-history view consume it without type-specific branches.

## Integrated development

```powershell
docker compose up --build
```

The shared shell serves the frontend at `http://localhost:8080/automations/`
and proxies the API under `http://localhost:8080/api/automations/`.
