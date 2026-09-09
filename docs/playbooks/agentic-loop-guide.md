# Agentic Loop Evaluation Guide

> Operating instructions for running automated multi-agent architecture evaluations, codebase linting, and compliance verification.

---

## 1. Overview

The `tools/agentic_loop.py` tool executes an automated multi-agent deliberation process over the codebase. It inspects service implementations against the universal architectural standards defined in `AGENTS.md`.

```
┌────────────────────────────────────────────────────────┐
│               Target Selection (e.g. student-1)        │
└──────────────────────────┬─────────────────────────────┘
                           ▼
┌────────────────────────────────────────────────────────┐
│           Codebase & Documentation Ingestion           │
│   (AGENTS.md, docs/architecture, owner prompt/docs)    │
└──────────────────────────┬─────────────────────────────┘
                           ▼
┌────────────────────────────────────────────────────────┐
│             Layer Collectors (AST / File)              │
│      Frontend (Vue), Backend (.NET), Database, Compose │
└──────────────────────────┬─────────────────────────────┘
                           ▼
┌────────────────────────────────────────────────────────┐
│         LLM Multi-Agent Evaluator (ai-mode)            │
│  - Proposer Agent (Drafts fixes / compliance report)   │
│  - Reviewer Agent (Critiques and validates rules)      │
└────────────────────────────────────────────────────────┘
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

---

## 4. Automatic Logging & Audit Evidence

The runner automatically records evaluation transcripts and structured evidence logs into timestamped folders:
- Collector execution traces and observed HTTP responses.
- LLM proposal and review deliberation outputs.
- Verification status against the Five Golden Architectural Rules.
- Each run is written to `tools/agentic_loop/logs/<target>/<timestamp>_<layer>.log`.
