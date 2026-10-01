import axiosClient from './axiosClient.js'

export function getPlacementRequests(filter) {
  return axiosClient.get('/placement-requests', { params: filter }).then((res) => res.data.data)
}

export function approveRequest(id) {
  return axiosClient.post(`/placement-requests/${id}/approve`)
}

export function rejectRequest(id, rejectReason) {
  return axiosClient.post(`/placement-requests/${id}/reject`, { rejectReason })
}
