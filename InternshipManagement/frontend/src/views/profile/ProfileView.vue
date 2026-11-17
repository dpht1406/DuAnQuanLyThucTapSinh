<script setup>
import { computed, onMounted, ref } from 'vue'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import {
  getMyPlacementRequests,
  getMyProfile,
  getMyStatusHistory,
  reportInterviewed
} from '../../api/me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { formatServerDateTime } from '../../utils/dateTime.js'
import { mapPlacementRequestStatus } from '../../utils/companyPlacementRequest.js'
import {
  STATUS_LABELS,
  STATUS_OPTIONS,
  STATUS_SEVERITY
} from '../../utils/studentStatus.js'

const confirm = useConfirm()
const toast = useToast()
const profile = ref(null)
const statusHistory = ref([])
const placementRequests = ref([])
const loading = ref(false)
const errorMessage = ref('')
const requestsLoading = ref(false)
const requestsError = ref('')
const reportDialogVisible = ref(false)
const reportRequest = ref(null)
const reportNote = ref('')
const reportError = ref('')
const reporting = ref(false)
const reportConfirmationPending = ref(false)

const statusOptionsByValue = new Map(STATUS_OPTIONS.map((option) => [option.value, option]))
const hasAcceptedPlacement = computed(() =>
  profile.value?.status === 3
  || placementRequests.value.some((request) => mapPlacementRequestStatus(request.status).value === 4)
)

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
  return formatServerDateTime(value) ?? '—'
}

function requestStatus(request) {
  return mapPlacementRequestStatus(request?.status)
}

function canReportInterview(request) {
  return requestStatus(request).value === 3
    && !request.studentReportedInterviewAt
    && !request.studentReportDismissedAt
}

function openReportDialog(request) {
  reportRequest.value = request
  reportNote.value = ''
  reportError.value = ''
  reportDialogVisible.value = true
}

function closeReportDialog() {
  if (reporting.value || reportConfirmationPending.value) return
  reportDialogVisible.value = false
  reportRequest.value = null
}

async function submitReport() {
  if (reporting.value || !reportRequest.value || !canReportInterview(reportRequest.value)) return

  reporting.value = true
  reportError.value = ''
  try {
    const note = reportNote.value.trim()
    await reportInterviewed(reportRequest.value.id, note || null)
    const reportedAt = new Date().toISOString()
    const request = placementRequests.value.find((item) => item.id === reportRequest.value.id)
    if (request) {
      request.studentReportedInterviewAt = reportedAt
      request.studentInterviewNote = note || null
    }
    reportDialogVisible.value = false
    reportRequest.value = null
    toast.add({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Đã báo cáo phỏng vấn, vui lòng chờ Admin xác nhận.',
      life: 3000
    })
    await loadPlacementRequests()
  } catch (err) {
    reportError.value = extractErrorMessage(err, 'Không thể báo cáo phỏng vấn, vui lòng thử lại.')
  } finally {
    reporting.value = false
  }
}

function confirmReport() {
  if (reporting.value || reportConfirmationPending.value || !reportRequest.value) return

  reportConfirmationPending.value = true
  confirm.require({
    header: 'Xác nhận báo cáo phỏng vấn',
    message: 'Bạn xác nhận đã tham gia buổi phỏng vấn cho đơn này?',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Xác nhận',
    rejectLabel: 'Quay lại',
    accept: async () => {
      reportConfirmationPending.value = false
      await submitReport()
    },
    reject: () => {
      reportConfirmationPending.value = false
    },
    onHide: () => {
      reportConfirmationPending.value = false
    }
  })
}

