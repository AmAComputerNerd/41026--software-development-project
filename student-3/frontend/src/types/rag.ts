export type RagConfidence = 'high' | 'medium' | 'low' | 'insufficient'

export interface RagCitation {
  sourceId: string
  title: string
  heading: string
  score: number
}

export interface RagRetrievalSummary {
  matchedChunks: number
  consideredChunks: number
}

export interface RagAnswerResult {
  status: 'success' | 'insufficient_context'
  answer: string
  confidence: RagConfidence
  citations: RagCitation[]
  retrieval: RagRetrievalSummary
}
