<script setup>
import { useRouter } from 'vue-router'
import Card from 'primevue/card'
import { useToast } from 'primevue/usetoast'
import ChangePasswordForm from '../../components/common/ChangePasswordForm.vue'

const router = useRouter()

let toast = null
try { toast = useToast() } catch { }

async function onSuccess() {
  toast?.add({ severity: 'success', summary: 'Thành công', detail: 'Đổi mật khẩu thành công.', life: 3000 })
  await router.push('/')
}
</script>

<template>
  <div class="page-container change-password-page">
  <Card class="change-password-card">
    <template #title><span class="page-title">Đổi mật khẩu</span></template>
    <template #content>
      <ChangePasswordForm @success="onSuccess" />
    </template>
  </Card>
  </div>
</template>

<style scoped>
.change-password-card {
  width: 100%;
  max-width: 32rem;
}

.change-password-page {
  display: flex;
  justify-content: center;
}

</style>