async function loadPlacementRequests() {
  requestsLoading.value = true
  requestsError.value = ''
  try {
    placementRequests.value = await getMyPlacementRequests() ?? []
  } catch (err) {
    requestsError.value = extractErrorMessage(err, 'Không tải được danh sách đơn ứng tuyển.')
  } finally {
    requestsLoading.value = false
  }
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

onMounted(() => {
  loadProfile()
  loadPlacementRequests()
})
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
      <Message v-if="hasAcceptedPlacement" severity="success" :closable="false">
        Bạn đã được doanh nghiệp nhận. Theo dõi trạng thái và lịch sử thực tập tại hồ sơ này.
      </Message>

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
            <p v-if="[1, 2, 3].includes(profile.status)" class="next-step-description">Trạng thái này do doanh nghiệp hoặc Admin cập nhật.</p>
            <p v-else-if="noActionMessage" class="status-note">{{ noActionMessage }}</p>
          </section>
        </template>
      </Card>

      <Card class="placement-requests-card">
        <template #title>Đơn ứng tuyển của tôi</template>
        <template #content>
          <Message v-if="requestsError" severity="error" :closable="false">
            {{ requestsError }}
          </Message>
          <p v-else-if="requestsLoading && placementRequests.length === 0" class="requests-empty">
            Đang tải đơn ứng tuyển...
          </p>
          <p v-else-if="placementRequests.length === 0" class="requests-empty">
            Bạn chưa có đơn ứng tuyển nào.
          </p>
          <div v-else class="placement-request-list">
            <article v-for="request in placementRequests" :key="request.id" class="placement-request">
              <header class="placement-request-heading">
                <div>
                  <h3>{{ emptyDisplay(request.jobPositionTitle) }}</h3>
                  <p>{{ emptyDisplay(request.companyName) }}</p>
                </div>
                <Tag :value="requestStatus(request).label" :severity="requestStatus(request).severity" />
              </header>
              <dl class="request-details">
                <div><dt>Hạn phản hồi</dt><dd>{{ formatDate(request.dueAt) }}</dd></div>
                <div><dt>Thời gian phỏng vấn</dt><dd>{{ formatDate(request.interviewAt) }}</dd></div>
                <div><dt>Địa điểm phỏng vấn</dt><dd>{{ emptyDisplay(request.interviewLocation) }}</dd></div>
                <div><dt>Ghi chú kết quả</dt><dd>{{ emptyDisplay(request.resultNote) }}</dd></div>
              </dl>
              <Message
                v-if="request.studentReportDismissedAt"
                severity="warn"
                :closable="false"
                class="request-report-message"
              >
                Báo cáo phỏng vấn đã bị Admin bác vào {{ formatDate(request.studentReportDismissedAt) }}. Bạn không thể báo cáo lại.
              </Message>
              <Message
                v-else-if="request.studentReportedInterviewAt"
                severity="info"
                :closable="false"
                class="request-report-message"
              >
                Đã báo cáo, chờ Admin xác nhận ({{ formatDate(request.studentReportedInterviewAt) }}).
                <span v-if="request.studentInterviewNote">Ghi chú: {{ request.studentInterviewNote }}</span>
              </Message>
              <Button
                v-if="canReportInterview(request)"
                label="Tôi đã phỏng vấn"
                icon="pi pi-check"
                outlined
                :disabled="reporting || reportConfirmationPending"
                @click="openReportDialog(request)"
              />
            </article>
          </div>
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

    <Dialog
      v-model:visible="reportDialogVisible"
      modal
      header="Báo cáo đã phỏng vấn"
      :style="{ width: 'min(32rem, calc(100vw - 2rem))' }"
      :closable="!reporting && !reportConfirmationPending"
      @update:visible="(visible) => { if (!visible) closeReportDialog() }"
    >
      <Message v-if="reportError" severity="error" :closable="false">{{ reportError }}</Message>
      <form class="report-form" @submit.prevent="confirmReport">
        <p>
          Xác nhận bạn đã tham gia phỏng vấn tại
          <strong>{{ emptyDisplay(reportRequest?.companyName) }}</strong>.
        </p>
        <label for="student-interview-note">Ghi chú <span>(không bắt buộc)</span></label>
        <Textarea
          id="student-interview-note"
          v-model="reportNote"
          rows="4"
          maxlength="500"
          :disabled="reporting || reportConfirmationPending"
        />
        <div class="dialog-actions">
          <Button
            type="button"
            label="Hủy"
            severity="secondary"
            text
            :disabled="reporting || reportConfirmationPending"
            @click="closeReportDialog"
          />
          <Button
            type="submit"
            label="Gửi báo cáo"
            icon="pi pi-check"
            :loading="reporting"
            :disabled="reporting || reportConfirmationPending || !canReportInterview(reportRequest)"
          />
        </div>
      </form>
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
.next-step-description { margin: .45rem 0 0; color: var(--text); line-height: 1.55; overflow-wrap: anywhere; }
.status-note { display: block; margin: .75rem 0 0; color: var(--text); }
.profile-details { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1.25rem 1.5rem; margin: 0; }
.profile-details > div { display: grid; min-width: 0; gap: .3rem; }
.placement-requests-card { min-width: 0; }
.placement-request-list { display: grid; gap: 1rem; }
.placement-request { display: grid; min-width: 0; gap: .85rem; padding: 1rem; border: 1px solid var(--border); border-radius: 10px; }
.placement-request-heading { display: flex; align-items: start; justify-content: space-between; gap: 1rem; }
.placement-request-heading > div { min-width: 0; }
.placement-request-heading h3 { margin: 0; color: var(--text-h); font-size: 1rem; overflow-wrap: anywhere; }
.placement-request-heading p { margin: .3rem 0 0; color: var(--text); overflow-wrap: anywhere; }
.request-details { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .85rem 1.25rem; margin: 0; }
.request-details > div { display: grid; min-width: 0; gap: .25rem; }
.request-details dt { color: var(--text); font-size: var(--text-body); }
.request-details dd { margin: 0; color: var(--text-h); overflow-wrap: anywhere; }
.request-report-message { margin: 0; }
.requests-empty { margin: 0; color: var(--text); }
.report-form { display: grid; gap: .75rem; }
.report-form p { margin: 0 0 .25rem; line-height: 1.5; }
.report-form label { color: var(--text-h); font-weight: 600; }
.report-form label span { color: var(--text); font-weight: 400; }
.dialog-actions { display: flex; justify-content: end; gap: .5rem; margin-top: .5rem; }
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
@media (max-width: 767px) {
  .profile-avatar { flex-basis: 52px; width: 52px; height: 52px; }
  .profile-heading-content :deep(.p-tag), .next-step-label { font-size: 14px; }
  .profile-details { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
  .request-details { grid-template-columns: minmax(0, 1fr); }
  .placement-request-heading { align-items: start; flex-direction: column-reverse; gap: .6rem; }
  .dialog-actions { flex-wrap: wrap; }
  .history-desktop { display: none; }
  .history-mobile-list { display: grid; }
}
@media (max-width: 420px) { .profile-details { grid-template-columns: minmax(0, 1fr); } }
</style>
