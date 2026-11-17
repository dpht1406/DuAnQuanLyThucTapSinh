<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { onBeforeRouteLeave, useRouter } from 'vue-router'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import ConfirmDialog from 'primevue/confirmdialog'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Skeleton from 'primevue/skeleton'
import Textarea from 'primevue/textarea'
import ItemCard from '../../components/about/ItemCard.vue'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { getAbout, resetAbout, updateAbout } from '../../api/about.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import {
  ABOUT_LIMITS,
  ALLOWED_ICONS,
  generateChecklistId,
  validateAboutContent
} from '../../utils/aboutContent.js'

const router = useRouter()
const confirm = useConfirm()
const toast = useToast()

const loading = ref(true)
const saving = ref(false)
const resetting = ref(false)
const formLoaded = ref(false)
const submitted = ref(false)
const loadError = ref('')
const formError = ref('')
const serverErrors = ref([])
const fieldErrors = ref({})

const form = reactive({
  name: '',
  shortName: '',
  tagline: '',
  summary: '',
  footnote: '',
  facts: [],
  history: [],
  fields: [],
  environment: {
    highlights: [],
    week: [],
    tools: []
  },
  values: [],
  studentInfo: {
    eligibility: [],
    documents: [],
    process: [],
    checklist: [],
    faq: []
  },
  contact: {
    person: '',
    address: '',
    hours: '',
    email: '',
    phone: ''
  }
})

const initialFormValue = ref(null)
const hasUnsavedChanges = computed(
  () =>
    !submitted.value &&
    formLoaded.value &&
    initialFormValue.value !== null &&
    JSON.stringify(form) !== initialFormValue.value
)

function clearFieldError(path) {
  if (fieldErrors.value[path]) {
    delete fieldErrors.value[path]
  }
  if (formError.value) formError.value = ''
  if (serverErrors.value.length > 0) serverErrors.value = []
}

async function focusFirstInvalidField() {
  await nextTick()
  const errorKeys = Object.keys(fieldErrors.value)
  if (errorKeys.length === 0) return

  const firstKey = errorKeys[0]
  // Ánh xạ key path sang id
  let targetId = firstKey
    .replace(/\./g, '-')
    .replace(/^facts/, 'fact')
    .replace(/^history/, 'history')
    .replace(/^fields/, 'field')
    .replace(/^environment-highlights/, 'highlight')
    .replace(/^environment-week/, 'week')
    .replace(/^environment-tools/, 'tool')
    .replace(/^values/, 'value')
    .replace(/^studentInfo-eligibility/, 'eligibility')
    .replace(/^studentInfo-documents/, 'document')
    .replace(/^studentInfo-process/, 'process')
    .replace(/^studentInfo-checklist/, 'checklist')
    .replace(/^studentInfo-faq/, 'faq')
    .replace(/^contact/, 'contact')
    .replace(/^name/, 'about-name')
    .replace(/^shortName/, 'about-short-name')
    .replace(/^tagline/, 'about-tagline')
    .replace(/^summary/, 'about-summary')
    .replace(/^footnote/, 'about-footnote')

  const element = document.getElementById(targetId) || document.querySelector(`[id*="${targetId}"]`)
  if (element) {
    element.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
    element.focus?.()
  }
}

async function loadData() {
  loading.value = true
  formLoaded.value = false
  submitted.value = false
  loadError.value = ''
  formError.value = ''
  serverErrors.value = []
  fieldErrors.value = {}

  try {
    const data = await getAbout()
    const content = data?.content || {}

    form.name = content.name || ''
    form.shortName = content.shortName || ''
    form.tagline = content.tagline || ''
    form.summary = content.summary || ''
    form.footnote = content.footnote || ''
    form.facts = (content.facts || []).map((f) => ({ ...f }))
    form.history = (content.history || []).map((h) => ({ ...h }))
    form.fields = (content.fields || []).map((f) => ({ ...f, tags: [...(f.tags || [])] }))
    form.environment = {
      highlights: (content.environment?.highlights || []).map((h) => ({ ...h })),
      week: (content.environment?.week || []).map((w) => ({ ...w })),
      tools: [...(content.environment?.tools || [])]
    }
    form.values = (content.values || []).map((v) => ({ ...v }))
    form.studentInfo = {
      eligibility: [...(content.studentInfo?.eligibility || [])],
      documents: [...(content.studentInfo?.documents || [])],
      process: (content.studentInfo?.process || []).map((p) => ({ ...p })),
      checklist: (content.studentInfo?.checklist || []).map((c) => ({ ...c })),
      faq: (content.studentInfo?.faq || []).map((faq) => ({ ...faq }))
    }
    form.contact = {
      person: content.contact?.person || '',
      address: content.contact?.address || '',
      hours: content.contact?.hours || '',
      email: content.contact?.email || '',
      phone: content.contact?.phone || ''
    }

    initialFormValue.value = JSON.stringify(form)
    formLoaded.value = true
  } catch (err) {
    loadError.value = extractErrorMessage(err, 'Không tải được dữ liệu trang Giới thiệu.')
  } finally {
    loading.value = false
  }
}

// Reorder helpers
function moveUp(list, index) {
  if (index <= 0) return
  const temp = list[index]
  list[index] = list[index - 1]
  list[index - 1] = temp
}

function moveDown(list, index) {
  if (index >= list.length - 1) return
  const temp = list[index]
  list[index] = list[index + 1]
  list[index + 1] = temp
}

