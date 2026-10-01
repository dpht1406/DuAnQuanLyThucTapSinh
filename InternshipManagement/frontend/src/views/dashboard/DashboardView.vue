<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import dayjs from '../../utils/dayjs.js'
import { ArcElement, Chart, Tooltip } from 'chart.js'
import { Doughnut } from 'vue-chartjs'
import Card from 'primevue/card'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Message from 'primevue/message'
import Button from 'primevue/button'
import ProgressBar from 'primevue/progressbar'
import Tag from 'primevue/tag'
import StudentStatusDialog from '../../components/students/StudentStatusDialog.vue'
import { getDashboardSummary, getNeedAssignmentList, getRecentRejections } from '../../api/dashboard.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { formatIntegerVi, formatNumberVi, formatPercentVi } from '../../utils/numberFormat.js'

Chart.register(ArcElement, Tooltip)
const router = useRouter()
const summary = ref({ totalStudents: 0, withCompanyCount: 0, withoutCompanyCount: 0, completedCount: 0, completionRate: 0 })
const students = ref([])
const recentRejections = ref([])
const loading = ref(false)
const errorMessage = ref('')
const statusDialogVisible = ref(false)
const selectedStudent = ref(null)
const showAllStudents = ref(false)

const summaryCards = computed(() => [
  { label: 'Tổng sinh viên', value: summary.value.totalStudents, className: 'default' },
  { label: 'Đã có doanh nghiệp', value: summary.value.withCompanyCount, className: 'success' },
  { label: 'Chưa có doanh nghiệp', value: summary.value.withoutCompanyCount, className: 'warning' },
  { label: 'Đã hoàn thành', value: summary.value.completedCount, className: 'success' }
])
const assignedPercent = computed(() => summary.value.totalStudents ? (summary.value.withCompanyCount / summary.value.totalStudents) * 100 : 0)
const remainingPercent = computed(() => Math.max(0, 100 - assignedPercent.value))
const incompleteCount = computed(() => Math.max(0, summary.value.totalStudents - summary.value.completedCount))
const visibleStudents = computed(() => showAllStudents.value ? students.value : students.value.slice(0, 3))
const moreStudentCount = computed(() => Math.max(0, students.value.length - 3))
const chartData = computed(() => ({
  labels: ['Đã có doanh nghiệp', 'Chưa có doanh nghiệp'],
  datasets: [{ data: [summary.value.withCompanyCount, summary.value.withoutCompanyCount], backgroundColor: ['#10B981', '#F59E0B'], borderWidth: 0, hoverOffset: 4 }]
}))
const chartOptions = { responsive: true, maintainAspectRatio: false, cutout: '72%', plugins: { legend: { display: false }, tooltip: { enabled: true } } }

