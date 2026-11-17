<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { confirmAdminPlacementResult, getPlacementRequestById } from '../../api/placement-requests.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateSafeText } from '../../utils/emailValidation.js'
import { placementRequestStatusLabels, placementRequestStatusSeverities } from '../../utils/placementRequestStatus.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const request = ref(null)
const loading = ref(false)
const saving = ref(false)
const formLoaded = ref(false)
const submitted = ref(false)
const loadError = ref('')
const formError = ref('')
const fieldErrors = reactive({ decision: '', confirmationSource: '', note: '' })
const form = reactive({ decision: null, confirmationSource: '', note: '' })
const initialFormValue = ref(null)
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  formLoaded.value &&
  initialFormValue.value !== null &&
  JSON.stringify(form) !== initialFormValue.value
)
const decisionOptions = [
  { label: 'Đạt', value: 0 },
  { label: 'Không đạt', value: 1 },
  { label: 'Bác báo cáo', value: 2 }
]
const returnLocation = computed(() => ({
  name: 'placement-requests',
  query: route.query.tab === 'confirmation' ? { tab: 'confirmation' } : {}
}))
const canConfirm = computed(() =>
  request.value?.status === 3 &&
  Boolean(request.value.studentReportedInterviewAt) &&
  !request.value.studentReportDismissedAt
)
const eligibilityMessage = computed(() => {
  if (!request.value) return ''
  if (request.value.status !== 3) return 'Đơn không còn ở trạng thái đã mời phỏng vấn nên không thể xác nhận kết quả.'
  if (!request.value.studentReportedInterviewAt) return 'Đơn chưa có báo cáo phỏng vấn cần Admin xác nhận.'
  if (request.value.studentReportDismissedAt) return 'Báo cáo phỏng vấn này đã được xử lý.'
  return ''
})

function clearError(field) {
  fieldErrors[field] = ''
  formError.value = ''
}

async function loadRequest() {
  loading.value = true
  formLoaded.value = false
  loadError.value = ''
  request.value = null
  try {
    request.value = await getPlacementRequestById(route.params.id)
    if (initialFormValue.value === null) initialFormValue.value = JSON.stringify(form)
    formLoaded.value = true
  } catch (error) {
    loadError.value = extractErrorMessage(error, 'Không tải được thông tin đơn thực tập.')
  } finally {
    loading.value = false
  }
}

async function submitForm() {
  if (saving.value || !canConfirm.value) return
  formError.value = ''
  Object.keys(fieldErrors).forEach((field) => { fieldErrors[field] = '' })
  if (form.decision === null) {
    fieldErrors.decision = 'Vui lòng chọn kết quả xác nhận.'
    return
  }
  if (!form.confirmationSource.trim()) {
    fieldErrors.confirmationSource = 'Nguồn xác nhận không được để trống.'
    return
  }
  fieldErrors.confirmationSource = validateSafeText(form.confirmationSource, 500, true) || ''
  if (fieldErrors.confirmationSource) return
  if (form.decision === 1 && !form.note.trim()) {
    fieldErrors.note = 'Ghi chú bắt buộc khi chọn Không đạt.'
    return
  }
  fieldErrors.note = validateSafeText(form.note, 1000, true) || ''
  if (fieldErrors.note) return

  saving.value = true
  try {
    await confirmAdminPlacementResult(route.params.id, {
      decision: form.decision,
      confirmationSource: form.confirmationSource.trim(),
      note: form.note.trim() || null
    })
    submitted.value = true
    toast.add({ severity: 'success', summary: 'Thành công', detail: 'Đã cập nhật kết quả phỏng vấn.', life: 3000 })
    await router.push(returnLocation.value)
  } catch (error) {
    if (error?.response?.status === 409) {
      formError.value = 'Dữ liệu vừa thay đổi'
      await loadRequest()
    } else {
      formError.value = extractErrorMessage(error, 'Không thể cập nhật kết quả phỏng vấn, vui lòng thử lại.')
    }
  } finally {
    saving.value = false
  }
}

