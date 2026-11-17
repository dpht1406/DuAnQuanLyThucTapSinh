import axiosClient from './axiosClient.js'

export function getPlacementRequests(filter) {
  return axiosClient.get('/placement-requests', { params: filter }).then((res) => res.data.data)
}

export function getPlacementRequestById(id) {
  return axiosClient.get(`/placement-requests/${id}`).then((res) => res.data.data)
}

export function getPendingConfirmationRequests(filter) {
  return axiosClient.get('/placement-requests/pending-confirmation', { params: filter }).then((res) => res.data.data)
}

export function confirmAdminPlacementResult(id, dto) {
  return axiosClient.post(`/placement-requests/${id}/admin-confirm-result`, dto)
}

export function cancelAcceptedPlacementResult(id, note) {
  return axiosClient.post(`/placement-requests/${id}/cancel-accepted-result`, { note })
}
