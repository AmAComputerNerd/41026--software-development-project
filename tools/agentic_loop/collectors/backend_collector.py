"""
Generalized .NET minimal-API discovery: works for any owner whose backend
follows the `endpoints.MapGroup("...")` + `group.MapGet/MapPost/MapPut/
MapDelete("...")` convention.
"""

import os
import re
from pathlib import Path

import requests

from config.review_config import REQUEST_TIMEOUT_SECONDS, owner_path
from core.compose_utils import get_service_host_port, load_compose

GROUP_PATTERN = re.compile(r'\.MapGroup\("([^"]+)"\)')
ROUTE_PATTERN = re.compile(r'\.Map(Get|Post|Put|Delete)\("([^"]*)"')


def _discover_routes(backend_dir: Path) -> list[tuple[str, str]]:
    routes: list[tuple[str, str]] = []
    endpoint_files = [p for p in backend_dir.rglob("*.cs") if p.parent.name == "Endpoints"]

    for endpoint_file in endpoint_files:
        content = endpoint_file.read_text(encoding="utf-8")
        group_match = GROUP_PATTERN.search(content)
        prefix = group_match.group(1) if group_match else ""
        for method, route in ROUTE_PATTERN.findall(content):
            full_path = prefix.rstrip("/") + route if route != "/" else prefix
            routes.append((method.upper(), full_path))

    return routes


def _resolve_base_url(owner: str, repo_root: Path) -> str | None:
    env_key = f"API_BASE_URL_{owner.replace('-', '_').upper()}"
    override = os.getenv(env_key)
    if override:
        return override

    compose = load_compose(repo_root)
    port = get_service_host_port(compose, f"{owner}-backend")
    if port:
        return f"http://localhost:{port}"

    return None


def _extract_first_id(payload) -> str | None:
    if not isinstance(payload, list) or not payload:
        return None
    first = payload[0]
    if not isinstance(first, dict):
        return None
    for key in first:
        if key.lower() == "id":
            return str(first[key])
    return None


def collect(owner: str, repo_root: Path) -> tuple[bool, str]:
    backend_dir = owner_path(owner, repo_root) / "backend"
    if not backend_dir.exists():
        return False, f"No '{owner}/backend' directory found."

    routes = _discover_routes(backend_dir)
    if not routes:
        return False, (
            f"No 'endpoints.MapGroup(...)' + 'group.MapGet/MapPost/MapPut/"
            f"MapDelete(...)' routes found under '{owner}/backend'. Backend "
            "review is only implemented for that convention today - add a "
            "collector for this stack if it differs."
        )

    base_url = _resolve_base_url(owner, repo_root)
    if not base_url:
        env_key = f"API_BASE_URL_{owner.replace('-', '_').upper()}"
        return False, (
            f"Discovered routes {routes} but no reachable base URL. Set "
            f"'{env_key}' in .env, or add a published port for "
            f"'{owner}-backend' in docker-compose.yml."
        )

    evidence_lines = [f"Statically discovered routes: {routes}."]
    connection_failures = 0
    total_checks = 0
    list_results: dict[str, list] = {}

    for method, path in routes:
        if method != "GET":
            evidence_lines.append(f"{method} {path} discovered, not probed (state-changing).")
            continue

        target_path = path
        if "{" in path:
            list_prefix = path.split("/{")[0]
            first_id = _extract_first_id(list_results.get(list_prefix))
            if not first_id:
                evidence_lines.append(
                    f"GET {path} discovered, not probed (needs a real id and "
                    "none was available from a sibling list endpoint)."
                )
                continue
            target_path = re.sub(r"\{[^}]+\}", first_id, path, count=1)

        total_checks += 1
        url = f"{base_url}{target_path}"
        try:
            response = requests.get(url, timeout=REQUEST_TIMEOUT_SECONDS)
            elapsed_ms = int(response.elapsed.total_seconds() * 1000)
            evidence_lines.append(f"GET {target_path} returned {response.status_code} in {elapsed_ms}ms")
            if "{" not in path and response.status_code == 200:
                try:
                    list_results[path] = response.json()
                except ValueError:
                    pass
        except requests.exceptions.ConnectionError:
            connection_failures += 1
            evidence_lines.append(f"GET {target_path} [CONNECTION REFUSED - {owner} backend not running at {base_url}]")
        except requests.exceptions.Timeout:
            evidence_lines.append(f"GET {target_path} [TIMEOUT]")
        except Exception as exc:  # noqa: BLE001 - surfaced as evidence, not raised
            evidence_lines.append(f"GET {target_path} [ERROR: {type(exc).__name__}]")

    evidence = "Live endpoint evidence: " + "; ".join(evidence_lines) + "."

    if total_checks > 0 and connection_failures == total_checks:
        return False, (
            f"'{owner}' backend not reachable at {base_url}. Start it, or "
            "correct the base URL, then re-run this review."
        )

    return True, evidence
