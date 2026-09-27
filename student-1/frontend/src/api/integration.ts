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
