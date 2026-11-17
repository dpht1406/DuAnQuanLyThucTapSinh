<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
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
import {
  getCompanyPlacementRequestById,
  rejectCompanyPlacementRequest
} from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import {
  mapPlacementRequestStatus,
  validateCompanyRejectReason
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
const reason = ref('')
const initialReason = ref('')
const fieldError = ref('')

const statusInfo = computed(() => mapPlacementRequestStatus(detail.value?.status))
const canReject = computed(() => statusInfo.value.value === 0)
const hasUnsavedChanges = computed(() => !submitted.value && reason.value !== initialReason.value)

function applicantName(item) {
  return item?.applicantFullName || item?.studentFullName || '—'
}

async function loadDetail() {
  loading.value = true
  loadError.value = ''
  detail.value = null
  try {
    detail.value = await getCompanyPlacementRequestById(route.params.id)
    reason.value = ''
    initialReason.value = ''
  } catch (err) {
    loadError.value = extractErrorMessage(err, 'Không tải được chi tiết đơn ứng tuyển.')
  } finally {
    loading.value = false
  }
}

async function submitRejection() {
  if (saving.value || !canReject.value) return
  formError.value = ''
  fieldError.value = ''

  const errorMsg = validateCompanyRejectReason(reason.value)
  if (errorMsg) {
    fieldError.value = errorMsg
    return
  }

  saving.value = true
  try {
    await rejectCompanyPlacementRequest(route.params.id, {
      reason: reason.value.trim()
    })
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Đã từ chối đơn ứng tuyển.',
      life: 3000
    })
    router.push(`/company/placement-requests/${route.params.id}`)
  } catch (err) {
    if (err?.response?.status === 409) {
      formError.value = 'Dữ liệu vừa thay đổi'
      await loadDetail()
    } else {
      formError.value = extractErrorMessage(err, 'Không thể từ chối đơn ứng tuyển.')
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
    title="Từ chối đơn ứng tuyển"
    subtitle="Kiểm tra thông tin ứng viên và nhập lý do từ chối hồ sơ."
    back-label="Quay lại chi tiết đơn"
    :back-to="`/company/placement-requests/${route.params.id}`"
    class="company-placement-reject-page"
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
    <Message v-if="detail && !canReject" severity="warn" :closable="false">
      Đơn ứng tuyển này không còn ở trạng thái chờ xử lý nên không thể từ chối.
    </Message>

    <form v-if="detail" class="reject-form" novalidate @submit.prevent="submitRejection">
      <FormSection title="Đơn ứng tuyển" description="Xác nhận hồ sơ trước khi thực hiện từ chối.">
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

      <FormSection title="Lý do từ chối" description="Nhập giải thích ngắn gọn để gửi phản hồi đến ứng viên.">
        <FormField
          label="Lý do từ chối *"
          for="reject-reason-input"
          hint="Lý do sẽ được lưu trong hệ thống và thông báo đến ứng viên."
          :error="fieldError"
        >
          <Textarea
            id="reject-reason-input"
            v-model="reason"
            rows="5"
            maxlength="1000"
            auto-resize
            placeholder="Ví dụ: Chưa đáp ứng đủ yêu cầu kỹ năng của vị trí, hoặc đã tuyển đủ số lượng..."
            :disabled="loading || saving || !canReject"
            :invalid="Boolean(fieldError)"
            @input="fieldError = ''"
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
          label="Xác nhận từ chối"
          icon="pi pi-times"
          severity="danger"
          type="submit"
          :loading="saving"
          :disabled="loading || saving || !canReject"
        />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.reject-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.reject-form :deep(.p-textarea) {
  width: 100%;
  min-height: 130px;
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
