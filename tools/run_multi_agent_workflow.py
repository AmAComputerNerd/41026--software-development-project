#!/usr/bin/env python3
"""Terminal runner for the Release 2 Multi-Agent Server (Planner -> Worker -> Reviewer -> Human Review)."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
import urllib.error
import urllib.request

DEFAULT_STUDENT_ID = "11111111-1111-1111-1111-111111111111"


def _post_json(url: str, payload: dict[str, object], timeout: float = 30.0) -> dict[str, object]:
    if not url.startswith(("http://", "https://")):
        raise ValueError("URL must start with http:// or https://")
    data = json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(  # noqa: S310
        url,
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=timeout) as response:  # noqa: S310
        raw = response.read().decode("utf-8", errors="replace")
        return json.loads(raw)


def _format_workflow_transcript(
    stage1: dict[str, object],
    stage2: dict[str, object],
) -> str:
    planner = stage1.get("planner", {}) if isinstance(stage1.get("planner"), dict) else {}
    worker = stage1.get("worker", {}) if isinstance(stage1.get("worker"), dict) else {}
    reviewer = stage1.get("reviewer", {}) if isinstance(stage1.get("reviewer"), dict) else {}
    human = stage2.get("humanReview", {}) if isinstance(stage2.get("humanReview"), dict) else {}
    audit_log = stage2.get("auditLog", []) if isinstance(stage2.get("auditLog"), list) else []

    lines = [
        "======================================================================",
        "RELEASE 2 MULTI-AGENT WORKFLOW EXECUTION TRANSCRIPT (TERMINAL)",
        "Flow: Planner -> Worker -> Reviewer -> Human Review",
        "======================================================================",
        f"Workflow ID : {stage2.get('id')}",
        f"Owner Slice : {stage2.get('owner')}",
        f"Student ID  : {stage2.get('studentId')}",
        f"Goal        : {stage2.get('goal')}",
        f"Stage 1 Status : {stage1.get('status')}",
        f"Final Status   : {stage2.get('status')}",
        "",
        "--- [1. PLANNER AGENT] ---",
        f"Objective   : {planner.get('objective')}",
        f"Evidence Req: {', '.join(str(x) for x in planner.get('requiredEvidence', []))}",
        "Steps:",
    ]
    for idx, step in enumerate(planner.get("steps", []), start=1):
        lines.append(f"  {idx}. {step}")

    lines.extend(
        [
            "",
            "--- [2. WORKER AGENT] ---",
            f"Evidence Summary : {worker.get('evidenceSummary')}",
            f"Draft Output     : {worker.get('draftOutput')}",
            f"Citations        : {', '.join(str(x) for x in worker.get('citations', []))}",
            "Recommended Actions:",
        ]
    )
    for idx, action in enumerate(worker.get("recommendedActions", []), start=1):
        lines.append(f"  {idx}. {action}")

    lines.extend(
        [
            "",
            "--- [3. REVIEWER AGENT] ---",
            f"Verdict          : {reviewer.get('verdict')} (Confidence: {reviewer.get('confidence')})",
            f"Critique         : {reviewer.get('critique')}",
            "Identified Risks:",
        ]
    )
    for idx, risk in enumerate(reviewer.get("identifiedRisks", []), start=1):
        lines.append(f"  {idx}. {risk}")

    lines.extend(
        [
            "",
            "--- [4. HUMAN REVIEW GATE] ---",
            f"Decision         : {human.get('decision')}",
            f"Reviewer Name    : {human.get('reviewerName')}",
            f"Reviewer Notes   : {human.get('comments')}",
            f"Final Output     : {human.get('finalOutput')}",
            f"Reviewed At (UTC): {human.get('reviewedAtUtc')}",
            "",
            "--- [COORDINATION & AUDIT LOG] ---",
        ]
    )
    for entry in audit_log:
        if isinstance(entry, dict):
            lines.append(
                f"  [{entry.get('timestampUtc')}] [{entry.get('agent')}] "
                f"{entry.get('action')}: {entry.get('summary')}"
            )

    lines.append("======================================================================")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Execute Planner -> Worker -> Reviewer -> Human Review on the Multi-Agent Server."
    )
    parser.add_argument(
        "--base-url",
        default="http://127.0.0.1:5004",
        help="Base URL of multi-agent-server (default: http://127.0.0.1:5004)",
    )
    parser.add_argument(
        "--owner",
        default="student-1",
        help="Microservice owner slice (e.g. student-1)",
    )
    parser.add_argument(
        "--student-id",
        default=DEFAULT_STUDENT_ID,
        help="Student GUID",
    )
    parser.add_argument(
        "--goal",
        default="Triage my unread Canvas deadline and grade notifications and build a 48-hour action plan.",
        help="Workflow goal for the Planner agent",
    )
    parser.add_argument(
        "--decision",
        choices=["Approve", "Correct", "PartialAccept", "Reject"],
        default="Approve",
        help="Human review decision to submit at Stage 2",
    )
    parser.add_argument(
        "--reviewer",
        default="Bryan Lee (25495108)",
        help="Human reviewer name",
    )
    parser.add_argument(
        "--comments",
        default="Verified notification priorities and approved the 48-hour study action plan.",
        help="Human review notes",
    )
    parser.add_argument(
        "--corrected-output",
        default="",
        help="Optional corrected output when decision is Correct or PartialAccept",
    )
    parser.add_argument(
        "--out",
        type=Path,
        default=None,
        help="Optional output file path to write the terminal transcript",
    )
    args = parser.parse_args()

    base = args.base_url.rstrip("/")
    try:
        stage1 = _post_json(
            f"{base}/api/workflows/run",
            {
                "owner": args.owner,
                "studentId": args.student_id,
                "goal": args.goal,
            },
        )
        workflow_id = stage1.get("id")
        if not workflow_id:
            print("Multi-Agent Server response did not contain a workflow id.", file=sys.stderr)
            return 1

        stage2 = _post_json(
            f"{base}/api/workflows/{workflow_id}/human-review",
            {
                "decision": args.decision,
                "reviewerName": args.reviewer,
                "comments": args.comments,
                "correctedOutput": args.corrected_output or None,
            },
        )
    except (urllib.error.URLError, ValueError, json.JSONDecodeError) as exc:
        print(f"Multi-Agent workflow execution failed: {exc}", file=sys.stderr)
        return 1

    transcript = _format_workflow_transcript(stage1, stage2)
    print(transcript)
    if args.out is not None:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(transcript + "\n", encoding="utf-8")
        print(f"\nSaved terminal workflow transcript to {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
