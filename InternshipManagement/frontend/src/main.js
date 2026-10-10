import { createApp } from 'vue'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import { definePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'
import '@fontsource/newsreader/500.css'
import '@fontsource/newsreader/600.css'
import '@fontsource/be-vietnam-pro/400.css'
import '@fontsource/be-vietnam-pro/500.css'
import '@fontsource/be-vietnam-pro/600.css'
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
      50: '#EEF6F2',
      100: '#D6EAE0',
      200: '#AED5C2',
      300: '#80BAA1',
      400: '#4F9A7D',
      500: '#2E7D63',
      600: '#1F6650',
      700: '#19523F',
      800: '#153F32',
      900: '#102F26',
      950: '#081B15'
    },
    formField: {
      borderRadius: '6px'
    },
    colorScheme: {
      light: {
        surface: {
          0: '#ffffff',
          50: '{stone.50}',
          100: '{stone.100}',
          200: '{stone.200}',
          300: '{stone.300}',
          400: '{stone.400}',
          500: '{stone.500}',
          600: '{stone.600}',
          700: '{stone.700}',
          800: '{stone.800}',
          900: '{stone.900}',
          950: '{stone.950}'
        }
      }
    }
  },
  components: {
    card: { root: { borderRadius: '10px', boxShadow: 'none' } },
    button: { root: { borderRadius: '6px' } }
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
