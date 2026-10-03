<script setup>
import { computed, ref } from 'vue'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { useAuthStore } from '../../stores/auth.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateSafeText } from '../../utils/emailValidation.js'

const emit = defineEmits(['success'])
const authStore = useAuthStore()
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const loading = ref(false)
const errorMessage = ref('')

const canSubmit = computed(() => currentPassword.value !== '' && newPassword.value !== '' && confirmPassword.value !== '' && !loading.value)

function validateForm() {
  if (!canSubmit.value) return 'Vui lòng nhập đầy đủ thông tin.'
  for (const value of [currentPassword.value, newPassword.value, confirmPassword.value]) {
    const inputError = validateSafeText(value, 300)
    if (inputError) return inputError
  }
  if (newPassword.value.length < 6) return 'Mật khẩu mới phải có ít nhất 6 ký tự.'
  if (newPassword.value === currentPassword.value) return 'Mật khẩu mới phải khác mật khẩu hiện tại.'
  if (newPassword.value !== confirmPassword.value) return 'Xác nhận mật khẩu mới không khớp.'
  return ''
}

async function onSubmit() {
  if (!canSubmit.value) return
  errorMessage.value = validateForm()
  if (errorMessage.value) return

  loading.value = true
  try {
    await authStore.changePassword(currentPassword.value, newPassword.value)
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
    emit('success')
  } catch (err) {
    errorMessage.value = extractErrorMessage(err, 'Đổi mật khẩu thất bại, vui lòng thử lại.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <form class="change-password-form" @submit.prevent="onSubmit">
    <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>
    <div class="field">
      <label for="current-password">Mật khẩu hiện tại</label>
      <Password id="current-password" v-model="currentPassword" :feedback="false" toggle-mask autocomplete="current-password" :pt="{ input: { maxlength: 300 } }" input-class="w-full" class="w-full" :disabled="loading" />
    </div>
    <div class="field">
      <label for="new-password">Mật khẩu mới</label>
      <Password id="new-password" v-model="newPassword" :feedback="false" toggle-mask autocomplete="new-password" :pt="{ input: { maxlength: 300 } }" input-class="w-full" class="w-full" :disabled="loading" />
    </div>
    <div class="field">
      <label for="confirm-password">Xác nhận mật khẩu mới</label>
      <Password id="confirm-password" v-model="confirmPassword" :feedback="false" toggle-mask autocomplete="new-password" :pt="{ input: { maxlength: 300 } }" input-class="w-full" class="w-full" :disabled="loading" />
    </div>
    <Button type="submit" label="Đổi mật khẩu" class="w-full" :loading="loading" :disabled="!canSubmit" />
  </form>
</template>

<style scoped>
.change-password-form { display: flex; flex-direction: column; gap: 1rem; }
.field { display: flex; flex-direction: column; gap: .35rem; }
.field label { font-weight: 500; }
.w-full { width: 100%; }
</style>