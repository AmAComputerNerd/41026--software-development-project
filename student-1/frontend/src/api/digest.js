// Fetch-based client for the AI digest endpoints.

import { buildQuery, request } from './http'

export function getDigests(studentId) {
  const query = buildQuery({ studentId })
  return request(`/digest${query}`)
}

export function generateDigest(studentId) {
  const query = buildQuery({ studentId })
  return request(`/digest/generate${query}`, { method: 'POST' })
}

export function chatWithAssistant(studentId, prompt, history = []) {
  return request('/digest/chat', {
    method: 'POST',
    body: JSON.stringify({ studentId, prompt, history }),
  })
}
