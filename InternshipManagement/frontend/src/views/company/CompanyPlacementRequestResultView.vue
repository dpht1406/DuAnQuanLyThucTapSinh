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
import {
  getCompanyPlacementRequestById,
  recordCompanyPlacementResult
} from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { formatServerDateTime } from '../../utils/dateTime.js'
import {
  mapPlacementRequestStatus,
  validateCompanyPlacementResult
} from '../../utils/companyPlacementRequest.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const detail = ref(null)
const loading = ref(false)
const saving = ref(false)
const submitted = ref(false)
const loadError = ref('')
const formError = ref('')

const form = reactive({
  isPassed: null,
  note: ''
})

const fieldErrors = reactive({
  isPassed: '',
  note: ''
})

const initialFormJson = ref(JSON.stringify(form))

const outcomeOptions = [
  { label: 'Đạt (Tiếp nhận thực tập)', value: true },
  { label: 'Không đạt (Từ chối hồ sơ)', value: false }
]

const statusInfo = computed(() => mapPlacementRequestStatus(detail.value?.status))
const canRecord = computed(() => statusInfo.value.value === 3)
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  JSON.stringify(form) !== initialFormJson.value
)

function applicantName(item) {
  return item?.applicantFullName || item?.studentFullName || '—'
}

function dateLabel(value) {
  return formatServerDateTime(value) ?? '—'
}

function clearError(field) {
  fieldErrors[field] = ''
  formError.value = ''
}

async function loadDetail() {
  loading.value = true
  loadError.value = ''
  detail.value = null
  try {
    detail.value = await getCompanyPlacementRequestById(route.params.id)
  } catch (err) {
    loadError.value = extractErrorMessage(err, 'Không tải được chi tiết đơn ứng tuyển.')
  } finally {
    loading.value = false
  }
}

async function submitResult() {
  if (saving.value || !canRecord.value) return
  formError.value = ''
  fieldErrors.isPassed = ''
  fieldErrors.note = ''

  if (form.isPassed === null) {
    fieldErrors.isPassed = 'Vui lòng chọn kết quả phỏng vấn.'
    return
  }

  const noteError = validateCompanyPlacementResult(form.isPassed, form.note)
  if (noteError) {
    fieldErrors.note = noteError
    return
  }

  saving.value = true
  try {
    await recordCompanyPlacementResult(route.params.id, {
      isPassed: form.isPassed,
      note: form.note.trim() || null
    })
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: `Đã ghi nhận kết quả ${form.isPassed ? 'Đạt' : 'Không đạt'}.`,
      life: 3000
    })
    router.push(`/company/placement-requests/${route.params.id}`)
  } catch (err) {
    if (err?.response?.status === 409) {
      formError.value = 'Dữ liệu vừa thay đổi'
      await loadDetail()
    } else {
      formError.value = extractErrorMessage(err, 'Không thể ghi nhận kết quả phỏng vấn.')
    }
  } finally {
    saving.value = false
  }
}

