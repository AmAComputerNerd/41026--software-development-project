"""Collector for the Release 2 Testing Report Review mode (`testing`)."""

from __future__ import annotations

from pathlib import Path
import subprocess
import sys

SLICE_ENDPOINT_TESTS: dict[str, tuple[str, ...]] = {
    "student-2": (
        "student-2/backend/Api.Tests/AutomationEndpointsTests.cs",
        "student-2/backend/Api.Tests/QuizReviewEndpointsTests.cs",
    ),
}


def _read_excerpt(path: Path, max_lines: int = 80) -> str:
    if not path.is_file():
        return f"[MISSING] {path}"
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    snippet = "\n".join(lines[:max_lines])
    if len(lines) > max_lines:
        snippet += f"\n... ({len(lines) - max_lines} more lines)"
    return snippet


def _run_quick_command(args: list[str], cwd: Path, timeout: int = 25) -> str:
    try:
        proc = subprocess.run(  # noqa: S603
            args,
            cwd=cwd,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=timeout,
            check=False,
        )
        out = (proc.stdout + "\n" + proc.stderr).strip()
        lines = out.splitlines()
        tail = "\n".join(lines[-25:]) if len(lines) > 25 else out
        return f"ExitCode={proc.returncode}\n{tail}"
    except Exception as exc:
        return f"Execution skipped/unavailable: {exc}"


def collect(owner: str | None, repo_root: Path) -> tuple[bool, str]:
    target_owner = owner or "shared"
    sections: list[str] = [
        f"=== RELEASE 2 TESTING REPORT EVIDENCE (Target: {target_owner}) ===",
    ]

    # 1. Pre-Commit Security Testing Configuration (Criterion 3)
    precommit_cfg = repo_root / ".pre-commit-config.yaml"
    ruff_toml = repo_root / "ruff.toml"
    security_ci = repo_root / ".github" / "workflows" / "security-ci.yml"
    setup_security = repo_root / "tools" / "setup_precommit_security.py"

    if not precommit_cfg.is_file():
        return False, ".pre-commit-config.yaml is missing."

    sections.append("--- 1. Pre-Commit Security Configuration (Ruff Security, detect-secrets, pip-audit) ---")
    sections.append(f"File: .pre-commit-config.yaml\n{_read_excerpt(precommit_cfg, 50)}")
    sections.append(f"File: ruff.toml\n{_read_excerpt(ruff_toml, 30)}")
    sections.append(f"File: .github/workflows/security-ci.yml\n{_read_excerpt(security_ci, 45)}")
    sections.append(f"File: tools/setup_precommit_security.py\n{_read_excerpt(setup_security, 50)}")

    # 2. Live Ruff check on repository Python tools
    sections.append("--- 2. Live Static Security Check (Python Tools Syntax & Security) ---")
    sections.append(
        _run_quick_command(
            [sys.executable, "-m", "py_compile", "tools/setup_precommit_security.py", "tools/run_ai_services.py"],
            repo_root,
        )
    )

    # 3. Post-Commit CI/CD Endpoint Testing Workflows (Criterion 4: student-1.yml .. student-5.yml)
    sections.append("--- 3. Post-Commit CI/CD Workflows (student-1.yml .. student-5.yml) ---")
    owners_to_inspect = (
        [owner]
        if owner and owner in {"student-1", "student-2", "student-3", "student-4", "student-5"}
        else ["student-1", "student-2", "student-3", "student-4", "student-5"]
    )
    for student in owners_to_inspect:
        wf_path = repo_root / ".github" / "workflows" / f"{student}.yml"
        if not wf_path.is_file():
            wf_path = repo_root / ".github" / "workflows" / f"{student}-ci.yml"
        sections.append(f"Workflow ({wf_path.relative_to(repo_root)}):\n{_read_excerpt(wf_path, 95)}")

    # 4. Student 1 Backend Endpoint & Multi-Agent Test Files
    s1_notif_tests = (
        repo_root / "student-1" / "backend" / "Api.Tests" / "Integration" / "NotificationEndpointsTests.cs"
    )
    s1_ma_tests = (
        repo_root / "student-1" / "backend" / "Api.Tests" / "Integration" / "MultiAgentIntegrationEndpointsTests.cs"
    )
    if s1_notif_tests.is_file():
        sections.append(
            f"--- 4. Student 1 Endpoint Test Suite (NotificationEndpointsTests.cs) ---\n{_read_excerpt(s1_notif_tests, 75)}"
        )
    if s1_ma_tests.is_file():
        sections.append(
            f"--- 5. Student 1 Multi-Agent Endpoint Test Suite (MultiAgentIntegrationEndpointsTests.cs) ---\n{_read_excerpt(s1_ma_tests, 75)}"
        )

    for student, test_files in SLICE_ENDPOINT_TESTS.items():
        if student not in owners_to_inspect:
            continue
        for relative in test_files:
            sections.append(f"--- {student} Endpoint Test Suite ({relative}) ---\n{_read_excerpt(repo_root / relative, 75)}")

    return True, "\n\n".join(sections)
