<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Message from 'primevue/message'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import CredentialsCard from '../../components/accounts/CredentialsCard.vue'
import { exportCompanyAccountsCsv } from '../../api/companies.js'
import { exportAccountsCsv } from '../../api/students.js'
import { useAccountResultStore } from '../../stores/accountResult.js'
import { extractErrorMessage } from '../../utils/apiError.js'

const route = useRoute()
const router = useRouter()
const accountResultStore = useAccountResultStore()

const kind = computed(() => accountResultStore.kind)
const payload = computed(() => accountResultStore.payload)

const exporting = ref(false)
const exportError = ref('')
const csvDownloaded = ref(false)

const pageTitle = computed(() => {
  if (kind.value === 'company') {
    return payload.value?.mode === 'create'
      ? 'Tạo tài khoản thành công'
      : 'Đặt lại mật khẩu thành công'
  }
  if (kind.value === 'company-bulk') {
    return 'Tài khoản doanh nghiệp đã tạo'
  }
  if (kind.value === 'student-reset') {
    return 'Đặt lại mật khẩu thành công'
  }
  if (kind.value === 'student-bulk') {
    return 'Tài khoản đã tạo'
  }
  return 'Thông tin tài khoản'
})

const pageSubtitle = computed(() => {
  if (kind.value === 'company') {
    return 'Thông tin đăng nhập tài khoản doanh nghiệp.'
  }
  if (kind.value === 'company-bulk') {
    return 'Danh sách tài khoản doanh nghiệp vừa được khởi tạo.'
  }
  if (kind.value === 'student-reset') {
    return 'Thông tin đăng nhập của sinh viên sau khi đặt lại mật khẩu.'
  }
  if (kind.value === 'student-bulk') {
    return 'Danh sách tài khoản sinh viên vừa được khởi tạo.'
  }
  return ''
})

function getFileName(response) {
  const header = response.headers?.['content-disposition'] ?? ''
  const match = header.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i)
  return match?.[1] ? decodeURIComponent(match[1]) : 'accounts.csv'
}

async function downloadAccountsCsv() {
  if (kind.value === 'company-bulk') {
    if (!payload.value?.created?.length) return
    exporting.value = true
    exportError.value = ''
    try {
      const response = await exportCompanyAccountsCsv(payload.value.created)
      const url = URL.createObjectURL(response.data)
      const link = document.createElement('a')
      link.href = url
      link.download = getFileName(response)
      link.click()
      URL.revokeObjectURL(url)
      csvDownloaded.value = true
    } catch (err) {
      exportError.value = extractErrorMessage(err, 'Không thể tải file CSV, vui lòng thử lại.')
    } finally {
      exporting.value = false
    }
    return
  }

  if (!payload.value?.createdAccounts?.length) return

  exporting.value = true
  exportError.value = ''
  try {
    const response = await exportAccountsCsv(payload.value.createdAccounts)
    const url = URL.createObjectURL(response.data)
    const link = document.createElement('a')
    link.href = url
    link.download = getFileName(response)
    link.click()
    URL.revokeObjectURL(url)
    csvDownloaded.value = true
  } catch (err) {
    exportError.value = extractErrorMessage(err, 'Không thể tải file CSV, vui lòng thử lại.')
  } finally {
    exporting.value = false
  }
}

function onBeforeUnload(event) {
  const hasUnsavedCredentials =
    (kind.value === 'student-bulk' && payload.value?.createdAccounts?.length > 0) ||
    (kind.value === 'company-bulk' && payload.value?.created?.length > 0)

  if (hasUnsavedCredentials && !csvDownloaded.value) {
    event.preventDefault()
    event.returnValue = ''
  }
}

onBeforeRouteLeave(() => {
  const hasUnsavedCredentials =
    (kind.value === 'student-bulk' && payload.value?.createdAccounts?.length > 0) ||
    (kind.value === 'company-bulk' && payload.value?.created?.length > 0)

  if (hasUnsavedCredentials && !csvDownloaded.value) {
    return window.confirm(
      'Bạn chưa tải file CSV chứa mật khẩu. Mật khẩu sẽ không hiển thị lại sau khi rời trang. Bạn có chắc muốn rời đi?'
    )
  }
  return true
})

