from __future__ import annotations

import argparse
from datetime import datetime, timezone
from pathlib import Path
import shutil
import subprocess
import sys


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
SCANNERS = {
    "ruff-security": "ruff",
    "detect-secrets": "detect-secrets-hook",  # pragma: allowlist secret -- scanner executable name
    "pip-audit": "pip-audit",
}


def run_scan(scanner: str, arguments: list[str]) -> int:
    started = datetime.now(timezone.utc)
    log_directory = REPOSITORY_ROOT / ".local" / "security-scans"
    log_path = log_directory / "pre-commit.log"
    try:
        log_directory.mkdir(parents=True, exist_ok=True)
        with log_path.open("ab") as log:
            log_path.chmod(0o600)
            executable = shutil.which(SCANNERS[scanner])
            if executable is None:
                output = f"Scanner executable not found: {SCANNERS[scanner]}\n".encode()
                exit_code = 127
            else:
                try:
                    # Fixed scanner allowlist, resolved executable and argv; no shell execution.
                    result = subprocess.run(  # noqa: S603
                        [executable, *arguments],
                        cwd=REPOSITORY_ROOT,
                        stdout=subprocess.PIPE,
                        stderr=subprocess.STDOUT,
                        check=False,
                    )
                    output = result.stdout
                    exit_code = result.returncode if result.returncode >= 0 else 128 - result.returncode
                except OSError as exception:
                    output = f"Could not start {scanner}: {exception}\n".encode()
                    exit_code = 1
                except KeyboardInterrupt:
                    output = b"Security scan interrupted.\n"
                    exit_code = 130
            log.write(
                f"\n=== Security scan ===\nScanner: {scanner}\nStarted (UTC): {started.isoformat()}\n\n".encode()
                + output
                + f"\nFinished (UTC): {datetime.now(timezone.utc).isoformat()}\n"
                f"Exit code: {exit_code}\n".encode()
            )
        sys.stdout.write(output.decode("utf-8", errors="replace"))
        print(f"Scan log: {log_path.relative_to(REPOSITORY_ROOT)}")
        return exit_code
    except OSError as exception:
        print(f"Security scan logging failed: {exception}", file=sys.stderr)
        return 1


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a security scanner and save its local log.")
    parser.add_argument("scanner", choices=SCANNERS)
    parser.add_argument("arguments", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    return run_scan(args.scanner, args.arguments)


if __name__ == "__main__":
    raise SystemExit(main())
