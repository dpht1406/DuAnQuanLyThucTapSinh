<script setup>
import { computed, onMounted, ref } from 'vue'
import dayjs from 'dayjs'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { changeMyStatus, getMyProfile, getMyStatusHistory } from '../../api/me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import {
  STATUS_LABELS,
  STATUS_OPTIONS,
  STATUS_SEVERITY
} from '../../utils/studentStatus.js'

const profile = ref(null)
const statusHistory = ref([])
const loading = ref(false)
const errorMessage = ref('')
const actionLoading = ref(false)
const backDialogVisible = ref(false)
const backNote = ref('')
const backError = ref('')
const confirm = useConfirm()
let toast = null
try { toast = useToast() } catch { }

const statusOptionsByValue = new Map(STATUS_OPTIONS.map((option) => [option.value, option]))

const statusActions = computed(() => {
  const status = profile.value?.status
  if (status === 1) {
    return {
      forward: { status: 2, label: 'Xác nhận đã phỏng vấn' },
      backward: { status: 0, label: 'Báo doanh nghiệp không phù hợp / rớt phỏng vấn' }
    }
  }
  if (status === 2) {
    return { backward: { status: 1, label: 'Quay lại trạng thái đã giới thiệu' } }
  }
  if (status === 3) {
    return { forward: { status: 4, label: 'Xác nhận đã đi thực tập' } }
  }
  return {}
})

const noActionMessage = computed(() => {
  if (profile.value?.status === 0) return 'Vào mục Doanh nghiệp để tạo yêu cầu chọn doanh nghiệp.'
  if (profile.value?.status === 4) return 'Trạng thái thực tập chỉ có thể được cập nhật bởi Admin.'
  if (profile.value?.status === 5) return 'Trạng thái hoàn thành chỉ có thể được cập nhật bởi Admin.'
  return ''
})

function statusLabel(status) {
  return STATUS_LABELS[status] ?? statusOptionsByValue.get(status)?.label ?? '—'
}

function emptyDisplay(value) {
  return value == null || value === '' ? '—' : value
}

function initials(fullName) {
  const words = (fullName ?? '').trim().split(/\s+/).filter(Boolean)
  if (words.length === 0) return '?'
  return `${words[0][0]}${words.at(-1)[0]}`.toUpperCase()
}

function formatDate(value) {
  return value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—'
}

function notifySuccess(detail) {
  try { toast?.add({ severity: 'success', summary: 'Thành công', detail, life: 3000 }) } catch { }
}

async function loadProfile() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [profileResult, historyResult] = await Promise.all([
      getMyProfile(),
      getMyStatusHistory()
    ])
    profile.value = profileResult
    statusHistory.value = historyResult ?? []
  } catch (err) {
    errorMessage.value = extractErrorMessage(err, 'Không tải được hồ sơ, vui lòng thử lại.')
    profile.value = null
    statusHistory.value = []
  } finally {
    loading.value = false
  }
}

function confirmForward(action) {
  confirm.require({
    message: `Bạn có chắc muốn chuyển sang trạng thái "${statusLabel(action.status)}"?`,
    header: 'Xác nhận đổi trạng thái',
    icon: 'pi pi-question-circle',
    rejectLabel: 'Hủy',
    acceptLabel: 'Xác nhận',
    rejectClass: 'p-button-secondary p-button-text',
    acceptClass: 'p-button-primary',
    accept: () => submitStatusChange(action.status, null)
  })
}

function openBackDialog() {
  backNote.value = ''
  backError.value = ''
  backDialogVisible.value = true
}

function closeBackDialog() {
  if (!actionLoading.value) backDialogVisible.value = false
}

async function submitBack() {
  backError.value = ''
  if (!backNote.value.trim()) {
    backError.value = 'Lý do không được để trống.'
    return
  }
  await submitStatusChange(statusActions.value.backward.status, backNote.value.trim(), true)
}

async function submitStatusChange(newStatus, note, closeDialog = false) {
  actionLoading.value = true
  errorMessage.value = ''
  try {
    await changeMyStatus({ newStatus, note, rejectReason: null })
    if (closeDialog) backDialogVisible.value = false
    notifySuccess(`Đã chuyển sang trạng thái ${statusLabel(newStatus)}.`)
    await loadProfile()
  } catch (err) {
    const message = extractErrorMessage(err, 'Không thể đổi trạng thái, vui lòng thử lại.')
    if (closeDialog) backError.value = message
    else errorMessage.value = message
  } finally {
    actionLoading.value = false
  }
}

onMounted(loadProfile)
</script>

