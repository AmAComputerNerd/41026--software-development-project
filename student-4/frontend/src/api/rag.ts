import { request } from './http'

export interface RagAnswer {
  status: 'success' | 'insufficient_context'
  answer: string
  confidence: 'low' | 'medium' | 'high' | 'insufficient'
  citations: { sourceId: string; title: string; heading: string; score: number }[]
  retrieval: { matchedChunks: number; consideredChunks: number }
}

export function askAccountHelp(question: string): Promise<RagAnswer> {
  return request('/users/help/answers', {
    method: 'POST',
    body: JSON.stringify({ question }),
  })
}
