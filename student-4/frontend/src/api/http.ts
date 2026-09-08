export const BASE_URL = import.meta.env.VITE_ACCOUNT_API_BASE_URL || '/api'

export class ApiError extends Error {
  status: number
  statusText: string
  body: string

  constructor(status: number, statusText: string, body: string) {
    const message = formatErrorMessage(status, statusText, body)
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.statusText = statusText
    this.body = body
  }
}

function formatErrorMessage(status: number, statusText: string, body: string): string {
  if (!body) {
    return `Account API request failed: ${status} ${statusText}`
  }
  try {
    const parsed = JSON.parse(body) as { error?: string; title?: string; detail?: string }
    if (typeof parsed.error === 'string') {
      return parsed.error
    }
    if (typeof parsed.title === 'string') {
      return parsed.title
    }
    if (typeof parsed.detail === 'string') {
      return parsed.detail
    }
  } catch {
    // body not JSON
  }
  return `Account API request failed: ${status} ${statusText} - ${body}`
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