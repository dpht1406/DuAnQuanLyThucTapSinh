<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Textarea from 'primevue/textarea'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { createJobPosition, getJobPositionById, updateJobPosition } from '../../api/job-positions.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import dayjs from '../../utils/dayjs.js'
import { validateSafeText } from '../../utils/emailValidation.js'

const route = useRoute()
const router = useRouter()
const id = computed(() => route.params.id ? Number(route.params.id) : null)
const companyId = computed(() => route.params.companyId ? Number(route.params.companyId) : null)
const isCreate = computed(() => id.value === null)
const loading = ref(false)
const saving = ref(false)
const formLoaded = ref(false)
const submitted = ref(false)
const loadError = ref('')
const formError = ref('')
const fieldErrors = reactive({})
const initialFormValue = ref(null)
const today = new Date()
const form = reactive({
  companyId: null,
  title: '',
  department: '',
  location: '',
  deadline: null,
  quantity: 1,
  description: '',
  isOpen: true
})
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  formLoaded.value &&
  initialFormValue.value !== null &&
  JSON.stringify(form) !== initialFormValue.value
)

function clearFieldError(field) {
  delete fieldErrors[field]
  formError.value = ''
}

function validateForm() {
  Object.keys(fieldErrors).forEach((field) => delete fieldErrors[field])
  if (!String(form.title ?? '').trim()) fieldErrors.title = 'Tên vị trí không được để trống.'
  const limits = [['title', 200], ['department', 100], ['location', 300], ['description', 2000]]
  for (const [field, maximumLength] of limits) {
    const error = validateSafeText(form[field] ?? '', maximumLength, field === 'description')
    if (error) fieldErrors[field] = error
  }
  if (!Number.isInteger(form.quantity) || form.quantity < 1) {
    fieldErrors.quantity = 'Số lượng phải lớn hơn hoặc bằng 1.'
  }
  if (String(form.department ?? '').trim().length > 100) {
    fieldErrors.department = 'Phòng ban không được quá 100 ký tự.'
  }
  if (String(form.location ?? '').trim().length > 300) {
    fieldErrors.location = 'Địa điểm không được quá 300 ký tự.'
  }
  return Object.keys(fieldErrors).length === 0
}

async function loadPosition() {
  formLoaded.value = false
  submitted.value = false
  initialFormValue.value = null
  if (isCreate.value) {
    form.companyId = companyId.value
    initialFormValue.value = JSON.stringify(form)
    formLoaded.value = true
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    const position = await getJobPositionById(id.value)
    Object.assign(form, {
      companyId: position.companyId,
      title: position.title ?? '',
      department: position.department ?? '',
      location: position.location ?? '',
      deadline: position.deadline ? dayjs(position.deadline).toDate() : null,
      quantity: position.quantity ?? 1,
      description: position.description ?? '',
      isOpen: position.isOpen === true
    })
    initialFormValue.value = JSON.stringify(form)
    formLoaded.value = true
  } catch (error) {
    loadError.value = extractErrorMessage(error, 'Không tải được vị trí tuyển dụng.')
  } finally {
    loading.value = false
  }
}

async function savePosition() {
  if (saving.value || !validateForm()) return
  saving.value = true
  formError.value = ''
  try {
    if (isCreate.value) {
      await createJobPosition({
        ...form,
        deadline: form.deadline ? dayjs(form.deadline).format('YYYY-MM-DD') : null
      })
    } else {
      const { companyId: _companyId, ...dto } = form
      await updateJobPosition(id.value, {
        ...dto,
        deadline: form.deadline ? dayjs(form.deadline).format('YYYY-MM-DD') : null
      })
    }
    submitted.value = true
    await router.push({ path: '/companies', query: { company: form.companyId } })
  } catch (error) {
    formError.value = extractErrorMessage(error, 'Có lỗi xảy ra, vui lòng thử lại.')
  } finally {
    saving.value = false
  }
}

watch(() => [route.params.id, route.params.companyId], loadPosition, { immediate: true })

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
    :title="isCreate ? 'Thêm vị trí' : 'Sửa vị trí'"
    subtitle="Thông tin vị trí tuyển dụng của doanh nghiệp."
    back-label="Quay lại"
    :back-to="{ name: 'companies' }"
    class="admin-job-position-form-page"
  >
    <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
    <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
    <form v-if="!loadError" class="admin-job-position-form" novalidate @submit.prevent="savePosition">
      <FormSection title="Thông tin vị trí" description="Các thông tin cơ bản về cơ hội thực tập.">
        <FormField label="Tên vị trí" for="admin-job-title" :error="fieldErrors.title">
          <InputText id="admin-job-title" v-model="form.title" maxlength="200" :disabled="loading || saving" :invalid="Boolean(fieldErrors.title)" @input="clearFieldError('title')" />
        </FormField>
        <div class="form-grid">
          <FormField label="Số lượng" for="admin-job-quantity" :error="fieldErrors.quantity">
            <InputNumber id="admin-job-quantity" v-model="form.quantity" :min="1" :use-grouping="false" :disabled="loading || saving" :invalid="Boolean(fieldErrors.quantity)" @input="clearFieldError('quantity')" />
          </FormField>
          <FormField label="Phòng ban" for="admin-job-department" optional :error="fieldErrors.department">
            <InputText id="admin-job-department" v-model="form.department" maxlength="100" :disabled="loading || saving" :invalid="Boolean(fieldErrors.department)" @input="clearFieldError('department')" />
          </FormField>
          <FormField label="Địa điểm" for="admin-job-location" optional :error="fieldErrors.location">
            <InputText id="admin-job-location" v-model="form.location" maxlength="300" placeholder="Để trống để dùng địa chỉ công ty" :disabled="loading || saving" :invalid="Boolean(fieldErrors.location)" @input="clearFieldError('location')" />
          </FormField>
          <FormField label="Hạn nộp hồ sơ" for="admin-job-deadline" optional>
            <DatePicker id="admin-job-deadline" v-model="form.deadline" date-format="dd/mm/yy" show-button-bar :min-date="isCreate ? today : undefined" :disabled="loading || saving" />
          </FormField>
        </div>
        <FormField label="Mô tả" for="admin-job-description" optional :error="fieldErrors.description">
          <Textarea id="admin-job-description" v-model="form.description" rows="6" maxlength="2000" auto-resize :disabled="loading || saving" :invalid="Boolean(fieldErrors.description)" @input="clearFieldError('description')" />
        </FormField>
        <label class="open-toggle" for="admin-job-is-open">
          <Checkbox input-id="admin-job-is-open" v-model="form.isOpen" binary :disabled="loading || saving" />
          <span>Đang tuyển</span>
        </label>
      </FormSection>
      <FormActionBar>
        <Button label="Hủy" severity="secondary" text class="btn-cancel" :disabled="saving" @click="router.push('/companies')" />
        <Button label="Lưu" icon="pi pi-check" type="submit" class="btn-primary" :loading="saving" :disabled="loading || saving" />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.admin-job-position-form { display: flex; flex-direction: column; gap: 8px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
.admin-job-position-form :deep(.p-inputtext),
.admin-job-position-form :deep(.p-inputnumber),
.admin-job-position-form :deep(.p-datepicker),
.admin-job-position-form :deep(.p-textarea) { width: 100%; min-height: 44px; }
.admin-job-position-form :deep(.p-textarea) { min-height: 140px; }
.open-toggle { display: flex; align-items: center; gap: 10px; color: var(--ui-ink); font-size: 14px; }
@media (max-width: 640px) {
  .form-grid { grid-template-columns: 1fr; }
}
</style>
