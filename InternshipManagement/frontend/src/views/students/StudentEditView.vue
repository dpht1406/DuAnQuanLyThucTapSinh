<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { getStudentById, updateStudent } from '../../api/students.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { formatServerDateTime, parseServerUtc } from '../../utils/dateTime.js'
import { mapStudentServerErrors, validateStudentEditForm } from '../../utils/studentEditValidation.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const student = ref(null)
const updatedAtText = ref(null)
const updatedAtIso = ref(null)
const loading = ref(false)
const formLoaded = ref(false)
const saving = ref(false)
const submitted = ref(false)
const formError = ref('')
const fieldErrors = reactive({})
const fieldRefs = {}
const form = reactive({
  FullName: '',
  Email: '',
  PhoneNumber: '',
  Major: '',
  ClassName: ''
})
const initialFormValue = ref(null)

const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  initialFormValue.value !== null &&
  JSON.stringify(form) !== initialFormValue.value
)

let loadSequence = 0

function setFieldRef(name, instance) {
  fieldRefs[name] = instance?.$el ?? instance
}

function clearFieldError(name) {
  if (fieldErrors[name]) delete fieldErrors[name]
  if (formError.value) formError.value = ''
}

function fieldId(name) {
  return `${name.toLowerCase().replace(/([A-Z])/g, '-$1')}-input`
}

function validateField(name) {
  const errors = validateStudentEditForm(form)
  if (errors[name]) fieldErrors[name] = errors[name]
  else delete fieldErrors[name]
}

async function focusFirstInvalidField() {
  await nextTick()
  const firstField = ['FullName', 'Email', 'PhoneNumber', 'Major', 'ClassName']
    .find((name) => fieldErrors[name])
  if (!firstField) return

  const element = fieldRefs[firstField] || document.getElementById(fieldId(firstField))
  if (element) {
    element.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
    element.focus?.()
  }
}

function clearFieldErrors() {
  Object.keys(fieldErrors).forEach((name) => delete fieldErrors[name])
}

