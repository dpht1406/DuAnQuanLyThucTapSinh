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
import StudentStatusDialog from '../../components/students/StudentStatusDialog.vue'
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
const statusDialogVisible = ref(false)
const selectedStatusStudent = ref(null)

let searchDebounceTimer = null

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

function openStatusDialog(student) {
  selectedStatusStudent.value = student
  statusDialogVisible.value = true
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
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getStudents(filter)
    students.value = result.items ?? []
    totalRecords.value = result.totalCount ?? 0
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message?.trim() ||
      err?.message?.trim() ||
      'Không tải được danh sách sinh viên, vui lòng thử lại.'
    students.value = []
    totalRecords.value = 0
  } finally {
    loading.value = false
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
    filter.Search = searchInput.value
    filter.PageNumber = 1
    fetchStudents()
  }, 400)
})

onMounted(() => {
  const queryStatus = Number(route.query.status)
  if (Number.isInteger(queryStatus) && queryStatus >= 0 && queryStatus <= 5) {
    filter.Status = queryStatus
  }
  loadCompanyOptions()
  fetchStudents()
})

onBeforeUnmount(() => {
  clearTimeout(searchDebounceTimer)
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
            placeholder="MSSV hoặc họ tên"
            class="w-full"
            maxlength="150"
            :disabled="loading"
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
        <Button
          label="Thêm mới"
          icon="pi pi-plus"
          class="toolbar-create"
          @click="router.push({ name: 'students-new' })"
        />
      </div>

      <Message v-if="errorMessage" severity="error" :closable="false" class="students-error">
        {{ errorMessage }}
      </Message>

      <DataTable
        :value="students"
        :lazy="true"
        :paginator="true"
        :rows="filter.PageSize"
        :total-records="totalRecords"
        :loading="loading"
        :first="(filter.PageNumber - 1) * filter.PageSize"
        data-key="id"
        class="students-table"
        @page="onPage"
      >
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
                @click="openStatusDialog(data)"
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

      <StudentStatusDialog
        v-model:visible="statusDialogVisible"
        :student="selectedStatusStudent"
        @changed="fetchStudents"
      />
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

@media (min-width: 768px) {
  .students-toolbar {
    grid-template-columns: 1.5fr 1fr 1fr auto;
    align-items: end;
  }
}

.toolbar-create {
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
</style>
