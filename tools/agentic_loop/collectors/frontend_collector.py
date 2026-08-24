"""
Vue 3 + Vuetify frontend discovery: works for any owner whose frontend uses
`vue` + `vuetify` as declared package.json dependencies. Falls back to the
generic "not implemented for this stack" stub for any other frontend, or
one with no frontend at all - nothing here is hardcoded to a single owner.
"""

import json
import os
import re
from pathlib import Path

import requests

from config.review_config import REQUEST_TIMEOUT_SECONDS, owner_path
from core.compose_utils import get_service_host_port, load_compose

_IGNORED_DIR_PARTS = {"node_modules", "dist", ".git"}

# vue-router route declarations, e.g. `path: '/tasks/:id'`
ROUTE_PATTERN = re.compile(r"path:\s*['\"]([^'\"]+)['\"]")

# Any Vuetify component tag used in a template, e.g. <v-btn ...>, <v-data-table>
VUETIFY_TAG_PATTERN = re.compile(r"<(v-[a-z][a-z0-9-]*)")


def _is_vue_vuetify_project(package_json_path: Path) -> bool:
    try:
        data = json.loads(package_json_path.read_text(encoding="utf-8"))
    except (ValueError, OSError):
        return False
    deps = {**(data.get("dependencies") or {}), **(data.get("devDependencies") or {})}
    return "vue" in deps and "vuetify" in deps


def _iter_source_files(frontend_dir: Path, suffix: str) -> list[Path]:
    return [
        p for p in frontend_dir.rglob(f"*{suffix}")
        if not _IGNORED_DIR_PARTS.intersection(p.parts)
    ]


def _discover_components(frontend_dir: Path) -> list[str]:
    src_root = frontend_dir / "src"
    search_root = src_root if src_root.exists() else frontend_dir
    return sorted(
        str(p.relative_to(frontend_dir)) for p in _iter_source_files(search_root, ".vue")
    )


def _discover_routes(frontend_dir: Path) -> list[str]:
    routes: set[str] = set()
    for js_file in _iter_source_files(frontend_dir, ".js") + _iter_source_files(frontend_dir, ".ts"):
        if "router" not in js_file.parts and "router" not in js_file.stem:
            continue
        content = js_file.read_text(encoding="utf-8")
        routes.update(ROUTE_PATTERN.findall(content))
    return sorted(routes)


def _discover_vuetify_usage(frontend_dir: Path, component_paths: list[str]) -> dict[str, int]:
    usage: dict[str, int] = {}
    for rel_path in component_paths:
        content = (frontend_dir / rel_path).read_text(encoding="utf-8")
        for tag in VUETIFY_TAG_PATTERN.findall(content):
            usage[tag] = usage.get(tag, 0) + 1
    return usage


def _resolve_base_url(owner: str, repo_root: Path) -> str | None:
    env_key = f"FRONTEND_BASE_URL_{owner.replace('-', '_').upper()}"
    override = os.getenv(env_key)
    if override:
        return override

    compose = load_compose(repo_root)
    port = get_service_host_port(compose, f"{owner}-frontend")
    if port:
        return f"http://localhost:{port}"

    return None


def _probe_dev_server(owner: str, repo_root: Path) -> str:
    base_url = _resolve_base_url(owner, repo_root)
    if not base_url:
        env_key = f"FRONTEND_BASE_URL_{owner.replace('-', '_').upper()}"
        return (
            f"No reachable dev/preview server configured (set '{env_key}' "
            f"in .env, or add a published port for '{owner}-frontend' in "
            "docker-compose.yml) - static evidence only."
        )

    try:
        response = requests.get(base_url, timeout=REQUEST_TIMEOUT_SECONDS)
        elapsed_ms = int(response.elapsed.total_seconds() * 1000)
        return f"GET {base_url} returned {response.status_code} in {elapsed_ms}ms."
    except requests.exceptions.ConnectionError:
        return f"[CONNECTION REFUSED - {owner} frontend not running at {base_url}]"
    except requests.exceptions.Timeout:
        return f"GET {base_url} [TIMEOUT]"
    except Exception as exc:  # noqa: BLE001 - surfaced as evidence, not raised
        return f"GET {base_url} [ERROR: {type(exc).__name__}]"


def collect(owner: str, repo_root: Path) -> tuple[bool, str]:
    frontend_dir = owner_path(owner, repo_root) / "frontend"

    if not frontend_dir.exists():
        return False, f"No '{owner}/frontend' directory found."

    package_json_path = frontend_dir / "package.json"
    if not package_json_path.exists() or not _is_vue_vuetify_project(package_json_path):
        return False, (
            f"'{owner}/frontend' does not declare both 'vue' and 'vuetify' "
            "in package.json. Frontend review is only implemented for that "
            "stack today - add a collector for this stack if it differs."
        )

    components = _discover_components(frontend_dir)
    if not components:
        return False, (
            f"'{owner}/frontend' declares vue + vuetify but no '.vue' "
            "component files were found under 'src/' yet. Re-run once "
            "there are real components to evaluate."
        )

    routes = _discover_routes(frontend_dir)
    vuetify_usage = _discover_vuetify_usage(frontend_dir, components)
    dev_server_status = _probe_dev_server(owner, repo_root)

    evidence = (
        f"Discovered {len(components)} component(s): {components}. "
        f"Statically discovered vue-router route(s): {routes or 'none found'}. "
        f"Vuetify component tags in use: {vuetify_usage or 'none found'}. "
        f"Dev server check: {dev_server_status}"
    )
    return True, evidence