async function loadStudent(id) {
  const sequence = ++loadSequence
  loading.value = true
  formLoaded.value = false
  formError.value = ''
  student.value = null
  updatedAtText.value = null
  updatedAtIso.value = null
  initialFormValue.value = null
  clearFieldErrors()

  try {
    const data = await getStudentById(id)
    if (sequence !== loadSequence) return

    student.value = data
    updatedAtText.value = formatServerDateTime(data.updatedAt)
    updatedAtIso.value = updatedAtText.value ? parseServerUtc(data.updatedAt)?.toISOString() ?? null : null
    form.FullName = data.fullName ?? ''
    form.Email = data.email ?? ''
    form.PhoneNumber = data.phoneNumber ?? ''
    form.Major = data.major ?? ''
    form.ClassName = data.className ?? ''
    initialFormValue.value = JSON.stringify(form)
    formLoaded.value = true
  } catch (error) {
    if (sequence === loadSequence) {
      formError.value = extractErrorMessage(error, 'Không tải được thông tin sinh viên.')
    }
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}

function navigateBack() {
  const id = route.params.id
  if (route.query.from === 'detail') {
    return router.push({ name: 'student-detail', params: { id } })
  }
  return router.push({ name: 'students' })
}

async function submitForm() {
  if (saving.value) return

  const errors = validateStudentEditForm(form)
  clearFieldErrors()
  Object.assign(fieldErrors, errors)
  formError.value = ''

  if (Object.keys(fieldErrors).length > 0) {
    await focusFirstInvalidField()
    return
  }

  saving.value = true
  try {
    await updateStudent(route.params.id, {
      FullName: form.FullName.trim(),
      Email: form.Email.trim(),
      PhoneNumber: form.PhoneNumber.trim(),
      Major: form.Major.trim(),
      ClassName: form.ClassName.trim()
    })
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Cập nhật sinh viên thành công.',
      life: 3000
    })
    await navigateBack()
  } catch (error) {
    const mappedErrors = mapStudentServerErrors(error)
    if (Object.keys(mappedErrors).length > 0) {
      clearFieldErrors()
      Object.assign(fieldErrors, mappedErrors)
      formError.value = ''
      await focusFirstInvalidField()
      return
    }

    formError.value = extractErrorMessage(error, 'Không thể cập nhật sinh viên. Vui lòng thử lại.')
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

onMounted(() => {
  window.addEventListener('beforeunload', onBeforeUnload)
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', onBeforeUnload)
})
</script>

<template>
  <div class="page-container student-edit-page">
    <header class="student-edit-header">
      <Button
        label="Quay lại"
        icon="pi pi-arrow-left"
        severity="secondary"
        text
        class="back-button"
        :disabled="saving"
        @click="navigateBack"
      />
      <div class="student-edit-title-block">
        <h1 class="page-title">Sửa sinh viên</h1>
        <p v-if="updatedAtText" class="student-edit-updated">
          <i class="pi pi-clock" aria-hidden="true" />
          Cập nhật lần cuối: <time :datetime="updatedAtIso">{{ updatedAtText }}</time>
        </p>
      </div>
    </header>

    <p v-if="loading" class="loading-state">Đang tải thông tin sinh viên...</p>

    <Message v-else-if="formError && !formLoaded" severity="error" :closable="false" class="form-global-error">
      {{ formError }}
    </Message>

    <section v-else-if="formLoaded" class="student-edit-card">
      <Message v-if="formError" severity="error" :closable="false" class="form-global-error">
        {{ formError }}
      </Message>

      <form class="student-edit-form" novalidate @submit.prevent="submitForm">
        <div class="form-section">
          <div class="form-section-header">
            <h2>Thông tin cá nhân</h2>
            <p>Thông tin liên hệ cơ bản của sinh viên.</p>
          </div>

          <div class="form-grid">
            <div class="form-field">
              <label for="student-code-input">MSSV</label>
              <InputText
                id="student-code-input"
                :model-value="student.studentCode ?? ''"
                class="student-input student-code-input"
                disabled
              />
              <small class="student-code-note">Mã sinh viên không thể thay đổi.</small>
            </div>

            <div class="form-field" :class="{ 'has-error': Boolean(fieldErrors.FullName) }">
              <label for="full-name-input">Họ tên <span class="required-mark">*</span></label>
              <InputText
                id="full-name-input"
                v-model="form.FullName"
                maxlength="150"
                placeholder="Nhập họ và tên"
                class="student-input"
                :invalid="Boolean(fieldErrors.FullName)"
                :disabled="saving"
                :aria-invalid="Boolean(fieldErrors.FullName)"
                :aria-describedby="fieldErrors.FullName ? 'full-name-error' : undefined"
                @blur="validateField('FullName')"
                @input="clearFieldError('FullName')"
                :ref="(el) => setFieldRef('FullName', el)"
              />
              <small v-if="fieldErrors.FullName" id="full-name-error" class="field-error" aria-live="polite">
                <i class="pi pi-exclamation-circle" aria-hidden="true" />
                {{ fieldErrors.FullName }}
              </small>
            </div>

            <div class="form-field" :class="{ 'has-error': Boolean(fieldErrors.Email) }">
              <label for="email-input">Email <span class="required-mark">*</span></label>
              <InputText
                id="email-input"
                v-model="form.Email"
                type="email"
                maxlength="150"
                placeholder="ten@truong.edu.vn"
                class="student-input"
                :invalid="Boolean(fieldErrors.Email)"
                :disabled="saving"
                :aria-invalid="Boolean(fieldErrors.Email)"
                :aria-describedby="fieldErrors.Email ? 'email-error' : undefined"
                @blur="validateField('Email')"
                @input="clearFieldError('Email')"
                :ref="(el) => setFieldRef('Email', el)"
              />
              <small v-if="fieldErrors.Email" id="email-error" class="field-error" aria-live="polite">
                <i class="pi pi-exclamation-circle" aria-hidden="true" />
                {{ fieldErrors.Email }}
              </small>
            </div>

            <div class="form-field" :class="{ 'has-error': Boolean(fieldErrors.PhoneNumber) }">
              <label for="phone-number-input">Số điện thoại <span class="required-mark">*</span></label>
              <InputText
                id="phone-number-input"
                v-model="form.PhoneNumber"
                type="tel"
                inputmode="numeric"
                maxlength="10"
                placeholder="0912 345 678"
                class="student-input"
                :invalid="Boolean(fieldErrors.PhoneNumber)"
                :disabled="saving"
                :aria-invalid="Boolean(fieldErrors.PhoneNumber)"
                :aria-describedby="fieldErrors.PhoneNumber ? 'phone-number-error' : undefined"
                @blur="validateField('PhoneNumber')"
                @input="clearFieldError('PhoneNumber')"
                :ref="(el) => setFieldRef('PhoneNumber', el)"
              />
              <small v-if="fieldErrors.PhoneNumber" id="phone-number-error" class="field-error" aria-live="polite">
                <i class="pi pi-exclamation-circle" aria-hidden="true" />
                {{ fieldErrors.PhoneNumber }}
              </small>
            </div>
          </div>
        </div>

        <div class="form-section">
          <div class="form-section-header">
            <h2>Thông tin học tập</h2>
            <p>Ngành và lớp sinh viên đang theo học.</p>
          </div>

          <div class="form-grid">
            <div class="form-field" :class="{ 'has-error': Boolean(fieldErrors.Major) }">
              <label for="major-input">Ngành <span class="required-mark">*</span></label>
              <InputText
                id="major-input"
                v-model="form.Major"
                maxlength="150"
                placeholder="Ví dụ: Công nghệ thông tin"
                class="student-input"
                :invalid="Boolean(fieldErrors.Major)"
                :disabled="saving"
                :aria-invalid="Boolean(fieldErrors.Major)"
                :aria-describedby="fieldErrors.Major ? 'major-error' : undefined"
                @blur="validateField('Major')"
                @input="clearFieldError('Major')"
                :ref="(el) => setFieldRef('Major', el)"
              />
              <small v-if="fieldErrors.Major" id="major-error" class="field-error" aria-live="polite">
                <i class="pi pi-exclamation-circle" aria-hidden="true" />
                {{ fieldErrors.Major }}
              </small>
            </div>

            <div class="form-field" :class="{ 'has-error': Boolean(fieldErrors.ClassName) }">
              <label for="class-name-input">Lớp <span class="required-mark">*</span></label>
              <InputText
                id="class-name-input"
                v-model="form.ClassName"
                maxlength="50"
                placeholder="Ví dụ: CNTT2023A"
                class="student-input"
                :invalid="Boolean(fieldErrors.ClassName)"
                :disabled="saving"
                :aria-invalid="Boolean(fieldErrors.ClassName)"
                :aria-describedby="fieldErrors.ClassName ? 'class-name-error' : undefined"
                @blur="validateField('ClassName')"
                @input="clearFieldError('ClassName')"
                :ref="(el) => setFieldRef('ClassName', el)"
              />
              <small v-if="fieldErrors.ClassName" id="class-name-error" class="field-error" aria-live="polite">
                <i class="pi pi-exclamation-circle" aria-hidden="true" />
                {{ fieldErrors.ClassName }}
              </small>
            </div>
          </div>
        </div>

        <div class="form-footer">
          <span class="required-note"><span class="required-mark">*</span> Trường bắt buộc</span>
          <div class="form-actions">
            <Button
              type="button"
              label="Hủy"
              severity="secondary"
              variant="outlined"
              :disabled="saving"
              @click="navigateBack"
            />
            <Button
              type="submit"
              label="Lưu thay đổi"
              icon="pi pi-check"
              :loading="saving"
              :disabled="saving"
            />
          </div>
        </div>
      </form>
    </section>
  </div>
</template>

<style scoped>
.student-edit-page {
  min-width: 0;
  color: var(--apply-ink);
}

.student-edit-header {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-bottom: 1.25rem;
}

.student-edit-title-block {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 0.75rem;
  min-width: 0;
}

.student-edit-header .page-title {
  margin: 0;
  font-size: 28px;
  line-height: 1.25;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.student-edit-updated {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.3rem;
  margin: 0;
  color: var(--apply-muted);
  font-size: 13px;
  line-height: 1.4;
}

.student-edit-updated i {
  font-size: 12px;
}

.back-button {
  min-height: 44px;
  flex: 0 0 auto;
}

.student-edit-card {
  min-width: 0;
  background: var(--apply-surface);
  border: 1px solid var(--apply-border);
  border-radius: 18px;
  box-shadow: var(--apply-shadow);
  padding: 1.5rem;
}

.student-edit-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.25rem;
  border: 1px solid var(--apply-border);
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.5);
}

