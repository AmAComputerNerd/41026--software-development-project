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
