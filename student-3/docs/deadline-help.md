# Deadline Tracker Help

## Tasks, calendar and assignments

01 TASKS is the editable task list. Search by title, description or course and
filter by status, priority or course. The default Active status hides completed
tasks; select All or Completed to see them. A matching sub-task also shows its
parent so the task hierarchy remains understandable.

02 CALENDAR shows dated, incomplete tasks in a monthly view. Tasks without a
due date are not placed on the calendar. Select a task to edit it, or select a
day to create a task with an initial due date.

03 ASSIGNMENTS shows active, incomplete top-level Canvas assignments. Choose a
course, use Sync Canvas to import assignments, add one sub-task, or use AI
breakdown to generate a plan.

## Creating and editing tasks

Use New task to enter a title, optional description and due date, priority and
optional course. Tasks can also have sub-tasks. Open an existing task to edit
its details or status, then use Save changes. The course cannot be changed in
the existing-task editor. Removing a task also removes its descendants.

Task status can be Todo, InProgress or Completed. Priority can be Low, Medium
or High. Dates are stored in UTC and displayed using the browser's local
time zone.

## Completing a parent task

Marking a parent task Completed also marks all of its descendant sub-tasks
Completed. This applies to nested descendants, not only immediate children.
Completed tasks disappear from the default Active task list, calendar and
upcoming-deadline MCP results. Use the All or Completed task-list filter to
review them.

## Canvas assignment sync and read-only fields

Use Sync Canvas on 03 ASSIGNMENTS. The Deadline Tracker backend requests
courses and assignments through the shared Canvas gateway; it does not call
Canvas directly. Assignment descriptions are sanitized into plain text.
Repeated syncs update existing records rather than creating duplicates.

Canvas assignment title, description, due date and course are read-only in the
task editor. Local priority and status remain editable. These local edits are
not a way to change the Canvas assignment's official details.

Submitted or graded Canvas assignments can be marked completed during sync.
Other submission states do not overwrite local completion status. Assignments
no longer returned by Canvas are retained as inactive and hidden from normal
task and course lists rather than deleted.

## AI assignment breakdown and description drafts

On 03 ASSIGNMENTS, choose AI breakdown for an assignment. Review or edit the
planning prompt and select the priority for the generated sub-tasks. The
backend loads the saved assignment and course context and calls the local
AI Mode gateway. Validated generated sub-tasks are saved together.

For a non-Canvas task, Generate description with AI drafts a description from
the title and selected course or parent assessment. The draft can be edited
before saving the task. AI features require the local AI Mode service.

## MCP upcoming deadlines and opening tasks

AI ASSIST below the navigation opens AI ASSISTANT. Select MCP and choose Days
ahead (1-90) and Maximum tasks (1-50). Show upcoming deadlines invokes the
read-only registered tool `deadlines_list_upcoming` through the tracker backend.
It returns incomplete, active tasks due between the current UTC time and the
end of the selected look-ahead period, ordered by due date and then priority.

Overdue tasks, completed tasks and tasks without a due date are not included.
The default request looks ahead 7 days and returns at most 10 tasks. The
response shows the actual tool name, status, readable deadline results and
the structured JSON returned to the frontend. The JSON section can be
collapsed. OPEN TASK closes the assistant and opens that task's normal editor.
Opening a task does not itself change or complete it.

## RAG documentation answers, sources and confidence

Select RAG in AI ASSISTANT. Use a sample question or enter a question of 3-500
characters, then select Ask project help. RAG retrieves Deadline Tracker
documentation and explicitly shared project sources before asking AI Mode
for an answer grounded in those excerpts.

Answers show source titles, section headings, source paths and a confidence
category. The confidence category reflects retrieval relevance and source
coverage; it is not a probability that every statement is correct. Retrieval
counts show matching excerpts and the number of excerpts eligible for the
selected feature scope.

RAG explains documented behavior. It does not read your live tasks or change
them. Use MCP or the task list for actual upcoming deadlines. If relevant
context is missing, the assistant returns Insufficient context rather than
inventing an answer. Questions about undocumented institutional rules or
unrelated subjects may therefore have insufficient context.

## Unavailable AI services

MCP and RAG are local host services reached through the feature backend.
They are optional: ordinary task, calendar and assignment views do not
depend on them. If an AI feature is disabled or a required service is
unavailable, the assistant displays an error rather than a grounded answer.

From the repository root, `python tools/run_ai_services.py` starts local
AI Mode, MCP and RAG together using the configured environment. Restart the
launcher after updating corpus documentation because RAG indexes it at startup.