function formatDate(value) { return value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—' }
function formatRelative(value) { return value ? dayjs(value).fromNow() : '—' }
function appendError(message) { errorMessage.value = errorMessage.value ? `${errorMessage.value} ${message}` : message }
async function loadDashboard() {
  loading.value = true
  errorMessage.value = ''
  const results = await Promise.allSettled([getDashboardSummary(), getNeedAssignmentList(), getRecentRejections()])
  const [summaryResult, studentsResult, rejectionResult] = results
  if (summaryResult.status === 'fulfilled') summary.value = { ...summary.value, ...(summaryResult.value ?? {}) }
  else appendError(extractErrorMessage(summaryResult.reason, 'Không tải được số liệu tổng quan.'))
  if (studentsResult.status === 'fulfilled') students.value = Array.isArray(studentsResult.value) ? studentsResult.value : []
  else { students.value = []; appendError(extractErrorMessage(studentsResult.reason, 'Không tải được danh sách sinh viên cần gán doanh nghiệp.')) }
  if (rejectionResult.status === 'fulfilled') recentRejections.value = Array.isArray(rejectionResult.value) ? rejectionResult.value : []
  else { recentRejections.value = []; appendError(extractErrorMessage(rejectionResult.reason, 'Không tải được danh sách sinh viên vừa báo rớt.')) }
  loading.value = false
}
function openAssignmentDialog(student) {
  selectedStudent.value = { id: student.studentId, fullName: student.fullName, status: 0, companyId: null, companyName: null }
  statusDialogVisible.value = true
}
function viewStudent(student) { router.push({ name: 'student-detail', params: { id: student.studentId } }) }
function openNeedAssignmentList() { router.push({ path: '/students', query: { status: 0 } }) }
onMounted(loadDashboard)
</script>

<template>
  <div class="page-container dashboard-view">
    <header class="dashboard-header"><div><h1 class="page-title">Tổng quan</h1><p class="page-description">Theo dõi tiến độ gán doanh nghiệp thực tập của sinh viên</p></div><Button label="Tạo tài khoản" icon="pi pi-plus" @click="router.push('/students/accounts')" /></header>
    <Message v-if="errorMessage" severity="error" :closable="false" class="view-message">{{ errorMessage }}</Message>

    <section class="summary-grid" aria-label="Chỉ số tổng quan">
      <Card v-for="item in summaryCards" :key="item.label" class="summary-card" :class="item.className"><template #content><div class="summary-value">{{ formatIntegerVi(item.value) }}</div><div class="summary-label">{{ item.label }}</div></template></Card>
    </section>

    <section class="insights-grid">
      <Card class="distribution-card"><template #title>Phân bổ doanh nghiệp</template><template #subtitle>{{ formatIntegerVi(summary.withCompanyCount) }}/{{ formatIntegerVi(summary.totalStudents) }} sinh viên đã có nơi thực tập</template><template #content><div class="distribution-content"><div class="donut-wrap"><Doughnut :data="chartData" :options="chartOptions" /><div class="donut-center"><strong>{{ formatPercentVi(assignedPercent) }}</strong><span>đã gán</span></div></div><div class="legend-list"><div class="legend-row"><span class="legend-dot success" /><div><strong>Đã có doanh nghiệp</strong><span>{{ formatIntegerVi(summary.withCompanyCount) }} · {{ formatNumberVi(assignedPercent) }}%</span></div></div><div class="legend-row"><span class="legend-dot warning" /><div><strong>Chưa có doanh nghiệp</strong><span>{{ formatIntegerVi(summary.withoutCompanyCount) }} · {{ formatNumberVi(remainingPercent) }}%</span></div></div></div></div><button type="button" class="card-link" @click="openNeedAssignmentList">Gán doanh nghiệp cho {{ formatIntegerVi(summary.withoutCompanyCount) }} sinh viên <span aria-hidden="true">→</span></button></template></Card>
      <Card class="completion-card"><template #title>Tỷ lệ hoàn thành</template><template #subtitle>Sinh viên đã hoàn thành kỳ thực tập</template><template #content><div class="completion-rate">{{ formatPercentVi(summary.completionRate) }}</div><ProgressBar :value="Number(summary.completionRate) || 0" :show-value="false" class="completion-progress" /><div class="completion-meta"><span>{{ formatIntegerVi(summary.completedCount) }} / {{ formatIntegerVi(summary.totalStudents) }} sinh viên</span><span>Còn {{ formatIntegerVi(incompleteCount) }} sinh viên chưa hoàn thành</span></div></template></Card>
    </section>

    <Card class="table-card"><template #title><div class="card-title-row"><span>Sinh viên cần gán doanh nghiệp</span><span class="count-badge">{{ formatIntegerVi(students.length) }}</span></div></template><template #content><DataTable :value="students" :loading="loading" :paginator="true" :rows="5" data-key="studentId" paginator-template="PrevPageLink PageLinks NextPageLink CurrentPageReport RowsPerPageDropdown" current-page-report-template="Hiển thị {first}–{last} trên {totalRecords} sinh viên" :rows-per-page-options="[5, 10, 20]" class="assignment-table"><Column field="studentCode" header="MSSV" /><Column field="fullName" header="Họ tên" /><Column field="email" header="Email" /><Column field="phoneNumber" header="SĐT" /><Column header="Ngày tạo tài khoản"><template #body="{ data }">{{ formatDate(data.accountCreatedAt) }}</template></Column><Column header="Chờ gán"><template #body="{ data }"><Tag :value="`${data.daysSinceCreated ?? 0} ngày`" severity="warn" rounded /></template></Column><Column header="Thao tác"><template #body="{ data }"><Button label="Gán DN" size="small" outlined @click="openAssignmentDialog(data)" /></template></Column><template #empty><div class="empty-state"><span class="pi pi-check-circle" /><strong>Tất cả sinh viên đã được xử lý</strong><span>Không có sinh viên nào cần gán doanh nghiệp.</span></div></template></DataTable></template></Card>

    <section class="mobile-student-list"><div v-if="loading" class="mobile-loading">Đang tải danh sách...</div><div v-else-if="students.length === 0" class="empty-state"><span class="pi pi-check-circle" /><strong>Tất cả sinh viên đã được xử lý</strong><span>Không có sinh viên nào cần gán doanh nghiệp.</span></div><article v-for="student in visibleStudents" :key="student.studentId" class="student-mobile-card"><div class="student-card-heading"><strong>{{ student.fullName }}</strong><Tag :value="`${student.daysSinceCreated ?? 0} ngày`" severity="warn" rounded /></div><span>{{ student.studentCode }}</span><span>{{ student.email }}</span><span v-if="student.phoneNumber">{{ student.phoneNumber }}</span><Button label="Gán doanh nghiệp" outlined class="mobile-assign-button" @click="openAssignmentDialog(student)" /></article><Button v-if="moreStudentCount > 0" :label="showAllStudents ? 'Thu gọn danh sách' : `Xem thêm ${moreStudentCount} sinh viên`" text class="show-more-button" @click="showAllStudents = !showAllStudents" /></section>

    <Card class="table-card rejection-card"><template #title><div class="card-title-row"><span>Sinh viên vừa báo rớt</span><span class="count-badge">{{ formatIntegerVi(recentRejections.length) }}</span></div></template><template #content><DataTable :value="recentRejections" :loading="loading" :paginator="true" :rows="5" data-key="studentId" paginator-template="PrevPageLink PageLinks NextPageLink CurrentPageReport" current-page-report-template="Hiển thị {first}–{last} trên {totalRecords} sinh viên"><Column field="studentCode" header="MSSV" /><Column field="fullName" header="Họ tên" /><Column field="rejectedCompanyName" header="Doanh nghiệp vừa rớt" /><Column field="reason" header="Lý do" /><Column header="Thời điểm"><template #body="{ data }">{{ formatRelative(data.rejectedAt) }}</template></Column><Column header="Thao tác"><template #body="{ data }"><Button label="Xem" size="small" outlined @click="viewStudent(data)" /></template></Column><template #empty><div class="empty-state"><span class="pi pi-check-circle" /><strong>Chưa có sinh viên nào báo rớt</strong><span>Thông tin sẽ hiển thị tại đây khi có sinh viên báo rớt.</span></div></template></DataTable></template></Card>
    <section class="mobile-rejection-list"><div v-if="loading" class="mobile-loading">Đang tải danh sách...</div><div v-else-if="recentRejections.length === 0" class="empty-state"><span class="pi pi-check-circle" /><strong>Chưa có sinh viên nào báo rớt</strong><span>Thông tin sẽ hiển thị tại đây khi có sinh viên báo rớt.</span></div><article v-for="student in recentRejections" :key="student.studentId" class="student-mobile-card"><div class="student-card-heading"><strong>{{ student.fullName }}</strong><span class="rejection-time">{{ formatRelative(student.rejectedAt) }}</span></div><span>{{ student.studentCode }}</span><span>{{ student.rejectedCompanyName || '—' }}</span><span>{{ student.reason || 'Chưa có lý do' }}</span><Button label="Xem" outlined class="mobile-assign-button" @click="viewStudent(student)" /></article></section>
    <StudentStatusDialog v-model:visible="statusDialogVisible" :student="selectedStudent" @changed="loadDashboard" />
  </div>
</template>

<style scoped>
.dashboard-view { display: grid; gap: 24px; }.dashboard-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; }.page-description { margin: 8px 0 0; color: var(--text); font-size: var(--text-body); }.view-message { margin: 0; }.summary-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 20px; }.summary-card { border: 1px solid var(--border); }.summary-value { color: var(--text-h); font-size: var(--text-kpi); font-weight: 700; line-height: 1.1; }.summary-card.success .summary-value { color: var(--success); }.summary-card.warning .summary-value { color: var(--warning); }.summary-label { margin-top: 10px; color: var(--text); font-size: var(--text-body); }.insights-grid { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(0, .65fr); gap: 20px; }.distribution-card, .completion-card, .table-card { border: 1px solid var(--border); }.distribution-card :deep(.p-card-subtitle), .completion-card :deep(.p-card-subtitle) { color: var(--text); font-size: var(--text-body); }.distribution-content { display: flex; align-items: center; justify-content: center; gap: 56px; padding: 20px 0 24px; }.donut-wrap { position: relative; flex: 0 0 200px; width: 200px; height: 200px; }.donut-wrap :deep(canvas) { width: 200px !important; height: 200px !important; }.donut-center { position: absolute; inset: 0; display: grid; place-content: center; justify-items: center; pointer-events: none; }.donut-center strong { color: var(--text-h); font-size: 24px; font-weight: 700; }.donut-center span { color: var(--text); font-size: 12px; }.legend-list { display: grid; gap: 20px; }.legend-row { display: flex; align-items: flex-start; gap: 10px; }.legend-dot { flex: 0 0 10px; width: 10px; height: 10px; margin-top: 4px; border-radius: 50%; }.legend-dot.success { background: var(--success); }.legend-dot.warning { background: var(--warning); }.legend-row div { display: grid; gap: 4px; }.legend-row strong { color: var(--text-h); font-size: var(--text-body); }.legend-row span:last-child { color: var(--text); font-size: 13px; }.card-link { padding: 0; color: var(--primary); background: transparent; border: 0; font: inherit; font-size: 13px; font-weight: 600; cursor: pointer; }.completion-rate { margin: 28px 0 20px; color: var(--success); font-size: 32px; font-weight: 700; }.completion-progress { height: 10px; }.completion-progress :deep(.p-progressbar-value) { background: var(--success); }.completion-meta { display: flex; justify-content: space-between; gap: 12px; margin-top: 14px; color: var(--text); font-size: 13px; }.card-title-row { display: flex; align-items: center; gap: 10px; }.count-badge { display: inline-grid; place-items: center; min-width: 24px; height: 24px; padding: 0 7px; color: var(--primary); background: var(--primary-tint); border-radius: var(--radius-pill); font-size: 12px; }.assignment-table { margin-top: 2px; }.assignment-table :deep(.p-datatable-thead > tr > th) { white-space: nowrap; }.assignment-table :deep(.p-paginator) { justify-content: center; }.empty-state { display: grid; justify-items: center; gap: 8px; padding: 28px 12px; color: var(--text); text-align: center; }.empty-state > span:first-child { color: var(--success); font-size: 28px; }.empty-state strong { color: var(--text-h); font-size: 14px; }.mobile-student-list { display: none; }.rejection-card { margin-top: 0; }
.mobile-student-list, .mobile-rejection-list { display: none; }
@media (max-width: 900px) { .summary-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }.insights-grid { grid-template-columns: 1fr; } }
@media (max-width: 767px) { .dashboard-view { gap: 16px; }.dashboard-header { align-items: flex-start; flex-direction: column; gap: 16px; }.dashboard-header :deep(.p-button) { width: 100%; }.summary-grid { gap: 12px; }.summary-card :deep(.p-card-body) { padding: 16px; }.summary-value { font-size: 26px; }.insights-grid { gap: 16px; }.distribution-content { flex-direction: column; gap: 24px; padding-top: 12px; }.legend-list { width: min(100%, 260px); }.completion-rate { margin-top: 12px; }.completion-meta { flex-direction: column; gap: 6px; }.table-card { display: none; }.mobile-student-list, .mobile-rejection-list { display: grid; gap: 12px; }.student-mobile-card { display: grid; gap: 8px; padding: 16px; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); }.student-card-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }.student-mobile-card > span { overflow-wrap: anywhere; color: var(--text); font-size: 13px; }.rejection-time { color: var(--text); font-size: 12px; }.mobile-assign-button { width: 100%; margin-top: 4px; }.show-more-button { justify-self: center; }.mobile-loading { padding: 24px; color: var(--text); text-align: center; } }
</style>
