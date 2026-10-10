import { describe, expect, it } from 'vitest'
import { defaultRouteForRole } from './roleRoutes.js'

describe('defaultRouteForRole', () => {
  it.each([
    ['Admin', '/dashboard'],
    ['User', '/profile'],
    ['Company', '/company-home']
  ])('returns the default route for %s', (role, expectedRoute) => {
    expect(defaultRouteForRole(role)).toBe(expectedRoute)
  })

  it.each(['Unknown', null, undefined])('returns null for unsupported role %s', (role) => {
    expect(defaultRouteForRole(role)).toBeNull()
  })
})
