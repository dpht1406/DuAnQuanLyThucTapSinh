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
  createCompanyAccountsBulk,
  createCompanyAccountsByFilter,
  getCompanies,
  getIndustries
} from '../../api/companies.js'
import { useAccountResultStore } from '../../stores/accountResult.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import {
  getCompanyAccountWarning,
  isCompanySelectable
} from '../../utils/companyAccountValidation.js'

const router = useRouter()
const accountResultStore = useAccountResultStore()
const confirm = useConfirm()

const filter = reactive({
  Search: '',
  Industry: '',
  HasAccount: null,
  PageNumber: 1,
  PageSize: 10
})

const searchInput = ref('')
const companies = ref([])
const totalRecords = ref(0)
const loadingCompanies = ref(false)
const companiesError = ref('')
const selectedCompanies = ref([])

const industriesList = ref([])
const loadingIndustries = ref(false)

const creatingAccounts = ref(false)
const createError = ref('')
let searchDebounceTimer = null

const accountFilterOptions = [
  { label: 'Tất cả', value: null },
  { label: 'Đã có tài khoản', value: true },
  { label: 'Chưa có tài khoản', value: false }
]

const industryOptions = ref([{ label: 'Tất cả ngành', value: '' }])

async function loadIndustries() {
  loadingIndustries.value = true
  try {
    const list = await getIndustries()
    industriesList.value = list ?? []
    industryOptions.value = [
      { label: 'Tất cả ngành', value: '' },
      ...(list ?? []).map((ind) => ({ label: ind, value: ind }))
    ]
  } catch {
    industriesList.value = []
  } finally {
    loadingIndustries.value = false
  }
}

async function fetchCompanies() {
  loadingCompanies.value = true
  companiesError.value = ''

  try {
    const result = await getCompanies({
      Search: filter.Search || undefined,
      Industry: filter.Industry || undefined,
      HasAccount: filter.HasAccount ?? undefined,
      PageNumber: filter.PageNumber,
      PageSize: filter.PageSize
    })
    companies.value = result?.items ?? []
    totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    companiesError.value = extractErrorMessage(
      err,
      'Không tải được danh sách doanh nghiệp, vui lòng thử lại.'
    )
    companies.value = []
    totalRecords.value = 0
  } finally {
    loadingCompanies.value = false
  }
}

function onPage(event) {
  filter.PageNumber = event.page + 1
  filter.PageSize = event.rows
  fetchCompanies()
}

function onFilterChange() {
  selectedCompanies.value = []
  filter.PageNumber = 1
  fetchCompanies()
}

watch(searchInput, () => {
  clearTimeout(searchDebounceTimer)
  searchDebounceTimer = setTimeout(() => {
    selectedCompanies.value = []
    filter.Search = searchInput.value.trim()
    filter.PageNumber = 1
    fetchCompanies()
  }, 400)
})

function onCheckDataSelectable(event) {
  const company = event?.data ?? event
  return isCompanySelectable(company)
}

function onSelectionChange(selection) {
  selectedCompanies.value = (selection ?? []).filter(isCompanySelectable)
}

async function submitCreateAccounts() {
  if (selectedCompanies.value.length === 0 || creatingAccounts.value) return

  creatingAccounts.value = true
  createError.value = ''

  try {
    const ids = selectedCompanies.value.map((c) => c.id)
    const result = await createCompanyAccountsBulk(ids)
    accountResultStore.set('company-bulk', result)
    router.push({ name: 'account-result', query: { from: 'companies' } })
  } catch (err) {
    createError.value = extractErrorMessage(
      err,
      'Không thể tạo tài khoản doanh nghiệp, vui lòng thử lại.'
    )
  } finally {
    creatingAccounts.value = false
  }
}

function submitCreateAccountsByFilter() {
  if (totalRecords.value === 0 || creatingAccounts.value) return

  confirm.require({
    message: `Bạn sắp tạo tài khoản cho TẤT CẢ ${totalRecords.value} doanh nghiệp đang khớp bộ lọc hiện tại (không chỉ trang đang xem). Tiếp tục?`,
    header: 'Xác nhận tạo tài khoản',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy',
    acceptLabel: 'Tiếp tục',
    rejectClass: 'p-button-secondary p-button-text',
    accept: async () => {
      creatingAccounts.value = true
      createError.value = ''
      try {
        const result = await createCompanyAccountsByFilter({
          Search: filter.Search || undefined,
          Industry: filter.Industry || undefined,
          HasAccount: filter.HasAccount ?? undefined
        })
        accountResultStore.set('company-bulk', result)
        router.push({ name: 'account-result', query: { from: 'companies' } })
      } catch (err) {
        createError.value = extractErrorMessage(
          err,
          'Không thể tạo tài khoản doanh nghiệp, vui lòng thử lại.'
        )
      } finally {
        creatingAccounts.value = false
      }
    }
  })
}

onMounted(() => {
  loadIndustries()
  fetchCompanies()
})

