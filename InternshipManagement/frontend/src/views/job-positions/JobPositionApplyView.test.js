import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  createPlacementRequest: vi.fn(),
  getMyProfile: vi.fn(),
  getJobPositionById: vi.fn(),
  toastAdd: vi.fn(),
  push: vi.fn()
}))

vi.mock('vue', async () => {
  const actual = await vi.importActual('vue')
  return {
    ...actual,
    useSSRContext: () => ({ modules: new Set() })
  }
})

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: '1' }, query: {} }),
  useRouter: () => ({ push: mocks.push }),
  onBeforeRouteLeave: vi.fn()
}))

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mocks.toastAdd })
}))

vi.mock('../../api/me.js', () => ({
  createPlacementRequest: mocks.createPlacementRequest,
  getMyProfile: mocks.getMyProfile
}))

vi.mock('../../api/job-positions.js', () => ({
  getJobPositionById: mocks.getJobPositionById
}))

import JobPositionApplyView from './JobPositionApplyView.vue'

describe('JobPositionApplyView (CR-03 saving guard)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.getJobPositionById.mockResolvedValue({
      id: 1,
      companyId: 2,
      isOpen: true,
      isExpired: false,
      deadline: '2099-12-31T23:59:59Z'
    })
    mocks.getMyProfile.mockResolvedValue({
      fullName: 'Nguyễn Văn A',
      email: 'student@example.com',
      phoneNumber: '0901234567',
      major: 'Công nghệ thông tin'
    })
  })

  async function instantiate() {
    const vm = JobPositionApplyView.setup({}, {
      expose: vi.fn(),
      emit: vi.fn(),
      attrs: {},
      slots: {}
    })
    // Allow loadData microtasks to settle
    await new Promise((resolve) => setTimeout(resolve, 0))
    vm.position.value = {
      id: 1,
      companyId: 2,
      isOpen: true,
      isExpired: false,
      deadline: '2099-12-31T23:59:59Z'
    }
    return vm
  }

  it('resets saving flag to false when client validation produces invalid fields', async () => {
    const vm = await instantiate()
    vm.form.value = {
      applicantFullName: '',
      applicantEmail: '',
      applicantPhone: '',
      applicantSchool: '',
      applicantMajor: '',
      cvUrl: '',
      coverLetter: ''
    }

    expect(vm.saving.value).toBe(false)
    await vm.submit()
    expect(vm.saving.value).toBe(false)
    expect(vm.invalidFieldCount.value).toBeGreaterThan(0)
    expect(mocks.createPlacementRequest).not.toHaveBeenCalled()
  })

  it('resets saving flag to false even if client validation throws an unexpected exception', async () => {
    const vm = await instantiate()
    // A faulty form property getter throws during validation
    vm.form.value = {
      get applicantFullName() {
        throw new Error('Unexpected client validation failure')
      }
    }

    expect(vm.saving.value).toBe(false)
    await vm.submit()
    // CR-03: try...finally wraps the full submit body, ensuring saving is restored to false
    expect(vm.saving.value).toBe(false)
    expect(mocks.createPlacementRequest).not.toHaveBeenCalled()
  })

  it('resets saving flag to false when submission succeeds', async () => {
    const vm = await instantiate()
    vm.form.value = {
      applicantFullName: 'Nguyễn Văn A',
      applicantEmail: 'student@example.com',
      applicantPhone: '0901234567',
      applicantSchool: 'Đại học Bách Khoa',
      applicantMajor: 'Công nghệ thông tin',
      cvUrl: 'https://example.com/cv.pdf',
      coverLetter: 'Kính gửi quý công ty, tôi mong muốn thực tập tại vị trí này.'
    }
    mocks.createPlacementRequest.mockResolvedValueOnce({ id: 99 })

    expect(vm.saving.value).toBe(false)
    await vm.submit()
    expect(mocks.createPlacementRequest).toHaveBeenCalledTimes(1)
    expect(vm.submitted.value).toBe(true)
    expect(vm.saving.value).toBe(false)
  })

  it('resets saving flag to false when server request rejects with an error', async () => {
    const vm = await instantiate()
    vm.form.value = {
      applicantFullName: 'Nguyễn Văn A',
      applicantEmail: 'student@example.com',
      applicantPhone: '0901234567',
      applicantSchool: 'Đại học Bách Khoa',
      applicantMajor: 'Công nghệ thông tin',
      cvUrl: 'https://example.com/cv.pdf',
      coverLetter: 'Kính gửi quý công ty, tôi mong muốn thực tập tại vị trí này.'
    }
    mocks.createPlacementRequest.mockRejectedValueOnce({
      response: {
        status: 400,
        data: { message: 'Đã nộp đơn trước đó.' }
      }
    })

    expect(vm.saving.value).toBe(false)
    await vm.submit()
    expect(mocks.createPlacementRequest).toHaveBeenCalledTimes(1)
    expect(vm.submitted.value).toBe(false)
    expect(vm.saving.value).toBe(false)
    expect(vm.formError.value).toBe('Đã nộp đơn trước đó.')
  })

  it('safely trims only string values and preserves non-string attributes on submit (P4-02)', async () => {
    const vm = await instantiate()
    vm.form.value = {
      applicantFullName: '  Nguyễn Văn SafeTrim  ',
      applicantEmail: 'safetrim@example.com',
      applicantPhone: '0901234567',
      applicantSchool: 'Đại học Bách Khoa',
      applicantMajor: 'CNTT',
      cvUrl: 'https://example.com/cv.pdf',
      coverLetter: 'Kính gửi quý công ty, tôi mong muốn thực tập tại vị trí này.',
      customNull: null,
      customUndefined: undefined,
      customNumber: 42,
      customBool: false
    }
    mocks.createPlacementRequest.mockResolvedValueOnce({ id: 101 })

    await vm.submit()
    expect(vm.saving.value).toBe(false)
    expect(mocks.createPlacementRequest).toHaveBeenCalledTimes(1)
    expect(mocks.createPlacementRequest).toHaveBeenCalledWith(expect.objectContaining({
      applicantFullName: 'Nguyễn Văn SafeTrim',
      customNull: null,
      customUndefined: undefined,
      customNumber: 42,
      customBool: false
    }))
  })
})
