"""
Central configuration for the agentic loop.
"""

from __future__ import annotations

import re
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parent.parent
REPO_ROOT = TOOLS_ROOT.parent.parent

PROMPT_ROOT = TOOLS_ROOT / "prompts" / "service"
OWNER_PROMPT_ROOT = TOOLS_ROOT / "prompts" / "owners"
README_PATH = REPO_ROOT / "README.md"

# Each individual student's working directory, plus the cross-cutting
# "shared" microservice all students contribute to.
OWNERS = ["student-1", "student-2", "student-3", "student-4", "student-5", "shared"]

LAYERS = [
    ("frontend", "Frontend"),
    ("backend", "Backend"),
    ("database", "Database"),
]

REQUEST_TIMEOUT_SECONDS = 5.0


def owner_path(owner: str, repo_root: Path = REPO_ROOT) -> Path:
    return repo_root / owner


def _readme_blocks(readme_path: Path) -> list[str]:
    if not readme_path.exists():
        return []
    text = readme_path.read_text(encoding="utf-8")
    return [block.strip() for block in re.split(r"\n\s*\n", text) if block.strip()]


def _readme_owner_context(owner: str, repo_root: Path) -> str:
    blocks = _readme_blocks(repo_root / "README.md")

    for block in blocks:
        match = re.search(r"Working directory:\s*`([^`/]+)/?`", block)
        if match and match.group(1).strip() == owner:
            return block

    if owner == "shared" and blocks:
        return blocks[0]

    return (
        f"No feature description found in README.md for '{owner}', and no "
        f"prompts/owners/{owner}/context_prompt.txt has been written yet. "
        "Add one so review prompts can compare evidence against it."
    )


def get_owner_context(owner: str, repo_root: Path = REPO_ROOT) -> tuple[str, str]:
    """Return `(context_text, source)` describing what `owner` is building.

    Prefers a student-authored `prompts/owners/<owner>/context_prompt.txt`;
    falls back to a README.md-derived blurb if that file doesn't exist yet,
    so the loop still works before every student has written one.
    """
    owner_prompt_path = OWNER_PROMPT_ROOT / owner / "context_prompt.txt"
    if owner_prompt_path.exists():
        return owner_prompt_path.read_text(encoding="utf-8").strip(), str(owner_prompt_path)

    return _readme_owner_context(owner, repo_root), "README.md (fallback - no context_prompt.txt yet)"
