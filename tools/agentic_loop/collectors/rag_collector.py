from __future__ import annotations

import os
from pathlib import Path

import requests

from config.review_config import REQUEST_TIMEOUT_SECONDS
from core.compose_utils import get_service_host_port, load_compose

INTEGRATION_PATH = "/api/integrations/rag/answers"


def _resolve_base_url(repo_root: Path) -> str | None:
    override = os.getenv("RAG_VALIDATION_BASE_URL") or os.getenv("API_BASE_URL_STUDENT_3")
    if override:
        return override.rstrip("/")

    compose = load_compose(repo_root)
    port = get_service_host_port(compose, "student-3-backend")
    return f"http://localhost:{port}" if port else None


def _validate_static_contract(repo_root: Path) -> tuple[bool, str]:
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

    compose = load_compose(repo_root)
    services = (compose or {}).get("services") or {}
    rag_service = services.get("rag-server") or {}
    backend_service = services.get("student-3-backend") or {}
    if "ai" not in set(rag_service.get("networks") or []) or \
            "ai" not in set(backend_service.get("networks") or []):
        return False, "RAG and Student 3 backend are not both attached to the ai network."
    if rag_service.get("ports"):
        return False, "RAG server unexpectedly publishes a host port."

    return True, (
        "Static contract: curated Markdown retrieval has a relevance threshold, "
        "generation routes through ai-mode, responses carry structured citations "
        "and confidence, and an insufficient-context branch bypasses generation. "
        "Docker boundary: RAG and Student 3 share ai; RAG publishes no host port."
    )


def _post_question(base_url: str, question: str) -> requests.Response:
    return requests.post(
        f"{base_url}{INTEGRATION_PATH}",
        json={"question": question},
        timeout=max(REQUEST_TIMEOUT_SECONDS, 120),
    )


def collect(_owner: str | None, repo_root: Path) -> tuple[bool, str]:
    static_ok, static_evidence = _validate_static_contract(repo_root)
    if not static_ok:
        return False, static_evidence

    base_url = _resolve_base_url(repo_root)
    if not base_url:
        return False, (
            "No Student 3 backend URL is available. Set RAG_VALIDATION_BASE_URL "
            "or publish student-3-backend in docker-compose.yml."
        )

    try:
        grounded_response = _post_question(
            base_url,
            "How does Canvas assignment sync work in the Deadline Tracker?")
        insufficient_response = _post_question(
            base_url,
            "What meals are served in the campus cafeteria today?")
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
        f"RAG VALIDATION PASSED. {static_evidence} Live grounded query returned "
        f"confidence '{grounded['confidence']}' with {len(citations)} citation(s) "
        f"from {citation_sources}. Unrelated query returned status "
        "'insufficient_context', confidence 'insufficient', and zero citations."
    )
