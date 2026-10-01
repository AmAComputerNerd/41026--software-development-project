# Account service

The service manages accounts - settings, details and verification.
It has a "forgot password" prompt where it will send a user an email
to change their password. Passwords are securely stored using hashes.

AI planning calls the internal `ai-mode` gateway through the account
backend. AI profile summaries can still be edited manually, but an AI
prompt will help users generate a generic summary for their profile.
This will be expanded upon in future releases.

Future releases will focus on improving the AI summary by giving it
context from other services and expanding on user roles and
permissions.

```mermaid
flowchart TD
    Shell["shared-shell (:8080)"]

    subgraph Student4Slice["student-4 Account & Auth Slice"]
        FE["student-4-frontend (Vue 3)<br>/account/"]
        BE["student-4-backend (:5104)<br>Profiles & AI Summaries"]
        Auth["student-4-authentication (:5114)<br>Auth & Password Resets"]
        DB["student-4-database (:5204)<br>Internal Persistence Service"]
        Storage[("SQLite DB File<br>(student-4-db)")]
        
        FE -- "/api/users, students, teachers" --> BE
        FE -- "/api/auth" --> Auth
        BE -- "HTTP (student-4-data)" --> DB
        Auth -- "HTTP (student-4-data)" --> DB
        DB --> Storage
    end

    subgraph ExternalServices["External & Shared Services"]
        MailHog["MailHog (:1025 SMTP / :8025 Web)<br>Developer Mailbox"]
        AIM["ai-mode (:5001)<br>Profile Summary LLM"]
    end

    Shell -- "Proxy /account/" --> FE
    Shell -- "Proxy /api/users, /api/students, /api/teachers" --> BE
    Shell -- "Proxy /api/auth/" --> Auth

    Auth -- "SMTP password reset" --> MailHog
    BE -. "Profile summary generation" .-> AIM
```

## Standalone development

It is suggested to run all services together with Docker from the repository
root:

```powershell
docker compose up --build
```

Standalone, the slice needs three processes, each in its own terminal — the
private database service first, then the profile API and the authentication
API:

```powershell
dotnet run --project student-4\database\Database
```

```powershell
dotnet run --project student-4\backend\Api
```

```powershell
dotnet run --project student-4\authentication\Authentication
```

In another terminal, run the frontend. Vite serves it under its `/account/`
base path, so the dev URL is `http://localhost:5173/account/`:

```powershell
npm run dev --workspace=student-4-frontend
```

The dev server proxies `/api` to the profile API on port 5104. AI prompts are
unavailable unless the `ai-mode` gateway is also configured.

## Forgot Password Functionality

In testing, the email service sends mail to MailHog; read it in the web inbox
at `http://localhost:8025` (SMTP listens on port 1025).

When deployed in Release 1, SMTP details can be edited and set in the .env
file.