<template>
  <div class="page-container profile-view">
    <header class="profile-page-heading">
      <h1 class="page-title">Thông tin cá nhân</h1>
      <p>Theo dõi tiến trình thực tập của bạn</p>
    </header>

    <Message v-if="errorMessage" severity="error" :closable="false" class="view-message">
      {{ errorMessage }}
    </Message>

    <template v-if="loading && !profile">
      <p class="profile-loading">Đang tải hồ sơ...</p>
    </template>
    <template v-else-if="profile">
      <Card class="profile-header-card">
        <template #content>
          <div class="profile-identity">
            <div class="profile-avatar" aria-hidden="true">{{ initials(profile.fullName) }}</div>
            <div class="profile-heading-content">
              <h2>{{ emptyDisplay(profile.fullName) }}</h2>
              <p>MSSV {{ emptyDisplay(profile.studentCode) }} · Lớp {{ emptyDisplay(profile.className) }} · {{ emptyDisplay(profile.major) }}</p>
              <Tag :value="statusLabel(profile.status)" :severity="STATUS_SEVERITY[profile.status]" />
            </div>
          </div>
        </template>
      </Card>

      <Card class="next-step-card">
        <template #content>
          <section class="next-step-content">
            <p class="next-step-label">BƯỚC TIẾP THEO</p>
            <template v-if="profile.status === 1">
              <h2>Xác nhận sau khi bạn phỏng vấn xong</h2>
              <p class="next-step-description">Bạn đã được giới thiệu đến {{ emptyDisplay(profile.companyName) }}. Khi phỏng vấn xong, hãy xác nhận để tiếp tục.</p>
            </template>
            <template v-else-if="profile.status === 2">
              <h2>Đang chờ kết quả phỏng vấn</h2>
              <p class="next-step-description">Nếu doanh nghiệp không phù hợp, bạn có thể quay lại trạng thái đã giới thiệu.</p>
            </template>
            <template v-else-if="profile.status === 3">
              <h2>Xác nhận khi bạn bắt đầu thực tập</h2>
              <p class="next-step-description">Khi đã đi thực tập, hãy xác nhận để cập nhật tiến trình.</p>
            </template>
            <div v-if="statusActions.forward || statusActions.backward" class="status-actions">
              <Button
                v-if="statusActions.forward"
                :label="statusActions.forward.label"
                icon="pi pi-arrow-right"
                :loading="actionLoading"
                @click="confirmForward(statusActions.forward)"
              />
              <Button
                v-if="statusActions.backward"
                :label="statusActions.backward.label"
                icon="pi pi-undo"
                severity="secondary"
                outlined
                :disabled="actionLoading"
                @click="openBackDialog"
              />
            </div>
            <p v-else-if="noActionMessage" class="status-note">{{ noActionMessage }}</p>
          </section>
        </template>
      </Card>

      <Card class="information-card">
        <template #title>Thông tin chi tiết</template>
        <template #content>
          <dl class="profile-details">
            <div><dt>MSSV</dt><dd>{{ emptyDisplay(profile.studentCode) }}</dd></div>
            <div><dt>Họ tên</dt><dd>{{ emptyDisplay(profile.fullName) }}</dd></div>
            <div><dt>Lớp</dt><dd>{{ emptyDisplay(profile.className) }}</dd></div>
            <div><dt>Chuyên ngành</dt><dd>{{ emptyDisplay(profile.major) }}</dd></div>
            <div><dt>Email</dt><dd>{{ emptyDisplay(profile.email) }}</dd></div>
            <div><dt>SĐT</dt><dd>{{ emptyDisplay(profile.phoneNumber) }}</dd></div>
            <div><dt>Doanh nghiệp</dt><dd>{{ emptyDisplay(profile.companyName) }}</dd></div>
            <div><dt>Vị trí</dt><dd>{{ emptyDisplay(profile.jobPositionTitle) }}</dd></div>
          </dl>
        </template>
      </Card>

      <Card class="history-card">
        <template #title>Lịch sử trạng thái</template>
        <template #content>
          <div class="history-desktop">
            <DataTable :value="statusHistory" :loading="loading" data-key="id">
              <Column field="fromStatus" header="Từ trạng thái">
                <template #body="{ data }">{{ statusLabel(data.fromStatus) }}</template>
              </Column>
              <Column field="toStatus" header="Đến trạng thái">
                <template #body="{ data }">{{ statusLabel(data.toStatus) }}</template>
              </Column>
              <Column field="note" header="Ghi chú">
                <template #body="{ data }">{{ emptyDisplay(data.note) }}</template>
              </Column>
              <Column field="changedByUsername" header="Người đổi">
                <template #body="{ data }">{{ emptyDisplay(data.changedByUsername) }}</template>
              </Column>
              <Column field="changedAt" header="Thời gian">
                <template #body="{ data }">{{ formatDate(data.changedAt) }}</template>
              </Column>
              <template #empty>Chưa có lịch sử trạng thái.</template>
            </DataTable>
          </div>
          <ol class="history-mobile-list">
            <li v-for="item in statusHistory" :key="item.id" class="history-mobile-entry">
              <time>{{ formatDate(item.changedAt) }}</time>
              <strong>{{ statusLabel(item.fromStatus) }} → {{ statusLabel(item.toStatus) }}</strong>
              <p>{{ emptyDisplay(item.note) }}</p>
              <span>Người đổi: {{ emptyDisplay(item.changedByUsername) }}</span>
            </li>
            <li v-if="statusHistory.length === 0 && !loading" class="history-empty">Chưa có lịch sử trạng thái.</li>
          </ol>
        </template>
      </Card>
    </template>

    <Dialog v-model:visible="backDialogVisible" modal header="Lý do chuyển trạng thái" :closable="!actionLoading" :style="{ width: 'min(32rem, 90vw)' }" @hide="closeBackDialog">
      <div class="dialog-field">
        <label for="back-note">Ghi chú <span class="required-mark">*</span></label>
        <Textarea id="back-note" v-model="backNote" rows="5" auto-resize :disabled="actionLoading" />
        <small v-if="backError" class="dialog-error">{{ backError }}</small>
      </div>
      <template #footer>
        <Button label="Hủy" severity="secondary" text :disabled="actionLoading" @click="closeBackDialog" />
        <Button label="Xác nhận" icon="pi pi-check" :loading="actionLoading" @click="submitBack" />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.profile-view { display: grid; gap: 1rem; min-width: 0; }
