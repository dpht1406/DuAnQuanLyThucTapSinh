<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import {
  getCompanyPlacementRequestById,
  inviteCompanyPlacementRequestToInterview
} from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import {
  mapPlacementRequestStatus,
  validateCompanyInterviewForm
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
const fieldErrors = ref({})

function getDefaultInterviewAt() {
  const defaultDate = new Date(Date.now() + 24 * 60 * 60 * 1000)
  const localDate = new Date(defaultDate.getTime() - defaultDate.getTimezoneOffset() * 60000)
  return localDate.toISOString().slice(0, 16)
}

const interviewForm = reactive({
  interviewAt: getDefaultInterviewAt(),
  interviewLocation: '',
  interviewNote: ''
})

const initialFormJson = ref(JSON.stringify(interviewForm))

const statusInfo = computed(() => mapPlacementRequestStatus(detail.value?.status))
const canInvite = computed(() => statusInfo.value.value === 0)
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  JSON.stringify(interviewForm) !== initialFormJson.value
)

function applicantName(item) {
  return item?.applicantFullName || item?.studentFullName || '—'
}

function clearError(field) {
  if (fieldErrors.value[field]) {
    delete fieldErrors.value[field]
  }
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

async function submitInterview() {
  if (saving.value || !canInvite.value) return
  formError.value = ''

  fieldErrors.value = validateCompanyInterviewForm(interviewForm)
  if (Object.keys(fieldErrors.value).length > 0) return

  saving.value = true
  try {
    await inviteCompanyPlacementRequestToInterview(route.params.id, {
      interviewAt: new Date(interviewForm.interviewAt).toISOString(),
      interviewLocation: interviewForm.interviewLocation.trim(),
      interviewNote: interviewForm.interviewNote.trim() || null
    })
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Đã gửi lời mời phỏng vấn.',
      life: 3000
    })
    router.push(`/company/placement-requests/${route.params.id}`)
  } catch (err) {
    if (err?.response?.status === 409) {
      formError.value = 'Dữ liệu vừa thay đổi'
      await loadDetail()
    } else {
      formError.value = extractErrorMessage(err, 'Không thể gửi lời mời phỏng vấn.')
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
    title="Mời phỏng vấn"
    subtitle="Nhập thông tin thời gian, địa điểm và gửi lời mời phỏng vấn đến ứng viên."
    back-label="Quay lại chi tiết đơn"
    :back-to="`/company/placement-requests/${route.params.id}`"
    class="company-placement-invite-page"
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
    <Message v-if="detail && !canInvite" severity="warn" :closable="false">
      Đơn ứng tuyển này không còn ở trạng thái chờ xử lý nên không thể gửi lời mời phỏng vấn.
    </Message>

    <form v-if="detail" class="invite-form" novalidate @submit.prevent="submitInterview">
      <!-- Applicant & Position Summary -->
      <FormSection title="Đơn ứng tuyển" description="Thông tin tóm tắt của hồ sơ ứng viên.">
        <dl class="request-summary">
          <dt>Ứng viên</dt>
          <dd>{{ applicantName(detail) }} ({{ detail.applicantEmail || 'Chưa có email' }})</dd>
          <dt>Vị trí</dt>
          <dd>{{ detail.jobPositionTitle || 'Ứng tuyển chung' }}</dd>
          <dt>Trạng thái</dt>
          <dd>
            <Tag :value="statusInfo.label" :severity="statusInfo.severity" />
          </dd>
        </dl>
      </FormSection>

      <!-- Interview Details Form -->
      <FormSection title="Thông tin phỏng vấn" description="Thời gian, địa điểm và hướng dẫn chi tiết cho ứng viên.">
        <FormField
          label="Ngày giờ phỏng vấn *"
          for="interview-at-input"
          :error="fieldErrors.interviewAt"
        >
          <InputText
            id="interview-at-input"
            v-model="interviewForm.interviewAt"
            type="datetime-local"
            :disabled="loading || saving || !canInvite"
            :invalid="Boolean(fieldErrors.interviewAt)"
            @input="clearError('interviewAt')"
          />
        </FormField>

        <FormField
          label="Địa điểm phỏng vấn *"
          for="interview-location-input"
          hint="Có thể là địa chỉ văn phòng hoặc liên kết phòng họp trực tuyến (Google Meet, MS Teams, Zoom...)"
          :error="fieldErrors.interviewLocation"
        >
          <InputText
            id="interview-location-input"
            v-model="interviewForm.interviewLocation"
            placeholder="Nhập địa chỉ hoặc liên kết họp online"
            maxlength="500"
            :disabled="loading || saving || !canInvite"
            :invalid="Boolean(fieldErrors.interviewLocation)"
            @input="clearError('interviewLocation')"
          />
        </FormField>

        <FormField
          label="Ghi chú phỏng vấn"
          optional
          for="interview-note-input"
          hint="Các lưu ý về tài liệu cần chuẩn bị, trang phục hoặc liên hệ khi đến."
          :error="fieldErrors.interviewNote"
        >
          <Textarea
            id="interview-note-input"
            v-model="interviewForm.interviewNote"
            rows="4"
            maxlength="1000"
            auto-resize
            placeholder="Nhập ghi chú hoặc dặn dò cho ứng viên..."
            :disabled="loading || saving || !canInvite"
            :invalid="Boolean(fieldErrors.interviewNote)"
            @input="clearError('interviewNote')"
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
          label="Gửi lời mời"
          icon="pi pi-send"
          type="submit"
          class="btn-primary"
          :loading="saving"
          :disabled="loading || saving || !canInvite"
        />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.invite-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.invite-form :deep(.p-inputtext),
.invite-form :deep(.p-textarea) {
  width: 100%;
}

.invite-form :deep(.p-inputtext) {
  min-height: 44px;
}

.invite-form :deep(.p-textarea) {
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
