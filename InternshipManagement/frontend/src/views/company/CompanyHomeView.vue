<script setup>
import { onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import { useRouter } from 'vue-router'
import { getCompanyJobPositions, getCompanyProfile } from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const router = useRouter()
const profile = ref(null)
const counts = ref({ draft: 0, pendingApproval: 0, approved: 0, rejected: 0 })
const loading = ref(false)
const error = ref('')

const statusCards = [
  { key: 'draft', label: 'Draft', icon: 'pi pi-file-edit' },
  { key: 'pendingApproval', label: 'PendingApproval', icon: 'pi pi-clock' },
  { key: 'approved', label: 'Approved', icon: 'pi pi-check-circle' },
  { key: 'rejected', label: 'Rejected', icon: 'pi pi-times-circle' }
]
const statusFilters = [
  ['draft', 0],
  ['pendingApproval', 1],
  ['approved', 2],
  ['rejected', 3]
]

async function loadDashboard() {
  loading.value = true
  error.value = ''
  try {
    const [company, ...positionCounts] = await Promise.all([
      getCompanyProfile(),
      ...statusFilters.map(([, approvalStatus]) =>
        getCompanyJobPositions({ approvalStatus, pageNumber: 1, pageSize: 1 })
      )
    ])
    profile.value = company
    counts.value = Object.fromEntries(
      statusFilters.map(([key], index) => [key, positionCounts[index]?.totalCount ?? 0])
    )
  } catch (err) {
    error.value = extractErrorMessage(err, 'Không tải được thông tin doanh nghiệp.')
  } finally {
    loading.value = false
  }
}

onMounted(loadDashboard)
</script>

<template>
  <div class="page-container company-home">
    <header class="home-heading">
      <div>
        <h1 class="page-title">Trang doanh nghiệp</h1>
        <p class="home-subtitle">Tổng quan thông tin và vị trí tuyển dụng của doanh nghiệp.</p>
      </div>
      <Button
        label="Quản lý vị trí"
        icon="pi pi-briefcase"
        @click="router.push({ name: 'company-job-positions' })"
      />
    </header>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <template v-if="profile">
      <Card class="company-profile-card">
        <template #title>{{ profile.name }}</template>
        <template #content>
          <dl class="company-details">
            <div><dt>Ngành nghề</dt><dd>{{ profile.industry || '—' }}</dd></div>
            <div><dt>Địa chỉ</dt><dd>{{ profile.address || '—' }}</dd></div>
            <div><dt>Người liên hệ</dt><dd>{{ profile.contactPerson || '—' }}</dd></div>
            <div><dt>Chức vụ</dt><dd>{{ profile.contactPosition || '—' }}</dd></div>
            <div><dt>Điện thoại</dt><dd>{{ profile.contactPhone || '—' }}</dd></div>
            <div><dt>Email</dt><dd>{{ profile.contactEmail || '—' }}</dd></div>
          </dl>
        </template>
      </Card>

      <section class="position-summary" aria-label="Số lượng vị trí theo trạng thái">
        <Card v-for="status in statusCards" :key="status.key" class="status-card">
          <template #content>
            <div class="status-card-content">
              <span class="status-icon" :class="status.icon" aria-hidden="true" />
              <div><span>{{ status.label }}</span><strong>{{ loading ? '—' : counts[status.key] }}</strong></div>
            </div>
          </template>
        </Card>
      </section>
    </template>
  </div>
</template>

<style scoped>
.company-home { display: grid; gap: 20px; }
.home-heading { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.home-subtitle { margin: 7px 0 0; color: var(--text); font-size: 14px; }
.company-profile-card, .status-card { border: 1px solid var(--border); }
.company-details { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px 28px; margin: 0; }
.company-details > div { min-width: 0; }
.company-details dt { margin-bottom: 5px; color: var(--text); font-size: 12px; }
.company-details dd { margin: 0; color: var(--text-h); font-size: 14px; overflow-wrap: anywhere; }
.position-summary { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.status-card-content { display: flex; align-items: center; gap: 13px; }
.status-icon { display: grid; place-items: center; width: 42px; height: 42px; color: var(--primary); background: var(--primary-tint); border-radius: 10px; font-size: 18px; }
.status-card-content > div { display: grid; gap: 4px; color: var(--text); font-size: 12px; }
.status-card-content strong { color: var(--text-h); font-size: 22px; line-height: 1; }
@media (max-width: 767px) {
  .home-heading { align-items: flex-start; flex-direction: column; }
  .home-heading :deep(.p-button) { width: 100%; min-height: 44px; }
  .position-summary { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
  .company-details { grid-template-columns: 1fr; gap: 14px; }
}
</style>