function removeItem(list, index) {
  list.splice(index, 1)
}

// Add item helpers
function addFact() {
  if (form.facts.length >= ABOUT_LIMITS.facts.max) return
  form.facts.push({ icon: 'pi pi-calendar', label: '', value: '' })
}

function addHistory() {
  if (form.history.length >= ABOUT_LIMITS.history.max) return
  form.history.push({ year: '', title: '', text: '' })
}

function addField() {
  if (form.fields.length >= ABOUT_LIMITS.fields.max) return
  form.fields.push({ icon: 'pi pi-server', title: '', text: '', tags: [] })
}

const newTagInputs = reactive({})
function addTagToField(fieldIndex) {
  const currentTags = form.fields[fieldIndex].tags
  if (currentTags.length >= ABOUT_LIMITS.fields.maxTags) return
  const val = (newTagInputs[fieldIndex] || '').trim()
  if (!val) return
  currentTags.push(val)
  newTagInputs[fieldIndex] = ''
}

function removeTagFromField(fieldIndex, tagIndex) {
  form.fields[fieldIndex].tags.splice(tagIndex, 1)
}

function addHighlight() {
  if (form.environment.highlights.length >= ABOUT_LIMITS.environment.highlights.max) return
  form.environment.highlights.push({ icon: 'pi pi-user', title: '', text: '' })
}

function addWeek() {
  if (form.environment.week.length >= ABOUT_LIMITS.environment.week.max) return
  form.environment.week.push({ day: '', text: '' })
}

function addTool() {
  if (form.environment.tools.length >= ABOUT_LIMITS.environment.tools.max) return
  form.environment.tools.push('')
}

function addValue() {
  if (form.values.length >= ABOUT_LIMITS.values.max) return
  form.values.push({ icon: 'pi pi-shield', title: '', text: '' })
}

function addEligibility() {
  if (form.studentInfo.eligibility.length >= ABOUT_LIMITS.studentInfo.eligibility.max) return
  form.studentInfo.eligibility.push('')
}

function addDocument() {
  if (form.studentInfo.documents.length >= ABOUT_LIMITS.studentInfo.documents.max) return
  form.studentInfo.documents.push('')
}

function addProcess() {
  if (form.studentInfo.process.length >= ABOUT_LIMITS.studentInfo.process.max) return
  form.studentInfo.process.push({ status: '', text: '' })
}

function addChecklist() {
  if (form.studentInfo.checklist.length >= ABOUT_LIMITS.studentInfo.checklist.max) return
  const existingIds = form.studentInfo.checklist.map((c) => c.id)
  const newId = generateChecklistId(existingIds)
  form.studentInfo.checklist.push({ id: newId, label: '' })
}

function addFaq() {
  if (form.studentInfo.faq.length >= ABOUT_LIMITS.studentInfo.faq.max) return
  form.studentInfo.faq.push({ q: '', a: '' })
}

async function submitForm() {
  if (saving.value || resetting.value) return

  const errors = validateAboutContent(form)
  fieldErrors.value = errors

  if (Object.keys(errors).length > 0) {
    formError.value = 'Vui lòng kiểm tra lại các trường thông tin có lỗi bên dưới.'
    await focusFirstInvalidField()
    return
  }

  saving.value = true
  formError.value = ''
  serverErrors.value = []

  try {
    await updateAbout(form)
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Cập nhật trang Giới thiệu thành công.',
      life: 3000
    })
    await router.push({ name: 'about' })
  } catch (err) {
    formError.value = extractErrorMessage(err, 'Có lỗi xảy ra khi lưu thay đổi.')
    if (err?.response?.data?.errors && Array.isArray(err.response.data.errors)) {
      serverErrors.value = err.response.data.errors
    }
    await focusFirstInvalidField()
  } finally {
    saving.value = false
  }
}

function confirmReset() {
  confirm.require({
    message: 'Bạn có chắc chắn muốn khôi phục nội dung về mặc định không? Mọi thay đổi hiện tại sẽ bị xóa.',
    header: 'Xác nhận khôi phục mặc định',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy',
    acceptLabel: 'Khôi phục',
    rejectClass: 'p-button-secondary p-button-text',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await executeReset()
    }
  })
}

async function executeReset() {
  resetting.value = true
  formError.value = ''
  serverErrors.value = []

  try {
    await resetAbout()
    submitted.value = true
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Đã khôi phục nội dung mặc định thành công.',
      life: 3000
    })
    await router.push({ name: 'about' })
  } catch (err) {
    formError.value = extractErrorMessage(err, 'Không thể khôi phục nội dung mặc định.')
  } finally {
    resetting.value = false
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
  loadData()
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', onBeforeUnload)
})
</script>

