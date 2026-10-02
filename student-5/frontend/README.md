# Grades & Progress Frontend

Vue 3 frontend for viewing a student's current and desired marks, browsing enrolled courses and assignments, and testing temporary marks to project course results.

```mermaid
flowchart TD
    Shell["shared-shell (/grades/)"] --> App["Student 5 App.vue"]
    App --> Courses["Course Selector & Progress"]
    App --> WhatIf["What-If Mark Simulator"]
    App --> AIModal["AI Recommendation Modal"]
    App -. "@better-canvas/ui-kit" .-> UIKit["Neobrutalism Design System"]
    App -- "HTTP REST" --> Backend["student-5-backend (:5105)"]
```

## Development

The app expects the grades backend at `http://localhost:5105` during Vite development.

- `npm run dev --workspace=student-5-frontend` starts local build.
- In Docker Compose, open the feature under `/grades/` through the shared shell.
