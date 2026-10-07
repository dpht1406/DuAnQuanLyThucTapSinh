import { describe, expect, it } from 'vitest'
import { mapStudentServerErrors, validateStudentEditForm } from './studentEditValidation.js'

const validForm = {
  FullName: 'Nguyễn Văn A',
  Email: 'student@gmail.com',
  PhoneNumber: '0912345678',
  Major: 'Công nghệ thông tin',
  ClassName: 'CNTT2023A'
}

describe('validateStudentEditForm', () => {
  it('accepts a valid form', () => {
    expect(validateStudentEditForm(validForm)).toEqual({})
  })

  it('reports every required field when empty', () => {
    expect(validateStudentEditForm({
      FullName: '',
      Email: '',
      PhoneNumber: '',
      Major: '',
      ClassName: ''
    })).toEqual({
      FullName: 'Họ tên không được để trống',
      Email: 'Email không được để trống',
      PhoneNumber: 'Số điện thoại không được để trống',
      Major: 'Ngành không được để trống',
      ClassName: 'Lớp không được để trống'
    })
  })

  it.each([
    ['FullName', 'Họ tên không được để trống'],
    ['Email', 'Email không được để trống'],
    ['PhoneNumber', 'Số điện thoại không được để trống'],
    ['Major', 'Ngành không được để trống'],
    ['ClassName', 'Lớp không được để trống']
  ])('requires %s when it contains only whitespace', (field, message) => {
    expect(validateStudentEditForm({ ...validForm, [field]: '  ' })[field]).toBe(message)
  })

  it('trims values before validating them', () => {
    expect(validateStudentEditForm({
      FullName: '  Nguyễn Văn A  ',
      Email: '  student@gmail.com  ',
      PhoneNumber: '  0912345678  ',
      Major: '  Công nghệ thông tin  ',
      ClassName: '  CNTT2023A  '
    })).toEqual({})
  })

  it('rejects malformed email addresses and preserves typo suggestions', () => {
    expect(validateStudentEditForm({ ...validForm, Email: 'student-at-example' }).Email)
      .toBe('Email chưa đúng định dạng')
    expect(validateStudentEditForm({ ...validForm, Email: 'student@gmail.con' }).Email)
      .toContain('Email có vẻ gõ sai')
  })

  it.each([
    ['9 digits', '091234567'],
    ['11 digits', '09123456789'],
    ['not starting with zero', '1912345678'],
    ['letters', '09123a5678']
  ])('rejects phone numbers with %s', (_caseName, PhoneNumber) => {
    expect(validateStudentEditForm({ ...validForm, PhoneNumber }).PhoneNumber)
      .toBe('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0')
  })

  it('rejects over-limit full names and class names', () => {
    expect(validateStudentEditForm({ ...validForm, FullName: 'A'.repeat(151) }).FullName)
      .toBe('Không được vượt quá 150 ký tự.')
    expect(validateStudentEditForm({ ...validForm, ClassName: 'A'.repeat(51) }).ClassName)
      .toBe('Không được vượt quá 50 ký tự.')
  })

  it('rejects control characters', () => {
    expect(validateStudentEditForm({ ...validForm, FullName: 'Nguyễn\nVăn A' }).FullName)
      .toBe('Không được chứa ký tự điều khiển không hợp lệ.')
  })
})

describe('mapStudentServerErrors', () => {
  it('maps case-insensitive field names and joins array messages', () => {
    expect(mapStudentServerErrors({
      response: {
        data: {
          errors: {
            FULLNAME: ['Tên không hợp lệ.', 'Vui lòng kiểm tra lại.'],
            phonenumber: ['Số điện thoại sai.'],
            major: 'Ngành không hợp lệ.'
          }
        }
      }
    })).toEqual({
      FullName: 'Tên không hợp lệ. Vui lòng kiểm tra lại.',
      PhoneNumber: 'Số điện thoại sai.',
      Major: 'Ngành không hợp lệ.'
    })
  })

  it('returns an empty object when there are no recognized field errors', () => {
    expect(mapStudentServerErrors({ response: { data: { message: 'Server error' } } })).toEqual({})
    expect(mapStudentServerErrors({
      response: { data: { errors: { studentcode: ['MSSV không hợp lệ.'] } } }
    })).toEqual({})
  })
})
