<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { cancelAcceptedPlacementResult, getPlacementRequestById } from '../../api/placement-requests.js'
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
const fieldErrors = reactive({ note: '' })
const note = ref('')
const initialFormValue = ref(null)
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  formLoaded.value &&
  initialFormValue.value !== null &&
  JSON.stringify(note.value) !== initialFormValue.value
)
const returnLocation = computed(() => ({
  name: 'placement-requests',
  query: route.query.tab === 'confirmation' ? { tab: 'confirmation' } : {}
}))
const canCancel = computed(() => request.value?.status === 4)

async function loadRequest() {
  loading.value = true
  formLoaded.value = false
  loadError.value = ''
  request.value = null
  try {
    request.value = await getPlacementRequestById(route.params.id)
    if (initialFormValue.value === null) initialFormValue.value = JSON.stringify(note.value)
    formLoaded.value = true
  } catch (error) {
    loadError.value = extractErrorMessage(error, 'Không tải được thông tin đơn thực tập.')
  } finally {
    loading.value = false
  }
}

async function submitForm() {
  if (saving.value || !canCancel.value) return
  formError.value = ''
  fieldErrors.note = validateSafeText(note.value, 1000, true) || ''
  if (fieldErrors.note) return

  saving.value = true
  try {
    await cancelAcceptedPlacementResult(route.params.id, note.value.trim() || null)
    submitted.value = true
    toast.add({ severity: 'success', summary: 'Thành công', detail: 'Đã hủy kết quả tiếp nhận sinh viên.', life: 3000 })
    await router.push(returnLocation.value)
  } catch (error) {
    if (error?.response?.status === 409) {
      formError.value = 'Dữ liệu vừa thay đổi'
      await loadRequest()
    } else {
      formError.value = extractErrorMessage(error, 'Không thể hủy kết quả tiếp nhận, vui lòng thử lại.')
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
    title="Hủy kết quả tiếp nhận"
    subtitle="Xác nhận hủy kết quả tiếp nhận của sinh viên."
    back-label="Quay lại danh sách"
    :back-to="returnLocation"
    class="placement-result-form-page"
  >
    <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
    <Message v-if="formError" severity="error" :closable="false">
      {{ formError }}
      <Button v-if="formError === 'Dữ liệu vừa thay đổi'" label="Quay lại danh sách" text @click="router.push(returnLocation)" />
    </Message>
    <Message v-if="request && !canCancel" severity="warn" :closable="false">
      Đơn không còn ở trạng thái đã tiếp nhận nên không thể hủy kết quả.
    </Message>
    <form v-if="request" class="placement-result-form" novalidate @submit.prevent="submitForm">
      <FormSection title="Đơn thực tập" description="Thông tin đơn cần xác nhận hủy.">
        <dl class="request-summary">
          <dt>Sinh viên</dt><dd>{{ request.studentFullName }} ({{ request.studentCode }})</dd>
          <dt>Doanh nghiệp</dt><dd>{{ request.companyName }}</dd>
          <dt>Vị trí</dt><dd>{{ request.jobPositionTitle || '—' }}</dd>
          <dt>Trạng thái</dt><dd><Tag :value="placementRequestStatusLabels[request.status] ?? 'Không xác định'" :severity="placementRequestStatusSeverities[request.status]" /></dd>
        </dl>
      </FormSection>
      <FormSection title="Xác nhận hủy">
        <p class="cancel-warning">Bạn xác nhận hủy kết quả tiếp nhận của {{ request.studentFullName }}?</p>
        <FormField label="Ghi chú" for="cancel-accepted-note" optional :error="fieldErrors.note">
          <Textarea id="cancel-accepted-note" v-model="note" rows="4" maxlength="1000" auto-resize :disabled="loading || saving || !canCancel" :invalid="Boolean(fieldErrors.note)" @input="fieldErrors.note = ''" />
        </FormField>
      </FormSection>
      <FormActionBar>
        <Button label="Quay lại" severity="secondary" text class="btn-cancel" :disabled="saving" @click="router.push(returnLocation)" />
        <Button label="Xác nhận hủy" icon="pi pi-undo" severity="danger" type="submit" :loading="saving" :disabled="loading || saving || !canCancel" />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.placement-result-form { display: flex; flex-direction: column; gap: 8px; }
.placement-result-form :deep(.p-textarea) { width: 100%; min-height: 44px; }
.request-summary { display: grid; grid-template-columns: minmax(8rem, .35fr) 1fr; gap: 12px 16px; margin: 0; }
.request-summary dt { color: var(--ui-muted); font-weight: 600; }
.request-summary dd { min-width: 0; margin: 0; color: var(--ui-ink); overflow-wrap: anywhere; }
.cancel-warning { margin: 0; color: var(--ui-ink); font-weight: 600; }
@media (max-width: 640px) {
  .request-summary { grid-template-columns: 1fr; gap: 4px; }
  .request-summary dd { margin-bottom: 8px; }
}
</style>
