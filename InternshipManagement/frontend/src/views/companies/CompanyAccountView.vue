<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import {
  activateCompanyAccount,
  createCompanyAccount,
  deactivateCompanyAccount,
  getCompanyAccount,
  getCompanyById,
  resetCompanyAccountPassword
} from '../../api/companies.js'
import { useAccountResultStore } from '../../stores/accountResult.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { formatServerDateTime } from '../../utils/dateTime.js'
import { validateEmailAddress } from '../../utils/emailValidation.js'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const confirm = useConfirm()
const accountResultStore = useAccountResultStore()

const companyId = computed(() => Number(route.params.id))
const loading = ref(false)
const saving = ref(false)
const loadError = ref('')
const actionError = ref('')
const company = ref(null)
const account = ref(null)

const emailValidationError = computed(() => {
  const email = company.value?.contactEmail
  if (!email || !String(email).trim()) {
    return 'Chưa có email liên hệ'
  }
  return validateEmailAddress(String(email).trim(), 150)
})

const isEmailValid = computed(() => emailValidationError.value === null)

async function loadData() {
  const id = companyId.value
  if (!id || Number.isNaN(id)) return

  loading.value = true
  loadError.value = ''
  actionError.value = ''

  try {
    const [companyData, accountData] = await Promise.all([
      getCompanyById(id),
      getCompanyAccount(id)
    ])
    company.value = companyData
    account.value = accountData
  } catch (err) {
    if (err?.response?.status === 404) {
      toast.add({
        severity: 'error',
        summary: 'Không tìm thấy',
        detail: extractErrorMessage(err, 'Không tìm thấy thông tin doanh nghiệp.'),
        life: 3000
      })
      router.replace('/companies')
      return
    }
    loadError.value = extractErrorMessage(err, 'Không tải được thông tin tài khoản doanh nghiệp.')
  } finally {
    loading.value = false
  }
}

function goToUpdateEmail() {
  router.push({
    name: 'company-edit',
    params: { id: companyId.value },
    query: { redirect: 'account' }
  })
}

async function handleCreateAccount() {
  if (saving.value || !isEmailValid.value) return

  saving.value = true
  actionError.value = ''

  try {
    const data = await createCompanyAccount(companyId.value)
    accountResultStore.set('company', {
      ...data,
      mode: 'create',
      companyId: companyId.value
    })
    await router.replace({ name: 'account-result' })
  } catch (err) {
    actionError.value = extractErrorMessage(err, 'Không thể tạo tài khoản doanh nghiệp.')
  } finally {
    saving.value = false
  }
}

function confirmResetPassword() {
  actionError.value = ''
  confirm.require({
    header: 'Xác nhận đặt lại mật khẩu',
    message: `Đặt lại mật khẩu cho tài khoản doanh nghiệp "${account.value?.username}"? Mọi phiên đăng nhập hiện tại của doanh nghiệp sẽ bị đăng xuất. Mật khẩu tạm chỉ hiển thị một lần ở bước tiếp theo.`,
    icon: 'pi pi-key',
    rejectLabel: 'Hủy',
    acceptLabel: 'Đặt lại',
    rejectClass: 'p-button-secondary p-button-text',
    acceptClass: 'p-button-danger',
    accept: async () => {
      saving.value = true
      actionError.value = ''
      try {
        const data = await resetCompanyAccountPassword(companyId.value)
        accountResultStore.set('company', {
          ...data,
          mode: 'reset',
          companyId: companyId.value
        })
        await router.replace({ name: 'account-result' })
      } catch (err) {
        actionError.value = extractErrorMessage(err, 'Không thể đặt lại mật khẩu.')
      } finally {
        saving.value = false
      }
    }
  })
}

