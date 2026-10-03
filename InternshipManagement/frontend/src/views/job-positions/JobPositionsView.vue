<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Button from 'primevue/button'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Paginator from 'primevue/paginator'
import Select from 'primevue/select'
import Skeleton from 'primevue/skeleton'
import { useRouter } from 'vue-router'
import { getJobPositions } from '../../api/job-positions.js'
import JobPositionCard from '../../components/job-positions/JobPositionCard.vue'
import JobPositionDrawer from '../../components/job-positions/JobPositionDrawer.vue'
import { useAuthStore } from '../../stores/auth.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const authStore = useAuthStore()
const router = useRouter()
const isAdmin = computed(() => {
  const role = Array.isArray(authStore.user?.role) ? authStore.user.role[0] : authStore.user?.role
  return String(role ?? '').trim().toLowerCase() === 'admin' || authStore.isAdmin === true
})
const filters = [{ label: 'Tất cả', value: '' }, { label: 'Đang nhận hồ sơ', value: 'open' }, { label: 'Đã đóng hoặc hết hạn', value: 'closed' }]
const searchInput = ref('')
const search = ref('')
const availability = ref('')
const positions = ref([])
const totalRecords = ref(0)
const pageNumber = ref(1)
const pageSize = ref(12)
const loading = ref(false)
const error = ref('')
const drawerVisible = ref(false)
const selectedPosition = ref(null)
const compactViewport = ref(false)
let debounceTimer = null
let compactMediaQuery = null

async function fetchPositions() {
  loading.value = true
  error.value = ''
  try {
    const result = await getJobPositions({ search: search.value, availability: availability.value, sort: 'recommended', pageNumber: pageNumber.value, pageSize: pageSize.value })
    positions.value = result?.items ?? []
    totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    error.value = err?.response?.data?.message?.trim() || extractErrorMessage(err, 'Không tải được danh sách vị trí, vui lòng thử lại.')
    positions.value = []
    totalRecords.value = 0
  } finally {
    loading.value = false
  }
}
function openDetails(position) { selectedPosition.value = position; drawerVisible.value = true }
function openApply(position) {
  router.push({ name: 'job-position-apply', params: { id: position.id }, query: { from: 'job-positions' } })
}
function onPage(event) { pageNumber.value = event.page + 1; pageSize.value = event.rows; fetchPositions() }
function clearFilters() { searchInput.value = ''; search.value = ''; availability.value = ''; pageNumber.value = 1; fetchPositions() }
watch(searchInput, () => {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => { search.value = searchInput.value.trim(); pageNumber.value = 1; fetchPositions() }, 400)
})
watch(availability, () => { pageNumber.value = 1; fetchPositions() })
function onCompactViewportChange(event) { compactViewport.value = event.matches }
onMounted(() => {
  compactMediaQuery = window.matchMedia('(max-width: 768px)')
  compactViewport.value = compactMediaQuery.matches
  compactMediaQuery.addEventListener('change', onCompactViewportChange)
  fetchPositions()
})
onBeforeUnmount(() => {
  clearTimeout(debounceTimer)
  compactMediaQuery?.removeEventListener('change', onCompactViewportChange)
})
</script>

