<script setup>
import { computed, nextTick, reactive, ref, watch } from 'vue'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'
import { createPlacementRequest, getMyProfile } from '../../api/me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { LIMITS, mapServerErrors, validateApplication } from '../../utils/applicationValidation.js'

const props = defineProps({
  visible: { type: Boolean, default: false },
  position: { type: Object, default: null }
})
const emit = defineEmits(['update:visible', 'success'])
const toast = useToast()
const form = ref(createEmptyForm())
const fieldErrors = reactive({})
const formError = ref('')
const profileWarning = ref('')
const profileLoading = ref(false)
const saving = ref(false)
const fieldRefs = {}
const invalidFieldCount = computed(() => Object.keys(fieldErrors).length)
let profileLoadId = 0

function createEmptyForm() {
  return {
    applicantFullName: '',
    applicantEmail: '',
    applicantPhone: '',
    applicantSchool: '',
    applicantMajor: '',
    cvUrl: '',
    coverLetter: ''
  }
}

function setFieldRef(name, instance) {
  fieldRefs[name] = instance
}

function clearFieldError(name) {
  delete fieldErrors[name]
  formError.value = ''
}

async function focusFirstInvalidField() {
  await nextTick()
  const field = Object.keys(fieldErrors)[0]
  const element = fieldRefs[field]?.$el ?? fieldRefs[field]
  element?.focus?.()
}

function validateField(name) {
  const errors = validateApplication(form.value)
  if (errors[name]) fieldErrors[name] = errors[name]
  else delete fieldErrors[name]
}

function updateVisible(visible) {
  if (!saving.value) emit('update:visible', visible)
}

watch(() => props.visible, async (visible) => {
  profileLoadId += 1
  if (!visible) return

  const currentLoadId = profileLoadId
  form.value = createEmptyForm()
  Object.keys(fieldErrors).forEach((field) => delete fieldErrors[field])
  formError.value = ''
  profileWarning.value = ''
  profileLoading.value = true

  try {
    const profile = await getMyProfile()
    if (currentLoadId !== profileLoadId) return
    form.value.applicantFullName = profile?.fullName ?? ''
    form.value.applicantEmail = profile?.email ?? ''
    form.value.applicantPhone = profile?.phoneNumber ?? ''
    form.value.applicantMajor = profile?.major ?? ''
  } catch (error) {
    if (currentLoadId === profileLoadId) {
      profileWarning.value = extractErrorMessage(error, 'Không tải được hồ sơ. Bạn vẫn có thể nhập thông tin ứng tuyển thủ công.')
    }
  } finally {
    if (currentLoadId === profileLoadId) profileLoading.value = false
  }
})

