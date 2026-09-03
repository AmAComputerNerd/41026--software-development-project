# Shared Team Agentic AI Loop (Plan → Act → Observe → Adapt)

An automated multi-agent architecture, code quality, and compliance review pipeline designed to implement the **Plan → Act → Observe → Adapt** Agentic AI workflow for this 5-student microservices project.

---

## 1. Core Capabilities

- **Plan → Act → Observe → Adapt Lifecycle**:
  - **Plan**: Ingests authoritative codebase documentation (`AGENTS.md`, `docs/`, microservice READMEs) and student feature context to establish evaluation criteria.
  - **Act**: Executes live endpoint probes, .NET Minimal API endpoint scanning, SQLite `PRAGMA` schema introspection, and Docker Compose configuration analysis.
  - **Observe**: Structures runtime and static evidence without hallucination.
  - **Adapt**:
    - *Phase 1 (Implementation Agent)*: Generates one evidence-backed recommendation strictly bounded by the 5 Golden Architectural Rules.
    - *Phase 2 (Review Agent)*: Critiques or confirms the recommendation against gathered evidence and architectural documentation.
- **Authoritative Codebase Documentation Grounding**:
  - Reads `AGENTS.md` (database isolation, Canvas gateway boundary, centralized AI gateway, `@better-canvas/ui-kit` Neobrutalism design system, port maps).
  - Ingests layer-specific docs from `docs/` (`services.md`, `data-flows.md`, `database-and-migrations.md`, `new-frontend-microservice.md`).
- **OpenRouter & Local LLM Support**:
  - Out-of-the-box support for OpenRouter (`nvidia/nemotron-3-ultra-550b-a55b:free` / OpenAI-compatible endpoints) as approved by the tutor, with fallback to local Ollama.
- **Frontend Discovery for `@better-canvas/ui-kit`**:
  - Inspects Vue 3 `<script setup>` SFCs, Vue Router definitions, and `@better-canvas/ui-kit` Neobrutalism tokens and components (`TopNav`, `--nb-*`).

---

## 2. Directory Structure

```text
tools/
├── agentic_loop.py            # Entrypoint wrapper
└── agentic_loop/
    ├── main.py                 # Interactive terminal menu (Owner -> Layer -> Run All)
    ├── config/
    │   └── review_config.py    # Owners, layers, and context resolution
    ├── core/
    │   ├── doc_loader.py       # Documentation ingestion & context loader
    │   ├── orchestrator.py     # Plan -> Act -> Observe -> Adapt orchestrator
    │   ├── ai_runner.py        # OpenRouter / OpenAI / Ollama client
    │   ├── prompt_registry.py  # Prompt loader
    │   ├── compose_utils.py    # docker-compose.yml parser
    │   └── reporter.py         # Terminal output formatting
    ├── collectors/
    │   ├── frontend_collector.py  # Vue 3 + @better-canvas/ui-kit static & live collector
    │   ├── backend_collector.py   # .NET minimal-API route discovery + live GET prober
    │   ├── database_collector.py  # SQLite PRAGMA schema introspector
    │   └── compose_collector.py   # docker-compose.yml configuration collector
    ├── pipelines/
    │   ├── frontend_pipeline.py
    │   ├── backend_pipeline.py
    │   ├── database_pipeline.py
    │   ├── compose_pipeline.py
    │   └── review_pipeline.py     # Second-pass critique & validation prompt builder
    ├── prompts/
    │   ├── service/               # Shared system baseline & task prompts
    │   └── owners/                # Owner-specific feature context prompts
    ├── .env.example
    └── requirements.txt
```

---

## 3. Setup & Execution

### Setup
```bash
cd tools/agentic_loop
pip install -r requirements.txt
cp .env.example .env
```

Configure `.env` (or let the tool automatically read `OPENROUTER_API_KEY` from the repository root `.env`):
```dotenv
OPENROUTER_API_KEY=sk-or-v1-xxxxxxxxxxxxxxxxxxxx
OPENROUTER_MODEL=nvidia/nemotron-3-ultra-550b-a55b:free
```

### Run
From repository root:
```bash
python tools/agentic_loop.py
```

Select a target owner (`student-1` through `student-5`, `shared`), `docker-compose`, or `Run All`.