.form-section-header {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.form-section-header h2 {
  margin: 0;
  color: var(--apply-ink);
  font-size: 16px;
  font-weight: 600;
  letter-spacing: -0.01em;
}

.form-section-header p {
  margin: 0;
  color: var(--apply-muted);
  font-size: 12.5px;
  line-height: 1.5;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem 1.25rem;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  min-width: 0;
}

.form-field label {
  color: var(--apply-ink);
  font-size: 13.5px;
  font-weight: 600;
  line-height: 1.4;
}

.required-mark {
  color: var(--apply-error);
}

.student-input {
  width: 100%;
  min-height: 44px;
  border: 1px solid var(--apply-border);
  border-radius: 10px;
  background: var(--apply-surface);
  color: var(--apply-ink);
  font-size: 15px;
  font-weight: 400;
  transition: border-color 0.15s ease, box-shadow 0.15s ease, background-color 0.15s ease;
}

.student-input::placeholder {
  color: var(--apply-placeholder);
  font-size: 14px;
}

.student-input:focus {
  border-color: var(--apply-primary);
  box-shadow: var(--apply-focus-ring);
}

.student-code-input.student-input {
  background: var(--apply-neutral-soft);
}

.student-code-note {
  color: var(--apply-muted);
  font-size: 12px;
}

.form-field.has-error .student-input,
.form-field.has-error .p-inputtext {
  border-color: var(--apply-error);
  background: var(--apply-danger-soft);
}

.form-field.has-error .student-input:focus,
.form-field.has-error .p-inputtext:focus {
  border-color: var(--apply-error);
  box-shadow: var(--apply-error-ring);
}

.field-error {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin: 0;
  color: var(--apply-error);
  font-size: 12.5px;
  line-height: 1.5;
  font-weight: 500;
}

.field-error i {
  font-size: 12px;
}

.form-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 0.5rem;
  border-top: 1px solid var(--apply-border);
}

