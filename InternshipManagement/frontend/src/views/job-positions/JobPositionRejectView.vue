<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Textarea from 'primevue/textarea'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { getJobPositionById, rejectJobPosition } from '../../api/job-positions.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateSafeText } from '../../utils/emailValidation.js'

const route = useRoute()
const router = useRouter()
const position = ref(null)
const loading = ref(false)
const saving = ref(false)
const submitted = ref(false)
const loadError = ref('')
const formError = ref('')
const fieldErrors = reactive({ note: '' })
const note = ref('')
const initialNote = ref('')
const isPending = computed(() => position.value?.approvalStatus === 1)
const hasUnsavedChanges = computed(() => !submitted.value && note.value !== initialNote.value)

async function loadPosition(resetForm = true) {
  loading.value = true
  if (resetForm) submitted.value = false
  loadError.value = ''
  position.value = null
  try {
    position.value = await getJobPositionById(route.params.id)
    if (resetForm) {
      note.value = ''
      initialNote.value = ''
    }
  } catch (error) {
    loadError.value = extractErrorMessage(error, 'Không tải được vị trí tuyển dụng.')
  } finally {
    loading.value = false
  }
}

async function submitRejection() {
  if (saving.value || !isPending.value) return
  formError.value = ''
  fieldErrors.note = ''
  if (!note.value.trim()) {
    fieldErrors.note = 'Lý do từ chối không được để trống.'
    return
  }
  fieldErrors.note = validateSafeText(note.value, 1000, true) || ''
  if (fieldErrors.note) return

  saving.value = true
  try {
    await rejectJobPosition(route.params.id, note.value.trim())
    submitted.value = true
    router.push('/job-positions')
  } catch (error) {
    if (error?.response?.status === 409) {
      formError.value = 'Dữ liệu vừa thay đổi'
      await loadPosition(false)
    } else {
      formError.value = extractErrorMessage(error, 'Không thể từ chối tin tuyển dụng, vui lòng thử lại.')
    }
  } finally {
    saving.value = false
  }
}

watch(() => route.params.id, () => loadPosition(), { immediate: true })

onBeforeRouteLeave(() => {
  if (!hasUnsavedChanges.value) return true
  return window.confirm('Bạn có thay đổi chưa lưu. Rời trang?')
})

function onBeforeUnload(event) {
  if (!hasUnsavedChanges.value) return
  event.preventDefault()
  event.returnValue = ''
}

onMounted(() => window.addEventListener('beforeunload', onBeforeUnload))
onBeforeUnmount(() => window.removeEventListener('beforeunload', onBeforeUnload))
</script>

<template>
  <FormPage
    title="Từ chối tin tuyển dụng"
    subtitle="Kiểm tra thông tin vị trí và nhập lý do từ chối."
    back-label="Quay lại danh sách"
    :back-to="{ name: 'job-positions' }"
    class="job-position-reject-page"
  >
    <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
    <Message v-if="formError" severity="error" :closable="false">
      {{ formError }}
      <Button v-if="formError === 'Dữ liệu vừa thay đổi'" label="Quay lại danh sách" text @click="router.push('/job-positions')" />
    </Message>
    <Message v-if="position && !isPending" severity="info" :closable="false">
      Tin tuyển dụng này không còn ở trạng thái chờ duyệt nên không thể từ chối.
    </Message>
    <form v-if="position" class="job-position-reject-form" novalidate @submit.prevent="submitRejection">
      <FormSection title="Vị trí tuyển dụng" description="Xác nhận tin đang chờ duyệt trước khi từ chối.">
        <dl class="position-summary">
          <dt>Tên vị trí</dt><dd>{{ position.title }}</dd>
          <dt>Doanh nghiệp</dt><dd>{{ position.companyName }}</dd>
        </dl>
      </FormSection>
      <FormSection title="Lý do từ chối">
        <FormField label="Lý do từ chối *" for="reject-position-note" :error="fieldErrors.note">
          <Textarea id="reject-position-note" v-model="note" rows="5" maxlength="1000" auto-resize :disabled="loading || saving || !isPending" :invalid="Boolean(fieldErrors.note)" @input="fieldErrors.note = ''" />
        </FormField>
      </FormSection>
      <FormActionBar>
        <Button label="Hủy" severity="secondary" text class="btn-cancel" :disabled="saving" @click="router.push('/job-positions')" />
        <Button label="Xác nhận từ chối" icon="pi pi-times" severity="danger" type="submit" :loading="saving" :disabled="loading || saving || !isPending" />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.job-position-reject-form { display: flex; flex-direction: column; gap: 8px; }
.job-position-reject-form :deep(.p-textarea) { width: 100%; min-height: 140px; }
.position-summary { display: grid; grid-template-columns: minmax(8rem, .35fr) 1fr; gap: 12px 16px; margin: 0; }
.position-summary dt { color: var(--ui-muted); font-weight: 600; }
.position-summary dd { min-width: 0; margin: 0; color: var(--ui-ink); overflow-wrap: anywhere; }
@media (max-width: 640px) {
  .position-summary { grid-template-columns: 1fr; gap: 4px; }
  .position-summary dd { margin-bottom: 8px; }
}
</style>
