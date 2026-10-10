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
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import FormField from '../../components/form/FormField.vue'
import FormActionBar from '../../components/form/FormActionBar.vue'

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
  <FormPage
    title="Thêm sinh viên"
    subtitle="Tất cả các trường đều bắt buộc"
    back-label="Sinh viên"
    :back-to="{ name: 'students' }"
    class="student-create-page"
  >
    <Message v-if="formError" severity="error" :closable="false" class="form-global-error">
      {{ formError }}
    </Message>

    <form class="student-create-form" novalidate @submit.prevent="submitForm">
      <FormSection
        title="Thông tin cá nhân"
        description="Thông tin liên hệ cơ bản của sinh viên."
      >
        <div class="fields-grid">
          <FormField
            label="MSSV"
            for="student-code-input"
            :error="fieldErrors.StudentCode"
          >
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
          </FormField>

          <FormField
            label="Họ tên"
            for="full-name-input"
            :error="fieldErrors.FullName"
          >
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
          </FormField>

          <FormField
            label="Email"
            for="email-input"
            :error="fieldErrors.Email"
          >
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
          </FormField>

          <FormField
            label="Số điện thoại"
            for="phone-number-input"
            :error="fieldErrors.PhoneNumber"
          >
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
          </FormField>
        </div>
      </FormSection>

      <FormSection
        title="Thông tin học tập"
        description="Ngành và lớp sinh viên đang theo học."
      >
        <div class="fields-grid">
          <FormField
            label="Ngành"
            for="major-input"
            :error="fieldErrors.Major"
          >
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
          </FormField>

          <FormField
            label="Lớp"
            for="class-name-input"
            :error="fieldErrors.ClassName"
          >
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
          </FormField>
        </div>
      </FormSection>

      <FormActionBar>
        <Button
          type="button"
          label="Hủy"
          class="btn-cancel"
          :disabled="saving"
          @click="router.push({ name: 'students' })"
        />
        <Button
          type="submit"
          label="Lưu sinh viên"
          class="btn-primary"
          :loading="saving"
          :disabled="saving"
        />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.student-create-form {
  display: flex;
  flex-direction: column;
}

.form-global-error {
  margin-bottom: 1.5rem;
  border-radius: var(--ui-radius-sm);
}

.fields-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px 20px;
}

:deep(.student-input.p-inputtext),
.student-input {
  box-sizing: border-box;
  width: 100%;
  height: 40px;
  padding: 0 12px;
  font-family: var(--ui-font-sans);
  font-size: 14.5px;
  color: var(--ui-ink);
  background: var(--ui-surface);
  border: 1px solid var(--ui-border-strong);
  border-radius: var(--ui-radius-sm);
  transition: var(--ui-transition);
}

:deep(.student-input.p-inputtext::placeholder),
.student-input::placeholder {
  color: var(--ui-placeholder);
  font-size: 14px;
}

:deep(.student-input.p-inputtext:hover),
.student-input:hover {
  border-color: var(--ui-ink);
}

:deep(.student-input.p-inputtext:focus),
.student-input:focus {
  border-color: var(--ui-primary-600);
  outline: 2px solid var(--ui-primary-600);
  outline-offset: 1px;
}

:deep(.student-input.p-inputtext.p-invalid),
:deep(.student-input.p-inputtext[aria-invalid="true"]),
.student-input[aria-invalid="true"] {
  border-color: var(--ui-error);
  background: var(--ui-error-bg);
}

:deep(.student-input.p-inputtext.p-invalid:focus),
:deep(.student-input.p-inputtext[aria-invalid="true"]:focus),
.student-input[aria-invalid="true"]:focus {
  border-color: var(--ui-error);
  outline: 2px solid var(--ui-error);
  outline-offset: 1px;
}

@media (max-width: 640px) {
  .fields-grid {
    grid-template-columns: 1fr;
    gap: 16px;
  }

  :deep(.student-input.p-inputtext),
  .student-input {
    height: 44px;
    font-size: 15px;
  }
}
</style>
