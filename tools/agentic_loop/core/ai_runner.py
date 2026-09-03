from __future__ import annotations

import os
from pathlib import Path

try:
    from dotenv import load_dotenv
except ImportError:
    def load_dotenv(*_args, **_kwargs):
        pass

try:
    from openai import OpenAI
except ImportError:
    OpenAI = None


def _truncate_words(text: str, limit: int = 150) -> str:
    words = " ".join(text.split()).split()
    if len(words) <= limit:
        return " ".join(words)
    return " ".join(words[:limit]) + " ..."


class AIRunner:
    def __init__(self, repo_root: Path | None = None):
        if repo_root:
            load_dotenv(repo_root / ".env")
        load_dotenv(Path(__file__).resolve().parent.parent / ".env")

        # Prioritize OpenRouter (approved by tutor), then Ollama / OpenAI
        openrouter_key = os.getenv("OPENROUTER_API_KEY")
        if openrouter_key:
            self.base_url = os.getenv("OPENROUTER_BASE_URL", "https://openrouter.ai/api/v1")
            self.api_key = openrouter_key
            self.implementation_model = os.getenv(
                "OPENROUTER_MODEL",
                "nvidia/nemotron-3-ultra-550b-a55b:free",
            )
            self.review_model = os.getenv("OPENROUTER_REVIEW_MODEL", self.implementation_model)
        else:
            self.base_url = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434/v1")
            self.api_key = os.getenv("OPENAI_API_KEY", "ollama")
            self.implementation_model = os.getenv("OLLAMA_MODEL", "llama3.1:8b")
            self.review_model = os.getenv("OLLAMA_REVIEW_MODEL", self.implementation_model)

        if OpenAI is not None:
            self.client = OpenAI(base_url=self.base_url, api_key=self.api_key, timeout=180.0)
        else:
            self.client = None

    def call(
        self,
        system_prompt: str,
        user_prompt: str,
        *,
        review: bool = False,
        max_tokens: int = 350,
    ) -> tuple[str | None, str | None]:
        if self.client is None:
            return None, "OpenAI SDK not installed. Run 'pip install openai' in tools/agentic_loop."

        model_name = self.review_model if review else self.implementation_model
        try:
            response = self.client.chat.completions.create(
                model=model_name,
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
                max_tokens=max_tokens,
                temperature=0.1,
            )
            content = (response.choices[0].message.content or "").strip()
            if not content:
                return "No response generated.", None
            return _truncate_words(content), None
        except Exception as exc:
            return None, f"Model call failed ({model_name} @ {self.base_url}): {exc}"
