# Graph Report - 41026--software-development-project  (2026-08-24)

## Corpus Check
- 126 files · ~18,341 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 610 nodes · 855 edges · 59 communities (39 shown, 20 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 5 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `34cf64ca`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- orchestrator.py
- PreferenceEndpoints
- TaskEntity
- frontend_collector.py
- Student-1 Launch Settings
- Student-3 Launch Settings
- InitialCreate
- Api.Models
- OpenRouterDigestService
- useNotifications.ts
- CI Docs
- Api
- shared/frontend/package.json
- shared/frontend/tsconfig.app.json
- .GenerateDigest
- README Docs
- AppDbContext
- Misc Node 19
- Misc Node 20
- Misc Node 21
- Misc Node 22
- Misc Node 23
- Misc Node 24
- Misc Node 25
- compilerOptions
- TaskEndpoints
- Api
- App.vue
- request
- tsconfig.json
- NotificationsView.vue
- Notifications UI — design reference
- devDependencies
- PreferencesPanel.vue
- DigestHistoryList.vue
- useAiDigest.ts
- AiDigestView.vue
- DigestCard.vue
- config.ts
- plugins/index.ts
- student-1 backend
- Project notes for AI agents
- tiles.ts
- shared/frontend/src/config.ts
- shared/frontend/tsconfig.json
- DashboardGrid.vue
- TopNav.vue

## God Nodes (most connected - your core abstractions)
1. `AppDbContext` - 35 edges
2. `Api.Models` - 20 edges
3. `Api.Data` - 18 edges
4. `Api.DTOs` - 16 edges
5. `request()` - 14 edges
6. `Api.Extensions` - 13 edges
7. `InitialCreate` - 11 edges
8. `OpenRouterDigestService` - 11 edges
9. `AIRunner` - 11 edges
10. `TaskEntity` - 10 edges

## Surprising Connections (you probably didn't know these)
- `include` --extends--> `src/**/*.vue`  [EXTRACTED]
  student-1/frontend/tsconfig.app.json → shared/frontend/tsconfig.app.json
- `Student 3 CI Workflow` --references--> `Student 3 Backend Service`  [INFERRED]
  .github/workflows/student-3-ci.yml → docker-compose.yml
- `Student 3 Feature Context` --conceptually_related_to--> `Student 3 Backend Service`  [INFERRED]
  tools/agentic_loop/prompts/owners/student-3/context_prompt.txt → docker-compose.yml
- `Serena Project Config` --references--> `Project README`  [INFERRED]
  .serena/project.yml → README.md
- `exclude` --extends--> `src/**/__tests__/*`  [EXTRACTED]
  student-1/frontend/tsconfig.app.json → shared/frontend/tsconfig.app.json

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Agentic Loop Prompt System** — tools_agentic_loop_prompts_service_system_prompt, tools_agentic_loop_prompts_service_review_system_prompt, tools_agentic_loop_prompts_service_backend_task_prompt, tools_agentic_loop_prompts_service_database_task_prompt, tools_agentic_loop_prompts_service_frontend_task_prompt, tools_agentic_loop_prompts_service_compose_task_prompt, tools_agentic_loop_prompts_service_review_task_prompt [EXTRACTED 1.00]
- **Student 3 Microservice Infrastructure** — student_3_backend, student_3_db_volume, github_workflows_student_3_ci, tools_agentic_loop_prompts_owners_student_3_context_prompt [INFERRED 0.90]

## Communities (59 total, 20 thin omitted)

### Community 0 - "orchestrator.py"
Cohesion: 0.08
Nodes (26): AIRunner, _truncate_words(), Path, Second-pass review agent: critiques the implementation agent's recommendation…, _run_review_stage(), run_target(), _stage(), PromptRegistry (+18 more)

### Community 1 - "PreferenceEndpoints"
Cohesion: 0.31
Nodes (6): NotificationPreferenceRequestDto, PreferenceEndpoints, Guid, IEndpointRouteBuilder, IResult, Task

### Community 2 - "TaskEntity"
Cohesion: 0.10
Nodes (18): DateTimeOffset, DbContext, AppDbContext, DbSet, ModelBuilder, DbSeeder, ICollection, List (+10 more)

### Community 3 - "frontend_collector.py"
Cohesion: 0.11
Nodes (34): Any, collect(), _discover_routes(), _extract_first_id(), Path, Generalized .NET minimal-API discovery: works for any owner whose backend…, _resolve_base_url(), collect() (+26 more)

### Community 4 - "Student-1 Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 5 - "Student-3 Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 6 - "InitialCreate"
Cohesion: 0.08
Nodes (14): Api.Migrations, Migration, ModelSnapshot, MigrationBuilder, InitialCreate, ModelBuilder, MigrationBuilder, AddNotificationMessage (+6 more)

### Community 7 - "Api.Models"
Cohesion: 0.08
Nodes (19): Api.Models, Api.Extensions, Api.Services, Api.Data, Api.DTOs, Api.Endpoints, AiDigestDto, NotificationDto (+11 more)

### Community 8 - "OpenRouterDigestService"
Cohesion: 0.10
Nodes (23): ChatCompletionChoice, ChatMessage, IConfiguration, IHttpClientFactory, string, Notification, NotificationType, DateTime (+15 more)

### Community 9 - "useNotifications.ts"
Cohesion: 0.06
Nodes (26): { notifications, unreadCount, fetchNotifications, markAsRead, markAllAsRead }, open, recentNotifications, root, toggle(), allActive(), emit, emit (+18 more)

### Community 10 - "CI Docs"
Cohesion: 0.29
Nodes (8): Agentic Loop Tool, Docker Compose Configuration, Student 3 CI Workflow, Student 3 Backend Service, Student 3 DB Volume, Student 3 Feature Context, Agentic Loop README, Agentic Loop Requirements

### Community 11 - "Api"
Cohesion: 0.25
Nodes (7): Api, net10.0, Microsoft.AspNetCore.OpenApi (10.0.5), Microsoft.EntityFrameworkCore.Design (10.0.11), Microsoft.EntityFrameworkCore.Sqlite (10.0.11), Swashbuckle.AspNetCore (10.2.3), Microsoft.NET.Sdk.Web

### Community 12 - "shared/frontend/package.json"
Cohesion: 0.07
Nodes (27): dependencies, vue, vue-router, name, private, scripts, build, build-only (+19 more)

### Community 13 - "shared/frontend/tsconfig.app.json"
Cohesion: 0.07
Nodes (27): env.d.ts, src/**/*, src/**/*.js, src/**/__tests__/*, src/**/*.ts, src/**/*.tsx, src/**/*.vue, vue-router/volar/sfc-route-blocks (+19 more)

### Community 14 - ".GenerateDigest"
Cohesion: 0.18
Nodes (10): ILoggerFactory, AiDigestEndpoints, Guid, IEndpointRouteBuilder, IResult, Task, CancellationToken, Guid (+2 more)

### Community 16 - "AppDbContext"
Cohesion: 0.08
Nodes (23): AppDbContext, DbSet, ModelBuilder, DbSeeder, Guid, NotificationFilterDto, PushNotificationRequestDto, NotificationEndpoints (+15 more)

### Community 26 - "compilerOptions"
Cohesion: 0.08
Nodes (24): cypress.config.*, nightwatch.conf.*, node, playwright.config.*, @tsconfig/node22/tsconfig.json, vite.config.*, vite.config.ts, vitest.config.* (+16 more)

### Community 27 - "TaskEndpoints"
Cohesion: 0.25
Nodes (8): CreateTaskRequestDto, ModifyTaskRequestDto, TaskFilterDto, TaskEndpoints, Guid, IEndpointRouteBuilder, IResult, Task

### Community 28 - "Api"
Cohesion: 0.25
Nodes (7): Api, net10.0, Microsoft.AspNetCore.OpenApi (10.0.5), Microsoft.EntityFrameworkCore.Design (10.0.11), Microsoft.EntityFrameworkCore.Sqlite (10.0.11), Swashbuckle.AspNetCore (10.2.3), Microsoft.NET.Sdk.Web

### Community 29 - "App.vue"
Cohesion: 0.33
Nodes (4): navLinks, route, app, router

### Community 30 - "request"
Cohesion: 0.27
Nodes (13): generateDigest(), getDigests(), BASE_URL, buildQuery(), request(), getNotifications(), markAllNotificationsRead(), markNotificationRead() (+5 more)

### Community 36 - "Notifications UI — design reference"
Cohesion: 0.25
Nodes (7): Notifications UI — design reference, Screen 1 — Notification centre (dropdown), Screen 2 — Full notification list with filters, Screen 3 — Preferences panel, Screen 4 — AI digest, Shared chrome, State shape (from the mockup's `Component` class)

### Community 37 - "devDependencies"
Cohesion: 0.09
Nodes (26): npm-run-all2, sass-embedded, devDependencies, npm-run-all2, @tsconfig/node22, @types/node, typescript, vite (+18 more)

### Community 39 - "PreferencesPanel.vue"
Cohesion: 0.40
Nodes (3): emit, { grid, loading, error, fetchPreferences, toggle }, TYPE_DESCRIPTIONS

### Community 46 - "plugins/index.ts"
Cohesion: 0.33
Nodes (3): app, registerPlugins(), router

### Community 47 - "student-1 backend"
Cohesion: 0.40
Nodes (4): OpenRouter API key (required for AI digest generation), Run, Setup, student-1 backend

### Community 49 - "tiles.ts"
Cohesion: 0.50
Nodes (3): Tile, TileIcon, TILES

## Knowledge Gaps
- **152 isolated node(s):** `name`, `private`, `type`, `version`, `dev` (+147 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `AppDbContext` connect `AppDbContext` to `PreferenceEndpoints`, `TaskEntity`, `Api.Models`, `OpenRouterDigestService`, `.GenerateDigest`, `TaskEndpoints`?**
  _High betweenness centrality (0.054) - this node is a cross-community bridge._
- **Why does `Api.Data` connect `Api.Models` to `InitialCreate`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **Why does `Api.Models` connect `Api.Models` to `OpenRouterDigestService`, `TaskEntity`?**
  _High betweenness centrality (0.021) - this node is a cross-community bridge._
- **What connects `name`, `private`, `type` to the rest of the system?**
  _152 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `orchestrator.py` be split into smaller, more focused modules?**
  _Cohesion score 0.08194905869324474 - nodes in this community are weakly interconnected._
- **Should `TaskEntity` be split into smaller, more focused modules?**
  _Cohesion score 0.09686609686609686 - nodes in this community are weakly interconnected._
- **Should `frontend_collector.py` be split into smaller, more focused modules?**
  _Cohesion score 0.11025641025641025 - nodes in this community are weakly interconnected._