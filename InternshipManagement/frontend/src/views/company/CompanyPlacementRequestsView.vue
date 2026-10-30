<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Message from 'primevue/message'
import Paginator from 'primevue/paginator'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import { getCompanyPlacementRequests } from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { formatServerDateTime } from '../../utils/dateTime.js'
import {
  mapPlacementRequestDueStatus,
  mapPlacementRequestStatus,
  PLACEMENT_REQUEST_STATUSES
} from '../../utils/companyPlacementRequest.js'

const router = useRouter()
const requests = ref([])
const totalRecords = ref(0)
const pageNumber = ref(1)
const pageSize = ref(10)
const statusFilter = ref(null)
const loading = ref(false)
const error = ref('')

const statusOptions = [
  { label: 'Tất cả trạng thái', value: null },
  ...PLACEMENT_REQUEST_STATUSES.map(({ label, value }) => ({ label, value }))
]

function statusOf(request) {
  return mapPlacementRequestStatus(request?.status)
}

function dueOf(request) {
  return mapPlacementRequestDueStatus(request?.dueAt)
}

function applicantName(request) {
  return request?.applicantFullName || request?.studentFullName || '—'
}

function dateLabel(value) {
  return formatServerDateTime(value) ?? '—'
}

function dueLabel(request) {
  const dueStatus = dueOf(request)
  if (dueStatus === 'overdue') return 'Đã quá hạn'
  if (dueStatus === 'soon') return 'Sắp đến hạn'
  return dateLabel(request.dueAt)
}

function goToDetail(id) {
  router.push(`/company/placement-requests/${id}`)
}

async function loadRequests() {
  loading.value = true
  error.value = ''
  try {
    const result = await getCompanyPlacementRequests({
      status: statusFilter.value,
      pageNumber: pageNumber.value,
      pageSize: pageSize.value
    })
    requests.value = result?.items ?? []
    totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    error.value = extractErrorMessage(err, 'Không tải được danh sách đơn ứng tuyển.')
    requests.value = []
    totalRecords.value = 0
  } finally {
    loading.value = false
  }
}

function onStatusChange() {
  pageNumber.value = 1
  loadRequests()
}

function onPage(event) {
  pageNumber.value = event.page + 1
  pageSize.value = event.rows
  loadRequests()
}

onMounted(loadRequests)
</script>

