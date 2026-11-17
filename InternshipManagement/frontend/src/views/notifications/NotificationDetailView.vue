<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import { getNotificationById, markNotificationRead } from '../../api/notifications.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import dayjs from '../../utils/dayjs.js'
import { getNotificationTypeMeta } from '../../utils/notificationType.js'

const route = useRoute()
const router = useRouter()

const notification = ref(null)
const loading = ref(false)
const errorMessage = ref('')
const notFound = ref(false)
const markingRead = ref(false)

const typeMeta = computed(() => getNotificationTypeMeta(notification.value?.type))

function formatDate(value) {
  return value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—'
}

async function loadNotification(idParam) {
  const parsedId = Number(idParam)
  if (!Number.isInteger(parsedId) || parsedId <= 0) {
    notification.value = null
    notFound.value = true
    errorMessage.value = ''
    loading.value = false
    return
  }

  loading.value = true
  errorMessage.value = ''
  notFound.value = false
  notification.value = null

  try {
    const data = await getNotificationById(parsedId)
    if (parsedId !== Number(route.params.id)) return
    if (!data) {
      notFound.value = true
      notification.value = null
      return
    }
    notification.value = data

    if (!data.isRead) {
      try {
        await markNotificationRead(data.id)
        if (notification.value && notification.value.id === data.id) {
          notification.value.isRead = true
          notification.value.readAt = new Date().toISOString()
        }
      } catch {
        // Lỗi đánh dấu đã đọc tự động không làm gián đoạn hiển thị chi tiết
      }
    }
  } catch (err) {
    if (parsedId !== Number(route.params.id)) return
    if (err?.response?.status === 404) {
      notFound.value = true
      notification.value = null
    } else {
      notification.value = null
      errorMessage.value = extractErrorMessage(err, 'Không tải được chi tiết thông báo.')
    }
  } finally {
    if (parsedId === Number(route.params.id)) {
      loading.value = false
    }
  }
}

async function handleManualMarkRead() {
  if (!notification.value || notification.value.isRead || markingRead.value) return
  markingRead.value = true
  try {
    await markNotificationRead(notification.value.id)
    notification.value.isRead = true
    notification.value.readAt = new Date().toISOString()
  } catch (err) {
    errorMessage.value = extractErrorMessage(err, 'Không thể đánh dấu đã đọc.')
  } finally {
    markingRead.value = false
  }
}

watch(
  () => route.params.id,
  (newId) => {
    loadNotification(newId)
  },
  { immediate: true }
)
</script>

<template>
  <div class="page-container notification-detail-page">
    <Button
      label="Quay lại danh sách"
      icon="pi pi-arrow-left"
      severity="secondary"
      text
      class="back-button"
      aria-label="Quay lại danh sách thông báo"
      @click="router.push({ name: 'notifications' })"
    />

    <Message v-if="errorMessage" severity="error" :closable="false" class="error-banner">
      {{ errorMessage }}
    </Message>

    <p v-if="loading" class="state-msg">Đang tải chi tiết thông báo...</p>

    <Card v-else-if="notFound" class="detail-card not-found-card">
      <template #content>
        <div class="empty-state">
          <p class="not-found-text">Không tìm thấy thông báo.</p>
          <Button
            label="Quay lại danh sách"
            icon="pi pi-arrow-left"
            severity="secondary"
            aria-label="Quay lại danh sách thông báo"
            @click="router.push({ name: 'notifications' })"
          />
        </div>
      </template>
    </Card>

    <template v-else-if="notification">
      <Card class="detail-card">
        <template #content>
          <div class="detail-header">
            <div class="header-main">
              <span class="type-badge" :class="typeMeta.tone">
                <i :class="typeMeta.icon" aria-hidden="true" />
                <span>{{ typeMeta.label }}</span>
              </span>
              <h1 class="notification-title">{{ notification.title }}</h1>
            </div>

            <div class="header-actions">
              <Button
                v-if="!notification.isRead"
                label="Đánh dấu đã đọc"
                icon="pi pi-check"
                severity="secondary"
                outlined
                :loading="markingRead"
                aria-label="Đánh dấu đã đọc"
                @click="handleManualMarkRead"
              />
              <Button
                v-if="notification.studentId != null"
                label="Xem hồ sơ sinh viên"
                icon="pi pi-user"
                aria-label="Xem hồ sơ sinh viên"
                @click="router.push({ name: 'student-detail', params: { id: notification.studentId } })"
              />
            </div>
          </div>

          <div class="notification-body">
            <p>{{ notification.message }}</p>
          </div>

          <div v-if="notification.reason" class="reason-block">
            <div class="reason-heading">
              <i class="pi pi-info-circle" aria-hidden="true" />
              <strong>Lý do chi tiết</strong>
            </div>
            <p class="reason-text">{{ notification.reason }}</p>
          </div>

          <div class="meta-section">
            <dl class="meta-grid">
              <div class="meta-item">
                <dt>Thời gian tạo</dt>
                <dd>
                  <span>{{ formatDate(notification.createdAt) }}</span>
                  <span v-if="notification.createdAt"> ({{ dayjs(notification.createdAt).fromNow() }})</span>
                </dd>
              </div>
              <div class="meta-item">
                <dt>Trạng thái</dt>
                <dd>
                  <span v-if="notification.isRead" class="status-read">
                    Đã đọc <small v-if="notification.readAt">(lúc {{ formatDate(notification.readAt) }})</small>
                  </span>
                  <span v-else class="status-unread">Chưa đọc</span>
                </dd>
              </div>
              <div v-if="notification.companyName" class="meta-item">
                <dt>Doanh nghiệp</dt>
                <dd>{{ notification.companyName }}</dd>
              </div>
            </dl>
          </div>
        </template>
      </Card>
    </template>
  </div>
