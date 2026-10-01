import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'
import { decodeJwt } from '../utils/jwt.js'

// Instance axios "thô" riêng cho store: KHÔNG dùng chung với axiosClient chính (src/api/axiosClient.js)
// vì axiosClient gắn interceptor 401 -> gọi lại các action của store này (refreshAccessToken).
// Nếu store lại gọi ngược qua axiosClient sẽ tạo vòng lặp interceptor.
const authApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 15000
})

const ACCESS_TOKEN_KEY = 'accessToken'
const REFRESH_TOKEN_KEY = 'refreshToken'

// Chuyển payload JWT đã decode thành object user gọn cho UI dùng.
// role trong claim có thể là 1 chuỗi hoặc mảng (do backend gắn cả ClaimTypes.Role lẫn "role"
// cùng ánh xạ ra key "role"), nên chuẩn hoá về 1 chuỗi.
function buildUserFromPayload(payload) {
  if (!payload) return null

  const rawRole = payload.role
  const role = Array.isArray(rawRole) ? rawRole[0] : rawRole

  const rawUserId = payload.nameid ?? payload.sub ?? null
  const rawStudentId = payload.StudentId ?? null

  return {
    userId: rawUserId !== null ? Number(rawUserId) : null,
    username: payload.unique_name ?? null,
    role: role ?? null,
    studentId: rawStudentId !== null ? Number(rawStudentId) : null
  }
}

function decodeUserFromToken(token) {
  const payload = decodeJwt(token)
  return buildUserFromPayload(payload)
}

export const useAuthStore = defineStore('auth', () => {
  // ---------- state ----------
  const accessToken = ref(localStorage.getItem(ACCESS_TOKEN_KEY) || null)
  const refreshToken = ref(localStorage.getItem(REFRESH_TOKEN_KEY) || null)
  const mustChangePassword = ref(false)
  const user = ref(decodeUserFromToken(accessToken.value))
  const ready = ref(false)

  // ---------- getters ----------
  const isAuthenticated = computed(() => !!accessToken.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')

  // ---------- actions ----------

  // Cập nhật token mới vào state + đồng bộ localStorage. Dùng chung cho login/refresh.
  function setTokens(newAccessToken, newRefreshToken) {
    accessToken.value = newAccessToken || null
    refreshToken.value = newRefreshToken || null
    user.value = decodeUserFromToken(accessToken.value)

    if (accessToken.value) {
      localStorage.setItem(ACCESS_TOKEN_KEY, accessToken.value)
    } else {
      localStorage.removeItem(ACCESS_TOKEN_KEY)
    }

    if (refreshToken.value) {
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken.value)
    } else {
      localStorage.removeItem(REFRESH_TOKEN_KEY)
    }
  }

  async function login(username, password) {
    const res = await authApi.post('/auth/login', { username, password })
    const body = res.data
    if (!body?.success || !body?.data) {
      throw new Error(body?.message || 'Đăng nhập thất bại.')
    }

    const { accessToken: newAccessToken, refreshToken: newRefreshToken, mustChangePassword: mustChange } = body.data
    setTokens(newAccessToken, newRefreshToken)
    mustChangePassword.value = !!mustChange

    try {
      await fetchMe({ logoutOnError: false })
    } catch {
      // Giữ user decode từ JWT nếu /auth/me thất bại sau login thành công.
    }

    return body.data
  }

  async function fetchMe({ logoutOnError = true } = {}) {
    if (!accessToken.value) {
      return
    }

    const fail = () => {
      if (logoutOnError) {
        logout()
      }
    }

    try {
      const { default: axiosClient } = await import('../api/axiosClient.js')
      const res = await axiosClient.get('/auth/me')
      const body = res.data
      if (body?.success === false) {
        fail()
        return
      }
      if (!body?.data) return

      const data = body.data
      user.value = {
        userId: data.userId,
        username: data.username,
        role: data.role,
        studentId: data.studentId ?? null
      }
      mustChangePassword.value = !!data.mustChangePassword
    } catch (error) {
      if (logoutOnError && error?.response?.status === 401 && error?.authRefreshFailed) {
        logout()
      }
    }
  }

  // Chỉ xoá state cục bộ + localStorage. KHÔNG gọi API /api/auth/logout ở đây vì endpoint đó
  // cần refreshToken hợp lệ trong body — để component tự gọi API logout riêng (nếu cần thu hồi
  // refresh token phía server) trước khi gọi hàm này.
  function logout() {
    setTokens(null, null)
    mustChangePassword.value = false
  }

  // Dùng nội bộ bởi response interceptor của axiosClient khi gặp lỗi 401.
  // Trả về accessToken mới nếu thành công; ném lỗi nếu refresh thất bại.
  async function refreshAccessToken() {
    if (!refreshToken.value) {
      throw new Error('Không có refresh token để làm mới.')
    }

    const res = await authApi.post('/auth/refresh', { refreshToken: refreshToken.value })
    const body = res.data
    if (!body?.success || !body?.data) {
      throw new Error(body?.message || 'Làm mới token thất bại.')
    }

    const { accessToken: newAccessToken, refreshToken: newRefreshToken, mustChangePassword: mustChange } = body.data
    setTokens(newAccessToken, newRefreshToken)
    mustChangePassword.value = !!mustChange

    return newAccessToken
  }

  async function changePassword(currentPassword, newPassword) {
    const res = await authApi.post(
      '/auth/change-password',
      { currentPassword, newPassword },
      {
        headers: accessToken.value ? { Authorization: `Bearer ${accessToken.value}` } : {}
      }
    )

    const body = res.data
    if (!body?.success) {
      throw new Error(body?.message || 'Đổi mật khẩu thất bại.')
    }

    mustChangePassword.value = false
    return body
  }

  return {
    // state
    accessToken,
    refreshToken,
    mustChangePassword,
    user,
    ready,
    // getters
    isAuthenticated,
    isAdmin,
    // actions
    login,
    logout,
    refreshAccessToken,
    changePassword,
    setTokens,
    fetchMe
  }
})
