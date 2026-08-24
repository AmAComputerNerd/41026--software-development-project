def build_user_prompt(task_prompt: str, context: str, evidence: str) -> str:
    """Build the user prompt for the Docker Compose architecture review."""

    task_with_evidence = task_prompt.replace("{{REVIEW_TARGET}}", "Docker Compose architecture")
    task_with_evidence = task_with_evidence.replace("{{VALIDATION_EVIDENCE}}", evidence)

    return f"""
{task_with_evidence}

Project Context:
{context}
""".strip()