<template>
  <div class="page-container job-positions-page">
    <header class="positions-heading"><div><h1 class="page-title">Vị trí thực tập</h1><p class="page-subtitle">Tìm vị trí phù hợp và gửi yêu cầu thực tập.</p></div></header>
    <div class="positions-toolbar">
      <div class="toolbar-field"><label for="position-search">Tìm kiếm</label><IconField><InputIcon class="pi pi-search" /><InputText id="position-search" v-model="searchInput" maxlength="200" placeholder="Tên vị trí, công ty hoặc phòng ban" /></IconField></div>
      <div class="toolbar-field toolbar-filter"><label for="position-availability">Trạng thái</label><Select id="position-availability" v-model="availability" :options="filters" option-label="label" option-value="value" placeholder="Tất cả" /></div>
    </div>
    <Message v-if="error" severity="error" :closable="false" class="positions-message"><div class="error-content"><span>{{ error }}</span><Button label="Thử lại" icon="pi pi-refresh" text @click="fetchPositions" /></div></Message>
    <section v-else class="positions-grid" aria-label="Danh sách vị trí thực tập">
      <template v-if="loading"><article v-for="placeholder in 6" :key="placeholder" class="position-skeleton"><div class="skeleton-heading"><Skeleton shape="square" size="44px" /><div><Skeleton width="13rem" height="1rem" /><Skeleton width="8rem" height=".8rem" /></div></div><Skeleton width="6rem" height="1.5rem" /><Skeleton width="90%" height=".9rem" /><Skeleton width="95%" height=".9rem" /><Skeleton width="78%" height=".9rem" /><div class="skeleton-footer"><Skeleton width="8rem" height="2rem" /><Skeleton width="7rem" height="2rem" /></div></article></template>
      <JobPositionCard v-for="position in positions" v-else :key="position.id" :position="position" :is-admin="isAdmin" @view="openDetails" @apply="openApply" />
      <div v-if="!loading && positions.length === 0" class="positions-empty"><i class="pi pi-briefcase" /><h2>Chưa có vị trí phù hợp.</h2><Button label="Xóa bộ lọc" icon="pi pi-filter-slash" severity="secondary" outlined @click="clearFilters" /></div>
    </section>
    <Paginator v-if="totalRecords > 0 && !error" :first="(pageNumber - 1) * pageSize" :rows="pageSize" :total-records="totalRecords" :rows-per-page-options="[12, 24, 48]" :template="compactViewport ? 'PrevPageLink CurrentPageReport NextPageLink' : 'FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown'" class="positions-paginator" @page="onPage" />
    <JobPositionDrawer v-model:visible="drawerVisible" :position="selectedPosition" :is-admin="isAdmin" @apply="openApply" />
  </div>
</template>

<style scoped>
.positions-heading { margin-bottom: 1.5rem; }
.page-subtitle { margin: .4rem 0 0; color: var(--text); font-size: .95rem; }
.positions-toolbar { display: flex; align-items: flex-end; gap: 12px; margin-bottom: 16px; }
.toolbar-field { display: grid; flex: 1; min-width: 0; gap: 6px; }
.toolbar-field label { color: var(--text-h); font-size: 13px; font-weight: 600; }
.toolbar-field :deep(.p-inputtext), .toolbar-field :deep(.p-select) { width: 100%; min-height: 44px; }
.toolbar-filter { flex: 0 0 min(18rem, 34%); }
.positions-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); align-items: stretch; gap: 16px; }
.positions-message { margin-bottom: 16px; }
.error-content { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.error-content > span { min-width: 0; overflow-wrap: anywhere; }
.error-content :deep(.p-button) { flex: 0 0 auto; min-height: 44px; }
.position-skeleton { display: flex; min-height: 294px; flex-direction: column; gap: 14px; padding: 16px; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); }
.skeleton-heading { display: flex; align-items: center; gap: 12px; }
.skeleton-heading > div { display: grid; gap: 8px; }
.skeleton-footer { display: flex; justify-content: space-between; gap: 8px; margin-top: auto; padding-top: 10px; border-top: 1px solid var(--border); }
.positions-empty { grid-column: 1 / -1; display: grid; justify-items: center; gap: 12px; padding: 52px 16px; color: var(--text); text-align: center; }
.positions-empty > i { color: var(--primary); font-size: 30px; }
.positions-empty h2 { margin: 0; color: var(--text-h); font-size: 18px; }
.positions-paginator { display: flex; max-width: 100%; flex-wrap: wrap; justify-content: center; overflow: hidden; margin-top: 18px; }
@media (max-width: 768px) { .job-positions-page { padding-bottom: calc(84px + env(safe-area-inset-bottom)); }.job-positions-page .page-title { font-size: 1.6rem; }.positions-toolbar { align-items: stretch; flex-direction: column; gap: 12px; }.toolbar-field, .toolbar-filter { width: 100%; flex: 1 1 auto; }.toolbar-field :deep(.p-inputtext), .toolbar-field :deep(.p-select) { width: 100%; min-height: 44px; font-size: 16px; }.positions-grid { grid-template-columns: minmax(0, 1fr); }.position-skeleton { min-height: 280px; }.positions-paginator { gap: 4px; padding: 6px; }.positions-paginator :deep(.p-paginator-prev), .positions-paginator :deep(.p-paginator-next) { min-width: 44px; min-height: 44px; }.positions-paginator :deep(.p-paginator-current) { min-height: 44px; margin: 0 4px; font-size: 12px; } }
</style>