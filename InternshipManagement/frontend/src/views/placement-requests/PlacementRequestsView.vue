<script setup>
import { onMounted, reactive, ref } from 'vue'
import dayjs from 'dayjs'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { approveRequest, getPlacementRequests, rejectRequest } from '../../api/placement-requests.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const filter = reactive({ Status: null, PageNumber: 1, PageSize: 10 })
const requests = ref([])
const totalRecords = ref(0)
const loading = ref(false)
const errorMessage = ref('')
const rejectDialogVisible = ref(false)
const selectedRequest = ref(null)
const rejectReason = ref('')
const rejectError = ref('')
const rejecting = ref(false)
const actionLoading = ref(null)
const confirm = useConfirm()
let toast = null
try { toast = useToast() } catch { }

const statusOptions = [
  { label: 'Tất cả', value: null },
  { label: 'Chờ duyệt', value: 0 },
  { label: 'Đã duyệt', value: 1 },
  { label: 'Từ chối', value: 2 }
]

const statusLabels = { 0: 'Chờ duyệt', 1: 'Đã duyệt', 2: 'Từ chối' }
const statusSeverities = { 0: 'warn', 1: 'success', 2: 'danger' }

function emptyDisplay(value) {
  return value == null || value === '' ? '—' : value
}

function formatDate(value) {
  return value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—'
}

function notifySuccess(detail) {
  try { toast?.add({ severity: 'success', summary: 'Thành công', detail, life: 3000 }) } catch { }
}

async function fetchRequests() {
  loading.value = true
  errorMessage.value = ''
  try {
    const result = await getPlacementRequests(filter)
    requests.value = result?.items ?? []
    totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    errorMessage.value = extractErrorMessage(err, 'Không tải được danh sách yêu cầu, vui lòng thử lại.')
    requests.value = []
    totalRecords.value = 0
  } finally {
    loading.value = false
  }
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

function confirmApprove(request) {
  confirm.require({
    message: `Bạn có chắc muốn duyệt yêu cầu của ${request.studentFullName || request.studentCode}?`,
    header: 'Xác nhận duyệt yêu cầu',
    icon: 'pi pi-check-circle',
    rejectLabel: 'Hủy',
    acceptLabel: 'Duyệt',
    rejectClass: 'p-button-secondary p-button-text',
    acceptClass: 'p-button-success',
    accept: async () => {
      actionLoading.value = request.id
      errorMessage.value = ''
      try {
        await approveRequest(request.id)
        notifySuccess('Đã duyệt yêu cầu thực tập.')
        await fetchRequests()
      } catch (err) {
        errorMessage.value = extractErrorMessage(err, 'Không thể duyệt yêu cầu, vui lòng thử lại.')
      } finally {
        actionLoading.value = null
      }
    }
  })
}

function openRejectDialog(request) {
  selectedRequest.value = request
  rejectReason.value = ''
  rejectError.value = ''
  rejectDialogVisible.value = true
}

function closeRejectDialog() {
  if (!rejecting.value) rejectDialogVisible.value = false
}

async function submitReject() {
  rejectError.value = ''
  if (!rejectReason.value.trim()) {
    rejectError.value = 'Lý do từ chối không được để trống.'
    return
  }

  rejecting.value = true
  errorMessage.value = ''
  try {
    await rejectRequest(selectedRequest.value.id, rejectReason.value.trim())
    rejectDialogVisible.value = false
    notifySuccess('Đã từ chối yêu cầu thực tập.')
    await fetchRequests()
  } catch (err) {
    rejectError.value = extractErrorMessage(err, 'Không thể từ chối yêu cầu, vui lòng thử lại.')
  } finally {
    rejecting.value = false
  }
}

onMounted(fetchRequests)
</script>

<template>
  <div class="page-container">
  <Card>
    <template #title><span class="page-title">Duyệt yêu cầu thực tập</span></template>
    <template #content>
      <div class="requests-toolbar">
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
        <Column header="Ghi chú">
          <template #body="{ data }">{{ emptyDisplay(data.note) }}</template>
        </Column>
        <Column header="Ngày tạo">
          <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
        </Column>
        <Column header="Thao tác">
          <template #body="{ data }">
            <div v-if="data.status === 0" class="action-buttons">
              <Button
                label="Duyệt"
                icon="pi pi-check"
                severity="success"
                size="small"
                :loading="actionLoading === data.id"
                :disabled="loading || rejecting"
                @click="confirmApprove(data)"
              />
              <Button
                label="Từ chối"
                icon="pi pi-times"
                severity="danger"
                outlined
                size="small"
                :disabled="loading || actionLoading !== null"
                @click="openRejectDialog(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </template>
  </Card>

  <Dialog
    v-model:visible="rejectDialogVisible"
    modal
    header="Từ chối yêu cầu thực tập"
    :closable="!rejecting"
    :style="{ width: 'min(32rem, calc(100vw - 2rem))' }"
  >
    <Message v-if="rejectError" severity="error" :closable="false" class="view-message">
      {{ rejectError }}
    </Message>
    <form class="reject-form" @submit.prevent="submitReject">
      <div class="form-field">
        <label for="reject-reason">Lý do từ chối</label>
        <Textarea id="reject-reason" v-model="rejectReason" rows="5" :disabled="rejecting" auto-resize />
      </div>
      <div class="dialog-actions">
        <Button type="button" label="Hủy" severity="secondary" text :disabled="rejecting" @click="closeRejectDialog" />
        <Button type="submit" label="Xác nhận" icon="pi pi-check" severity="danger" :loading="rejecting" />
      </div>
    </form>
  </Dialog>
</div>
</template>

<style scoped>
.requests-toolbar { display: flex; justify-content: flex-end; margin-bottom: 1rem; }
.toolbar-field, .form-field { display: flex; flex-direction: column; gap: .35rem; }
.toolbar-field label, .form-field label { font-size: .875rem; font-weight: 600; }
.status-filter { min-width: 13rem; }
.view-message { margin-bottom: 1rem; }
.requests-table { margin-top: .25rem; }
.action-buttons, .dialog-actions { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; }
.dialog-actions { justify-content: flex-end; margin-top: .5rem; }
.reject-form { display: grid; gap: 1rem; }
.form-field :deep(.p-textarea) { width: 100%; }
</style>
