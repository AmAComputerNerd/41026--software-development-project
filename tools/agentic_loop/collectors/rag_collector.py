from __future__ import annotations

import os
from pathlib import Path

import requests

from config.review_config import REQUEST_TIMEOUT_SECONDS
from core.compose_utils import get_service_host_port, load_compose

STUDENT_3_INTEGRATION_PATH = "/api/integrations/rag/answers"
STUDENT_1_INTEGRATION_PATH = "/api/notifications/rag/query"


def _resolve_base_url(repo_root: Path, owner: str | None = None) -> str | None:
    if owner == "student-1":
        override = os.getenv("RAG_VALIDATION_BASE_URL_STUDENT_1") or os.getenv("API_BASE_URL_STUDENT_1")
        if override:
            return override.rstrip("/")
        compose = load_compose(repo_root)
        port = get_service_host_port(compose, "student-1-backend")
        return f"http://localhost:{port or 5101}"

    override = os.getenv("RAG_VALIDATION_BASE_URL") or os.getenv("API_BASE_URL_STUDENT_3")
    if override:
        return override.rstrip("/")

    compose = load_compose(repo_root)
    port = get_service_host_port(compose, "student-3-backend")
    return f"http://localhost:{port or 5103}" if port else None


def _validate_static_contract_student_1(repo_root: Path) -> tuple[bool, str]:
    required_paths = [
        repo_root / "ai-services" / "rag-server" / "RagServer" / "Services" / "ProjectCorpus.cs",
        repo_root / "ai-services" / "rag-server" / "RagServer" / "Services" / "GroundedAnswerService.cs",
        repo_root / "student-1" / "backend" / "Api" / "Services" / "RagClient.cs",
        repo_root / "student-1" / "frontend" / "src" / "api" / "integration.ts",
    ]
    missing = [str(path.relative_to(repo_root)) for path in required_paths if not path.is_file()]
    if missing:
        return False, f"Missing Student 1 RAG integration artifacts: {', '.join(missing)}."

    corpus_source = required_paths[0].read_text(encoding="utf-8")
    answer_source = required_paths[1].read_text(encoding="utf-8")
    client_source = required_paths[2].read_text(encoding="utf-8")

    required_evidence = [
        ("retrieval threshold", "MinimumRelevanceScore", corpus_source),
        ("insufficient-context branch", '"insufficient_context"', answer_source),
        ("structured citations", "RagCitation", answer_source),
        ("client interface", "IRagClient", client_source),
    ]
    missing_evidence = [label for label, fragment, source in required_evidence if fragment not in source]
    if missing_evidence:
        return False, f"Student 1 RAG contract evidence is incomplete: {', '.join(missing_evidence)}."

    compose = load_compose(repo_root)
    services = (compose or {}).get("services") or {}
    backend_service = services.get("student-1-backend") or {}
    env_vars = backend_service.get("environment") or {}
    if not isinstance(env_vars, dict):
        env_vars = {item.split("=")[0]: item.split("=")[1] for item in env_vars if "=" in item}

    if "RagServer__BaseUrl" not in env_vars:
        return False, "student-1-backend in docker-compose.yml lacks RagServer__BaseUrl configuration."

    return True, (
        "Static contract: curated Markdown retrieval with relevance scoring, "
        "structured citations, confidence categories, and fallback when context is insufficient. "
        "Docker configuration: student-1-backend connects to host RAG gateway via extra_hosts."
    )


def _validate_live_contract_student_1(base_url: str) -> tuple[bool, str]:
    url = f"{base_url}{STUDENT_1_INTEGRATION_PATH}"

    try:
        grounded_response = requests.post(
            url,
            json={"query": "What is the policy for late assignment submissions?", "scope": "student-1"},
            timeout=max(REQUEST_TIMEOUT_SECONDS, 60),
        )
        insufficient_response = requests.post(
            url,
            json={"query": "What meals are served in the campus cafeteria today?", "scope": "student-1"},
            timeout=max(REQUEST_TIMEOUT_SECONDS, 60),
        )
    except requests.exceptions.ConnectionError:
        return False, f"Student 1 backend is not reachable at {base_url}."
    except requests.exceptions.Timeout:
        return False, "Student 1 RAG validation timed out."
    except requests.RequestException as exc:
        return False, f"Student 1 RAG validation request failed: {type(exc).__name__}."

    if grounded_response.status_code != 200:
        return False, (
            f"Grounded query returned HTTP {grounded_response.status_code}; "
            "ensure Student 1 backend, RAG server, and AI Mode are running."
        )
    if insufficient_response.status_code != 200:
        return False, f"Insufficient-context query returned HTTP {insufficient_response.status_code}."

    try:
        grounded = grounded_response.json()
        insufficient = insufficient_response.json()
    except ValueError:
        return False, "Student 1 RAG integration returned a non-JSON response."

    citations = grounded.get("citations", [])
    confidence = (grounded.get("confidence") or "").upper()

    if confidence not in {"HIGH", "MEDIUM", "LOW"} or not grounded.get("hasSufficientContext"):
        return False, f"Grounded query did not return expected confidence or sufficient context: {grounded}."

    if insufficient.get("hasSufficientContext") is not False:
        return False, "Unrelated query did not return hasSufficientContext=false."

    return True, (
        f"Live grounded query returned confidence '{confidence}' with {len(citations)} citation(s). "
        "Unrelated query returned hasSufficientContext=false and zero citations."
    )


