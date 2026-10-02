<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Textarea from 'primevue/textarea'
import { createPlacementRequest, getMyProfile } from '../../api/me.js'
import { getJobPositionById } from '../../api/job-positions.js'
import { LIMITS, mapServerErrors, resolveReturnPath, validateApplication } from '../../utils/applicationValidation.js'
import { canApply, daysLeft, formatDeadline } from '../../utils/jobPosition.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const position = ref(null)
const loading = ref(true)
const loadError = ref('')
const notFound = ref(false)
const profileWarning = ref('')
const form = ref(createEmptyForm())
const fieldErrors = reactive({})
const formError = ref('')
const saving = ref(false)
const submitted = ref(false)
const formInitialized = ref(false)
const initialFormValue = ref('')
const fieldRefs = {}
let loadId = 0

const invalidFieldCount = computed(() => Object.keys(fieldErrors).length)
const returnPath = computed(() => resolveReturnPath(route.query.from))
const hasUnsavedChanges = computed(() => formInitialized.value && !submitted.value && JSON.stringify(form.value) !== initialFormValue.value)

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

async function loadData() {
  const currentLoadId = ++loadId
  loading.value = true
  loadError.value = ''
  notFound.value = false
  position.value = null
  profileWarning.value = ''
  form.value = createEmptyForm()
  formInitialized.value = false
  Object.keys(fieldErrors).forEach((field) => delete fieldErrors[field])
  formError.value = ''

  const [positionResult, profileResult] = await Promise.allSettled([
    getJobPositionById(route.params.id),
    getMyProfile()
  ])
  if (currentLoadId !== loadId) return

  if (positionResult.status === 'rejected') {
    if (positionResult.reason?.response?.status === 404) notFound.value = true
    else loadError.value = 'Không tải được vị trí thực tập. Vui lòng thử lại.'
  } else {
    position.value = positionResult.value
    if (profileResult.status === 'fulfilled') {
      form.value.applicantFullName = profileResult.value?.fullName ?? ''
      form.value.applicantEmail = profileResult.value?.email ?? ''
      form.value.applicantPhone = profileResult.value?.phoneNumber ?? ''
      form.value.applicantMajor = profileResult.value?.major ?? ''
    } else {
      profileWarning.value = 'Không tải được hồ sơ. Bạn vẫn có thể nhập thông tin ứng tuyển thủ công.'
    }
    initialFormValue.value = JSON.stringify(form.value)
    formInitialized.value = true
  }

  loading.value = false
}

async function submit() {
  if (!position.value || saving.value || !canApply(position.value)) return

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
      companyId: position.value.companyId,
      jobPositionId: position.value.id,
      ...payload
    })
    submitted.value = true
    toast.add({ severity: 'success', summary: 'Thành công', detail: 'Nộp đơn thành công.', life: 3000 })
    await router.push(returnPath.value)
  } catch (error) {
    const serverErrors = mapServerErrors(error)
    for (const [field, message] of Object.entries(serverErrors)) {
      if (field === '_form') formError.value = message
      else fieldErrors[field] = message
    }
    if (invalidFieldCount.value > 0) await focusFirstInvalidField()
    else if (!formError.value) formError.value = 'Không thể nộp đơn, vui lòng thử lại.'
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
  return window.confirm('Bạn có thay đổi chưa gửi. Rời trang?')
})

watch(() => route.params.id, loadData, { immediate: true })
onMounted(() => window.addEventListener('beforeunload', onBeforeUnload))
onBeforeUnmount(() => window.removeEventListener('beforeunload', onBeforeUnload))
</script>

