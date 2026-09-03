// No auth flow exists yet — every screen scopes to this
// user id until real login is wired up.
export const CURRENT_USER_ID =
  import.meta.env.VITE_USER_ID || '11111111-1111-1111-1111-111111111111'