export const BASE_URL =
  import.meta.env.VITE_DEADLINES_API_BASE_URL || 'http://localhost:5103/api'

export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!response.ok) {
    const detail = await response.text()
    throw new Error(detail || `Deadline API request failed: ${response.status} ${response.statusText}`)
  }

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}
