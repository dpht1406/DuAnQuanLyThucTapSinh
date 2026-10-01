<script setup>
import { onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import FileUpload from 'primevue/fileupload'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Select from 'primevue/select'
import TabPanel from 'primevue/tabpanel'
import TabView from 'primevue/tabview'
import Tag from 'primevue/tag'
import { useConfirm } from 'primevue/useconfirm'
import {
  createAccounts,
  createAccountsByFilter,
  exportAccountsCsv,
  getStudents,
  importStudents
} from '../../api/students.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const filter = reactive({
  Search: '',
  Status: null,
  CompanyId: null,
  HasAccount: null,
  PageNumber: 1,
  PageSize: 10
})

const searchInput = ref('')
const students = ref([])
const totalRecords = ref(0)
const loadingStudents = ref(false)
const studentsError = ref('')
const selectedStudents = ref([])

const selectedFile = ref(null)
const fileUpload = ref(null)
const importing = ref(false)
const importError = ref('')
const importResult = ref(null)

const creatingAccounts = ref(false)
const createError = ref('')
const accountResult = ref(null)
const resultDialogVisible = ref(false)
const exporting = ref(false)
const confirm = useConfirm()
let searchDebounceTimer = null

const accountFilterOptions = [
  { label: 'Tất cả', value: null },
  { label: 'Đã có tài khoản', value: true },
  { label: 'Chưa có tài khoản', value: false }
]

async function fetchStudents() {
  loadingStudents.value = true
  studentsError.value = ''

  try {
    const result = await getStudents(filter)
    students.value = result.items ?? []
    totalRecords.value = result.totalCount ?? 0
  } catch (err) {
    studentsError.value = extractErrorMessage(err, 'Không tải được danh sách sinh viên, vui lòng thử lại.')
    students.value = []
    totalRecords.value = 0
  } finally {
    loadingStudents.value = false
  }
}

function onPage(event) {
  filter.PageNumber = event.page + 1
  filter.PageSize = event.rows
  fetchStudents()
}

function onAccountStatusChange() {
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

function onFileSelect(event) {
  selectedFile.value = event.files?.[0] ?? null
  importError.value = ''
  importResult.value = null
}

async function uploadStudents() {
  if (!selectedFile.value) return

  importing.value = true
  importError.value = ''
  importResult.value = null

  try {
    importResult.value = await importStudents(selectedFile.value)
    if ((importResult.value?.successCount ?? 0) > 0) {
      await fetchStudents()
    }
    selectedFile.value = null
    fileUpload.value?.clear()
  } catch (err) {
    importError.value = extractErrorMessage(err, 'Không thể import sinh viên, vui lòng thử lại.')
  } finally {
    importing.value = false
  }
}

function onSelectionChange(selection) {
  selectedStudents.value = (selection ?? []).filter((student) => !student.hasAccount)
}

async function submitCreateAccounts() {
  if (selectedStudents.value.length === 0) return

  creatingAccounts.value = true
  createError.value = ''

  try {
    openAccountResult(await createAccounts(selectedStudents.value.map((student) => student.id)))
  } catch (err) {
    createError.value = extractErrorMessage(err, 'Không thể tạo tài khoản, vui lòng thử lại.')
  } finally {
    creatingAccounts.value = false
  }
}

function openAccountResult(result) {
  accountResult.value = result
  resultDialogVisible.value = true
}

function submitCreateAccountsByFilter() {
  if (totalRecords.value === 0) return

  confirm.require({
    message: `Bạn sắp tạo tài khoản cho TẤT CẢ ${totalRecords.value} sinh viên đang khớp bộ lọc hiện tại (không chỉ trang đang xem). Tiếp tục?`,
    header: 'Xác nhận tạo tài khoản',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy',
    acceptLabel: 'Tiếp tục',
    rejectClass: 'p-button-secondary p-button-text',
    accept: async () => {
      creatingAccounts.value = true
      createError.value = ''
      try {
        openAccountResult(await createAccountsByFilter({
          Search: filter.Search,
          Status: filter.Status,
          CompanyId: filter.CompanyId,
          HasAccount: filter.HasAccount
        }))
      } catch (err) {
        createError.value = extractErrorMessage(err, 'Không thể tạo tài khoản, vui lòng thử lại.')
      } finally {
        creatingAccounts.value = false
      }
    }
  })
}

function getFileName(response) {
  const header = response.headers?.['content-disposition'] ?? ''
  const match = header.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i)
  return match?.[1] ? decodeURIComponent(match[1]) : 'accounts.csv'
}

