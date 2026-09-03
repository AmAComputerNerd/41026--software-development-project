// Shared fetch-based client plumbing for the student-4 Account Service.
// No axios: this is the only API surface the frontend needs so far.

// Use relative URLs when proxying through Vite dev server
export const BASE_URL = ''

// Typed error so call sites can branch on HTTP status (e.g. handle 404
// "user not found" without grepping the error message string).
export class ApiError extends Error {
  status: number
  statusText: string
  body: string

  constructor(status: number, statusText: string, body: string) {
    super(`Account API request failed: ${status} ${statusText} - ${body}`)
    this.name = 'ApiError'
    this.status = status
    this.statusText = statusText
    this.body = body
  }
}

export async function request(path: string, options: RequestInit = {}) {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new ApiError(response.status, response.statusText, errorText)
  }

  const text = await response.text()
  return text ? JSON.parse(text) : null
}

export function buildQuery(params: Record<string, unknown>) {
  const query = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      query.set(key, String(value))
    }
  }
  const stringified = query.toString()
  return stringified ? `?${stringified}` : ''
}