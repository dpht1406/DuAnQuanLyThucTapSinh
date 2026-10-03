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
const companyInitial = computed(() => position.value?.companyName?.trim()?.charAt(0)?.toUpperCase() || '?')
const deadlineDays = computed(() => daysLeft(position.value?.deadline))

function displayFieldError(name) {
  const message = fieldErrors[name]
  if (name === 'applicantSchool' && message === 'Trường học là bắt buộc.') return 'Vui lòng chọn trường của bạn'
  if (name === 'cvUrl' && message === 'Liên kết CV là bắt buộc.') return 'Liên kết CV không được để trống'
  if (name === 'coverLetter' && message?.startsWith('Lời giới thiệu phải từ 20')) return 'Cần tối thiểu 20 ký tự'
  return message
}

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
      <Button label="Quay lại" icon="pi pi-arrow-left" severity="secondary" text class="back-button" @click="router.push(returnPath)" />
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

    <template v-else-if="position">
      <section class="position-summary" aria-label="Vị trí ứng tuyển">
        <div class="company-mark" aria-hidden="true">{{ companyInitial }}</div>
        <div class="summary-copy">
          <p class="summary-company">{{ position.companyName }}</p>
          <h2>{{ position.title }}</h2>
          <div class="summary-details">
            <span><i class="pi pi-sitemap" aria-hidden="true" />{{ position.department || 'Chưa cập nhật phòng ban' }}</span>
            <span><i class="pi pi-map-marker" aria-hidden="true" />{{ position.displayLocation || position.location || 'Chưa cập nhật địa điểm' }}</span>
          </div>
        </div>
        <span class="deadline-chip" :class="{ 'deadline-chip-expired': position.isExpired || deadlineDays < 0, 'deadline-chip-soon': !position.isExpired && deadlineDays !== null && deadlineDays >= 0 && deadlineDays <= 3, 'deadline-chip-neutral': !position.isExpired && (deadlineDays === null || deadlineDays > 3) }">
          <i class="pi pi-calendar" aria-hidden="true" />
          <template v-if="position.isExpired || deadlineDays < 0">Đã hết hạn</template>
          <template v-else>Hạn nộp {{ formatDeadline(position.deadline) }}<template v-if="deadlineDays !== null"> · còn {{ deadlineDays }} ngày</template></template>
        </span>
      </section>

      <section v-if="!canApply(position)" class="apply-state unavailable-state">
        <Message severity="warn" :closable="false">
          {{ position.isExpired || deadlineDays < 0 ? 'Vị trí thực tập đã hết hạn nhận hồ sơ.' : 'Vị trí thực tập đã đóng.' }}
        </Message>
        <Button label="Quay lại danh sách vị trí" icon="pi pi-arrow-left" severity="secondary" @click="router.push(returnPath)" />
      </section>

      <section v-else-if="submitted" class="application-success" role="status">
        <div class="success-icon"><i class="pi pi-check" aria-hidden="true" /></div>
        <h2>Đã gửi đơn ứng tuyển</h2>
        <p>Doanh nghiệp sẽ xem hồ sơ và phản hồi sớm. Bạn có thể theo dõi trạng thái trong “Hồ sơ của tôi”.</p>
        <div class="success-actions">
          <Button label="Xem đơn đã gửi" severity="secondary" outlined @click="router.push('/profile')" />
          <Button label="Về danh sách vị trí" icon="pi pi-briefcase" @click="router.push('/job-positions')" />
        </div>
      </section>

      <template v-else>
        <Message v-if="profileWarning" severity="warn" :closable="false" role="status" class="inline-message">{{ profileWarning }}</Message>
        <Message v-if="formError" severity="error" :closable="false" role="alert" class="inline-message">{{ formError }}</Message>

        <p class="prefill-note">Thông tin được điền sẵn từ hồ sơ, bạn có thể chỉnh sửa; việc chỉnh sửa chỉ áp dụng cho đơn này.</p>
        <form class="apply-form" novalidate @submit.prevent="submit">
          <section class="form-section">
            <h2><span class="step-number">1</span>Thông tin cá nhân</h2>
            <div class="form-grid">
              <div class="form-field">
                <label for="apply-full-name">Họ tên <span aria-hidden="true">*</span></label>
                <InputText id="apply-full-name" :ref="(element) => setFieldRef('applicantFullName', element)" v-model="form.applicantFullName" maxlength="100" autocomplete="name" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantFullName }" :aria-invalid="Boolean(fieldErrors.applicantFullName)" :aria-describedby="fieldErrors.applicantFullName ? 'apply-full-name-error' : undefined" @input="clearFieldError('applicantFullName')" @blur="validateField('applicantFullName')" />
                <small id="apply-full-name-error" class="field-error" aria-live="polite"><template v-if="fieldErrors.applicantFullName"><i class="pi pi-exclamation-circle" aria-hidden="true" />{{ displayFieldError('applicantFullName') }}</template></small>
              </div>
              <div class="form-field">
                <label for="apply-email">Email <span aria-hidden="true">*</span></label>
                <InputText id="apply-email" :ref="(element) => setFieldRef('applicantEmail', element)" v-model="form.applicantEmail" type="email" maxlength="254" autocomplete="email" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantEmail }" :aria-invalid="Boolean(fieldErrors.applicantEmail)" :aria-describedby="fieldErrors.applicantEmail ? 'apply-email-error' : undefined" @input="clearFieldError('applicantEmail')" @blur="validateField('applicantEmail')" />
                <small id="apply-email-error" class="field-error" aria-live="polite"><template v-if="fieldErrors.applicantEmail"><i class="pi pi-exclamation-circle" aria-hidden="true" />{{ displayFieldError('applicantEmail') }}</template></small>
              </div>
              <div class="form-field">
                <label for="apply-phone">Số điện thoại <span aria-hidden="true">*</span></label>
                <InputText id="apply-phone" :ref="(element) => setFieldRef('applicantPhone', element)" v-model="form.applicantPhone" type="tel" maxlength="10" autocomplete="tel" inputmode="numeric" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantPhone }" :aria-invalid="Boolean(fieldErrors.applicantPhone)" :aria-describedby="fieldErrors.applicantPhone ? 'apply-phone-error' : undefined" @input="clearFieldError('applicantPhone')" @blur="validateField('applicantPhone')" />
                <small id="apply-phone-error" class="field-error" aria-live="polite"><template v-if="fieldErrors.applicantPhone"><i class="pi pi-exclamation-circle" aria-hidden="true" />{{ displayFieldError('applicantPhone') }}</template></small>
              </div>
              <div class="form-field">
                <label for="apply-school">Trường <span aria-hidden="true">*</span></label>
                <InputText id="apply-school" :ref="(element) => setFieldRef('applicantSchool', element)" v-model="form.applicantSchool" maxlength="150" autocomplete="organization" placeholder="Nhập tên trường" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantSchool }" :aria-invalid="Boolean(fieldErrors.applicantSchool)" :aria-describedby="fieldErrors.applicantSchool ? 'apply-school-error' : undefined" @input="clearFieldError('applicantSchool')" @blur="validateField('applicantSchool')" />
                <small id="apply-school-error" class="field-error" aria-live="polite"><template v-if="fieldErrors.applicantSchool"><i class="pi pi-exclamation-circle" aria-hidden="true" />{{ displayFieldError('applicantSchool') }}</template></small>
              </div>
              <div class="form-field">
                <label for="apply-major">Ngành <span aria-hidden="true">*</span></label>
                <InputText id="apply-major" :ref="(element) => setFieldRef('applicantMajor', element)" v-model="form.applicantMajor" maxlength="150" :disabled="saving" :class="{ 'p-invalid': fieldErrors.applicantMajor }" :aria-invalid="Boolean(fieldErrors.applicantMajor)" :aria-describedby="fieldErrors.applicantMajor ? 'apply-major-error' : undefined" @input="clearFieldError('applicantMajor')" @blur="validateField('applicantMajor')" />
                <small id="apply-major-error" class="field-error" aria-live="polite"><template v-if="fieldErrors.applicantMajor"><i class="pi pi-exclamation-circle" aria-hidden="true" />{{ displayFieldError('applicantMajor') }}</template></small>
              </div>
            </div>
          </section>

          <section class="form-section">
            <h2><span class="step-number">2</span>Hồ sơ ứng tuyển</h2>
            <div class="form-field">
              <label for="apply-cv-url">Liên kết CV <span aria-hidden="true">*</span></label>
              <InputText id="apply-cv-url" :ref="(element) => setFieldRef('cvUrl', element)" v-model="form.cvUrl" type="url" placeholder="https://..." autocomplete="url" :maxlength="LIMITS.cvUrl" :disabled="saving" :class="{ 'p-invalid': fieldErrors.cvUrl }" :aria-invalid="Boolean(fieldErrors.cvUrl)" :aria-describedby="fieldErrors.cvUrl ? 'apply-cv-url-help apply-cv-url-error' : 'apply-cv-url-help'" @input="clearFieldError('cvUrl')" @blur="validateField('cvUrl')" />
              <small id="apply-cv-url-help" class="field-hint">Dán liên kết Google Drive / PDF ở chế độ chia sẻ công khai</small>
              <small id="apply-cv-url-error" class="field-error" aria-live="polite"><template v-if="fieldErrors.cvUrl"><i class="pi pi-exclamation-circle" aria-hidden="true" />{{ displayFieldError('cvUrl') }}</template></small>
            </div>
            <div class="form-field">
              <label for="apply-cover-letter">Lời giới thiệu <span aria-hidden="true">*</span></label>
              <Textarea id="apply-cover-letter" :ref="(element) => setFieldRef('coverLetter', element)" v-model="form.coverLetter" rows="5" :maxlength="LIMITS.coverLetter[1]" :disabled="saving" :class="{ 'p-invalid': fieldErrors.coverLetter }" :aria-invalid="Boolean(fieldErrors.coverLetter)" :aria-describedby="fieldErrors.coverLetter ? 'apply-cover-letter-error apply-cover-letter-count' : 'apply-cover-letter-count'" @input="clearFieldError('coverLetter')" @blur="validateField('coverLetter')" />
              <small id="apply-cover-letter-error" class="field-error" aria-live="polite"><template v-if="fieldErrors.coverLetter"><i class="pi pi-exclamation-circle" aria-hidden="true" />{{ displayFieldError('coverLetter') }}</template></small>
              <small id="apply-cover-letter-count" class="character-count">{{ form.coverLetter.length }}/{{ LIMITS.coverLetter[1] }} ký tự · tối thiểu {{ LIMITS.coverLetter[0] }}</small>
            </div>
          </section>

          <footer class="apply-actions">
            <p v-if="invalidFieldCount" class="submit-error-summary" role="alert">Vui lòng kiểm tra lại {{ invalidFieldCount }} trường bắt buộc</p>
            <div class="action-buttons">
              <Button type="button" label="Hủy" severity="secondary" outlined :disabled="saving" @click="router.push(returnPath)" />
              <Button type="submit" label="Gửi đơn" icon="pi pi-send" :loading="saving" :disabled="saving" />
            </div>
          </footer>
        </form>
      </template>
    </template>
  </div>
