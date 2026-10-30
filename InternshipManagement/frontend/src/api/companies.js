import axiosClient from './axiosClient.js'

export function getCompanies(params = {}) {
  return axiosClient.get('/companies', { params }).then((res) => res.data.data)
}

export function getIndustries() {
  return axiosClient.get('/companies/industries').then((res) => res.data.data)
}

export function getCompanyById(id) {
  return axiosClient.get(`/companies/${id}`).then((res) => res.data.data)
}

export function createCompany(dto) {
  return axiosClient.post('/companies', dto).then((res) => res.data.data)
}

export function updateCompany(id, dto) {
  return axiosClient.put(`/companies/${id}`, dto)
}

export function deleteCompany(id) {
  return axiosClient.delete(`/companies/${id}`)
}

export function getCompanyAccount(id) {
  return axiosClient.get(`/companies/${id}/account`).then((res) => res.data.data)
}

export function createCompanyAccount(id) {
  return axiosClient.post(`/companies/${id}/account`).then((res) => res.data.data)
}

export function resetCompanyAccountPassword(id) {
  return axiosClient.post(`/companies/${id}/account/reset-password`).then((res) => res.data.data)
}

export function deactivateCompanyAccount(id) {
  return axiosClient.post(`/companies/${id}/account/deactivate`).then((res) => res.data.data)
}

export function activateCompanyAccount(id) {
  return axiosClient.post(`/companies/${id}/account/activate`).then((res) => res.data.data)
}

export function createCompanyAccountsBulk(companyIds) {
  return axiosClient.post('/companies/accounts/bulk', { companyIds }).then((res) => res.data.data)
}

export function createCompanyAccountsByFilter(filter = {}) {
  return axiosClient.post('/companies/accounts/bulk-by-filter', filter).then((res) => res.data.data)
}

export function exportCompanyAccountsCsv(accounts) {
  return axiosClient.post('/companies/accounts/export', { accounts }, { responseType: 'blob' })
}


