import sqlite3
from pathlib import Path

from config.review_config import owner_path

_IGNORED_DIR_PARTS = {"bin", "obj", "node_modules", ".git"}


def _find_db_files(root: Path) -> list[Path]:
    if not root.exists():
        return []
    return [
        p for p in root.rglob("*.db")
        if not _IGNORED_DIR_PARTS.intersection(p.parts)
    ] + [
        p for p in root.rglob("*.sqlite")
        if not _IGNORED_DIR_PARTS.intersection(p.parts)
    ]


def _describe_database(db_path: Path) -> str:
    conn = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    cursor = conn.cursor()

    tables = [
        row[0] for row in cursor.execute(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%'"
        ).fetchall()
    ]

    if not tables:
        conn.close()
        return f"'{db_path.name}' has no user-defined tables."

    lines = []
    for table in tables:
        columns = cursor.execute(f'PRAGMA table_info("{table}")').fetchall()
        # cid, name, type, notnull, dflt_value, pk
        column_desc = ", ".join(
            f"{col[1]} {col[2]}{' PK' if col[5] else ''}{' NOT NULL' if col[3] else ''}"
            for col in columns
        )
        foreign_keys = cursor.execute(f'PRAGMA foreign_key_list("{table}")').fetchall()
        fk_desc = ", ".join(f"{fk[3]} -> {fk[2]}.{fk[4]}" for fk in foreign_keys) or "none"
        row_count = cursor.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0]
        has_pk = any(col[5] for col in columns)

        lines.append(
            f"Table '{table}' ({row_count} row(s), primary key: {'yes' if has_pk else 'NO'}): "
            f"columns=[{column_desc}]; foreign keys=[{fk_desc}]"
        )

    fk_violations = cursor.execute("PRAGMA foreign_key_check").fetchall()
    if fk_violations:
        lines.append(f"Foreign key violations detected: {len(fk_violations)} row(s) reference missing parents.")

    conn.close()
    return f"'{db_path.name}' schema evidence: " + " | ".join(lines) + "."


def collect(owner: str, repo_root: Path) -> tuple[bool, str]:
    search_root = owner_path(owner, repo_root)
    db_files = _find_db_files(search_root)

    if not db_files:
        return False, (
            f"No SQLite database file (*.db / *.sqlite) found under "
            f"'{owner}'. Run the backend at least once so it creates/"
            "migrates its database, then re-run this review."
        )

    evidence = " ".join(_describe_database(db_path) for db_path in db_files)
    return True, evidence
