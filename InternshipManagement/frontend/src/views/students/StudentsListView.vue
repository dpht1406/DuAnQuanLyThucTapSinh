<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import { deleteStudent, getStudents } from '../../api/students.js'
import { getCompanies } from '../../api/companies.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import {
  STATUS_OPTIONS,
  STATUS_LABELS,
  STATUS_SEVERITY
} from '../../utils/studentStatus.js'

const filter = reactive({
  Search: '',
  Status: null,
  CompanyId: null,
  PageNumber: 1,
  PageSize: 10
})

const searchInput = ref('')
const students = ref([])
const totalRecords = ref(0)
const loading = ref(false)
const errorMessage = ref('')
const companyOptions = ref([])

const confirm = useConfirm()
const route = useRoute()
const router = useRouter()

let searchDebounceTimer = null
let latestRequestId = 0
const isMobile = ref(false)
let mobileMediaQuery = null

const paginatorTemplate = computed(() => {
  const template = isMobile.value
    ? 'PrevPageLink CurrentPageReport NextPageLink'
    : 'FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown'

  return totalRecords.value > 0
    ? template
    : template.replace(' CurrentPageReport', '')
})

const statusFilterOptions = [
  { label: 'Tất cả trạng thái', value: null },
  ...STATUS_OPTIONS
]

const companyFilterOptions = computed(() => [
  { label: 'Tất cả doanh nghiệp', value: null },
  ...companyOptions.value
])

function emptyDisplay(value) {
  return value == null || value === '' ? '—' : value
}

function onMobileMediaChange(event) {
  isMobile.value = event.matches
}

function openStudentDetails(student) {
  router.push({ name: 'student-detail', params: { id: student.id } })
}

function confirmDelete(student) {
  confirm.require({
    message: `Bạn có chắc muốn xóa sinh viên ${student.fullName || student.studentCode}?`,
    header: 'Xác nhận xóa',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy',
    acceptLabel: 'Đồng ý',
    rejectClass: 'p-button-secondary p-button-text',
    acceptClass: 'p-button-danger',
    accept: async () => {
      errorMessage.value = ''
      try {
        await deleteStudent(student.id)
        students.value = students.value.filter((item) => item.id !== student.id)
        if (students.value.length === 0 && filter.PageNumber > 1) {
          filter.PageNumber -= 1
        }
        await fetchStudents()
      } catch (err) {
        errorMessage.value = extractErrorMessage(
          err,
          'Không xóa được sinh viên, vui lòng thử lại.'
        )
      }
    }
  })
}

async function fetchStudents() {
  const requestId = ++latestRequestId
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getStudents({ ...filter })
    if (requestId !== latestRequestId) return
    students.value = result.items ?? []
    totalRecords.value = result.totalCount ?? 0
  } catch (err) {
    if (requestId !== latestRequestId) return
    errorMessage.value = extractErrorMessage(err, 'Không tải được danh sách sinh viên, vui lòng thử lại.')
    students.value = []
    totalRecords.value = 0
  } finally {
    if (requestId === latestRequestId) loading.value = false
  }
}

async function loadCompanyOptions() {
  try {
    const result = await getCompanies({ pageSize: 100 })
    const items = result.items ?? []
    companyOptions.value = items.map((c) => ({
      label: c.name,
      value: c.id
    }))
  } catch {
    companyOptions.value = []
  }
}

function onPage(event) {
  filter.PageNumber = event.page + 1
  filter.PageSize = event.rows
  fetchStudents()
}

function onStatusChange() {
  filter.PageNumber = 1
  fetchStudents()
}

function onCompanyChange() {
  filter.PageNumber = 1
  fetchStudents()
}

watch(searchInput, () => {
  clearTimeout(searchDebounceTimer)
  searchDebounceTimer = setTimeout(() => {
    const search = searchInput.value.trim()
    if (search === filter.Search) return
    filter.Search = search
    filter.PageNumber = 1
    fetchStudents()
  }, 400)
})

onMounted(() => {
  const queryStatus = Number(route.query.status)
  if (Number.isInteger(queryStatus) && queryStatus >= 0 && queryStatus <= 5) {
    filter.Status = queryStatus
  }
  mobileMediaQuery = window.matchMedia('(max-width: 640px)')
  isMobile.value = mobileMediaQuery.matches
  mobileMediaQuery.addEventListener('change', onMobileMediaChange)
  loadCompanyOptions()
  fetchStudents()
})

onBeforeUnmount(() => {
  clearTimeout(searchDebounceTimer)
  latestRequestId++
  mobileMediaQuery?.removeEventListener('change', onMobileMediaChange)
})
</script>

