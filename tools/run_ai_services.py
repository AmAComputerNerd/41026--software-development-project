from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import threading
import time
from urllib.error import URLError
from urllib.request import urlopen


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
SERVICE_SPECS = (
    (
        "ai-mode",
        REPOSITORY_ROOT / "ai-services" / "ai-mode" / "Api" / "Api.csproj",
    ),
    (
        "mcp",
        REPOSITORY_ROOT / "ai-services" / "mcp-server" / "McpServer" / "McpServer.csproj",
    ),
    (
        "rag",
        REPOSITORY_ROOT / "ai-services" / "rag-server" / "RagServer" / "RagServer.csproj",
    ),
)
CORPUS_SOURCES = (
    Path("AGENTS.md"),
    Path("docs/architecture/data-flows.md"),
    Path("student-3/README.md"),
)


def load_root_environment() -> dict[str, str]:
    values: dict[str, str] = {}
    env_path = REPOSITORY_ROOT / ".env"
    if not env_path.exists():
        return values

    for raw_line in env_path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        values[key.strip()] = value.strip().strip("\"'")
    return values


def configured_port(name: str, root_environment: dict[str, str]) -> int:
    raw_value = os.environ.get(name, root_environment.get(name, "")).strip()
    if not raw_value:
        raise ValueError(
            f"{name} must be configured in the root .env. "
            "Run python tools/setup_env.py."
        )
    try:
        port = int(raw_value)
    except ValueError as exception:
        raise ValueError(f"{name} must be a valid port number.") from exception
    if not 1 <= port <= 65535:
        raise ValueError(f"{name} must be between 1 and 65535.")
    return port


def parse_args(root_environment: dict[str, str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run the non-containerised AI Mode, MCP, and RAG services."
    )
    parser.add_argument(
        "--mcp-port",
        type=int,
        default=configured_port("MCP_HOST_PORT", root_environment),
    )
    parser.add_argument(
        "--rag-port",
        type=int,
        default=configured_port("RAG_HOST_PORT", root_environment),
    )
    parser.add_argument(
        "--ai-mode-port",
        type=int,
        default=configured_port("AI_MODE_HOST_PORT", root_environment),
    )
    parser.add_argument(
        "--student-3-port",
        type=int,
        default=5103,
    )
    return parser.parse_args()


def prepare_corpus(destination: Path) -> None:
    for relative_path in CORPUS_SOURCES:
        source = REPOSITORY_ROOT / relative_path
        if not source.is_file():
            raise FileNotFoundError(f"RAG corpus source not found: {source}")
        target = destination / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)


def stream_output(name: str, process: subprocess.Popen[str]) -> None:
    assert process.stdout is not None
    for line in process.stdout:
        print(f"[{name}] {line}", end="", flush=True)


