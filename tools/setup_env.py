from __future__ import annotations

import argparse
from getpass import getpass
import os
from pathlib import Path
import re
import sys
import tempfile
from urllib.parse import urlparse


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
EXAMPLE_PATH = REPOSITORY_ROOT / ".env.example"
OUTPUT_PATH = REPOSITORY_ROOT / ".env"
ASSIGNMENT_PATTERN = re.compile(r"^(?P<key>[A-Za-z_][A-Za-z0-9_]*)=(?P<value>.*)$")
PROMPTED_KEYS = {
    "OPENROUTER_API_KEY",
    "CANVAS_BASE_URL",
    "CANVAS_API_TOKEN",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create the repository .env from .env.example."
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Replace an existing .env after collecting new values.",
    )
    return parser.parse_args()


def read_defaults(lines: list[str]) -> dict[str, str]:
    defaults: dict[str, str] = {}
    for line in lines:
        match = ASSIGNMENT_PATTERN.match(line)
        if match:
            defaults[match.group("key")] = match.group("value")
    missing = PROMPTED_KEYS.difference(defaults)
    if missing:
        raise ValueError(
            f".env.example is missing required setup keys: {', '.join(sorted(missing))}"
        )
    return defaults


def prompt_values(defaults: dict[str, str]) -> dict[str, str]:
    print("Configure Better Canvas local services. Press Enter to keep shown defaults.")
    openrouter_key = getpass("OpenRouter API key (input hidden; may be left blank): ").strip()

    canvas_default = defaults["CANVAS_BASE_URL"]
    canvas_base_url = input(f"Canvas base URL [{canvas_default}]: ").strip() or canvas_default
    parsed_canvas_url = urlparse(canvas_base_url)
    if parsed_canvas_url.scheme not in {"http", "https"} or not parsed_canvas_url.netloc:
        raise ValueError("Canvas base URL must be an absolute HTTP or HTTPS URL.")

    canvas_token = getpass("Canvas API token (input hidden; may be left blank): ").strip()
    return {
        "OPENROUTER_API_KEY": openrouter_key,
        "CANVAS_BASE_URL": canvas_base_url.rstrip("/"),
        "CANVAS_API_TOKEN": canvas_token,
    }


def encode_value(value: str) -> str:
    if "\n" in value or "\r" in value:
        raise ValueError("Environment values cannot contain line breaks.")
    if not value:
        return ""
    escaped = value.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def render_environment(lines: list[str], values: dict[str, str]) -> str:
    rendered: list[str] = []
    for line in lines:
        match = ASSIGNMENT_PATTERN.match(line)
        key = match.group("key") if match else None
        rendered.append(f"{key}={encode_value(values[key])}" if key in values else line)
    return "\n".join(rendered) + "\n"


def write_environment(content: str) -> None:
    with tempfile.NamedTemporaryFile(
        mode="w",
        encoding="utf-8",
        newline="\n",
        prefix=".env.",
        suffix=".tmp",
        dir=REPOSITORY_ROOT,
        delete=False,
    ) as temporary_file:
        temporary_file.write(content)
        temporary_path = Path(temporary_file.name)

    try:
        os.chmod(temporary_path, 0o600)
        temporary_path.replace(OUTPUT_PATH)
    finally:
        temporary_path.unlink(missing_ok=True)


def main() -> int:
    args = parse_args()
    if OUTPUT_PATH.exists() and not args.force:
        print(
            f"{OUTPUT_PATH.name} already exists; it was not changed. "
            "Run again with --force to replace it.",
            file=sys.stderr,
        )
        return 1

    try:
        lines = EXAMPLE_PATH.read_text(encoding="utf-8").splitlines()
        defaults = read_defaults(lines)
        values = prompt_values(defaults)
        write_environment(render_environment(lines, values))
    except (EOFError, KeyboardInterrupt):
        print("\nEnvironment setup cancelled; .env was not changed.", file=sys.stderr)
        return 1
    except (OSError, ValueError) as exception:
        print(f"Environment setup failed: {exception}", file=sys.stderr)
        return 1

    print("Created .env. Secrets were written locally and remain ignored by Git.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
