import { validateSafeText } from './emailValidation.js'

export const COMPANY_JOB_POSITION_LIMITS = Object.freeze({
  title: 200,
  department: 100,
  location: 300,
  description: 2000
})

const fields = [
  ['title', 'Tiêu đề', COMPANY_JOB_POSITION_LIMITS.title, false],
  ['department', 'Phòng ban', COMPANY_JOB_POSITION_LIMITS.department, false],
  ['location', 'Địa điểm', COMPANY_JOB_POSITION_LIMITS.location, false],
  ['description', 'Mô tả', COMPANY_JOB_POSITION_LIMITS.description, true]
]

export function validateCompanyJobPosition(form = {}, { isCreate = false, today = new Date() } = {}) {
  const errors = {}
  const title = typeof form.title === 'string' ? form.title.trim() : ''

  if (!title) errors.title = 'Tiêu đề (Title) là bắt buộc.'

  for (const [field, label, limit, allowLineBreaks] of fields) {
    const value = typeof form[field] === 'string' ? form[field] : ''
    const error = validateSafeText(value, limit, allowLineBreaks)
    if (error) errors[field] = `${label}: ${error}`
  }

  const quantity = Number(form.quantity)
  if (!Number.isInteger(quantity) || quantity <= 0) {
    errors.quantity = 'Số lượng tuyển (Quantity) phải là số nguyên lớn hơn 0.'
  }

  if (isCreate && form.deadline) {
    const dateParts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(form.deadline)
    const deadline = dateParts
      ? new Date(Number(dateParts[1]), Number(dateParts[2]) - 1, Number(dateParts[3]))
      : new Date(Number.NaN)
    const todayDate = new Date(today.getFullYear(), today.getMonth(), today.getDate())
    if (Number.isNaN(deadline.getTime()) ||
        deadline.getFullYear() !== Number(dateParts?.[1]) ||
        deadline.getMonth() + 1 !== Number(dateParts?.[2]) ||
        deadline.getDate() !== Number(dateParts?.[3])) {
      errors.deadline = 'Hạn nộp hồ sơ không hợp lệ.'
    } else if (deadline < todayDate) {
      errors.deadline = 'Hạn nộp hồ sơ không được trước ngày hôm nay.'
    }
  }

  return errors
}