def start_service(
    name: str,
    project: Path,
    environment: dict[str, str],
) -> subprocess.Popen[str]:
    command = [
        "dotnet",
        "run",
        "--project",
        str(project),
        "--no-launch-profile",
    ]
    process = subprocess.Popen(
        command,
        cwd=REPOSITORY_ROOT,
        env=environment,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    threading.Thread(
        target=stream_output,
        args=(name, process),
        daemon=True,
    ).start()
    return process


def wait_until_ready(
    name: str,
    process: subprocess.Popen[str],
    readiness_url: str,
    timeout_seconds: int = 60,
) -> None:
    deadline = time.monotonic() + timeout_seconds
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError(f"{name} exited with code {process.returncode}.")
        try:
            with urlopen(readiness_url, timeout=2) as response:
                if response.status == 200:
                    print(f"[launcher] {name} ready at {readiness_url}")
                    return
        except URLError:
            pass
        time.sleep(1)
    raise TimeoutError(f"{name} did not become ready within {timeout_seconds} seconds.")


def report_dependency(name: str, url: str) -> None:
    try:
        with urlopen(url, timeout=2) as response:
            if response.status == 200:
                print(f"[launcher] dependency ready: {name} ({url})")
                return
    except URLError:
        pass
    print(
        f"[launcher] warning: {name} is unavailable at {url}; "
        "the host services will run, but related requests will fail until it is available.",
        file=sys.stderr,
    )


def stop_process(name: str, process: subprocess.Popen[str]) -> None:
    if process.poll() is not None:
        return
    try:
        process.terminate()
        process.wait(timeout=10)
    except (ProcessLookupError, subprocess.TimeoutExpired):
        process.kill()
        process.wait(timeout=5)
    print(f"[launcher] stopped {name}")


def main() -> int:
    root_environment = load_root_environment()
    args = parse_args(root_environment)
    if len({args.mcp_port, args.rag_port, args.ai_mode_port, args.student_3_port}) != 4:
        print("Configured service ports must be distinct.", file=sys.stderr)
        return 2

    processes: list[tuple[str, subprocess.Popen[str]]] = []
    with tempfile.TemporaryDirectory(prefix="better-canvas-rag-") as corpus_directory:
        try:
            prepare_corpus(Path(corpus_directory))
            base_environment = os.environ.copy()
            base_environment["ASPNETCORE_ENVIRONMENT"] = "Development"
            base_environment["DOTNET_NOLOGO"] = "true"
            base_environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true"

            openrouter_api_key = os.environ.get(
                "OPENROUTER_API_KEY",
                root_environment.get("OPENROUTER_API_KEY", ""),
            ).strip()
            if not openrouter_api_key:
                raise ValueError(
                    "OPENROUTER_API_KEY must be configured in the root .env or environment."
                )
            openrouter_model = os.environ.get(
                "OPENROUTER_MODEL",
                root_environment.get("OPENROUTER_MODEL", ""),
            ).strip()
            if not openrouter_model:
                raise ValueError(
                    "OPENROUTER_MODEL must be configured in the root .env. "
                    "Run python tools/setup_env.py."
                )

            environments = {
                "ai-mode": {
                    **base_environment,
                    "ASPNETCORE_URLS": f"http://127.0.0.1:{args.ai_mode_port}",
                    "OPENROUTER_API_KEY": openrouter_api_key,
                    "OPENROUTER_MODEL": openrouter_model,
                },
                "mcp": {
                    **base_environment,
                    "ASPNETCORE_URLS": f"http://127.0.0.1:{args.mcp_port}",
                    "Student3__BaseUrl": f"http://127.0.0.1:{args.student_3_port}",
                },
                "rag": {
                    **base_environment,
                    "ASPNETCORE_URLS": f"http://127.0.0.1:{args.rag_port}",
                    "AiGateway__BaseUrl": f"http://127.0.0.1:{args.ai_mode_port}",
                    "Rag__CorpusPath": corpus_directory,
                },
            }

            for name, project in SERVICE_SPECS:
                process = start_service(name, project, environments[name])
                processes.append((name, process))

            process_by_name = dict(processes)
            readiness_urls = {
                "AI Mode": (
                    process_by_name["ai-mode"],
                    f"http://127.0.0.1:{args.ai_mode_port}/health/ready",
                ),
                "MCP": (
                    process_by_name["mcp"],
                    f"http://127.0.0.1:{args.mcp_port}/health/ready",
                ),
                "RAG": (
                    process_by_name["rag"],
                    f"http://127.0.0.1:{args.rag_port}/health/ready",
                ),
            }
            for name, (process, readiness_url) in readiness_urls.items():
                wait_until_ready(name, process, readiness_url)

            report_dependency(
                "Student 3 backend",
                f"http://127.0.0.1:{args.student_3_port}/health/ready",
            )
            print("[launcher] AI services are running. Press Ctrl+C to stop.")

            while all(process.poll() is None for _, process in processes):
                time.sleep(1)
            failed = next((item for item in processes if item[1].poll() is not None), None)
            if failed is not None:
                print(
                    f"[launcher] {failed[0]} exited unexpectedly with code "
                    f"{failed[1].returncode}.",
                    file=sys.stderr,
                )
                return 1
        except KeyboardInterrupt:
            print("\n[launcher] stopping host services...")
        except (FileNotFoundError, RuntimeError, TimeoutError, ValueError) as exception:
            print(f"[launcher] startup failed: {exception}", file=sys.stderr)
            return 1
        finally:
            for name, process in reversed(processes):
                stop_process(name, process)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
