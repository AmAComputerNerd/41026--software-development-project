import { request } from './http'

export interface McpBroadcastResult {
  success: boolean
  status: string
  tool: string
  data: any
  error?: string | null
}

export interface RagQueryResult {
  question: string
  answer: string
  citations: string[]
  confidence: string
  hasSufficientContext: boolean
}

export async function broadcastMcpAlert(
  studentId: string,
  title: string,
  message: string,
  urgency = 'Medium',
): Promise<McpBroadcastResult> {
  return request('/api/notifications/mcp/broadcast', {
    method: 'POST',
    body: JSON.stringify({ studentId, title, message, urgency }),
  })
}

export async function queryRagKnowledge(query: string, scope = 'student-1'): Promise<RagQueryResult> {
  return request('/api/notifications/rag/query', {
    method: 'POST',
    body: JSON.stringify({ query, scope }),
  })
}

export interface MultiAgentPlannerStage {
  objective: string
  requiredEvidence: string[]
  steps: string[]
  completedAtUtc: string
}

export interface MultiAgentWorkerStage {
  evidenceSummary: string
  draftOutput: string
  recommendedActions: string[]
  citations: string[]
  completedAtUtc: string
}

export interface MultiAgentReviewerStage {
  verdict: string
  confidence: string
  critique: string
  identifiedRisks: string[]
  recommendations: string[]
  completedAtUtc: string
}

export interface MultiAgentHumanReviewStage {
  decision: string
  reviewerName: string
  comments: string
  finalOutput: string
  reviewedAtUtc: string
}

export interface MultiAgentAuditEntry {
  timestampUtc: string
  agent: string
  action: string
  summary: string
}

export interface MultiAgentWorkflowRecord {
  id: string
  owner: string
  studentId: string
  goal: string
  status: string
  createdAtUtc: string
  updatedAtUtc: string
  planner: MultiAgentPlannerStage
  worker: MultiAgentWorkerStage
  reviewer: MultiAgentReviewerStage
  humanReview?: MultiAgentHumanReviewStage | null
  auditLog: MultiAgentAuditEntry[]
}

export async function listMultiAgentWorkflows(studentId?: string): Promise<MultiAgentWorkflowRecord[]> {
  const qs = studentId ? `?studentId=${encodeURIComponent(studentId)}` : ''
  return request(`/api/notifications/multi-agent/workflows${qs}`, {
    method: 'GET',
  })
}

export async function runMultiAgentWorkflow(
  goal: string,
  studentId?: string,
): Promise<MultiAgentWorkflowRecord> {
  return request('/api/notifications/multi-agent/run', {
    method: 'POST',
    body: JSON.stringify({ studentId, goal }),
  })
}

export async function submitMultiAgentHumanReview(
  workflowId: string,
  decision: 'Approve' | 'Correct' | 'PartialAccept' | 'Reject',
  reviewerName: string,
  comments?: string,
  correctedOutput?: string,
): Promise<MultiAgentWorkflowRecord> {
  return request(`/api/notifications/multi-agent/workflows/${encodeURIComponent(workflowId)}/review`, {
    method: 'POST',
    body: JSON.stringify({ decision, reviewerName, comments, correctedOutput }),
  })
}

