export function getPasswordChangeRedirect(isAuthenticated, mustChangePassword, path) {
  if (
    isAuthenticated &&
    mustChangePassword &&
    path !== '/login' &&
    path !== '/change-password'
  ) {
    return '/change-password'
  }

  return null
}