watch(() => route.params.id, loadRequest, { immediate: true })

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
    title="Xác nhận kết quả phỏng vấn"
    subtitle="Kiểm tra đơn thực tập và ghi nhận kết quả."
    back-label="Quay lại danh sách"
    :back-to="returnLocation"
    class="placement-result-form-page"
  >
    <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
    <Message v-if="formError" severity="error" :closable="false">
      {{ formError }}
      <Button v-if="formError === 'Dữ liệu vừa thay đổi'" label="Quay lại danh sách" text @click="router.push(returnLocation)" />
    </Message>
    <Message v-if="request && !canConfirm" severity="warn" :closable="false">{{ eligibilityMessage }}</Message>
    <form v-if="request" class="placement-result-form" novalidate @submit.prevent="submitForm">
      <FormSection title="Đơn thực tập" description="Thông tin đơn cần xác nhận.">
        <dl class="request-summary">
          <dt>Sinh viên</dt><dd>{{ request.studentFullName }} ({{ request.studentCode }})</dd>
          <dt>Doanh nghiệp</dt><dd>{{ request.companyName }}</dd>
          <dt>Vị trí</dt><dd>{{ request.jobPositionTitle || '—' }}</dd>
          <dt>Trạng thái</dt><dd><Tag :value="placementRequestStatusLabels[request.status] ?? 'Không xác định'" :severity="placementRequestStatusSeverities[request.status]" /></dd>
        </dl>
      </FormSection>
      <FormSection title="Kết quả phỏng vấn" description="Nhập kết quả và nguồn xác nhận của Admin.">
        <FormField label="Kết quả *" for="placement-decision" :error="fieldErrors.decision">
          <Select id="placement-decision" v-model="form.decision" :options="decisionOptions" option-label="label" option-value="value" placeholder="Chọn kết quả" :disabled="loading || saving || !canConfirm" :invalid="Boolean(fieldErrors.decision)" @update:model-value="clearError('decision')" />
        </FormField>
        <FormField label="Nguồn xác nhận *" for="placement-confirmation-source" :error="fieldErrors.confirmationSource">
          <Textarea id="placement-confirmation-source" v-model="form.confirmationSource" rows="3" maxlength="500" auto-resize :disabled="loading || saving || !canConfirm" :invalid="Boolean(fieldErrors.confirmationSource)" @input="clearError('confirmationSource')" />
        </FormField>
        <FormField :label="`Ghi chú${form.decision === 1 ? ' (bắt buộc khi Không đạt)' : ' (tùy chọn)'}`" for="placement-confirmation-note" :error="fieldErrors.note">
          <Textarea id="placement-confirmation-note" v-model="form.note" rows="4" maxlength="1000" auto-resize :disabled="loading || saving || !canConfirm" :invalid="Boolean(fieldErrors.note)" @input="clearError('note')" />
        </FormField>
      </FormSection>
      <FormActionBar>
        <Button label="Quay lại" severity="secondary" text class="btn-cancel" :disabled="saving" @click="router.push(returnLocation)" />
        <Button label="Ghi nhận kết quả" icon="pi pi-check" type="submit" class="btn-primary" :loading="saving" :disabled="loading || saving || !canConfirm" />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.placement-result-form { display: flex; flex-direction: column; gap: 8px; }
.placement-result-form :deep(.p-select),
.placement-result-form :deep(.p-textarea) { width: 100%; min-height: 44px; }
.placement-result-form :deep(.p-textarea) { min-height: 110px; }
.request-summary { display: grid; grid-template-columns: minmax(8rem, .35fr) 1fr; gap: 12px 16px; margin: 0; }
.request-summary dt { color: var(--ui-muted); font-weight: 600; }
.request-summary dd { min-width: 0; margin: 0; color: var(--ui-ink); overflow-wrap: anywhere; }
@media (max-width: 640px) {
  .request-summary { grid-template-columns: 1fr; gap: 4px; }
  .request-summary dd { margin-bottom: 8px; }
}
</style>