<template>
  <FormPage
    title="Chỉnh sửa nội dung Giới thiệu"
    subtitle="Quản lý và điều chỉnh các khối nội dung của trang Giới thiệu."
    back-label="Quay lại Giới thiệu"
    :back-to="{ name: 'about' }"
    class="about-edit-page"
  >
    <ConfirmDialog />

    <div v-if="loading" class="about-edit-loading" aria-busy="true">
      <Skeleton height="3rem" class="mb-3" />
      <Skeleton height="15rem" class="mb-4" />
      <Skeleton height="15rem" />
    </div>

    <Message v-else-if="loadError" severity="error" :closable="false">{{ loadError }}</Message>

    <form v-else class="about-edit-form" novalidate @submit.prevent="submitForm">
      <Message v-if="formError" severity="error" :closable="false" class="mb-3">
        {{ formError }}
        <ul v-if="serverErrors.length > 0" class="server-error-list">
          <li v-for="(msg, idx) in serverErrors" :key="idx">{{ msg }}</li>
        </ul>
      </Message>

      <!-- 1. THÔNG TIN CHUNG -->
      <FormSection title="1. Thông tin chung" description="Tên đơn vị, khẩu hiệu và phần mô tả tóm tắt đầu trang.">
        <FormField label="Tên đơn vị tiếp nhận *" for="about-name" :error="fieldErrors.name">
          <InputText
            id="about-name"
            v-model="form.name"
            maxlength="200"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors.name)"
            @input="clearFieldError('name')"
          />
        </FormField>

        <FormField label="Tên viết tắt" for="about-short-name" optional :error="fieldErrors.shortName">
          <InputText
            id="about-short-name"
            v-model="form.shortName"
            maxlength="100"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors.shortName)"
            @input="clearFieldError('shortName')"
          />
        </FormField>

        <FormField label="Khẩu hiệu (Tagline) *" for="about-tagline" :error="fieldErrors.tagline">
          <InputText
            id="about-tagline"
            v-model="form.tagline"
            maxlength="300"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors.tagline)"
            @input="clearFieldError('tagline')"
          />
        </FormField>

        <FormField label="Giới thiệu tóm tắt (Summary) *" for="about-summary" :error="fieldErrors.summary">
          <Textarea
            id="about-summary"
            v-model="form.summary"
            rows="4"
            maxlength="1000"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors.summary)"
            @input="clearFieldError('summary')"
          />
        </FormField>

        <FormField label="Ghi chú cuối trang (Footnote)" for="about-footnote" optional :error="fieldErrors.footnote">
          <InputText
            id="about-footnote"
            v-model="form.footnote"
            maxlength="300"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors.footnote)"
            @input="clearFieldError('footnote')"
          />
        </FormField>
      </FormSection>

      <!-- 2. THÔNG TIN NHANH -->
      <FormSection title="2. Thông tin nhanh" description="Số liệu điểm nhấn dạng thẻ ở đầu trang (từ 1 đến 8 mục).">
        <Message v-if="fieldErrors.facts" severity="error" :closable="false" class="mb-2">
          {{ fieldErrors.facts }}
        </Message>

        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.facts"
            :key="i"
            :title="`Thông tin #${i + 1}`"
            :index="i"
            :total="form.facts.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.facts, i)"
            @move-down="moveDown(form.facts, i)"
            @remove="removeItem(form.facts, i)"
          >
            <div class="grid-row-3">
              <FormField label="Biểu tượng *" :for="`fact-${i}-icon`" :error="fieldErrors[`facts.${i}.icon`]">
                <Select
                  :inputId="`fact-${i}-icon`"
                  v-model="item.icon"
                  :options="ALLOWED_ICONS"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="Chọn biểu tượng"
                  aria-label="Chọn biểu tượng thông tin nhanh"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`facts.${i}.icon`])"
                  @change="clearFieldError(`facts.${i}.icon`)"
                />
              </FormField>
              <FormField label="Nhãn *" :for="`fact-${i}-label`" :error="fieldErrors[`facts.${i}.label`]">
                <InputText
                  :id="`fact-${i}-label`"
                  v-model="item.label"
                  maxlength="50"
                  aria-label="Nhãn thông tin nhanh"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`facts.${i}.label`])"
                  @input="clearFieldError(`facts.${i}.label`)"
                />
              </FormField>
              <FormField label="Giá trị *" :for="`fact-${i}-value`" :error="fieldErrors[`facts.${i}.value`]">
                <InputText
                  :id="`fact-${i}-value`"
                  v-model="item.value"
                  maxlength="100"
                  aria-label="Giá trị thông tin nhanh"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`facts.${i}.value`])"
                  @input="clearFieldError(`facts.${i}.value`)"
                />
              </FormField>
            </div>
          </ItemCard>
        </div>

        <div class="add-action-bar">
          <Button
            label="Thêm thông tin nhanh"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.facts.length >= ABOUT_LIMITS.facts.max || saving || resetting"
            @click="addFact"
          />
          <span v-if="form.facts.length >= ABOUT_LIMITS.facts.max" class="limit-note">
            Đã đạt tối đa {{ ABOUT_LIMITS.facts.max }} mục.
          </span>
        </div>
      </FormSection>

      <!-- 3. LỊCH SỬ HÌNH THÀNH -->
      <FormSection title="3. Lịch sử hình thành" description="Các cột mốc thời gian phát triển (tối đa 20 mục).">
        <Message v-if="fieldErrors.history" severity="error" :closable="false" class="mb-2">
          {{ fieldErrors.history }}
        </Message>

        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.history"
            :key="i"
            :title="`Cột mốc #${i + 1} (${item.year || 'Năm'})`"
            :index="i"
            :total="form.history.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.history, i)"
            @move-down="moveDown(form.history, i)"
            @remove="removeItem(form.history, i)"
          >
            <div class="grid-row-2">
              <FormField label="Năm *" :for="`history-${i}-year`" :error="fieldErrors[`history.${i}.year`]">
                <InputText
                  :id="`history-${i}-year`"
                  v-model="item.year"
                  maxlength="20"
                  aria-label="Năm cột mốc lịch sử"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`history.${i}.year`])"
                  @input="clearFieldError(`history.${i}.year`)"
                />
              </FormField>
              <FormField label="Tiêu đề *" :for="`history-${i}-title`" :error="fieldErrors[`history.${i}.title`]">
                <InputText
                  :id="`history-${i}-title`"
                  v-model="item.title"
                  maxlength="150"
                  aria-label="Tiêu đề cột mốc lịch sử"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`history.${i}.title`])"
                  @input="clearFieldError(`history.${i}.title`)"
                />
              </FormField>
            </div>
            <FormField label="Nội dung mô tả *" :for="`history-${i}-text`" :error="fieldErrors[`history.${i}.text`]">
              <Textarea
                :id="`history-${i}-text`"
                v-model="item.text"
                rows="2"
                maxlength="500"
                aria-label="Nội dung mô tả cột mốc lịch sử"
                :disabled="saving || resetting"
                :invalid="Boolean(fieldErrors[`history.${i}.text`])"
                @input="clearFieldError(`history.${i}.text`)"
              />
            </FormField>
          </ItemCard>
        </div>

        <div class="add-action-bar">
          <Button
            label="Thêm cột mốc lịch sử"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.history.length >= ABOUT_LIMITS.history.max || saving || resetting"
            @click="addHistory"
          />
          <span v-if="form.history.length >= ABOUT_LIMITS.history.max" class="limit-note">
            Đã đạt tối đa {{ ABOUT_LIMITS.history.max }} mục.
          </span>
        </div>
      </FormSection>

      <!-- 4. LĨNH VỰC HOẠT ĐỘNG -->
      <FormSection title="4. Lĩnh vực hoạt động" description="Các mảng nghiệp vụ chuyên môn (tối đa 8 mục, mỗi mục tối đa 8 thẻ).">
        <Message v-if="fieldErrors.fields" severity="error" :closable="false" class="mb-2">
          {{ fieldErrors.fields }}
        </Message>

        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.fields"
            :key="i"
            :title="`Lĩnh vực #${i + 1}: ${item.title || ''}`"
            :index="i"
            :total="form.fields.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.fields, i)"
            @move-down="moveDown(form.fields, i)"
            @remove="removeItem(form.fields, i)"
          >
            <div class="grid-row-2">
              <FormField label="Biểu tượng *" :for="`field-${i}-icon`" :error="fieldErrors[`fields.${i}.icon`]">
                <Select
                  :inputId="`field-${i}-icon`"
                  v-model="item.icon"
                  :options="ALLOWED_ICONS"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="Chọn biểu tượng"
                  aria-label="Chọn biểu tượng lĩnh vực"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`fields.${i}.icon`])"
                  @change="clearFieldError(`fields.${i}.icon`)"
                />
              </FormField>
              <FormField label="Tiêu đề *" :for="`field-${i}-title`" :error="fieldErrors[`fields.${i}.title`]">
                <InputText
                  :id="`field-${i}-title`"
                  v-model="item.title"
                  maxlength="150"
                  aria-label="Tiêu đề lĩnh vực"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`fields.${i}.title`])"
                  @input="clearFieldError(`fields.${i}.title`)"
                />
              </FormField>
            </div>

            <FormField label="Mô tả *" :for="`field-${i}-text`" :error="fieldErrors[`fields.${i}.text`]">
              <Textarea
                :id="`field-${i}-text`"
                v-model="item.text"
                rows="2"
                maxlength="400"
                aria-label="Mô tả lĩnh vực"
                :disabled="saving || resetting"
                :invalid="Boolean(fieldErrors[`fields.${i}.text`])"
                @input="clearFieldError(`fields.${i}.text`)"
              />
            </FormField>

            <!-- Tags -->
            <div class="tags-manager">
              <label class="tag-manager-label">Thẻ công nghệ (tối đa 8 thẻ):</label>
              <div class="tags-list">
                <span v-for="(tag, t) in item.tags" :key="t" class="tag-pill">
                  {{ tag }}
                  <button
                    type="button"
                    class="btn-remove-tag"
                    :aria-label="`Xóa thẻ ${tag}`"
                    :disabled="saving || resetting"
                    @click="removeTagFromField(i, t)"
                  >
                    &times;
                  </button>
                </span>
              </div>
              <div v-if="item.tags.length < ABOUT_LIMITS.fields.maxTags" class="tag-add-row">
                <InputText
                  v-model="newTagInputs[i]"
                  placeholder="Nhập thẻ mới (Enter hoặc bấm Thêm)"
                  size="small"
                  maxlength="30"
                  aria-label="Tên thẻ mới"
                  :disabled="saving || resetting"
                  @keydown.enter.prevent="addTagToField(i)"
                />
                <Button label="Thêm thẻ" size="small" text :disabled="saving || resetting" @click="addTagToField(i)" />
              </div>
            </div>
          </ItemCard>
        </div>

        <div class="add-action-bar">
          <Button
            label="Thêm lĩnh vực"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.fields.length >= ABOUT_LIMITS.fields.max || saving || resetting"
            @click="addField"
          />
          <span v-if="form.fields.length >= ABOUT_LIMITS.fields.max" class="limit-note">
            Đã đạt tối đa {{ ABOUT_LIMITS.fields.max }} mục.
          </span>
        </div>
      </FormSection>

      <!-- 5. MÔI TRƯỜNG LÀM VIỆC -->
      <FormSection title="5. Môi trường làm việc" description="Điểm nổi bật, lịch tuần và công cụ làm việc.">
        <!-- Highlights -->
        <h4 class="sub-section-title">Điểm nổi bật (tối đa 8 mục)</h4>
        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.environment.highlights"
            :key="i"
            :title="`Điểm nổi bật #${i + 1}`"
            :index="i"
            :total="form.environment.highlights.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.environment.highlights, i)"
            @move-down="moveDown(form.environment.highlights, i)"
            @remove="removeItem(form.environment.highlights, i)"
          >
            <div class="grid-row-2">
              <FormField label="Biểu tượng *" :for="`highlight-${i}-icon`" :error="fieldErrors[`environment.highlights.${i}.icon`]">
                <Select
                  :inputId="`highlight-${i}-icon`"
                  v-model="item.icon"
                  :options="ALLOWED_ICONS"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="Chọn biểu tượng"
                  aria-label="Chọn biểu tượng điểm nổi bật"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`environment.highlights.${i}.icon`])"
                  @change="clearFieldError(`environment.highlights.${i}.icon`)"
                />
              </FormField>
              <FormField label="Tiêu đề *" :for="`highlight-${i}-title`" :error="fieldErrors[`environment.highlights.${i}.title`]">
                <InputText
                  :id="`highlight-${i}-title`"
                  v-model="item.title"
                  maxlength="150"
                  aria-label="Tiêu đề điểm nổi bật"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`environment.highlights.${i}.title`])"
                  @input="clearFieldError(`environment.highlights.${i}.title`)"
                />
              </FormField>
            </div>
            <FormField label="Mô tả *" :for="`highlight-${i}-text`" :error="fieldErrors[`environment.highlights.${i}.text`]">
              <InputText
                :id="`highlight-${i}-text`"
                v-model="item.text"
                maxlength="300"
                aria-label="Mô tả điểm nổi bật"
                :disabled="saving || resetting"
                :invalid="Boolean(fieldErrors[`environment.highlights.${i}.text`])"
                @input="clearFieldError(`environment.highlights.${i}.text`)"
              />
            </FormField>
          </ItemCard>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm điểm nổi bật"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.environment.highlights.length >= ABOUT_LIMITS.environment.highlights.max || saving || resetting"
            @click="addHighlight"
          />
        </div>

        <!-- Week schedule -->
        <h4 class="sub-section-title mt-4">Một tuần thực tập diễn ra thế nào (tối đa 7 mục)</h4>
        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.environment.week"
            :key="i"
            :title="`Lịch #${i + 1}: ${item.day || ''}`"
            :index="i"
            :total="form.environment.week.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.environment.week, i)"
            @move-down="moveDown(form.environment.week, i)"
            @remove="removeItem(form.environment.week, i)"
          >
            <div class="grid-row-2">
              <FormField label="Ngày/Thời gian *" :for="`week-${i}-day`" :error="fieldErrors[`environment.week.${i}.day`]">
                <InputText
                  :id="`week-${i}-day`"
                  v-model="item.day"
                  maxlength="50"
                  aria-label="Ngày thời gian lịch tuần"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`environment.week.${i}.day`])"
                  @input="clearFieldError(`environment.week.${i}.day`)"
                />
              </FormField>
              <FormField label="Nội dung *" :for="`week-${i}-text`" :error="fieldErrors[`environment.week.${i}.text`]">
                <InputText
                  :id="`week-${i}-text`"
                  v-model="item.text"
                  maxlength="300"
                  aria-label="Nội dung lịch tuần"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`environment.week.${i}.text`])"
                  @input="clearFieldError(`environment.week.${i}.text`)"
                />
              </FormField>
            </div>
          </ItemCard>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm lịch tuần"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.environment.week.length >= ABOUT_LIMITS.environment.week.max || saving || resetting"
            @click="addWeek"
          />
        </div>

        <!-- Tools -->
        <h4 class="sub-section-title mt-4">Công cụ bạn sẽ dùng (tối đa 12 công cụ)</h4>
        <div class="list-container">
          <div v-for="(tool, i) in form.environment.tools" :key="i" class="tool-row">
            <FormField :label="`Công cụ #${i + 1}`" :for="`tool-${i}-name`" :error="fieldErrors[`environment.tools.${i}`]">
              <div class="tool-input-group">
                <InputText
                  :id="`tool-${i}-name`"
                  v-model="form.environment.tools[i]"
                  maxlength="40"
                  aria-label="Tên công cụ bạn sẽ dùng"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`environment.tools.${i}`])"
                  @input="clearFieldError(`environment.tools.${i}`)"
                />
                <Button
                  icon="pi pi-arrow-up"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting || i === 0"
                  :aria-label="`Di chuyển công cụ #${i + 1} lên trên`"
                  @click="moveUp(form.environment.tools, i)"
                />
                <Button
                  icon="pi pi-arrow-down"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting || i === form.environment.tools.length - 1"
                  :aria-label="`Di chuyển công cụ #${i + 1} xuống dưới`"
                  @click="moveDown(form.environment.tools, i)"
                />
                <Button
                  icon="pi pi-trash"
                  severity="danger"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting"
                  :aria-label="`Xóa công cụ #${i + 1}`"
                  @click="removeItem(form.environment.tools, i)"
                />
              </div>
            </FormField>
          </div>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm công cụ"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.environment.tools.length >= ABOUT_LIMITS.environment.tools.max || saving || resetting"
            @click="addTool"
          />
        </div>
      </FormSection>

      <!-- 6. GIÁ TRỊ CỐT LÕI -->
      <FormSection title="6. Giá trị cốt lõi" description="Các nguyên tắc hoạt động (tối đa 10 mục).">
        <Message v-if="fieldErrors.values" severity="error" :closable="false" class="mb-2">
          {{ fieldErrors.values }}
        </Message>

        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.values"
            :key="i"
            :title="`Giá trị #${i + 1}: ${item.title || ''}`"
            :index="i"
            :total="form.values.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.values, i)"
            @move-down="moveDown(form.values, i)"
            @remove="removeItem(form.values, i)"
          >
            <div class="grid-row-2">
              <FormField label="Biểu tượng *" :for="`value-${i}-icon`" :error="fieldErrors[`values.${i}.icon`]">
                <Select
                  :inputId="`value-${i}-icon`"
                  v-model="item.icon"
                  :options="ALLOWED_ICONS"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="Chọn biểu tượng"
                  aria-label="Chọn biểu tượng giá trị"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`values.${i}.icon`])"
                  @change="clearFieldError(`values.${i}.icon`)"
                />
              </FormField>
              <FormField label="Tiêu đề *" :for="`value-${i}-title`" :error="fieldErrors[`values.${i}.title`]">
                <InputText
                  :id="`value-${i}-title`"
                  v-model="item.title"
                  maxlength="150"
                  aria-label="Tiêu đề giá trị cốt lõi"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`values.${i}.title`])"
                  @input="clearFieldError(`values.${i}.title`)"
                />
              </FormField>
            </div>
            <FormField label="Mô tả *" :for="`value-${i}-text`" :error="fieldErrors[`values.${i}.text`]">
              <Textarea
                :id="`value-${i}-text`"
                v-model="item.text"
                rows="2"
                maxlength="300"
                aria-label="Mô tả giá trị cốt lõi"
                :disabled="saving || resetting"
                :invalid="Boolean(fieldErrors[`values.${i}.text`])"
                @input="clearFieldError(`values.${i}.text`)"
              />
            </FormField>
          </ItemCard>
        </div>

        <div class="add-action-bar">
          <Button
            label="Thêm giá trị"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.values.length >= ABOUT_LIMITS.values.max || saving || resetting"
            @click="addValue"
          />
        </div>
      </FormSection>

      <!-- 7. THÔNG TIN CHO SINH VIÊN -->
      <FormSection title="7. Thông tin cho sinh viên" description="Điều kiện, hồ sơ, quy trình, checklist và câu hỏi thường gặp.">
        <!-- Eligibility -->
        <h4 class="sub-section-title">Điều kiện ứng tuyển (tối đa 10 mục)</h4>
        <div class="list-container">
          <div v-for="(item, i) in form.studentInfo.eligibility" :key="i" class="string-item-row">
            <FormField :label="`Điều kiện #${i + 1}`" :for="`eligibility-${i}`" :error="fieldErrors[`studentInfo.eligibility.${i}`]">
              <div class="tool-input-group">
                <InputText
                  :id="`eligibility-${i}`"
                  v-model="form.studentInfo.eligibility[i]"
                  maxlength="300"
                  aria-label="Nội dung điều kiện ứng tuyển"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`studentInfo.eligibility.${i}`])"
                  @input="clearFieldError(`studentInfo.eligibility.${i}`)"
                />
                <Button
                  icon="pi pi-arrow-up"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting || i === 0"
                  :aria-label="`Di chuyển điều kiện #${i + 1} lên trên`"
                  @click="moveUp(form.studentInfo.eligibility, i)"
                />
                <Button
                  icon="pi pi-arrow-down"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting || i === form.studentInfo.eligibility.length - 1"
                  :aria-label="`Di chuyển điều kiện #${i + 1} xuống dưới`"
                  @click="moveDown(form.studentInfo.eligibility, i)"
                />
                <Button
                  icon="pi pi-trash"
                  severity="danger"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting"
                  :aria-label="`Xóa điều kiện #${i + 1}`"
                  @click="removeItem(form.studentInfo.eligibility, i)"
                />
              </div>
            </FormField>
          </div>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm điều kiện"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.studentInfo.eligibility.length >= ABOUT_LIMITS.studentInfo.eligibility.max || saving || resetting"
            @click="addEligibility"
          />
        </div>

        <!-- Documents -->
        <h4 class="sub-section-title mt-4">Hồ sơ cần chuẩn bị (tối đa 10 mục)</h4>
        <div class="list-container">
          <div v-for="(item, i) in form.studentInfo.documents" :key="i" class="string-item-row">
            <FormField :label="`Giấy tờ #${i + 1}`" :for="`document-${i}`" :error="fieldErrors[`studentInfo.documents.${i}`]">
              <div class="tool-input-group">
                <InputText
                  :id="`document-${i}`"
                  v-model="form.studentInfo.documents[i]"
                  maxlength="150"
                  aria-label="Tên giấy tờ cần chuẩn bị"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`studentInfo.documents.${i}`])"
                  @input="clearFieldError(`studentInfo.documents.${i}`)"
                />
                <Button
                  icon="pi pi-arrow-up"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting || i === 0"
                  :aria-label="`Di chuyển giấy tờ #${i + 1} lên trên`"
                  @click="moveUp(form.studentInfo.documents, i)"
                />
                <Button
                  icon="pi pi-arrow-down"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting || i === form.studentInfo.documents.length - 1"
                  :aria-label="`Di chuyển giấy tờ #${i + 1} xuống dưới`"
                  @click="moveDown(form.studentInfo.documents, i)"
                />
                <Button
                  icon="pi pi-trash"
                  severity="danger"
                  text
                  rounded
                  size="small"
                  :disabled="saving || resetting"
                  :aria-label="`Xóa giấy tờ #${i + 1}`"
                  @click="removeItem(form.studentInfo.documents, i)"
                />
              </div>
            </FormField>
          </div>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm hồ sơ"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.studentInfo.documents.length >= ABOUT_LIMITS.studentInfo.documents.max || saving || resetting"
            @click="addDocument"
          />
        </div>

        <!-- Process -->
        <h4 class="sub-section-title mt-4">Quy trình từ lúc nộp đến khi hoàn thành (tối đa 8 bước)</h4>
        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.studentInfo.process"
            :key="i"
            :title="`Bước ${i + 1}: ${item.status || ''}`"
            :index="i"
            :total="form.studentInfo.process.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.studentInfo.process, i)"
            @move-down="moveDown(form.studentInfo.process, i)"
            @remove="removeItem(form.studentInfo.process, i)"
          >
            <div class="grid-row-2">
              <FormField label="Trạng thái *" :for="`process-${i}-status`" :error="fieldErrors[`studentInfo.process.${i}.status`]">
                <InputText
                  :id="`process-${i}-status`"
                  v-model="item.status"
                  maxlength="50"
                  aria-label="Trạng thái bước quy trình"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`studentInfo.process.${i}.status`])"
                  @input="clearFieldError(`studentInfo.process.${i}.status`)"
                />
              </FormField>
              <FormField label="Mô tả *" :for="`process-${i}-text`" :error="fieldErrors[`studentInfo.process.${i}.text`]">
                <InputText
                  :id="`process-${i}-text`"
                  v-model="item.text"
                  maxlength="300"
                  aria-label="Mô tả bước quy trình"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`studentInfo.process.${i}.text`])"
                  @input="clearFieldError(`studentInfo.process.${i}.text`)"
                />
              </FormField>
            </div>
          </ItemCard>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm bước quy trình"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.studentInfo.process.length >= ABOUT_LIMITS.studentInfo.process.max || saving || resetting"
            @click="addProcess"
          />
        </div>

        <!-- Checklist -->
        <h4 class="sub-section-title mt-4">Checklist kiểm tra nhanh (tối đa 10 mục)</h4>
        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.studentInfo.checklist"
            :key="i"
            :title="`Mục kiểm tra #${i + 1}`"
            :index="i"
            :total="form.studentInfo.checklist.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.studentInfo.checklist, i)"
            @move-down="moveDown(form.studentInfo.checklist, i)"
            @remove="removeItem(form.studentInfo.checklist, i)"
          >
            <div class="grid-row-2">
              <FormField label="Mã định danh (ID) *" :for="`checklist-${i}-id`" :error="fieldErrors[`studentInfo.checklist.${i}.id`]">
                <InputText
                  :id="`checklist-${i}-id`"
                  v-model="item.id"
                  maxlength="40"
                  aria-label="Mã định danh checklist"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`studentInfo.checklist.${i}.id`])"
                  @input="clearFieldError(`studentInfo.checklist.${i}.id`)"
                />
              </FormField>
              <FormField label="Nội dung kiểm tra *" :for="`checklist-${i}-label`" :error="fieldErrors[`studentInfo.checklist.${i}.label`]">
                <InputText
                  :id="`checklist-${i}-label`"
                  v-model="item.label"
                  maxlength="200"
                  aria-label="Nội dung mục kiểm tra"
                  :disabled="saving || resetting"
                  :invalid="Boolean(fieldErrors[`studentInfo.checklist.${i}.label`])"
                  @input="clearFieldError(`studentInfo.checklist.${i}.label`)"
                />
              </FormField>
            </div>
          </ItemCard>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm mục checklist"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.studentInfo.checklist.length >= ABOUT_LIMITS.studentInfo.checklist.max || saving || resetting"
            @click="addChecklist"
          />
        </div>

        <!-- FAQ -->
        <h4 class="sub-section-title mt-4">Câu hỏi thường gặp (tối đa 15 câu)</h4>
        <div class="list-container">
          <ItemCard
            v-for="(item, i) in form.studentInfo.faq"
            :key="i"
            :title="`Hỏi đáp #${i + 1}`"
            :index="i"
            :total="form.studentInfo.faq.length"
            :disable-actions="saving || resetting"
            @move-up="moveUp(form.studentInfo.faq, i)"
            @move-down="moveDown(form.studentInfo.faq, i)"
            @remove="removeItem(form.studentInfo.faq, i)"
          >
            <FormField label="Câu hỏi *" :for="`faq-${i}-q`" :error="fieldErrors[`studentInfo.faq.${i}.q`]">
              <InputText
                :id="`faq-${i}-q`"
                v-model="item.q"
                maxlength="200"
                aria-label="Câu hỏi thường gặp"
                :disabled="saving || resetting"
                :invalid="Boolean(fieldErrors[`studentInfo.faq.${i}.q`])"
                @input="clearFieldError(`studentInfo.faq.${i}.q`)"
              />
            </FormField>
            <FormField label="Câu trả lời *" :for="`faq-${i}-a`" :error="fieldErrors[`studentInfo.faq.${i}.a`]">
              <Textarea
                :id="`faq-${i}-a`"
                v-model="item.a"
                rows="3"
                maxlength="1000"
                aria-label="Câu trả lời thường gặp"
                :disabled="saving || resetting"
                :invalid="Boolean(fieldErrors[`studentInfo.faq.${i}.a`])"
                @input="clearFieldError(`studentInfo.faq.${i}.a`)"
              />
            </FormField>
          </ItemCard>
        </div>
        <div class="add-action-bar">
          <Button
            label="Thêm câu hỏi FAQ"
            icon="pi pi-plus"
            severity="secondary"
            outlined
            size="small"
            :disabled="form.studentInfo.faq.length >= ABOUT_LIMITS.studentInfo.faq.max || saving || resetting"
            @click="addFaq"
          />
        </div>
      </FormSection>

      <!-- 8. THÔNG TIN LIÊN HỆ -->
      <FormSection title="8. Thông tin liên hệ" description="Người phụ trách và thông tin liên lạc tại đơn vị tiếp nhận.">
        <FormField label="Người phụ trách" for="contact-person" optional :error="fieldErrors['contact.person']">
          <InputText
            id="contact-person"
            v-model="form.contact.person"
            maxlength="150"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors['contact.person'])"
            @input="clearFieldError('contact.person')"
          />
        </FormField>

        <FormField label="Địa chỉ" for="contact-address" optional :error="fieldErrors['contact.address']">
          <InputText
            id="contact-address"
            v-model="form.contact.address"
            maxlength="300"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors['contact.address'])"
            @input="clearFieldError('contact.address')"
          />
        </FormField>

        <FormField label="Giờ làm việc" for="contact-hours" optional :error="fieldErrors['contact.hours']">
          <InputText
            id="contact-hours"
            v-model="form.contact.hours"
            maxlength="100"
            :disabled="saving || resetting"
            :invalid="Boolean(fieldErrors['contact.hours'])"
            @input="clearFieldError('contact.hours')"
          />
        </FormField>

        <div class="grid-row-2">
          <FormField label="Email liên hệ" for="contact-email" optional :error="fieldErrors['contact.email']">
            <InputText
              id="contact-email"
              v-model="form.contact.email"
              type="email"
              maxlength="150"
              :disabled="saving || resetting"
              :invalid="Boolean(fieldErrors['contact.email'])"
              @input="clearFieldError('contact.email')"
            />
          </FormField>

          <FormField label="Số điện thoại" for="contact-phone" optional :error="fieldErrors['contact.phone']">
            <InputText
              id="contact-phone"
              v-model="form.contact.phone"
              maxlength="20"
              :disabled="saving || resetting"
              :invalid="Boolean(fieldErrors['contact.phone'])"
              @input="clearFieldError('contact.phone')"
            />
          </FormField>
        </div>
      </FormSection>

      <!-- FORM ACTION BAR -->
      <FormActionBar>
        <Button
          label="Khôi phục mặc định"
          icon="pi pi-undo"
          severity="warn"
          text
          :disabled="saving || resetting || !formLoaded"
          @click="confirmReset"
        />
        <Button
          label="Hủy"
          severity="secondary"
          text
          :disabled="saving || resetting"
          @click="router.push({ name: 'about' })"
        />
        <Button
          label="Lưu thay đổi"
          icon="pi pi-check"
          type="submit"
          class="btn-primary"
          :loading="saving"
          :disabled="saving || resetting || !formLoaded"
        />
      </FormActionBar>
    </form>
  </FormPage>
