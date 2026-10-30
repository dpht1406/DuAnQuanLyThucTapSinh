<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { getCompanyById, updateCompany } from '../../api/companies.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateEmailAddress, validateSafeText } from '../../utils/emailValidation.js'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const saving = ref(false)
const formLoaded = ref(false)
const submitted = ref(false)
const loadError = ref('')
const formError = ref('')
const fieldErrors = reactive({})
const form = reactive({
  name: '',
  address: '',
  industry: '',
  contactPerson: '',
  contactPhone: '',
  contactEmail: '',
  contactPosition: ''
})
const initialFormValue = ref(null)
const hasUnsavedChanges = computed(() =>
  !submitted.value &&
  formLoaded.value &&
  initialFormValue.value !== null &&
  JSON.stringify(form) !== initialFormValue.value
)
const requiredFields = [
  ['name', 'Tên doanh nghiệp'],
  ['address', 'Địa chỉ'],
  ['industry', 'Ngành'],
  ['contactPerson', 'Người liên hệ'],
  ['contactEmail', 'Email']
]
const textLimits = [
  ['name', 200],
  ['address', 300],
  ['industry', 150],
  ['contactPerson', 150],
  ['contactPhone', 20],
  ['contactEmail', 150],
  ['contactPosition', 100]
]

const FIELD_IDS = {
  name: 'company-name',
  address: 'company-address',
  industry: 'company-industry',
  contactPerson: 'company-contact-person',
  contactPhone: 'company-contact-phone',
  contactEmail: 'company-contact-email',
  contactPosition: 'company-contact-position'
}

function clearFieldError(field) {
  delete fieldErrors[field]
  formError.value = ''
}

async function focusFirstInvalidField() {
  await nextTick()
  const firstField = Object.keys(fieldErrors)[0]
  if (!firstField) return
  const id = FIELD_IDS[firstField] || `company-${firstField}`
  const element = document.getElementById(id)
  if (element) {
    element.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
    element.focus?.()
  }
}

function validateForm() {
  Object.keys(fieldErrors).forEach((field) => delete fieldErrors[field])
  for (const [field, label] of requiredFields) {
    if (!String(form[field] ?? '').trim()) {
      fieldErrors[field] = `${label} không được để trống.`
      return false
    }
  }

  for (const [field, maximumLength] of textLimits) {
    const error = validateSafeText(form[field] ?? '', maximumLength)
    if (error) {
      fieldErrors[field] = error
      return false
    }
  }

  const phone = String(form.contactPhone ?? '').trim()
  const position = String(form.contactPosition ?? '').trim()
  if (phone.length > 20) {
    fieldErrors.contactPhone = 'Số điện thoại người tuyển dụng không được quá 20 ký tự.'
    return false
  }
  if (phone && !/^0\d{9}$/.test(phone)) {
    fieldErrors.contactPhone = 'Số điện thoại người tuyển dụng phải gồm 10 chữ số và bắt đầu bằng số 0.'
    return false
  }
  const emailError = validateEmailAddress(form.contactEmail, 150)
  if (emailError) {
    fieldErrors.contactEmail = emailError
    return false
  }
  if (position.length > 100) {
    fieldErrors.contactPosition = 'Chức vụ người tuyển dụng không được quá 100 ký tự.'
    return false
  }
  return true
}

async function loadCompany(id) {
  loading.value = true
  formLoaded.value = false
  submitted.value = false
  loadError.value = ''
  formError.value = ''
  Object.keys(fieldErrors).forEach((field) => delete fieldErrors[field])
  try {
    const company = await getCompanyById(id)
    Object.assign(form, {
      name: company?.name ?? '',
      address: company?.address ?? '',
      industry: company?.industry ?? '',
      contactPerson: company?.contactPerson ?? '',
      contactPhone: company?.contactPhone ?? '',
      contactEmail: company?.contactEmail ?? '',
      contactPosition: company?.contactPosition ?? ''
    })
    initialFormValue.value = JSON.stringify(form)
    formLoaded.value = true
  } catch (error) {
    loadError.value = extractErrorMessage(error, 'Không tải được thông tin doanh nghiệp.')
  } finally {
    loading.value = false
  }
}

async function submitForm() {
  if (saving.value) return
  if (!validateForm()) {
    await focusFirstInvalidField()
    return
  }
  saving.value = true
  formError.value = ''
  try {
    await updateCompany(route.params.id, { ...form })
    submitted.value = true
    if (route.query.redirect === 'account') {
      await router.push({ name: 'company-account', params: { id: route.params.id } })
    } else {
      await router.push('/companies')
    }
  } catch (error) {
    formError.value = extractErrorMessage(error, 'Có lỗi xảy ra, vui lòng thử lại.')
  } finally {
    saving.value = false
  }
}


