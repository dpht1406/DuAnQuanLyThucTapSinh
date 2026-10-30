import { parseServerUtc } from './dateTime.js'

export const PLACEMENT_REQUEST_STATUSES = Object.freeze([
  { value: 0, label: 'Chờ xử lý', severity: 'warn', key: 'Pending' },
  { value: 3, label: 'Đã mời phỏng vấn', severity: 'info', key: 'InterviewInvited' },
  { value: 4, label: 'Đã nhận', severity: 'success', key: 'Accepted' },
  { value: 2, label: 'Đã từ chối', severity: 'danger', key: 'Rejected' },
  { value: 5, label: 'Quá hạn', severity: 'secondary', key: 'Expired' }
])

const statusByKey = new Map(PLACEMENT_REQUEST_STATUSES.map((status) => [status.key.toLowerCase(), status]))
const statusByValue = new Map(PLACEMENT_REQUEST_STATUSES.map((status) => [status.value, status]))
const MAX_NOTE_LENGTH = 1000

export function mapPlacementRequestStatus(status) {
  const numericStatus = typeof status === 'number'
    ? status
    : typeof status === 'string' && status.trim() !== ''
      ? Number(status)
      : Number.NaN
  if (Number.isInteger(numericStatus) && statusByValue.has(numericStatus)) {
    return statusByValue.get(numericStatus)
  }
  if (typeof status === 'string') {
    return statusByKey.get(status.trim().toLowerCase()) ?? {
      value: null,
      label: 'Không xác định',
      severity: 'secondary',
      key: 'Unknown'
    }
  }
  return { value: null, label: 'Không xác định', severity: 'secondary', key: 'Unknown' }
}

export function mapPlacementRequestDueStatus(dueAt, now = new Date()) {
  const dueDate = parseServerUtc(dueAt)
  if (!dueDate) return 'none'
  const timeUntilDue = dueDate.valueOf() - now.valueOf()
  if (timeUntilDue <= 0) return 'overdue'
  if (timeUntilDue <= 48 * 60 * 60 * 1000) return 'soon'
  return 'normal'
}

export function validateCompanyInterviewForm(form = {}, now = new Date()) {
  const errors = {}
  const interviewDate = typeof form.interviewAt === 'string' && form.interviewAt
    ? new Date(form.interviewAt)
    : null
  if (!interviewDate || Number.isNaN(interviewDate.valueOf()) || interviewDate <= now) {
    errors.interviewAt = 'Thời gian phỏng vấn phải ở tương lai.'
  }

  const location = typeof form.interviewLocation === 'string' ? form.interviewLocation.trim() : ''
  if (!location) errors.interviewLocation = 'Địa điểm phỏng vấn là bắt buộc.'
  else if (location.length > 500) errors.interviewLocation = 'Địa điểm phỏng vấn không được vượt quá 500 ký tự.'

  const note = typeof form.interviewNote === 'string' ? form.interviewNote.trim() : ''
  if (note.length > MAX_NOTE_LENGTH) {
    errors.interviewNote = 'Ghi chú phỏng vấn không được vượt quá 1000 ký tự.'
  }
  return errors
}

export function validateCompanyRejectReason(reason) {
  const value = typeof reason === 'string' ? reason.trim() : ''
  if (!value) return 'Lý do từ chối là bắt buộc.'
  if (value.length > MAX_NOTE_LENGTH) return 'Lý do từ chối không được vượt quá 1000 ký tự.'
  return ''
}

export function validateCompanyPlacementResult(isPassed, note) {
  const value = typeof note === 'string' ? note.trim() : ''
  if (value.length > MAX_NOTE_LENGTH) return 'Ghi chú kết quả không được vượt quá 1000 ký tự.'
  if (!isPassed && !value) return 'Ghi chú là bắt buộc khi sinh viên không đạt.'
  return ''
}

export function getEmbedCvUrl(url) {
  if (!url || typeof url !== 'string') return ''
  const trimmed = url.trim()
  const driveFileRegex = /https?:\/\/drive\.google\.com\/file\/d\/([^/?#]+)(?:\/[^?#]*)?(?:\?[^#]*)?(?:#.*)?/i
  const match = trimmed.match(driveFileRegex)
  if (match && match[1]) {
    return `https://drive.google.com/file/d/${match[1]}/preview`
  }
  const driveIdRegex = /https?:\/\/drive\.google\.com\/(?:open|uc)\?id=([^&#]+)/i
  const idMatch = trimmed.match(driveIdRegex)
  if (idMatch && idMatch[1]) {
    return `https://drive.google.com/file/d/${idMatch[1]}/preview`
  }
  return trimmed
}
