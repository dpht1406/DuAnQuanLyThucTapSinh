<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { onBeforeRouteLeave, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { createCompany } from '../../api/companies.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateCompanyForm, mapCompanyServerErrors } from '../../utils/companyFormValidation.js'

const router = useRouter()
const toast = useToast()
const saving = ref(false)
const submitted = ref(false)
const formError = ref('')
const fieldErrors = reactive({})
const fieldRefs = {}

const form = reactive({
  name: '',
  address: '',
  industry: '',
  contactPerson: '',
  contactPhone: '',
  contactEmail: '',
  contactPosition: ''
})

const initialFormValue = JSON.stringify(form)
const hasUnsavedChanges = computed(() => !submitted.value && JSON.stringify(form) !== initialFormValue)

function setFieldRef(name, instance) {
  fieldRefs[name] = instance?.$el ?? instance
}

function clearFieldError(name) {
  delete fieldErrors[name]
  if (formError.value) formError.value = ''
}

function fieldId(name) {
  return `company-${name}-input`
}

function validateField(name) {
  const errors = validateCompanyForm(form)
  if (errors[name]) fieldErrors[name] = errors[name]
  else delete fieldErrors[name]
}

async function focusFirstInvalidField() {
  await nextTick()
  const firstField = Object.keys(fieldErrors)[0]
  if (!firstField) return

  const element = fieldRefs[firstField] || document.getElementById(fieldId(firstField))
  if (!element) return

  element.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
  element.focus?.()
}

async function submitForm() {
  if (saving.value) return

  const errors = validateCompanyForm(form)
  Object.keys(fieldErrors).forEach((key) => delete fieldErrors[key])
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

    await createCompany(payload)
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Thêm doanh nghiệp thành công.',
      life: 3000
    })
    router.push('/companies')
  } catch (error) {
    const mappedErrors = mapCompanyServerErrors(error)
    const mappedFormError = mappedErrors._form
    if (mappedFormError) delete mappedErrors._form

    if (Object.keys(mappedErrors).length > 0) {
      Object.keys(fieldErrors).forEach((key) => delete fieldErrors[key])
      Object.assign(fieldErrors, mappedErrors)
      if (mappedFormError) formError.value = mappedFormError
      await focusFirstInvalidField()
      return
    }

    formError.value = mappedFormError || extractErrorMessage(error, 'Không thể lưu doanh nghiệp. Vui lòng thử lại.')
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
  <div class="page-container company-create-page">
    <header class="company-create-header">
      <Button
        label="Quay lại"
        icon="pi pi-arrow-left"
        severity="secondary"
        text
        class="back-button"
        @click="router.push('/companies')"
      />
      <h1 class="page-title">Thêm doanh nghiệp</h1>
    </header>

    <section class="company-create-card">
      <Message v-if="formError" severity="error" :closable="false" class="form-global-error">
        {{ formError }}
      </Message>

      <form class="company-create-form" novalidate @submit.prevent="submitForm">
        <div class="form-section">
          <div class="form-section-header">
            <h2>Thông tin doanh nghiệp</h2>
            <p>Tên, địa chỉ và lĩnh vực hoạt động của doanh nghiệp.</p>
          </div>

          <div class="form-grid">
            <div class="form-field full-width">
              <label for="company-name">Tên doanh nghiệp <span class="required-mark" aria-hidden="true">*</span></label>
              <InputText
                id="company-name"
                :ref="(element) => setFieldRef('name', element)"
                v-model="form.name"
                maxlength="200"
                placeholder="Nhập tên doanh nghiệp"
                :disabled="saving"
                :invalid="Boolean(fieldErrors.name)"
                :aria-invalid="Boolean(fieldErrors.name)"
                :aria-describedby="fieldErrors.name ? 'company-name-error' : undefined"
                @input="clearFieldError('name')"
                @blur="validateField('name')"
              />
              <small id="company-name-error" class="field-error" aria-live="polite">
                <template v-if="fieldErrors.name">
                  <i class="pi pi-exclamation-circle" aria-hidden="true" />{{ fieldErrors.name }}
                </template>
              </small>
            </div>

            <div class="form-field full-width">
              <label for="company-address">Địa chỉ <span class="required-mark" aria-hidden="true">*</span></label>
              <InputText
                id="company-address"
                :ref="(element) => setFieldRef('address', element)"
                v-model="form.address"
                maxlength="300"
                placeholder="Số nhà, đường, quận/huyện, tỉnh/thành"
                :disabled="saving"
                :invalid="Boolean(fieldErrors.address)"
                :aria-invalid="Boolean(fieldErrors.address)"
                :aria-describedby="fieldErrors.address ? 'company-address-error' : undefined"
                @input="clearFieldError('address')"
                @blur="validateField('address')"
              />
              <small id="company-address-error" class="field-error" aria-live="polite">
                <template v-if="fieldErrors.address">
                  <i class="pi pi-exclamation-circle" aria-hidden="true" />{{ fieldErrors.address }}
                </template>
              </small>
            </div>

            <div class="form-field full-width">
              <label for="company-industry">Ngành <span class="required-mark" aria-hidden="true">*</span></label>
              <InputText
                id="company-industry"
                :ref="(element) => setFieldRef('industry', element)"
                v-model="form.industry"
                maxlength="150"
                placeholder="Ví dụ: Công nghệ thông tin"
                :disabled="saving"
                :invalid="Boolean(fieldErrors.industry)"
                :aria-invalid="Boolean(fieldErrors.industry)"
                :aria-describedby="fieldErrors.industry ? 'company-industry-error' : undefined"
                @input="clearFieldError('industry')"
                @blur="validateField('industry')"
              />
              <small id="company-industry-error" class="field-error" aria-live="polite">
                <template v-if="fieldErrors.industry">
                  <i class="pi pi-exclamation-circle" aria-hidden="true" />{{ fieldErrors.industry }}
                </template>
              </small>
            </div>
          </div>
        </div>

        <div class="form-section">
          <div class="form-section-header">
            <h2>Thông tin người tuyển dụng</h2>
            <p>Người phụ trách liên hệ với sinh viên và nhà trường.</p>
          </div>

          <div class="form-grid">
            <div class="form-field">
              <label for="company-contact-person">Người liên hệ <span class="required-mark" aria-hidden="true">*</span></label>
              <InputText
                id="company-contact-person"
                :ref="(element) => setFieldRef('contactPerson', element)"
                v-model="form.contactPerson"
                maxlength="150"
                placeholder="Nhập họ và tên người liên hệ"
                :disabled="saving"
                :invalid="Boolean(fieldErrors.contactPerson)"
                :aria-invalid="Boolean(fieldErrors.contactPerson)"
                :aria-describedby="fieldErrors.contactPerson ? 'company-contact-person-error' : undefined"
                @input="clearFieldError('contactPerson')"
                @blur="validateField('contactPerson')"
              />
              <small id="company-contact-person-error" class="field-error" aria-live="polite">
                <template v-if="fieldErrors.contactPerson">
                  <i class="pi pi-exclamation-circle" aria-hidden="true" />{{ fieldErrors.contactPerson }}
                </template>
              </small>
            </div>

            <div class="form-field">
              <label for="company-contact-position">Chức vụ <span class="field-optional">(tuỳ chọn)</span></label>
              <InputText
                id="company-contact-position"
                :ref="(element) => setFieldRef('contactPosition', element)"
                v-model="form.contactPosition"
                maxlength="100"
                placeholder="Ví dụ: Trưởng phòng nhân sự"
                :disabled="saving"
                :invalid="Boolean(fieldErrors.contactPosition)"
                :aria-invalid="Boolean(fieldErrors.contactPosition)"
                :aria-describedby="fieldErrors.contactPosition ? 'company-contact-position-error' : undefined"
                @input="clearFieldError('contactPosition')"
                @blur="validateField('contactPosition')"
              />
              <small id="company-contact-position-error" class="field-error" aria-live="polite">
                <template v-if="fieldErrors.contactPosition">
                  <i class="pi pi-exclamation-circle" aria-hidden="true" />{{ fieldErrors.contactPosition }}
                </template>
              </small>
            </div>

            <div class="form-field">
              <label for="company-contact-phone">Số điện thoại <span class="field-optional">(tuỳ chọn)</span></label>
              <InputText
                id="company-contact-phone"
                :ref="(element) => setFieldRef('contactPhone', element)"
                v-model="form.contactPhone"
                type="tel"
                maxlength="10"
                inputmode="numeric"
                placeholder="0912 345 678"
                :disabled="saving"
                :invalid="Boolean(fieldErrors.contactPhone)"
                :aria-invalid="Boolean(fieldErrors.contactPhone)"
                :aria-describedby="fieldErrors.contactPhone ? 'company-contact-phone-error' : undefined"
                @input="clearFieldError('contactPhone')"
                @blur="validateField('contactPhone')"
              />
              <small id="company-contact-phone-error" class="field-error" aria-live="polite">
                <template v-if="fieldErrors.contactPhone">
                  <i class="pi pi-exclamation-circle" aria-hidden="true" />{{ fieldErrors.contactPhone }}
                </template>
              </small>
            </div>

            <div class="form-field">
              <label for="company-contact-email">Email <span class="field-optional">(tuỳ chọn)</span></label>
              <InputText
                id="company-contact-email"
                :ref="(element) => setFieldRef('contactEmail', element)"
                v-model="form.contactEmail"
                type="email"
                maxlength="150"
                placeholder="ten@congty.com"
                :disabled="saving"
                :invalid="Boolean(fieldErrors.contactEmail)"
                :aria-invalid="Boolean(fieldErrors.contactEmail)"
                :aria-describedby="fieldErrors.contactEmail ? 'company-contact-email-error' : undefined"
                @input="clearFieldError('contactEmail')"
                @blur="validateField('contactEmail')"
              />
              <small id="company-contact-email-error" class="field-error" aria-live="polite">
                <template v-if="fieldErrors.contactEmail">
                  <i class="pi pi-exclamation-circle" aria-hidden="true" />{{ fieldErrors.contactEmail }}
                </template>
              </small>
            </div>
          </div>
        </div>

        <footer class="form-footer">
          <span class="required-note">* Trường bắt buộc</span>
          <div class="form-actions">
            <Button type="button" label="Hủy" severity="secondary" variant="outlined" :disabled="saving" @click="router.push('/companies')" />
            <Button type="submit" label="Lưu doanh nghiệp" icon="pi pi-check" :loading="saving" :disabled="saving" />
          </div>
        </footer>
      </form>
    </section>
  </div>
</template>

<style scoped>
.company-create-page {
  width: min(100%, 800px);
  max-width: 800px;
  padding-top: 24px;
  color: var(--apply-ink);
}

.company-create-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
}