function confirmToggleActive(lock) {
  actionError.value = ''
  confirm.require({
    header: lock ? 'Xác nhận khóa tài khoản' : 'Xác nhận mở khóa tài khoản',
    message: lock
      ? `Bạn có chắc muốn khóa tài khoản "${account.value?.username}"? Doanh nghiệp sẽ không thể đăng nhập vào hệ thống.`
      : `Bạn có chắc muốn mở khóa tài khoản "${account.value?.username}"? Doanh nghiệp sẽ có thể đăng nhập lại vào hệ thống.`,
    icon: lock ? 'pi pi-lock' : 'pi pi-lock-open',
    rejectLabel: 'Hủy',
    acceptLabel: lock ? 'Khóa tài khoản' : 'Mở khóa',
    rejectClass: 'p-button-secondary p-button-text',
    acceptClass: lock ? 'p-button-danger' : 'p-button-primary',
    accept: async () => {
      saving.value = true
      actionError.value = ''
      try {
        if (lock) {
          await deactivateCompanyAccount(companyId.value)
          toast.add({
            severity: 'success',
            summary: 'Thành công',
            detail: 'Khóa tài khoản doanh nghiệp thành công.',
            life: 3000
          })
        } else {
          await activateCompanyAccount(companyId.value)
          toast.add({
            severity: 'success',
            summary: 'Thành công',
            detail: 'Mở khóa tài khoản doanh nghiệp thành công.',
            life: 3000
          })
        }
        account.value = await getCompanyAccount(companyId.value)
      } catch (err) {
        actionError.value = extractErrorMessage(err, lock ? 'Không thể khóa tài khoản.' : 'Không thể mở khóa tài khoản.')
      } finally {
        saving.value = false
      }
    }
  })
}

watch(() => route.params.id, loadData, { immediate: true })
</script>