</template>

<style scoped>
.about-edit-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.about-edit-form :deep(.p-inputtext),
.about-edit-form :deep(.p-textarea),
.about-edit-form :deep(.p-select) {
  width: 100%;
}

.list-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.grid-row-2 {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.grid-row-3 {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr) minmax(0, 1fr);
  gap: 16px;
}

.sub-section-title {
  margin: 16px 0 8px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-h, #1e293b);
}

.mt-4 {
  margin-top: 1.25rem;
}

.add-action-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 8px;
}

.limit-note {
  font-size: 13px;
  color: var(--text-muted, #64748b);
  font-style: italic;
}

.tags-manager {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 8px 12px;
  background: var(--surface, #ffffff);
  border: 1px dashed var(--border, #cbd5e1);
  border-radius: var(--radius-sm, 6px);
}

.tag-manager-label {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-muted, #64748b);
}

.tags-list {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.tag-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 10px;
  background: var(--surface-soft, #f1f5f9);
  border: 1px solid var(--border, #e2e8f0);
  border-radius: var(--radius-pill, 9999px);
  font-size: 12px;
  font-weight: 500;
}

.btn-remove-tag {
  background: transparent;
  border: none;
  cursor: pointer;
  font-size: 14px;
  line-height: 1;
  color: var(--text-muted, #64748b);
  padding: 0;
}

.btn-remove-tag:hover {
  color: var(--ui-error, #ef4444);
}

.tag-add-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 4px;
}

.tool-input-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.tool-input-group :deep(.p-inputtext) {
  flex: 1;
}

.server-error-list {
  margin: 6px 0 0;
  padding-left: 20px;
  font-size: 13px;
}

@media (max-width: 768px) {
  .grid-row-2,
  .grid-row-3 {
    grid-template-columns: 1fr;
    gap: 12px;
  }
}
</style>
