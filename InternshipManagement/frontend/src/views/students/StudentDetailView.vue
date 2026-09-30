<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import dayjs from 'dayjs'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import { getCompanyById } from '../../api/companies.js'
import { getStudentById, getStudentStatusHistory, resetStudentPassword } from '../../api/students.js'
import ResetPasswordDialog from '../../components/students/ResetPasswordDialog.vue'
import StudentStatusDialog from '../../components/students/StudentStatusDialog.vue'
import { extractErrorMessage } from '../../utils/apiError.js'
import { STATUS_LABELS, STATUS_SEVERITY } from '../../utils/studentStatus.js'

const route = useRoute()
const router = useRouter()
const student = ref(null)
const company = ref(null)
const history = ref([])
const loading = ref(false)
const errorMessage = ref('')
const statusDialogVisible = ref(false)
const resetDialogVisible = ref(false)
const resetPasswordResult = ref(null)
const resettingPassword = ref(false)
const confirm = useConfirm()
let toast = null
try { toast = useToast() } catch { }

const sortedHistory = computed(() => [...history.value].sort(
  (left, right) => dayjs(right.changedAt).valueOf() - dayjs(left.changedAt).valueOf()
))

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

async function loadStudentDetails() {
  loading.value = true
  errorMessage.value = ''
  student.value = null
  company.value = null
  history.value = []

  try {
    student.value = await getStudentById(route.params.id)
  } catch (err) {
    errorMessage.value = extractErrorMessage(err, 'Không tải được thông tin sinh viên.')
    loading.value = false
    return
  }

  const historyResult = await getStudentStatusHistory(route.params.id)
    .then((items) => ({ items, error: null }))
    .catch((err) => ({ items: [], error: extractErrorMessage(err, 'Không tải được lịch sử trạng thái.') }))
  history.value = historyResult.items ?? []
  if (historyResult.error) errorMessage.value = historyResult.error

  if (student.value?.companyId != null) {
    try {
      company.value = await getCompanyById(student.value.companyId)
    } catch (err) {
      if (!errorMessage.value) errorMessage.value = extractErrorMessage(err, 'Không tải được thông tin doanh nghiệp.')
    }
  }

  loading.value = false
}

function confirmPasswordReset() {
  confirm.require({
    message: `Đặt lại mật khẩu cho ${student.value.fullName} (${student.value.studentCode})? Mật khẩu hiện tại sẽ không còn dùng được.`,
    header: 'Xác nhận đặt lại mật khẩu',
    icon: 'pi pi-key',
    rejectLabel: 'Hủy',
    acceptLabel: 'Đặt lại',
    rejectClass: 'p-button-secondary p-button-text',
    accept: resetPassword
  })
}

async function resetPassword() {
  resettingPassword.value = true
  try {
    resetPasswordResult.value = await resetStudentPassword(student.value.id)
    resetDialogVisible.value = true
  } catch (err) {
    toast?.add({
      severity: 'error',
      summary: 'Không thể đặt lại mật khẩu',
      detail: extractErrorMessage(err, 'Không thể đặt lại mật khẩu, vui lòng thử lại.'),
      life: 4000
    })
  } finally {
    resettingPassword.value = false
  }
}

function onResetDialogVisibilityChange(visible) {
  resetDialogVisible.value = visible
  if (!visible) resetPasswordResult.value = null
}

watch(() => route.params.id, loadStudentDetails, { immediate: true })
</script>

