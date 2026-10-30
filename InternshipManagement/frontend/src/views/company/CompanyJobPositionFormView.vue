<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Checkbox from 'primevue/checkbox'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Textarea from 'primevue/textarea'
import Button from 'primevue/button'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import {
  createCompanyJobPosition,
  getCompanyJobPositionById,
  updateCompanyJobPosition
} from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { COMPANY_JOB_POSITION_LIMITS, validateCompanyJobPosition } from '../../utils/companyJobPositionValidation.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const id = computed(() => route.params.id ? Number(route.params.id) : null)
const isCreate = computed(() => id.value === null)
const loading = ref(false)
const saving = ref(false)
const loadError = ref('')
const formError = ref('')
const fieldErrors = reactive({})
const positionStatus = ref(null)
const form = reactive({
  title: '',
  department: '',
  location: '',
  deadline: '',
  quantity: 1,
  description: '',
  isOpen: true
})
const editable = computed(() => isCreate.value || positionStatus.value === 0 || positionStatus.value === 3 ||
  positionStatus.value === 'Draft' || positionStatus.value === 'Rejected')
const pageTitle = computed(() => isCreate.value ? 'Tạo vị trí tuyển dụng' : editable.value ? 'Cập nhật vị trí tuyển dụng' : 'Chi tiết vị trí tuyển dụng')

function dateValue(value) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return localDate.toISOString().slice(0, 10)
}

function clearFieldError(field) {
  delete fieldErrors[field]
  formError.value = ''
}

function validate() {
  Object.keys(fieldErrors).forEach((key) => delete fieldErrors[key])
  Object.assign(fieldErrors, validateCompanyJobPosition(form, { isCreate: isCreate.value }))
  if (Object.keys(fieldErrors).length > 0) {
    const firstField = Object.keys(fieldErrors)[0]
    document.getElementById(`company-position-${firstField}`)?.focus()
    return false
  }
  return true
}

async function loadPosition() {
  if (isCreate.value) return
  loading.value = true
  loadError.value = ''
  try {
    const position = await getCompanyJobPositionById(id.value)
    positionStatus.value = position.approvalStatus
    Object.assign(form, {
      title: position.title ?? '',
      department: position.department ?? '',
      location: position.location ?? '',
      deadline: dateValue(position.deadline),
      quantity: position.quantity ?? 1,
      description: position.description ?? '',
      isOpen: Boolean(position.isOpen)
    })
  } catch (err) {
    loadError.value = extractErrorMessage(err, 'Không tải được vị trí tuyển dụng.')
  } finally {
    loading.value = false
  }
}

async function savePosition() {
  if (saving.value || !editable.value || !validate()) return
  saving.value = true
  formError.value = ''
  try {
    const payload = {
      title: form.title.trim(),
      department: form.department.trim(),
      location: form.location.trim() || null,
      deadline: form.deadline ? `${form.deadline}T00:00:00` : null,
      quantity: Number(form.quantity),
      description: form.description,
      isOpen: Boolean(form.isOpen)
    }
    if (isCreate.value) {
      await createCompanyJobPosition(payload)
      toast.add({ severity: 'success', summary: 'Thành công', detail: 'Đã tạo vị trí tuyển dụng ở trạng thái Draft.', life: 3000 })
    } else {
      await updateCompanyJobPosition(id.value, payload)
      toast.add({ severity: 'success', summary: 'Thành công', detail: 'Đã cập nhật vị trí tuyển dụng.', life: 3000 })
    }
    await router.push({ name: 'company-job-positions' })
  } catch (err) {
    formError.value = extractErrorMessage(err, 'Không thể lưu vị trí tuyển dụng.')
  } finally {
    saving.value = false
  }
}

onMounted(loadPosition)
</script>

