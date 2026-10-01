import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  optimizeDeps: {
    include: [
      'vue',
      'vue-router',
      'pinia',
      'axios',
      'chart.js',
      'vue-chartjs',
      'dayjs',
      'dayjs/plugin/relativeTime',
      'dayjs/locale/vi',
      '@primeuix/themes',
      '@primeuix/themes/aura',
      'primevue/autocomplete',
      'primevue/avatar',
      'primevue/badge',
      'primevue/button',
      'primevue/card',
      'primevue/checkbox',
      'primevue/column',
      'primevue/config',
      'primevue/confirmationservice',
      'primevue/confirmdialog',
      'primevue/datatable',
      'primevue/dialog',
      'primevue/drawer',
      'primevue/fileupload',
      'primevue/iconfield',
      'primevue/inputicon',
      'primevue/inputnumber',
      'primevue/inputswitch',
      'primevue/inputtext',
      'primevue/menu',
      'primevue/message',
      'primevue/paginator',
      'primevue/password',
      'primevue/popover',
      'primevue/progressbar',
      'primevue/select',
      'primevue/tabpanel',
      'primevue/tabview',
      'primevue/tag',
      'primevue/textarea',
      'primevue/toast',
      'primevue/toastservice',
      'primevue/useconfirm',
      'primevue/usetoast',
      'primevue/progressspinner',
      'primevue/selectbutton',
      'primevue/skeleton'
    ]
  },
  server: {
    host: 'localhost',
    port: 5173,
    strictPort: false,
    warmup: {
      clientFiles: ['./src/main.js', './src/App.vue', './src/views/**/*.vue', './src/layouts/*.vue']
    }
  }
})