.company-create-header .page-title {
  font-size: 28px;
  line-height: 1.25;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.company-create-card {
  background: var(--apply-surface);
  border: 1px solid var(--apply-border);
  border-radius: 18px;
  box-shadow: var(--apply-shadow);
  padding: 32px;
}

.company-create-form {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.form-section {
  display: flex;
  flex-direction: column;
  gap: 18px;
  padding: 20px;
  border: 1px solid var(--apply-border);
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.5);
}

.form-section-header {
  display: flex;
  flex-direction: column;
  gap: 4px;
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
  gap: 20px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-field.full-width {
  grid-column: 1 / -1;
}

.form-field label {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: var(--apply-ink);
  font-size: 13.5px;
  font-weight: 600;
  line-height: 1.4;
}

.required-mark {
  color: var(--apply-error);
}

.field-optional {
  color: var(--apply-muted);
  font-size: 11.5px;
  font-weight: 500;
}

:deep(.p-inputtext),
:deep(.p-inputtext:focus) {
  min-height: 46px;
  border-radius: 10px;
  border: 1px solid var(--apply-border);
  background: var(--apply-surface);
  color: var(--apply-ink);
  font-size: 15px;
  font-weight: 400;
  transition: border-color 0.15s ease, box-shadow 0.15s ease, background-color 0.15s ease;
}

:deep(.p-inputtext::placeholder) {
  color: var(--apply-placeholder);
  font-size: 14px;
}

:deep(.p-inputtext:focus) {
  border-color: var(--apply-primary);
  box-shadow: var(--apply-focus-ring);
}

:deep(.p-inputtext.p-invalid) {
  border-color: var(--apply-error);
  background: var(--apply-danger-soft);
}

:deep(.p-inputtext.p-invalid:focus) {
  border-color: var(--apply-error);
  box-shadow: var(--apply-error-ring);
}

.field-error {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  min-height: 18px;
  margin: 0;
  color: var(--apply-error);
  font-size: 12.5px;
  line-height: 1.5;
  font-weight: 500;
}

.field-error i {
  font-size: 12px;
}

.form-global-error {
  margin-bottom: 16px;
}

.form-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding-top: 8px;
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
  gap: 12px;
}

.form-actions :deep(.p-button) {
  min-height: 46px;
  border-radius: 10px;
}

@media (max-width: 1023px) {
  .company-create-page {
    width: 100%;
    max-width: 800px;
  }
}

@media (max-width: 640px) {
  .company-create-page {
    width: 100%;
    max-width: none;
    padding: 16px 0 92px;
  }

  .company-create-header {
    margin-bottom: 12px;
  }

  .company-create-header .page-title {
    font-size: 17px;
    font-weight: 600;
  }

  .company-create-card {
    background: transparent;
    border: 0;
    box-shadow: none;
    padding: 0;
  }

  .company-create-form {
    gap: 14px;
  }

  .form-section {
    padding: 16px;
    border-radius: 16px;
    background: var(--apply-surface);
    border-color: var(--apply-border);
  }

  .form-grid {
    grid-template-columns: 1fr;
    gap: 14px;
  }

  .form-section-header {
    display: none;
  }

  .form-footer {
    position: fixed;
    right: 0;
    bottom: 0;
    left: 0;
    z-index: 30;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 12px 12px calc(12px + env(safe-area-inset-bottom));
    background: rgba(255, 255, 255, 0.96);
    border-top: 1px solid var(--apply-border);
    backdrop-filter: blur(6px);
  }

  .required-note {
    display: none;
  }

  .form-actions {
    width: 100%;
    display: flex;
    gap: 12px;
  }

  .form-actions :deep(.p-button) {
    flex: 1 1 0;
    height: 48px;
  }

  .form-actions :deep(.p-button-primary) {
    min-width: 0;
  }
}
</style>
