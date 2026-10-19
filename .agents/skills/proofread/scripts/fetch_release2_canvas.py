#!/usr/bin/env python3
"""Fetch the Release 2 assignment page (course 39716, assignment 267943) via Isaac's Canvas API (shared-backend).

Follows Rule 2 of AGENTS.md:
- Never calls Canvas LMS directly from downstream consumers when shared-backend is used;
  routes through shared-backend (`/api/canvas/courses/{courseId}/assignments`).
- Automatically detects if `shared-backend` is already running on :8080, :5110, or :5010.
- If not running, temporarily boots `shared/backend/Api/Api.csproj` on an ephemeral local port,
  queries `/api/canvas/courses/39716/assignments`, extracts assignment 267943, and shuts down cleanly.
"""

from __future__ import annotations

import argparse
import http.client
import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.parse
from pathlib import Path
from typing import Any

DEFAULT_COURSE_ID = 39716
DEFAULT_ASSIGNMENT_ID = 267943
CANDIDATE_BASE_URLS = [
    "http://localhost:5110",
    "http://localhost:8080",
    "http://localhost:5010",
]


def repo_root() -> Path:
    return Path(__file__).resolve().parents[4]


def try_fetch_assignments(
    base_url: str, course_id: int, timeout: float = 15.0
) -> tuple[list[dict[str, Any]] | None, str]:
    parsed = urllib.parse.urlparse(base_url)
    host = parsed.hostname or "127.0.0.1"
    port = parsed.port or (443 if parsed.scheme == "https" else 80)
    path = f"/api/canvas/courses/{course_id}/assignments"
    conn = http.client.HTTPConnection(host, port, timeout=timeout)
    try:
        conn.request("GET", path, headers={"Accept": "application/json"})
        resp = conn.getresponse()
        body = resp.read().decode("utf-8", errors="replace")
        if resp.status == 200:
            data = json.loads(body)
            if isinstance(data, list):
                return data, "OK"
        return None, f"HTTP {resp.status}: {body[:400]}"
    except Exception as exc:
        return None, str(exc)
    finally:
        conn.close()


def find_free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind(("127.0.0.1", 0))
        return int(s.getsockname()[1])


def wait_until_ready(base_url: str, timeout_sec: float = 25.0) -> bool:
    parsed = urllib.parse.urlparse(base_url)
    host = parsed.hostname or "127.0.0.1"
    port = parsed.port or 80
    deadline = time.time() + timeout_sec
    while time.time() < deadline:
        conn = http.client.HTTPConnection(host, port, timeout=2.0)
        try:
            conn.request("GET", "/health/live")
            resp = conn.getresponse()
            resp.read()
            if resp.status in (200, 404):
                return True
        except OSError:
            time.sleep(0.3)
        finally:
            conn.close()
    return False


def fetch_via_ephemeral_shared_backend(course_id: int) -> list[dict[str, Any]]:
    root = repo_root()
    project_path = root / "shared" / "backend" / "Api" / "Api.csproj"
    dll_path = root / "shared" / "backend" / "Api" / "bin" / "Debug" / "net10.0" / "Api.dll"

    port = find_free_port()
    base_url = f"http://127.0.0.1:{port}"
    env = os.environ.copy()
    env["ASPNETCORE_URLS"] = base_url
    env["ASPNETCORE_ENVIRONMENT"] = "Development"
    env["DOTNET_CLI_HOME"] = str(root / ".dotnet")
    for proxy_var in ("HTTP_PROXY", "HTTPS_PROXY", "http_proxy", "https_proxy"):
        env.pop(proxy_var, None)

    temp_db = Path(tempfile.gettempdir()) / f"shared-canvas-proofread-{port}.db"
    env["ConnectionStrings__DefaultConnection"] = f"Data Source={temp_db}"

    dotnet_bin = shutil.which("dotnet") or "dotnet"
    if dll_path.exists():
        cmd = [dotnet_bin, str(dll_path)]
    else:
        cmd = [dotnet_bin, "run", "--project", str(project_path), "--no-launch-profile"]

    # Fixed local dotnet command without shell execution.
    proc = subprocess.Popen(  # noqa: S603
        cmd,
        cwd=str(root / "shared" / "backend" / "Api"),
        env=env,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    try:
        if not wait_until_ready(base_url):
            proc.terminate()
            stdout, stderr = proc.communicate(timeout=5)
            raise RuntimeError(f"shared-backend failed to start on {base_url}: {stdout}\n{stderr}")

        assignments, err_detail = try_fetch_assignments(base_url, course_id, timeout=30.0)
        if assignments is None:
            raise RuntimeError(
                f"shared-backend started on {base_url}, but GET /api/canvas/courses/{course_id}/assignments failed "
                f"({err_detail}). Verify CANVAS_BASE_URL and CANVAS_API_TOKEN in root .env."
            )
        return assignments
    finally:
        proc.terminate()
        try:
            proc.wait(timeout=5)
        except subprocess.TimeoutExpired:
            proc.kill()


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Fetch Release 2 assignment specifications via Isaac's shared-backend Canvas API."
    )
    parser.add_argument("--course-id", type=int, default=DEFAULT_COURSE_ID)
    parser.add_argument("--assignment-id", type=int, default=DEFAULT_ASSIGNMENT_ID)
    parser.add_argument("--json", action="store_true", help="Output raw JSON DTO")
    parser.add_argument("--out", type=str, default=None, help="Optional file path to save output")
    args = parser.parse_args()

    assignments: list[dict[str, Any]] | None = None
    used_source = ""

    for candidate in CANDIDATE_BASE_URLS:
        assignments, _ = try_fetch_assignments(candidate, args.course_id)
        if assignments is not None:
            used_source = candidate
            break

    if assignments is None:
        used_source = "ephemeral shared-backend instance"
        assignments = fetch_via_ephemeral_shared_backend(args.course_id)

    target = next((a for a in assignments if a.get("id") == args.assignment_id), None)
    if target is None:
        available = [f"{a.get('id')}: {a.get('name')}" for a in assignments]
        print(
            f"Assignment {args.assignment_id} not found in course {args.course_id}. "
            f"Available assignments: {available}",
            file=sys.stderr,
        )
        return 1

    if args.json:
        output = json.dumps(target, indent=2, ensure_ascii=False)
    else:
        lines = [
            f"# Canvas Assignment {target.get('id')}: {target.get('name')}",
            f"- **Fetched via**: Isaac's Canvas Gateway (`shared-backend` @ {used_source})",
            f"- **Course ID**: {target.get('courseId')}",
            f"- **Assignment ID**: {target.get('id')}",
            f"- **Due At**: {target.get('dueAt')}",
            f"- **Updated At**: {target.get('updatedAt')}",
            f"- **Workflow State**: {target.get('workflowState')}",
            "",
            "## Sanitized Assignment Description",
            "",
            target.get("description") or "(No description)",
        ]
        output = "\n".join(lines)

    if args.out:
        out_path = Path(args.out)
        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_text(output, encoding="utf-8")
        print(f"Saved assignment {args.assignment_id} to {out_path}")
    else:
        print(output)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
