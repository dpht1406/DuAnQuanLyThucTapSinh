<script setup>
import { computed, onMounted, ref } from 'vue'
import dayjs from 'dayjs'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import { getMyProfile, getMyStatusHistory } from '../../api/me.js'
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

const statusOptionsByValue = new Map(STATUS_OPTIONS.map((option) => [option.value, option]))

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
            <p v-if="[1, 2, 3].includes(profile.status)" class="next-step-description">Trạng thái này do doanh nghiệp hoặc Admin cập nhật.</p>
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
  .history-desktop { display: none; }
  .history-mobile-list { display: grid; }
}
@media (max-width: 420px) { .profile-details { grid-template-columns: minmax(0, 1fr); } }
</style>
