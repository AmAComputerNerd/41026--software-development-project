# 41026 - Advanced Software Development; Project
An LLM-enhanced web application utilising the Canvas Infrastructure API.

## Students
**Student 1: Bryan Lee (25495108).**  
Working directory: `student-1/`  
Notifications service: manages student notifications (deadlines, grades,
automation, account, and AI-sourced) with read/unread state, per-student
delivery preferences by notification type and channel (in-app or email),
and AI-generated digests summarising a student's recent notification
activity.

**Student 2: Isaac Thomas (STUDENT-NUM).**  
Working directory: `student-2/`  
TODO: Short summary of microservice, other info

**Student 3: Jonathon Thomson (25488154).**  
Working directory: `student-3/`  
TODO: Short summary of microservice, other info

**Student 4: Tristan Huang (STUDENT-NUM).**  
Working directory: `student-4/`  
TODO: Short summary of microservice, other info

**Student 5: William Hannah (STUDENT-NUM).**  
Working directory: `student-5/`  
TODO: Short summary of microservice, other info

## Setup

Copy `.env.example` to `.env` and set `OPENROUTER_API_KEY` (get one at
https://openrouter.ai/keys). Every microservice's AI features (digests,
agentic loop, etc.) read this same key via `docker-compose.yml`, which
injects it into each service's container as `OPENROUTER_API_KEY`.

```bash
cp .env.example .env
# edit .env and paste your key
docker compose up
```

Running a service outside Docker (e.g. `dotnet run` directly)? See that
service's own README for how to set the key locally.

## Release 0: Summary
Working branch: `main`  
Feature set:  
- TBD