.required-note {
  color: var(--apply-muted);
  font-size: 12.5px;
  font-weight: 500;
}

.form-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
}

.form-global-error {
  margin-bottom: 1rem;
}

.loading-state {
  margin: 0;
  color: var(--apply-muted);
}

@media (max-width: 640px) {
  .student-edit-header {
    margin-bottom: 1rem;
  }

  .student-edit-header .page-title {
    font-size: 17px;
    font-weight: 600;
  }

  .student-edit-title-block {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.125rem;
  }

  .student-edit-updated {
    font-size: 12.5px;
  }

  .student-edit-card {
    padding: 1rem;
    border-radius: 14px;
  }

  .form-section {
    padding: 0.875rem;
  }

  .form-grid {
    grid-template-columns: minmax(0, 1fr);
  }

  .form-section-header h2 {
    font-size: 15px;
  }

  .student-input {
    font-size: 16px;
  }

  .form-footer {
    position: sticky;
    bottom: 0;
    z-index: 2;
    display: flex;
    flex-direction: column-reverse;
    align-items: stretch;
    gap: 0.75rem;
    padding: 0.75rem 0 calc(0.75rem + env(safe-area-inset-bottom));
    background: var(--apply-surface);
    border-top: 1px solid var(--apply-border);
  }

  .required-note {
    text-align: left;
  }

  .form-actions {
    display: flex;
    width: 100%;
    gap: 0.75rem;
  }

  .form-actions :deep(.p-button) {
    flex: 1 1 0;
    min-height: 44px;
  }
}
</style>
