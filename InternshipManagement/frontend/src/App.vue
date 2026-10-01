<script setup>
import { onErrorCaptured, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import Button from 'primevue/button'
import ConfirmDialog from 'primevue/confirmdialog'
import ProgressSpinner from 'primevue/progressspinner'
import Toast from 'primevue/toast'
import { useAuthStore } from './stores/auth.js'

const authStore = useAuthStore()
const route = useRoute()
const hasRenderError = ref(false)

watch(() => route.fullPath, () => {
  hasRenderError.value = false
})

onErrorCaptured((error, _instance, info) => {
  console.error('Lỗi khi render trang:', error, info)
  hasRenderError.value = true
  return false
})

function reloadPage() {
  window.location.reload()
}
</script>

<template>
  <main v-if="!authStore.ready" class="app-state app-loading" role="status">
    <ProgressSpinner aria-label="Đang tải" />
    <span>Đang tải...</span>
  </main>
  <main v-else-if="hasRenderError" class="app-state app-error" role="alert">
    <p>Đã có lỗi xảy ra</p>
    <Button label="Tải lại trang" icon="pi pi-refresh" @click="reloadPage" />
  </main>
  <router-view v-else />
  <ConfirmDialog />
  <Toast />
</template>

<style scoped>
.app-state {
  align-items: center;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  justify-content: center;
  min-height: 100vh;
  padding: 1.5rem;
  text-align: center;
}

.app-error p {
  margin: 0;
}
</style>
