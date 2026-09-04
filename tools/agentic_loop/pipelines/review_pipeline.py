from __future__ import annotations


def build_review_prompt(
    review_task_prompt: str,
    recommendation: str,
    evidence: str,
    doc_context: str = "",
) -> str:
    """Build the review agent's user prompt."""
    prompt = (
        review_task_prompt
        .replace("{{RECOMMENDATION}}", recommendation)
        .replace("{{VALIDATION_EVIDENCE}}", evidence)
        .replace("{{DOCUMENTATION_CONTEXT}}", doc_context)
    )
    return prompt.strip()
