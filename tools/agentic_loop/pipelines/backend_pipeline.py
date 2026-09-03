from __future__ import annotations


def build_user_prompt(
    task_prompt: str,
    context: str,
    evidence: str,
    doc_context: str = "",
) -> str:
    """Build the user prompt for a Backend review, injecting placeholders."""
    prompt = (
        task_prompt
        .replace("{{REVIEW_TARGET}}", "Backend")
        .replace("{{VALIDATION_EVIDENCE}}", evidence)
        .replace("{{FEATURE_CONTEXT}}", context)
        .replace("{{DOCUMENTATION_CONTEXT}}", doc_context)
    )
    return prompt.strip()
