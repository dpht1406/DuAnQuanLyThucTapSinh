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
