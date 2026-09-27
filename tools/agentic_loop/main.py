from __future__ import annotations

import argparse
import sys

from config.review_config import LAYERS, OWNERS, REPO_ROOT
from core.ai_runner import AIRunner
from core.log_writer import capture_run
from core.orchestrator import run_target
from core.reporter import print_layer_menu, print_owner_menu, print_result


def _run_and_report(layer: str, owner: str | None, ai: AIRunner) -> None:
    label = f"{owner}/{layer}" if owner else layer
    with capture_run(label) as log_path:
        result = run_target(layer, owner, REPO_ROOT, ai)
        print_result(label, result)
    print(f"[{label}] Run transcript saved to {log_path.relative_to(REPO_ROOT)}")


def _owner_submenu(owner: str, ai: AIRunner) -> None:
    while True:
        print_layer_menu(owner, LAYERS)
        choice = input("Choose a layer: ").strip()

        if choice == "0":
            return

        if choice == str(len(LAYERS) + 1):
            for key, _label in LAYERS:
                _run_and_report(key, owner, ai)
            continue

        try:
            layer_key, _label = LAYERS[int(choice) - 1]
        except (ValueError, IndexError):
            print(f"Invalid choice. Select 0-{len(LAYERS) + 1}.")
            continue

        _run_and_report(layer_key, owner, ai)


def _run_all(ai: AIRunner) -> None:
    for owner in OWNERS:
        for key, _label in LAYERS:
            _run_and_report(key, owner, ai)
    _run_and_report("compose", None, ai)
    _run_and_report("mcp", None, ai)
    _run_and_report("rag", None, ai)


def main() -> None:
    parser = argparse.ArgumentParser(description="Shared Team Agentic AI Loop")
    parser.add_argument("--target", "--owner", dest="target", choices=OWNERS, help="Target student or microservice")
    parser.add_argument("--mode", "--layer", dest="mode", choices=["frontend", "backend", "database", "compose", "mcp", "rag", "all"], help="Layer or mode to evaluate")
    args = parser.parse_args()

    ai = AIRunner(repo_root=REPO_ROOT)

    # CLI execution mode
    if args.mode or args.target:
        mode = args.mode or "all"
        target = args.target

        if mode == "all":
            if target:
                for key, _ in LAYERS:
                    _run_and_report(key, target, ai)
                _run_and_report("mcp", target, ai)
                _run_and_report("rag", target, ai)
            else:
                _run_all(ai)
            return

        if mode in {"compose", "mcp", "rag"}:
            _run_and_report(mode, target, ai)
            return

        if not target:
            print("Error: --target is required when specifying layer modes (frontend, backend, database).")
            sys.exit(1)

        _run_and_report(mode, target, ai)
        return

    # Interactive menu mode
    print("======================================================================")
    print("SHARED TEAM AGENTIC AI LOOP: Plan -> Act -> Observe -> Adapt")
    print("Authoritative Codebase Documentation & Multi-Agent Architecture Review")
    print("======================================================================")

    while True:
        print_owner_menu(OWNERS)
        choice = input("Choose a target: ").strip()

        if choice == "0":
            print("Loop closed.")
            break

        if choice == str(len(OWNERS) + 1):
            _run_and_report("compose", None, ai)
            continue

        if choice == str(len(OWNERS) + 2):
            _run_and_report("mcp", None, ai)
            continue

        if choice == str(len(OWNERS) + 3):
            _run_and_report("rag", None, ai)
            continue

        if choice == str(len(OWNERS) + 4):
            _run_all(ai)
            continue

        try:
            owner = OWNERS[int(choice) - 1]
        except (ValueError, IndexError):
            print(f"Invalid choice. Select 0-{len(OWNERS) + 4}.")
            continue

        _owner_submenu(owner, ai)


if __name__ == "__main__":
    main()
