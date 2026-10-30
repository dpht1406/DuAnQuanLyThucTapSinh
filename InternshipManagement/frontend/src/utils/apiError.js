export function extractErrorMessage(err, fallback = 'Có lỗi xảy ra, vui lòng thử lại.') {
  const status = err?.response?.status
  if (
    !err?.response ||
    ['ECONNABORTED', 'ETIMEDOUT', 'ERR_NETWORK'].includes(err?.code) ||
    err?.isNetworkError
  ) {
    return 'Không kết nối được máy chủ. Vui lòng kiểm tra mạng hoặc thử lại sau.'
  }
  if (status >= 500) {
    return 'Hệ thống đang gặp sự cố. Vui lòng thử lại sau.'
  }

  const data = err?.response?.data
  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors).flat()
    if (messages.length) return messages.join(' ')
  }
  if (data?.message) return data.message
  return fallback
}