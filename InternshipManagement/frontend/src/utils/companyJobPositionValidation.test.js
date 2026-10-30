import { describe, expect, it } from 'vitest'
import { COMPANY_JOB_POSITION_LIMITS, validateCompanyJobPosition } from './companyJobPositionValidation.js'

const validForm = {
  title: 'Thực tập sinh .NET',
  department: 'Kỹ thuật',
  location: 'TP. Hồ Chí Minh',
  deadline: '2030-06-30',
  quantity: 2,
  description: 'Tham gia phát triển sản phẩm.\nHỗ trợ đội ngũ kỹ thuật.'
}

describe('validateCompanyJobPosition', () => {
  it('requires a title and a positive integer quantity', () => {
    expect(validateCompanyJobPosition({ ...validForm, title: '   ', quantity: 0 })).toMatchObject({
      title: 'Tiêu đề (Title) là bắt buộc.',
      quantity: 'Số lượng tuyển (Quantity) phải là số nguyên lớn hơn 0.'
    })
    expect(validateCompanyJobPosition({ ...validForm, quantity: 1.5 }).quantity).toBeTruthy()
  })

  it('accepts exact backend text limits and line breaks in descriptions', () => {
    expect(validateCompanyJobPosition({
      ...validForm,
      title: 'T'.repeat(COMPANY_JOB_POSITION_LIMITS.title),
      department: 'D'.repeat(COMPANY_JOB_POSITION_LIMITS.department),
      location: 'L'.repeat(COMPANY_JOB_POSITION_LIMITS.location),
      description: `D${'\n'}${'x'.repeat(COMPANY_JOB_POSITION_LIMITS.description - 2)}`
    })).toEqual({})
  })

  it('rejects text beyond backend limits and invalid control characters', () => {
    const errors = validateCompanyJobPosition({
      ...validForm,
      title: `${validForm.title}${'x'.repeat(COMPANY_JOB_POSITION_LIMITS.title)}`,
      department: 'D'.repeat(COMPANY_JOB_POSITION_LIMITS.department + 1),
      location: 'L'.repeat(COMPANY_JOB_POSITION_LIMITS.location + 1),
      description: `bad\u0000${'x'.repeat(COMPANY_JOB_POSITION_LIMITS.description)}`
    })

    expect(errors.title).toBeTruthy()
    expect(errors.department).toBeTruthy()
    expect(errors.location).toBeTruthy()
    expect(errors.description).toBeTruthy()
  })

  it('rejects a past deadline on create but permits it while editing', () => {
    const options = { today: new Date(2030, 5, 15) }
    expect(validateCompanyJobPosition({ ...validForm, deadline: '2030-06-14' }, { ...options, isCreate: true }).deadline)
      .toBe('Hạn nộp hồ sơ không được trước ngày hôm nay.')
    expect(validateCompanyJobPosition({ ...validForm, deadline: '2030-06-14' }, options).deadline).toBeUndefined()
  })

  it('accepts an empty optional deadline and rejects an invalid date on create', () => {
    expect(validateCompanyJobPosition({ ...validForm, deadline: '' }, { isCreate: true })).toEqual({})
    expect(validateCompanyJobPosition({ ...validForm, deadline: 'not-a-date' }, { isCreate: true }).deadline)
      .toBe('Hạn nộp hồ sơ không hợp lệ.')
    expect(validateCompanyJobPosition({ ...validForm, deadline: '2030-02-31' }, { isCreate: true }).deadline)
      .toBe('Hạn nộp hồ sơ không hợp lệ.')
  })
})
