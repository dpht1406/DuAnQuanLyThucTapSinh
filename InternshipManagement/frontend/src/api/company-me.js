import axiosClient from './axiosClient.js'

const BASE_URL = '/company-me'

export function getCompanyProfile() {
  return axiosClient.get(`${BASE_URL}/profile`).then((res) => res.data.data)
}

export function getCompanyJobPositions(params = {}) {
  return axiosClient.get(`${BASE_URL}/job-positions`, { params }).then((res) => res.data.data)
}

export function getCompanyJobPositionById(id) {
  return axiosClient.get(`${BASE_URL}/job-positions/${id}`).then((res) => res.data.data)
}

export function createCompanyJobPosition(dto) {
  return axiosClient.post(`${BASE_URL}/job-positions`, dto).then((res) => res.data.data)
}

export function updateCompanyJobPosition(id, dto) {
  return axiosClient.put(`${BASE_URL}/job-positions/${id}`, dto).then((res) => res.data.data)
}

export function submitCompanyJobPosition(id) {
  return axiosClient.post(`${BASE_URL}/job-positions/${id}/submit`).then((res) => res.data.data)
}

export function toggleCompanyJobPositionOpen(id, isOpen) {
  return axiosClient.patch(`${BASE_URL}/job-positions/${id}/toggle-open`, { isOpen })
    .then((res) => res.data.data)
}

export function deleteCompanyJobPosition(id) {
  return axiosClient.delete(`${BASE_URL}/job-positions/${id}`).then((res) => res.data.data)
}

export function getCompanyPlacementRequests(params = {}) {
  return axiosClient.get(`${BASE_URL}/placement-requests`, { params }).then((res) => res.data.data)
}

export function getCompanyPlacementRequestById(id) {
  return axiosClient.get(`${BASE_URL}/placement-requests/${id}`).then((res) => res.data.data)
}

export function inviteCompanyPlacementRequestToInterview(id, dto) {
  return axiosClient.post(`${BASE_URL}/placement-requests/${id}/invite-interview`, dto)
    .then((res) => res.data.data)
}

export function rejectCompanyPlacementRequest(id, dto) {
  return axiosClient.post(`${BASE_URL}/placement-requests/${id}/reject`, dto)
    .then((res) => res.data.data)
}

export function recordCompanyPlacementResult(id, dto) {
  return axiosClient.post(`${BASE_URL}/placement-requests/${id}/result`, dto)
    .then((res) => res.data.data)
}
