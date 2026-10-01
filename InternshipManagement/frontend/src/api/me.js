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

export function changeMyStatus(dto) {
  return axiosClient.post('/me/change-status', dto)
}