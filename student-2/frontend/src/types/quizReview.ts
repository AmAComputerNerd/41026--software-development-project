export interface MultiAgentWorkflow {
  id: string
  owner: string
  goal: string
  status: string
  revision: number
  planner: { objective: string; requiredEvidence: string[]; steps: string[] }
  worker: { evidenceSummary: string; draftOutput: string; recommendedActions: string[]; citations: string[] }
  reviewer: {
    verdict: string
    confidence: string
    critique: string
    identifiedRisks: string[]
    recommendations: string[]
  }
  humanReview: {
    decision: string
    reviewerName: string
    comments: string
    finalOutput: string
    reviewedAtUtc: string
  } | null
  auditLog: { timestampUtc: string; agent: string; action: string; summary: string }[]
}

export interface QuizReviewQuestion {
  id: number
  questionType: string
  questionText: string
  options: { id: number; text: string }[]
  proposedAnswerId: number | null
  proposedText: string | null
}

export interface QuizReview {
  runId: string
  automationId: string
  quizTitle: string
  courseId: number
  quizId: number
  result: 'RUN' | 'SUC' | 'FAI' | 'REV' | 'REJ'
  workflowId: string | null
  workflowRevision: number
  proposalSource: 'multi-agent-worker' | 'ai-mode-baseline'
  questions: QuizReviewQuestion[]
  workflow: MultiAgentWorkflow | null
  workflowError: string | null
  reviewDecision: string | null
  reviewerName: string | null
  reviewedAtUtc: string | null
}

export type QuizReviewDecision = 'Approve' | 'Correct' | 'PartialAccept' | 'Reject'

export interface QuizReviewAnswer {
  questionId: number
  answerId: number | null
  text: string | null
}

export interface SubmitQuizReviewInput {
  decision: QuizReviewDecision
  reviewerName: string
  comments: string
  answers?: QuizReviewAnswer[]
}

export interface ReviseQuizReviewInput {
  requestId: string
  reviewerName: string
  feedback: string
}
