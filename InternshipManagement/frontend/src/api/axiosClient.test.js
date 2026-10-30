import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { AxiosError } from 'axios'
import axiosClient from './axiosClient.js'

describe('axiosClient GET retries', () => {
  const originalAdapter = axiosClient.defaults.adapter

  beforeEach(() => {
    vi.stubGlobal('localStorage', {
      getItem: vi.fn(() => null),
      setItem: vi.fn(),
      removeItem: vi.fn()
    })
    setActivePinia(createPinia())
    vi.spyOn(console, 'warn').mockImplementation(() => {})
  })

  afterEach(() => {
    axiosClient.defaults.adapter = originalAdapter
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('retries a network-failed GET twice and marks the final error', async () => {
    let attempts = 0
    axiosClient.defaults.adapter = async (config) => {
      attempts += 1
      throw new AxiosError('Network Error', 'ERR_NETWORK', config)
    }

    await expect(axiosClient.get('/api/students?search=example')).rejects.toMatchObject({
      isNetworkError: true
    })

    expect(attempts).toBe(3)
    expect(console.warn).toHaveBeenNthCalledWith(1, '[api] retry 1/2 GET /api/students')
    expect(console.warn).toHaveBeenNthCalledWith(2, '[api] retry 2/2 GET /api/students')
  })

  it('does not retry a network-failed POST', async () => {
    let attempts = 0
    axiosClient.defaults.adapter = async (config) => {
      attempts += 1
      throw new AxiosError('Network Error', 'ERR_NETWORK', config)
    }

    await expect(axiosClient.post('/api/students', {})).rejects.toMatchObject({
      code: 'ERR_NETWORK'
    })

    expect(attempts).toBe(1)
    expect(console.warn).not.toHaveBeenCalled()
  })

  it('does not retry or refresh a GET after a 429 response', async () => {
    let attempts = 0
    axiosClient.defaults.adapter = async (config) => {
      attempts += 1
      throw new AxiosError('Too Many Requests', 'ERR_BAD_REQUEST', config, null, {
        status: 429,
        statusText: 'Too Many Requests',
        headers: {},
        config,
        data: { message: 'Vui lòng thử lại sau.' }
      })
    }

    await expect(axiosClient.get('/api/students')).rejects.toMatchObject({
      response: { status: 429, data: { message: 'Vui lòng thử lại sau.' } }
    })

    expect(attempts).toBe(1)
    expect(console.warn).not.toHaveBeenCalled()
  })
})
