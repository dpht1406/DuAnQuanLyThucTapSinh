import axiosClient from './axiosClient.js'

export function getStudents(filter) {
  return axiosClient.get('/students', { params: filter }).then((res) => res.data.data)
}

export function createStudent(dto) {
  return axiosClient.post('/students', dto).then((res) => res.data.data)
}

export function updateStudent(id, dto) {
  return axiosClient.put(`/students/${id}`, dto)
}

export function deleteStudent(id) {
  return axiosClient.delete(`/students/${id}`)
}

export function getStudentById(id) {
  return axiosClient.get(`/students/${id}`).then((res) => res.data.data)
}

export function changeStudentStatus(id, dto) {
  return axiosClient.post(`/students/${id}/change-status`, dto).then((res) => res.data.data)
}

export function getStudentStatusHistory(id) {
  return axiosClient.get(`/students/${id}/status-history`).then((res) => res.data.data)
}

export function assignCompanyToStudent(id, dto) {
  return axiosClient.post(`/students/${id}/assign-company`, dto).then((res) => res.data.data)
}

export function resetStudentPassword(id) {
  return axiosClient.post(`/students/${id}/reset-password`).then((res) => res.data.data)
}

export function createAccounts(studentIds) {
  return axiosClient.post('/students/create-accounts', { StudentIds: studentIds }).then((res) => res.data.data)
}

export function createAccountsByFilter(filter) {
  return axiosClient.post('/students/create-accounts/by-filter', filter).then((res) => res.data.data)
}

export function exportAccountsCsv(accounts) {
  return axiosClient.post('/students/accounts/export', { Accounts: accounts }, { responseType: 'blob' })
}

export function importStudents(file) {
  const formData = new FormData()
  formData.append('file', file)
  return axiosClient.post('/students/import', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  }).then((res) => res.data.data)
}
