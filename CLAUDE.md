# Project notes for AI agents

This is a 5-student microservices project (ASD unit, Release 0).
Keep `docs/architecture/overview.md` up to date with what's actually built.

## Service boundaries

Each student's `student-N/backend` owns its own SQLite database. No
service reads another service's database directly, cross-service data
goes through HTTP APIs only. `shared/backend` owns Canvas API integration
specifically (courses, assignments, users), everything else calls it
over HTTP rather than hitting Canvas directly.

## OpenRouter API key

All AI features across microservices (digest generation, agentic loop,
etc.) use one shared key: `OPENROUTER_API_KEY`.

- Root `.env` (gitignored, copy from `.env.example`) holds the real key.
- `docker-compose.yml` reads root `.env` and injects it into each service
  container as `OPENROUTER_API_KEY`.
- Running a .NET backend outside Docker: set `OPENROUTER_API_KEY` as an
  actual environment variable, or `dotnet user-secrets set OpenRouter:ApiKey
  <key>` in that service's `Api/` directory. user-secrets only loads under
  `ASPNETCORE_ENVIRONMENT=Development` — `dotnet run --no-launch-profile`
  skips it and the key will silently appear unset.

If an AI feature returns 500 with no obvious cause, check this first.

## AI-mode

Don't call OpenRouter directly from a new backend. Call the shared
`ai-mode` gateway service instead (`http://ai-mode:8080/v1/chat/completions`
inside Docker), it holds the only OpenRouter key any service needs.

## Adding a new frontend microservice

Follow `docs/playbooks/new-frontend-microservice.md`.

Short version: Vue 3, no Vuetify, plain SCSS. Depend on
`@better-canvas/ui-kit` (workspace package) for tokens, fonts, and shared
components (Navbar, etc.) instead of writing your own. Add an nginx
proxy block in `shared/frontend/nginx.conf`, add your service to the root
`docker-compose.yml`, and enable its tile in `shared/ui-kit/src/services.ts`.

## Git conventions used throughout this repo

- Branch off `main` per feature, don't stack feature branches on other
  open feature branches.
- Commit after each logical step, not one giant commit at the end, push
  as you go.
- Open a PR into `main` when done, don't push directly to main.
