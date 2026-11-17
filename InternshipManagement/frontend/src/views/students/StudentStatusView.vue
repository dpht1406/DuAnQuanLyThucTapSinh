<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { changeStudentStatus, getStudentById } from '../../api/students.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateSafeText } from '../../utils/emailValidation.js'
import { STATUS_LABELS, STATUS_OPTIONS, STATUS_SEVERITY } from '../../utils/studentStatus.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const student = ref(null)
const loading = ref(false)
const saving = ref(false)
const formLoaded = ref(false)
const submitted = ref(false)
const loadError = ref('')
const statusError = ref('')
const targetStatus = ref(null)
const note = ref('')
const rejectReason = ref('')
const initialFormValue = ref(null)

const source = computed(() => ['list', 'detail'].includes(route.query.from) ? route.query.from : 'detail')
const backTarget = computed(() => source.value === 'list'
  ? { name: 'students' }
  : { name: 'student-detail', params: { id: route.params.id } })
const statusOptions = computed(() => STATUS_OPTIONS
  .filter((option) => option.value !== student.value?.status)
  .filter((option) => student.value?.companyId != null || option.value < 1)
  .map((option) => ({
    ...option,
    label: option.value === Number(student.value?.status) + 1
      ? `${option.label} (đề xuất)`
      : option.label
  })))
const isBackward = computed(() =>
  targetStatus.value != null && targetStatus.value < student.value?.status
)
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  initialFormValue.value !== null &&
  JSON.stringify([targetStatus.value, note.value, rejectReason.value]) !== initialFormValue.value
)

let loadSequence = 0

function navigateBack() {
  return router.push(backTarget.value)
}

function openCompanyAssignment() {
  return router.push({
    name: 'student-assign-company',
    params: { id: route.params.id },
    query: { from: source.value }
  })
}

async function loadStudent(id) {
  const sequence = ++loadSequence
  loading.value = true
  formLoaded.value = false
  submitted.value = false
  loadError.value = ''
  statusError.value = ''
  student.value = null
  targetStatus.value = null
  note.value = ''
  rejectReason.value = ''
  initialFormValue.value = null

  try {
    const data = await getStudentById(id)
    if (sequence !== loadSequence) return
    student.value = data
    initialFormValue.value = JSON.stringify([targetStatus.value, note.value, rejectReason.value])
    formLoaded.value = true
  } catch (error) {
    if (sequence === loadSequence) {
      loadError.value = extractErrorMessage(error, 'Không tải được thông tin sinh viên.')
    }
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}

async function submitStatusChange() {
  statusError.value = ''
  if (targetStatus.value == null) {
    statusError.value = 'Vui lòng chọn trạng thái cần chuyển.'
    return
  }
  if (targetStatus.value >= 1 && student.value?.companyId == null) {
    statusError.value = 'Vui lòng gán doanh nghiệp trước khi chuyển trạng thái.'
    return
  }
  for (const value of [note.value, rejectReason.value]) {
    const inputError = validateSafeText(value, 1000, true)
    if (inputError) {
      statusError.value = inputError
      return
    }
  }
  if (isBackward.value && !note.value.trim()) {
    statusError.value = 'Phải nhập ghi chú khi lùi trạng thái.'
    return
  }

  saving.value = true
  try {
    await changeStudentStatus(student.value.id, {
      newStatus: targetStatus.value,
      note: note.value.trim() || null,
      rejectReason: rejectReason.value.trim() || null
    })
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Đổi giai đoạn thành công.',
      life: 3000
    })
    await navigateBack()
  } catch (error) {
    statusError.value = extractErrorMessage(error, 'Không thể đổi giai đoạn, vui lòng thử lại.')
  } finally {
    saving.value = false
  }
}

function onBeforeUnload(event) {
  if (!hasUnsavedChanges.value) return
  event.preventDefault()
  event.returnValue = ''
}

watch(() => route.params.id, loadStudent, { immediate: true })

onBeforeRouteLeave(() => {
  if (!hasUnsavedChanges.value) return true
  return window.confirm('Bạn có thay đổi chưa lưu. Rời trang?')
})

onMounted(() => window.addEventListener('beforeunload', onBeforeUnload))
onBeforeUnmount(() => window.removeEventListener('beforeunload', onBeforeUnload))
</script>

<template>
  <FormPage
    class="student-status-page"
    title="Cập nhật giai đoạn sinh viên"
    subtitle="Cập nhật tiến độ thực tập của sinh viên."
    back-label="Quay lại"
    :back-to="backTarget"
  >
    <Message v-if="loading" severity="info" :closable="false">Đang tải thông tin sinh viên...</Message>
    <Message v-else-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>

    <template v-else-if="formLoaded && student">
      <Message v-if="statusError" severity="error" :closable="false">{{ statusError }}</Message>

      <FormSection title="Sinh viên">
        <div class="student-summary">
          <div>
            <strong>{{ student.fullName || '—' }}</strong>
            <span>MSSV {{ student.studentCode || '—' }}</span>
          </div>
          <Tag :value="STATUS_LABELS[student.status] ?? '—'" :severity="STATUS_SEVERITY[student.status]" />
        </div>
      </FormSection>

      <FormSection title="Giai đoạn" description="Chọn giai đoạn mới và nhập thông tin cần thiết.">
        <FormField label="Chuyển sang" for="target-student-status">
          <Select
            id="target-student-status"
            v-model="targetStatus"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            placeholder="Chọn giai đoạn"
            class="w-full"
            :disabled="saving || statusOptions.length === 0"
          />
        </FormField>

        <Message v-if="student.companyId == null" severity="info" :closable="false">
          Cần gán doanh nghiệp trước khi chuyển sang giai đoạn từ “Đã giới thiệu”.
        </Message>
        <Button
          v-if="student.status === 0 && student.companyId == null"
          type="button"
          label="Gán doanh nghiệp"
          icon="pi pi-building"
          severity="secondary"
          outlined
          :disabled="saving"
          @click="openCompanyAssignment"
        />

        <FormField
          :label="`Ghi chú${isBackward ? ' (bắt buộc khi lùi)' : ''}`"
          for="status-note"
        >
          <Textarea id="status-note" v-model="note" rows="3" maxlength="1000" :disabled="saving" />
        </FormField>

        <FormField v-if="isBackward" label="Lý do từ chối" for="status-reject-reason">
          <Textarea
            id="status-reject-reason"
            v-model="rejectReason"
            rows="2"
            maxlength="1000"
            :disabled="saving"
          />
        </FormField>
      </FormSection>

      <FormActionBar>
        <Button
          type="button"
          label="Hủy"
          severity="secondary"
          class="btn-cancel"
          :disabled="saving"
          @click="navigateBack"
        />
        <Button
          type="button"
          label="Lưu giai đoạn"
          icon="pi pi-check"
          class="btn-primary"
          :loading="saving"
          :disabled="saving || statusOptions.length === 0"
          @click="submitStatusChange"
        />
      </FormActionBar>
    </template>
  </FormPage>
</template>

<style scoped>
.student-status-page { min-height: 100dvh; }
.student-summary { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
.student-summary > div { display: grid; gap: .25rem; min-width: 0; }
.student-summary strong { overflow-wrap: anywhere; }
.student-summary span { color: var(--ui-muted); }
.w-full { width: 100%; }
@media (max-width: 640px) {
  .student-status-page { min-height: 100dvh; }
}
</style>