<template>
  <div class="page-container">
  <Card>
    <template #title><span class="page-title">Danh sách sinh viên</span></template>
    <template #content>
      <div class="students-toolbar">
        <div class="toolbar-field toolbar-search">
          <label for="student-search">Tìm kiếm</label>
          <InputText
            id="student-search"
            v-model="searchInput"
            placeholder="Họ tên, MSSV hoặc email"
            class="w-full"
            maxlength="150"
          />
        </div>
        <div class="toolbar-field">
          <label for="student-status">Trạng thái</label>
          <Select
            id="student-status"
            v-model="filter.Status"
            :options="statusFilterOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="loading"
            @update:model-value="onStatusChange"
          />
        </div>
        <div class="toolbar-field">
          <label for="student-company">Doanh nghiệp</label>
          <Select
            id="student-company"
            v-model="filter.CompanyId"
            :options="companyFilterOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="loading"
            @update:model-value="onCompanyChange"
          />
        </div>
        <div class="toolbar-actions">
          <Button
            label="Thêm mới"
            icon="pi pi-plus"
            class="toolbar-create"
            @click="router.push({ name: 'students-new' })"
          />
          <Button
            label="Nhập từ file"
            icon="pi pi-file-import"
            severity="secondary"
            outlined
            class="toolbar-import"
            @click="router.push({ name: 'students-import' })"
          />
        </div>
      </div>

      <Message v-if="errorMessage" severity="error" :closable="false" class="students-error">
        {{ errorMessage }}
      </Message>

      <DataTable
        :value="students"
        :lazy="true"
        :paginator="true"
        :rows="filter.PageSize"
        :rows-per-page-options="[10, 20, 50]"
        :paginator-template="paginatorTemplate"
        :current-page-report-template="isMobile
          ? 'Trang {currentPage}/{totalPages} · {totalRecords} sinh viên'
          : 'Hiển thị {first}–{last} trên {totalRecords} sinh viên'"
        :total-records="totalRecords"
        :loading="loading"
        :first="(filter.PageNumber - 1) * filter.PageSize"
        data-key="id"
        class="students-table"
        @page="onPage"
      >
        <template #empty>
          <div class="students-empty">
            <template v-if="filter.Search">
              <p>Không tìm thấy sinh viên phù hợp với '{{ filter.Search }}'</p>
              <Button label="Xóa tìm kiếm" text @click="searchInput = ''" />
            </template>
            <p v-else>Chưa có sinh viên nào</p>
          </div>
        </template>
        <Column field="studentCode" header="MSSV" />
        <Column field="fullName" header="Họ tên" />
        <Column field="className" header="Lớp" />
        <Column field="major" header="Ngành" />
        <Column header="Trạng thái">
          <template #body="{ data }">
            <Tag
              :value="STATUS_LABELS[data.status]"
              :severity="STATUS_SEVERITY[data.status]"
            />
          </template>
        </Column>
        <Column header="Doanh nghiệp">
          <template #body="{ data }">
            {{ emptyDisplay(data.companyName) }}
          </template>
        </Column>
        <Column header="Vị trí">
          <template #body="{ data }">
            {{ emptyDisplay(data.jobPositionTitle) }}
          </template>
        </Column>
        <Column header="Thao tác">
          <template #body="{ data }">
            <div class="action-buttons">
              <Button
                icon="pi pi-sync"
                severity="info"
                text
                rounded
                aria-label="Đổi giai đoạn"
                title="Đổi giai đoạn"
                @click="router.push({ name: 'student-status', params: { id: data.id }, query: { from: 'list' } })"
              />
              <Button
                icon="pi pi-eye"
                severity="secondary"
                text
                rounded
                aria-label="Xem chi tiết"
                title="Xem chi tiết"
                @click="openStudentDetails(data)"
              />
              <Button
                icon="pi pi-pencil"
                severity="secondary"
                text
                rounded
                aria-label="Sửa thông tin"
                title="Sửa thông tin"
                @click="router.push({ name: 'student-edit', params: { id: data.id }, query: { from: 'list' } })"
              />
              <Button
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                aria-label="Xóa"
                @click="confirmDelete(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>

    </template>
  </Card>
  </div>
</template>

<style scoped>
.students-toolbar {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1rem;
  margin-bottom: 1rem;
}

@media (min-width: 1024px) {
  .students-toolbar {
    grid-template-columns: minmax(0, 1.5fr) minmax(10rem, 1fr) minmax(10rem, 1fr) auto;
    align-items: end;
  }
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.toolbar-create,
.toolbar-import {
  white-space: nowrap;
}

.toolbar-field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.toolbar-field label {
  font-size: 0.875rem;
  color: var(--text-h);
}

.students-error {
  margin-bottom: 1rem;
}

.students-table {
  margin-top: 0.25rem;
}

.w-full {
  width: 100%;
}

.action-buttons {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

@media (max-width: 767px) {
  .toolbar-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .toolbar-actions :deep(.p-button) {
    width: 100%;
  }
}

@media (max-width: 640px) {
  .students-table :deep(.p-paginator) {
    flex-wrap: wrap;
    gap: 0.25rem;
    padding-inline: 0.25rem;
  }

  .students-table :deep(.p-paginator-prev),
  .students-table :deep(.p-paginator-next) {
    min-width: 40px;
    min-height: 40px;
  }
}
</style>
