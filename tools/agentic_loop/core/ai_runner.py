import os

from openai import OpenAI


def _truncate_words(text: str, limit: int = 90) -> str:
    words = " ".join(text.split()).split()
    if len(words) <= limit:
        return " ".join(words)
    return " ".join(words[:limit]) + " ..."


class AIRunner:
    def __init__(self):
        self.base_url = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434/v1")
        self.implementation_model = os.getenv("OLLAMA_MODEL", "llama3.1:8b")
        self.review_model = os.getenv("OLLAMA_REVIEW_MODEL", self.implementation_model)
        api_key = os.getenv("OPENAI_API_KEY", "ollama")
        self.client = OpenAI(base_url=self.base_url, api_key=api_key, timeout=180.0)

    def call(
        self,
        system_prompt: str,
        user_prompt: str,
        *,
        review: bool = False,
        max_tokens: int = 220,
    ) -> tuple[str | None, str | None]:
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
            return None, f"Model call failed ({model_name}): {exc}"
