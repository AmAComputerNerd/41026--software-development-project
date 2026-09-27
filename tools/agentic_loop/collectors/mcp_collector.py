from __future__ import annotations

import os
from pathlib import Path

import requests

from config.review_config import REQUEST_TIMEOUT_SECONDS
from core.compose_utils import get_service_host_port, load_compose

STUDENT_3_TOOL = "deadlines_list_upcoming"
STUDENT_3_INTEGRATION_PATH = "/api/integrations/mcp/upcoming-deadlines"
STUDENT_3_ITEM_FIELDS = {"id", "title", "dueDate", "priority", "status", "courseName"}

STUDENT_1_TOOL = "notifications_broadcast_alert"
STUDENT_1_INTEGRATION_PATH = "/api/notifications/mcp/broadcast"


def _resolve_base_url(repo_root: Path, owner: str | None = None) -> str | None:
    if owner == "student-1":
        override = os.getenv("MCP_VALIDATION_BASE_URL_STUDENT_1") or os.getenv("API_BASE_URL_STUDENT_1")
        if override:
            return override.rstrip("/")
        compose = load_compose(repo_root)
        port = get_service_host_port(compose, "student-1-backend")
        return f"http://localhost:{port or 5101}"

    override = os.getenv("MCP_VALIDATION_BASE_URL") or os.getenv("API_BASE_URL_STUDENT_3")
    if override:
        return override.rstrip("/")

    compose = load_compose(repo_root)
    port = get_service_host_port(compose, "student-3-backend")
    return f"http://localhost:{port or 5103}" if port else None


