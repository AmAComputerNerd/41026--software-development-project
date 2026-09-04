"""
Documentation Loader for the Agentic Loop.

Resolves, extracts, and formats authoritative repository documentation
(AGENTS.md, docs/ library, and microservice READMEs) to ground the
Implementation and Review agents in real architectural constraints.
"""

from __future__ import annotations

from pathlib import Path


def _read_file_safe(path: Path) -> str | None:
    if path.exists() and path.is_file():
        try:
            return path.read_text(encoding="utf-8").strip()
        except OSError:
            return None
    return None


def _format_doc_section(title: str, file_path: Path, content: str) -> str:
    rel_path = file_path.name
    return f"### Document: {title} (`{rel_path}`)\n\n{content}\n"


def load_documentation(
    owner: str | None,
    layer: str,
    repo_root: Path,
) -> tuple[str, list[str]]:
    """Load universal, layer-specific, and owner-specific documentation.

    Returns:
        tuple[str, list[str]]: (formatted_doc_context, list_of_loaded_doc_paths)
    """
    loaded_paths: list[str] = []
    doc_sections: list[str] = []

    # 1. Universal Architectural Rules & System Specs (AGENTS.md)
    agents_md = repo_root / "AGENTS.md"
    content = _read_file_safe(agents_md)
    if content:
        doc_sections.append(_format_doc_section("Universal Agent Architecture & Golden Rules", agents_md, content))
        loaded_paths.append("AGENTS.md")

    # 2. Layer-Specific Documentation
    if layer == "frontend":
        ui_kit_readme = repo_root / "shared" / "ui-kit" / "README.md"
        content = _read_file_safe(ui_kit_readme)
        if content:
            doc_sections.append(_format_doc_section("Neobrutalism Design System & UI Kit", ui_kit_readme, content))
            loaded_paths.append("shared/ui-kit/README.md")

        frontend_playbook = repo_root / "docs" / "playbooks" / "new-frontend-microservice.md"
        content = _read_file_safe(frontend_playbook)
        if content:
            doc_sections.append(_format_doc_section("Frontend Playbook & Specifications", frontend_playbook, content))
            loaded_paths.append("docs/playbooks/new-frontend-microservice.md")

        if owner and owner != "shared":
            owner_frontend_readme = repo_root / owner / "frontend" / "README.md"
            content = _read_file_safe(owner_frontend_readme)
            if content:
                doc_sections.append(_format_doc_section(f"{owner} Frontend README", owner_frontend_readme, content))
                loaded_paths.append(f"{owner}/frontend/README.md")

    elif layer == "backend":
        services_doc = repo_root / "docs" / "architecture" / "services.md"
        content = _read_file_safe(services_doc)
        if content:
            doc_sections.append(_format_doc_section("Microservices Catalog & Endpoints", services_doc, content))
            loaded_paths.append("docs/architecture/services.md")

        data_flows_doc = repo_root / "docs" / "architecture" / "data-flows.md"
        content = _read_file_safe(data_flows_doc)
        if content:
            doc_sections.append(_format_doc_section("Cross-Service Data Flows", data_flows_doc, content))
            loaded_paths.append("docs/architecture/data-flows.md")

        backend_playbook = repo_root / "docs" / "playbooks" / "new-backend-microservice.md"
        content = _read_file_safe(backend_playbook)
        if content:
            doc_sections.append(_format_doc_section("Backend Playbook & Standards", backend_playbook, content))
            loaded_paths.append("docs/playbooks/new-backend-microservice.md")

        if owner and owner != "shared":
            owner_backend_readme = repo_root / owner / "backend" / "README.md"
            content = _read_file_safe(owner_backend_readme)
            if content:
                doc_sections.append(_format_doc_section(f"{owner} Backend README", owner_backend_readme, content))
                loaded_paths.append(f"{owner}/backend/README.md")

    elif layer == "database":
        db_doc = repo_root / "docs" / "development" / "database-and-migrations.md"
        content = _read_file_safe(db_doc)
        if content:
            doc_sections.append(_format_doc_section("Database Isolation & Migrations Guide", db_doc, content))
            loaded_paths.append("docs/development/database-and-migrations.md")

        if owner and owner != "shared":
            owner_backend_readme = repo_root / owner / "backend" / "README.md"
            content = _read_file_safe(owner_backend_readme)
            if content:
                doc_sections.append(_format_doc_section(f"{owner} Backend/DB README", owner_backend_readme, content))
                loaded_paths.append(f"{owner}/backend/README.md")

    elif layer == "compose":
        overview_doc = repo_root / "docs" / "architecture" / "overview.md"
        content = _read_file_safe(overview_doc)
        if content:
            doc_sections.append(_format_doc_section("Architecture Topology & Routing", overview_doc, content))
            loaded_paths.append("docs/architecture/overview.md")

    # 3. Owner-level README (if present)
    if owner and owner != "shared":
        owner_readme = repo_root / owner / "README.md"
        content = _read_file_safe(owner_readme)
        if content:
            doc_sections.append(_format_doc_section(f"{owner} Microservice Blueprint", owner_readme, content))
            loaded_paths.append(f"{owner}/README.md")

    if not doc_sections:
        return "No documentation files found in repository.", []

    formatted_context = "## Authoritative Codebase Documentation & Architectural Rules\n\n" + "\n---\n".join(doc_sections)
    return formatted_context, loaded_paths
