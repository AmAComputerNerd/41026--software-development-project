# Contributing

How to work on this repo, regardless of which student slice you own.

## Repo layout

```
.
├── ai-services/ai-mode/         # OpenRouter gateway (one shared API key)
├── shared/
│   ├── backend/                 # Canvas API integration
│   ├── frontend/                # dashboard shell + nginx reverse proxy
│   └── ui-kit/                  # @better-canvas/ui-kit workspace package
├── student-1/                   # Bryan — notifications
├── student-2/                   # Isaac — automations
├── student-3/                   # Jonathon — deadlines & tasks
├── student-4/                   # Tristan — account & authentication
├── student-5/                   # William — grades & progress
├── docs/                        # architecture + playbooks
└── docker-compose.yml           # wires everything together
```

Each `student-N/` has its own `backend/` (ASP.NET Core) and `frontend/`
(Vue 3 + plain SCSS). Students 3, 4, and 5 additionally have a private
`database/` service that exclusively owns EF Core and SQLite, plus a
`contracts/` project for the internal HTTP contract; student 1 owns a
dedicated PostgreSQL container under `database/`, and student 2 keeps
EF Core inside its API. Cross-service calls go through HTTP; never read
another service's database directly.

## Branch workflow

- Branch off `main` per feature. Don't stack feature branches on other
  open feature branches.
- Commit after each logical step, not one giant commit at the end.
  Push as you go so others can see progress.
- Open a PR into `main` when done. Don't push directly to `main`.
- CI runs per service group (see `.github/workflows/`). Each workflow
  is path-filtered to its own directories, so unrelated changes don't
  trigger unrelated builds.

## Commit messages

Short subject line, imperative mood. Body when the diff needs context.
Reference the student slice in the subject when the change is
slice-specific, e.g. `student-3: add subtask reordering`.

## Adding a new service (or your first slice)

If you're scaffolding a new frontend, follow
[`docs/playbooks/new-frontend-microservice.md`](docs/playbooks/new-frontend-microservice.md).
It covers:

- workspace wiring (root `package.json`),
- depending on `@better-canvas/ui-kit` for tokens, fonts, and shared
  components,
- Dockerfile shape (multi-stage `node:22-alpine` build, `nginx:1.27-alpine`
  serve),
- the entry in root `docker-compose.yml`,
- the nginx route block in `shared/frontend/nginx.conf`,
- the dashboard tile registration in `shared/ui-kit/src/services.ts`
  and the matching icon + description in
  `shared/frontend/src/data/tiles.ts`.

For new backends, follow
[`docs/playbooks/new-backend-microservice.md`](docs/playbooks/new-backend-microservice.md):
an ASP.NET Core 10 public API plus a private EF Core + SQLite database
service and a shared contracts project. The `shared-backend` Canvas
gateway is the only place that should hit the Canvas API directly. For
AI features, call the `ai-mode` gateway rather than OpenRouter directly
— it holds the only API key the project uses.

## Design tokens and theming

All frontends pull design tokens (colours, borders, shadows, spacing,
fonts) from `@better-canvas/ui-kit`. Don't hardcode values in
component styles — use the CSS custom properties from
`shared/ui-kit/src/styles/tokens.css`. The token names are prefixed
`--nb-` (neobrutalist).

If a new token is needed, add it to `tokens.css` with both its light
and dark variant if it has one, and document the use case in a short
inline comment. Components across every frontend pick up the new
token on their next `npm run build`.

## Service boundaries

Each backend owns its own database — either through its own EF Core
context or, for students 3, 4, and 5, through a private database
service reached over HTTP. No service reads another service's database
directly; cross-service data goes through HTTP APIs only.
`shared/backend` owns Canvas API integration; everything else calls it
over HTTP rather than hitting Canvas directly.

## AI features

Don't call OpenRouter from a new backend. Call the shared `ai-mode`
gateway instead (`http://ai-mode:8080/v1/chat/completions` inside
Docker). The gateway holds the only `OPENROUTER_API_KEY` any service
needs. The root `.env` is the source of truth; `docker-compose.yml`
injects it into the ai-mode container.

If an AI feature returns 500 with no obvious cause, check
`OPENROUTER_API_KEY` first.

## Verifying your change locally

```bash
docker compose up                       # bring the stack up
docker compose logs -f <service>        # tail a service
docker compose down                     # stop
docker compose down -v                  # stop and wipe database volumes
```

For frontend iteration, `npm run dev` from inside the frontend's
directory is usually faster than rebuilding the Docker image. The
backend URL the dev server hits is baked in at build time (see the
`VITE_*_API_BASE_URL` lines in each frontend's Dockerfile).

For backend iteration, `dotnet run` from a backend's project
directory works as long as the env vars it needs are set (notably
`OPENROUTER_API_KEY`, `SharedService__BaseUrl`,
`AiGateway__BaseUrl`, and `DatabaseService__BaseUrl` if applicable).
Most cross-service calls only work in compose, not standalone.

## Where to ask questions

For architecture-level questions, see
[`docs/architecture/overview.md`](docs/architecture/overview.md). For
questions about your own slice, reach out to the student who owns it
per the team roster in `README.md`.