</template>

<style scoped>
.apply-page { width: min(100%, 812px); max-width: 812px; padding-top: 24px; color: var(--apply-ink); }
.apply-page-header { display: flex; align-items: center; gap: 16px; margin-bottom: 22px; }
.apply-page-header .page-title { color: var(--apply-ink); font-size: 24px; font-weight: 700; }
.back-button { flex: 0 0 auto; min-height: 40px; padding-left: 0; color: var(--apply-muted); }
.back-button:focus-visible { outline: 3px solid rgba(99, 102, 241, .24); outline-offset: 2px; }
.apply-state { display: grid; justify-items: start; gap: 16px; padding: 22px; background: var(--apply-surface); border: 1px solid var(--apply-border); border-radius: 16px; box-shadow: var(--apply-shadow); }
.apply-state :deep(.p-message) { width: 100%; margin: 0; }
.position-summary { display: grid; grid-template-columns: 48px minmax(0, 1fr) auto; align-items: center; gap: 16px; margin-bottom: 12px; padding: 18px; background: var(--apply-surface); border: 1px solid var(--apply-border); border-radius: 16px; box-shadow: var(--apply-shadow); }
.company-mark { display: grid; width: 48px; aspect-ratio: 1; place-items: center; color: var(--apply-primary-hover); background: var(--apply-primary-soft); border-radius: 12px; font-size: 20px; font-weight: 700; }
.summary-copy { min-width: 0; }
.summary-company { margin: 0 0 4px; color: var(--apply-muted); font-size: 13px; font-weight: 500; }
.position-summary h2 { margin: 0 0 8px; color: var(--apply-ink); font-size: 18px; font-weight: 700; line-height: 1.35; overflow-wrap: anywhere; }
.summary-details { display: flex; flex-wrap: wrap; gap: 6px 18px; color: var(--apply-muted); font-size: 13px; }
.summary-details span { display: inline-flex; align-items: baseline; gap: 7px; min-width: 0; overflow-wrap: anywhere; }
.summary-details i { color: var(--apply-primary); font-size: 12px; }
.deadline-chip { display: inline-flex; align-items: center; justify-content: center; gap: 7px; max-width: 220px; padding: 8px 10px; border-radius: 8px; font-size: 12px; line-height: 1.35; text-align: center; }
.deadline-chip-neutral { color: var(--apply-muted); background: var(--apply-neutral-soft); }
.deadline-chip-soon { color: var(--apply-warning-ink); background: var(--apply-warning-soft); }
.deadline-chip-expired { color: var(--apply-error-ink); background: var(--apply-danger-soft); }
.inline-message { margin: 12px 0 0; }
.prefill-note { margin: 14px 0 16px; color: var(--apply-muted); font-size: 12px; line-height: 1.55; }
.apply-form { display: grid; gap: 14px; }
.form-section { display: grid; gap: 16px; padding: 20px; background: var(--apply-surface); border: 1px solid var(--apply-border); border-radius: 16px; box-shadow: var(--apply-shadow); }
.form-section h2 { display: flex; align-items: center; gap: 10px; margin: 0; color: var(--apply-ink); font-size: 18px; font-weight: 700; }
.step-number { display: inline-grid; flex: 0 0 28px; width: 28px; aspect-ratio: 1; place-items: center; color: var(--apply-primary-hover); background: var(--apply-primary-soft); border-radius: 50%; font-size: 13px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px 16px; }
.form-field { display: grid; align-content: start; min-width: 0; gap: 6px; }
.form-field label { color: var(--apply-ink); font-size: 12.5px; font-weight: 600; }
.form-field label span { color: var(--apply-error); }
.form-field :deep(.p-inputtext), .form-field :deep(.p-textarea) { box-sizing: border-box; width: 100%; min-height: 44px; padding: 10px 12px; color: var(--apply-ink); background: var(--apply-surface); border: 1px solid var(--apply-border); border-radius: 10px; font-family: inherit; font-size: 14px; }
.form-field :deep(.p-inputtext::placeholder), .form-field :deep(.p-textarea::placeholder) { color: var(--apply-placeholder); opacity: 1; }
.form-field :deep(.p-inputtext:enabled:focus), .form-field :deep(.p-textarea:enabled:focus) { border-color: var(--apply-primary); box-shadow: var(--apply-focus-ring); }
.form-field :deep(.p-inputtext:disabled), .form-field :deep(.p-textarea:disabled) { color: var(--apply-muted); background: var(--apply-neutral-soft); cursor: not-allowed; }
.form-field :deep(.p-inputtext.p-invalid), .form-field :deep(.p-textarea.p-invalid) { background: var(--apply-danger-soft); border-color: var(--apply-error); }
.form-field :deep(.p-inputtext.p-invalid:focus), .form-field :deep(.p-textarea.p-invalid:focus) { box-shadow: var(--apply-error-ring); }
.form-field :deep(.p-textarea) { min-height: 136px; resize: vertical; }
.field-hint { color: var(--apply-muted); font-size: 12px; line-height: 1.45; }
.field-error { display: block; min-height: 16px; color: var(--apply-error); font-size: 12px; line-height: 16px; }
.field-error i { margin-right: 5px; font-size: 11px; }
.character-count { color: var(--apply-muted); text-align: right; font-size: 12px; }
.apply-actions { display: flex; align-items: center; justify-content: flex-end; gap: 16px; padding: 2px 0 10px; }
.action-buttons { display: flex; justify-content: flex-end; gap: 10px; }
.apply-actions :deep(.p-button), .success-actions :deep(.p-button) { min-height: 44px; padding: 0 16px; border-radius: 11px; font-size: 14px; font-weight: 600; }
.apply-actions :deep(.p-button:not(.p-button-outlined)), .success-actions :deep(.p-button:not(.p-button-outlined)) { background: var(--apply-primary); border-color: var(--apply-primary); box-shadow: var(--apply-button-shadow); }
.apply-actions :deep(.p-button:not(.p-button-outlined):hover), .success-actions :deep(.p-button:not(.p-button-outlined):hover) { background: var(--apply-primary-hover); border-color: var(--apply-primary-hover); }
.apply-actions :deep(.p-button:disabled) { color: rgba(255, 255, 255, .72); background: var(--apply-disabled-bg); border-color: var(--apply-disabled-bg); box-shadow: none; cursor: not-allowed; }
.submit-error-summary { margin: 0; color: var(--apply-error); font-size: 12px; font-weight: 600; }
.application-success { display: grid; justify-items: center; gap: 12px; margin-top: 32px; padding: 36px 24px; background: var(--apply-surface); border: 1px solid var(--apply-border); border-radius: 16px; box-shadow: var(--apply-shadow); text-align: center; }
.success-icon { display: grid; width: 56px; aspect-ratio: 1; place-items: center; color: var(--apply-success); background: var(--apply-success-soft); border-radius: 50%; font-size: 24px; }
.application-success h2 { margin: 4px 0 0; color: var(--apply-ink); font-size: 20px; font-weight: 700; }
.application-success p { max-width: 520px; margin: 0; color: var(--apply-muted); font-size: 14px; line-height: 1.6; }
.success-actions { display: flex; flex-wrap: wrap; justify-content: center; gap: 10px; margin-top: 8px; }
.success-actions :deep(.p-button-outlined) { color: var(--apply-primary-hover); border-color: var(--apply-border); }
@media (max-width: 640px) {
  .apply-page { padding: 18px 16px calc(150px + env(safe-area-inset-bottom)); }
  .apply-page-header { align-items: flex-start; flex-direction: column; gap: 5px; margin-bottom: 16px; }
  .apply-page-header .page-title { font-size: 24px; line-height: 1.25; }
  .back-button { min-height: 36px; }
  .position-summary { grid-template-columns: 42px minmax(0, 1fr); gap: 12px; padding: 14px; }
  .company-mark { width: 42px; border-radius: 10px; font-size: 18px; }
  .position-summary h2 { font-size: 16px; }
  .summary-details { gap: 6px 12px; font-size: 12px; }
  .deadline-chip { grid-column: 2; justify-self: start; max-width: 100%; text-align: left; }
  .form-section { gap: 14px; padding: 16px; }
  .form-section h2 { font-size: 16px; }
  .form-grid { grid-template-columns: minmax(0, 1fr); gap: 12px; }
  .form-field :deep(.p-inputtext), .form-field :deep(.p-textarea) { min-height: 46px; font-size: 16px; }
  .form-field :deep(.p-textarea) { min-height: 136px; }
  .apply-actions { position: fixed; right: 0; bottom: calc(60px + env(safe-area-inset-bottom)); left: 0; z-index: 105; display: grid; gap: 8px; padding: 10px 16px max(10px, env(safe-area-inset-bottom)); background: var(--apply-surface); border-top: 1px solid var(--apply-border); box-shadow: 0 -5px 16px rgba(15, 23, 42, .06); }
  .action-buttons { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.3fr); }
  .action-buttons :deep(.p-button) { width: 100%; min-height: 46px; }
  .application-success { margin-top: 16px; padding: 28px 18px; }
  .success-actions { width: 100%; flex-direction: column-reverse; }
  .success-actions :deep(.p-button) { width: 100%; }
}
</style>