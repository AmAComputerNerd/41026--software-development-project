import { request } from './http'
import type { RagAnswerResult } from '@/types/rag'

export const askProjectQuestion = (question: string) =>
  request<RagAnswerResult>('/integrations/rag/answers', {
    method: 'POST',
    body: JSON.stringify({ question }),
  })
