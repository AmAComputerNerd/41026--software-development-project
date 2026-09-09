# Testing & Continuous Integration Guide

> Guide to local testing commands, format verification, and GitHub Actions CI pipelines.

---

## 1. Local Testing & Verification Cheatsheet

Run these commands locally before committing and opening a Pull Request:

### Backend .NET Tests & Linters
```bash
# 1. Run the automated test suites
#    student-1 is currently the primary slice with a .NET test project (unit + integration tests)
dotnet test student-1/backend/NotificationService.sln
dotnet test student-3/backend/DeadlineTaskTracker.sln

# 2. Check code formatting against .editorconfig
dotnet format student-1/backend/NotificationService.sln --verify-no-changes
dotnet format student-2/backend/Automations.sln --verify-no-changes
dotnet format student-3/backend/DeadlineTaskTracker.sln --verify-no-changes
dotnet format student-4/backend/AccountService.sln --verify-no-changes
dotnet format student-5/backend/GradesManager/GradesManager.slnx --verify-no-changes
dotnet format shared/backend/SharedBackend.sln --verify-no-changes

# 3. Check for EF Core migration drift (ensure models match migrations)
#    Run against whichever project owns persistence for that slice
dotnet ef migrations has-pending-model-changes   --project student-1/backend/Api/Api.csproj
dotnet ef migrations has-pending-model-changes   --project student-2/backend/Api/Api.csproj
dotnet ef migrations has-pending-model-changes   --project student-3/database/Database/Database.csproj
dotnet ef migrations has-pending-model-changes   --project student-4/database/Database/Database.csproj
dotnet ef migrations has-pending-model-changes   --project student-5/database/Database/Database.csproj
dotnet ef migrations has-pending-model-changes   --project shared/backend/Api/Api.csproj
```

### Frontend TypeScript & Vite Builds
```bash
# Typecheck and build all frontends
npm run build --workspaces --if-present

# Or build individual frontend slices
npm run build --workspace=shared-frontend
npm run build --workspace=student-1-frontend
npm run build --workspace=student-2-frontend
npm run build --workspace=student-3-frontend
npm run build --workspace=student-4-frontend
npm run build --workspace=student-5-frontend

# Playwright end-to-end tests (student-1/frontend/e2e)
npx playwright install --with-deps chromium
npm run test:e2e --workspace=student-1-frontend
```

### End-to-End (E2E) Testing
Student 1 maintains Playwright end-to-end browser test suites:

```bash
cd student-1/frontend
npx playwright test
```

---

## 2. GitHub Actions CI Architecture

The repository enforces CI checks on all pull requests targeting `main` and all pushes to `main`.

| Workflow File | Trigger Paths | Key Jobs Executed |
|---|---|---|
| **`docker-ci.yml`** | `ai-services/**`, `shared/**`, `student-*/**`, `docker-compose.yml`, `package.json`, `package-lock.json`, `.env.example` | Compose contract validation, dynamically discovered parallel image builds, and a Student 3 Compose integration smoke test |
| **`shared-ci.yml`** | `shared/backend/**`, `shared/frontend/**`, `shared/ui-kit/**` | Frontend build, .NET build/format check, EF migrations drift check, NuGet vulnerability audit |
| **`student-1-ci.yml`** | `student-1/**`, `shared/ui-kit/**` | Notifications frontend build + Playwright E2E, .NET build/test/format check, EF migration drift check, NuGet audit |
| **`student-2-ci.yml`** | `student-2/**`, `shared/ui-kit/**` | Automations frontend build, .NET build/format check, EF migration drift check, NuGet audit |
| **`student-3-ci.yml`** | `student-3/**`, `shared/ui-kit/**` | Deadlines frontend build, .NET build/format check, API contract & browser CORS smoke test, EF migration drift check, NuGet audit |
| **`student-4-ci.yml`** | `student-4/**`, `shared/ui-kit/**` | Account & Auth frontend build, .NET build/format check, EF migration drift check, NuGet audit |
| **`student-5-ci.yml`** | `student-5/**`, `shared/ui-kit/**` | Grades frontend build, .NET build/format check, API contract smoke test, EF migration drift check, NuGet audit |

Docker image targets are discovered from the rendered Compose configuration
rather than maintained as a second service list. Each image builds in a
separate matrix job, with bake-cache acceleration so repeated runs build only
what changed.

---

## 3. Pre-PR Checklist

Before opening a PR, ensure:
1. `dotnet format --verify-no-changes` passes on all modified .NET projects.
2. `dotnet test` passes with zero failures for any slice that has a test project.
3. `npm run build --workspaces --if-present` succeeds without TypeScript or Vite errors.
4. If database models were modified, an EF Core migration was generated and committed.
5. All commits follow Conventional Commits format (`feat:`, `fix:`, `docs:`, `test:`, `chore:`).
