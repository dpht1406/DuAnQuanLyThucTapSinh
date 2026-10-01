<script setup>
import { onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import Avatar from 'primevue/avatar'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import Drawer from 'primevue/drawer'
import AutoComplete from 'primevue/autocomplete'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputNumber from 'primevue/inputnumber'
import InputSwitch from 'primevue/inputswitch'
import InputText from 'primevue/inputtext'
import Menu from 'primevue/menu'
import Message from 'primevue/message'
import Paginator from 'primevue/paginator'
import SelectButton from 'primevue/selectbutton'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { useAuthStore } from '../../stores/auth.js'
import { createCompany, deleteCompany, getCompanies, getCompanyById, getIndustries, updateCompany } from '../../api/companies.js'
import { createJobPosition, deleteJobPosition, getJobPositions, toggleOpenJobPosition, updateJobPosition } from '../../api/job-positions.js'
import { createPlacementRequest } from '../../api/me.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const authStore = useAuthStore()
const confirm = useConfirm()
let toast = null
try { toast = useToast() } catch { }
const filter = reactive({ Search: '', Industry: '', PageNumber: 1, PageSize: 10 })
const searchInput = ref('')
const industryInput = ref('')
const industriesList = ref([])
const industrySuggestions = ref([])
const companies = ref([])
const totalRecords = ref(0)
const loading = ref(false)
const errorMessage = ref('')
const expandedRows = ref({})
const jobsByCompany = reactive({})
const jobLoading = reactive({})
const jobErrors = reactive({})
const dialogVisible = ref(false)
const dialogMode = ref('create')
const dialogType = ref('company')
const editingId = ref(null)
const saving = ref(false)
const dialogErrorMessage = ref('')
const placementDialogVisible = ref(false)
const placementSaving = ref(false)
const placementError = ref('')
const placementSuccess = ref('')
const selectedPlacement = ref(null)
const placementNote = ref('')
const companyForm = reactive({ name: '', address: '', industry: '', contactPerson: '', contactPhone: '', contactEmail: '', contactPosition: '' })
const jobForm = reactive({ companyId: null, title: '', quantity: 1, description: '', isOpen: true })
let searchDebounceTimer = null
let industryDebounceTimer = null
const isMobile = ref(false)
const viewMode = ref(readCompaniesViewMode())
const jobsDrawerVisible = ref(false)
const selectedCompany = ref(null)
const companyMenuRef = ref(null)
const menuCompany = ref(null)
let mobileMediaQuery = null

function readCompaniesViewMode() {
  try { return window.localStorage.getItem('companies-view-mode') === 'table' ? 'table' : 'grid' } catch { return 'grid' }
}
function onMobileMediaChange(event) { isMobile.value = event.matches }
function openCompanyJobs(company) {
  selectedCompany.value = company
  jobsDrawerVisible.value = true
  loadJobs(company.id)
}
function toggleCompanyMenu(event, company) {
  menuCompany.value = company
  companyMenuRef.value?.toggle(event)
}
const viewModeOptions = [{ value: 'grid', icon: 'pi pi-th-large', label: 'Lưới' }, { value: 'table', icon: 'pi pi-list', label: 'Bảng' }]
const companyMenuItems = [
  { label: 'Sửa doanh nghiệp', icon: 'pi pi-pencil', command: () => menuCompany.value && openEditCompany(menuCompany.value) },
  { label: 'Xóa doanh nghiệp', icon: 'pi pi-trash', command: () => menuCompany.value && confirmDeleteCompany(menuCompany.value) },
]

function emptyDisplay(value) { return value == null || value === '' ? '—' : value }
function contactSummary(company) {
  return [company.contactPosition, company.contactPhone, company.contactEmail]
    .map((value) => String(value ?? '').trim())
    .filter(Boolean)
    .join(' • ')
}
function errorText(err, fallback = 'Có lỗi xảy ra, vui lòng thử lại.') {
  return err?.response?.data?.message?.trim() || extractErrorMessage(err, fallback)
}
function resetCompanyForm() { Object.assign(companyForm, { name: '', address: '', industry: '', contactPerson: '', contactPhone: '', contactEmail: '', contactPosition: '' }) }
function resetJobForm(companyId = null) { Object.assign(jobForm, { companyId, title: '', quantity: 1, description: '', isOpen: true }) }

function openCreateCompany() {
  resetCompanyForm(); dialogType.value = 'company'; dialogMode.value = 'create'; editingId.value = null
  dialogErrorMessage.value = ''; dialogVisible.value = true
}

async function openEditCompany(company) {
  dialogType.value = 'company'; dialogMode.value = 'edit'; editingId.value = company.id; dialogErrorMessage.value = ''
  try {
    const detail = await getCompanyById(company.id)
    Object.assign(companyForm, { name: detail?.name ?? company.name ?? '', address: detail?.address ?? company.address ?? '', industry: detail?.industry ?? company.industry ?? '', contactPerson: detail?.contactPerson ?? company.contactPerson ?? '', contactPhone: detail?.contactPhone ?? company.contactPhone ?? '', contactEmail: detail?.contactEmail ?? company.contactEmail ?? '', contactPosition: detail?.contactPosition ?? company.contactPosition ?? '' })
  } catch { Object.assign(companyForm, { name: company.name ?? '', address: company.address ?? '', industry: company.industry ?? '', contactPerson: company.contactPerson ?? '', contactPhone: company.contactPhone ?? '', contactEmail: company.contactEmail ?? '', contactPosition: company.contactPosition ?? '' }) }
  dialogVisible.value = true
}

function openCreateJob(company) {
  resetJobForm(company.id); dialogType.value = 'job'; dialogMode.value = 'create'; editingId.value = null
  dialogErrorMessage.value = ''; dialogVisible.value = true
}

function openEditJob(position) {
  Object.assign(jobForm, { companyId: position.companyId, title: position.title ?? '', quantity: position.quantity ?? 1, description: position.description ?? '', isOpen: position.isOpen === true })
  dialogType.value = 'job'; dialogMode.value = 'edit'; editingId.value = position.id; dialogErrorMessage.value = ''; dialogVisible.value = true
}

function closeDialog() { if (!saving.value) dialogVisible.value = false }

function validateForm() {
  const values = dialogType.value === 'company' ? companyForm : jobForm
  const requiredFields = dialogType.value === 'company'
    ? [['Tên doanh nghiệp', values.name], ['Địa chỉ', values.address], ['Ngành', values.industry], ['Người liên hệ', values.contactPerson]]
    : [['Tên vị trí', values.title]]
  const missing = requiredFields.find(([, value]) => !String(value ?? '').trim())
  if (missing) { dialogErrorMessage.value = `${missing[0]} không được để trống.`; return false }
  if (dialogType.value === 'company') {
    const phone = String(values.contactPhone ?? '').trim()
    const email = String(values.contactEmail ?? '').trim()
    const position = String(values.contactPosition ?? '').trim()
    if (phone.length > 20) { dialogErrorMessage.value = 'Số điện thoại người tuyển dụng không được quá 20 ký tự.'; return false }
    if (phone && !/^0\d{9}$/.test(phone)) { dialogErrorMessage.value = 'Số điện thoại người tuyển dụng phải gồm 10 chữ số và bắt đầu bằng số 0.'; return false }
    if (email.length > 150) { dialogErrorMessage.value = 'Email người tuyển dụng không được quá 150 ký tự.'; return false }
    if (email && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) { dialogErrorMessage.value = 'Email người tuyển dụng không đúng định dạng.'; return false }
    if (position.length > 100) { dialogErrorMessage.value = 'Chức vụ người tuyển dụng không được quá 100 ký tự.'; return false }
  }
  if (dialogType.value === 'job' && (!Number.isInteger(values.quantity) || values.quantity < 1)) { dialogErrorMessage.value = 'Số lượng phải lớn hơn hoặc bằng 1.'; return false }
  return true
}

async function submitDialog() {
  dialogErrorMessage.value = ''
  if (!validateForm()) return
  saving.value = true
  try {
    if (dialogType.value === 'company') {
      if (dialogMode.value === 'create') await createCompany({ ...companyForm })
      else await updateCompany(editingId.value, { ...companyForm })
      dialogVisible.value = false; await fetchCompanies()
    } else if (dialogMode.value === 'create') {
      await createJobPosition({ ...jobForm }); dialogVisible.value = false; await loadJobs(jobForm.companyId)
    } else {
      const { companyId, ...dto } = jobForm
      await updateJobPosition(editingId.value, dto); dialogVisible.value = false; await loadJobs(companyId)
    }
  } catch (err) { dialogErrorMessage.value = errorText(err) } finally { saving.value = false }
}

function confirmDeleteCompany(company) {
  confirm.require({ message: `Bạn có chắc muốn xóa doanh nghiệp ${company.name || ''}?`, header: 'Xác nhận xóa', icon: 'pi pi-exclamation-triangle', rejectLabel: 'Hủy', acceptLabel: 'Đồng ý', rejectClass: 'p-button-secondary p-button-text', acceptClass: 'p-button-danger', accept: async () => {
    try { await deleteCompany(company.id); await fetchCompanies() } catch (err) { errorMessage.value = errorText(err, 'Không xóa được doanh nghiệp, vui lòng thử lại.') }
  } })
}

function confirmDeleteJob(position, companyId) {
  confirm.require({ message: `Bạn có chắc muốn xóa vị trí ${position.title || ''}?`, header: 'Xác nhận xóa', icon: 'pi pi-exclamation-triangle', rejectLabel: 'Hủy', acceptLabel: 'Đồng ý', rejectClass: 'p-button-secondary p-button-text', acceptClass: 'p-button-danger', accept: async () => {
    try { await deleteJobPosition(position.id); await loadJobs(companyId) } catch (err) { jobErrors[companyId] = errorText(err, 'Không xóa được vị trí, vui lòng thử lại.') }
  } })
}

async function fetchCompanies() {
  loading.value = true; errorMessage.value = ''
  try {
    const result = await getCompanies(filter); companies.value = result?.items ?? []; totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    errorMessage.value = errorText(err, 'Không tải được danh sách doanh nghiệp, vui lòng thử lại.'); companies.value = []; totalRecords.value = 0
  } finally { loading.value = false }
}

async function loadJobs(companyId) {
  jobLoading[companyId] = true; jobErrors[companyId] = ''
  try {
    const result = await getJobPositions({ companyId, pageNumber: 1, pageSize: 100 })
    jobsByCompany[companyId] = Array.isArray(result) ? result : result?.items ?? []
  } catch (err) { jobErrors[companyId] = errorText(err, 'Không tải được danh sách vị trí.'); jobsByCompany[companyId] = [] } finally { jobLoading[companyId] = false }
}

function onRowExpand(event) { loadJobs(event.data.id) }

async function toggleJob(position, companyId) {
  const previousValue = !position.isOpen
  try { await toggleOpenJobPosition(position.id, position.isOpen); await loadJobs(companyId) } catch (err) { position.isOpen = previousValue; jobErrors[companyId] = errorText(err, 'Không thể cập nhật trạng thái vị trí.') }
}

function openPlacementDialog(company, position) {
  selectedPlacement.value = { company, position }; placementNote.value = ''; placementError.value = ''; placementSuccess.value = ''; placementDialogVisible.value = true
}

async function submitPlacement() {
  placementSaving.value = true; placementError.value = ''
  const { company, position } = selectedPlacement.value
  try {
    await createPlacementRequest({ companyId: company.id, jobPositionId: position.id, note: placementNote.value })
    placementDialogVisible.value = false; placementSuccess.value = 'Tạo yêu cầu thành công.'
    try { toast?.add({ severity: 'success', summary: 'Thành công', detail: placementSuccess.value, life: 3000 }) } catch { }
  } catch (err) { placementError.value = errorText(err) } finally { placementSaving.value = false }
}

function onPage(event) { filter.PageNumber = event.page + 1; filter.PageSize = event.rows; fetchCompanies() }
async function loadIndustries() {
  try { industriesList.value = await getIndustries() } catch { industriesList.value = [] }
}
function searchIndustry(event) {
  const q = (event.query || '').trim().toLowerCase()
  industrySuggestions.value = q
    ? industriesList.value.filter((i) => i.toLowerCase().includes(q))
    : industriesList.value
}
watch(searchInput, () => { clearTimeout(searchDebounceTimer); searchDebounceTimer = setTimeout(() => { filter.Search = searchInput.value; filter.PageNumber = 1; fetchCompanies() }, 400) })
watch(industryInput, () => { clearTimeout(industryDebounceTimer); industryDebounceTimer = setTimeout(() => { filter.Industry = industryInput.value; filter.PageNumber = 1; fetchCompanies() }, 400) })
watch(viewMode, (mode) => { try { window.localStorage.setItem('companies-view-mode', mode) } catch { } })
onMounted(() => {
  fetchCompanies(); loadIndustries()
  mobileMediaQuery = window.matchMedia('(max-width: 767px)')
  isMobile.value = mobileMediaQuery.matches
  mobileMediaQuery.addEventListener('change', onMobileMediaChange)
})
onBeforeUnmount(() => {
  clearTimeout(searchDebounceTimer); clearTimeout(industryDebounceTimer)
  mobileMediaQuery?.removeEventListener('change', onMobileMediaChange)
})
</script>

<template>
  <div class="page-container">
    <header class="companies-heading">
      <div><h1 class="page-title">Doanh nghiệp</h1><p class="page-subtitle">Khám phá đơn vị tuyển dụng và các vị trí thực tập.</p></div>
      <Button v-if="authStore.isAdmin" class="desktop-add-company" label="Thêm doanh nghiệp" icon="pi pi-plus" @click="openCreateCompany" />
    </header>
    <div class="companies-toolbar">
      <div class="toolbar-field toolbar-search"><label for="company-search">Tìm kiếm</label><IconField><InputIcon class="pi pi-search" /><InputText id="company-search" v-model="searchInput" placeholder="Tên doanh nghiệp" class="w-full" /></IconField></div>
      <div class="toolbar-field toolbar-industry"><label for="company-industry">Ngành</label><AutoComplete id="company-industry" v-model="industryInput" :suggestions="industrySuggestions" placeholder="Lọc theo ngành" class="w-full" input-class="w-full" @complete="searchIndustry" /></div>
      <SelectButton v-model="viewMode" class="view-mode-control" :options="viewModeOptions" option-value="value" aria-label="Chế độ hiển thị">
        <template #option="{ option }"><i :class="option.icon" :aria-label="option.label" :title="option.label" /></template>
      </SelectButton>
    </div>
    <Message v-if="errorMessage" severity="error" :closable="false" class="view-message">{{ errorMessage }}</Message>
    <Message v-if="placementSuccess" severity="success" :closable="true" class="view-message" @close="placementSuccess = ''">{{ placementSuccess }}</Message>

    <section v-if="viewMode === 'grid' || isMobile" class="companies-grid" aria-label="Danh sách doanh nghiệp">
      <article v-for="company in loading ? [] : companies" :key="company.id" class="company-card">
        <button type="button" class="company-card-main" :aria-label="`Xem vị trí tại ${company.name}`" @click="openCompanyJobs(company)">
          <div class="company-card-heading"><Avatar :label="(company.name || '?').trim().charAt(0).toUpperCase()" class="company-avatar" /><div class="company-card-title"><h2>{{ company.name }}</h2><Tag :value="emptyDisplay(company.industry)" severity="secondary" /></div></div>
          <div class="company-detail-line"><i class="pi pi-map-marker" /><span class="company-address">{{ emptyDisplay(company.address) }}</span></div>
          <div class="company-detail-line company-contact-line"><i class="pi pi-user" /><span><strong>{{ emptyDisplay(company.contactPerson) }}</strong><small v-if="contactSummary(company)">{{ contactSummary(company) }}</small></span></div>
        </button>
        <footer class="company-card-footer"><Button label="Xem vị trí" icon="pi pi-briefcase" text @click="openCompanyJobs(company)" /><Button v-if="authStore.isAdmin" icon="pi pi-ellipsis-h" text rounded aria-label="Thao tác doanh nghiệp" @click.stop="toggleCompanyMenu($event, company)" /></footer>
      </article>
      <template v-if="loading"><article v-for="placeholder in 6" :key="`company-skeleton-${placeholder}`" class="company-card company-card-skeleton" aria-hidden="true">
        <div class="company-card-heading"><Skeleton shape="square" size="44px" /><div class="skeleton-title"><Skeleton width="75%" height="1rem" /><Skeleton width="42%" height="1.4rem" /></div></div><Skeleton width="92%" height=".9rem" /><Skeleton width="68%" height=".9rem" /><div class="skeleton-footer"><Skeleton width="7rem" height="2rem" /></div>
      </article></template>
      <div v-if="!loading && companies.length === 0" class="companies-empty"><i class="pi pi-building" /><h2>Chưa có doanh nghiệp nào</h2><p v-if="authStore.isAdmin">Bấm “Thêm doanh nghiệp” để bắt đầu xây dựng danh sách.</p></div>
    </section>
    <Paginator v-if="(viewMode === 'grid' || isMobile) && totalRecords > 0" :first="(filter.PageNumber - 1) * filter.PageSize" :rows="filter.PageSize" :total-records="totalRecords" :rows-per-page-options="[10, 20, 50]" :template="isMobile ? 'PrevPageLink CurrentPageReport NextPageLink' : 'FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown'" current-page-report-template="Trang {currentPage} / {totalPages}" class="companies-paginator" @page="onPage" />

    <DataTable v-if="viewMode === 'table' && !isMobile" v-model:expanded-rows="expandedRows" :value="companies" :lazy="true" :paginator="true" :rows="filter.PageSize" :total-records="totalRecords" :loading="loading" :first="(filter.PageNumber - 1) * filter.PageSize" data-key="id" class="companies-table" @page="onPage" @row-expand="onRowExpand">
      <Column expander style="width: 3rem" /><Column field="name" header="Tên" /><Column field="address" header="Địa chỉ" /><Column field="industry" header="Ngành"><template #body="{ data }"><Tag :value="emptyDisplay(data.industry)" severity="secondary" /></template></Column><Column field="contactPerson" header="Người liên hệ (tên)"><template #body="{ data }"><div class="contact-cell"><span>{{ emptyDisplay(data.contactPerson) }}</span><small v-if="contactSummary(data)">{{ contactSummary(data) }}</small></div></template></Column>
      <Column v-if="authStore.isAdmin" header="Thao tác"><template #body="{ data }"><div class="action-buttons"><Button icon="pi pi-pencil" severity="secondary" text rounded aria-label="Sửa" @click="openEditCompany(data)" /><Button icon="pi pi-trash" severity="danger" text rounded aria-label="Xóa" @click="confirmDeleteCompany(data)" /></div></template></Column>
      <template #expansion="{ data }">
        <div class="job-expansion"><section class="company-contact-info" aria-label="Thông tin người tuyển dụng"><h3>Thông tin người tuyển dụng</h3><div class="contact-info-grid"><div><span>Người liên hệ (tên)</span><strong>{{ emptyDisplay(data.contactPerson) }}</strong></div><div v-if="data.contactPosition"><span>Chức vụ</span><strong>{{ data.contactPosition }}</strong></div><div v-if="data.contactPhone"><span>Số điện thoại</span><strong>{{ data.contactPhone }}</strong></div><div v-if="data.contactEmail"><span>Email</span><strong>{{ data.contactEmail }}</strong></div></div></section><div class="job-header"><h3>Vị trí tuyển dụng</h3><Button v-if="authStore.isAdmin" label="Thêm vị trí" icon="pi pi-plus" size="small" @click="openCreateJob(data)" /></div>
          <Message v-if="jobErrors[data.id]" severity="error" :closable="false">{{ jobErrors[data.id] }}</Message>
          <DataTable :value="jobsByCompany[data.id] || []" :loading="jobLoading[data.id]" size="small">
            <Column field="title" header="Tên vị trí" /><Column field="quantity" header="Số lượng" /><Column field="description" header="Mô tả"><template #body="{ data: position }">{{ emptyDisplay(position.description) }}</template></Column>
            <Column header="Trạng thái"><template #body="{ data: position }"><Tag :value="position.isOpen ? 'Đang tuyển' : 'Đã đóng'" :severity="position.isOpen ? 'success' : 'secondary'" /></template></Column>
            <Column v-if="authStore.isAdmin" header="Đã nhận/Tổng"><template #body="{ data: position }">{{ position.acceptedCount ?? 0 }}/{{ position.quantity }}</template></Column>
            <Column header="Thao tác"><template #body="{ data: position }"><div class="action-buttons"><template v-if="authStore.isAdmin"><InputSwitch v-model="position.isOpen" :disabled="jobLoading[data.id]" @update:model-value="toggleJob(position, data.id)" /><Button icon="pi pi-pencil" severity="secondary" text rounded aria-label="Sửa vị trí" @click="openEditJob(position)" /><Button icon="pi pi-trash" severity="danger" text rounded aria-label="Xóa vị trí" @click="confirmDeleteJob(position, data.id)" /></template><Button v-else label="Tạo yêu cầu" icon="pi pi-send" size="small" :disabled="position.isOpen !== true" @click="openPlacementDialog(data, position)" /></div></template></Column>
          </DataTable>
        </div>
      </template>
    </DataTable>

    <Button v-if="authStore.isAdmin" class="mobile-add-company" icon="pi pi-plus" rounded aria-label="Thêm doanh nghiệp" @click="openCreateCompany" />
    <Menu ref="companyMenuRef" :model="companyMenuItems" popup />
    <Drawer v-model:visible="jobsDrawerVisible" :position="isMobile ? 'bottom' : 'right'" class="company-jobs-drawer" :style="isMobile ? { height: '85dvh' } : { width: 'min(520px, 100vw)' }">
      <template v-if="selectedCompany">
        <header class="jobs-drawer-header"><Avatar :label="(selectedCompany.name || '?').trim().charAt(0).toUpperCase()" class="company-avatar" /><div><h2>{{ selectedCompany.name }}</h2><Tag :value="emptyDisplay(selectedCompany.industry)" severity="secondary" /></div></header>
        <section class="drawer-contact-info"><h3>Thông tin người tuyển dụng</h3><dl><div><dt>Người liên hệ</dt><dd>{{ emptyDisplay(selectedCompany.contactPerson) }}</dd></div><div v-if="selectedCompany.contactPosition"><dt>Chức vụ</dt><dd>{{ selectedCompany.contactPosition }}</dd></div><div v-if="selectedCompany.contactPhone"><dt>Số điện thoại</dt><dd><a :href="`tel:${selectedCompany.contactPhone}`">{{ selectedCompany.contactPhone }}</a></dd></div><div v-if="selectedCompany.contactEmail"><dt>Email</dt><dd><a :href="`mailto:${selectedCompany.contactEmail}`">{{ selectedCompany.contactEmail }}</a></dd></div></dl></section>
        <section class="drawer-jobs"><div class="drawer-jobs-heading"><h3>Vị trí tuyển dụng</h3><Button v-if="authStore.isAdmin" label="Thêm vị trí" icon="pi pi-plus" @click="openCreateJob(selectedCompany)" /></div>
          <Message v-if="jobErrors[selectedCompany.id]" severity="error" :closable="false">{{ jobErrors[selectedCompany.id] }}</Message>
          <div v-if="jobLoading[selectedCompany.id]" class="drawer-job-list"><article v-for="placeholder in 3" :key="`job-skeleton-${placeholder}`" class="drawer-job-card"><Skeleton width="65%" height="1.1rem" /><Skeleton width="35%" height="1.4rem" /><Skeleton width="100%" height=".9rem" /><Skeleton width="80%" height=".9rem" /></article></div>
          <div v-else-if="!jobErrors[selectedCompany.id] && (jobsByCompany[selectedCompany.id] || []).length === 0" class="drawer-jobs-empty">Chưa có vị trí nào</div>
          <div v-else class="drawer-job-list"><article v-for="position in jobsByCompany[selectedCompany.id] || []" :key="position.id" class="drawer-job-card"><div class="drawer-job-title"><h4>{{ position.title }}</h4><Tag :value="position.isOpen ? 'Đang tuyển' : 'Đã đóng'" :severity="position.isOpen ? 'success' : 'secondary'" /></div><div class="drawer-job-meta"><span>Số lượng: {{ position.quantity }}</span><span v-if="authStore.isAdmin">Đã nhận/Tổng: {{ position.acceptedCount ?? 0 }}/{{ position.quantity }}</span></div><p class="drawer-job-description">{{ emptyDisplay(position.description) }}</p><div class="drawer-job-actions"><div v-if="authStore.isAdmin" class="drawer-admin-actions"><InputSwitch v-model="position.isOpen" :disabled="jobLoading[selectedCompany.id]" :aria-label="`Đổi trạng thái vị trí ${position.title}`" @update:model-value="toggleJob(position, selectedCompany.id)" /><Button icon="pi pi-pencil" severity="secondary" text rounded aria-label="Sửa vị trí" @click="openEditJob(position)" /><Button icon="pi pi-trash" severity="danger" text rounded aria-label="Xóa vị trí" @click="confirmDeleteJob(position, selectedCompany.id)" /></div><Button v-else label="Tạo yêu cầu" icon="pi pi-send" :disabled="position.isOpen !== true" @click="openPlacementDialog(selectedCompany, position)" /></div></article></div>
        </section>
      </template>
    </Drawer>

  <Dialog v-model:visible="dialogVisible" modal class="app-fullscreen-dialog" :header="dialogType === 'company' ? (dialogMode === 'create' ? 'Thêm doanh nghiệp' : 'Sửa doanh nghiệp') : (dialogMode === 'create' ? 'Thêm vị trí' : 'Sửa vị trí')" :closable="!saving" :style="{ width: 'min(34rem, calc(100vw - 2rem))' }">
    <Message v-if="dialogErrorMessage" severity="error" :closable="false">{{ dialogErrorMessage }}</Message>
    <form class="entity-form" @submit.prevent="submitDialog">
      <template v-if="dialogType === 'company'"><div class="form-field"><label for="company-name">Tên doanh nghiệp</label><InputText id="company-name" v-model="companyForm.name" :disabled="saving" /></div><div class="form-field"><label for="company-address">Địa chỉ</label><InputText id="company-address" v-model="companyForm.address" :disabled="saving" /></div><div class="form-field"><label for="company-industry-form">Ngành</label><InputText id="company-industry-form" v-model="companyForm.industry" :disabled="saving" /></div><div class="form-field"><label for="company-contact">Người liên hệ (tên)</label><InputText id="company-contact" v-model="companyForm.contactPerson" :disabled="saving" /></div><div class="form-field"><label for="company-contact-phone">Số điện thoại</label><InputText id="company-contact-phone" v-model="companyForm.contactPhone" :disabled="saving" /></div><div class="form-field"><label for="company-contact-email">Email</label><InputText id="company-contact-email" v-model="companyForm.contactEmail" :disabled="saving" /></div><div class="form-field"><label for="company-contact-position">Chức vụ</label><InputText id="company-contact-position" v-model="companyForm.contactPosition" :disabled="saving" /></div></template>
      <template v-else><div class="form-field"><label for="job-title">Tên vị trí</label><InputText id="job-title" v-model="jobForm.title" :disabled="saving" /></div><div class="form-field"><label for="job-quantity">Số lượng</label><InputNumber id="job-quantity" v-model="jobForm.quantity" :min="1" :use-grouping="false" :disabled="saving" /></div><div class="form-field"><label for="job-description">Mô tả</label><Textarea id="job-description" v-model="jobForm.description" rows="4" :disabled="saving" /></div><div class="form-checkbox"><Checkbox v-model="jobForm.isOpen" input-id="job-is-open" binary :disabled="saving" /><label for="job-is-open">Đang tuyển</label></div></template>
      <div class="dialog-actions"><Button type="button" label="Hủy" severity="secondary" text :disabled="saving" @click="closeDialog" /><Button type="submit" label="Lưu" icon="pi pi-check" :loading="saving" /></div>
    </form>
  </Dialog>

  <Dialog v-model:visible="placementDialogVisible" modal class="app-fullscreen-dialog" header="Tạo yêu cầu thực tập" :style="{ width: 'min(32rem, calc(100vw - 2rem))' }"><Message v-if="placementError" severity="error" :closable="false">{{ placementError }}</Message><div v-if="selectedPlacement" class="entity-form"><div class="form-field"><label>Công ty</label><InputText :model-value="selectedPlacement.company.name" readonly /></div><div class="form-field"><label>Vị trí</label><InputText :model-value="selectedPlacement.position.title" readonly /></div><div class="form-field"><label for="placement-note">Ghi chú</label><Textarea id="placement-note" v-model="placementNote" rows="4" :disabled="placementSaving" /></div><div class="dialog-actions"><Button label="Hủy" severity="secondary" text :disabled="placementSaving" @click="placementDialogVisible = false" /><Button label="Xác nhận" icon="pi pi-check" :loading="placementSaving" @click="submitPlacement" /></div></div></Dialog>
</div>
</template>

<style scoped>
.companies-heading { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-bottom: 1.5rem; }.page-subtitle { margin: .4rem 0 0; color: var(--text); font-size: .95rem; }
.companies-toolbar { display: flex; align-items: flex-end; gap: .75rem; margin-bottom: 1rem; }.toolbar-field { min-width: 12rem; flex: 1; }.toolbar-search { min-width: 16rem; }.toolbar-industry { min-width: 14rem; }.toolbar-field label, .form-field label { display: block; margin-bottom: .35rem; color: var(--text-h); font-weight: 600; }.view-mode-control { flex: 0 0 auto; }.view-mode-control :deep(.p-button) { min-width: 44px; min-height: 44px; }.view-mode-control :deep(.p-button.p-highlight) { color: var(--primary); background: var(--primary-tint); border-color: var(--primary-tint); }.view-message { margin-bottom: 1rem; }
.companies-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 16px; }.company-card { min-width: 0; padding: 1rem; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); box-shadow: var(--shadow-sm); transition: box-shadow .15s ease, transform .15s ease; }.company-card:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); }.company-card-main { display: grid; width: 100%; gap: 1rem; padding: 0; color: inherit; text-align: left; background: transparent; border: 0; font: inherit; cursor: pointer; }.company-card-heading { display: flex; align-items: center; gap: .75rem; min-width: 0; }.company-avatar { flex: 0 0 44px; width: 44px; height: 44px; color: var(--primary); background: var(--primary-tint); border-radius: var(--radius-sm); font-weight: 700; }.company-card-title { display: grid; min-width: 0; justify-items: start; gap: .4rem; }.company-card-title h2 { display: -webkit-box; overflow: hidden; margin: 0; color: var(--text-h); font-size: 1rem; line-height: 1.35; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }.company-detail-line { display: flex; align-items: flex-start; gap: .6rem; min-width: 0; color: var(--text); font-size: .9rem; line-height: 1.45; }.company-detail-line > i { flex: 0 0 1rem; padding-top: .15rem; color: var(--primary); }.company-address { display: -webkit-box; overflow: hidden; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }.company-contact-line > span { display: grid; min-width: 0; gap: .2rem; }.company-contact-line strong { color: var(--text-h); font-weight: 600; }.company-contact-line small { overflow: hidden; color: var(--text); font-size: .8rem; text-overflow: ellipsis; white-space: nowrap; }.company-card-footer { display: flex; align-items: center; justify-content: space-between; margin-top: .8rem; padding-top: .65rem; border-top: 1px solid var(--border); }.company-card-footer :deep(.p-button) { min-height: 44px; }.companies-empty { grid-column: 1 / -1; display: grid; justify-items: center; padding: 3rem 1rem; color: var(--text); text-align: center; }.companies-empty > i { color: var(--primary); font-size: 2.5rem; }.companies-empty h2 { margin: .8rem 0 .25rem; color: var(--text-h); font-size: 1.1rem; }.companies-empty p { margin: 0; }.company-card-skeleton { display: grid; align-content: start; gap: 1rem; }.skeleton-title { display: grid; flex: 1; gap: .5rem; }.skeleton-footer { display: flex; justify-content: flex-end; margin-top: .25rem; }.companies-paginator { margin-top: 1rem; border: 1px solid var(--border); background: var(--surface); border-radius: var(--radius-md); }
.job-header, .action-buttons, .dialog-actions, .form-checkbox { display: flex; align-items: center; gap: .75rem; }.job-expansion { padding: .75rem 1rem 1rem; }.job-header { justify-content: space-between; margin-bottom: .75rem; }.job-header h3 { margin: 0; font-size: 1rem; }.contact-cell { display: grid; gap: .2rem; }.contact-cell small, .contact-info-grid span { color: var(--text); font-size: .8rem; }.company-contact-info { margin-bottom: 1rem; padding-bottom: 1rem; border-bottom: 1px solid var(--border); }.company-contact-info h3 { margin: 0 0 .65rem; font-size: 1rem; }.contact-info-grid { display: flex; flex-wrap: wrap; gap: .75rem 2rem; }.contact-info-grid div { display: grid; gap: .2rem; }.companies-table { overflow: hidden; border: 1px solid var(--border); border-radius: var(--radius-md); }.entity-form { display: grid; gap: 1rem; }.form-field :deep(.p-inputtext), .form-field :deep(.p-inputnumber), .form-field :deep(.p-inputnumber-input), .form-field :deep(.p-textarea) { width: 100%; }.dialog-actions { justify-content: flex-end; margin-top: .25rem; }.action-buttons { flex-wrap: wrap; }
@media (max-width: 767px) { .page-container { padding-top: 1.25rem; }.companies-heading { align-items: flex-start; margin-bottom: 1rem; }.companies-heading .page-title { font-size: 1.6rem; }.desktop-add-company, .view-mode-control { display: none; }.companies-toolbar { align-items: stretch; flex-direction: column; gap: .75rem; }.toolbar-field, .toolbar-search, .toolbar-industry { width: 100%; min-width: 0; }.toolbar-field :deep(.p-inputtext) { min-height: 44px; font-size: 16px; }.toolbar-field :deep(.p-autocomplete-input) { min-height: 44px; font-size: 16px; }.companies-grid { grid-template-columns: minmax(0, 1fr); }.company-card-footer :deep(.p-button) { min-width: 44px; min-height: 44px; }.mobile-add-company { position: fixed; right: 1rem; bottom: calc(68px + env(safe-area-inset-bottom) + 12px); z-index: 90; width: 56px; height: 56px; box-shadow: var(--shadow-md); }.companies-paginator { gap: .25rem; padding: .35rem; }.companies-paginator :deep(.p-paginator-prev), .companies-paginator :deep(.p-paginator-next) { min-width: 44px; min-height: 44px; }.companies-paginator :deep(.p-paginator-current) { min-height: 44px; margin: 0 .25rem; font-size: .8rem; }.drawer-jobs-heading { align-items: flex-start; }.drawer-jobs-heading :deep(.p-button) { min-height: 44px; }.drawer-job-actions { flex-wrap: wrap; }.drawer-job-actions > :deep(.p-button) { flex: 1 1 100%; min-height: 44px; }.drawer-admin-actions { display: flex; align-items: center; gap: .5rem; }.drawer-admin-actions :deep(.p-button) { min-width: 44px; min-height: 44px; }.dialog-actions { flex-direction: column-reverse; align-items: stretch; }.dialog-actions :deep(.p-button) { width: 100%; min-height: 44px; } }
</style>
