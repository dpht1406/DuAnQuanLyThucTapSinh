import axiosClient from './axiosClient.js'

export function getCompanies(params = {}) {
  return axiosClient.get('/companies', { params }).then((res) => res.data.data)
}
