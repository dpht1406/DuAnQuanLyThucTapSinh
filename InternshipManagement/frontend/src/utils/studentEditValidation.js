import { validateEmailAddress, validateSafeText } from './emailValidation.js'

export const STUDENT_EDIT_LIMITS = {
  FullName: 150,
  Email: 150,
  PhoneNumber: 10,
  Major: 150,
  ClassName: 50
}

const SERVER_FIELD_NAMES = {
  fullname: 'FullName',
  email: 'Email',
  phonenumber: 'PhoneNumber',
  major: 'Major',
  classname: 'ClassName'
}

function trimmed(value) {
  return typeof value === 'string' ? value.trim() : ''
}

export function validateStudentEditForm(form = {}) {
  const errors = {}
  const values = {
    FullName: trimmed(form.FullName),
    Email: trimmed(form.Email),
    PhoneNumber: trimmed(form.PhoneNumber),
    Major: trimmed(form.Major),
    ClassName: trimmed(form.ClassName)
  }

  if (!values.FullName) {
    errors.FullName = 'Họ tên không được để trống'
  } else {
    const error = validateSafeText(values.FullName, STUDENT_EDIT_LIMITS.FullName)
    if (error) errors.FullName = error
  }

  if (!values.Email) {
    errors.Email = 'Email không được để trống'
  } else {
    const error = validateEmailAddress(values.Email, STUDENT_EDIT_LIMITS.Email)
    if (error) {
      errors.Email = error.startsWith('Email có vẻ gõ sai') ? error : 'Email chưa đúng định dạng'
    }
  }

  if (!values.PhoneNumber) {
    errors.PhoneNumber = 'Số điện thoại không được để trống'
  } else if (!/^0\d{9}$/.test(values.PhoneNumber)) {
    errors.PhoneNumber = 'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0'
  }

  if (!values.Major) {
    errors.Major = 'Ngành không được để trống'
  } else {
    const error = validateSafeText(values.Major, STUDENT_EDIT_LIMITS.Major)
    if (error) errors.Major = error
  }

  if (!values.ClassName) {
    errors.ClassName = 'Lớp không được để trống'
  } else {
    const error = validateSafeText(values.ClassName, STUDENT_EDIT_LIMITS.ClassName)
    if (error) errors.ClassName = error
  }

  return errors
}

export function mapStudentServerErrors(error) {
  const result = {}
  const serverErrors = error?.response?.data?.errors

  if (!serverErrors || typeof serverErrors !== 'object' || Array.isArray(serverErrors)) {
    return result
  }

  for (const [key, value] of Object.entries(serverErrors)) {
    const targetField = SERVER_FIELD_NAMES[key.trim().toLowerCase()]
    if (!targetField) continue

    const candidates = Array.isArray(value) ? value : [value]
    const message = candidates.filter(Boolean).map(String).join(' ').trim()
    if (message) result[targetField] = message
  }

  return result
}
