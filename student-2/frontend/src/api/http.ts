export const BASE_URL =
  import.meta.env.VITE_AUTOMATIONS_API_BASE_URL || 'http://localhost:5102/api'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

async function getErrorMessage(response: Response): Promise<string> {
  const body = await response.text()

  if (body) {
    try {
      const parsed: unknown = JSON.parse(body)
      const message = getJsonErrorMessage(parsed)
      if (message) return message
    } catch {
      if (isSafePlainText(body)) return body.trim()
    }
  }

  return getStatusMessage(response.status)
}

function getJsonErrorMessage(value: unknown): string | null {
  if (typeof value === 'string') return value.trim() || null
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null

  const problem = value as ProblemDetails
  const validationMessages = problem.errors
    ? Object.values(problem.errors).flat().filter(Boolean)
    : []

  if (validationMessages.length) return validationMessages.join(' ')
  if (typeof problem.detail === 'string' && problem.detail.trim()) return problem.detail.trim()
  if (typeof problem.title === 'string' && problem.title.trim()) return problem.title.trim()
  return null
}

function isSafePlainText(value: string): boolean {
  const text = value.trim()
  return text.length <= 300 &&
    !text.includes('\n') &&
    !text.startsWith('<') &&
    !text.includes('Exception:') &&
    !text.includes(' at ')
}

function getStatusMessage(status: number): string {
  if (status === 400) return 'The submitted information is invalid.'
  if (status === 401) return 'You need to sign in before continuing.'
  if (status === 403) return 'You do not have permission to perform this action.'
  if (status === 404) return 'The requested information could not be found.'
  if (status === 409) return 'The request conflicts with the current data.'
  if (status === 429) return 'Too many requests were made. Try again shortly.'
  if (status >= 500) return 'The service is temporarily unavailable.'
  return 'The request could not be completed.'
}