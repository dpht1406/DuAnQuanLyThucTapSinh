<script setup>
import { onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import { useConfirm } from 'primevue/useconfirm'
import {
  createAccounts,
  createAccountsByFilter,
  getStudents
} from '../../api/students.js'
import { useAccountResultStore } from '../../stores/accountResult.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const router = useRouter()
const accountResultStore = useAccountResultStore()

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

const creatingAccounts = ref(false)
const createError = ref('')
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

function onSelectionChange(selection) {
  selectedStudents.value = (selection ?? []).filter((student) => !student.hasAccount)
}

async function submitCreateAccounts() {
  if (selectedStudents.value.length === 0) return

  creatingAccounts.value = true
  createError.value = ''

  try {
    const result = await createAccounts(selectedStudents.value.map((student) => student.id))
    accountResultStore.set('student-bulk', result)
    router.push({ name: 'account-result' })
  } catch (err) {
    createError.value = extractErrorMessage(err, 'Không thể tạo tài khoản, vui lòng thử lại.')
  } finally {
    creatingAccounts.value = false
  }
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
        const result = await createAccountsByFilter({
          Search: filter.Search,
          Status: filter.Status,
          CompanyId: filter.CompanyId,
          HasAccount: filter.HasAccount
        })
        accountResultStore.set('student-bulk', result)
        router.push({ name: 'account-result' })
      } catch (err) {
        createError.value = extractErrorMessage(err, 'Không thể tạo tài khoản, vui lòng thử lại.')
      } finally {
        creatingAccounts.value = false
      }
    }
  })
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
          <div class="filters-toolbar">
            <div class="toolbar-field toolbar-search">
              <label for="account-search">Tìm kiếm</label>
              <InputText
                id="account-search"
                v-model="searchInput"
                placeholder="MSSV hoặc họ tên"
                class="w-full"
                maxlength="150"
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
    </template>
  </Card>
  </div>
</template>

<style scoped>
.filters-toolbar,
.accounts-toolbar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
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

.accounts-table {
  margin-top: 0.25rem;
}

:deep(.row-disabled) {
  opacity: 0.5;
}

@media (max-width: 600px) {
  .accounts-toolbar {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>

