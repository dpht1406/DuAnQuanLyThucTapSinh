<script setup>
import { ref, watch } from 'vue'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'
import { createPlacementRequest } from '../../api/me.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const props = defineProps({
  visible: { type: Boolean, default: false },
  position: { type: Object, default: null }
})
const emit = defineEmits(['update:visible', 'success'])
const toast = useToast()
const note = ref('')
const error = ref('')
const saving = ref(false)

watch(() => props.visible, (visible) => { if (visible) { note.value = ''; error.value = '' } })
function errorText(err) {
  return err?.response?.data?.message?.trim() || extractErrorMessage(err, 'Có lỗi xảy ra, vui lòng thử lại.')
}
async function submit() {
  if (!props.position || saving.value) return
  saving.value = true
  error.value = ''
  try {
    await createPlacementRequest({ companyId: props.position.companyId, jobPositionId: props.position.id, note: note.value.trim() })
    emit('update:visible', false)
    toast.add({ severity: 'success', summary: 'Thành công', detail: 'Tạo yêu cầu thành công.', life: 3000 })
    emit('success')
  } catch (err) {
    error.value = errorText(err)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :visible="visible" modal class="app-fullscreen-dialog" header="Tạo yêu cầu thực tập" :style="{ width: 'min(32rem, calc(100vw - 2rem))' }" :closable="!saving" @update:visible="emit('update:visible', $event)">
    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <form v-if="position" class="apply-form" @submit.prevent="submit">
      <div class="form-field"><label for="apply-company">Công ty</label><InputText id="apply-company" :model-value="position.companyName" readonly /></div>
      <div class="form-field"><label for="apply-position">Vị trí</label><InputText id="apply-position" :model-value="position.title" readonly /></div>
      <div class="form-field"><label for="apply-note">Ghi chú</label><Textarea id="apply-note" v-model="note" rows="4" maxlength="500" :disabled="saving" /><small>{{ note.length }}/500 ký tự</small></div>
      <div class="dialog-actions"><Button type="button" label="Hủy" severity="secondary" text :disabled="saving" @click="emit('update:visible', false)" /><Button type="submit" label="Xác nhận" icon="pi pi-check" :loading="saving" /></div>
    </form>
  </Dialog>
</template>

<style scoped>
.apply-form { display: grid; gap: 1rem; }
.form-field { min-width: 0; }
.form-field label { display: block; margin-bottom: .35rem; color: var(--text-h); font-weight: 600; }
.form-field :deep(.p-inputtext), .form-field :deep(.p-textarea) { width: 100%; }
.form-field small { display: block; margin-top: 4px; color: var(--text); text-align: right; }
.dialog-actions { display: flex; justify-content: flex-end; gap: .75rem; margin-top: .25rem; }
@media (max-width: 767px) { .dialog-actions { flex-direction: column-reverse; align-items: stretch; }.dialog-actions :deep(.p-button) { min-height: 44px; } }
:global(.p-dialog.app-fullscreen-dialog.p-component) { width: min(32rem, calc(100vw - 2rem)) !important; max-width: min(32rem, calc(100vw - 2rem)) !important; max-height: calc(100dvh - 2rem) !important; }
@media (max-width: 767px) {
  :global(.p-dialog.app-fullscreen-dialog.p-component) { height: auto !important; max-height: calc(100dvh - 2rem) !important; margin: 1rem; border-radius: var(--radius-md) !important; }
  :global(.p-dialog.app-fullscreen-dialog.p-component .p-dialog-content) { min-height: 0; overflow-y: auto; }
  :global(.p-dialog.app-fullscreen-dialog.p-component .dialog-actions) { position: sticky; bottom: 0; z-index: 1; padding: 12px 0 max(12px, env(safe-area-inset-bottom)); background: var(--surface); }
}
</style>