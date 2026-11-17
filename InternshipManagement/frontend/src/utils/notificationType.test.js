import { describe, expect, it } from 'vitest'
import {
  DEFAULT_NOTIFICATION_TYPE,
  NOTIFICATION_TYPE_MAP,
  getNotificationTypeMeta
} from './notificationType.js'

describe('notificationType util', () => {
  it('maps string NotificationType names correctly', () => {
    expect(getNotificationTypeMeta('StudentReportedRejection')).toEqual({
      label: 'Sinh viên báo bị doanh nghiệp từ chối',
      icon: 'pi pi-times-circle',
      tone: 'danger'
    })
    expect(getNotificationTypeMeta('StudentRevertedStage')).toEqual({
      label: 'Sinh viên bị lùi giai đoạn',
      icon: 'pi pi-replay',
      tone: 'warning'
    })
    expect(getNotificationTypeMeta('StudentAcceptedByCompany')).toEqual({
      label: 'Sinh viên được doanh nghiệp nhận',
      icon: 'pi pi-check-circle',
      tone: 'success'
    })
  })

  it('maps numeric NotificationType values correctly', () => {
    expect(getNotificationTypeMeta(0).label).toBe('Sinh viên báo bị doanh nghiệp từ chối')
    expect(getNotificationTypeMeta(1).label).toBe('Sinh viên bị lùi giai đoạn')
    expect(getNotificationTypeMeta(2).label).toBe('Sinh viên được doanh nghiệp nhận')
    expect(getNotificationTypeMeta('0').label).toBe('Sinh viên báo bị doanh nghiệp từ chối')
    expect(getNotificationTypeMeta('1').label).toBe('Sinh viên bị lùi giai đoạn')
    expect(getNotificationTypeMeta('2').label).toBe('Sinh viên được doanh nghiệp nhận')
  })

  it('falls back to default for null, undefined, or unknown values', () => {
    expect(getNotificationTypeMeta(null)).toEqual(DEFAULT_NOTIFICATION_TYPE)
    expect(getNotificationTypeMeta(undefined)).toEqual(DEFAULT_NOTIFICATION_TYPE)
    expect(getNotificationTypeMeta('UnknownType')).toEqual(DEFAULT_NOTIFICATION_TYPE)
    expect(getNotificationTypeMeta(999)).toEqual(DEFAULT_NOTIFICATION_TYPE)
    expect(getNotificationTypeMeta('')).toEqual(DEFAULT_NOTIFICATION_TYPE)
  })

  it('exposes NOTIFICATION_TYPE_MAP with all known types', () => {
    expect(NOTIFICATION_TYPE_MAP.StudentReportedRejection.tone).toBe('danger')
    expect(NOTIFICATION_TYPE_MAP.StudentRevertedStage.tone).toBe('warning')
    expect(NOTIFICATION_TYPE_MAP.StudentAcceptedByCompany.tone).toBe('success')
  })
})
