# Testing & Continuous Integration (CI)

This guide documents the automated testing, code validation, and continuous integration workflows used across the repository.

---

## 1. Local Testing & Verification Commands

### .NET Backend Testing & Code Quality
Run these commands from the repository root:

```bash
# 1. Run the automated test suites
#    student-1 is currently the only slice with a .NET test project
#    (student-1/backend/Api.Tests: xUnit unit + integration tests)
dotnet test student-1/backend/NotificationService.sln

# 2. Check for code formatting compliance
dotnet format student-1/backend/NotificationService.sln --verify-no-changes
dotnet format student-3/backend/DeadlineTaskTracker.sln --verify-no-changes
dotnet format student-4/backend/AccountService.sln --verify-no-changes
dotnet format student-5/backend/GradesManager/GradesManager.slnx --verify-no-changes
dotnet format shared/backend/SharedBackend.sln --verify-no-changes

# 3. Check for EF Core migration drift (ensure models match migrations)
#    Run against whichever project owns persistence for that slice
dotnet ef migrations has-pending-model-changes \
  --project student-1/backend/Api/Api.csproj
dotnet ef migrations has-pending-model-changes \
  --project student-2/backend/Api/Api.csproj
dotnet ef migrations has-pending-model-changes \
  --project student-3/database/Database/Database.csproj
dotnet ef migrations has-pending-model-changes \
  --project student-4/database/Database/Database.csproj
dotnet ef migrations has-pending-model-changes \
  --project student-5/database/Database/Database.csproj
dotnet ef migrations has-pending-model-changes \
  --project shared/backend/Api/Api.csproj
```

### Frontend Typechecking, Building & E2E Tests
Run these commands from the repository root:

```bash
# Typecheck and build every workspace that defines a build script
# (@better-canvas/ui-kit has none, so --if-present is required)
npm run build --workspaces --if-present

# Or build individual workspaces
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

---

## 2. GitHub Actions CI Architecture

The repository enforces CI checks on all Pull Requests targeting `main` and pushes to `main`. Workflows are modularized under `.github/workflows/` with precise path triggers:

| Workflow File | Trigger Paths | Key Jobs Executed |
|---|---|---|
| **`docker-ci.yml`** | `ai-services/**`, `shared/**`, `student-*/**`, `docker-compose.yml`, `package.json`, `package-lock.json`, `.env.example` | Compose contract validation, dynamically discovered parallel image builds, and a Student 3 Compose integration smoke test |
| **`shared-ci.yml`** | `shared/backend/**`, `shared/frontend/**`, `shared/ui-kit/**` | Frontend build, .NET build/format check, EF migrations drift check, NuGet vulnerability audit |
| **`student-1-ci.yml`** | `student-1/**`, `shared/ui-kit/**` | Notifications frontend build + Playwright E2E, .NET build/test/format check, EF migration drift check, NuGet audit |
| **`student-2-ci.yml`** | `student-2/**`, `shared/ui-kit/**` | Automations frontend build, .NET build/format check, EF migration drift check, NuGet audit |
| **`student-3-ci.yml`** | `student-3/**`, `shared/ui-kit/**` | Deadlines frontend build, .NET build/format check, API contract & browser CORS smoke test, EF migration drift check, NuGet audit |
| **`student-4-ci.yml`** | `student-4/**`, `shared/ui-kit/**` | Account frontend build, .NET build/format check, EF migration drift check, NuGet audit |
| **`student-5-ci.yml`** | `student-5/**`, `shared/ui-kit/**` | Grades frontend build, .NET build/format check, API contract smoke test, EF migration drift check, NuGet audit |

Only `student-1-ci.yml` currently runs `dotnet test`, because
`student-1/backend/Api.Tests` is the repository's only .NET test project. The
other slices rely on build-with-`-warnaserror`, format checks, migration drift
checks, and endpoint smoke tests.

Docker image targets are discovered from the rendered Compose configuration
rather than maintained as a second service list. Each image builds in a
four-wide matrix with its own BuildKit GitHub Actions cache scope. The stable
`Validate Compose & build images` aggregate check succeeds only when the
Compose contract, every image build, and the Student 3 integration smoke test
all pass.

---

## 3. Pre-PR Checklist for AI Agents & Developers

Before opening a PR, ensure:
1. `dotnet format --verify-no-changes` passes on all modified .NET projects.
2. `dotnet test` passes with zero failures for any slice that has a test project.
3. `npm run build --workspaces --if-present` succeeds without TypeScript or Vite errors.
4. If database models were modified, an EF Core migration was generated and committed.
5. All commits follow Conventional Commits format (`feat:`, `fix:`, `docs:`, `test:`, `chore:`).
