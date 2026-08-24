from pathlib import Path

from collectors import backend_collector, compose_collector, database_collector, frontend_collector
from config.review_config import PROMPT_ROOT, get_owner_context
from core.ai_runner import AIRunner
from core.prompt_registry import PromptRegistry
from pipelines import backend_pipeline, compose_pipeline, database_pipeline, frontend_pipeline, review_pipeline

COLLECTORS = {
    "frontend": frontend_collector.collect,
    "backend": backend_collector.collect,
    "database": database_collector.collect,
    "compose": compose_collector.collect,
}

# For a stack without a frontend collector implementation (i.e. not Vue +
# Vuetify), the collector fails fast with a "not implemented" message, so
# the LLM stage below is never reached for that owner.
TASK_PROMPTS = {
    "frontend": "frontend_task_prompt.txt",
    "backend": "backend_task_prompt.txt",
    "database": "database_task_prompt.txt",
    "compose": "compose_task_prompt.txt",
}

IMPLEMENTATION_PIPELINES = {
    "frontend": frontend_pipeline.build_user_prompt,
    "backend": backend_pipeline.build_user_prompt,
    "database": database_pipeline.build_user_prompt,
    "compose": compose_pipeline.build_user_prompt,
}


def _stage(label: str, step: str, message: str) -> None:
    print(f"[{label}][{step}] {message}")


def _run_review_stage(label: str, prompts: PromptRegistry, ai: AIRunner, recommendation: str, evidence: str) -> str:
    """Second-pass review agent: critiques the implementation agent's
    recommendation against the same evidence, using baseline prompts shared
    across every owner/layer. Not fatal if it fails - the implementation
    result is still useful on its own.
    """
    _stage(label, "REVIEW-PROMPTS", "Loading review prompt set")
    try:
        review_system_prompt = prompts.read("review_system_prompt.txt")
        review_task_prompt = prompts.read("review_task_prompt.txt")
    except FileNotFoundError as exc:
        _stage(label, "REVIEW-PROMPTS", "Failed")
        return f"Review skipped: {exc}"

    review_user_prompt = review_pipeline.build_review_prompt(review_task_prompt, recommendation, evidence)

    _stage(label, "REVIEW-LLM", "Running review model")
    review_output, review_err = ai.call(review_system_prompt, review_user_prompt, review=True)
    if review_err:
        _stage(label, "REVIEW-LLM", "Failed")
        return f"Review unavailable: {review_err}"

    _stage(label, "REVIEW-LLM", "Complete")
    return review_output


def run_target(layer: str, owner: str | None, repo_root: Path, ai: AIRunner) -> str:
    label = f"{owner}/{layer}" if owner else layer
    prompts = PromptRegistry(PROMPT_ROOT)

    _stage(label, "START", "Starting review flow")

    _stage(label, "OBSERVE", "Collecting evidence")
    collector = COLLECTORS[layer]
    ok, evidence = collector(owner, repo_root)
    if not ok:
        _stage(label, "OBSERVE", "Failed")
        return f"OBSERVE FAILED: {evidence}"
    _stage(label, "OBSERVE", "Complete")

    if layer not in TASK_PROMPTS:
        _stage(label, "DONE", "No review prompt configured for this layer")
        return f"OBSERVE: {evidence}"

    _stage(label, "PROMPTS", "Loading prompt set")
    system_prompt = prompts.read("system_prompt.txt")
    task_prompt = prompts.read(TASK_PROMPTS[layer])
    context, context_source = get_owner_context(owner if owner else "shared", repo_root)
    _stage(label, "PROMPTS", f"Loaded implementation prompt set (context from: {context_source})")

    build_user_prompt = IMPLEMENTATION_PIPELINES[layer]
    user_prompt = build_user_prompt(task_prompt, context, evidence)

    _stage(label, "LLM", "Running implementation model")
    output, err = ai.call(system_prompt, user_prompt, review=False)
    if err:
        _stage(label, "LLM", "Failed")
        return f"MODEL FAILED: {err}"
    _stage(label, "LLM", "Complete")

    review_output = _run_review_stage(label, prompts, ai, output, evidence)

    _stage(label, "DONE", "Review complete")
    return f"OBSERVE: {evidence}\n\nIMPLEMENTATION: {output}\n\nREVIEW: {review_output}"
