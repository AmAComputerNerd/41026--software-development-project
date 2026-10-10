from __future__ import annotations

import argparse
from pathlib import Path
import subprocess
import sys
import venv


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
ENVIRONMENT_PATH = REPOSITORY_ROOT / ".local" / "pre-commit"
PRE_COMMIT_VERSION = "4.6.2"


def main() -> int:
    argparse.ArgumentParser(
        description="Install local commit-time security hooks and scan the repository."
    ).parse_args()
    if sys.version_info < (3, 10):
        print("Security setup requires Python 3.10 or newer.", file=sys.stderr)
        return 1

    environment_python = ENVIRONMENT_PATH / (
        "Scripts/python.exe" if sys.platform == "win32" else "bin/python"
    )
    stage = "creating the local Python environment"
    try:
        if not environment_python.is_file():
            print(f"Creating {ENVIRONMENT_PATH.relative_to(REPOSITORY_ROOT)}", flush=True)
            venv.EnvBuilder(with_pip=True).create(ENVIRONMENT_PATH)

        commands = (
            (
                "installing pre-commit",
                ["pip", "install", f"pre-commit=={PRE_COMMIT_VERSION}"],
            ),
            (
                "installing the Git hook and scanner environments",
                ["pre_commit", "install", "--install-hooks"],
            ),
            (
                "running the initial repository security scan",
                ["pre_commit", "run", "--all-files"],
            ),
        )
        for stage, arguments in commands:
            print(f"Security setup: {stage}", flush=True)
            # Repository-local interpreter and fixed argument lists; no shell execution.
            subprocess.run(  # noqa: S603
                [str(environment_python), "-m", *arguments],
                cwd=REPOSITORY_ROOT,
                check=True,
            )
    except subprocess.CalledProcessError as exception:
        print(
            f"Security setup failed while {stage} (exit code {exception.returncode}). "
            "Resolve the errors above and rerun this script; no findings were suppressed.",
            file=sys.stderr,
        )
        return exception.returncode
    except OSError as exception:
        print(f"Security setup failed while {stage}: {exception}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\nSecurity setup cancelled.", file=sys.stderr)
        return 130

    print("Security hooks are installed and the initial scan passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