async function downloadAccountsCsv() {
  if (!accountResult.value?.createdAccounts?.length) return

  exporting.value = true
  createError.value = ''
  try {
    const response = await exportAccountsCsv(accountResult.value.createdAccounts)
    const url = URL.createObjectURL(response.data)
    const link = document.createElement('a')
    link.href = url
    link.download = getFileName(response)
    link.click()
    URL.revokeObjectURL(url)
  } catch (err) {
    createError.value = extractErrorMessage(err, 'Không thể tải file CSV, vui lòng thử lại.')
  } finally {
    exporting.value = false
  }
}

function closeResultDialog() {
  resultDialogVisible.value = false
  selectedStudents.value = []
  accountResult.value = null
  createError.value = ''
  fetchStudents()
}

onMounted(fetchStudents)

onBeforeUnmount(() => {
  clearTimeout(searchDebounceTimer)
})
</script>

<template>
  <div class="page-container">
  <Card>
    <template #title><span class="page-title">Tạo tài khoản</span></template>
    <template #content>
      <TabView>
        <TabPanel header="Nhập từ file">
          <div class="import-panel">
            <FileUpload
              ref="fileUpload"
              mode="basic"
              accept=".xlsx,.xls,.csv"
              :auto="false"
              choose-label="Chọn file"
              :disabled="importing"
              @select="onFileSelect"
            />
            <span v-if="selectedFile" class="selected-file">{{ selectedFile.name }}</span>
            <Button
              label="Tải lên"
              icon="pi pi-upload"
              :loading="importing"
              :disabled="!selectedFile"
              @click="uploadStudents"
            />
          </div>

          <Message v-if="importError" severity="error" :closable="false" class="view-message">
            {{ importError }}
          </Message>
          <Message v-if="importResult" severity="success" :closable="false" class="view-message">
            Đã import thành công {{ importResult.successCount }} sinh viên.
          </Message>
          <Message
            v-if="importResult?.errors?.length"
            severity="warn"
            :closable="false"
            class="view-message"
          >
            Các dòng lỗi dưới đây đã được bỏ qua.
          </Message>
          <DataTable
            v-if="importResult?.errors?.length"
            :value="importResult.errors"
            size="small"
            class="errors-table"
          >
            <Column field="rowNumber" header="Dòng" />
            <Column field="reason" header="Lý do" />
          </DataTable>
        </TabPanel>

        <TabPanel header="Tạo tài khoản">
          <div class="filters-toolbar">
            <div class="toolbar-field toolbar-search">
              <label for="account-search">Tìm kiếm</label>
              <InputText
                id="account-search"
                v-model="searchInput"
                placeholder="MSSV hoặc họ tên"
                class="w-full"
                :disabled="loadingStudents"
              />
            </div>
            <div class="toolbar-field">
              <label for="account-status">Trạng thái tài khoản</label>
              <Select
                id="account-status"
                v-model="filter.HasAccount"
                :options="accountFilterOptions"
                option-label="label"
                option-value="value"
                class="w-full"
                :disabled="loadingStudents"
                @update:model-value="onAccountStatusChange"
              />
            </div>
          </div>
          <div class="accounts-toolbar">
            <Button
              :label="`Tạo tài khoản cho ${selectedStudents.length} sinh viên đã chọn`"
              icon="pi pi-user-plus"
              :loading="creatingAccounts"
              :disabled="selectedStudents.length === 0"
              @click="submitCreateAccounts"
            />
            <Button
              :label="`Tạo tài khoản cho tất cả theo bộ lọc (~${totalRecords} sinh viên)`"
              icon="pi pi-users"
              severity="secondary"
              :loading="creatingAccounts"
              :disabled="totalRecords === 0"
              @click="submitCreateAccountsByFilter"
            />
          </div>
          <Message v-if="studentsError" severity="error" :closable="false" class="view-message">
            {{ studentsError }}
          </Message>
          <Message v-if="createError" severity="error" :closable="false" class="view-message">
            {{ createError }}
          </Message>

          <DataTable
            v-model:selection="selectedStudents"
            :value="students"
            :lazy="true"
            :paginator="true"
            :rows="filter.PageSize"
            :total-records="totalRecords"
            :loading="loadingStudents"
            :first="(filter.PageNumber - 1) * filter.PageSize"
            data-key="id"
            selection-mode="multiple"
            :row-class="(data) => data.hasAccount ? 'row-disabled' : ''"
            class="accounts-table"
            @update:selection="onSelectionChange"
            @page="onPage"
          >
            <Column field="studentCode" header="MSSV" />
            <Column field="fullName" header="Họ tên" />
            <Column field="className" header="Lớp" />
            <Column header="Tài khoản">
              <template #body="{ data }">
                <Tag :value="data.hasAccount ? 'Đã có' : 'Chưa có'" :severity="data.hasAccount ? 'success' : 'secondary'" />
              </template>
            </Column>
          </DataTable>
        </TabPanel>
      </TabView>

      <Dialog
        v-model:visible="resultDialogVisible"
        modal
        header="Tài khoản đã tạo"
        :closable="!creatingAccounts && !exporting && !(accountResult?.successCount > 0)"
        :dismissable-mask="false"
        :style="{ width: 'min(42rem, calc(100vw - 2rem))' }"
        @hide="closeResultDialog"
      >
        <Message severity="success" :closable="false">
          Đã tạo {{ accountResult?.successCount ?? 0 }} tài khoản, bỏ qua {{ accountResult?.skippedCount ?? 0 }} (đã có tài khoản).
        </Message>
        <template v-if="accountResult?.successCount > 0">
          <Message severity="warn" :closable="false">
            Mật khẩu chỉ hiển thị một lần. Hãy tải file CSV ngay trước khi đóng.
          </Message>
          <DataTable :value="accountResult.createdAccounts" size="small" class="accounts-result-table">
            <Column field="studentCode" header="MSSV" />
            <Column field="plainPassword" header="Mật khẩu" />
          </DataTable>
        </template>
        <Message v-if="createError" severity="error" :closable="false" class="view-message">
          {{ createError }}
        </Message>
        <div class="dialog-actions">
          <Button
            label="Tải CSV"
            icon="pi pi-download"
            :loading="exporting"
            :disabled="!accountResult?.createdAccounts?.length"
            @click="downloadAccountsCsv"
          />
          <Button label="Đóng" severity="secondary" icon="pi pi-times" @click="closeResultDialog" />
        </div>
      </Dialog>
    </template>
  </Card>
  </div>
