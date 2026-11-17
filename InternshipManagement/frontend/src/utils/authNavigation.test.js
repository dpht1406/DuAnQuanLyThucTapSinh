import { describe, expect, it } from 'vitest'
import { completePasswordChange, getPasswordChangeRedirect, getSafeReturnPath } from './authNavigation.js'

describe('getPasswordChangeRedirect', () => {
  it('redirects an authenticated user who must change their password', () => {
    expect(getPasswordChangeRedirect(true, true, '/dashboard')).toBe('/change-password')
  })

  it('allows the change-password page and unauthenticated routes', () => {
    expect(getPasswordChangeRedirect(true, true, '/change-password')).toBeNull()
    expect(getPasswordChangeRedirect(false, true, '/dashboard')).toBeNull()
  })
})

describe('getSafeReturnPath', () => {
  it('returns a safe internal path', () => {
    expect(getSafeReturnPath('/students?status=2')).toBe('/students?status=2')
  })

  describe('completePasswordChange', () => {
    it('logs out, shows a localized message, and navigates to login', async () => {
      const calls = []
      await completePasswordChange({
        authStore: { logout: () => calls.push('logout') },
        toast: { add: (message) => calls.push(message.detail) },
        router: { replace: (path) => calls.push(path) }
      })

      expect(calls).toEqual([
        'logout',
        'Đổi mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới.',
        '/login'
      ])
    })
  })

  it.each(['//evil.com', 'http://x', '', undefined, '/login'])(
    'falls back for unsafe return path %s',
    (raw) => {
      expect(getSafeReturnPath(raw)).toBe('/')
    }
  )

  it('does not return the change-password route', () => {
    expect(getSafeReturnPath('/change-password')).toBe('/')
  })
})