.profile-page-heading p { margin: .35rem 0 0; color: var(--text); }
.view-message { margin: 0; }
.profile-loading { margin: 0; color: var(--text); }
.profile-identity { display: flex; align-items: center; gap: 1rem; min-width: 0; }
.profile-avatar { display: grid; flex: 0 0 64px; width: 64px; height: 64px; place-items: center; border-radius: 50%; background: var(--primary-tint); color: var(--primary); font-size: 1.25rem; font-weight: 700; }
.profile-heading-content { min-width: 0; }
.profile-heading-content h2 { margin: 0; color: var(--text-h); font-size: 1.25rem; overflow-wrap: anywhere; }
.profile-heading-content p { margin: .3rem 0 .55rem; color: var(--text); overflow-wrap: anywhere; }
.next-step-card { background: var(--primary-tint); border-color: transparent; }
.next-step-content { min-width: 0; }
.next-step-label { margin: 0 0 .65rem; color: var(--primary); font-size: var(--text-caption); font-weight: 700; }
.next-step-content h2 { margin: 0; color: var(--text-h); font-size: 1.15rem; }
.next-step-description { margin: .45rem 0 0; color: var(--text); line-height: 1.55; overflow-wrap: anywhere; }
.status-actions { display: flex; flex-wrap: wrap; gap: .75rem; margin-top: 1.25rem; }
.status-actions :deep(.p-button) { min-height: 42px; }
.status-note { display: block; margin: .75rem 0 0; color: var(--text); }
.profile-details { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1.25rem 1.5rem; margin: 0; }
.profile-details > div { display: grid; min-width: 0; gap: .3rem; }
.profile-details dt { color: var(--text); font-size: var(--text-body); }
.profile-details dd { margin: 0; color: var(--text-h); overflow-wrap: anywhere; }
.history-card { min-width: 0; }
.history-desktop { min-width: 0; overflow-x: auto; }
.history-desktop :deep(.p-datatable-table) { min-width: 680px; }
.history-desktop :deep(.p-datatable-tbody > tr > td) { overflow-wrap: anywhere; }
.history-mobile-list { display: none; list-style: none; margin: 0; padding: 0; }
.history-mobile-entry { display: grid; gap: .5rem; padding: .9rem 0; border-bottom: 1px solid var(--border); overflow-wrap: anywhere; }
.history-mobile-entry:first-child { padding-top: 0; }
.history-mobile-entry:last-child { padding-bottom: 0; border-bottom: 0; }
.history-mobile-entry time, .history-mobile-entry span { color: var(--text); }
.history-mobile-entry p { margin: 0; }
.history-empty { color: var(--text); }
.dialog-field { display: grid; gap: .5rem; }
.dialog-field textarea { width: 100%; }
.required-mark, .dialog-error { color: var(--red-500); }
@media (max-width: 767px) {
  .profile-avatar { flex-basis: 52px; width: 52px; height: 52px; }
  .profile-heading-content :deep(.p-tag), .next-step-label { font-size: 14px; }
  .profile-details { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
  .status-actions { flex-direction: column; }
  .status-actions :deep(.p-button) { width: 100%; min-height: 44px; }
  .history-desktop { display: none; }
  .history-mobile-list { display: grid; }
}
@media (max-width: 420px) { .profile-details { grid-template-columns: minmax(0, 1fr); } }
</style>