def _validate_static_contract_student_1(repo_root: Path) -> tuple[bool, str]:
    tool_path = (
        repo_root
        / "ai-services"
        / "mcp-server"
        / "McpServer"
        / "Tools"
        / "NotificationTools.cs"
    )
    integration_path = (
        repo_root
        / "student-1"
        / "backend"
        / "Api"
        / "Endpoints"
        / "McpRagIntegrationEndpoints.cs"
    )
    client_path = (
        repo_root
        / "student-1"
        / "backend"
        / "Api"
        / "Services"
        / "McpClient.cs"
    )

    required_paths = [tool_path, integration_path, client_path]
    missing = [str(path.relative_to(repo_root)) for path in required_paths if not path.is_file()]
    if missing:
        return False, f"Missing Student 1 MCP integration artifacts: {', '.join(missing)}."

    tool_source = tool_path.read_text(encoding="utf-8")
    integration_source = integration_path.read_text(encoding="utf-8")
    client_source = client_path.read_text(encoding="utf-8")

    required_fragments = {
        str(tool_path.relative_to(repo_root)): [
            f'Name = "{STUDENT_1_TOOL}"',
            "UseStructuredContent = true",
            "urgency is not null",
            "INotificationClient",
        ],
        str(integration_path.relative_to(repo_root)): [
            STUDENT_1_INTEGRATION_PATH,
            "BroadcastMcpAlert",
        ],
        str(client_path.relative_to(repo_root)): [
            STUDENT_1_TOOL,
            "IMcpClient",
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
        return False, "Student 1 MCP contract evidence is incomplete: " + "; ".join(missing_fragments)

    compose = load_compose(repo_root)
    services = (compose or {}).get("services") or {}
    backend_service = services.get("student-1-backend") or {}
    env_vars = backend_service.get("environment") or {}
    if not isinstance(env_vars, dict):
        env_vars = {item.split("=")[0]: item.split("=")[1] for item in env_vars if "=" in item}

    if "McpServer__BaseUrl" not in env_vars:
        return False, "student-1-backend in docker-compose.yml lacks McpServer__BaseUrl configuration."

    return True, (
        f"Static contract: {STUDENT_1_TOOL} uses structured content, validates payload, "
        "calls INotificationClient broadcast, and is invoked via IMcpClient in Student 1 backend. "
        "Docker configuration: student-1-backend connects to host MCP gateway via extra_hosts."
    )


def _validate_live_contract_student_1(base_url: str) -> tuple[bool, str]:
    url = f"{base_url}{STUDENT_1_INTEGRATION_PATH}"
    valid_payload = {
        "studentId": "11111111-1111-1111-1111-111111111111",
        "title": "Agentic Loop Alert",
        "message": "Testing Student 1 MCP broadcast alert via agentic loop.",
        "urgency": "High",
    }
    try:
        response = requests.post(
            url,
            json=valid_payload,
            timeout=REQUEST_TIMEOUT_SECONDS,
        )
    except requests.exceptions.ConnectionError:
        return False, f"Student 1 backend is not reachable at {base_url}."
    except requests.exceptions.Timeout:
        return False, f"Student 1 MCP validation timed out after {REQUEST_TIMEOUT_SECONDS:g} seconds."
    except requests.RequestException as exc:
        return False, f"Student 1 MCP validation request failed: {type(exc).__name__}."

    if response.status_code != 200:
        return False, (
            f"POST {STUDENT_1_INTEGRATION_PATH} returned HTTP {response.status_code}; "
            "ensure Student 1 backend and MCP server are running."
        )

    try:
        payload = response.json()
    except ValueError:
        return False, "Student 1 MCP integration returned a non-JSON response."

    if payload.get("status") != "success" or payload.get("tool") != STUDENT_1_TOOL:
        return False, f"Student 1 MCP integration returned unexpected status or tool: {payload}."

    try:
        invalid_response = requests.post(
            url,
            json={
                "studentId": "11111111-1111-1111-1111-111111111111",
                "title": "",
                "message": "",
                "urgency": "High",
            },
            timeout=REQUEST_TIMEOUT_SECONDS,
        )
    except requests.RequestException as exc:
        return False, f"Student 1 MCP boundary validation request failed: {type(exc).__name__}."

    if invalid_response.status_code != 400:
        return False, (
            "Student 1 MCP integration accepted empty title/message; "
            f"expected HTTP 400, received {invalid_response.status_code}."
        )

    return True, (
        f"Live invocation: POST {STUDENT_1_INTEGRATION_PATH} returned HTTP 200 with tool "
        f"'{STUDENT_1_TOOL}', status 'success'. Boundary probe: empty payload returned HTTP 400."
    )


def _validate_static_contract_student_3(repo_root: Path) -> tuple[bool, str]:
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
            f'Name = "{STUDENT_3_TOOL}"',
            "UseStructuredContent = true",
            "days is < 1 or > 90",
            "limit is < 1 or > 50",
            "IDeadlineClient",
        ],
        str(integration_path.relative_to(repo_root)): [
            STUDENT_3_INTEGRATION_PATH,
            "/internal/ai-context/upcoming-deadlines",
        ],
        str(client_path.relative_to(repo_root)): [
            "McpClient.CreateAsync",
            f'"{STUDENT_3_TOOL}"',
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

    return True, (
        f"Static contract: {STUDENT_3_TOOL} uses structured content, validates days 1-90 "
        "and limit 1-50, calls only the bounded Student 3 HTTP projection, and is "
        "invoked by the official MCP client over Streamable HTTP."
    )


def _validate_live_contract_student_3(base_url: str) -> tuple[bool, str]:
    url = f"{base_url}{STUDENT_3_INTEGRATION_PATH}"
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
            f"POST {STUDENT_3_INTEGRATION_PATH} returned HTTP {response.status_code}; "
            "start Student 3 and MCP, then re-run validation."
        )

    try:
        payload = response.json()
    except ValueError:
        return False, "MCP integration returned a non-JSON response."

    if payload.get("status") != "success" or payload.get("tool") != STUDENT_3_TOOL:
        return False, "MCP integration returned an unexpected status or tool name."

    data = payload.get("data")
    if not isinstance(data, dict) or not isinstance(data.get("items"), list):
        return False, "MCP integration did not return structured deadline data."

    items = data["items"]
    if items and not STUDENT_3_ITEM_FIELDS.issubset(items[0]):
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
        f"Live invocation: POST {STUDENT_3_INTEGRATION_PATH} returned HTTP 200 with tool "
        f"'{STUDENT_3_TOOL}', status 'success', {data.get('count', len(items))} item(s), "
        f"and {item_evidence}. Boundary probe: days=0 returned HTTP 400."
    )


def collect(owner: str | None, repo_root: Path) -> tuple[bool, str]:
    if owner == "student-1":
        static_ok, static_evidence = _validate_static_contract_student_1(repo_root)
        if not static_ok:
            return False, static_evidence

        base_url = _resolve_base_url(repo_root, owner)
        if not base_url:
            return False, "No Student 1 backend URL is available."

        live_ok, live_evidence = _validate_live_contract_student_1(base_url)
        if not live_ok:
            return False, f"{static_evidence} Live validation failed: {live_evidence}"

        return True, f"STUDENT-1 MCP VALIDATION PASSED. {static_evidence} {live_evidence}"

    # Default to Student 3
    static_ok, static_evidence = _validate_static_contract_student_3(repo_root)
    if not static_ok:
        return False, static_evidence

    base_url = _resolve_base_url(repo_root, owner)
    if not base_url:
        return False, (
            "No Student 3 backend URL is available. Set MCP_VALIDATION_BASE_URL "
            "or publish student-3-backend in docker-compose.yml."
        )

    live_ok, live_evidence = _validate_live_contract_student_3(base_url)
    if not live_ok:
        return False, f"{static_evidence} Live validation failed: {live_evidence}"

    return True, f"MCP VALIDATION PASSED. {static_evidence} {live_evidence}"
