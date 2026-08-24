# Project notes for AI agents

## OpenRouter API key

All AI features across microservices (digest generation, agentic loop, etc.)
use one shared key: `OPENROUTER_API_KEY`.

- Root `.env` (gitignored, copy from `.env.example`) holds the real key.
- `docker-compose.yml` reads root `.env` and injects it into each service
  container as `OPENROUTER_API_KEY`.
- Running a .NET backend outside Docker: set `OPENROUTER_API_KEY` as an
  actual environment variable, or `dotnet user-secrets set OpenRouter:ApiKey
  <key>` in that service's `Api/` directory. user-secrets only loads under
  `ASPNETCORE_ENVIRONMENT=Development` — `dotnet run --no-launch-profile`
  skips it and the key will silently appear unset.

If an AI feature returns 500 with no obvious cause, check this first.
