export function defaultRouteForRole(role) {
  if (role === 'Admin') return '/dashboard'
  if (role === 'User') return '/profile'
  if (role === 'Company') return '/company-home'
  return null
}
