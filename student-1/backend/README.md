# student-1 backend

ASP.NET Core API (notifications, preferences, AI digest).

## Setup

```bash
cd Api
dotnet restore
```

### OpenRouter API key (required for AI digest generation)

`POST /digest/generate` calls OpenRouter and requires an API key. Without it, the
endpoint throws and returns `500`. See root `CLAUDE.md` / `README.md` for the
project-wide setup (shared key via root `.env` + docker-compose).

Running outside Docker:

```bash
OPENROUTER_API_KEY="<your-key>" dotnet run
```

or `dotnet user-secrets set "OpenRouter:ApiKey" "<your-key>"` in `Api/` —
note this only loads under `ASPNETCORE_ENVIRONMENT=Development`, so
`dotnet run --no-launch-profile` will skip it silently.

If the key is missing at startup, the app logs a warning to the console.

## Run

```bash
cd Api
dotnet run
```
