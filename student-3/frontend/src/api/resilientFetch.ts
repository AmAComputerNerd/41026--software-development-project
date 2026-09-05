const TransientStatuses = new Set([408, 429, 502, 503, 504])

export async function resilientGet(
  input: string,
  { attempts = 3, timeoutMilliseconds = 8_000 }: {
    attempts?: number
    timeoutMilliseconds?: number
  } = {},
) {
  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    let retryAfterMilliseconds: number | null = null
    try {
      const response = await fetch(input, {
        method: 'GET',
        signal: AbortSignal.timeout(timeoutMilliseconds),
      })

      if (!TransientStatuses.has(response.status) || attempt === attempts) {
        return response
      }

      retryAfterMilliseconds = parseRetryAfter(response.headers.get('Retry-After'))
      await response.body?.cancel()
    } catch (reason) {
      if (attempt === attempts) {
        throw reason
      }
    }

    const delay = Math.min(retryAfterMilliseconds ?? attempt * 400, 2_000)
    await new Promise((resolve) => window.setTimeout(resolve, delay))
  }

  throw new Error('The request did not complete.')
}

function parseRetryAfter(value: string | null) {
  if (!value) return null

  const seconds = Number(value)
  if (Number.isFinite(seconds) && seconds >= 0) {
    return seconds * 1_000
  }

  const retryAt = Date.parse(value)
  return Number.isNaN(retryAt) ? null : Math.max(0, retryAt - Date.now())
}
