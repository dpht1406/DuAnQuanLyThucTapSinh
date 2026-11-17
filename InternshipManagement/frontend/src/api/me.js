import axiosClient from './axiosClient.js'

export function createPlacementRequest(dto) {
  return axiosClient.post('/me/placement-requests', dto).then((res) => res.data.data)
}

export function getMyProfile() {
  return axiosClient.get('/me/profile').then((res) => res.data.data)
}

export function getMyStatusHistory() {
  return axiosClient.get('/me/status-history').then((res) => res.data.data)
}

export function getMyPlacementRequests() {
  return axiosClient.get('/me/placement-requests').then((res) => res.data.data)
}

export function reportInterviewed(id, note) {
  return axiosClient.post(`/me/placement-requests/${id}/report-interviewed`, { note })
    .then((res) => res.data.data)
}