<template>
  <div class="page-container apply-page">
    <header class="apply-page-header">
      <Button label="Quay lại" icon="pi pi-arrow-left" severity="secondary" text @click="router.push(returnPath)" />
      <h1 class="page-title">Ứng tuyển vị trí thực tập</h1>
    </header>

    <section v-if="loading" class="apply-state" role="status" aria-label="Đang tải thông tin">
      <Skeleton width="35%" height="1.4rem" />
      <Skeleton width="70%" height="2rem" />
      <Skeleton width="100%" height="6rem" />
      <Skeleton width="100%" height="12rem" />
    </section>

    <section v-else-if="notFound" class="apply-state">
      <Message severity="error" :closable="false">Không tìm thấy vị trí thực tập.</Message>
      <Button label="Quay lại danh sách vị trí" icon="pi pi-arrow-left" severity="secondary" @click="router.push(returnPath)" />
    </section>

    <section v-else-if="loadError" class="apply-state">
      <Message severity="error" :closable="false">{{ loadError }}</Message>
      <Button label="Thử lại" icon="pi pi-refresh" @click="loadData" />
    </section>

    <section v-else-if="position && !canApply(position)" class="apply-state">
      <Message severity="warn" :closable="false">
        {{ position.isExpired ? 'Vị trí thực tập đã hết hạn nhận hồ sơ.' : 'Vị trí thực tập đã đóng.' }}
      </Message>
      <Button label="Quay lại danh sách vị trí" icon="pi pi-arrow-left" severity="secondary" @click="router.push(returnPath)" />
    </section>

    <template v-else-if="position">
      <section class="position-summary" aria-label="Vị trí ứng tuyển">
        <p class="summary-company">{{ position.companyName }}</p>
        <h2>{{ position.title }}</h2>
        <div class="summary-details">
          <span><i class="pi pi-sitemap" />{{ position.department || 'Chưa cập nhật phòng ban' }}</span>
          <span><i class="pi pi-map-marker" />{{ position.displayLocation || position.location || 'Chưa cập nhật địa điểm' }}</span>
          <span><i class="pi pi-calendar" />Hạn nộp: {{ formatDeadline(position.deadline) }}<template v-if="daysLeft(position.deadline) !== null"> · còn {{ daysLeft(position.deadline) }} ngày</template></span>
        </div>
      </section>

      <Message v-if="profileWarning" severity="warn" :closable="false" role="status">{{ profileWarning }}</Message>
      <Message v-if="formError" severity="error" :closable="false" role="alert">{{ formError }}</Message>
      <Message v-if="invalidFieldCount" severity="error" :closable="false" role="alert">
        Vui lòng kiểm tra {{ invalidFieldCount }} trường chưa hợp lệ.
      </Message>

      <p class="prefill-note">Thông tin được điền sẵn từ hồ sơ, bạn có thể chỉnh sửa; việc chỉnh sửa chỉ áp dụng cho đơn này.</p>
      <form class="apply-form" novalidate @submit.prevent="submit">
        <section class="form-section">
          <h2>Thông tin cá nhân</h2>
          <div class="form-grid">
            <div class="form-field">
              <label for="apply-full-name">Họ tên <span aria-hidden="true">*</span></label>
              <InputText id="apply-full-name" :ref="(element) => setFieldRef('applicantFullName', element)" v-model="form.applicantFullName" autocomplete="name" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantFullName }" :aria-invalid="Boolean(fieldErrors.applicantFullName)" :aria-describedby="fieldErrors.applicantFullName ? 'apply-full-name-error' : undefined" @input="clearFieldError('applicantFullName')" @blur="validateField('applicantFullName')" />
              <small v-if="fieldErrors.applicantFullName" id="apply-full-name-error" class="field-error">{{ fieldErrors.applicantFullName }}</small>
            </div>
            <div class="form-field">
              <label for="apply-email">Email <span aria-hidden="true">*</span></label>
              <InputText id="apply-email" :ref="(element) => setFieldRef('applicantEmail', element)" v-model="form.applicantEmail" type="email" autocomplete="email" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantEmail }" :aria-invalid="Boolean(fieldErrors.applicantEmail)" :aria-describedby="fieldErrors.applicantEmail ? 'apply-email-error' : undefined" @input="clearFieldError('applicantEmail')" @blur="validateField('applicantEmail')" />
              <small v-if="fieldErrors.applicantEmail" id="apply-email-error" class="field-error">{{ fieldErrors.applicantEmail }}</small>
            </div>
            <div class="form-field">
              <label for="apply-phone">Số điện thoại <span aria-hidden="true">*</span></label>
              <InputText id="apply-phone" :ref="(element) => setFieldRef('applicantPhone', element)" v-model="form.applicantPhone" type="tel" autocomplete="tel" inputmode="numeric" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantPhone }" :aria-invalid="Boolean(fieldErrors.applicantPhone)" :aria-describedby="fieldErrors.applicantPhone ? 'apply-phone-error' : undefined" @input="clearFieldError('applicantPhone')" @blur="validateField('applicantPhone')" />
              <small v-if="fieldErrors.applicantPhone" id="apply-phone-error" class="field-error">{{ fieldErrors.applicantPhone }}</small>
            </div>
            <div class="form-field">
              <label for="apply-school">Trường <span aria-hidden="true">*</span></label>
              <InputText id="apply-school" :ref="(element) => setFieldRef('applicantSchool', element)" v-model="form.applicantSchool" autocomplete="organization" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantSchool }" :aria-invalid="Boolean(fieldErrors.applicantSchool)" :aria-describedby="fieldErrors.applicantSchool ? 'apply-school-error' : undefined" @input="clearFieldError('applicantSchool')" @blur="validateField('applicantSchool')" />
              <small v-if="fieldErrors.applicantSchool" id="apply-school-error" class="field-error">{{ fieldErrors.applicantSchool }}</small>
            </div>
            <div class="form-field">
              <label for="apply-major">Ngành <span aria-hidden="true">*</span></label>
              <InputText id="apply-major" :ref="(element) => setFieldRef('applicantMajor', element)" v-model="form.applicantMajor" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantMajor }" :aria-invalid="Boolean(fieldErrors.applicantMajor)" :aria-describedby="fieldErrors.applicantMajor ? 'apply-major-error' : undefined" @input="clearFieldError('applicantMajor')" @blur="validateField('applicantMajor')" />
              <small v-if="fieldErrors.applicantMajor" id="apply-major-error" class="field-error">{{ fieldErrors.applicantMajor }}</small>
            </div>
          </div>
        </section>

        <section class="form-section">
          <h2>Hồ sơ ứng tuyển</h2>
          <div class="form-field">
            <label for="apply-cv-url">Liên kết CV <span aria-hidden="true">*</span></label>
            <InputText id="apply-cv-url" :ref="(element) => setFieldRef('cvUrl', element)" v-model="form.cvUrl" type="url" placeholder="https://..." autocomplete="url" :maxlength="LIMITS.cvUrl" :disabled="saving" :class="{ 'p-invalid': fieldErrors.cvUrl }" :aria-invalid="Boolean(fieldErrors.cvUrl)" :aria-describedby="fieldErrors.cvUrl ? 'apply-cv-url-error' : undefined" @input="clearFieldError('cvUrl')" @blur="validateField('cvUrl')" />
            <small v-if="fieldErrors.cvUrl" id="apply-cv-url-error" class="field-error">{{ fieldErrors.cvUrl }}</small>
          </div>
          <div class="form-field">
            <label for="apply-cover-letter">Lời giới thiệu <span aria-hidden="true">*</span></label>
            <Textarea id="apply-cover-letter" :ref="(element) => setFieldRef('coverLetter', element)" v-model="form.coverLetter" rows="5" :maxlength="LIMITS.coverLetter[1]" :disabled="saving" :class="{ 'p-invalid': fieldErrors.coverLetter }" :aria-invalid="Boolean(fieldErrors.coverLetter)" :aria-describedby="fieldErrors.coverLetter ? 'apply-cover-letter-error' : undefined" @input="clearFieldError('coverLetter')" @blur="validateField('coverLetter')" />
            <small v-if="fieldErrors.coverLetter" id="apply-cover-letter-error" class="field-error">{{ fieldErrors.coverLetter }}</small>
            <small class="character-count">{{ form.coverLetter.length }}/{{ LIMITS.coverLetter[1] }} ký tự · tối thiểu {{ LIMITS.coverLetter[0] }}</small>
          </div>
        </section>

        <footer class="apply-actions">
          <Button type="button" label="Hủy" severity="secondary" outlined :disabled="saving" @click="router.push(returnPath)" />
          <Button type="submit" label="Gửi đơn" icon="pi pi-send" :loading="saving" />
        </footer>
      </form>
    </template>
  </div>
