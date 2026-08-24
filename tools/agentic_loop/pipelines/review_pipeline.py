def build_review_prompt(review_task_prompt: str, recommendation: str, evidence: str) -> str:
    """Build the review agent's user prompt. Shared across every layer/owner
    - the review agent's job (critique the recommendation against the
    evidence, or confirm "no improvement" was a valid outcome) doesn't vary
    by feature.
    """
    return (
        review_task_prompt
        .replace("{{RECOMMENDATION}}", recommendation)
        .replace("{{VALIDATION_EVIDENCE}}", evidence)
    )
