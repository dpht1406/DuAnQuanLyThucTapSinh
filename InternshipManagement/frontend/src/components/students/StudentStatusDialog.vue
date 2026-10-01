<script setup>
import { computed, ref, watch } from 'vue'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'
import { assignCompanyToStudent, changeStudentStatus } from '../../api/students.js'
import { getCompanies, getCompanyById } from '../../api/companies.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { STATUS_LABELS, STATUS_OPTIONS, STATUS_SEVERITY } from '../../utils/studentStatus.js'

const props = defineProps({
  student: { type: Object, default: null },
  visible: { type: Boolean, default: false }
})
const emit = defineEmits(['update:visible', 'changed'])

const toast = (() => { try { return useToast() } catch { return null } })()
const targetStatus = ref(null)
const note = ref('')
const rejectReason = ref('')
const statusError = ref('')
const assignmentError = ref('')
const companyError = ref('')
const saving = ref(false)
const companyOptions = ref([])
const jobPositionOptions = ref([])
const companyId = ref(null)
const jobPositionId = ref(null)
const assignmentNote = ref('')

const currentStatusLabel = computed(() => STATUS_LABELS[props.student?.status] ?? '—')
const statusOptions = computed(() => STATUS_OPTIONS
  .filter((option) => option.value !== props.student?.status)
  .filter((option) => props.student?.companyId != null || option.value < 1)
  .map((option) => ({
    ...option,
    label: option.value === Number(props.student?.status) + 1
      ? `${option.label} (đề xuất)`
      : option.label
  })))
const isBackward = computed(() => targetStatus.value != null && targetStatus.value < props.student?.status)

function showSuccess(message) {
  try { toast?.add({ severity: 'success', summary: 'Thành công', detail: message, life: 3000 }) } catch { }
}

function closeDialog() {
  if (!saving.value) emit('update:visible', false)
}

async function loadCompanies() {
  companyError.value = ''
  try {
    const result = await getCompanies({ pageSize: 100 })
    companyOptions.value = (result?.items ?? []).map((company) => ({ label: company.name, value: company.id }))
  } catch (err) {
    companyError.value = extractErrorMessage(err, 'Không tải được danh sách doanh nghiệp.')
  }
}

watch(companyId, async (selectedCompanyId) => {
  jobPositionId.value = null
  jobPositionOptions.value = []
  assignmentError.value = ''
  if (selectedCompanyId == null) return

  try {
    const company = await getCompanyById(selectedCompanyId)
    jobPositionOptions.value = (company?.jobPositions ?? [])
      .filter((position) => position.isOpen === true)
      .map((position) => ({ label: position.title, value: position.id }))
  } catch (err) {
    assignmentError.value = extractErrorMessage(err, 'Không tải được vị trí tuyển dụng.')
  }
})

watch(() => [props.visible, props.student?.id], async ([visible]) => {
  if (!visible) return
  targetStatus.value = null
  note.value = ''
  rejectReason.value = ''
  statusError.value = ''
  assignmentError.value = ''
  companyError.value = ''
  companyId.value = null
  assignmentNote.value = ''
  if (props.student?.status === 0) await loadCompanies()
})

async function submitStatusChange() {
  statusError.value = ''
  if (targetStatus.value == null) {
    statusError.value = 'Vui lòng chọn trạng thái cần chuyển.'
    return
  }
  if (targetStatus.value >= 1 && props.student?.companyId == null) {
    statusError.value = 'Vui lòng gán doanh nghiệp trước khi chuyển trạng thái.'
    return
  }
  if (isBackward.value && !note.value.trim()) {
    statusError.value = 'Phải nhập ghi chú khi lùi trạng thái.'
    return
  }

  saving.value = true
  try {
    await changeStudentStatus(props.student.id, {
      newStatus: targetStatus.value,
      note: note.value.trim() || null,
      rejectReason: rejectReason.value.trim() || null
    })
    showSuccess('Đổi giai đoạn thành công.')
    emit('update:visible', false)
    emit('changed')
  } catch (err) {
    statusError.value = extractErrorMessage(err, 'Không thể đổi giai đoạn, vui lòng thử lại.')
  } finally {
    saving.value = false
  }
}

