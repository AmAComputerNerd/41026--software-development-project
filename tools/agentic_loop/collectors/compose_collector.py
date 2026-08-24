from pathlib import Path

from core.compose_utils import load_compose


def collect(owner: str | None, repo_root: Path) -> tuple[bool, str]:
    compose_path = repo_root / "docker-compose.yml"
    if not compose_path.exists():
        return False, "No docker-compose.yml found at the repo root."

    compose = load_compose(repo_root)
    services = (compose or {}).get("services") or {}
    if not services:
        return False, "docker-compose.yml exists but defines no services yet."

    lines = []
    for name, cfg in services.items():
        cfg = cfg or {}
        build = cfg.get("build")
        image = cfg.get("image")
        ports = cfg.get("ports") or []
        volumes = cfg.get("volumes") or []
        depends_on = cfg.get("depends_on") or []
        environment = cfg.get("environment") or {}
        lines.append(
            f"service '{name}': source={build or image or 'undefined'}, "
            f"ports={ports}, volumes={volumes}, depends_on={depends_on}, "
            f"env_keys={list(environment) if isinstance(environment, dict) else environment}"
        )

    networks = list((compose.get("networks") or {}).keys())
    top_level_volumes = list((compose.get("volumes") or {}).keys())

    evidence = (
        "Docker Compose evidence: " + "; ".join(lines) + ". "
        f"Networks defined: {networks or 'none'}. "
        f"Top-level volumes defined: {top_level_volumes or 'none'}."
    )
    return True, evidence
