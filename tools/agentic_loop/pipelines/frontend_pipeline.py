def build_user_prompt(task_prompt: str, context: str, evidence: str) -> str:
    """Build the user prompt for a Frontend (Vue + Vuetify) review."""

    task_with_evidence = task_prompt.replace("{{REVIEW_TARGET}}", "Frontend")
    task_with_evidence = task_with_evidence.replace("{{VALIDATION_EVIDENCE}}", evidence)

    return f"""
{task_with_evidence}

Feature Context:
{context}
""".strip()
