from pathlib import Path

class PromptRegistry:
    def __init__(self, prompt_root: Path):
        self.root = prompt_root

    def resolve(self, relative_file: str) -> Path:
        candidate = self.root / relative_file
        if not candidate.exists():
            raise FileNotFoundError(f"Missing prompt file: {candidate}")
        return candidate

    def read(self, relative_file: str) -> str:
        return self.resolve(relative_file).read_text(encoding="utf-8").strip()