async function submitAssignment() {
  assignmentError.value = ''
  if (companyId.value == null) {
    assignmentError.value = 'Vui lòng chọn doanh nghiệp.'
    return
  }

  saving.value = true
  try {
    await assignCompanyToStudent(props.student.id, {
      companyId: companyId.value,
      jobPositionId: jobPositionId.value,
      note: assignmentNote.value.trim() || null
    })
    showSuccess('Gán doanh nghiệp thành công.')
    emit('update:visible', false)
    emit('changed')
  } catch (err) {
    assignmentError.value = extractErrorMessage(err, 'Không thể gán doanh nghiệp, vui lòng thử lại.')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Cập nhật giai đoạn sinh viên"
    :closable="!saving"
    :style="{ width: 'min(34rem, calc(100vw - 2rem))' }"
    @update:visible="emit('update:visible', $event)"
  >
    <div v-if="student" class="status-dialog-content">
      <div class="current-status">
        <span>Trạng thái hiện tại</span>
        <Tag :value="currentStatusLabel" :severity="STATUS_SEVERITY[student.status]" />
      </div>

      <Message v-if="statusError" severity="error" :closable="false">{{ statusError }}</Message>
      <form class="status-form" @submit.prevent="submitStatusChange">
        <div class="form-field">
          <label for="target-student-status">Chuyển sang</label>
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
        </div>
        <Message v-if="student.companyId == null" severity="info" :closable="false">
          Cần gán doanh nghiệp trước khi chuyển sang giai đoạn từ “Đã giới thiệu”.
        </Message>
        <div class="form-field">
          <label for="status-note">Ghi chú{{ isBackward ? ' (bắt buộc khi lùi)' : '' }}</label>
          <Textarea id="status-note" v-model="note" rows="3" :disabled="saving" />
        </div>
        <div v-if="isBackward" class="form-field">
          <label for="status-reject-reason">Lý do từ chối</label>
          <Textarea id="status-reject-reason" v-model="rejectReason" rows="2" :disabled="saving" />
        </div>
        <div class="dialog-actions">
          <Button type="button" label="Đóng" severity="secondary" text :disabled="saving" @click="closeDialog" />
          <Button type="submit" label="Lưu giai đoạn" icon="pi pi-check" :loading="saving" :disabled="statusOptions.length === 0" />
        </div>
      </form>

      <section v-if="student.status === 0" class="assignment-section">
        <h3>Gán doanh nghiệp</h3>
        <Message v-if="companyError" severity="error" :closable="false">{{ companyError }}</Message>
        <Message v-if="assignmentError" severity="error" :closable="false">{{ assignmentError }}</Message>
        <div class="form-field">
          <label for="assignment-company">Doanh nghiệp</label>
          <Select
            id="assignment-company"
            v-model="companyId"
            :options="companyOptions"
            option-label="label"
            option-value="value"
            placeholder="Chọn doanh nghiệp"
            class="w-full"
            :disabled="saving || companyOptions.length === 0"
          />
        </div>
        <div class="form-field">
          <label for="assignment-position">Vị trí tuyển dụng</label>
          <Select
            id="assignment-position"
            v-model="jobPositionId"
            :options="jobPositionOptions"
            option-label="label"
            option-value="value"
            placeholder="Không chọn vị trí"
            show-clear
            class="w-full"
            :disabled="saving || companyId == null || jobPositionOptions.length === 0"
          />
        </div>
        <div class="form-field">
          <label for="assignment-note">Ghi chú gán doanh nghiệp</label>
          <Textarea id="assignment-note" v-model="assignmentNote" rows="2" :disabled="saving" />
        </div>
        <div class="dialog-actions">
          <Button label="Gán doanh nghiệp" icon="pi pi-building" :loading="saving" :disabled="companyOptions.length === 0" @click="submitAssignment" />
        </div>
      </section>
    </div>
  </Dialog>
</template>

<style scoped>
.status-dialog-content, .status-form, .assignment-section { display: grid; gap: 1rem; }
.current-status { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
.current-status > span, .form-field label { font-weight: 600; }
.form-field { display: grid; gap: .35rem; }
.w-full, .form-field :deep(.p-inputtext), .form-field :deep(.p-textarea) { width: 100%; }
.dialog-actions { display: flex; justify-content: flex-end; gap: .5rem; }
.assignment-section { border-top: 1px solid var(--surface-border); padding-top: 1rem; }
.assignment-section h3 { margin: 0; font-size: 1rem; }
</style>