</template>

<style scoped>
.import-panel,
.filters-toolbar,
.accounts-toolbar,
.dialog-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.import-panel {
  flex-wrap: wrap;
  margin-bottom: 1rem;
}

.selected-file {
  color: var(--text-color);
  overflow-wrap: anywhere;
}

.accounts-toolbar {
  flex-wrap: wrap;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.filters-toolbar {
  display: grid;
  grid-template-columns: 1fr;
  margin-bottom: 1rem;
}

@media (min-width: 768px) {
  .filters-toolbar {
    grid-template-columns: 1.5fr 1fr;
  }
}

.toolbar-field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.toolbar-field label {
  color: var(--text-h);
  font-size: 0.875rem;
}

.w-full {
  width: 100%;
}

.view-message {
  margin-bottom: 1rem;
}

.errors-table,
.accounts-table,
.accounts-result-table {
  margin-top: 0.25rem;
}

:deep(.row-disabled) {
  opacity: 0.5;
}

.dialog-actions {
  justify-content: flex-end;
  margin-top: 1rem;
}

@media (max-width: 600px) {
  .import-panel,
  .accounts-toolbar,
  .dialog-actions {
    align-items: stretch;
    flex-direction: column;
  }

  .import-panel > *,
  .dialog-actions > * {
    width: 100%;
  }
}
</style>
