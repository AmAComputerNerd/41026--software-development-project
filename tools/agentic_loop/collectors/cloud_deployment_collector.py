"""Collector for the Release 2 Cloud Deployment Report Review mode (`cloud-deployment`)."""

from __future__ import annotations

from pathlib import Path


def _read_excerpt(path: Path, max_lines: int = 90) -> str:
    if not path.is_file():
        return f"[MISSING] {path}"
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    snippet = "\n".join(lines[:max_lines])
    if len(lines) > max_lines:
        snippet += f"\n... ({len(lines) - max_lines} more lines)"
    return snippet


def collect(owner: str | None, repo_root: Path) -> tuple[bool, str]:
    target_owner = owner or "shared"
    sections: list[str] = [
        f"=== RELEASE 2 CLOUD DEPLOYMENT REPORT EVIDENCE (Target: {target_owner}) ===",
    ]

    bicep_path = repo_root / "infra" / "azure" / "main.bicep"
    deploy_sh = repo_root / "infra" / "azure" / "deploy.sh"
    validate_sh = repo_root / "infra" / "azure" / "validate-cloud.sh"
    cloud_wf = repo_root / ".github" / "workflows" / "cloud-deployment.yml"
    docker_ci = repo_root / ".github" / "workflows" / "docker-ci.yml"

    if not bicep_path.is_file() or not cloud_wf.is_file():
        return False, "Cloud deployment artefacts (infra/azure/main.bicep or .github/workflows/cloud-deployment.yml) are missing."

    sections.append("--- 1. Azure Infrastructure-as-Code (infra/azure/main.bicep) ---")
    sections.append(_read_excerpt(bicep_path, 110))

    sections.append("--- 2. Reusable Deployment & Validation Scripts (infra/azure/deploy.sh & validate-cloud.sh) ---")
    sections.append(f"File: infra/azure/deploy.sh\n{_read_excerpt(deploy_sh, 80)}")
    sections.append(f"File: infra/azure/validate-cloud.sh\n{_read_excerpt(validate_sh, 80)}")

    sections.append("--- 3. Cloud Deployment GitHub Actions Workflow (.github/workflows/cloud-deployment.yml) ---")
    sections.append(_read_excerpt(cloud_wf, 100))

    sections.append("--- 4. Baseline AI-Disabled & Host-Boundary Contract (.github/workflows/docker-ci.yml) ---")
    sections.append(_read_excerpt(docker_ci, 95))

    return True, "\n\n".join(sections)
