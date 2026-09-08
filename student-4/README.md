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

## Standalone development

It is suggested to run all services together using docker using the 
following command:

```cd C:\Users\tetec\source\repos\41026--software-development-project
docker compose up --build
```

The account and authentication APIs can run without other services:

```powershell
dotnet run --project student-4\backend\Api
```

In a second terminal, run the frontend at
`http://localhost:3004/account/`:

```powershell
npm run dev --workspace=student-4-frontend
```

The frontend calls the published standalone API on port 5104. Ai
prompts are unavailable unless the shared backend is also configured.

## Forgot Password Functionality

In testing, the email service will send an email via MailHog on port 8025. 

When deployed in Release 1, SMTP details can be edited and set in the .env
file.
