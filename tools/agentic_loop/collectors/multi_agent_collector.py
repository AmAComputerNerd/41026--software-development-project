"""Collector for the Release 2 Multi-Agent Workflow Review mode (`multi-agent`)."""

from __future__ import annotations

import json
from pathlib import Path
import urllib.error
import urllib.request


def _read_excerpt(path: Path, max_lines: int = 80) -> str:
    if not path.is_file():
        return f"[MISSING] {path}"
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    snippet = "\n".join(lines[:max_lines])
    if len(lines) > max_lines:
        snippet += f"\n... ({len(lines) - max_lines} more lines)"
    return snippet


def _probe_multi_agent_server(owner: str | None) -> str:
    base_url = "http://127.0.0.1:5004"
    lines: list[str] = []
    try:
        with urllib.request.urlopen(f"{base_url}/health/ready", timeout=3) as resp:  # noqa: S310
            lines.append(f"GET {base_url}/health/ready -> HTTP {resp.status}")
        with urllib.request.urlopen(f"{base_url}/", timeout=3) as resp:  # noqa: S310
            lines.append(f"GET {base_url}/ -> {resp.read().decode('utf-8', errors='replace')}")
        qs = f"?owner={owner}" if owner and owner != "shared" else ""
        with urllib.request.urlopen(f"{base_url}/api/workflows{qs}", timeout=4) as resp:  # noqa: S310
            workflows = json.loads(resp.read().decode("utf-8", errors="replace"))
            lines.append(f"GET {base_url}/api/workflows{qs} -> {len(workflows)} persisted workflow(s)")
            if workflows:
                latest = workflows[0]
                lines.append(
                    f"Latest Workflow ID={latest.get('id')} Status={latest.get('status')} "
                    f"Owner={latest.get('owner')} AuditEntries={len(latest.get('auditLog', []))}"
                )
    except (urllib.error.URLError, TimeoutError, ValueError) as exc:
        lines.append(f"Live HTTP probe to {base_url} skipped/offline ({exc}); using static & persisted file evidence.")
    return "\n".join(lines)


def collect(owner: str | None, repo_root: Path) -> tuple[bool, str]:
    target_owner = owner or "shared"
    sections: list[str] = [
        f"=== RELEASE 2 MULTI-AGENT WORKFLOW EVIDENCE (Target: {target_owner}) ===",
    ]

    # 1. Shared Multi-Agent Server implementation
    mas_dir = repo_root / "ai-services" / "multi-agent-server" / "MultiAgentServer"
    program_cs = mas_dir / "Program.cs"
    coordinator_cs = mas_dir / "Services" / "MultiAgentCoordinator.cs"
    endpoints_cs = mas_dir / "Endpoints" / "WorkflowEndpoints.cs"
    workflows_json = mas_dir / "data" / "workflows.json"

    if not program_cs.is_file() or not coordinator_cs.is_file():
        return False, "ai-services/multi-agent-server is missing Program.cs or MultiAgentCoordinator.cs"

    sections.append("--- 1. Shared Non-Containerised Multi-Agent Server (:5004) ---")
    sections.append(f"File: ai-services/multi-agent-server/MultiAgentServer/Program.cs\n{_read_excerpt(program_cs, 60)}")
    sections.append(
        f"File: ai-services/multi-agent-server/MultiAgentServer/Endpoints/WorkflowEndpoints.cs\n{_read_excerpt(endpoints_cs, 70)}"
    )
    sections.append(
        f"File: ai-services/multi-agent-server/MultiAgentServer/Services/MultiAgentCoordinator.cs (head)\n{_read_excerpt(coordinator_cs, 90)}"
    )

    # 2. Verify non-containerised boundary in docker-compose.yml and tools/run_ai_services.py
    compose_path = repo_root / "docker-compose.yml"
    compose_text = compose_path.read_text(encoding="utf-8", errors="replace") if compose_path.is_file() else ""
    has_containerised_mas = "\n    multi-agent-server:" in compose_text
    sections.append(
        "--- 2. Host-Service Boundary Verification ---\n"
        f"multi-agent-server containerised in docker-compose.yml: {has_containerised_mas} (Expected: False)\n"
        f"tools/run_ai_services.py includes multi-agent: "
        f"{'multi-agent-server' in (repo_root / 'tools' / 'run_ai_services.py').read_text(encoding='utf-8', errors='replace')}"
    )

    # 3. Slice integration (Student 1 or specified owner)
    slice_owner = owner if owner and owner != "shared" else "student-1"
    s1_endpoint = repo_root / slice_owner / "backend" / "Api" / "Endpoints" / "MultiAgentIntegrationEndpoints.cs"
    s1_panel = repo_root / slice_owner / "frontend" / "src" / "components" / "notifications" / "MultiAgentWorkflowPanel.vue"
    sections.append(f"--- 3. {slice_owner} Backend & Frontend Multi-Agent Integration ---")
    if s1_endpoint.is_file():
        sections.append(f"Backend Endpoint ({s1_endpoint.relative_to(repo_root)}):\n{_read_excerpt(s1_endpoint, 80)}")
    if s1_panel.is_file():
        sections.append(f"Frontend Panel ({s1_panel.relative_to(repo_root)}):\n{_read_excerpt(s1_panel, 70)}")

    # 4. Persisted workflow history & audit logs
    sections.append("--- 4. Persisted Workflow History & Audit Logs ---")
    if workflows_json.is_file():
        sections.append(f"File: {workflows_json.relative_to(repo_root)}\n{_read_excerpt(workflows_json, 90)}")
    else:
        sections.append("No workflows.json file persisted yet (will be created on first workflow run).")

    # 5. Live probe
    sections.append("--- 5. Live Multi-Agent Server Probe ---")
    sections.append(_probe_multi_agent_server(owner))

    return True, "\n\n".join(sections)
