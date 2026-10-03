import { validateEmailAddress, validateSafeText } from './emailValidation.js'

export const LIMITS = {
  fullName: [2, 100],
  school: [2, 150],
  major: [2, 150],
  coverLetter: [20, 2000],
  cvUrl: 500,
  email: 254
}

export function resolveReturnPath(from) {
  if (from === 'job-positions') return '/job-positions'
  if (from === 'companies') return '/companies'
  return '/profile'
}

const FIELD_LABELS = {
  applicantFullName: 'Họ tên',
  applicantEmail: 'Email ứng viên',
  applicantPhone: 'Số điện thoại',
  applicantSchool: 'Trường học',
  applicantMajor: 'Ngành học',
  cvUrl: 'Liên kết CV',
  coverLetter: 'Lời giới thiệu'
}

function trimmed(value) {
  return typeof value === 'string' ? value.trim() : ''
}

function hasLength(value, [minimum, maximum]) {
  return value.length >= minimum && value.length <= maximum
}

function validCvUrl(value) {
  if (value.length > LIMITS.cvUrl || /\s/.test(value)) return false

  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

export function validateApplication(form) {
  const errors = {}
  const fullName = trimmed(form?.applicantFullName)
  const email = trimmed(form?.applicantEmail)
  const phone = trimmed(form?.applicantPhone)
  const school = trimmed(form?.applicantSchool)
  const major = trimmed(form?.applicantMajor)
  const cvUrl = trimmed(form?.cvUrl)
  const coverLetter = trimmed(form?.coverLetter)

  const stringChecks = [
    ['applicantFullName', form?.applicantFullName, LIMITS.fullName[1], false],
    ['applicantPhone', form?.applicantPhone, 10, false],
    ['applicantSchool', form?.applicantSchool, LIMITS.school[1], false],
    ['applicantMajor', form?.applicantMajor, LIMITS.major[1], false],
    ['cvUrl', form?.cvUrl, LIMITS.cvUrl, false],
    ['coverLetter', form?.coverLetter, LIMITS.coverLetter[1], true]
  ]
  for (const [field, value, maximumLength, allowLineBreaks] of stringChecks) {
    const error = validateSafeText(value, maximumLength, allowLineBreaks)
    if (error) errors[field] = error
  }

  if (!fullName) errors.applicantFullName = 'Họ tên là bắt buộc.'
  else if (!hasLength(fullName, LIMITS.fullName)) errors.applicantFullName = 'Họ tên phải từ 2 đến 100 ký tự.'

  if (!email) errors.applicantEmail = 'Email ứng viên là bắt buộc.'
  else {
    const emailError = validateEmailAddress(form?.applicantEmail, LIMITS.email)
    if (emailError) errors.applicantEmail = emailError
  }

  if (!phone) errors.applicantPhone = 'Số điện thoại ứng viên là bắt buộc.'
  else if (!/^0\d{9}$/.test(phone)) errors.applicantPhone = 'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.'

  if (!school) errors.applicantSchool = 'Trường học là bắt buộc.'
  else if (!hasLength(school, LIMITS.school)) errors.applicantSchool = 'Trường học phải từ 2 đến 150 ký tự.'

  if (!major) errors.applicantMajor = 'Ngành học là bắt buộc.'
  else if (!hasLength(major, LIMITS.major)) errors.applicantMajor = 'Ngành học phải từ 2 đến 150 ký tự.'

  if (!cvUrl) errors.cvUrl = 'Liên kết CV là bắt buộc.'
  else if (!validCvUrl(cvUrl)) errors.cvUrl = 'Liên kết CV phải là URL http:// hoặc https: hợp lệ, tối đa 500 ký tự và không chứa khoảng trắng.'

  if (!coverLetter) errors.coverLetter = 'Lời giới thiệu là bắt buộc.'
  else if (!hasLength(coverLetter, LIMITS.coverLetter)) errors.coverLetter = 'Lời giới thiệu phải từ 20 đến 2000 ký tự.'

  return errors
}

function toCamelCase(field) {
  const normalized = String(field).replace(/^\$\.?/, '').replace(/^\./, '')
  return normalized ? normalized[0].toLowerCase() + normalized.slice(1) : '_form'
}

export function mapServerErrors(err) {
  const errors = {}
  const serverErrors = err?.response?.data?.errors

  if (serverErrors && typeof serverErrors === 'object' && !Array.isArray(serverErrors)) {
    for (const [serverField, messages] of Object.entries(serverErrors)) {
      const field = toCamelCase(serverField)
      const key = Object.hasOwn(FIELD_LABELS, field) ? field : '_form'
      const messageList = Array.isArray(messages) ? messages : [messages]
      const message = messageList.filter(Boolean).map(String).join(' ')
      if (!message) continue
      errors[key] = errors[key] ? `${errors[key]} ${message}` : message
    }
  } else if (Array.isArray(serverErrors) && serverErrors.length > 0) {
    errors._form = serverErrors.map(String).join(' ')
  }

  if (Object.keys(errors).length === 0) {
    const message = err?.response?.data?.message
    if (message) errors._form = String(message)
  }

  return errors
}