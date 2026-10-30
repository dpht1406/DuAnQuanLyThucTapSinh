import axiosClient from './axiosClient.js'

export function getJobPositions(filter) {
  return axiosClient.get('/job-positions', { params: filter }).then((res) => res.data.data)
}

export function getJobPositionById(id) {
  return axiosClient.get(`/job-positions/${id}`).then((res) => res.data.data)
}

export function createJobPosition(dto) {
  return axiosClient.post('/job-positions', dto).then((res) => res.data.data)
}

export function updateJobPosition(id, dto) {
  return axiosClient.put(`/job-positions/${id}`, dto)
}

export function deleteJobPosition(id) {
  return axiosClient.delete(`/job-positions/${id}`)
}

export function approveJobPosition(id) {
  return axiosClient.post(`/job-positions/${id}/approve`)
}

export function rejectJobPosition(id, note) {
  return axiosClient.post(`/job-positions/${id}/reject`, { note })
}

export function toggleOpenJobPosition(id, isOpen) {
  const body = isOpen === undefined ? {} : { isOpen }
  return axiosClient.patch(`/job-positions/${id}/toggle-open`, body).then((res) => res.data.data)
}