<script setup>
import { onBeforeUnmount, ref } from 'vue'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'

defineProps({
  visible: { type: Boolean, default: false },
  studentCode: { type: String, default: '' },
  temporaryPassword: { type: String, default: '' }
})
const emit = defineEmits(['update:visible'])

const toast = (() => { try { return useToast() } catch { return null } })()
const copied = ref(false)
let copiedTimer = null

async function copyPassword(password) {
  try {
    await navigator.clipboard.writeText(password)
    copied.value = true
    if (copiedTimer) window.clearTimeout(copiedTimer)
    copiedTimer = window.setTimeout(() => {
      copied.value = false
      copiedTimer = null
    }, 2000)
  } catch {
    toast?.add({
      severity: 'warn',
      summary: 'Không thể sao chép',
      detail: 'Vui lòng bôi đen mật khẩu để sao chép thủ công.',
      life: 4000
    })
  }
}

function onVisibilityChange(visible) {
  if (!visible) {
    copied.value = false
    if (copiedTimer) {
      window.clearTimeout(copiedTimer)
      copiedTimer = null
    }
    emit('update:visible', false)
  }
}

onBeforeUnmount(() => {
  if (copiedTimer) window.clearTimeout(copiedTimer)
})
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Mật khẩu tạm"
    :style="{ width: 'min(32rem, calc(100vw - 2rem))' }"
    @update:visible="onVisibilityChange"
  >
    <div class="reset-password-content">
      <p class="student-code">Mã sinh viên: <strong>{{ studentCode }}</strong></p>
      <div class="password-row">
        <code class="temporary-password" aria-label="Mật khẩu tạm">{{ temporaryPassword }}</code>
        <Button
          :label="copied ? 'Đã sao chép' : 'Sao chép'"
          :icon="copied ? 'pi pi-check' : 'pi pi-copy'"
          severity="secondary"
          outlined
          @click="copyPassword(temporaryPassword)"
        />
      </div>
      <Message severity="warn" :closable="false">
        Mật khẩu chỉ hiển thị một lần. Hãy gửi cho sinh viên ngay. Sinh viên sẽ phải đổi mật khẩu ở lần đăng nhập đầu tiên.
      </Message>
    </div>
  </Dialog>
</template>

<style scoped>
.reset-password-content { display: grid; gap: 1rem; }
.student-code { margin: 0; }
.password-row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
.temporary-password { min-width: 0; overflow-wrap: anywhere; user-select: text; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 1.5rem; font-weight: 700; }
@media (max-width: 420px) { .password-row { align-items: flex-start; flex-direction: column; } }
</style>