function onBeforeUnload(event) {
  if (!hasUnsavedChanges.value) return
  event.preventDefault()
  event.returnValue = ''
}

watch(() => route.params.id, loadCompany, { immediate: true })

onBeforeRouteLeave(() => {
  if (!hasUnsavedChanges.value) return true
  return window.confirm('Bạn có thay đổi chưa lưu. Rời trang?')
})

onMounted(() => window.addEventListener('beforeunload', onBeforeUnload))
onBeforeUnmount(() => window.removeEventListener('beforeunload', onBeforeUnload))
const backTarget = computed(() =>
  route.query.redirect === 'account'
    ? { name: 'company-account', params: { id: route.params.id } }
    : { name: 'companies' }
)
</script>

<template>
  <FormPage
    title="Sửa doanh nghiệp"
    subtitle="Cập nhật thông tin doanh nghiệp và người liên hệ."
    back-label="Quay lại"
    :back-to="backTarget"
    class="company-edit-page"
  >
    <Message v-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>
    <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
    <form v-if="!loadError" class="company-edit-form" novalidate @submit.prevent="submitForm">
      <FormSection title="Thông tin doanh nghiệp" description="Tên, địa chỉ và lĩnh vực hoạt động của doanh nghiệp.">
        <FormField label="Tên doanh nghiệp *" for="company-name" :error="fieldErrors.name">
          <InputText id="company-name" v-model="form.name" maxlength="200" :disabled="loading || saving" :invalid="Boolean(fieldErrors.name)" @input="clearFieldError('name')" />
        </FormField>
        <FormField label="Địa chỉ *" for="company-address" :error="fieldErrors.address">
          <InputText id="company-address" v-model="form.address" maxlength="300" :disabled="loading || saving" :invalid="Boolean(fieldErrors.address)" @input="clearFieldError('address')" />
        </FormField>
        <FormField label="Ngành *" for="company-industry" :error="fieldErrors.industry">
          <InputText id="company-industry" v-model="form.industry" maxlength="150" :disabled="loading || saving" :invalid="Boolean(fieldErrors.industry)" @input="clearFieldError('industry')" />
        </FormField>
      </FormSection>
      <FormSection title="Người liên hệ" description="Thông tin người phụ trách tuyển dụng tại doanh nghiệp.">
        <FormField label="Người liên hệ (tên) *" for="company-contact-person" :error="fieldErrors.contactPerson">
          <InputText id="company-contact-person" v-model="form.contactPerson" maxlength="150" :disabled="loading || saving" :invalid="Boolean(fieldErrors.contactPerson)" @input="clearFieldError('contactPerson')" />
        </FormField>
        <FormField label="Số điện thoại" for="company-contact-phone" optional :error="fieldErrors.contactPhone">
          <InputText id="company-contact-phone" v-model="form.contactPhone" maxlength="20" :disabled="loading || saving" :invalid="Boolean(fieldErrors.contactPhone)" @input="clearFieldError('contactPhone')" />
        </FormField>
        <FormField label="Email *" for="company-contact-email" :error="fieldErrors.contactEmail">
          <InputText id="company-contact-email" v-model="form.contactEmail" type="email" maxlength="150" :disabled="loading || saving" :invalid="Boolean(fieldErrors.contactEmail)" @input="clearFieldError('contactEmail')" />
        </FormField>
        <FormField label="Chức vụ" for="company-contact-position" optional :error="fieldErrors.contactPosition">
          <InputText id="company-contact-position" v-model="form.contactPosition" maxlength="100" :disabled="loading || saving" :invalid="Boolean(fieldErrors.contactPosition)" @input="clearFieldError('contactPosition')" />
        </FormField>
      </FormSection>
      <FormActionBar>
        <Button label="Hủy" severity="secondary" text class="btn-cancel" :disabled="saving" @click="router.push(backTarget)" />
        <Button label="Lưu thay đổi" icon="pi pi-check" type="submit" class="btn-primary" :loading="saving" :disabled="loading || saving || !formLoaded" />
      </FormActionBar>
    </form>
  </FormPage>
</template>


<style scoped>
.company-edit-form { display: flex; flex-direction: column; gap: 8px; }
.company-edit-form :deep(.p-inputtext) { width: 100%; min-height: 44px; }
</style>
