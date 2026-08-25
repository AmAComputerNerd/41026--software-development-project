// No auth flow exists yet — every screen scopes to this student id until
// real login is wired up.
export const CURRENT_STUDENT_ID =
  import.meta.env.VITE_STUDENT_ID || '11111111-1111-1111-1111-111111111111'

export const NOTIFICATIONS_API_BASE_URL =
  import.meta.env.VITE_NOTIFICATIONS_API_BASE_URL || 'http://localhost:5101'

export const DEADLINES_API_BASE_URL =
  import.meta.env.VITE_DEADLINES_API_BASE_URL || 'http://localhost:5103/api'