onMounted(() => {
  if (!kind.value || !payload.value) {
    if (route.query?.from === 'companies') {
      router.replace('/companies/accounts')
      return
    }
  }
  window.addEventListener('beforeunload', onBeforeUnload)
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', onBeforeUnload)
  accountResultStore.clear()
})
</script>

<template>
  <FormPage
    class="account-result-page"
    :title="pageTitle"
    :subtitle="pageSubtitle"
  >
    <!-- Trạng thái trống (F5 / truy cập trực tiếp URL) -->
    <template v-if="!kind || !payload">
      <Message severity="info" :closable="false" class="mb-4">
        Thông tin đăng nhập không còn hiển thị vì chỉ xem được một lần.
      </Message>
      <FormActionBar>
        <Button
          v-if="route.query?.from === 'companies'"
          label="Về tạo tài khoản doanh nghiệp"
          icon="pi pi-arrow-left"
          class="btn-primary"
          @click="router.push('/companies/accounts')"
        />
        <Button
          v-else
          label="Về bảng điều khiển"
          icon="pi pi-home"
          class="btn-primary"
          @click="router.push('/dashboard')"
        />
      </FormActionBar>
    </template>

    <!-- Rẽ nhánh 'company' -->
    <template v-else-if="kind === 'company'">
      <FormSection
        title="Thông tin đăng nhập"
        description="Thông tin tài khoản vừa được khởi tạo hoặc đặt lại."
      >
        <CredentialsCard
          username-label="Tên đăng nhập"
          :username="payload.username || ''"
          password-label="Mật khẩu tạm"
          :password="payload.temporaryPassword || ''"
        />

        <div v-if="payload.contactEmail" class="reference-info">
          <span class="reference-label">Email liên hệ:</span>
          <strong>{{ payload.contactEmail }}</strong>
        </div>

        <Message severity="warn" :closable="false" class="warn-message">
          Mật khẩu chỉ hiển thị một lần. Hãy chuyển cho doanh nghiệp qua kênh riêng (hệ thống không tự gửi email).
        </Message>
      </FormSection>

      <FormActionBar>
        <Button
          label="Về danh sách doanh nghiệp"
          icon="pi pi-arrow-left"
          class="btn-primary"
          @click="router.push('/companies')"
        />
      </FormActionBar>
    </template>

    <!-- Rẽ nhánh 'company-bulk' -->
    <template v-else-if="kind === 'company-bulk'">
      <FormSection
        title="Kết quả tạo tài khoản doanh nghiệp"
        description="Danh sách tài khoản doanh nghiệp được tạo."
      >
        <Message severity="success" :closable="false">
          Đã tạo {{ payload.successCount ?? 0 }} tài khoản, bỏ qua {{ payload.skippedCount ?? 0 }}.
        </Message>

        <template v-if="(payload.created?.length ?? 0) > 0">
          <Message severity="warn" :closable="false" class="warn-message">
            Mật khẩu chỉ hiển thị một lần, hãy tải CSV hoặc ghi lại ngay.
          </Message>

          <DataTable
            :value="payload.created"
            size="small"
            class="accounts-result-table"
            responsive-layout="scroll"
          >
            <Column field="companyName" header="Tên doanh nghiệp" />
            <Column field="username" header="Tên đăng nhập" />
            <Column field="temporaryPassword" header="Mật khẩu tạm" />
            <Column field="contactEmail" header="Email liên hệ" />
          </DataTable>
        </template>

        <template v-if="(payload.skipped?.length ?? 0) > 0">
          <div class="skipped-section-title">
            <strong>Không tạo được ({{ payload.skipped.length }} doanh nghiệp)</strong>
          </div>
          <DataTable
            :value="payload.skipped"
            size="small"
            class="accounts-result-table skipped-table"
            responsive-layout="scroll"
          >
            <Column header="Doanh nghiệp">
              <template #body="{ data }">
                <span>{{ data.companyName || `Mã DN: ${data.companyId}` }}</span>
              </template>
            </Column>
            <Column field="reason" header="Lý do" />
          </DataTable>
        </template>

        <Message v-if="exportError" severity="error" :closable="false" class="export-error-message">
          {{ exportError }}
        </Message>
      </FormSection>

      <FormActionBar>
        <Button
          label="Về tạo tài khoản doanh nghiệp"
          severity="secondary"
          text
          class="btn-cancel"
          @click="router.push('/companies/accounts')"
        />
        <Button
          v-if="(payload.created?.length ?? 0) > 0"
          label="Tải CSV"
          icon="pi pi-download"
          class="btn-primary"
          :loading="exporting"
          :disabled="!payload.created?.length"
          @click="downloadAccountsCsv"
        />
      </FormActionBar>
    </template>

    <!-- Rẽ nhánh 'student-reset' -->
    <template v-else-if="kind === 'student-reset'">
      <FormSection
        title="Thông tin đăng nhập"
        description="Thông tin tài khoản sinh viên vừa được đặt lại mật khẩu."
      >
        <CredentialsCard
          username-label="Mã sinh viên"
          :username="payload.studentCode || ''"
          password-label="Mật khẩu tạm"
          :password="payload.temporaryPassword || ''"
        />

        <Message severity="warn" :closable="false" class="warn-message">
          Mật khẩu chỉ hiển thị một lần. Hãy gửi cho sinh viên ngay. Sinh viên sẽ phải đổi mật khẩu ở lần đăng nhập đầu tiên.
        </Message>
      </FormSection>

      <FormActionBar>
        <Button
          label="Về chi tiết sinh viên"
          icon="pi pi-arrow-left"
          class="btn-primary"
          @click="router.push({ name: 'student-detail', params: { id: payload.studentId } })"
        />
      </FormActionBar>
    </template>

    <!-- Rẽ nhánh 'student-bulk' -->
    <template v-else-if="kind === 'student-bulk'">
      <FormSection
        title="Kết quả tạo tài khoản"
        description="Danh sách tài khoản sinh viên được tạo."
      >
        <Message severity="success" :closable="false">
          Đã tạo {{ payload.successCount ?? 0 }} tài khoản, bỏ qua {{ payload.skippedCount ?? 0 }} (đã có tài khoản).
        </Message>

        <template v-if="(payload.createdAccounts?.length ?? 0) > 0">
          <Message severity="warn" :closable="false" class="warn-message">
            Mật khẩu chỉ hiển thị một lần. Hãy tải file CSV ngay trước khi rời trang.
          </Message>

          <DataTable
            :value="payload.createdAccounts"
            size="small"
            class="accounts-result-table"
          >
            <Column field="studentCode" header="MSSV" />
            <Column field="plainPassword" header="Mật khẩu" />
          </DataTable>
        </template>

        <Message v-if="exportError" severity="error" :closable="false" class="export-error-message">
          {{ exportError }}
        </Message>
      </FormSection>

      <FormActionBar>
        <Button
          label="Về quản lý tài khoản sinh viên"
          severity="secondary"
          text
          class="btn-cancel"
          @click="router.push('/students/accounts')"
        />
        <Button
          v-if="(payload.createdAccounts?.length ?? 0) > 0"
          label="Tải CSV"
          icon="pi pi-download"
          class="btn-primary"
          :loading="exporting"
          :disabled="!payload.createdAccounts?.length"
          @click="downloadAccountsCsv"
        />
      </FormActionBar>
    </template>
  </FormPage>
</template>

<style scoped>
.account-result-page {
  min-height: 100dvh;
}

.reference-info {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: var(--ui-ink);
}

.reference-label {
  color: var(--ui-muted);
}

.warn-message {
  margin-top: 12px;
}

.export-error-message {
  margin-top: 12px;
}

.accounts-result-table {
  margin-top: 12px;
}

.skipped-section-title {
  margin-top: 1.5rem;
  margin-bottom: 0.5rem;
  color: var(--text-h);
  font-size: 0.95rem;
}

.skipped-table {
  margin-top: 0.5rem;
}
</style>
