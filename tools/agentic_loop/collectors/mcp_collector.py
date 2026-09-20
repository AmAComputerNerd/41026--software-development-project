from __future__ import annotations

import os
from pathlib import Path

import requests

from config.review_config import REQUEST_TIMEOUT_SECONDS
from core.compose_utils import get_service_host_port, load_compose

TOOL_NAME = "deadlines_list_upcoming"
INTEGRATION_PATH = "/api/integrations/mcp/upcoming-deadlines"
EXPECTED_ITEM_FIELDS = {"id", "title", "dueDate", "priority", "status", "courseName"}


def _resolve_base_url(repo_root: Path) -> str | None:
    override = os.getenv("MCP_VALIDATION_BASE_URL") or os.getenv("API_BASE_URL_STUDENT_3")
    if override:
        return override.rstrip("/")

    compose = load_compose(repo_root)
    port = get_service_host_port(compose, "student-3-backend")
    return f"http://localhost:{port}" if port else None


def _validate_static_contract(repo_root: Path) -> tuple[bool, str]:
    tool_path = (
        repo_root
        / "ai-services"
        / "mcp-server"
        / "McpServer"
        / "Tools"
        / "DeadlineTools.cs"
    )
    integration_path = (
        repo_root
        / "student-3"
        / "backend"
        / "Api"
        / "Endpoints"
        / "McpIntegrationEndpoints.cs"
    )
    client_path = (
        repo_root
        / "student-3"
        / "backend"
        / "Api"
        / "Services"
        / "McpDeadlineClient.cs"
    )

    required_paths = [tool_path, integration_path, client_path]
    missing = [str(path.relative_to(repo_root)) for path in required_paths if not path.is_file()]
    if missing:
        return False, f"Missing MCP integration artifacts: {', '.join(missing)}."

    tool_source = tool_path.read_text(encoding="utf-8")
    integration_source = integration_path.read_text(encoding="utf-8")
    client_source = client_path.read_text(encoding="utf-8")

    required_fragments = {
        str(tool_path.relative_to(repo_root)): [
            f'Name = "{TOOL_NAME}"',
            "UseStructuredContent = true",
            "days is < 1 or > 90",
            "limit is < 1 or > 50",
            "IDeadlineClient",
        ],
        str(integration_path.relative_to(repo_root)): [
            INTEGRATION_PATH,
            "/internal/ai-context/upcoming-deadlines",
        ],
        str(client_path.relative_to(repo_root)): [
            "McpClient.CreateAsync",
            f'"{TOOL_NAME}"',
            "HttpTransportMode.StreamableHttp",
        ],
    }
    source_by_path = {
        str(tool_path.relative_to(repo_root)): tool_source,
        str(integration_path.relative_to(repo_root)): integration_source,
        str(client_path.relative_to(repo_root)): client_source,
    }

    missing_fragments = [
        f"{relative_path}: {fragment}"
        for relative_path, fragments in required_fragments.items()
        for fragment in fragments
        if fragment not in source_by_path[relative_path]
    ]
    if missing_fragments:
        return False, "MCP contract evidence is incomplete: " + "; ".join(missing_fragments)

    compose = load_compose(repo_root)
    services = (compose or {}).get("services") or {}
    mcp_service = services.get("mcp-server") or {}
    backend_service = services.get("student-3-backend") or {}
    mcp_networks = set(mcp_service.get("networks") or [])
    backend_networks = set(backend_service.get("networks") or [])
    if "ai" not in mcp_networks or "ai" not in backend_networks:
        return False, "MCP and Student 3 backend are not both attached to the ai network."
    if mcp_service.get("ports"):
        return False, "MCP server unexpectedly publishes a host port."

    return True, (
        f"Static contract: {TOOL_NAME} uses structured content, validates days 1-90 "
        "and limit 1-50, calls only the bounded Student 3 HTTP projection, and is "
        "invoked by the official MCP client over Streamable HTTP. Docker boundary: "
        "MCP and Student 3 share the ai network; MCP publishes no host port."
    )


def _validate_live_contract(base_url: str) -> tuple[bool, str]:
    url = f"{base_url}{INTEGRATION_PATH}"
    try:
        response = requests.post(
            url,
            json={"days": 7, "limit": 10},
            timeout=REQUEST_TIMEOUT_SECONDS,
        )
    except requests.exceptions.ConnectionError:
        return False, f"Student 3 backend is not reachable at {base_url}."
    except requests.exceptions.Timeout:
        return False, f"MCP validation timed out after {REQUEST_TIMEOUT_SECONDS:g} seconds."
    except requests.RequestException as exc:
        return False, f"MCP validation request failed: {type(exc).__name__}."

    if response.status_code != 200:
        return False, (
            f"POST {INTEGRATION_PATH} returned HTTP {response.status_code}; "
            "start Student 3 and MCP, then re-run validation."
        )

    try:
        payload = response.json()
    except ValueError:
        return False, "MCP integration returned a non-JSON response."

    if payload.get("status") != "success" or payload.get("tool") != TOOL_NAME:
        return False, "MCP integration returned an unexpected status or tool name."

    data = payload.get("data")
    if not isinstance(data, dict) or not isinstance(data.get("items"), list):
        return False, "MCP integration did not return structured deadline data."

    items = data["items"]
    if items and not EXPECTED_ITEM_FIELDS.issubset(items[0]):
        return False, "MCP deadline items do not contain the expected safe projection."

    try:
        invalid_response = requests.post(
            url,
            json={"days": 0, "limit": 10},
            timeout=REQUEST_TIMEOUT_SECONDS,
        )
    except requests.RequestException as exc:
        return False, f"MCP boundary validation failed: {type(exc).__name__}."

    if invalid_response.status_code != 400:
        return False, (
            "MCP integration accepted an invalid days value; "
            f"expected HTTP 400, received {invalid_response.status_code}."
        )

    item_evidence = (
        f"observed item fields {sorted(items[0].keys())}"
        if items
        else "an empty items list (item fields were not observable)"
    )
    return True, (
        f"Live invocation: POST {INTEGRATION_PATH} returned HTTP 200 with tool "
        f"'{TOOL_NAME}', status 'success', {data.get('count', len(items))} item(s), "
        f"and {item_evidence}. Boundary probe: days=0 returned HTTP 400."
    )


def collect(_owner: str | None, repo_root: Path) -> tuple[bool, str]:
    static_ok, static_evidence = _validate_static_contract(repo_root)
    if not static_ok:
        return False, static_evidence

    base_url = _resolve_base_url(repo_root)
    if not base_url:
        return False, (
            "No Student 3 backend URL is available. Set MCP_VALIDATION_BASE_URL "
            "or publish student-3-backend in docker-compose.yml."
        )

    live_ok, live_evidence = _validate_live_contract(base_url)
    if not live_ok:
        return False, f"{static_evidence} Live validation failed: {live_evidence}"

    return True, f"MCP VALIDATION PASSED. {static_evidence} {live_evidence}"
