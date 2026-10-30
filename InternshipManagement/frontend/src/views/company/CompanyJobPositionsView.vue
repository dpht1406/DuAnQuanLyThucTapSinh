<script setup>
import { onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { useRouter } from 'vue-router'
import {
  deleteCompanyJobPosition,
  getCompanyJobPositions,
  submitCompanyJobPosition,
  toggleCompanyJobPositionOpen
} from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const router = useRouter()
const confirm = useConfirm()
const toast = useToast()
const positions = ref([])
const totalRecords = ref(0)
const pageNumber = ref(1)
const pageSize = ref(10)
const loading = ref(false)
const error = ref('')
const actionId = ref(null)
const previewPosition = ref(null)
const previewVisible = ref(false)

const statusDetails = {
  0: { label: 'Draft', severity: 'secondary' },
  1: { label: 'PendingApproval', severity: 'warn' },
  2: { label: 'Approved', severity: 'success' },
  3: { label: 'Rejected', severity: 'danger' },
  Draft: { label: 'Draft', severity: 'secondary' },
  PendingApproval: { label: 'PendingApproval', severity: 'warn' },
  Approved: { label: 'Approved', severity: 'success' },
  Rejected: { label: 'Rejected', severity: 'danger' }
}

function statusOf(position) {
  const status = position?.approvalStatus
  return statusDetails[status]?.label ?? 'Unknown'
}

function dateLabel(value) {
  return value ? new Date(value).toLocaleDateString('vi-VN') : '—'
}

function openEdit(position) {
  router.push({ name: 'company-job-position-edit', params: { id: position.id } })
}

function viewPosition(position) {
  previewPosition.value = position
  previewVisible.value = true
}

async function loadPositions() {
  loading.value = true
  error.value = ''
  try {
    const result = await getCompanyJobPositions({
      pageNumber: pageNumber.value,
      pageSize: pageSize.value
    })
    positions.value = result?.items ?? []
    totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    error.value = extractErrorMessage(err, 'Không tải được danh sách vị trí tuyển dụng.')
    positions.value = []
    totalRecords.value = 0
  } finally {
    loading.value = false
  }
}

function onPage(event) {
  pageNumber.value = event.page + 1
  pageSize.value = event.rows
  loadPositions()
}

function notifySuccess(detail) {
  toast.add({ severity: 'success', summary: 'Thành công', detail, life: 3000 })
}

async function submitForApproval(position) {
  actionId.value = position.id
  error.value = ''
  try {
    await submitCompanyJobPosition(position.id)
    notifySuccess('Đã gửi vị trí tuyển dụng để duyệt.')
    await loadPositions()
  } catch (err) {
    error.value = extractErrorMessage(err, 'Không thể gửi vị trí để duyệt.')
  } finally {
    actionId.value = null
  }
}

function confirmDelete(position) {
  confirm.require({
    message: `Bạn có chắc muốn xóa vị trí "${position.title}"?`,
    header: 'Xóa vị trí tuyển dụng',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy',
    acceptLabel: 'Xóa',
    rejectClass: 'p-button-secondary p-button-text',
    acceptClass: 'p-button-danger',
    accept: async () => {
      actionId.value = position.id
      error.value = ''
      try {
        await deleteCompanyJobPosition(position.id)
        notifySuccess('Đã xóa vị trí tuyển dụng.')
        if (positions.value.length === 1 && pageNumber.value > 1) pageNumber.value -= 1
        await loadPositions()
      } catch (err) {
        error.value = extractErrorMessage(err, 'Không thể xóa vị trí tuyển dụng.')
      } finally {
        actionId.value = null
      }
    }
  })
}

async function toggleOpen(position) {
  actionId.value = position.id
  error.value = ''
  try {
    await toggleCompanyJobPositionOpen(position.id, !position.isOpen)
    notifySuccess(position.isOpen ? 'Đã tắt nhận hồ sơ.' : 'Đã bật nhận hồ sơ.')
    await loadPositions()
  } catch (err) {
    error.value = extractErrorMessage(err, 'Không thể cập nhật trạng thái tuyển dụng.')
  } finally {
    actionId.value = null
  }
}

onMounted(loadPositions)
</script>

<template>
  <div class="page-container company-positions-page">
    <header class="positions-heading">
      <div>
        <h1 class="page-title">Vị trí tuyển dụng</h1>
        <p class="positions-subtitle">Tạo tin tuyển dụng và theo dõi trạng thái duyệt.</p>
      </div>
      <Button label="Tạo vị trí" icon="pi pi-plus" @click="router.push({ name: 'company-job-position-create' })" />
    </header>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <DataTable
      :value="positions"
      :lazy="true"
      :paginator="true"
      :rows="pageSize"
      :total-records="totalRecords"
      :first="(pageNumber - 1) * pageSize"
      :loading="loading"
      data-key="id"
      class="company-positions-table"
      @page="onPage"
    >
      <Column field="title" header="Vị trí" />
      <Column field="department" header="Phòng ban" />
      <Column field="quantity" header="Số lượng" />
      <Column header="Hạn nộp">
        <template #body="{ data }">{{ dateLabel(data.deadline) }}</template>
      </Column>
      <Column header="Trạng thái duyệt">
        <template #body="{ data }">
          <div class="approval-cell">
            <Tag
              :value="statusDetails[data.approvalStatus]?.label ?? 'Unknown'"
              :severity="statusDetails[data.approvalStatus]?.severity ?? 'secondary'"
            />
            <small v-if="statusOf(data) === 'Rejected' && data.approvalNote" class="rejection-note">
              Lý do: {{ data.approvalNote }}
            </small>
          </div>
        </template>
      </Column>
      <Column header="Tuyển dụng">
        <template #body="{ data }">
          <span v-if="statusOf(data) === 'Approved'">{{ data.isOpen ? 'Đang mở' : 'Đã đóng' }}</span>
          <span v-else>—</span>
        </template>
      </Column>
      <Column header="Thao tác">
        <template #body="{ data }">
          <div class="position-actions">
            <template v-if="statusOf(data) === 'Draft'">
              <Button label="Sửa" icon="pi pi-pencil" size="small" text :disabled="actionId !== null" @click="openEdit(data)" />
              <Button label="Gửi duyệt" icon="pi pi-send" size="small" :loading="actionId === data.id" :disabled="actionId !== null" @click="submitForApproval(data)" />
              <Button label="Xóa" icon="pi pi-trash" severity="danger" size="small" text :loading="actionId === data.id" :disabled="actionId !== null" @click="confirmDelete(data)" />
            </template>
            <template v-else-if="statusOf(data) === 'Rejected'">
              <Button label="Sửa" icon="pi pi-pencil" size="small" text :disabled="actionId !== null" @click="openEdit(data)" />
              <Button label="Gửi lại" icon="pi pi-send" size="small" :loading="actionId === data.id" :disabled="actionId !== null" @click="submitForApproval(data)" />
            </template>
            <Button
              v-else-if="statusOf(data) === 'Approved'"
              :label="data.isOpen ? 'Tắt tuyển' : 'Bật tuyển'"
              :icon="data.isOpen ? 'pi pi-pause' : 'pi pi-play'"
              :severity="data.isOpen ? 'secondary' : 'success'"
              size="small"
              :loading="actionId === data.id"
              :disabled="actionId !== null"
              @click="toggleOpen(data)"
            />
            <Button v-else label="Xem" icon="pi pi-eye" size="small" text @click="viewPosition(data)" />
          </div>
        </template>
      </Column>
      <template #empty>Chưa có vị trí tuyển dụng nào.</template>
    </DataTable>

    <Dialog
      v-model:visible="previewVisible"
      modal
      header="Chi tiết vị trí tuyển dụng"
      :style="{ width: 'min(40rem, calc(100vw - 2rem))' }"
    >
      <dl v-if="previewPosition" class="position-preview">
        <div><dt>Tiêu đề</dt><dd>{{ previewPosition.title }}</dd></div>
        <div><dt>Phòng ban</dt><dd>{{ previewPosition.department || '—' }}</dd></div>
        <div><dt>Địa điểm</dt><dd>{{ previewPosition.displayLocation || previewPosition.location || '—' }}</dd></div>
        <div><dt>Số lượng</dt><dd>{{ previewPosition.quantity }}</dd></div>
        <div><dt>Hạn nộp</dt><dd>{{ dateLabel(previewPosition.deadline) }}</dd></div>
        <div><dt>Mô tả</dt><dd class="preview-description">{{ previewPosition.description || '—' }}</dd></div>
      </dl>
    </Dialog>
  </div>
</template>

<style scoped>
.company-positions-page { display: grid; gap: 18px; }
.positions-heading { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.positions-subtitle { margin: 7px 0 0; color: var(--text); font-size: 14px; }
.company-positions-table { min-width: 0; }
.approval-cell { display: grid; justify-items: start; gap: 6px; }
.rejection-note { max-width: 19rem; color: var(--danger); line-height: 1.4; overflow-wrap: anywhere; }
.position-actions { display: flex; flex-wrap: wrap; gap: 4px; }
.position-preview { display: grid; gap: 14px; margin: 0; }
.position-preview dt { margin-bottom: 4px; color: var(--text); font-size: 12px; }
.position-preview dd { margin: 0; color: var(--text-h); overflow-wrap: anywhere; }
.preview-description { white-space: pre-wrap; }
@media (max-width: 767px) {
  .positions-heading { align-items: flex-start; flex-direction: column; }
  .positions-heading :deep(.p-button) { width: 100%; min-height: 44px; }
  .company-positions-table { overflow-x: auto; }
  .position-actions { min-width: 135px; }
}
</style>
