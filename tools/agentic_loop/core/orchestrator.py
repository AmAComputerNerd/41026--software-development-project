from __future__ import annotations

from pathlib import Path

from collectors import backend_collector, compose_collector, database_collector, frontend_collector
from config.review_config import PROMPT_ROOT, get_owner_context
from core.ai_runner import AIRunner
from core.doc_loader import load_documentation
from core.prompt_registry import PromptRegistry
from pipelines import backend_pipeline, compose_pipeline, database_pipeline, frontend_pipeline, review_pipeline

COLLECTORS = {
    "frontend": frontend_collector.collect,
    "backend": backend_collector.collect,
    "database": database_collector.collect,
    "compose": compose_collector.collect,
}

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


def _run_review_stage(
    label: str,
    prompts: PromptRegistry,
    ai: AIRunner,
    recommendation: str,
    evidence: str,
    doc_context: str,
) -> str:
    """Second-pass review agent (Adapt - Phase 2): critiques the implementation agent's
    recommendation against the evidence and authoritative documentation.
    """
    _stage(label, "ADAPT-REVIEW", "Loading review prompt and evaluating architectural compliance")
    try:
        review_system_prompt = prompts.read("review_system_prompt.txt")
        review_task_prompt = prompts.read("review_task_prompt.txt")
    except FileNotFoundError as exc:
        _stage(label, "ADAPT-REVIEW", "Failed to load review prompt")
        return f"Review skipped: {exc}"

    review_user_prompt = review_pipeline.build_review_prompt(
        review_task_prompt,
        recommendation,
        evidence,
        doc_context=doc_context,
    )

    review_output, review_err = ai.call(review_system_prompt, review_user_prompt, review=True)
    if review_err:
        _stage(label, "ADAPT-REVIEW", "Review model call failed")
        return f"Review unavailable: {review_err}"

    _stage(label, "ADAPT-REVIEW", "Complete")
    return review_output or "No review comments."


def run_target(layer: str, owner: str | None, repo_root: Path, ai: AIRunner) -> str:
    """Execute the Plan -> Act -> Observe -> Adapt Agentic AI workflow for a target."""
    label = f"{owner}/{layer}" if owner else layer
    prompts = PromptRegistry(PROMPT_ROOT)

    # 1. PLAN: Resolve documentation, feature context, and validation criteria
    _stage(label, "PLAN", "Loading authoritative codebase documentation and feature context")
    doc_context, loaded_docs = load_documentation(owner, layer, repo_root)
    context, context_source = get_owner_context(owner if owner else "shared", repo_root)
    _stage(label, "PLAN", f"Loaded context from '{context_source}' and docs: {', '.join(loaded_docs) if loaded_docs else 'none'}")

    # 2. ACT: Execute probes and collect evidence
    _stage(label, "ACT", "Executing live probes, schema queries, and static inspection")
    collector = COLLECTORS[layer]
    ok, evidence = collector(owner, repo_root)
    if not ok:
        _stage(label, "ACT", "Inspection failed")
        return f"ACT/OBSERVE FAILED: {evidence}"

    # 3. OBSERVE: Structure and compile gathered evidence
    _stage(label, "OBSERVE", f"Structured {len(evidence.split())} words of observation evidence")

    if layer not in TASK_PROMPTS:
        _stage(label, "DONE", "No evaluation prompt configured for this layer")
        return f"OBSERVE: {evidence}"

    # 4. ADAPT (Phase 1 - Implementation Agent Proposal)
    _stage(label, "ADAPT-PROPOSE", "Running implementation model grounded in documentation")
    system_prompt = prompts.read("system_prompt.txt")
    task_prompt = prompts.read(TASK_PROMPTS[layer])

    build_user_prompt = IMPLEMENTATION_PIPELINES[layer]
    user_prompt = build_user_prompt(
        task_prompt=task_prompt,
        context=context,
        evidence=evidence,
        doc_context=doc_context,
    )

    output, err = ai.call(system_prompt, user_prompt, review=False)
    if err:
        _stage(label, "ADAPT-PROPOSE", "Model call failed")
        return f"MODEL FAILED: {err}"
    _stage(label, "ADAPT-PROPOSE", "Proposal generated")

    # 4. ADAPT (Phase 2 - Review Agent Evaluation)
    review_output = _run_review_stage(
        label=label,
        prompts=prompts,
        ai=ai,
        recommendation=output or "",
        evidence=evidence,
        doc_context=doc_context,
    )

    _stage(label, "DONE", "Plan -> Act -> Observe -> Adapt cycle complete")
    return f"OBSERVE: {evidence}\n\nIMPLEMENTATION (ADAPT PROPOSAL): {output}\n\nREVIEW (ADAPT CRITIQUE): {review_output}"