<template>
  <FormPage
    class="company-account-page"
    title="Tài khoản doanh nghiệp"
    subtitle="Quản lý thông tin đăng nhập và quyền truy cập của doanh nghiệp."
    back-label="Quay lại"
    :back-to="{ name: 'companies' }"
  >
    <div v-if="loading" class="account-skeleton" aria-hidden="true">
      <Skeleton width="40%" height="1.5rem" />
      <Skeleton width="100%" height="4.5rem" />
      <Skeleton width="35%" height="1.5rem" />
      <Skeleton width="100%" height="6rem" />
    </div>

    <div v-else-if="loadError" class="account-load-error">
      <Message severity="error" :closable="false" class="w-full">{{ loadError }}</Message>
      <Button label="Thử lại" icon="pi pi-refresh" severity="secondary" outlined @click="loadData" />
    </div>

    <template v-else-if="company && account">
      <Message v-if="actionError" severity="error" :closable="false" class="account-error-message">
        {{ actionError }}
      </Message>

      <FormSection title="Doanh nghiệp" description="Thông tin doanh nghiệp và người đại diện.">
        <div class="account-info-grid">
          <div class="account-info-item">
            <span class="info-label">Tên doanh nghiệp</span>
            <strong class="info-value">{{ company.name || '—' }}</strong>
          </div>
          <div class="account-info-item">
            <span class="info-label">Người liên hệ</span>
            <strong class="info-value">{{ company.contactPerson || '—' }}</strong>
          </div>
          <div class="account-info-item">
            <span class="info-label">Email liên hệ</span>
            <strong class="info-value">{{ company.contactEmail || '—' }}</strong>
          </div>
        </div>

        <Message
          v-if="!isEmailValid"
          severity="warn"
          :closable="false"
          class="email-warning-message"
        >
          Doanh nghiệp chưa có email liên hệ hợp lệ, cần bổ sung trước khi tạo tài khoản
        </Message>
        <div v-if="!isEmailValid" class="email-action-row">
          <Button
            type="button"
            label="Cập nhật email"
            icon="pi pi-pencil"
            severity="secondary"
            outlined
            @click="goToUpdateEmail"
          />
        </div>
      </FormSection>

      <template v-if="!account.hasAccount">
        <FormSection
          title="Tạo tài khoản"
          description="Khởi tạo tài khoản đăng nhập hệ thống cho doanh nghiệp."
        >
          <div class="create-account-guide">
            <p>Tên đăng nhập do hệ thống tự sinh theo mã doanh nghiệp.</p>
            <p>Mật khẩu tạm chỉ hiển thị <strong>một lần</strong> ở bước sau, hệ thống không tự gửi email.</p>
            <p>Doanh nghiệp bắt buộc phải đổi mật khẩu ở lần đăng nhập đầu tiên.</p>
          </div>
        </FormSection>

        <FormActionBar>
          <Button
            type="button"
            label="Hủy"
            severity="secondary"
            text
            class="btn-cancel"
            :disabled="saving"
            @click="router.push('/companies')"
          />
          <Button
            type="button"
            label="Tạo tài khoản"
            icon="pi pi-user-plus"
            class="btn-primary"
            :loading="saving"
            :disabled="saving || !isEmailValid"
            @click="handleCreateAccount"
          />
        </FormActionBar>
      </template>

      <template v-else>
        <FormSection
          title="Tài khoản hiện có"
          description="Thông tin tài khoản và trạng thái hoạt động."
        >
          <div class="account-info-grid">
            <div class="account-info-item">
              <span class="info-label">Tên đăng nhập</span>
              <strong class="info-value font-mono">{{ account.username || '—' }}</strong>
            </div>
            <div class="account-info-item">
              <span class="info-label">Trạng thái</span>
              <Tag
                :value="account.isActive ? 'Đang hoạt động' : 'Đã khóa'"
                :severity="account.isActive ? 'success' : 'danger'"
              />
            </div>
            <div class="account-info-item">
              <span class="info-label">Phải đổi mật khẩu</span>
              <strong class="info-value">{{ account.mustChangePassword ? 'Có' : 'Không' }}</strong>
            </div>
            <div class="account-info-item">
              <span class="info-label">Ngày tạo</span>
              <strong class="info-value">{{ formatServerDateTime(account.createdAt) || '—' }}</strong>
            </div>
          </div>
        </FormSection>

        <FormActionBar>
          <Button
            type="button"
            label="Quay lại"
            severity="secondary"
            text
            class="btn-cancel"
            :disabled="saving"
            @click="router.push('/companies')"
          />
          <Button
            v-if="account.isActive"
            type="button"
            label="Khóa tài khoản"
            icon="pi pi-lock"
            severity="danger"
            outlined
            :loading="saving"
            :disabled="saving"
            @click="confirmToggleActive(true)"
          />
          <Button
            v-else
            type="button"
            label="Mở khóa"
            icon="pi pi-lock-open"
            severity="success"
            outlined
            :loading="saving"
            :disabled="saving"
            @click="confirmToggleActive(false)"
          />
          <Button
            type="button"
            label="Đặt lại mật khẩu"
            icon="pi pi-key"
            class="btn-primary"
            :loading="saving"
            :disabled="saving"
            @click="confirmResetPassword"
          />
        </FormActionBar>
      </template>
    </template>
  </FormPage>
</template>

<style scoped>
.company-account-page {
  min-height: 100dvh;
}

.account-skeleton {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 1rem 0;
}

.account-load-error {
  display: flex;
  flex-direction: column;
  gap: 16px;
  align-items: flex-start;
}

.w-full {
  width: 100%;
}

.account-error-message {
  margin-bottom: 1.5rem;
}

.account-info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 16px;
}

.account-info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.info-label {
  font-size: 13px;
  color: var(--ui-muted);
}

.info-value {
  font-size: 14px;
  color: var(--ui-ink);
  overflow-wrap: anywhere;
}

.font-mono {
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
}

.email-warning-message {
  margin-top: 12px;
}

.email-action-row {
  margin-top: 8px;
}

.create-account-guide {
  display: flex;
  flex-direction: column;
  gap: 8px;
  color: var(--ui-ink);
  font-size: 14px;
  line-height: 1.5;
}

.create-account-guide p {
  margin: 0;
}
</style>
