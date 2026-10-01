<script setup>
import { onMounted, ref } from 'vue'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Paginator from 'primevue/paginator'
import { getNotifications, markAllNotificationsRead, markNotificationRead } from '../../api/notifications.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import dayjs from '../../utils/dayjs.js'

const notifications = ref([])
const loading = ref(false)
const errorMessage = ref('')
const totalRecords = ref(0)
const page = ref(0)
const rows = ref(10)

function isRejection(notification) {
  const kind = String(notification.type || notification.notificationType || '').toLowerCase()
  return kind.includes('reject') || kind.includes('return') || kind.includes('back')
}
function notificationIcon(notification) { return isRejection(notification) ? 'pi pi-replay' : 'pi pi-exclamation' }
function notificationTone(notification) { return isRejection(notification) ? 'warning' : 'danger' }
async function loadNotifications() {
  loading.value = true
  errorMessage.value = ''
  try {
    const result = await getNotifications({ pageNumber: page.value + 1, pageSize: rows.value })
    notifications.value = result?.items ?? []
    totalRecords.value = result?.totalCount ?? 0
  } catch (err) {
    errorMessage.value = extractErrorMessage(err, 'Không tải được thông báo.')
  } finally { loading.value = false }
}
async function openNotification(notification) {
  if (notification.isRead) return
  try { await markNotificationRead(notification.id); notification.isRead = true } catch { }
}
async function markAllRead() {
  try { await markAllNotificationsRead(); notifications.value = notifications.value.map((item) => ({ ...item, isRead: true })) } catch { }
}
function onPage(event) { page.value = event.page; rows.value = event.rows; loadNotifications() }
onMounted(loadNotifications)
</script>

<template>
  <div class="page-container notifications-page">
    <div class="page-heading"><div><h1 class="page-title">Thông báo</h1><p class="page-subtitle">Cập nhật mới nhất về hoạt động thực tập.</p></div><button type="button" class="mark-all" :disabled="!notifications.some((item) => !item.isRead)" @click="markAllRead">Đánh dấu tất cả đã đọc</button></div>
    <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>
    <Card class="notifications-card">
      <template #content>
        <p v-if="!loading && notifications.length === 0" class="empty-state">Chưa có thông báo nào</p>
        <button v-for="notification in notifications" :key="notification.id" type="button" class="notification-row" :class="{ unread: !notification.isRead }" @click="openNotification(notification)"><span class="notification-type" :class="notificationTone(notification)"><span :class="notificationIcon(notification)" /></span><span class="notification-copy"><strong>{{ notification.title }}</strong><span>{{ notification.message }}</span><time>{{ dayjs(notification.createdAt).fromNow() }}</time></span><span v-if="!notification.isRead" class="unread-dot" /></button>
      </template>
    </Card>
    <Paginator v-if="totalRecords > rows" :first="page * rows" :rows="rows" :total-records="totalRecords" :rows-per-page-options="[10, 20, 50]" @page="onPage" />
  </div>
</template>

<style scoped>
.notifications-page { display: grid; gap: 20px; }.page-heading { display: flex; align-items: end; justify-content: space-between; gap: 20px; }.page-subtitle { margin: 8px 0 0; color: var(--text); font-size: 14px; }.mark-all { padding: 8px 12px; color: var(--primary); background: transparent; border: 0; font: inherit; font-size: 13px; cursor: pointer; }.mark-all:disabled { color: var(--text); cursor: default; }.notifications-card { border: 1px solid var(--border); }.notification-row { display: flex; align-items: flex-start; width: 100%; gap: 12px; padding: 14px 4px; text-align: left; background: transparent; border: 0; border-bottom: 1px solid var(--border); cursor: pointer; }.notification-row:hover, .notification-row.unread { background: var(--primary-tint); }.notification-type { flex: 0 0 30px; display: grid; place-items: center; width: 30px; height: 30px; color: white; border-radius: 50%; }.notification-type.danger { background: var(--danger); }.notification-type.warning { background: var(--warning); }.notification-copy { display: grid; min-width: 0; gap: 4px; }.notification-copy strong { color: var(--text-h); font-size: 14px; }.notification-copy > span { color: var(--text); font-size: 14px; line-height: 1.45; }.notification-copy time { color: var(--text); font-size: 12px; }.unread-dot { flex: 0 0 7px; width: 7px; height: 7px; margin-top: 5px; background: var(--primary); border-radius: 50%; }.empty-state { margin: 30px 0; color: var(--text); text-align: center; }.p-paginator { justify-content: center; border: 0; background: transparent; }
@media (max-width: 767px) { .page-heading { align-items: flex-start; flex-direction: column; gap: 8px; }.notifications-page { gap: 16px; } }
</style>
