<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import dayjs from '../../utils/dayjs.js'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import {
  getPendingConfirmationRequests,
  getPlacementRequests
} from '../../api/placement-requests.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import {
  placementRequestStatusLabels as statusLabels,
  placementRequestStatusSeverities as statusSeverities
} from '../../utils/placementRequestStatus.js'

const route = useRoute()
const router = useRouter()
const filter = reactive({ Status: null, PageNumber: 1, PageSize: 10 })
const requests = ref([])
const totalRecords = ref(0)
const loading = ref(false)
const errorMessage = ref('')
const activeTab = ref(route.query.tab === 'confirmation' ? 'confirmation' : 'requests')
const detailDialogVisible = ref(false)
const selectedRequest = ref(null)

const statusOptions = [
  { label: 'Tất cả trạng thái', value: null },
  { label: 'Chờ xử lý', value: 0 },
  { label: 'Từ chối', value: 2 },
  { label: 'Đã mời phỏng vấn', value: 3 },
  { label: 'Đã tiếp nhận', value: 4 },
  { label: 'Hết hạn', value: 5 }
]

function emptyDisplay(value) {
  return value == null || value === '' ? '—' : value
}

function formatDate(value) {
  return value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—'
}

async function fetchRequests() {
  loading.value = true
  errorMessage.value = ''
  try {
    const getRequests = activeTab.value === 'confirmation'
      ? getPendingConfirmationRequests
      : getPlacementRequests
    const result = await getRequests(filter)
    requests.value = result?.items ?? []
    totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    errorMessage.value = extractErrorMessage(err, 'Không tải được danh sách đơn thực tập, vui lòng thử lại.')
    requests.value = []
    totalRecords.value = 0
  } finally {
    loading.value = false
  }
}

function setTab(tab) {
  activeTab.value = tab
  filter.Status = null
  filter.PageNumber = 1
  router.replace({ name: 'placement-requests', query: tab === 'confirmation' ? { tab } : {} })
  fetchRequests()
}

function onStatusChange() {
  filter.PageNumber = 1
  fetchRequests()
}

function onPage(event) {
  filter.PageNumber = event.page + 1
  filter.PageSize = event.rows
  fetchRequests()
}

function openDetails(request) {
  selectedRequest.value = request
  detailDialogVisible.value = true
}

function openConfirmation(request) {
  router.push({
    name: 'placement-request-confirm-result',
    params: { id: request.id },
    query: activeTab.value === 'confirmation' ? { tab: 'confirmation' } : {}
  })
}

function openCancelAccepted(request) {
  router.push({
    name: 'placement-request-cancel-accepted',
    params: { id: request.id },
    query: activeTab.value === 'confirmation' ? { tab: 'confirmation' } : {}
  })
}

onMounted(fetchRequests)
</script>