watch(() => route.params.id, loadDetail, { immediate: true })

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
    title="Ghi nhận kết quả phỏng vấn"
    subtitle="Đánh giá kết quả phỏng vấn ứng viên và hoàn tất phản hồi."
    back-label="Quay lại chi tiết đơn"
    :back-to="`/company/placement-requests/${route.params.id}`"
    class="company-placement-result-page"
  >
    <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
    <Message v-if="formError" severity="error" :closable="false">
      {{ formError }}
      <Button
        v-if="formError === 'Dữ liệu vừa thay đổi'"
        label="Quay lại chi tiết"
        text
        @click="router.push(`/company/placement-requests/${route.params.id}`)"
      />
    </Message>
    <Message v-if="detail && !canRecord" severity="warn" :closable="false">
      Đơn ứng tuyển này không ở trạng thái đã mời phỏng vấn nên không thể ghi nhận kết quả.
    </Message>

    <form v-if="detail" class="result-form" novalidate @submit.prevent="submitResult">
      <!-- Applicant & Interview Summary -->
      <FormSection title="Đơn ứng tuyển" description="Thông tin hồ sơ và lịch phỏng vấn đã xếp.">
        <dl class="request-summary">
          <dt>Ứng viên</dt>
          <dd>{{ applicantName(detail) }} ({{ detail.applicantEmail || 'Chưa có email' }})</dd>
          <dt>Vị trí</dt>
          <dd>{{ detail.jobPositionTitle || 'Ứng tuyển chung' }}</dd>
          <dt>Trạng thái</dt>
          <dd>
            <Tag :value="statusInfo.label" :severity="statusInfo.severity" />
          </dd>
          <dt v-if="detail.interviewAt">Lịch phỏng vấn</dt>
          <dd v-if="detail.interviewAt">{{ dateLabel(detail.interviewAt) }}</dd>
          <dt v-if="detail.interviewLocation">Địa điểm</dt>
          <dd v-if="detail.interviewLocation">{{ detail.interviewLocation }}</dd>
        </dl>
      </FormSection>

      <!-- Outcome Selection & Note -->
      <FormSection title="Kết quả đánh giá" description="Chọn kết quả tiếp nhận và ghi chú chi tiết.">
        <FormField
          label="Kết quả phỏng vấn *"
          for="placement-result-outcome-select"
          :error="fieldErrors.isPassed"
        >
          <Select
            id="placement-result-outcome-select"
            v-model="form.isPassed"
            :options="outcomeOptions"
            option-label="label"
            option-value="value"
            placeholder="Chọn kết quả phỏng vấn"
            :disabled="loading || saving || !canRecord"
            :invalid="Boolean(fieldErrors.isPassed)"
            @change="clearError('isPassed')"
          />
        </FormField>

        <FormField
          :label="`Ghi chú đánh giá${form.isPassed === false ? ' * (bắt buộc khi Không đạt)' : ' (tùy chọn)'}`"
          :optional="form.isPassed !== false"
          for="placement-result-note-input"
          hint="Nhận xét về năng lực ứng viên hoặc lý do đánh giá."
          :error="fieldErrors.note"
        >
          <Textarea
            id="placement-result-note-input"
            v-model="form.note"
            rows="4"
            maxlength="1000"
            auto-resize
            placeholder="Nhập ghi chú hoặc lý do đánh giá kết quả..."
            :disabled="loading || saving || !canRecord"
            :invalid="Boolean(fieldErrors.note)"
            @input="clearError('note')"
          />
        </FormField>
      </FormSection>

      <FormActionBar>
        <Button
          label="Hủy"
          severity="secondary"
          text
          class="btn-cancel"
          :disabled="saving"
          @click="router.push(`/company/placement-requests/${route.params.id}`)"
        />
        <Button
          label="Ghi nhận kết quả"
          icon="pi pi-check"
          type="submit"
          class="btn-primary"
          :loading="saving"
          :disabled="loading || saving || !canRecord"
        />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.result-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.result-form :deep(.p-select),
.result-form :deep(.p-textarea) {
  width: 100%;
}

.result-form :deep(.p-select) {
  min-height: 44px;
}

.result-form :deep(.p-textarea) {
  min-height: 110px;
}

.request-summary {
  display: grid;
  grid-template-columns: minmax(8rem, 0.35fr) 1fr;
  gap: 12px 16px;
  margin: 0;
}

.request-summary dt {
  color: var(--ui-muted);
  font-weight: 600;
  font-size: 13.5px;
}

.request-summary dd {
  min-width: 0;
  margin: 0;
  color: var(--ui-ink);
  font-size: 14px;
  overflow-wrap: anywhere;
}

@media (max-width: 640px) {
  .request-summary {
    grid-template-columns: 1fr;
    gap: 4px;
  }

  .request-summary dd {
    margin-bottom: 8px;
  }
}
</style>
