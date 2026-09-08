"""
Persists a full transcript of each agentic_loop run to a timestamped log
file under `tools/agentic_loop/logs/<target>/`, in addition to the normal
terminal output - so every Plan -> Act -> Observe -> Adapt cycle leaves a
durable, human-readable record of what evidence was gathered and what the
implementation/review agents concluded, without changing anything else
about how the tool behaves interactively.

Logs are grouped into one folder per review target (the owner being
reviewed, e.g. `logs/student-3/`, or `logs/compose/` for the cross-cutting
docker-compose review), with one timestamped file per layer inside it.
"""

from __future__ import annotations

import sys
from contextlib import contextmanager
from datetime import datetime, timezone
from pathlib import Path
from typing import IO, Iterator

from config.review_config import LOGS_ROOT


class _Tee:
    """Minimal stdout replacement that mirrors every write to several streams."""

    def __init__(self, *streams: IO[str]) -> None:
        self._streams = streams

    def write(self, data: str) -> int:
        for stream in self._streams:
            stream.write(data)
        return len(data)

    def flush(self) -> None:
        for stream in self._streams:
            stream.flush()


def _log_path(label: str, logs_root: Path) -> Path:
    # `label` is either "<owner>/<layer>" (e.g. "student-3/backend") or just
    # "<layer>" for targets with no owner (e.g. the "compose" review).
    target, _, layer = label.partition("/")
    layer = layer or target

    target_dir = logs_root / target
    target_dir.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    safe_layer = layer.replace("/", "_")
    return target_dir / f"{timestamp}_{safe_layer}.log"


@contextmanager
def capture_run(label: str, logs_root: Path = LOGS_ROOT) -> Iterator[Path]:
    """Tee everything printed to stdout for the duration of a single review
    run (one owner/layer, or compose) into its own timestamped log file
    under `logs_root/<target>/`, while still printing to the real terminal
    as normal.
    """
    log_path = _log_path(label, logs_root)
    real_stdout = sys.stdout
    with log_path.open("w", encoding="utf-8") as log_file:
        sys.stdout = _Tee(real_stdout, log_file)
        try:
            yield log_path
        finally:
            sys.stdout.flush()
            sys.stdout = real_stdout

