import axiosClient from './axiosClient.js'

export function getAbout() {
  return axiosClient.get('/about').then((res) => res.data.data)
}

export function updateAbout(content) {
  return axiosClient.put('/about', content).then((res) => res.data.data)
}

export function resetAbout() {
  return axiosClient.post('/about/reset').then((res) => res.data.data)
}
