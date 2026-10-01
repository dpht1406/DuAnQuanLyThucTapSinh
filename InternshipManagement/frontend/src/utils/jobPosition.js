import dayjs from './dayjs.js'

export function formatDeadline(deadline) {
  return deadline ? dayjs(deadline).format('DD/MM/YYYY') : 'Không giới hạn'
}

export function daysLeft(deadline) {
  if (!deadline) return null
  return dayjs(deadline).startOf('day').diff(dayjs().startOf('day'), 'day')
}

export function getPositionStatus(position) {
  if (position?.isExpired) return { key: 'expired', label: 'Đã hết hạn', severity: 'danger' }
  if (!position?.isOpen) return { key: 'closed', label: 'Đã đóng', severity: 'secondary' }
  if (daysLeft(position?.deadline) !== null && daysLeft(position.deadline) <= 3) {
    return { key: 'closing-soon', label: 'Sắp hết hạn', severity: 'warn' }
  }
  return { key: 'open', label: 'Đang tuyển', severity: 'success' }
}

export function canApply(position) {
  return position?.isOpen === true && position?.isExpired !== true
}