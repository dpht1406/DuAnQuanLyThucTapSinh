// Giải mã phần payload của JWT (base64url) thành object JS thuần.
// KHÔNG xác thực chữ ký — chỉ dùng để đọc claim ở phía client cho mục đích hiển thị/điều
// hướng UI. Server (JwtBearer middleware) luôn là nơi xác thực token thật sự.
export function decodeJwt(token) {
  try {
    if (!token || typeof token !== 'string') return null

    const parts = token.split('.')
    if (parts.length !== 3) return null

    const payloadSegment = parts[1]
    // base64url -> base64 chuẩn, rồi bù padding '=' cho đủ bội số 4
    const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
    const padLength = (4 - (base64.length % 4)) % 4
    const padded = base64 + '='.repeat(padLength)

    const binary = atob(padded)
    const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0))
    const json = new TextDecoder('utf-8').decode(bytes)

    return JSON.parse(json)
  } catch {
    return null
  }
}
