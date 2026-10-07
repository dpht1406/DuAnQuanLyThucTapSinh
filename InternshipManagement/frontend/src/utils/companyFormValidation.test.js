import { describe, expect, it } from 'vitest'
import { LIMITS, mapCompanyServerErrors, validateCompanyForm } from './companyFormValidation.js'

const validForm = {
  name: 'FPT Software',
  address: 'Số 10, Đường D1, Quận 1, TP.HCM',
  industry: 'Công nghệ thông tin',
  contactPerson: 'Nguyễn Văn A',
  contactPhone: '0912345678',
  contactEmail: 'lienhe@congty.com',
  contactPosition: 'Trưởng phòng nhân sự'
}

describe('validateCompanyForm', () => {
  it('reports only the required fields when all required fields are empty', () => {
    const errors = validateCompanyForm({
      name: '',
      address: '',
      industry: '',
      contactPerson: '',
      contactPhone: '',
      contactEmail: '',
      contactPosition: ''
    })

    expect(errors).toMatchObject({
      name: 'Tên doanh nghiệp không được để trống',
      address: 'Địa chỉ không được để trống',
      industry: 'Ngành không được để trống',
      contactPerson: 'Người liên hệ không được để trống'
    })
    expect(errors.contactPhone).toBeUndefined()
    expect(errors.contactEmail).toBeUndefined()
    expect(errors.contactPosition).toBeUndefined()
  })

  it('accepts a valid form with all required fields filled', () => {
    expect(validateCompanyForm(validForm)).toEqual({})
  })

  it('accepts a valid form with only the required fields', () => {
    expect(validateCompanyForm({
      name: 'Công ty ABC',
      address: '123 Lê Lợi',
      industry: 'Kinh doanh',
      contactPerson: 'Trần Thị B'
    })).toEqual({})
  })

  it('accepts valid boundary lengths for all fields', () => {
    expect(validateCompanyForm({
      ...validForm,
      name: 'A'.repeat(LIMITS.name),
      address: 'A'.repeat(LIMITS.address),
      industry: 'A'.repeat(LIMITS.industry),
      contactPerson: 'A'.repeat(LIMITS.contactPerson),
      contactPosition: 'A'.repeat(LIMITS.contactPosition)
    })).toEqual({})
  })

  it('rejects over-limit values', () => {
    const errors = validateCompanyForm({
      ...validForm,
      name: 'A'.repeat(LIMITS.name + 1),
      address: 'A'.repeat(LIMITS.address + 1),
      industry: 'A'.repeat(LIMITS.industry + 1),
      contactPerson: 'A'.repeat(LIMITS.contactPerson + 1),
      contactPosition: 'A'.repeat(LIMITS.contactPosition + 1)
    })

    expect(errors.name).toBeTruthy()
    expect(errors.address).toBeTruthy()
    expect(errors.industry).toBeTruthy()
    expect(errors.contactPerson).toBeTruthy()
    expect(errors.contactPosition).toBeTruthy()
  })

  it('validates phone numbers', () => {
    expect(validateCompanyForm({ ...validForm, contactPhone: '091234567' }).contactPhone).toBe('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0')
    expect(validateCompanyForm({ ...validForm, contactPhone: '09123456789' }).contactPhone).toBe('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0')
    expect(validateCompanyForm({ ...validForm, contactPhone: '1234567890' }).contactPhone).toBe('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0')
    expect(validateCompanyForm({ ...validForm, contactPhone: '0912a34567' }).contactPhone).toBe('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0')
    expect(validateCompanyForm({ ...validForm, contactPhone: '0912 345 678' }).contactPhone).toBe('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0')
    expect(validateCompanyForm({ ...validForm, contactPhone: '' })).toEqual({})
  })

  it('validates email addresses', () => {
    expect(validateCompanyForm({ ...validForm, contactEmail: 'lienhe@congty' }).contactEmail).toBe('Email chưa đúng định dạng')
    expect(validateCompanyForm({ ...validForm, contactEmail: 'lienhecongty.com' }).contactEmail).toBe('Email chưa đúng định dạng')
    expect(validateCompanyForm({ ...validForm, contactEmail: 'lienhe @congty.com' }).contactEmail).toBe('Email chưa đúng định dạng')
    expect(validateCompanyForm({ ...validForm, contactEmail: 'a'.repeat(151) + '@example.com' }).contactEmail).toBe('Email chưa đúng định dạng')
    expect(validateCompanyForm({ ...validForm, contactEmail: '' })).toEqual({})
  })

  it('rejects control characters', () => {
    const errors = validateCompanyForm({
      ...validForm,
      name: 'FPT\nSoftware'
    })

    expect(errors.name).toBeTruthy()
  })

  it('maps server validation messages to the correct fields', () => {
    expect(mapCompanyServerErrors({
      response: { data: { message: 'Số điện thoại người tuyển dụng phải gồm 10 chữ số và bắt đầu bằng số 0.' } }
    })).toEqual({ contactPhone: 'Số điện thoại người tuyển dụng phải gồm 10 chữ số và bắt đầu bằng số 0.' })
  })
})