<template>
  <div class="page-container student-detail-page">
    <Button label="Quay lại danh sách" icon="pi pi-arrow-left" severity="secondary" text class="back-button" @click="router.push({ name: 'students' })" />
    <h1 class="page-title">Chi tiết sinh viên</h1>

    <Message v-if="errorMessage" severity="error" :closable="false" class="detail-message">{{ errorMessage }}</Message>
    <p v-if="loading" class="loading-state">Đang tải thông tin sinh viên...</p>

    <template v-if="student">
      <Card class="student-header-card">
        <template #content>
          <div class="student-header-content">
            <div class="student-identity">
              <div class="student-avatar" aria-hidden="true">{{ initials(student.fullName) }}</div>
              <div class="student-heading">
                <h2>{{ emptyDisplay(student.fullName) }}</h2>
                <p>MSSV {{ emptyDisplay(student.studentCode) }}</p>
                <Tag :value="STATUS_LABELS[student.status] ?? '—'" :severity="STATUS_SEVERITY[student.status]" />
              </div>
            </div>
            <div class="detail-actions">
              <Button
                v-if="student.hasAccount === true"
                label="Đặt lại mật khẩu"
                icon="pi pi-key"
                severity="secondary"
                outlined
                :loading="resettingPassword"
                :disabled="resettingPassword"
                @click="confirmPasswordReset"
              />
              <Button label="Đổi giai đoạn" icon="pi pi-sync" @click="statusDialogVisible = true" />
            </div>
          </div>
        </template>
      </Card>

      <Card class="information-card">
        <template #title>Thông tin sinh viên</template>
        <template #content>
          <dl class="student-information">
            <div><dt>MSSV</dt><dd>{{ emptyDisplay(student.studentCode) }}</dd></div>
            <div><dt>Họ tên</dt><dd>{{ emptyDisplay(student.fullName) }}</dd></div>
            <div><dt>Lớp</dt><dd>{{ emptyDisplay(student.className) }}</dd></div>
            <div><dt>Ngành</dt><dd>{{ emptyDisplay(student.major) }}</dd></div>
            <div><dt>Số điện thoại</dt><dd>{{ emptyDisplay(student.phoneNumber) }}</dd></div>
            <div><dt>Tài khoản</dt><dd>{{ student.hasAccount ? 'Đã tạo' : 'Chưa tạo' }}</dd></div>
            <div><dt>Email</dt><dd>{{ emptyDisplay(student.email) }}</dd></div>
          </dl>
        </template>
      </Card>

      <Card class="information-card">
        <template #title>Thông tin thực tập</template>
        <template #content>
          <dl class="student-information">
            <div><dt>Doanh nghiệp</dt><dd>{{ emptyDisplay(student.companyName) }}</dd></div>
            <div><dt>Vị trí</dt><dd>{{ emptyDisplay(student.jobPositionTitle) }}</dd></div>
            <div><dt>Người liên hệ</dt><dd>{{ emptyDisplay(company?.contactPerson) }}</dd></div>
            <div v-if="company?.contactPosition"><dt>Chức vụ</dt><dd>{{ company.contactPosition }}</dd></div>
            <div v-if="company?.contactPhone"><dt>Số điện thoại</dt><dd>{{ company.contactPhone }}</dd></div>
            <div v-if="company?.contactEmail"><dt>Email</dt><dd>{{ company.contactEmail }}</dd></div>
          </dl>
        </template>
      </Card>

      <Card class="history-card">
        <template #title>Lịch sử giai đoạn</template>
        <template #content>
          <p v-if="sortedHistory.length === 0" class="empty-history">Chưa có lịch sử trạng thái.</p>
          <div v-else>
            <div class="history-table-wrap">
              <table class="history-table">
                <thead><tr><th>Từ giai đoạn</th><th>Đến giai đoạn</th><th>Ghi chú</th><th>Người thực hiện</th><th>Thời gian</th></tr></thead>
                <tbody>
                  <tr v-for="item in sortedHistory" :key="item.id">
                    <td>{{ STATUS_LABELS[item.fromStatus] ?? '—' }}</td>
                    <td>{{ STATUS_LABELS[item.toStatus] ?? '—' }}</td>
                    <td>{{ emptyDisplay(item.note) }}<small v-if="item.companyName">{{ item.companyName }}</small></td>
                    <td>{{ emptyDisplay(item.changedByUsername) }}</td>
                    <td>{{ formatDate(item.changedAt) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <ol class="history-mobile-list">
              <li v-for="item in sortedHistory" :key="item.id" class="history-mobile-entry">
                <time>{{ formatDate(item.changedAt) }}</time>
                <strong>{{ STATUS_LABELS[item.fromStatus] ?? '—' }} → {{ STATUS_LABELS[item.toStatus] ?? '—' }}</strong>
                <p>{{ emptyDisplay(item.note) }}</p>
                <small v-if="item.companyName">{{ item.companyName }}</small>
                <span>Người thực hiện: {{ emptyDisplay(item.changedByUsername) }}</span>
              </li>
            </ol>
          </div>
        </template>
      </Card>

      <StudentStatusDialog
        v-model:visible="statusDialogVisible"
        :student="student"
        @changed="loadStudentDetails"
      />
      <ResetPasswordDialog
        :visible="resetDialogVisible"
        :student-code="resetPasswordResult?.studentCode ?? ''"
        :temporary-password="resetPasswordResult?.temporaryPassword ?? ''"
        @update:visible="onResetDialogVisibilityChange"
      />
    </template>
  </div>
</template>

<style scoped>
.student-detail-page { display: grid; gap: 1rem; }
.back-button { justify-self: start; min-height: 40px; }
.student-detail-page > .page-title { margin: -.4rem 0 .25rem; }
.detail-message { margin: 0; }
.student-header-content, .student-identity, .detail-actions { display: flex; align-items: center; }
.student-header-content { justify-content: space-between; gap: 1.5rem; }
.student-identity { min-width: 0; gap: 1rem; }
.student-avatar { display: grid; flex: 0 0 64px; width: 64px; height: 64px; place-items: center; border-radius: 50%; background: var(--primary-tint); color: var(--primary); font-size: 1.25rem; font-weight: 700; }
.student-heading { min-width: 0; }
.student-heading h2 { margin: 0; color: var(--text-h); font-size: 1.25rem; overflow-wrap: anywhere; }
.student-heading p { margin: .25rem 0 .5rem; color: var(--text); }
.detail-actions { flex-wrap: wrap; justify-content: flex-end; gap: .5rem; }
.detail-actions :deep(.p-button) { min-height: 40px; }
.student-information { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1.25rem 1.5rem; margin: 0; }
.student-information div { min-width: 0; }
.student-information dt { color: var(--text); font-size: var(--text-body); }
.student-information dd { margin: .3rem 0 0; color: var(--text-h); overflow-wrap: anywhere; }
.history-card { min-width: 0; }
.history-table-wrap { width: 100%; overflow-x: auto; }
.history-table { width: 100%; border-collapse: collapse; table-layout: fixed; text-align: left; }
.history-table th, .history-table td { padding: .8rem .65rem; border-bottom: 1px solid var(--border); vertical-align: top; overflow-wrap: anywhere; }
.history-table th { color: var(--text-h); background: var(--surface-soft); font-weight: 600; }
.history-table td small { display: block; margin-top: .3rem; color: var(--text); font-size: var(--text-caption); }
.history-table tbody tr:last-child td { border-bottom: 0; }
.history-mobile-list { display: none; list-style: none; margin: 0; padding: 0; }
.history-mobile-entry { display: grid; gap: .5rem; padding: .9rem 0; border-bottom: 1px solid var(--border); overflow-wrap: anywhere; }
.history-mobile-entry:first-child { padding-top: 0; }
.history-mobile-entry:last-child { padding-bottom: 0; border-bottom: 0; }
.history-mobile-entry time, .history-mobile-entry small { color: var(--text); }
.history-mobile-entry p { margin: 0; }
.loading-state, .empty-history { margin: 0; color: var(--text); }
@media (max-width: 767px) {
  .student-header-content, .student-identity { align-items: flex-start; }
  .student-header-content { flex-direction: column; }
  .student-avatar { flex-basis: 52px; width: 52px; height: 52px; }
  .student-heading :deep(.p-tag) { font-size: 14px; }
  .detail-actions { width: 100%; flex-direction: column-reverse; align-items: stretch; }
  .detail-actions :deep(.p-button) { width: 100%; min-height: 44px; }
  .student-information { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
  .history-table-wrap { display: none; }
  .history-mobile-list { display: grid; }
  .history-mobile-entry small { font-size: 14px; }
}
@media (max-width: 420px) { .student-information { grid-template-columns: minmax(0, 1fr); } }
</style>