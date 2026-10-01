import axiosClient from './axiosClient.js'

export function getNotifications(filter = {}) {
  return axiosClient.get('/notifications', { params: filter }).then((res) => res.data.data)
}

export function getUnreadCount() {
  return axiosClient.get('/notifications/unread-count').then((res) => res.data.data)
}

export function markNotificationRead(id) {
  return axiosClient.post(`/notifications/${id}/read`).then((res) => res.data.data)
}

export function markAllNotificationsRead() {
  return axiosClient.post('/notifications/read-all').then((res) => res.data.data)
}