</template>

<style scoped>
.notification-detail-page {
  display: grid;
  gap: 1rem;
}

.back-button {
  justify-self: start;
  min-height: 40px;
  color: var(--text);
}

.back-button:hover {
  color: var(--text-h);
}

.error-banner {
  margin: 0;
}

.state-msg {
  margin: 0;
  color: var(--text);
}

.detail-card {
  border: 1px solid var(--border);
}

.not-found-card .empty-state {
  display: grid;
  gap: 1rem;
  justify-items: center;
  text-align: center;
  padding: 2rem 1rem;
}

.not-found-text {
  margin: 0;
  color: var(--text);
  font-size: 15px;
}

.detail-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1.5rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--border);
}

.header-main {
  min-width: 0;
  display: grid;
  gap: 0.5rem;
}

.type-badge {
  display: inline-flex;
  align-items: center;
  align-self: flex-start;
  gap: 6px;
  padding: 4px 10px;
  border-radius: var(--radius-pill, 999px);
  font-size: 13px;
  font-weight: 500;
}

.type-badge.danger {
  color: var(--danger);
  background: var(--surface-soft);
  border: 1px solid var(--danger);
}

.type-badge.warning {
  color: var(--warning);
  background: var(--surface-soft);
  border: 1px solid var(--warning);
}

.type-badge.success {
  color: var(--success);
  background: var(--surface-soft);
  border: 1px solid var(--success);
}

.type-badge.primary {
  color: var(--primary);
  background: var(--primary-tint);
  border: 1px solid var(--primary);
}

.notification-title {
  margin: 0;
  color: var(--text-h);
  font-size: 1.25rem;
  font-weight: 700;
  line-height: 1.35;
  overflow-wrap: anywhere;
}

.header-actions {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 0.5rem;
}

.header-actions :deep(.p-button) {
  min-height: 40px;
}

.notification-body {
  padding: 1.25rem 0;
}

.notification-body p {
  margin: 0;
  color: var(--text-h);
  font-size: 15px;
  line-height: 1.6;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.reason-block {
  margin: 0.5rem 0 1.25rem;
  padding: 1rem 1.25rem;
  background: var(--surface-soft);
  border-left: 4px solid var(--warning);
  border-radius: var(--radius-sm, 6px);
}

.reason-heading {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--warning);
  font-size: 14px;
  margin-bottom: 0.35rem;
}

.reason-text {
  margin: 0;
  color: var(--text-h);
  font-size: 14px;
  line-height: 1.5;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.meta-section {
  padding-top: 1.25rem;
  border-top: 1px solid var(--border);
}

.meta-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.25rem 1.5rem;
  margin: 0;
}

.meta-item {
  min-width: 0;
}

.meta-item dt {
  color: var(--text);
  font-size: 13px;
  margin-bottom: 0.25rem;
}

.meta-item dd {
  margin: 0;
  color: var(--text-h);
  font-size: 14px;
  font-weight: 500;
  overflow-wrap: anywhere;
}

.status-unread {
  color: var(--warning);
}

.status-read {
  color: var(--text);
}

.status-read small {
  color: var(--text);
  font-size: 12px;
}

@media (max-width: 767px) {
  .detail-header {
    flex-direction: column;
    align-items: stretch;
    gap: 1rem;
  }

  .header-actions {
    flex-direction: column-reverse;
    align-items: stretch;
  }

  .header-actions :deep(.p-button) {
    width: 100%;
    min-height: 44px;
  }

  .meta-grid {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
}
</style>