<template>
  <div class="page-container company-placement-requests">
    <header class="requests-heading">
      <div>
        <h1 class="page-title">Đơn ứng tuyển</h1>
        <p class="requests-subtitle">Theo dõi hồ sơ, phản hồi ứng viên và ghi nhận kết quả phỏng vấn.</p>
      </div>
      <div class="status-filter">
        <label for="placement-request-status">Trạng thái</label>
        <Select
          id="placement-request-status"
          v-model="statusFilter"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          aria-label="Lọc theo trạng thái đơn ứng tuyển"
          @change="onStatusChange"
        />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <!-- Desktop Table View (>= 768px) -->
    <DataTable
      :value="requests"
      :lazy="true"
      :paginator="true"
      :rows="pageSize"
      :total-records="totalRecords"
      :first="(pageNumber - 1) * pageSize"
      :rows-per-page-options="[10, 20, 50]"
      :loading="loading"
      data-key="id"
      class="company-placement-table"
      @page="onPage"
    >
      <Column header="Ứng viên">
        <template #body="{ data }">
          <div class="applicant-cell">
            <strong>{{ applicantName(data) }}</strong>
            <small>{{ data.applicantEmail || 'Chưa có email' }}</small>
          </div>
        </template>
      </Column>
      <Column header="Vị trí">
        <template #body="{ data }">{{ data.jobPositionTitle || 'Ứng tuyển chung' }}</template>
      </Column>
      <Column header="Ngày gửi">
        <template #body="{ data }">{{ dateLabel(data.createdAt) }}</template>
      </Column>
      <Column header="Hạn phản hồi">
        <template #body="{ data }">
          <div class="due-cell" :class="`due-${dueOf(data)}`">
            <strong v-if="dueOf(data) !== 'normal' && dueOf(data) !== 'none'">{{ dueLabel(data) }}</strong>
            <span>{{ dateLabel(data.dueAt) }}</span>
          </div>
        </template>
      </Column>
      <Column header="Trạng thái">
        <template #body="{ data }">
          <Tag :value="statusOf(data).label" :severity="statusOf(data).severity" />
        </template>
      </Column>
      <Column header="Chi tiết">
        <template #body="{ data }">
          <Button label="Xem đơn" icon="pi pi-eye" size="small" text @click="goToDetail(data.id)" />
        </template>
      </Column>
      <template #empty>Chưa có đơn ứng tuyển phù hợp.</template>
    </DataTable>

    <!-- Mobile Card View (< 768px) -->
    <section class="mobile-request-list" aria-label="Danh sách đơn ứng tuyển">
      <div v-if="loading" class="mobile-loading">Đang tải danh sách đơn ứng tuyển...</div>
      <article v-for="request in requests" :key="request.id" class="mobile-request-card">
        <div class="mobile-request-top">
          <div class="applicant-cell">
            <strong>{{ applicantName(request) }}</strong>
            <small>{{ request.applicantEmail || 'Chưa có email' }}</small>
          </div>
          <Tag :value="statusOf(request).label" :severity="statusOf(request).severity" />
        </div>
        <div class="mobile-request-position">
          <span class="position-label">Vị trí:</span>
          <strong>{{ request.jobPositionTitle || 'Ứng tuyển chung' }}</strong>
        </div>
        <div class="mobile-request-meta">
          <span>Ngày gửi: {{ dateLabel(request.createdAt) }}</span>
          <span :class="`due-${dueOf(request)}`">Hạn phản hồi: {{ dueLabel(request) }}</span>
        </div>
        <Button
          label="Xem đơn"
          icon="pi pi-eye"
          outlined
          class="mobile-view-btn"
          @click="goToDetail(request.id)"
        />
      </article>
      <p v-if="!loading && requests.length === 0" class="empty-state">Chưa có đơn ứng tuyển phù hợp.</p>
      <Paginator
        v-if="totalRecords > pageSize"
        :first="(pageNumber - 1) * pageSize"
        :rows="pageSize"
        :total-records="totalRecords"
        :rows-per-page-options="[10, 20, 50]"
        @page="onPage"
      />
    </section>
  </div>
</template>

<style scoped>
.company-placement-requests { display: grid; gap: 18px; }
.requests-heading { display: flex; align-items: end; justify-content: space-between; gap: 18px; }
.requests-subtitle { margin: 7px 0 0; color: var(--text); font-size: 14px; }
.status-filter { display: grid; min-width: 220px; gap: 6px; }
.status-filter label { color: var(--text-h); font-size: 13px; font-weight: 600; }
.company-placement-table { border: 1px solid var(--border); border-radius: 12px; overflow: hidden; }
.applicant-cell { display: grid; min-width: 0; gap: 4px; }
.applicant-cell strong { color: var(--text-h); font-size: 14px; }
.applicant-cell small { color: var(--text); overflow-wrap: anywhere; }
.due-cell { display: grid; gap: 3px; white-space: nowrap; }
.due-cell span { color: var(--text); font-size: 12px; }
.due-overdue { color: var(--danger); font-weight: 600; }
.due-soon { color: #a15c00; font-weight: 600; }
.due-normal, .due-none { color: var(--text); }
.mobile-request-list { display: none; }
.empty-state { color: var(--text); text-align: center; margin: 1.5rem 0; }
.mobile-loading { color: var(--text); text-align: center; padding: 1rem 0; font-size: 14px; }

@media (max-width: 767px) {
  .company-placement-requests { gap: 14px; }
  .requests-heading { align-items: stretch; flex-direction: column; gap: 12px; }
  .status-filter { min-width: 0; width: 100%; }
  .company-placement-table { display: none; }
  .mobile-request-list { display: grid; gap: 12px; }
  .mobile-request-card {
    display: grid;
    gap: 12px;
    padding: 16px;
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 12px;
  }
  .mobile-request-top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 10px;
  }
  .mobile-request-position {
    display: flex;
    gap: 6px;
    font-size: 14px;
    color: var(--text-h);
    overflow-wrap: anywhere;
  }
  .position-label {
    color: var(--text);
    font-weight: normal;
  }
  .mobile-request-meta {
    display: grid;
    gap: 6px;
    color: var(--text);
    font-size: 12.5px;
  }
  .mobile-view-btn {
    width: 100%;
    min-height: 44px;
    font-size: 14px;
    font-weight: 500;
  }
}
</style>