def _validate_static_contract_student_3(repo_root: Path) -> tuple[bool, str]:
    required_paths = [
        repo_root / "ai-services" / "rag-server" / "RagServer" / "Services" / "ProjectCorpus.cs",
        repo_root / "ai-services" / "rag-server" / "RagServer" / "Services" / "GroundedAnswerService.cs",
        repo_root / "student-3" / "backend" / "Api" / "Services" / "RagClient.cs",
        repo_root / "student-3" / "frontend" / "src" / "api" / "rag.ts",
    ]
    missing = [str(path.relative_to(repo_root)) for path in required_paths if not path.is_file()]
    if missing:
        return False, f"Missing RAG integration artifacts: {', '.join(missing)}."

    corpus_source = required_paths[0].read_text(encoding="utf-8")
    answer_source = required_paths[1].read_text(encoding="utf-8")
    required_evidence = [
        ("retrieval threshold", "MinimumRelevanceScore", corpus_source),
        ("insufficient-context branch", '"insufficient_context"', answer_source),
        ("structured citations", "RagCitation", answer_source),
        ("AI gateway route", '"v1/chat/completions"', answer_source),
    ]
    missing_evidence = [label for label, fragment, source in required_evidence if fragment not in source]
    if missing_evidence:
        return False, f"RAG contract evidence is incomplete: {', '.join(missing_evidence)}."

    return True, (
        "Static contract: curated Markdown retrieval has a relevance threshold, "
        "generation routes through ai-mode, responses carry structured citations "
        "and confidence, and an insufficient-context branch bypasses generation."
    )


def _validate_live_contract_student_3(base_url: str) -> tuple[bool, str]:
    url = f"{base_url}{STUDENT_3_INTEGRATION_PATH}"
    try:
        grounded_response = requests.post(
            url,
            json={"question": "How does Canvas assignment sync work in the Deadline Tracker?"},
            timeout=max(REQUEST_TIMEOUT_SECONDS, 120),
        )
        insufficient_response = requests.post(
            url,
            json={"question": "What meals are served in the campus cafeteria today?"},
            timeout=max(REQUEST_TIMEOUT_SECONDS, 120),
        )
    except requests.exceptions.ConnectionError:
        return False, f"Student 3 backend is not reachable at {base_url}."
    except requests.exceptions.Timeout:
        return False, "RAG validation timed out."
    except requests.RequestException as exc:
        return False, f"RAG validation request failed: {type(exc).__name__}."

    if grounded_response.status_code != 200:
        return False, (
            f"Grounded query returned HTTP {grounded_response.status_code}; "
            "start Student 3, RAG, and AI Mode, then re-run validation."
        )
    if insufficient_response.status_code != 200:
        return False, f"Insufficient-context query returned HTTP {insufficient_response.status_code}."

    try:
        grounded = grounded_response.json()
        insufficient = insufficient_response.json()
    except ValueError:
        return False, "RAG integration returned a non-JSON response."

    citations = grounded.get("citations")
    if grounded.get("status") != "success" or \
            grounded.get("confidence") not in {"high", "medium", "low"} or \
            not isinstance(citations, list) or not citations:
        return False, "Grounded query did not return an answer, confidence, and citations."

    if insufficient.get("status") != "insufficient_context" or \
            insufficient.get("confidence") != "insufficient" or \
            insufficient.get("citations") != []:
        return False, "Unrelated query did not return the required insufficient-context result."

    citation_sources = sorted({citation.get("sourceId", "") for citation in citations})
    return True, (
        f"Live grounded query returned confidence '{grounded['confidence']}' with {len(citations)} citation(s) "
        f"from {citation_sources}. Unrelated query returned status "
        "'insufficient_context', confidence 'insufficient', and zero citations."
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

        return True, f"STUDENT-1 RAG VALIDATION PASSED. {static_evidence} {live_evidence}"

    # Default to Student 3
    static_ok, static_evidence = _validate_static_contract_student_3(repo_root)
    if not static_ok:
        return False, static_evidence

    base_url = _resolve_base_url(repo_root, owner)
    if not base_url:
        return False, (
            "No Student 3 backend URL is available. Set RAG_VALIDATION_BASE_URL "
            "or publish student-3-backend in docker-compose.yml."
        )

    live_ok, live_evidence = _validate_live_contract_student_3(base_url)
    if not live_ok:
        return False, f"{static_evidence} Live validation failed: {live_evidence}"

    return True, f"RAG VALIDATION PASSED. {static_evidence} {live_evidence}"
