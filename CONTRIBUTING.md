# Contributing

How to work on this repo, regardless of which student slice you own.

## Repo layout

```
.
├── ai-services/ai-mode/         # OpenRouter gateway (one shared API key)
├── shared/
│   ├── backend/                 # Canvas API integration
│   ├── frontend/                # dashboard shell + nginx reverse proxy
│   └── ui-kit/                  # @better-canvas/ui-kit workspace package
├── student-1/                   # Bryan — notifications
├── student-2/                   # Isaac — automations
├── student-3/                   # Jonathon — deadlines & tasks
├── student-4/                   # Tristan — account & authentication
├── student-5/                   # William — grades & progress
├── docs/                        # architecture + playbooks
└── docker-compose.yml           # wires everything together
```

Each `student-N/` has its own `backend/` (ASP.NET Core) and `frontend/`
(Vue 3 + plain SCSS). Students 3, 4, and 5 additionally have a private
`database/` service that exclusively owns EF Core and SQLite, plus a
`contracts/` project for the internal HTTP contract; student 1 owns a
dedicated PostgreSQL container under `database/`, and student 2 keeps
EF Core inside its API. Cross-service calls go through HTTP; never read
another service's database directly.

## Branch workflow

- Branch off `main` per feature. Don't stack feature branches on other
  open feature branches.
- Commit after each logical step, not one giant commit at the end.
  Push as you go so others can see progress.
- Open a PR into `main` when done. Don't push directly to `main`.
- CI runs per service group (see `.github/workflows/`). Each workflow
  is path-filtered to its own directories, so unrelated changes don't
  trigger unrelated builds.

## Commit messages

Short subject line, imperative mood. Body when the diff needs context.
Reference the student slice in the subject when the change is
slice-specific, e.g. `student-3: add subtask reordering`.

## Adding a new service (or your first slice)

If you're scaffolding a new frontend, follow
[`docs/playbooks/new-frontend-microservice.md`](docs/playbooks/new-frontend-microservice.md).
It covers:

- workspace wiring (root `package.json`),
- depending on `@better-canvas/ui-kit` for tokens, fonts, and shared
  components,
- Dockerfile shape (multi-stage `node:22-alpine` build, `nginx:1.27-alpine`
  serve),
- the entry in root `docker-compose.yml`,
- the nginx route block in `shared/frontend/nginx.conf`,
- the dashboard tile registration in `shared/ui-kit/src/services.ts`
  and the matching icon + description in
  `shared/frontend/src/data/tiles.ts`.

For new backends, follow
[`docs/playbooks/new-backend-microservice.md`](docs/playbooks/new-backend-microservice.md):
an ASP.NET Core 10 public API plus a private EF Core + SQLite database
service and a shared contracts project. The `shared-backend` Canvas
gateway is the only place that should hit the Canvas API directly. For
AI features, call the `ai-mode` gateway rather than OpenRouter directly
— it holds the only API key the project uses.

## Design tokens and theming

All frontends pull design tokens (colours, borders, shadows, spacing,
fonts) from `@better-canvas/ui-kit`. Don't hardcode values in
component styles — use the CSS custom properties from
`shared/ui-kit/src/styles/tokens.css`. The token names are prefixed
`--nb-` (neobrutalist).

If a new token is needed, add it to `tokens.css` with both its light
and dark variant if it has one, and document the use case in a short
inline comment. Components across every frontend pick up the new
token on their next `npm run build`.

## Service boundaries

Each backend owns its own database — either through its own EF Core
context or, for students 3, 4, and 5, through a private database
service reached over HTTP. No service reads another service's database
directly; cross-service data goes through HTTP APIs only.
`shared/backend` owns Canvas API integration; everything else calls it
over HTTP rather than hitting Canvas directly.

## AI features

Don't call OpenRouter from a new backend. Call the shared `ai-mode`
gateway instead (`http://ai-mode:8080/v1/chat/completions`
from Docker). The gateway holds the only `OPENROUTER_API_KEY` any service
needs. The root `.env` is the source of truth; Compose injects the key
and model only into the AI Mode container.

If an AI feature returns 500 with no obvious cause, check
`OPENROUTER_API_KEY` first.

## Verifying your change locally

### Commit-time security checks

Each contributor must install the local Git hook once per clone (Python 3.10+
required). From the repository root:

```bash
python tools/setup_precommit_security.py
```

The script creates or reuses `.local/pre-commit`, installs pre-commit and
the scanners configured in `.pre-commit-config.yaml`, installs the Git
hook, and runs the initial scan. No virtualenv activation is required.
Rerunning it is safe; errors or scan findings return a nonzero exit code.
It does not commit, stage files, or automatically add exclusions.

