import { validateEmailAddress, validateSafeText } from './emailValidation.js'

export const LIMITS = {
  name: 200,
  address: 300,
  industry: 150,
  contactPerson: 150,
  contactPhone: 20,
  contactEmail: 150,
  contactPosition: 100
}

const FIELD_ALIASES = {
  name: 'name',
  'company name': 'name',
  'tên công ty': 'name',
  'ten cong ty': 'name',
  address: 'address',
  'địa chỉ': 'address',
  'dia chi': 'address',
  industry: 'industry',
  'ngành': 'industry',
  'nganh': 'industry',
  'lĩnh vực': 'industry',
  'linh vuc': 'industry',
  contactperson: 'contactPerson',
  'người liên hệ': 'contactPerson',
  'nguoi lien he': 'contactPerson',
  contactpersonname: 'contactPerson',
  contactphone: 'contactPhone',
  'số điện thoại': 'contactPhone',
  'so dien thoai': 'contactPhone',
  contactemail: 'contactEmail',
  email: 'contactEmail',
  contactposition: 'contactPosition',
  'chức vụ': 'contactPosition',
  'chuc vu': 'contactPosition'
}

function trimmed(value) {
  return typeof value === 'string' ? value.trim() : ''
}

function emptyForm() {
  return {
    name: '',
    address: '',
    industry: '',
    contactPerson: '',
    contactPhone: '',
    contactEmail: '',
    contactPosition: ''
  }
}

export function validateCompanyForm(form = emptyForm()) {
  const values = { ...emptyForm(), ...form }
  const errors = {}

  const requiredFields = [
    ['name', 'Tên doanh nghiệp không được để trống'],
    ['address', 'Địa chỉ không được để trống'],
    ['industry', 'Ngành không được để trống'],
    ['contactPerson', 'Người liên hệ không được để trống'],
    ['contactEmail', 'Email liên hệ không được để trống']
  ]

  for (const [field, message] of requiredFields) {
    const value = trimmed(values[field])
    if (!value) {
      errors[field] = message
      continue
    }

    if (field === 'contactEmail') {
      const emailError = validateEmailAddress(values[field], LIMITS.contactEmail)
      if (emailError) {
        errors[field] = emailError.startsWith('Email có vẻ gõ sai') ? emailError : 'Email chưa đúng định dạng'
      }
      continue
    }

    const safeTextError = validateSafeText(values[field], LIMITS[field])
    if (safeTextError) errors[field] = safeTextError
  }

  const optionalFields = [
    ['contactPhone', 'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0'],
    ['contactPosition', '']
  ]

  for (const [field, defaultMessage] of optionalFields) {
    const value = trimmed(values[field])
    if (!value) continue

    const fieldLimit = LIMITS[field]

    const safeTextError = validateSafeText(values[field], fieldLimit)
    if (safeTextError) {
      errors[field] = safeTextError
      continue
    }

    if (field === 'contactPhone') {
      if (!/^0\d{9}$/.test(value)) errors[field] = defaultMessage
    }
  }


  return errors
}

function normalizeFieldKey(rawKey) {
  const key = String(rawKey ?? '').trim().toLowerCase()
  return FIELD_ALIASES[key] || key
}

function mapMessageToField(message) {
  const normalized = String(message ?? '').toLowerCase()
  if (!normalized) return null

  if (normalized.includes('số điện thoại') || normalized.includes('so dien thoai')) return 'contactPhone'
  if (normalized.includes('email')) return 'contactEmail'
  if (normalized.includes('người liên hệ') || normalized.includes('nguoi lien he')) return 'contactPerson'
  if (normalized.includes('địa chỉ') || normalized.includes('dia chi')) return 'address'
  if (normalized.includes('tên công ty') || normalized.includes('ten cong ty')) return 'name'
  if (normalized.includes('ngành') || normalized.includes('nganh') || normalized.includes('lĩnh vực') || normalized.includes('linh vuc')) return 'industry'
  if (normalized.includes('chức vụ') || normalized.includes('chuc vu')) return 'contactPosition'
  return null
}

export function mapCompanyServerErrors(err) {
  const result = {}
  const serverErrors = err?.response?.data?.errors

  if (serverErrors && typeof serverErrors === 'object' && !Array.isArray(serverErrors)) {
    for (const [field, value] of Object.entries(serverErrors)) {
      const alias = normalizeFieldKey(field)
      const candidates = Array.isArray(value) ? value : [value]
      const message = candidates.filter(Boolean).map(String).join(' ')
      if (!message) continue
      const target = alias in { name: true, address: true, industry: true, contactPerson: true, contactPhone: true, contactEmail: true, contactPosition: true }
        ? alias
        : null
      if (target) result[target] = message
    }
  }

  if (Object.keys(result).length === 0) {
    const rawMessage = err?.response?.data?.message
    if (typeof rawMessage === 'string' && rawMessage.trim()) {
      const target = mapMessageToField(rawMessage)
      if (target) result[target] = rawMessage.trim()
      else result._form = rawMessage.trim()
    }
  }

  return result
}
