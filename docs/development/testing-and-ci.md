# Testing & Continuous Integration (CI)

This guide documents the automated testing, code validation, and continuous integration workflows used across the repository.

---

## 1. Local Testing & Verification Commands

### .NET Backend Testing & Code Quality
Run these commands from the repository root or the service's `backend/` directory:

```bash
# 1. Run all unit/integration tests for a solution
dotnet test student-1/backend/NotificationService.sln
dotnet test student-3/backend/DeadlineTaskTracker.sln
dotnet test shared/backend/SharedBackend.sln

# 2. Check for code formatting compliance
dotnet format student-1/backend/NotificationService.sln --verify-no-changes
dotnet format student-3/backend/DeadlineTaskTracker.sln --verify-no-changes
dotnet format shared/backend/SharedBackend.sln --verify-no-changes

# 3. Check for EF Core migration drift (ensure models match migrations)
# (Run from the Api project directory)
dotnet ef migrations has-pending-model-changes --project Api/Api.csproj
```

### Frontend Typechecking & Building
Run these commands from the repository root:

```bash
# Typecheck all frontend workspaces
npm run build --workspaces

# Or build individual workspaces
npm run build --workspace=shared-frontend
npm run build --workspace=student-1-frontend
npm run build --workspace=student-3-frontend
npm run build --workspace=student-5-frontend
```

---

## 2. GitHub Actions CI Architecture

The repository enforces CI checks on all Pull Requests targeting `main` and pushes to `main`. Workflows are modularized under `.github/workflows/` with precise path triggers:

| Workflow File | Trigger Paths | Key Jobs Executed |
|---|---|---|
| **`docker-ci.yml`** | `ai-services/**`, `shared/**`, `student-*/**`, `docker-compose.yml`, `package.json` | Full multi-service Docker build verification |
| **`shared-ci.yml`** | `shared/**` | Frontend build, .NET build/test, `dotnet format` check, EF migrations check, NuGet vulnerability audit |
| **`student-1-ci.yml`** | `student-1/**` | Notifications frontend typecheck/build, .NET build/test, format check, EF migration check |
| **`student-3-ci.yml`** | `student-3/**` | Deadlines frontend typecheck/build, .NET build/test, format check, EF migration check |
| **`student-5-ci.yml`** | `student-5/**` | Grades frontend typecheck/build, .NET build/test, format check |

---

## 3. Pre-PR Checklist for AI Agents & Developers

Before opening a PR, ensure:
1. `dotnet format --verify-no-changes` passes on all modified .NET projects.
2. `dotnet test` passes with zero failures.
3. `npm run build --workspaces` succeeds without TypeScript or Vite errors.
4. If database models were modified, an EF Core migration was generated and committed.
5. All commits follow Conventional Commits format (`feat:`, `fix:`, `docs:`, `test:`, `chore:`).
