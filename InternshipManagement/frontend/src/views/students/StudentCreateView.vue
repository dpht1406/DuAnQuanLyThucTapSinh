<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { onBeforeRouteLeave, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { createStudent } from '../../api/students.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateEmailAddress, validateSafeText } from '../../utils/emailValidation.js'

const router = useRouter()
const toast = useToast()
const saving = ref(false)
const submitted = ref(false)
const formError = ref('')
const fieldErrors = reactive({})
const fieldRefs = {}

const form = reactive({
  StudentCode: '',
  FullName: '',
  Email: '',
  PhoneNumber: '',
  Major: '',
  ClassName: ''
})

const initialFormValue = ref(JSON.stringify(form))

const hasUnsavedChanges = computed(() => !submitted.value && JSON.stringify(form) !== initialFormValue.value)

function createEmptyForm() {
  return {
    StudentCode: '',
    FullName: '',
    Email: '',
    PhoneNumber: '',
    Major: '',
    ClassName: ''
  }
}

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

function validateStudentForm() {
  const errors = {}
  const safeStudentCode = form.StudentCode.trim()
  const safeFullName = form.FullName.trim()
  const safeEmail = form.Email.trim()
  const safePhone = form.PhoneNumber.trim()
  const safeMajor = form.Major.trim()
  const safeClassName = form.ClassName.trim()

  if (!safeStudentCode) {
    errors.StudentCode = 'MSSV không được để trống'
  } else {
    const safeCodeError = validateSafeText(form.StudentCode, 20)
    if (safeCodeError) errors.StudentCode = safeCodeError
  }

  if (!safeFullName) {
    errors.FullName = 'Họ tên không được để trống'
  } else {
    const safeFullNameError = validateSafeText(form.FullName, 100)
    if (safeFullNameError) errors.FullName = safeFullNameError
  }

  if (!safeEmail) {
    errors.Email = 'Email không được để trống'
  } else {
    const emailError = validateEmailAddress(form.Email, 150)
    if (emailError) {
      errors.Email = emailError.startsWith('Email có vẻ gõ sai') ? emailError : 'Email chưa đúng định dạng'
    }
  }

  if (!safePhone) {
    errors.PhoneNumber = 'Số điện thoại không được để trống'
  } else if (!/^0\d{9}$/.test(safePhone)) {
    errors.PhoneNumber = 'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0'
  }

  if (!safeMajor) {
    errors.Major = 'Ngành không được để trống'
  } else {
    const safeMajorError = validateSafeText(form.Major, 100)
    if (safeMajorError) errors.Major = safeMajorError
  }

  if (!safeClassName) {
    errors.ClassName = 'Lớp không được để trống'
  } else {
    const safeClassError = validateSafeText(form.ClassName, 50)
    if (safeClassError) errors.ClassName = safeClassError
  }

  return errors
}

function validateField(name) {
  const nextErrors = validateStudentForm()
  if (nextErrors[name]) fieldErrors[name] = nextErrors[name]
  else delete fieldErrors[name]
}

async function focusFirstInvalidField() {
  await nextTick()
  const firstField = Object.keys(fieldErrors)[0]
  if (!firstField) return

  const element = fieldRefs[firstField] || document.getElementById(fieldId(firstField))
  if (element) {
    element.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
    element.focus?.()
  }
}

function mapServerFieldErrors(error) {
  const mapped = {}
  const serverErrors = error?.response?.data?.errors

  if (!serverErrors || typeof serverErrors !== 'object' || Array.isArray(serverErrors)) return mapped

  const fieldMap = {
    studentcode: 'StudentCode',
    studentCode: 'StudentCode',
    fullname: 'FullName',
    fullName: 'FullName',
    email: 'Email',
    phonenumber: 'PhoneNumber',
    phoneNumber: 'PhoneNumber',
    major: 'Major',
    classname: 'ClassName',
    className: 'ClassName'
  }

  for (const [key, value] of Object.entries(serverErrors)) {
    const normalizedKey = String(key).trim()
    const targetField = fieldMap[normalizedKey.toLowerCase()] || fieldMap[normalizedKey]

    const messages = Array.isArray(value) ? value : [value]
    const message = messages.filter(Boolean).map(String).join(' ')

    if (!message) continue
    if (targetField && Object.prototype.hasOwnProperty.call(form, targetField)) {
      mapped[targetField] = message
    }
  }

  return mapped
}

async function submitForm() {
  if (saving.value) return

  const errors = validateStudentForm()
  Object.keys(fieldErrors).forEach((name) => delete fieldErrors[name])
  Object.assign(fieldErrors, errors)
  formError.value = ''

  if (Object.keys(fieldErrors).length > 0) {
    await focusFirstInvalidField()
    return
  }

  saving.value = true

  try {
    const payload = Object.fromEntries(
      Object.entries(form).map(([key, value]) => [key, String(value).trim()])
    )

    await createStudent(payload)
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Thêm sinh viên thành công.',
      life: 3000
    })
    router.push({ name: 'students' })
  } catch (error) {
    const message = error?.response?.data?.message

    if (typeof message === 'string' && message.toLowerCase().includes('đã tồn tại')) {
      fieldErrors.StudentCode = 'MSSV này đã tồn tại trong hệ thống'
      await focusFirstInvalidField()
      return
    }

    const mappedErrors = mapServerFieldErrors(error)
    if (Object.keys(mappedErrors).length > 0) {
      Object.keys(fieldErrors).forEach((key) => delete fieldErrors[key])
      Object.assign(fieldErrors, mappedErrors)
      await focusFirstInvalidField()
      return
    }

    formError.value = extractErrorMessage(error, 'Không thể lưu sinh viên. Vui lòng thử lại.')
  } finally {
    saving.value = false
  }
}

