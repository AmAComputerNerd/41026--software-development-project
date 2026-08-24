def build_user_prompt(task_prompt: str, context: str, evidence: str) -> str:
    """Build the user prompt for a Backend review, injecting placeholders."""

    task_with_evidence = task_prompt.replace("{{REVIEW_TARGET}}", "Backend")
    task_with_evidence = task_with_evidence.replace("{{VALIDATION_EVIDENCE}}", evidence)

    return f"""
{task_with_evidence}

Feature Context:
{context}
""".strip()
