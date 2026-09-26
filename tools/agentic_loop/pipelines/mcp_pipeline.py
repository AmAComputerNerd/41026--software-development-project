from __future__ import annotations


def build_user_prompt(
    task_prompt: str,
    context: str,
    evidence: str,
    doc_context: str = "",
) -> str:
    prompt = (
        task_prompt
        .replace("{{VALIDATION_EVIDENCE}}", evidence)
        .replace("{{PROJECT_CONTEXT}}", context)
        .replace("{{DOCUMENTATION_CONTEXT}}", doc_context)
    )
    return prompt.strip()
