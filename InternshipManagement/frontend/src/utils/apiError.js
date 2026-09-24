export function extractErrorMessage(err, fallback = 'Có lỗi xảy ra, vui lòng thử lại.') {
  const data = err?.response?.data
  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors).flat()
    if (messages.length) return messages.join(' ')
  }
  if (data?.message) return data.message
  if (err?.message) return err.message
  return fallback
}