# Notifications Frontend (`student-1/frontend`)

Vue 3 SPA for managing notifications, delivery preferences, AI digests, and interactive cross-service actions.

---

## 1. Capabilities

- **Live SSE Streaming**: Listens to `GET /notifications/stream` and surfaces incoming notifications immediately via `NotificationToast.vue`.
- **Conversational AI Digest Assistant**: Interactive chat interface (`DigestChatPanel.vue`) grounded in the user's unread notifications.
- **Cross-Service Actions**:
  - `AI BREAK DOWN`: Generates task subtasks via `student-3-backend` using `BreakdownDialog.vue`.
  - `GRADE IMPACT`: Simulates assignment score changes via `student-5-backend` using `GradeImpactDialog.vue`.
  - `MARK COMPLETE`: Inline completion of deadline tasks via `student-3-backend`.
- **Neobrutalism Design**: Fully styled with `@better-canvas/ui-kit`.

---

## 2. Running Locally

```bash
# From repository root
npm run dev --workspace=student-1-frontend
```

Runs by default on `http://localhost:5173`. When accessing through the shared shell, visit `http://localhost:8080/notifications/`.