For the manual commands below, activate the environment first:

```bash
# Linux/macOS:
source .local/pre-commit/bin/activate
# Windows PowerShell instead:
# .local/pre-commit/Scripts/Activate.ps1
```

Normal `git commit` commands then run Ruff's `S` security rules on staged
Python files, detect-secrets on staged text files, and pip-audit on the
agentic loop's declared Python requirements and their resolved dependencies.
pip-audit runs on every commit, including commits without Python changes.
The checks do not modify source files and failures block the commit.

The first installation downloads isolated scanner environments. Dependency
auditing requires network access to the package index and PyPI vulnerability
service; it sends package metadata, not application source or credentials.
Secret verification is disabled so candidate credentials are never sent to
external services. The `.secrets.baseline` contains only reviewed false
positives: skill-lock integrity hashes, an EF migration identifier, CI
placeholder tokens, and the unimplemented Canvas key placeholder.
The two existing Student 4 demo login passwords have explicit inline
exceptions: this is a temporary development risk acceptance, not a claim
that those credentials are safe for cloud use. Other seeder lines and
all CI workflows remain scanned. Remove real credentials and rotate any
that have been exposed. Ruff's documented file-specific exceptions in `ruff.toml` cover
reviewed local process invocation, loopback health probes, and best-effort
console encoding setup; the other security rules remain enabled.

Passing a scan does not establish that the application or cloud deployment
is secure.

#### Adding reviewed exclusions

No hook command changes are needed for ordinary exceptions:

- **One secret-detector false positive:** put `pragma: allowlist secret`
  in a comment on that exact line (`#` in Python/YAML, `//` in C#).
  Add a nearby explanation; the rest of the file stays scanned.
- **One Ruff finding:** use `# noqa: Sxxx` on the exact line with a
  justification. File-specific rule exceptions belong in `ruff.toml`
  under `[lint.per-file-ignores]`, not in the hook command.
- **Generated files that cannot carry comments:** use the reviewed
  `.secrets.baseline`. Generate a candidate locally and audit it:
  ```bash
  python -m pip install detect-secrets==1.5.0
  detect-secrets scan --baseline .secrets.baseline --no-verify > .local/secrets-candidate.baseline
  detect-secrets audit .local/secrets-candidate.baseline
  ```
  Remove real findings from the source first. Only replace the committed
  baseline after every remaining entry is reviewed as a false positive,
  and review the diff. Baseline entries match secret hashes; a different
  token pasted into a CI workflow is not automatically exempt.
- **Whole-file exceptions:** use an anchored `exclude` regex on the
  relevant hook in `.pre-commit-config.yaml`, with the reason documented.
  Reserve this for reviewed fixtures/generated data. Do not exclude
  `.github/workflows`, all seeders, or all migrations.

Do not regenerate the baseline merely to make a failed scan pass.

Keep the installed environment available; reinstall the hook if it is moved
or removed. Git hooks are local and are not activated merely by cloning.
Existing .NET dependency checks remain separate; these hooks do not scan
C# or TypeScript for insecure code.

For assessment evidence, retain the actual commit-time output locally along
with the commit ID, date, scanner versions, and any findings/resolutions.
For a readable full-repository scan transcript:

```bash
# Bash: preserve the scanner exit status while saving its output.
set -o pipefail
pre-commit run --all-files --verbose 2>&1 | tee .local/pre-commit-security.log
```

Review/redact logs before sharing them: secret findings can contain source
snippets. A manual scan transcript supplements, but does not replace,
evidence that checks executed during a commit.

### Running the application

```bash
docker compose up                       # bring the stack up
docker compose logs -f <service>        # tail a service
docker compose down                     # stop
docker compose down -v                  # stop and wipe database volumes
```

For frontend iteration, `npm run dev` from inside the frontend's
directory is usually faster than rebuilding the Docker image. The
backend URL the dev server hits is baked in at build time (see the
`VITE_*_API_BASE_URL` lines in each frontend's Dockerfile).

For backend iteration, `dotnet run` from a backend's project
directory works as long as the env vars it needs are set (notably
`OPENROUTER_API_KEY`, `SharedService__BaseUrl`,
`AiGateway__BaseUrl`, and `DatabaseService__BaseUrl` if applicable).
Most cross-service calls only work in compose, not standalone.

## Where to ask questions

For architecture-level questions, see
[`docs/architecture/overview.md`](docs/architecture/overview.md). For
questions about your own slice, reach out to the student who owns it
per the team roster in `README.md`.
