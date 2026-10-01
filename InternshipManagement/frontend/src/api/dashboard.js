import axiosClient from './axiosClient.js'

export function getDashboardSummary() {
  return axiosClient.get('/dashboard/summary').then((res) => res.data.data)
}

export function getNeedAssignmentList() {
  return axiosClient.get('/dashboard/need-assignment').then((res) => res.data.data)
}

export function getRecentRejections() {
  return axiosClient.get('/dashboard/recent-rejections').then((res) => res.data.data)
}
