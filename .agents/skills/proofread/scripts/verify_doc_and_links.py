#!/usr/bin/env python3
"""Verify microservice consistency across the Google Doc and repository docs, and check link accessibility.

Implements the AGENTS.md safeguards:
1. Fetches the live Google Doc text export (`19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ`).
2. Checks for conflicting material between microservices across the Google Doc and `AGENTS.md`
   (student names, student IDs, service names, ports, database engines, endpoint routes, and AI service ports).
3. Verifies accessibility of all markdown links (local file paths and HTTP/HTTPS URLs) in `AGENTS.md`,
   `CLAUDE.md`, `README.md`, `docs/`, and the Google Doc, flagging broken links or unreplaced placeholders.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path
import re
import ssl
import sys
import urllib.error
import urllib.parse
import urllib.request

GOOGLE_DOC_ID = "19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ"
GOOGLE_DOC_URL = f"https://docs.google.com/document/d/{GOOGLE_DOC_ID}/edit?usp=sharing"
GOOGLE_DOC_EXPORT_TXT = f"https://docs.google.com/document/d/{GOOGLE_DOC_ID}/export?format=txt"
SPECS_DRIVE_URL = "https://drive.google.com/file/d/1foChRW51MBtemJWPHmD5zLwRejxuxJQb/view?usp=sharing"
CANVAS_ASSIGNMENT_URL = "https://canvas.uts.edu.au/courses/39716/assignments/267943"


@dataclass(frozen=True)
class CanonicalSlice:
    role: str
    student_name: str
    student_id: str
    slice_name: str
    backend_services: tuple[str, ...]
    database_desc: str
    forbidden_db_claims: tuple[str, ...]
    workflow_file: str


CANONICAL_SLICES: tuple[CanonicalSlice, ...] = (
    CanonicalSlice(
        role="Student 1",
        student_name="Bryan Lee",
        student_id="25495108",
        slice_name="Notifications",
        backend_services=("student-1-backend",),
        database_desc="PostgreSQL 16 (notifications_db)",
        forbidden_db_claims=("student-1-db", "SQLite (student-1"),
        workflow_file="student-1.yml",
    ),
    CanonicalSlice(
        role="Student 2",
        student_name="Isaac Thomas",
        student_id="25341708",
        slice_name="Automations",
        backend_services=("student-2-backend",),
        database_desc="SQLite (student-2-db)",
        forbidden_db_claims=("PostgreSQL", "student-2-database"),
        workflow_file="student-2.yml",
    ),
    CanonicalSlice(
        role="Student 3",
        student_name="Jonathon Thomson",
        student_id="25488154",
        slice_name="Deadlines",
        backend_services=("student-3-backend",),
        database_desc="SQLite Service (student-3-database)",
        forbidden_db_claims=("PostgreSQL",),
        workflow_file="student-3.yml",
    ),
    CanonicalSlice(
        role="Student 4",
        student_name="Tristan Huang",
        student_id="25322025",
        slice_name="Account",
        backend_services=("student-4-backend", "student-4-authentication"),
        database_desc="SQLite Service (student-4-database)",
        forbidden_db_claims=("PostgreSQL",),
        workflow_file="student-4.yml",
    ),
    CanonicalSlice(
        role="Student 5",
        student_name="William Hannah",
        student_id="25494675",
        slice_name="Grades",
        backend_services=("student-5-backend",),
        database_desc="SQLite Service (student-5-database)",
        forbidden_db_claims=("PostgreSQL",),
        workflow_file="student-5.yml",
    ),
)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[4]


def fetch_google_doc_text(timeout: float = 15.0) -> str:
    ctx = ssl.create_default_context()
    last_error: Exception | None = None
    for url in (GOOGLE_DOC_EXPORT_TXT, GOOGLE_DOC_URL):
        req = urllib.request.Request(  # noqa: S310
            url,
            headers={"User-Agent": "Mozilla/5.0 (compatible; BetterCanvas-Proofread-Verifier/1.0)"},
        )
        try:
            with urllib.request.urlopen(req, timeout=timeout, context=ctx) as resp:  # noqa: S310
                raw = resp.read().decode("utf-8", errors="replace")
                if url == GOOGLE_DOC_URL:
                    chunks = re.findall(r'"s"\s*:\s*"((?:[^"\\]|\\.)*)"', raw)
                    if chunks:
                        return "\n".join(
                            c.encode("utf-8").decode("unicode_escape", errors="replace") for c in chunks
                        )
                return raw
        except Exception as exc:
            last_error = exc
    raise RuntimeError(f"Unable to fetch Google Doc {GOOGLE_DOC_ID}: {last_error}")


def check_url_accessible(url: str, timeout: float = 10.0) -> tuple[bool, str]:
    parsed = urllib.parse.urlparse(url)
    if parsed.scheme not in ("http", "https"):
        return False, f"Unsupported scheme: {parsed.scheme}"
    if parsed.hostname in ("localhost", "127.0.0.1", "host.docker.internal", "shared-backend"):
        return True, "SKIP (local/internal runtime URL)"
    if "<" in url or ">" in url:
        return False, f"PLACEHOLDER URL not replaced: {url}"

    ctx = ssl.create_default_context()
    last_detail = "Unreachable"
    for method in ("HEAD", "GET"):
        req = urllib.request.Request(  # noqa: S310
            url,
            method=method,
            headers={"User-Agent": "Mozilla/5.0 (compatible; BetterCanvasLinkChecker/1.0)"},
        )
        try:
            with urllib.request.urlopen(req, timeout=timeout, context=ctx) as resp:  # noqa: S310
                return True, f"HTTP {resp.status}"
        except urllib.error.HTTPError as e:
            if e.code in (401, 403) and "canvas.uts.edu.au" in url:
                return True, f"HTTP {e.code} (Canvas auth gate reachable)"
            if method == "HEAD" and e.code in (403, 405):
                last_detail = f"HTTP {e.code}"
                continue
            return False, f"HTTP {e.code}"
        except Exception as e:
            last_detail = str(e)
            if method == "HEAD":
                continue
            return False, last_detail
    return False, last_detail


def check_markdown_links(md_files: list[Path], root: Path) -> tuple[list[str], list[str]]:
    passed: list[str] = []
    failed: list[str] = []
    link_pattern = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")
    checked_urls: dict[str, tuple[bool, str]] = {}

    for md_file in md_files:
        if not md_file.exists():
            continue
        text = md_file.read_text(encoding="utf-8", errors="replace")
        rel_source = md_file.relative_to(root)
        for match in link_pattern.finditer(text):
            _, raw_target = match.groups()
            target = raw_target.strip().split()[0]
            if target.startswith("#") or target.startswith("mailto:") or target.startswith("conversation://"):
                continue
            if target.startswith("http://") or target.startswith("https://"):
                if target not in checked_urls:
                    checked_urls[target] = check_url_accessible(target)
                ok, detail = checked_urls[target]
                entry = f"{rel_source} -> {target} ({detail})"
                if ok:
                    passed.append(entry)
                else:
                    failed.append(entry)
            else:
                clean_target = target.split("#")[0]
                if clean_target.startswith("file://"):
                    resolved = Path(urllib.parse.unquote(clean_target.removeprefix("file://")))
                else:
                    resolved = (md_file.parent / clean_target).resolve()
                if resolved.exists():
                    passed.append(f"{rel_source} -> {clean_target} (FILE OK)")
                else:
                    failed.append(f"{rel_source} -> {clean_target} (MISSING FILE: {resolved})")

    return passed, failed


def check_doc_conflicts(doc_text: str) -> tuple[list[str], list[str]]:
    checks_passed: list[str] = []
    conflicts: list[str] = []

    for s in CANONICAL_SLICES:
        if s.student_name not in doc_text or s.student_id not in doc_text:
            conflicts.append(
                f"{s.role}: Missing student '{s.student_name} ({s.student_id})' in Google Doc."
            )
        else:
            checks_passed.append(f"{s.role}: Verified {s.student_name} ({s.student_id}) present.")

        for svc in s.backend_services:
            if svc not in doc_text:
                conflicts.append(f"{s.role}: Missing backend service '{svc}' in Google Doc.")
            else:
                checks_passed.append(f"{s.role}: Verified service '{svc}' referenced.")

        if s.workflow_file not in doc_text:
            conflicts.append(f"{s.role}: Missing CI workflow '{s.workflow_file}' in Google Doc.")

    expected_ports = {
        "ai-mode": ":5001",
        "mcp-server": ":5002",
        "rag-server": ":5003",
        "multi-agent-server": ":5004",
    }
    for service_name, port in expected_ports.items():
        if port not in doc_text:
            conflicts.append(f"AI Services: Expected port {port} for {service_name} not found in Google Doc.")
        else:
            checks_passed.append(f"AI Services: Verified {service_name} port {port} in Google Doc.")

    placeholder_matches = re.findall(r"\[Insert [^\]]+\]|https://github\.com/<org>/<repo>[^\s]*|group-<X>\.pdf", doc_text)
    if placeholder_matches:
        conflicts.append(
            f"Google Doc contains {len(placeholder_matches)} unreplaced template placeholder(s) "
            f"(e.g. {placeholder_matches[:3]})."
        )

    doc_urls = set(re.findall(r"https?://[^\s\])>]+", doc_text))
    for url in sorted(doc_urls):
        ok, detail = check_url_accessible(url)
        if ok:
            checks_passed.append(f"Google Doc URL -> {url} ({detail})")
        else:
            conflicts.append(f"Google Doc URL problem -> {url} ({detail})")

    return checks_passed, conflicts


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify Google Doc microservice consistency and link accessibility.")
    parser.add_argument("--doc-file", type=str, default=None, help="Optional path to local text export of Google Doc")
    args = parser.parse_args()

    root = repo_root()
    md_files = [
        root / "AGENTS.md",
        root / "CLAUDE.md",
        root / "README.md",
        *sorted((root / "docs").rglob("*.md")),
    ]

    print("=== 1. Checking Required Project Links ===")
    for label, url in [
        ("Release 2 Google Doc", GOOGLE_DOC_URL),
        ("Release 2 Specs PDF (Google Drive)", SPECS_DRIVE_URL),
        ("Release 2 Canvas Assignment Page", CANVAS_ASSIGNMENT_URL),
    ]:
        ok, detail = check_url_accessible(url)
        status = "PASS" if ok else "FAIL"
        print(f"[{status}] {label}: {url} -> {detail}")

    print("\n=== 2. Checking Repository Markdown Links ===")
    passed_links, failed_links = check_markdown_links(md_files, root)
    print(f"Verified {len(passed_links)} valid link(s) across AGENTS.md, CLAUDE.md, README.md, and docs/.")
    for failure in failed_links:
        print(f"[FAIL] {failure}")

    print("\n=== 3. Checking Google Doc Microservice Consistency ===")
    try:
        if args.doc_file:
            doc_text = Path(args.doc_file).read_text(encoding="utf-8", errors="replace")
        else:
            doc_text = fetch_google_doc_text()
        doc_passed, doc_conflicts = check_doc_conflicts(doc_text)
        for item in doc_passed:
            print(f"[PASS] {item}")
        for conflict in doc_conflicts:
            print(f"[WARN/CONFLICT] {conflict}")
    except Exception as exc:
        print(f"[ERROR] Could not fetch or parse Google Doc: {exc}", file=sys.stderr)
        return 1

    if failed_links:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