async function submit() {
  if (!props.position || saving.value) return

  Object.keys(fieldErrors).forEach((field) => delete fieldErrors[field])
  formError.value = ''
  Object.assign(fieldErrors, validateApplication(form.value))
  if (invalidFieldCount.value > 0) {
    await focusFirstInvalidField()
    return
  }

  saving.value = true
  try {
    const payload = Object.fromEntries(
      Object.entries(form.value).map(([field, value]) => [field, value.trim()])
    )
    await createPlacementRequest({
      companyId: props.position.companyId,
      jobPositionId: props.position.id,
      ...payload
    })
    emit('update:visible', false)
    toast.add({ severity: 'success', summary: 'Thành công', detail: 'Nộp đơn thành công.', life: 3000 })
    emit('success')
  } catch (error) {
    const serverErrors = mapServerErrors(error)
    for (const [field, message] of Object.entries(serverErrors)) {
      if (field === '_form') formError.value = message
      else fieldErrors[field] = message
    }
    if (invalidFieldCount.value > 0) await focusFirstInvalidField()
    else if (!formError.value) {
      formError.value = error?.response?.data?.message
        || extractErrorMessage(error, 'Không thể nộp đơn, vui lòng thử lại.')
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    class="app-fullscreen-dialog"
    header="Đơn ứng tuyển"
    :style="{ width: 'min(42rem, calc(100vw - 2rem))' }"
    :closable="!saving"
    @update:visible="updateVisible"
  >
    <Message v-if="formError" severity="error" :closable="false" role="alert">{{ formError }}</Message>
    <Message v-if="invalidFieldCount" severity="error" :closable="false" role="alert">
      Vui lòng kiểm tra {{ invalidFieldCount }} trường chưa hợp lệ.
    </Message>
    <Message v-if="profileLoading" severity="info" :closable="false" role="status">
      Đang tải thông tin hồ sơ...
    </Message>
    <Message v-if="profileWarning" severity="warn" :closable="false" role="status">
      {{ profileWarning }}
    </Message>

    <form v-if="position" class="apply-form" novalidate @submit.prevent="submit">
      <div class="form-field">
        <label for="apply-company">Công ty</label>
        <InputText id="apply-company" :model-value="position.companyName" readonly />
      </div>
      <div class="form-field">
        <label for="apply-position">Vị trí</label>
        <InputText id="apply-position" :model-value="position.title" readonly />
      </div>

      <div class="form-field">
        <label for="apply-full-name">Họ tên</label>
        <InputText id="apply-full-name" :ref="(element) => setFieldRef('applicantFullName', element)" v-model="form.applicantFullName" autocomplete="name" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantFullName }" :aria-invalid="Boolean(fieldErrors.applicantFullName)" :aria-describedby="fieldErrors.applicantFullName ? 'apply-full-name-error' : undefined" @input="clearFieldError('applicantFullName')" @blur="validateField('applicantFullName')" />
        <small v-if="fieldErrors.applicantFullName" id="apply-full-name-error" class="field-error">{{ fieldErrors.applicantFullName }}</small>
      </div>

      <div class="form-field">
        <label for="apply-email">Email</label>
        <InputText id="apply-email" :ref="(element) => setFieldRef('applicantEmail', element)" v-model="form.applicantEmail" type="email" autocomplete="email" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantEmail }" :aria-invalid="Boolean(fieldErrors.applicantEmail)" :aria-describedby="fieldErrors.applicantEmail ? 'apply-email-error' : undefined" @input="clearFieldError('applicantEmail')" @blur="validateField('applicantEmail')" />
        <small v-if="fieldErrors.applicantEmail" id="apply-email-error" class="field-error">{{ fieldErrors.applicantEmail }}</small>
      </div>

      <div class="form-field">
        <label for="apply-phone">Số điện thoại</label>
        <InputText id="apply-phone" :ref="(element) => setFieldRef('applicantPhone', element)" v-model="form.applicantPhone" type="tel" autocomplete="tel" inputmode="numeric" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantPhone }" :aria-invalid="Boolean(fieldErrors.applicantPhone)" :aria-describedby="fieldErrors.applicantPhone ? 'apply-phone-error' : undefined" @input="clearFieldError('applicantPhone')" @blur="validateField('applicantPhone')" />
        <small v-if="fieldErrors.applicantPhone" id="apply-phone-error" class="field-error">{{ fieldErrors.applicantPhone }}</small>
      </div>

      <div class="form-field">
        <label for="apply-school">Trường</label>
        <InputText id="apply-school" :ref="(element) => setFieldRef('applicantSchool', element)" v-model="form.applicantSchool" autocomplete="organization" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantSchool }" :aria-invalid="Boolean(fieldErrors.applicantSchool)" :aria-describedby="fieldErrors.applicantSchool ? 'apply-school-error' : undefined" @input="clearFieldError('applicantSchool')" @blur="validateField('applicantSchool')" />
        <small v-if="fieldErrors.applicantSchool" id="apply-school-error" class="field-error">{{ fieldErrors.applicantSchool }}</small>
      </div>

      <div class="form-field">
        <label for="apply-major">Ngành</label>
        <InputText id="apply-major" :ref="(element) => setFieldRef('applicantMajor', element)" v-model="form.applicantMajor" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantMajor }" :aria-invalid="Boolean(fieldErrors.applicantMajor)" :aria-describedby="fieldErrors.applicantMajor ? 'apply-major-error' : undefined" @input="clearFieldError('applicantMajor')" @blur="validateField('applicantMajor')" />
        <small v-if="fieldErrors.applicantMajor" id="apply-major-error" class="field-error">{{ fieldErrors.applicantMajor }}</small>
      </div>

      <div class="form-field">
        <label for="apply-cv-url">Liên kết CV</label>
        <InputText id="apply-cv-url" :ref="(element) => setFieldRef('cvUrl', element)" v-model="form.cvUrl" type="url" placeholder="https://..." autocomplete="url" :maxlength="LIMITS.cvUrl" :disabled="saving" :class="{ 'p-invalid': fieldErrors.cvUrl }" :aria-invalid="Boolean(fieldErrors.cvUrl)" :aria-describedby="fieldErrors.cvUrl ? 'apply-cv-url-error' : undefined" @input="clearFieldError('cvUrl')" @blur="validateField('cvUrl')" />
        <small v-if="fieldErrors.cvUrl" id="apply-cv-url-error" class="field-error">{{ fieldErrors.cvUrl }}</small>
      </div>

      <div class="form-field">
        <label for="apply-cover-letter">Lời giới thiệu</label>
        <Textarea id="apply-cover-letter" :ref="(element) => setFieldRef('coverLetter', element)" v-model="form.coverLetter" rows="5" :maxlength="LIMITS.coverLetter[1]" :disabled="saving" :class="{ 'p-invalid': fieldErrors.coverLetter }" :aria-invalid="Boolean(fieldErrors.coverLetter)" :aria-describedby="fieldErrors.coverLetter ? 'apply-cover-letter-error' : undefined" @input="clearFieldError('coverLetter')" @blur="validateField('coverLetter')" />
        <small v-if="fieldErrors.coverLetter" id="apply-cover-letter-error" class="field-error">{{ fieldErrors.coverLetter }}</small>
        <small class="character-count">{{ form.coverLetter.length }}/{{ LIMITS.coverLetter[1] }} ký tự, tối thiểu {{ LIMITS.coverLetter[0] }}</small>
      </div>

      <div class="dialog-actions">
        <Button type="button" label="Hủy" severity="secondary" text :disabled="saving" @click="updateVisible(false)" />
        <Button type="submit" label="Gửi đơn" icon="pi pi-send" :loading="saving" />
      </div>
    </form>
  </Dialog>
