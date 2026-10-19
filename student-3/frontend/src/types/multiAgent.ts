export type HumanDecision = 'Approve' | 'Correct' | 'PartialAccept' | 'Reject'

export interface HumanReviewInput {
  decision: HumanDecision
  reviewerName: string
  comments: string
  correctedOutput?: string
}

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
    decision: HumanDecision
    reviewerName: string
    comments: string
    finalOutput: string
    reviewedAtUtc: string
  } | null
  auditLog: {
    timestampUtc: string
    agent: string
    action: string
    summary: string
  }[]
}
