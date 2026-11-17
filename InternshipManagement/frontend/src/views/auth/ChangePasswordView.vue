<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useAuthStore } from '../../stores/auth.js'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import ChangePasswordForm from '../../components/common/ChangePasswordForm.vue'
import { completePasswordChange, getSafeReturnPath } from '../../utils/authNavigation.js'

const router = useRouter()
const route = useRoute()
const toast = useToast()
const authStore = useAuthStore()
const returnPath = computed(() => getSafeReturnPath(route.query.from))
const pageSubtitle = computed(() =>
  authStore.mustChangePassword
    ? 'Bạn cần cập nhật mật khẩu mới để tiếp tục sử dụng hệ thống.'
    : 'Cập nhật mật khẩu để bảo vệ tài khoản của bạn.'
)

async function onSuccess() {
  await completePasswordChange({ authStore, toast, router })
}
</script>

<template>
  <FormPage
    class="change-password-page"
    title="Đổi mật khẩu"
    :subtitle="pageSubtitle"
    :back-label="authStore.mustChangePassword ? '' : 'Quay lại'"
    :back-to="authStore.mustChangePassword ? null : returnPath"
  >
    <FormSection
      title="Thông tin mật khẩu"
      description="Nhập mật khẩu hiện tại và thiết lập mật khẩu mới (từ 8 đến 128 ký tự, gồm chữ và số)."
    >
      <ChangePasswordForm @success="onSuccess" />
    </FormSection>
  </FormPage>
</template>

<style scoped>
.change-password-page :deep(.form-page-container) {
  max-width: 520px;
}

.change-password-page :deep(.form-section) {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  padding: 0;
  border-top: none;
}

.change-password-page :deep(.form-section-header) {
  gap: 4px;
}

.change-password-page :deep(.form-section-title) {
  font-family: var(--ui-font-sans);
  font-size: 15px;
  font-weight: 600;
  line-height: 1.4;
  color: var(--ui-ink);
}

.change-password-page :deep(.form-section-description) {
  font-family: var(--ui-font-sans);
  font-size: 13px;
  line-height: 1.5;
  color: var(--ui-muted);
}

@media (max-width: 640px) {
  .change-password-page :deep(.form-page-container) {
    padding: 1.25rem 1rem 2.5rem;
  }
}
</style>
