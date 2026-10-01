import axios from 'axios'
import { useAuthStore } from '../stores/auth.js'

// Instance chính dùng cho toàn bộ API nghiệp vụ (students, companies, ...).
// KHÔNG dùng instance này để gọi /api/auth/refresh — logic refresh nằm trong
// authStore.refreshAccessToken(), dùng một axios instance thô riêng (xem src/stores/auth.js)
// để tránh việc response interceptor bên dưới tự gọi lại chính nó.
const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// ---------- request interceptor: gắn Bearer token ----------
axiosClient.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.accessToken) {
    config.headers = config.headers || {}
    config.headers.Authorization = `Bearer ${authStore.accessToken}`
  }
  return config
})

// ---------- response interceptor: tự refresh khi gặp 401 ----------

// Nhiều request cùng lúc bị 401 chỉ nên kích hoạt 1 lần gọi refresh — các request còn lại
// "ăn theo" cùng 1 promise này thay vì mỗi request tự gọi refresh riêng.
let refreshPromise = null
let loginRedirectInProgress = false

async function redirectToLoginOnce() {
  if (loginRedirectInProgress) return

  loginRedirectInProgress = true
  try {
    const { default: router } = await import('../router')
    if (router.currentRoute.value.path !== '/login') {
      await router.replace('/login')
    }
  } catch (error) {
    console.error('Không thể điều hướng về trang đăng nhập:', error)
  } finally {
    loginRedirectInProgress = false
  }
}

axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    const status = error.response?.status

    // Không có config (lỗi network trước khi request được gửi) hoặc không phải 401,
    // hoặc request này đã retry rồi -> trả lỗi ra ngoài như bình thường.
    if (status !== 401 || !originalRequest || originalRequest._retry) {
      return Promise.reject(error)
    }

    // Chính request refresh (nếu lỡ đi qua axiosClient) bị 401 thì không refresh nữa,
    // tránh vòng lặp vô hạn.
    if (originalRequest.url?.includes('/auth/refresh')) {
      return Promise.reject(error)
    }

    originalRequest._retry = true
    const authStore = useAuthStore()

    try {
      if (!refreshPromise) {
        refreshPromise = authStore.refreshAccessToken().finally(() => {
          refreshPromise = null
        })
      }

      const newAccessToken = await refreshPromise

      originalRequest.headers = originalRequest.headers || {}
      originalRequest.headers.Authorization = `Bearer ${newAccessToken}`

      return axiosClient(originalRequest)
    } catch (refreshError) {
      authStore.logout()
      refreshError.authRefreshFailed = true
      await redirectToLoginOnce()
      return Promise.reject(refreshError)
    }
  }
)

export default axiosClient
