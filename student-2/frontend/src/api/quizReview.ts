import { request } from './http'
import type { QuizReview, ReviseQuizReviewInput, SubmitQuizReviewInput } from '@/types/quizReview'

export const getQuizReview = (runId: string) =>
  request<QuizReview>(`/automation-runs/${encodeURIComponent(runId)}/quiz-review/`)

export const submitQuizReview = (runId: string, input: SubmitQuizReviewInput) =>
  request<QuizReview>(`/automation-runs/${encodeURIComponent(runId)}/quiz-review/`, {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const reviseQuizReview = (runId: string, input: ReviseQuizReviewInput) =>
  request<QuizReview>(`/automation-runs/${encodeURIComponent(runId)}/quiz-review/revise`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
