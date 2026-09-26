export const BASE_URL =
  import.meta.env.VITE_DEADLINES_API_BASE_URL || 'http://localhost:5103/api'

export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...options,
    })
  } catch {
    throw new Error('The Deadline Tracker API could not be reached. Please try again.')
  }

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

async function readErrorMessage(response: Response): Promise<string> {
  const fallback = `The request failed (${response.status} ${response.statusText}).`
  const text = await response.text()
  if (!text.trim()) return fallback

  let payload: unknown
  try {
    payload = JSON.parse(text)
  } catch {
    return response.headers.get('Content-Type')?.includes('text/plain') ? text.trim() : fallback
  }

  if (typeof payload === 'string' && payload.trim()) return payload.trim()
  if (!isRecord(payload)) return fallback

  const directMessage = firstString(payload.detail, payload.title, payload.message)
  if (directMessage) return directMessage

  if (isRecord(payload.error)) {
    const nestedMessage = firstString(payload.error.message)
    if (nestedMessage) return nestedMessage
  }

  if (isRecord(payload.errors)) {
    for (const value of Object.values(payload.errors)) {
      const validationMessage = Array.isArray(value)
        ? firstString(...value)
        : firstString(value)
      if (validationMessage) return validationMessage
    }
  }

  return fallback
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function firstString(...values: unknown[]): string | null {
  const value = values.find((candidate) => typeof candidate === 'string' && candidate.trim())
  return typeof value === 'string' ? value.trim() : null
}