function onBeforeUnload(event) {
  if (!hasUnsavedChanges.value) return
  event.preventDefault()
  event.returnValue = ''
}

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
  <div class="page-container student-create-page">
    <header class="student-create-header">
      <Button
        label="Quay lại"
        icon="pi pi-arrow-left"
        severity="secondary"
        text
        class="back-button"
        @click="router.push({ name: 'students' })"
      />
      <h1 class="page-title">Thêm sinh viên</h1>
    </header>

    <section class="student-create-card">
      <Message v-if="formError" severity="error" :closable="false" class="form-global-error">
        {{ formError }}
      </Message>

      <form class="student-create-form" novalidate @submit.prevent="submitForm">
        <div class="form-section">
          <div class="form-section-header">
            <h2>Thông tin cá nhân</h2>
            <p>Thông tin liên hệ cơ bản của sinh viên.</p>
          </div>

          <div class="form-grid">
            <div class="form-field" :class="{ 'has-error': Boolean(fieldErrors.StudentCode) }">
              <label for="student-code-input">MSSV <span class="required-mark">*</span></label>
              <InputText
                id="student-code-input"
                v-model="form.StudentCode"
                type="text"
                maxlength="20"
                placeholder="Ví dụ: SV2024001"
                class="student-input"
                :invalid="Boolean(fieldErrors.StudentCode)"
                :disabled="saving"
                :aria-invalid="Boolean(fieldErrors.StudentCode)"
                :aria-describedby="fieldErrors.StudentCode ? 'student-code-error' : undefined"
                @blur="validateField('StudentCode')"
                @input="clearFieldError('StudentCode')"
                :ref="(el) => setFieldRef('StudentCode', el)"
              />
              <small
                v-if="fieldErrors.StudentCode"
                id="student-code-error"
                class="field-error"
                aria-live="polite"
              >
                <i class="pi pi-exclamation-circle" aria-hidden="true" />
                {{ fieldErrors.StudentCode }}
              </small>
            </div>

            <div class="form-field" :class="{ 'has-error': Boolean(fieldErrors.FullName) }">
              <label for="full-name-input">Họ tên <span class="required-mark">*</span></label>
              <InputText
                id="full-name-input"
                v-model="form.FullName"
                maxlength="100"
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
              <small
                v-if="fieldErrors.FullName"
                id="full-name-error"
                class="field-error"
                aria-live="polite"
              >
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
              <small
                v-if="fieldErrors.Email"
                id="email-error"
                class="field-error"
                aria-live="polite"
              >
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
              <small
                v-if="fieldErrors.PhoneNumber"
                id="phone-number-error"
                class="field-error"
                aria-live="polite"
              >
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
                maxlength="100"
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
              <small
                v-if="fieldErrors.Major"
                id="major-error"
                class="field-error"
                aria-live="polite"
              >
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
              <small
                v-if="fieldErrors.ClassName"
                id="class-name-error"
                class="field-error"
                aria-live="polite"
              >
                <i class="pi pi-exclamation-circle" aria-hidden="true" />
                {{ fieldErrors.ClassName }}
              </small>
            </div>
          </div>
        </div>

        <div class="form-footer">
          <span class="required-note"><span class="required-mark">*</span> Trường bắt buộc</span>
          <div class="form-actions">
            <Button type="button" label="Hủy" severity="secondary" variant="outlined" :disabled="saving" @click="router.push({ name: 'students' })" />
            <Button type="submit" label="Lưu sinh viên" icon="pi pi-check" :loading="saving" :disabled="saving" />
          </div>
        </div>
      </form>
    </section>
  </div>
</template>

<style scoped>
.student-create-page {
  color: var(--apply-ink);
}

.student-create-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.25rem;
}

.student-create-header .page-title {
  font-size: 28px;
  line-height: 1.25;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.student-create-card {
  background: var(--apply-surface);
  border: 1px solid var(--apply-border);
  border-radius: 18px;
  box-shadow: var(--apply-shadow);
  padding: 1.5rem;
}

.student-create-form {
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

@media (max-width: 640px) {
  .student-create-header {
    margin-bottom: 1rem;
  }

  .student-create-header .page-title {
    font-size: 17px;
    font-weight: 600;
  }

  .student-create-card {
    padding: 1rem;
    border-radius: 14px;
  }

  .form-section {
    padding: 0.875rem;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .form-section-header h2 {
    font-size: 15px;
  }

  .form-footer {
    position: sticky;
    bottom: 0;
    z-index: 2;
    display: flex;
    flex-direction: column-reverse;
    align-items: stretch;
    gap: 0.75rem;
    padding: 0.75rem 0 0;
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

  .form-actions .p-button {
    flex: 1 1 0;
    min-height: 44px;
  }
}
</style>
