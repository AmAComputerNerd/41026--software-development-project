# student-1 backend

ASP.NET Core API (notifications, preferences, AI digest).

## Setup

```bash
cd Api
dotnet restore
```

### AI gateway (required for AI digest generation)

`POST /digest/generate` calls the shared `ai-mode` gateway service, which holds the
OpenRouter API key. This service does not need the key itself, only the gateway's
base URL, configured via `AiGateway:BaseUrl` (defaults to `http://ai-mode:8080`,
the internal docker network address). See root `CLAUDE.md` / `README.md` for the
project-wide setup.

If the base URL is missing at startup, the app logs a warning to the console.

## Run

```bash
cd Api
dotnet run
```
