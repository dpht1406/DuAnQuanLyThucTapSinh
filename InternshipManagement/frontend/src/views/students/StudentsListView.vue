<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import { createStudent, deleteStudent, getStudents, updateStudent } from '../../api/students.js'
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
const dialogVisible = ref(false)
const dialogMode = ref('create')
const editingId = ref(null)
const saving = ref(false)
const dialogErrorMessage = ref('')
const studentForm = reactive({
  StudentCode: '',
  FullName: '',
  Email: '',
  PhoneNumber: '',
  Major: '',
  ClassName: ''
})

const confirm = useConfirm()

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

function resetStudentForm() {
  studentForm.StudentCode = ''
  studentForm.FullName = ''
  studentForm.Email = ''
  studentForm.PhoneNumber = ''
  studentForm.Major = ''
  studentForm.ClassName = ''
}

function openCreateDialog() {
  resetStudentForm()
  dialogMode.value = 'create'
  editingId.value = null
  dialogErrorMessage.value = ''
  dialogVisible.value = true
}

function openEditDialog(student) {
  studentForm.StudentCode = student.studentCode ?? ''
  studentForm.FullName = student.fullName ?? ''
  studentForm.Email = student.email ?? ''
  studentForm.PhoneNumber = student.phoneNumber ?? ''
  studentForm.Major = student.major ?? ''
  studentForm.ClassName = student.className ?? ''
  dialogMode.value = 'edit'
  editingId.value = student.id
  dialogErrorMessage.value = ''
  dialogVisible.value = true
}

function closeStudentDialog() {
  if (saving.value) return
  dialogVisible.value = false
}

async function submitStudent() {
  saving.value = true
  dialogErrorMessage.value = ''

  try {
    if (dialogMode.value === 'create') {
      await createStudent({ ...studentForm })
    } else {
      const { StudentCode, ...updateDto } = studentForm
      await updateStudent(editingId.value, updateDto)
    }
    dialogVisible.value = false
    resetStudentForm()
    await fetchStudents()
  } catch (err) {
    dialogErrorMessage.value = extractErrorMessage(err)
  } finally {
    saving.value = false
  }
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
  loadCompanyOptions()
  fetchStudents()
})

onBeforeUnmount(() => {
  clearTimeout(searchDebounceTimer)
})
</script>

<template>
  <Card>
    <template #title>Danh sách sinh viên</template>
    <template #content>
      <div class="students-toolbar">
        <div class="toolbar-field toolbar-search">
          <label for="student-search">Tìm kiếm</label>
          <InputText
            id="student-search"
            v-model="searchInput"
            placeholder="MSSV hoặc họ tên"
            class="w-full"
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
          @click="openCreateDialog"
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
                icon="pi pi-pencil"
                severity="secondary"
                text
                rounded
                aria-label="Sửa"
                @click="openEditDialog(data)"
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

      <Dialog
        v-model:visible="dialogVisible"
        modal
        :header="dialogMode === 'create' ? 'Thêm sinh viên' : 'Sửa sinh viên'"
        :closable="!saving"
        :style="{ width: 'min(32rem, calc(100vw - 2rem))' }"
        @hide="resetStudentForm"
      >
        <Message v-if="dialogErrorMessage" severity="error" :closable="false">
          {{ dialogErrorMessage }}
        </Message>
        <form class="student-form" @submit.prevent="submitStudent">
          <div class="form-field">
            <label for="student-code">MSSV</label>
            <InputText
              id="student-code"
              v-model="studentForm.StudentCode"
              :disabled="dialogMode === 'edit' || saving"
              required
            />
          </div>
          <div class="form-field">
            <label for="student-full-name">Họ tên</label>
            <InputText id="student-full-name" v-model="studentForm.FullName" :disabled="saving" required />
          </div>
          <div class="form-field">
            <label for="student-email">Email</label>
            <InputText id="student-email" v-model="studentForm.Email" :disabled="saving" required />
          </div>
          <div class="form-field">
            <label for="student-phone">Số điện thoại</label>
            <InputText id="student-phone" v-model="studentForm.PhoneNumber" :disabled="saving" required />
          </div>
          <div class="form-field">
            <label for="student-major">Ngành</label>
            <InputText id="student-major" v-model="studentForm.Major" :disabled="saving" required />
          </div>
          <div class="form-field">
            <label for="student-class">Lớp</label>
            <InputText id="student-class" v-model="studentForm.ClassName" :disabled="saving" required />
          </div>
          <div class="dialog-actions">
            <Button type="button" label="Hủy" severity="secondary" text :disabled="saving" @click="closeStudentDialog" />
            <Button type="submit" label="Lưu" icon="pi pi-check" :loading="saving" />
          </div>
        </form>
      </Dialog>
    </template>
  </Card>
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

.action-buttons,
.dialog-actions {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.student-form {
  display: grid;
  gap: 1rem;
  margin-top: 0.25rem;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.form-field label {
  font-size: 0.875rem;
  color: var(--text-h);
}

.dialog-actions {
  justify-content: flex-end;
  margin-top: 0.25rem;
}
</style>
