import { describe, expect, it } from 'vitest'
import { getPasswordChangeRedirect } from './authNavigation.js'

describe('getPasswordChangeRedirect', () => {
  it('redirects an authenticated user who must change their password', () => {
    expect(getPasswordChangeRedirect(true, true, '/dashboard')).toBe('/change-password')
  })

  it('allows the change-password page and unauthenticated routes', () => {
    expect(getPasswordChangeRedirect(true, true, '/change-password')).toBeNull()
    expect(getPasswordChangeRedirect(false, true, '/dashboard')).toBeNull()
  })
})
