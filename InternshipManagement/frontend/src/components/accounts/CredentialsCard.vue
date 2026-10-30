<script setup>
import { onBeforeUnmount, ref } from 'vue'
import Button from 'primevue/button'
import { useToast } from 'primevue/usetoast'

defineProps({
  usernameLabel: {
    type: String,
    default: 'Tên đăng nhập'
  },
  username: {
    type: String,
    default: ''
  },
  passwordLabel: {
    type: String,
    default: 'Mật khẩu tạm'
  },
  password: {
    type: String,
    default: ''
  }
})

let toast = null
try {
  toast = useToast()
} catch {
  // Bỏ qua khi useToast không khả dụng trong ngữ cảnh test
}

const copiedUsername = ref(false)
const copiedPassword = ref(false)
let usernameTimer = null
let passwordTimer = null

async function copyText(text, isPassword = true) {
  if (!text) return
  try {
    await navigator.clipboard.writeText(text)
    if (isPassword) {
      copiedPassword.value = true
      if (passwordTimer) window.clearTimeout(passwordTimer)
      passwordTimer = window.setTimeout(() => {
        copiedPassword.value = false
        passwordTimer = null
      }, 2000)
    } else {
      copiedUsername.value = true
      if (usernameTimer) window.clearTimeout(usernameTimer)
      usernameTimer = window.setTimeout(() => {
        copiedUsername.value = false
        usernameTimer = null
      }, 2000)
    }
  } catch {
    toast?.add({
      severity: 'warn',
      summary: 'Không thể sao chép',
      detail: isPassword
        ? 'Vui lòng bôi đen mật khẩu để sao chép thủ công.'
        : 'Vui lòng bôi đen để sao chép thủ công.',
      life: 4000
    })
  }
}

onBeforeUnmount(() => {
  if (usernameTimer) window.clearTimeout(usernameTimer)
  if (passwordTimer) window.clearTimeout(passwordTimer)
})
</script>

<template>
  <div class="credentials-card">
    <div class="credential-item">
      <div class="credential-meta">
        <span class="credential-label">{{ usernameLabel }}</span>
        <code class="credential-value" aria-label="Tên đăng nhập">{{ username || '—' }}</code>
      </div>
      <Button
        :label="copiedUsername ? 'Đã sao chép' : 'Sao chép'"
        :icon="copiedUsername ? 'pi pi-check' : 'pi pi-copy'"
        severity="secondary"
        outlined
        size="small"
        :disabled="!username"
        @click="copyText(username, false)"
      />
    </div>

    <div class="credential-divider" />

    <div class="credential-item">
      <div class="credential-meta">
        <span class="credential-label">{{ passwordLabel }}</span>
        <code class="credential-value credential-password" aria-label="Mật khẩu tạm">{{ password || '—' }}</code>
      </div>
      <Button
        :label="copiedPassword ? 'Đã sao chép' : 'Sao chép'"
        :icon="copiedPassword ? 'pi pi-check' : 'pi pi-copy'"
        severity="secondary"
        outlined
        size="small"
        :disabled="!password"
        @click="copyText(password, true)"
      />
    </div>
  </div>
</template>

<style scoped>
.credentials-card {
  display: flex;
  flex-direction: column;
  padding: 1.25rem;
  background: var(--ui-surface);
  border: 1px solid var(--ui-border);
  border-radius: var(--ui-radius-md, 8px);
  gap: 1rem;
}

.credential-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.credential-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.credential-label {
  font-size: 13px;
  color: var(--ui-muted);
}

.credential-value {
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--ui-ink);
  overflow-wrap: anywhere;
  user-select: text;
}

.credential-password {
  font-size: 1.35rem;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.credential-divider {
  height: 1px;
  background: var(--ui-border);
}

@media (max-width: 520px) {
  .credential-item {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
