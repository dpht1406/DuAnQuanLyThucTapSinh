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

export function getSafeReturnPath(raw) {
  const path = typeof raw === 'string' ? raw.split(/[?#]/, 1)[0] : ''

  if (
    typeof raw === 'string' &&
    raw.startsWith('/') &&
    !raw.startsWith('//') &&
    path !== '/change-password' &&
    path !== '/login'
  ) {
    return raw
  }

  return '/'
}

export async function completePasswordChange({ authStore, router, toast }) {
  authStore.logout()
  toast.add({
    severity: 'success',
    summary: 'Thành công',
    detail: 'Đổi mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới.',
    life: 3000
  })
  await router.replace('/login')
}
