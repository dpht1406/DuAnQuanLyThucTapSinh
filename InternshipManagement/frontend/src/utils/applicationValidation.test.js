import { describe, expect, it } from 'vitest'
import { LIMITS, mapServerErrors, resolveReturnPath, validateApplication } from './applicationValidation.js'

const validForm = {
  applicantFullName: 'Nguyễn Văn A',
  applicantEmail: 'a@example.com',
  applicantPhone: '0901234567',
  applicantSchool: 'Đại học thử nghiệm',
  applicantMajor: 'Công nghệ thông tin',
  cvUrl: 'https://example.com/cv.pdf',
  coverLetter: 'Tôi mong muốn được tham gia thực tập.'
}

describe('validateApplication', () => {
  it('accepts a complete valid application', () => {
    expect(validateApplication(validForm)).toEqual({})
  })

  it.each([
    ['applicantFullName', 'Họ tên'],
    ['applicantEmail', 'Email'],
    ['applicantPhone', 'Số điện thoại'],
    ['applicantSchool', 'Trường'],
    ['applicantMajor', 'Ngành'],
    ['cvUrl', 'CV'],
    ['coverLetter', 'Lời giới thiệu']
  ])('requires %s when empty or whitespace-only', (field) => {
    for (const value of ['', '   ']) {
      const errors = validateApplication({ ...validForm, [field]: value })
      expect(errors[field]).toBeTruthy()
      expect(errors[field]).toMatch(/[À-ỹ]/)
    }
  })

  it.each([
    ['fullName-short', { applicantFullName: 'A' }, 'applicantFullName'],
    ['fullName-too-long', { applicantFullName: 'A'.repeat(101) }, 'applicantFullName'],
    ['email-malformed', { applicantEmail: 'abc' }, 'applicantEmail'],
    ['email-missing-domain', { applicantEmail: 'a@' }, 'applicantEmail'],
    ['email-contains-space', { applicantEmail: 'a b@c.com' }, 'applicantEmail'],
    ['email-too-long', { applicantEmail: `${'a'.repeat(243)}@example.com` }, 'applicantEmail'],
    ['phone-too-short', { applicantPhone: '0123' }, 'applicantPhone'],
    ['phone-not-leading-zero', { applicantPhone: '1234567890' }, 'applicantPhone'],
    ['phone-contains-letter', { applicantPhone: '012345678a' }, 'applicantPhone'],
    ['phone-too-long', { applicantPhone: '01234567890' }, 'applicantPhone'],
    ['cv-javascript', { cvUrl: 'javascript:alert(1)' }, 'cvUrl'],
    ['cv-data', { cvUrl: 'data:text/html,x' }, 'cvUrl'],
    ['cv-ftp', { cvUrl: 'ftp://x.com/cv' }, 'cvUrl'],
    ['cv-file', { cvUrl: 'file:///c:/cv.pdf' }, 'cvUrl'],
    ['cv-not-url', { cvUrl: 'not a url' }, 'cvUrl'],
    ['cv-space', { cvUrl: 'https://a.com/cv file.pdf' }, 'cvUrl'],
    ['cv-too-long', { cvUrl: `https://a.com/${'x'.repeat(487)}` }, 'cvUrl'],
    ['cover-letter-short', { coverLetter: 'x'.repeat(19) }, 'coverLetter'],
    ['cover-letter-too-long', { coverLetter: 'x'.repeat(2001) }, 'coverLetter']
  ])('rejects invalid case %s', (_name, values, field) => {
    expect(validateApplication({ ...validForm, ...values })[field]).toBeTruthy()
  })

  it.each([
    ['full name minimum', { applicantFullName: 'AB' }],
    ['full name maximum', { applicantFullName: 'A'.repeat(LIMITS.fullName[1]) }],
    ['school minimum', { applicantSchool: 'AB' }],
    ['school maximum', { applicantSchool: 'A'.repeat(LIMITS.school[1]) }],
    ['major minimum', { applicantMajor: 'AB' }],
    ['major maximum', { applicantMajor: 'A'.repeat(LIMITS.major[1]) }],
    ['email maximum', { applicantEmail: `${'a'.repeat(242)}@example.com` }],
    ['CV http', { cvUrl: 'http://x.com/cv' }],
    ['CV maximum', { cvUrl: `https://x.com/${'x'.repeat(486)}` }],
    ['CV https', { cvUrl: 'https://drive.google.com/file/d/abc/view' }],
    ['cover letter minimum', { coverLetter: 'x'.repeat(LIMITS.coverLetter[0]) }],
    ['cover letter maximum', { coverLetter: 'x'.repeat(LIMITS.coverLetter[1]) }]
  ])('accepts boundary case %s', (_name, values) => {
    expect(validateApplication({ ...validForm, ...values })).toEqual({})
  })
})

describe('mapServerErrors', () => {
  it('normalizes PascalCase server field names to camelCase', () => {
    expect(mapServerErrors({
      response: { data: { errors: { ApplicantFullName: ['Họ tên không hợp lệ.'] } } }
    })).toEqual({ applicantFullName: 'Họ tên không hợp lệ.' })
  })

  it('puts unassociated and unknown server errors in _form', () => {
    expect(mapServerErrors({
      response: { data: { errors: { General: ['Yêu cầu không hợp lệ.'] } } }
    })).toEqual({ _form: 'Yêu cầu không hợp lệ.' })
    expect(mapServerErrors({
      response: { data: { message: 'Bạn đã có một yêu cầu đang chờ duyệt.' } }
    })).toEqual({ _form: 'Bạn đã có một yêu cầu đang chờ duyệt.' })
  })
})

describe('resolveReturnPath', () => {
  it.each([
    ['job-positions', '/job-positions'],
    ['companies', '/companies'],
    ['//evil.com', '/profile'],
    ['https://example.com', '/profile'],
    ['unexpected-source', '/profile'],
    [['companies'], '/profile'],
    [undefined, '/profile']
  ])('allows only the known source %s', (from, expected) => {
    expect(resolveReturnPath(from)).toBe(expected)
  })
})