// No auth flow exists yet — every screen scopes notifications to this
// student id until real login is wired up.
export const CURRENT_STUDENT_ID =
  import.meta.env.VITE_STUDENT_ID || '00000000-0000-0000-0000-000000000001'