</template>

<style scoped>
.apply-form { display: grid; gap: 1rem; }
.form-field { display: grid; min-width: 0; gap: .35rem; }
.form-field label { color: var(--text-h); font-weight: 600; }
.form-field :deep(.p-inputtext), .form-field :deep(.p-textarea) { width: 100%; }
.field-error { color: var(--red-500); }
.character-count { color: var(--text); text-align: right; }
.dialog-actions { display: flex; justify-content: flex-end; gap: .75rem; margin-top: .25rem; }
:global(.p-dialog.app-fullscreen-dialog.p-component) { width: min(42rem, calc(100vw - 2rem)) !important; max-width: min(42rem, calc(100vw - 2rem)) !important; max-height: calc(100dvh - 2rem) !important; }
:global(.p-dialog.app-fullscreen-dialog.p-component .p-dialog-content) { min-height: 0; overflow-y: auto; }
@media (max-width: 767px) {
  .dialog-actions { flex-direction: column-reverse; align-items: stretch; }
  .dialog-actions :deep(.p-button) { min-height: 44px; }
  :global(.p-dialog.app-fullscreen-dialog.p-component) { height: auto !important; max-height: calc(100dvh - 2rem) !important; margin: 1rem; border-radius: var(--radius-md) !important; }
  :global(.p-dialog.app-fullscreen-dialog.p-component .dialog-actions) { position: sticky; bottom: 0; z-index: 1; padding: 12px 0 max(12px, env(safe-area-inset-bottom)); background: var(--surface); }
}
</style>