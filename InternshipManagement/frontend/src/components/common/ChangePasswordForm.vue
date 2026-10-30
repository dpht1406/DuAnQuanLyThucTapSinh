<script setup>
import { computed, ref } from 'vue'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import FormField from '../form/FormField.vue'
import { useAuthStore } from '../../stores/auth.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateSafeText } from '../../utils/emailValidation.js'
import { validatePassword } from '../../utils/passwordPolicy.js'

const emit = defineEmits(['success'])
const authStore = useAuthStore()
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const loading = ref(false)
const errorMessage = ref('')

const canSubmit = computed(
  () =>
    currentPassword.value !== '' &&
    newPassword.value !== '' &&
    confirmPassword.value !== '' &&
    !loading.value
)

function clearError() {
  if (errorMessage.value) {
    errorMessage.value = ''
  }
}

function validateForm() {
  if (!canSubmit.value) return 'Vui lòng nhập đầy đủ thông tin.'
  for (const value of [currentPassword.value, newPassword.value, confirmPassword.value]) {
    const inputError = validateSafeText(value, 300)
    if (inputError) return inputError
  }
  const passwordError = validatePassword(newPassword.value)
  if (passwordError) return passwordError
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
  <form class="change-password-form" novalidate @submit.prevent="onSubmit">
    <Message
      v-if="errorMessage"
      severity="error"
      :closable="false"
      class="password-form-error"
    >
      {{ errorMessage }}
    </Message>

    <div class="fields-stack">
      <FormField
        label="Mật khẩu hiện tại"
        for="current-password"
      >
        <Password
          input-id="current-password"
          v-model="currentPassword"
          :feedback="false"
          toggle-mask
          autocomplete="current-password"
          :pt="{ input: { maxlength: 300 } }"
          fluid
          input-class="w-full"
          class="w-full"
          :disabled="loading"
          placeholder="Nhập mật khẩu hiện tại"
          @update:model-value="clearError"
        />
      </FormField>

      <FormField
        label="Mật khẩu mới"
        for="new-password"
        hint="Từ 8 đến 128 ký tự, gồm chữ và số, khác mật khẩu hiện tại"
      >
        <Password
          input-id="new-password"
          v-model="newPassword"
          :feedback="false"
          toggle-mask
          autocomplete="new-password"
          :pt="{ input: { maxlength: 300 } }"
          fluid
          input-class="w-full"
          class="w-full"
          :disabled="loading"
          placeholder="Nhập mật khẩu mới"
          @update:model-value="clearError"
        />
      </FormField>

      <FormField
        label="Xác nhận mật khẩu mới"
        for="confirm-password"
      >
        <Password
          input-id="confirm-password"
          v-model="confirmPassword"
          :feedback="false"
          toggle-mask
          autocomplete="new-password"
          :pt="{ input: { maxlength: 300 } }"
          fluid
          input-class="w-full"
          class="w-full"
          :disabled="loading"
          placeholder="Nhập lại mật khẩu mới"
          @update:model-value="clearError"
        />
      </FormField>
    </div>

    <div class="form-actions">
      <Button
        type="submit"
        label="Đổi mật khẩu"
        class="submit-button"
        :loading="loading"
        :disabled="!canSubmit"
      />
    </div>
  </form>
</template>

<style scoped>
.change-password-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  width: 100%;
}

.password-form-error {
  margin: 0;
  word-break: break-word;
  overflow-wrap: anywhere;
}

.fields-stack {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  width: 100%;
}

.w-full {
  width: 100%;
}

:deep(.p-password) {
  display: flex;
  width: 100%;
}

:deep(.p-password input) {
  width: 100%;
  font-family: var(--ui-font-sans);
  font-size: 14px;
  line-height: 1.5;
  color: var(--ui-ink);
  background: var(--ui-surface);
  border: 1px solid var(--ui-input-border);
  border-radius: var(--ui-radius-sm);
  padding: 0.625rem 2.5rem 0.625rem 0.875rem;
  min-height: 42px;
  box-sizing: border-box;
  transition: var(--ui-transition);
}

:deep(.p-password input::placeholder) {
  color: var(--ui-placeholder);
  font-size: 13.5px;
}

:deep(.p-password input:hover:not(:disabled)) {
  border-color: var(--ui-input-border);
}

:deep(.p-password input:focus) {
  border-color: var(--ui-primary-600);
  outline: 2px solid var(--ui-primary-600);
  outline-offset: 1px;
}

:deep(.p-password input:disabled) {
  background: var(--ui-surface-sunken);
  color: var(--ui-muted);
  cursor: not-allowed;
  opacity: 0.75;
}

:deep([data-pc-section="maskicon"]),
:deep([data-pc-section="unmaskicon"]),
:deep(.p-password-toggle-mask-icon) {
  color: var(--ui-muted);
  right: 0.875rem;
  cursor: pointer;
  padding: 4px;
  transition: color 120ms ease;
}

:deep([data-pc-section="maskicon"]:hover),
:deep([data-pc-section="unmaskicon"]:hover),
:deep(.p-password-toggle-mask-icon:hover) {
  color: var(--ui-ink);
}

.form-actions {
  display: flex;
  flex-direction: column;
  padding-top: 0.5rem;
}

.submit-button {
  width: 100%;
  min-height: 44px;
  font-family: var(--ui-font-sans);
  font-size: 14.5px;
  font-weight: 600;
  border-radius: var(--ui-radius-sm);
  background: var(--ui-primary-600);
  border: 1px solid var(--ui-primary-600);
  color: #ffffff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  cursor: pointer;
  transition: var(--ui-transition);
  box-shadow: none;
}

.submit-button:hover:not(:disabled) {
  background: var(--ui-primary-700);
  border-color: var(--ui-primary-700);
}

.submit-button:focus-visible {
  outline: 2px solid var(--ui-primary-600);
  outline-offset: 2px;
}

.submit-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 640px) {
  .change-password-form {
    gap: 1.25rem;
  }

  .fields-stack {
    gap: 1.125rem;
  }

  .submit-button {
    min-height: 48px;
    font-size: 15px;
  }
}
</style>