// No auth flow exists yet — every screen scopes notifications to this
// student id until real login is wired up.
export const CURRENT_STUDENT_ID =
  import.meta.env.VITE_STUDENT_ID || '11111111-1111-1111-1111-111111111111'
