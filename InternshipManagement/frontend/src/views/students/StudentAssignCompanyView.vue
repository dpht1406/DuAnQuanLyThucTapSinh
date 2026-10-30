<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Textarea from 'primevue/textarea'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { getCompanies, getCompanyById } from '../../api/companies.js'
import { assignCompanyToStudent, getStudentById } from '../../api/students.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateSafeText } from '../../utils/emailValidation.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const student = ref(null)
const loading = ref(false)
const saving = ref(false)
const formLoaded = ref(false)
const submitted = ref(false)
const loadError = ref('')
const companyError = ref('')
const assignmentError = ref('')
const companyOptions = ref([])
const jobPositionOptions = ref([])
const companyId = ref(null)
const jobPositionId = ref(null)
const assignmentNote = ref('')
const initialFormValue = ref(null)

const source = computed(() => ['list', 'detail'].includes(route.query.from) ? route.query.from : 'detail')
const backTarget = computed(() => source.value === 'list'
  ? { name: 'students' }
  : { name: 'student-detail', params: { id: route.params.id } })
const eligible = computed(() => student.value?.status === 0)
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  eligible.value &&
  initialFormValue.value !== null &&
  JSON.stringify([companyId.value, jobPositionId.value, assignmentNote.value]) !== initialFormValue.value
)

let loadSequence = 0
let companyRequestSequence = 0

function navigateBack() {
  return router.push(backTarget.value)
}

async function loadCompanies() {
  companyError.value = ''
  try {
    const result = await getCompanies({ pageSize: 100 })
    companyOptions.value = (result?.items ?? []).map((company) => ({
      label: company.name,
      value: company.id
    }))
  } catch (error) {
    companyError.value = extractErrorMessage(error, 'Không tải được danh sách doanh nghiệp.')
  }
}

async function loadCompanyPositions(selectedCompanyId) {
  const sequence = ++companyRequestSequence
  jobPositionId.value = null
  jobPositionOptions.value = []
  assignmentError.value = ''
  if (selectedCompanyId == null) return

  try {
    const company = await getCompanyById(selectedCompanyId)
    if (sequence !== companyRequestSequence) return
    jobPositionOptions.value = (company?.jobPositions ?? [])
      .filter((position) => position.isOpen === true)
      .map((position) => ({ label: position.title, value: position.id }))
  } catch (error) {
    if (sequence === companyRequestSequence) {
      assignmentError.value = extractErrorMessage(error, 'Không tải được vị trí tuyển dụng.')
    }
  }
}

async function loadStudent(id) {
  const sequence = ++loadSequence
  loading.value = true
  formLoaded.value = false
  submitted.value = false
  loadError.value = ''
  companyError.value = ''
  assignmentError.value = ''
  student.value = null
  companyOptions.value = []
  jobPositionOptions.value = []
  companyId.value = null
  jobPositionId.value = null
  assignmentNote.value = ''
  initialFormValue.value = null

  try {
    const data = await getStudentById(id)
    if (sequence !== loadSequence) return
    student.value = data
    formLoaded.value = true
    if (data.status === 0) {
      initialFormValue.value = JSON.stringify([companyId.value, jobPositionId.value, assignmentNote.value])
      await loadCompanies()
    }
  } catch (error) {
    if (sequence === loadSequence) {
      loadError.value = extractErrorMessage(error, 'Không tải được thông tin sinh viên.')
    }
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}

async function submitAssignment() {
  assignmentError.value = ''
  const inputError = validateSafeText(assignmentNote.value, 1000, true)
  if (inputError) {
    assignmentError.value = inputError
    return
  }
  if (companyId.value == null) {
    assignmentError.value = 'Vui lòng chọn doanh nghiệp.'
    return
  }

  saving.value = true
  try {
    await assignCompanyToStudent(student.value.id, {
      companyId: companyId.value,
      jobPositionId: jobPositionId.value,
      note: assignmentNote.value.trim() || null
    })
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Gán doanh nghiệp thành công.',
      life: 3000
    })
    await navigateBack()
  } catch (error) {
    assignmentError.value = extractErrorMessage(error, 'Không thể gán doanh nghiệp, vui lòng thử lại.')
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
watch(companyId, loadCompanyPositions)

onBeforeRouteLeave(() => {
  if (!hasUnsavedChanges.value) return true
  return window.confirm('Bạn có thay đổi chưa lưu. Rời trang?')
})

onMounted(() => window.addEventListener('beforeunload', onBeforeUnload))
onBeforeUnmount(() => window.removeEventListener('beforeunload', onBeforeUnload))
</script>

<template>
  <FormPage
    class="student-assign-company-page"
    title="Gán doanh nghiệp"
    subtitle="Chọn doanh nghiệp và vị trí tuyển dụng cho sinh viên."
    back-label="Quay lại"
    :back-to="backTarget"
  >
    <Message v-if="loading" severity="info" :closable="false">Đang tải thông tin sinh viên...</Message>
    <Message v-else-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>

    <template v-else-if="formLoaded && student">
      <FormSection title="Sinh viên">
        <div class="student-summary">
          <div>
            <strong>{{ student.fullName || '—' }}</strong>
            <span>MSSV {{ student.studentCode || '—' }}</span>
          </div>
        </div>
      </FormSection>

      <template v-if="eligible">
        <Message v-if="companyError" severity="error" :closable="false">{{ companyError }}</Message>
        <Message v-if="assignmentError" severity="error" :closable="false">{{ assignmentError }}</Message>

        <FormSection title="Thông tin gán doanh nghiệp">
          <FormField label="Doanh nghiệp" for="assignment-company">
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
          </FormField>

          <FormField label="Vị trí tuyển dụng" for="assignment-position" optional>
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
          </FormField>

          <FormField label="Ghi chú gán doanh nghiệp" for="assignment-note" optional>
            <Textarea
              id="assignment-note"
              v-model="assignmentNote"
              rows="3"
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
            label="Gán doanh nghiệp"
            icon="pi pi-building"
            class="btn-primary"
            :loading="saving"
            :disabled="saving || companyOptions.length === 0"
            @click="submitAssignment"
          />
        </FormActionBar>
      </template>

      <template v-else>
        <Message severity="info" :closable="false">
          Chỉ có thể gán doanh nghiệp cho sinh viên ở trạng thái “Chưa có doanh nghiệp”.
        </Message>
        <FormActionBar>
          <Button
            type="button"
            label="Quay lại"
            severity="secondary"
            class="btn-cancel"
            @click="navigateBack"
          />
        </FormActionBar>
      </template>
    </template>
  </FormPage>
</template>

<style scoped>
.student-assign-company-page { min-height: 100dvh; }
.student-summary { display: flex; align-items: center; gap: 1rem; }
.student-summary > div { display: grid; gap: .25rem; min-width: 0; }
.student-summary strong { overflow-wrap: anywhere; }
.student-summary span { color: var(--ui-muted); }
.w-full { width: 100%; }
@media (max-width: 640px) {
  .student-assign-company-page { min-height: 100dvh; }
}
</style>
