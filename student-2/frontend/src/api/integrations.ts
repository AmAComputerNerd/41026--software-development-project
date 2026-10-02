import { request } from './http'

export interface McpAutomationResult {
  status: 'success' | 'error'
  tool: string
  data: {
    health: 'healthy' | 'attention' | 'in_progress' | 'no_activity'
    summary: string
    metrics: {
      windowDays: number
      enabledAutomations: number
      totalRuns: number
      successfulRuns: number
      failedRuns: number
      runningRuns: number
      successRatePercent: number | null
      lastRunAt: string | null
      generatedAt: string
    }
  } | null
  error: { code: string; message: string } | null
}

export interface RagAnswerResult {
  status: 'success' | 'insufficient_context'
  answer: string
  confidence: 'high' | 'medium' | 'low' | 'insufficient'
  citations: Array<{
    sourceId: string
    title: string
    heading: string
    score: number
  }>
  retrieval: {
    matchedChunks: number
    consideredChunks: number
  }
}

export const reviewAutomationHealthThroughMcp = (days: number) =>
  request<McpAutomationResult>('/integrations/mcp/automation-health', {
    method: 'POST',
    body: JSON.stringify({ days }),
  })

export const askProjectQuestion = (question: string) =>
  request<RagAnswerResult>('/integrations/rag/query', {
    method: 'POST',
    body: JSON.stringify({ question }),
  })