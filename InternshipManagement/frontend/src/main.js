import { createApp } from 'vue'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import { definePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'
import '@fontsource/inter/400.css'
import '@fontsource/inter/500.css'
import '@fontsource/inter/600.css'
import '@fontsource/inter/700.css'
import 'primeicons/primeicons.css'
import './style.css'
import App from './App.vue'
import { createPinia } from 'pinia'
import router from './router'
import { useAuthStore } from './stores/auth.js'

const app = createApp(App)

const QlttPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{indigo.50}', 100: '{indigo.100}', 200: '{indigo.200}', 300: '{indigo.300}',
      400: '{indigo.400}', 500: '#6366F1', 600: '{indigo.600}', 700: '{indigo.700}',
      800: '{indigo.800}', 900: '{indigo.900}', 950: '{indigo.950}'
    }
  },
  components: {
    card: { root: { borderRadius: '14px', boxShadow: 'none' } },
    button: { border: { radius: '8px' } }
  }
})

app.use(PrimeVue, {
  theme: {
    preset: QlttPreset,
    options: { darkModeSelector: '.app-dark' }
  }
})
app.use(ConfirmationService)
app.use(ToastService)
const pinia = createPinia()
app.use(pinia)
app.use(router)

app.config.errorHandler = (error, _instance, info) => {
  console.error('Lỗi khi render ứng dụng:', error, info)
  try {
    app.config.globalProperties.$toast?.add({
      severity: 'error',
      summary: 'Đã có lỗi xảy ra',
      detail: 'Vui lòng tải lại trang hoặc thử lại sau.',
      life: 5000
    })
  } catch {
    // Toast có thể chưa sẵn sàng khi ứng dụng đang khởi tạo.
  }
}

if (import.meta.env.DEV && !import.meta.env.VITE_API_BASE_URL) {
  console.warn('VITE_API_BASE_URL đang rỗng hoặc chưa được cấu hình; các yêu cầu API có thể thất bại.')
}

const authStore = useAuthStore(pinia)
app.mount('#app')

const readinessTimer = window.setTimeout(() => {
  authStore.ready = true
}, 10000)

void (async () => {
  try {
    await authStore.fetchMe()
  } catch (error) {
    console.error('Không thể tải thông tin người dùng:', error)
  } finally {
    window.clearTimeout(readinessTimer)
    authStore.ready = true
  }
})()
