"""
Utils for docker compose parsing.
"""

from pathlib import Path
from typing import Any


def load_compose(repo_root: Path) -> dict[str, Any] | None:
    compose_path = repo_root / "docker-compose.yml"
    if not compose_path.exists():
        return None

    import yaml  # local import: only needed when a compose file exists

    return yaml.safe_load(compose_path.read_text(encoding="utf-8")) or {}


def get_service_host_port(compose: dict[str, Any] | None, service_name: str) -> int | None:
    if not compose:
        return None

    service = (compose.get("services") or {}).get(service_name)
    if not service:
        return None

    for mapping in service.get("ports") or []:
        # Ports can be "8080:80", "8080:80/tcp", or an already-parsed int/dict.
        host_part = str(mapping).split(":")[0]
        if host_part.isdigit():
            return int(host_part)

    return None