</template>

<style scoped>
.apply-page { max-width: 900px; }
.apply-page-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.25rem; }
.apply-page-header .page-title { font-size: 1.6rem; }
.apply-state { display: grid; justify-items: start; gap: 1rem; padding: 1.5rem; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); }
.apply-state :deep(.p-message) { width: 100%; margin: 0; }
.position-summary { margin-bottom: 1.25rem; padding: 1.25rem; background: var(--surface); border: 1px solid var(--border); border-left: 4px solid var(--primary); border-radius: var(--radius-md); }
.summary-company { margin: 0 0 .35rem; color: var(--primary); font-size: .9rem; font-weight: 600; }
.position-summary h2 { margin: 0 0 1rem; color: var(--text-h); font-size: 1.35rem; }
.summary-details { display: grid; gap: .65rem; color: var(--text); font-size: .9rem; }
.summary-details span { display: flex; align-items: flex-start; gap: .55rem; min-width: 0; overflow-wrap: anywhere; }
.summary-details i { flex: 0 0 1rem; padding-top: .15rem; color: var(--primary); }
.apply-page > :deep(.p-message) { margin-bottom: .75rem; }
.prefill-note { margin: .75rem 0 1rem; color: var(--text); font-size: .875rem; line-height: 1.5; }
.apply-form { display: grid; gap: 1rem; }
.form-section { display: grid; gap: 1rem; padding: 1.25rem; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); }
.form-section h2 { margin: 0; color: var(--text-h); font-size: 1.1rem; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
.form-field { display: grid; min-width: 0; gap: .35rem; }
.form-field label { color: var(--text-h); font-size: .9rem; font-weight: 600; }
.form-field label span { color: var(--danger); }
.form-field :deep(.p-inputtext), .form-field :deep(.p-textarea) { box-sizing: border-box; width: 100%; min-height: 44px; }
.form-field :deep(.p-textarea) { min-height: 8rem; }
.field-error { color: var(--danger); font-size: .8rem; }
.character-count { color: var(--text); text-align: right; font-size: .8rem; }
.apply-actions { display: flex; justify-content: flex-end; gap: .75rem; }
.apply-actions :deep(.p-button) { min-height: 44px; }
@media (max-width: 640px) {
  .apply-page { padding-top: 1rem; }
  .apply-page-header { align-items: flex-start; flex-direction: column; gap: .35rem; }
  .apply-page-header .page-title { font-size: 1.35rem; }
  .position-summary, .form-section { padding: 1rem; }
  .form-grid { grid-template-columns: minmax(0, 1fr); }
  .apply-actions { flex-direction: column-reverse; }
  .apply-actions :deep(.p-button) { width: 100%; min-height: 48px; }
}
</style>