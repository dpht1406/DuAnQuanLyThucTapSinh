export const NOTIFICATION_TYPE_MAP = {
  StudentReportedRejection: {
    label: 'Sinh viên báo bị doanh nghiệp từ chối',
    icon: 'pi pi-times-circle',
    tone: 'danger'
  },
  StudentRevertedStage: {
    label: 'Sinh viên bị lùi giai đoạn',
    icon: 'pi pi-replay',
    tone: 'warning'
  },
  StudentAcceptedByCompany: {
    label: 'Sinh viên được doanh nghiệp nhận',
    icon: 'pi pi-check-circle',
    tone: 'success'
  }
}

export const DEFAULT_NOTIFICATION_TYPE = {
  label: 'Thông báo',
  icon: 'pi pi-bell',
  tone: 'primary'
}

const NUMERIC_TO_KEY = {
  0: 'StudentReportedRejection',
  1: 'StudentRevertedStage',
  2: 'StudentAcceptedByCompany'
}

export function getNotificationTypeMeta(type) {
  if (type === null || type === undefined) {
    return DEFAULT_NOTIFICATION_TYPE
  }

  if (typeof type === 'string' && NOTIFICATION_TYPE_MAP[type]) {
    return NOTIFICATION_TYPE_MAP[type]
  }

  const num = Number(type)
  if (!Number.isNaN(num) && Object.prototype.hasOwnProperty.call(NUMERIC_TO_KEY, num)) {
    const key = NUMERIC_TO_KEY[num]
    return NOTIFICATION_TYPE_MAP[key]
  }

  return DEFAULT_NOTIFICATION_TYPE
}