onBeforeUnmount(() => {
  clearTimeout(searchDebounceTimer)
})
</script>

<template>
  <div class="page-container">
    <Card>
      <template #title>
        <span class="page-title">Tạo tài khoản doanh nghiệp</span>
      </template>
      <template #content>
        <div class="filters-toolbar">
          <div class="toolbar-field toolbar-search">
            <label for="company-account-search">Tìm kiếm</label>
            <InputText
              id="company-account-search"
              v-model="searchInput"
              placeholder="Tên doanh nghiệp..."
              class="w-full"
              maxlength="150"
              :disabled="loadingCompanies"
            />
          </div>

          <div class="toolbar-field">
            <label for="company-account-industry">Ngành nghề</label>
            <Select
              id="company-account-industry"
              v-model="filter.Industry"
              :options="industryOptions"
              option-label="label"
              option-value="value"
              class="w-full"
              :disabled="loadingCompanies"
              @update:model-value="onFilterChange"
            />
          </div>

          <div class="toolbar-field">
            <label for="company-account-status">Trạng thái tài khoản</label>
            <Select
              id="company-account-status"
              v-model="filter.HasAccount"
              :options="accountFilterOptions"
              option-label="label"
              option-value="value"
              class="w-full"
              :disabled="loadingCompanies"
              @update:model-value="onFilterChange"
            />
          </div>
        </div>

        <div class="accounts-toolbar">
          <Button
            :label="`Tạo tài khoản cho ${selectedCompanies.length} doanh nghiệp đã chọn`"
            icon="pi pi-user-plus"
            :loading="creatingAccounts"
            :disabled="selectedCompanies.length === 0 || creatingAccounts"
            @click="submitCreateAccounts"
          />
          <Button
            :label="`Tạo tài khoản cho tất cả theo bộ lọc (~${totalRecords} doanh nghiệp)`"
            icon="pi pi-users"
            severity="secondary"
            :loading="creatingAccounts"
            :disabled="totalRecords === 0 || creatingAccounts"
            @click="submitCreateAccountsByFilter"
          />
        </div>

        <Message v-if="companiesError" severity="error" :closable="false" class="view-message">
          <div class="error-container">
            <span>{{ companiesError }}</span>
            <Button label="Thử lại" text size="small" class="p-0 ml-2" @click="fetchCompanies" />
          </div>
        </Message>

        <Message v-if="createError" severity="error" :closable="false" class="view-message">
          {{ createError }}
        </Message>

        <div class="table-container">
          <DataTable
            v-model:selection="selectedCompanies"
            :value="companies"
            :lazy="true"
            :paginator="true"
            :rows="filter.PageSize"
            :total-records="totalRecords"
            :loading="loadingCompanies"
            :first="(filter.PageNumber - 1) * filter.PageSize"
            data-key="id"
            selection-mode="multiple"
            :is-data-selectable="onCheckDataSelectable"
            :row-class="(data) => (!isCompanySelectable(data) ? 'row-disabled' : '')"
            class="accounts-table"
            responsive-layout="scroll"
            @update:selection="onSelectionChange"
            @page="onPage"
          >
            <template #empty>Không có doanh nghiệp phù hợp.</template>

            <Column selection-mode="multiple" header-style="width: 3.5rem" />
            <Column field="name" header="Tên doanh nghiệp" />
            <Column field="industry" header="Ngành nghề" />
            <Column field="contactPerson" header="Người liên hệ" />
            <Column header="Email liên hệ">
              <template #body="{ data }">
                <div class="email-cell">
                  <span v-if="data.contactEmail">{{ data.contactEmail }}</span>
                  <span v-else class="text-muted">—</span>
                  <Tag
                    v-if="getCompanyAccountWarning(data)"
                    :value="getCompanyAccountWarning(data)"
                    severity="warn"
                    class="warning-tag"
                  />
                </div>
              </template>
            </Column>
            <Column header="Trạng thái tài khoản">
              <template #body="{ data }">
                <Tag
                  :value="data.hasAccount ? 'Đã có tài khoản' : 'Chưa có tài khoản'"
                  :severity="data.hasAccount ? 'success' : 'secondary'"
                />
              </template>
            </Column>
          </DataTable>
        </div>
      </template>
    </Card>
  </div>
</template>

<style scoped>
.page-container {
  padding: 1.5rem;
}

.page-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-h);
}

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
  gap: 1rem;
  margin-bottom: 1rem;
}

@media (min-width: 768px) {
  .filters-toolbar {
    grid-template-columns: 1.5fr 1fr 1fr;
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

.error-container {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.table-container {
  width: 100%;
  overflow-x: auto;
}

.accounts-table {
  margin-top: 0.25rem;
  min-width: 600px;
}

:deep(.row-disabled) {
  opacity: 0.6;
}

.email-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.text-muted {
  color: var(--ui-muted, #64748b);
}

.warning-tag {
  font-size: 0.75rem;
}

@media (max-width: 600px) {
  .page-container {
    padding: 1rem;
  }

  .accounts-toolbar {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