<template>
  <div class="page-container">
    <Card>
      <template #title><span class="page-title">Quản lý đơn thực tập</span></template>
      <template #content>
        <div class="request-tabs" role="tablist" aria-label="Danh sách đơn thực tập">
          <Button
            label="Tất cả đơn"
            icon="pi pi-list"
            :outlined="activeTab !== 'requests'"
            :aria-selected="activeTab === 'requests'"
            role="tab"
            @click="setTab('requests')"
          />
          <Button
            label="Chờ xác nhận phỏng vấn"
            icon="pi pi-check-square"
            :outlined="activeTab !== 'confirmation'"
            :aria-selected="activeTab === 'confirmation'"
            role="tab"
            @click="setTab('confirmation')"
          />
        </div>

        <div v-if="activeTab === 'requests'" class="requests-toolbar">
          <div class="toolbar-field">
            <label for="request-status">Trạng thái</label>
            <Select
              id="request-status"
              v-model="filter.Status"
              :options="statusOptions"
              option-label="label"
              option-value="value"
              class="status-filter"
              :disabled="loading"
              @update:model-value="onStatusChange"
            />
          </div>
        </div>

        <Message v-if="errorMessage" severity="error" :closable="false" class="view-message">
          {{ errorMessage }}
        </Message>

        <DataTable
          :value="requests"
          :lazy="true"
          :paginator="true"
          :rows="filter.PageSize"
          :total-records="totalRecords"
          :loading="loading"
          :first="(filter.PageNumber - 1) * filter.PageSize"
          data-key="id"
          class="requests-table"
          @page="onPage"
        >
          <Column field="studentCode" header="MSSV" />
          <Column field="studentFullName" header="Họ tên" />
          <Column field="companyName" header="Doanh nghiệp" />
          <Column header="Vị trí">
            <template #body="{ data }">{{ emptyDisplay(data.jobPositionTitle) }}</template>
          </Column>
          <Column header="Trạng thái">
            <template #body="{ data }">
              <Tag :value="statusLabels[data.status] ?? 'Không xác định'" :severity="statusSeverities[data.status]" />
            </template>
          </Column>
          <Column header="Hạn phản hồi">
            <template #body="{ data }">{{ formatDate(data.dueAt) }}</template>
          </Column>
          <Column header="Ngày tạo">
            <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
          </Column>
          <Column header="Thao tác">
            <template #body="{ data }">
              <div class="action-buttons">
                <Button label="Chi tiết" icon="pi pi-eye" severity="secondary" outlined size="small" @click="openDetails(data)" />
                <Button
                  v-if="activeTab === 'confirmation'"
                  label="Xác nhận kết quả"
                  icon="pi pi-check"
                  severity="success"
                  size="small"
                  @click="openConfirmation(data)"
                />
                <Button
                  v-if="activeTab === 'requests' && data.status === 4"
                  label="Hủy kết quả tiếp nhận"
                  icon="pi pi-undo"
                  severity="danger"
                  outlined
                  size="small"
                  :disabled="loading"
                  @click="openCancelAccepted(data)"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </template>
    </Card>

    <Dialog
      v-model:visible="detailDialogVisible"
      modal
      header="Chi tiết đơn thực tập"
      :style="{ width: 'min(48rem, calc(100vw - 2rem))' }"
    >
      <dl v-if="selectedRequest" class="request-details">
        <dt>Sinh viên</dt><dd>{{ selectedRequest.studentFullName }} ({{ selectedRequest.studentCode }})</dd>
        <dt>Doanh nghiệp</dt><dd>{{ selectedRequest.companyName }}</dd>
        <dt>Vị trí</dt><dd>{{ emptyDisplay(selectedRequest.jobPositionTitle) }}</dd>
        <dt>Email ứng viên</dt><dd>{{ emptyDisplay(selectedRequest.applicantEmail) }}</dd>
        <dt>Số điện thoại</dt><dd>{{ emptyDisplay(selectedRequest.applicantPhone) }}</dd>
        <dt>Trường / ngành</dt><dd>{{ emptyDisplay(selectedRequest.applicantSchool) }} / {{ emptyDisplay(selectedRequest.applicantMajor) }}</dd>
        <dt>CV</dt><dd><a v-if="selectedRequest.cvUrl" :href="selectedRequest.cvUrl" target="_blank" rel="noopener noreferrer">Mở CV</a><span v-else>—</span></dd>
        <dt>Thư giới thiệu</dt><dd class="preserve-lines">{{ emptyDisplay(selectedRequest.coverLetter) }}</dd>
        <dt>Ghi chú đơn</dt><dd class="preserve-lines">{{ emptyDisplay(selectedRequest.note) }}</dd>
        <dt>Lý do từ chối</dt><dd class="preserve-lines">{{ emptyDisplay(selectedRequest.rejectReason) }}</dd>
        <dt>Thời gian phỏng vấn</dt><dd>{{ formatDate(selectedRequest.interviewAt) }}</dd>
        <dt>Địa điểm phỏng vấn</dt><dd>{{ emptyDisplay(selectedRequest.interviewLocation) }}</dd>
        <dt>Ghi chú phỏng vấn</dt><dd class="preserve-lines">{{ emptyDisplay(selectedRequest.interviewNote) }}</dd>
        <dt>Thời gian sinh viên báo cáo</dt><dd>{{ formatDate(selectedRequest.studentReportedInterviewAt) }}</dd>
        <dt>Báo cáo của sinh viên</dt><dd class="preserve-lines">{{ emptyDisplay(selectedRequest.studentInterviewNote) }}</dd>
        <dt>Kết quả / ghi chú</dt><dd class="preserve-lines">{{ emptyDisplay(selectedRequest.resultNote) }}</dd>
        <dt>Người rà soát</dt><dd>{{ emptyDisplay(selectedRequest.reviewedByUsername) }}</dd>
      </dl>
    </Dialog>

  </div>
</template>

<style scoped>
.request-tabs, .action-buttons { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; }
.request-tabs { margin-bottom: 1rem; }
.requests-toolbar { display: flex; justify-content: flex-end; margin-bottom: 1rem; }
.toolbar-field { display: flex; flex-direction: column; gap: .35rem; }
.toolbar-field label { font-size: .875rem; font-weight: 600; }
.status-filter { min-width: 13rem; }
.view-message { margin-bottom: 1rem; }
.requests-table { margin-top: .25rem; }
.request-details { display: grid; grid-template-columns: minmax(9rem, .4fr) 1fr; gap: .75rem 1rem; margin: 0; }
.request-details dt { color: var(--text); font-weight: 600; }
.request-details dd { min-width: 0; margin: 0; overflow-wrap: anywhere; }
.preserve-lines { white-space: pre-wrap; }
@media (max-width: 640px) {
  .request-tabs { align-items: stretch; flex-direction: column; }
  .request-tabs :deep(.p-button) { justify-content: flex-start; }
  .request-details { grid-template-columns: 1fr; gap: .25rem; }
  .request-details dd { margin-bottom: .65rem; }
}
</style>
