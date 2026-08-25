export function toDateInput(value: string | null) {
  if (!value) return ''
  const date = new Date(value)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

export function toUtcIso(value: string) {
  return value ? new Date(value).toISOString() : null
}

export function formatDueDate(value: string | null) {
  if (!value) return 'No due date'
  return new Intl.DateTimeFormat('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(value))
}

export function dueLabel(value: string | null, now = new Date()) {
  if (!value) return 'NO DUE DATE'
  const due = new Date(value)
  const start = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime()
  const end = new Date(due.getFullYear(), due.getMonth(), due.getDate()).getTime()
  const days = Math.round((end - start) / 86_400_000)
  if (days < 0) return `${Math.abs(days)}D OVERDUE`
  if (days === 0) return 'DUE TODAY'
  if (days === 1) return 'DUE TOMORROW'
  return `DUE IN ${days}D`
}