<template>
  <FormPage
    :title="pageTitle"
    :subtitle="isCreate ? 'Điền thông tin vị trí; tin mới sẽ được lưu ở trạng thái Draft.' : 'Thông tin vị trí tuyển dụng của doanh nghiệp.'"
    back-label="Vị trí tuyển dụng"
    :back-to="{ name: 'company-job-positions' }"
    class="company-position-form-page"
  >
    <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
    <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
    <Message v-if="!isCreate && !editable && !loading && !loadError" severity="info" :closable="false">
      Vị trí đang chờ duyệt hoặc đã được duyệt nên chỉ có thể xem.
    </Message>

    <form v-if="!loadError" class="company-position-form" novalidate @submit.prevent="savePosition">
      <FormSection title="Thông tin vị trí" description="Các thông tin cơ bản để ứng viên hiểu rõ cơ hội thực tập.">
        <FormField label="Tiêu đề" for="company-position-title" :error="fieldErrors.title">
          <InputText
            id="company-position-title"
            v-model="form.title"
            :maxlength="COMPANY_JOB_POSITION_LIMITS.title"
            placeholder="Ví dụ: Thực tập sinh phát triển phần mềm"
            :disabled="loading || saving || !editable"
            :invalid="Boolean(fieldErrors.title)"
            :aria-describedby="fieldErrors.title ? 'company-position-title-error' : undefined"
            @input="clearFieldError('title')"
          />
        </FormField>

        <div class="form-grid">
          <FormField label="Phòng ban" for="company-position-department" optional :error="fieldErrors.department">
            <InputText
              id="company-position-department"
              v-model="form.department"
              :maxlength="COMPANY_JOB_POSITION_LIMITS.department"
              placeholder="Ví dụ: Công nghệ"
              :disabled="loading || saving || !editable"
              :invalid="Boolean(fieldErrors.department)"
              @input="clearFieldError('department')"
            />
          </FormField>
          <FormField label="Địa điểm" for="company-position-location" optional :error="fieldErrors.location">
            <InputText
              id="company-position-location"
              v-model="form.location"
              :maxlength="COMPANY_JOB_POSITION_LIMITS.location"
              placeholder="Ví dụ: TP. Hồ Chí Minh"
              :disabled="loading || saving || !editable"
              :invalid="Boolean(fieldErrors.location)"
              @input="clearFieldError('location')"
            />
          </FormField>
        </div>
      </FormSection>

      <FormSection title="Thời hạn và số lượng" description="Thiết lập số lượng thực tập sinh và hạn tiếp nhận hồ sơ.">
        <div class="form-grid">
          <FormField label="Số lượng tuyển" for="company-position-quantity" :error="fieldErrors.quantity">
            <InputText
              id="company-position-quantity"
              v-model.number="form.quantity"
              type="number"
              min="1"
              step="1"
              inputmode="numeric"
              :disabled="loading || saving || !editable"
              :invalid="Boolean(fieldErrors.quantity)"
              @input="clearFieldError('quantity')"
            />
          </FormField>
          <FormField label="Hạn nộp hồ sơ" for="company-position-deadline" optional :error="fieldErrors.deadline">
            <InputText
              id="company-position-deadline"
              v-model="form.deadline"
              type="date"
              :disabled="loading || saving || !editable"
              :invalid="Boolean(fieldErrors.deadline)"
              @input="clearFieldError('deadline')"
            />
          </FormField>
        </div>
        <FormField
          label="Mô tả"
          for="company-position-description"
          optional
          :error="fieldErrors.description"
          :hint="`${form.description.length}/${COMPANY_JOB_POSITION_LIMITS.description} ký tự`"
        >
          <Textarea
            id="company-position-description"
            v-model="form.description"
            :maxlength="COMPANY_JOB_POSITION_LIMITS.description"
            rows="7"
            auto-resize
            placeholder="Mô tả công việc, yêu cầu và quyền lợi..."
            :disabled="loading || saving || !editable"
            :invalid="Boolean(fieldErrors.description)"
            @input="clearFieldError('description')"
          />
        </FormField>
        <label class="open-toggle" for="company-position-is-open">
          <Checkbox
            input-id="company-position-is-open"
            v-model="form.isOpen"
            binary
            :disabled="loading || saving || !editable"
          />
          <span>Cho phép nhận hồ sơ sau khi vị trí được duyệt</span>
        </label>
      </FormSection>

      <FormActionBar v-if="editable">
        <Button label="Hủy" severity="secondary" text class="btn-cancel" :disabled="saving" @click="router.push({ name: 'company-job-positions' })" />
        <Button
          :label="saving ? 'Đang lưu...' : isCreate ? 'Tạo vị trí' : 'Lưu thay đổi'"
          icon="pi pi-check"
          type="submit"
          class="btn-primary"
          :loading="saving"
          :disabled="loading"
        />
      </FormActionBar>
      <FormActionBar v-else>
        <Button label="Đóng" severity="secondary" text class="btn-cancel" @click="router.push({ name: 'company-job-positions' })" />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.company-position-form-page {
  --apply-primary: var(--ui-primary-600);
  --apply-border: var(--ui-input-border);
  --apply-surface: var(--ui-surface);
  --apply-ink: var(--ui-ink);
  --apply-muted: var(--ui-muted);
  --apply-error: var(--ui-error);
  --apply-placeholder: var(--ui-placeholder);
}
.company-position-form { display: flex; flex-direction: column; gap: 8px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
.company-position-form :deep(.p-inputtext),
.company-position-form :deep(.p-textarea) { width: 100%; min-height: 44px; }
.company-position-form :deep(.p-textarea) { min-height: 140px; resize: vertical; }
.company-position-form :deep(input[type='date']) { color: var(--apply-ink); }
.company-position-form :deep(.p-inputtext::placeholder),
.company-position-form :deep(.p-textarea::placeholder) { color: var(--apply-placeholder); opacity: 1; }
.open-toggle { display: flex; align-items: center; gap: 10px; color: var(--apply-ink); font-size: 14px; }
@media (max-width: 640px) {
  .form-grid { grid-template-columns: 1fr; gap: 16px; }
  .open-toggle { align-items: flex-start; line-height: 1.5; }
}
</style>
