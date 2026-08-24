// Shared fetch-based client plumbing for the student-1 Notification Service.
// No axios: this is the only API surface the frontend needs so far.

export const BASE_URL = import.meta.env.VITE_NOTIFICATIONS_API_BASE_URL || 'http://localhost:5101'

export async function request(path, options = {}) {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!response.ok) {
    throw new Error(`Notification API request failed: ${response.status} ${response.statusText}`)
  }

  const text = await response.text()
  return text ? JSON.parse(text) : null
}

export function buildQuery(params) {
  const query = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      query.set(key, value)
    }
  }
  const stringified = query.toString()
  return stringified ? `?${stringified}` : ''
}
