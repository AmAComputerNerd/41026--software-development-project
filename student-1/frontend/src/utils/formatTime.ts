export function formatRelativeTime(isoDate: string): string {
  // The backend serializes `DateTime` values as UTC but without a `Z` or
  // offset suffix, which `Date` would otherwise parse as local time.
  const hasTimezone = /[zZ]|[+-]\d\d:\d\d$/.test(isoDate)
  const date = new Date(hasTimezone ? isoDate : `${isoDate}Z`)
  const diffMs = Date.now() - date.getTime()
  const diffMin = Math.round(diffMs / 60_000)

  if (diffMin < 1) return 'NOW'
  if (diffMin < 60) return `${diffMin}M AGO`

  const diffHr = Math.round(diffMin / 60)
  if (diffHr < 24) return `${diffHr}H AGO`

  const diffDay = Math.round(diffHr / 24)
  if (diffDay < 7) return `${diffDay}D AGO`

  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }).toUpperCase()
}
