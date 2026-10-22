export interface MultiAgentWorkflow {
  id: string
  owner: string
  studentId: string
  goal: string
  status: string
  createdAtUtc: string
  updatedAtUtc: string
  planner: {
    objective: string
    requiredEvidence: string[]
    steps: string[]
    completedAtUtc: string
  }
  worker: {
    evidenceSummary: string
    draftOutput: string
    recommendedActions: string[]
    citations: string[]
    completedAtUtc: string
  }
  reviewer: {
    verdict: string
    confidence: string
    critique: string
    identifiedRisks: string[]
    recommendations: string[]
    completedAtUtc: string
  }
  humanReview: {
    decision: string
    reviewerName: string
    comments: string
    finalOutput: string
    reviewedAtUtc: string
  } | null
  auditLog: { timestampUtc: string; agent: string; action: string; summary: string }[]
  revision: number
  revisions: {
    revision: number
    requestId: string
    reviewerName: string
    feedback: string
    requestedAtUtc: string
    planner: MultiAgentWorkflow['planner']
    worker: MultiAgentWorkflow['worker']
    reviewer: MultiAgentWorkflow['reviewer']
  }[] | null
}

export interface ProfileSummaryProposal {
  workflow: MultiAgentWorkflow
  newSummary: string
}

export interface ProfileSummaryReviewResult {
  workflow: MultiAgentWorkflow
  approvedSummary: string | null
}