# Agentic Loop

An interactive Observe -> Prompts -> LLM -> Summary review loop for this
repo's multi-student microservices project.

- **Frontend/Backend/Database are reviewed per owner** (`student-1` ..
  `student-5`, `shared`), selected from an interactive menu.
- **Database review is schema-agnostic.** It introspects whatever SQLite
  file(s) actually exist under an owner's directory via `PRAGMA` (tables,
  columns, types, keys, FK violations, row counts) - never a fixed
  migration - and asks the LLM whether that schema makes sense for the
  owner's feature.
- **Feature context is student-authored per owner.** Each student can
  write `prompts/owners/<owner>/context_prompt.txt` describing what their
  service is meant to do; if it doesn't exist yet, the tool falls back to
  that owner's blurb in the root `README.md` so it still works out of the
  box. See [Adding your own feature context](#adding-your-own-feature-context)
  below.
- **Every run has two agents.** An implementation agent produces one
  evidence-backed recommendation (or explicitly reports that no
  improvement was found - a valid, good outcome), then a review agent
  critiques that recommendation against the same evidence, confirming it
  rather than manufacturing a complaint when it holds up.
- **Backend review** discovers routes by scanning `Endpoints/*.cs` files
  under `<owner>/backend` (the `.MapGroup(...)` + `.MapGet/MapPost/MapPut/
  MapDelete(...)` convention) and live-probes GET routes only; it never
  invents a POST/PUT payload, since request shapes are feature-specific.
- **Frontend review supports a Vue 3 + Vuetify stack.** It detects `vue` +
  `vuetify` in an owner's `frontend/package.json`, statically discovers
  `.vue` components, `vue-router` routes, and Vuetify component tags in
  use, and best-effort probes a running dev/preview server. For any owner
  not on that stack (or with no frontend yet), it reports that clearly
  instead of guessing - each student can add their own collector under
  `collectors/` if their stack differs.
- **Docker Compose review** parses the real `docker-compose.yml` at the
  repo root - services, ports, volumes, `depends_on`, networks - and works
  automatically as more services (including a future shared one) are added.

## Structure

```text
tools/
├── agentic_loop.py            # entrypoint wrapper
└── agentic_loop/
    ├── main.py                 # interactive menu: owner -> layer, + compose, + Run All
    ├── config/review_config.py # OWNERS/LAYERS + per-owner feature context resolution
    ├── core/
    │   ├── orchestrator.py     # Observe -> Prompts -> LLM (implement) -> LLM (review) -> Done
    │   ├── prompt_registry.py
    │   ├── ai_runner.py
    │   ├── compose_utils.py    # shared docker-compose.yml parsing
    │   └── reporter.py
    ├── collectors/
    │   ├── frontend_collector.py  # Vue 3 + Vuetify component/route discovery + dev-server probe
    │   ├── backend_collector.py   # generic .NET minimal-API route discovery + GET probing
    │   ├── database_collector.py # generic SQLite schema introspection
    │   └── compose_collector.py  # docker-compose.yml evidence
    ├── pipelines/
    │   ├── frontend_pipeline.py
    │   ├── backend_pipeline.py
    │   ├── database_pipeline.py
    │   ├── compose_pipeline.py
    │   └── review_pipeline.py   # builds the review agent's prompt
    ├── prompts/
    │   ├── service/              # shared baseline prompts - same for every owner
    │   │   ├── system_prompt.txt
    │   │   ├── frontend_task_prompt.txt
    │   │   ├── backend_task_prompt.txt
    │   │   ├── database_task_prompt.txt
    │   │   ├── compose_task_prompt.txt
    │   │   ├── review_system_prompt.txt
    │   │   └── review_task_prompt.txt
    │   └── owners/                # student-authored feature context, one folder per owner
    │       └── student-3/context_prompt.txt
    └── requirements.txt
```

## Setup

```powershell
cd tools\agentic_loop
pip install -r requirements.txt
Copy-Item .env.example .env
```

Requires an OpenAI-compatible model endpoint. For a local model, install
[Ollama](https://ollama.com) and run `ollama pull llama3.1` (or set
`OLLAMA_MODEL` to whatever you have pulled). To use the real OpenAI API
instead, set `OLLAMA_BASE_URL=https://api.openai.com/v1` and
`OPENAI_API_KEY=<your key>` in `.env`.

If an owner's backend isn't reachable via a published port already declared
in `docker-compose.yml` (as `<owner>-backend`), set
`API_BASE_URL_<OWNER_SLUG>` in `.env` (e.g. `API_BASE_URL_STUDENT_3` for
`dotnet run`'s default `http://localhost:5014`).

The same applies to a Vue frontend's dev/preview server: set
`FRONTEND_BASE_URL_<OWNER_SLUG>` in `.env` if it isn't reachable via a
published `<owner>-frontend` port in `docker-compose.yml` (e.g.
`FRONTEND_BASE_URL_STUDENT_3=http://localhost:5173` for `npm run dev`'s
default Vite port). This is optional - static component/route evidence is
still collected without it.

## Run

```powershell
python tools\agentic_loop.py
```

1. Choose an owner (`student-1` .. `student-5`, `shared`), `docker-compose`,
   or `Run All`.
2. For an owner, choose a layer: `Frontend`, `Backend`, `Database`, or
   `All layers`.

Each run prints `[START] -> [OBSERVE] -> [PROMPTS] -> [LLM] -> [REVIEW-PROMPTS]
-> [REVIEW-LLM] -> [DONE]` stage banners, followed by the collected
evidence, the implementation agent's one evidence-backed recommendation (or
an explicit "no improvement found"), and the review agent's short critique
or confirmation of that recommendation.

## Adding your own feature context

Task/system/review prompts under `prompts/service/` are a shared baseline
that every owner is judged against, so results stay consistent - don't
edit these per-feature. The only thing that should vary per owner is the
**feature context**: a short, plain description of what your service is
supposed to do.

To add yours, create `prompts/owners/<your-owner>/context_prompt.txt` (see
`prompts/owners/student-3/context_prompt.txt` for an example). A few short
paragraphs is enough - what the service manages, its key entities/fields,
and anything explicitly out of scope. Until you add this file, the tool
falls back to your blurb in the root `README.md`, so it still works, just
with less detail for the agents to reason against.
