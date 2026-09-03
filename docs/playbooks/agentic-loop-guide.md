# Playbook: Running & Customizing the Shared Team Agentic Review Loop

The `tools/agentic_loop` tool implements the **Plan → Act → Observe → Adapt** Agentic AI workflow to evaluate code quality, database boundaries, frontend design system compliance, and microservices architecture.

---

## 1. The Plan → Act → Observe → Adapt Lifecycle

```
                 [ Authoritative Documentation & System Context ]
                 (AGENTS.md, docs/ library, owner READMEs)
                                       │
                                       ▼
                              [ Stage 1: PLAN ]
       (Resolves target, loads 5 Golden Rules, extracts architectural contracts)
                                       │
                                       ▼
                               [ Stage 2: ACT ]
      (Collectors execute live HTTP probes, minimal API scanning, SQLite PRAGMA)
                                       │
                                       ▼
                             [ Stage 3: OBSERVE ]
         (Structures runtime and schema evidence into clean observation facts)
                                       │
                                       ▼
                              [ Stage 4: ADAPT ]
 ┌─────────────────────────────────────┴─────────────────────────────────────┐
 │ • Phase 1 (Propose): Implementation Agent produces evidence-backed fix    │
 │ • Phase 2 (Review): Review Agent critiques/confirms against docs & rules │
 └─────────────────────────────────────┬─────────────────────────────────────┘
                                       │
                                       ▼
                             [ Evaluation Report ]
```

---

## 2. Documentation Ingestion Engine

When the loop runs, `core/doc_loader.py` automatically reads:
- **`AGENTS.md`**: Universal 5 Golden Architectural Rules (database isolation, Canvas HTML sanitization, centralized AI gateway, `@better-canvas/ui-kit` tokens, port map).
- **`docs/architecture/`**: Topology, service maps, and cross-service data flows.
- **Layer-Specific Guides**:
  - Frontend: `shared/ui-kit/README.md`, `docs/playbooks/new-frontend-microservice.md`.
  - Backend: `docs/playbooks/new-backend-microservice.md`, `docs/architecture/services.md`.
  - Database: `docs/development/database-and-migrations.md`.
  - Compose: `docker-compose.yml`, `docs/architecture/overview.md`.
- **Owner Feature Context**: `prompts/owners/<owner>/context_prompt.txt` or `<owner>/README.md`.

---

## 3. Configuration & Running

1. Install Python dependencies:
   ```bash
   cd tools/agentic_loop
   pip install -r requirements.txt
   ```
2. Configure credentials in `tools/agentic_loop/.env` or rely on root `.env`:
   ```dotenv
   OPENROUTER_API_KEY=sk-or-v1-...
   OPENROUTER_MODEL=minimax/minimax-m3:free
   ```
3. Run the interactive review tool:
   ```bash
   python tools/agentic_loop.py
   ```
