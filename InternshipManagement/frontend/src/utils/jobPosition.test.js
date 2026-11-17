import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { canApply, daysLeft, formatDeadline, getPositionStatus } from './jobPosition.js'

// Cố định "hôm nay" = 26/10/2026 14:30 (giờ máy) để kết quả tái lập được.
const NOW = new Date(2026, 9, 26, 14, 30, 0)
const day = (offset, h = 0, m = 0) => new Date(2026, 9, 26 + offset, h, m, 0).toISOString()
const open = (deadline, extra = {}) => ({ isOpen: true, isExpired: false, deadline, ...extra })

beforeEach(() => {
  vi.useFakeTimers()
  vi.setSystemTime(NOW)
})
afterEach(() => vi.useRealTimers())

describe('daysLeft - tính số ngày còn lại theo ngày lịch', () => {
  it('TC-D01 không có hạn nộp -> null', () => {
    expect(daysLeft(null)).toBeNull()
    expect(daysLeft(undefined)).toBeNull()
  })
  it('TC-D02 hạn là hôm nay (00:00) -> 0', () => {
    expect(daysLeft(day(0))).toBe(0)
  })
  it('TC-D03 hạn là hôm nay nhưng giờ 23:59 -> vẫn 0 (không lệch theo giờ)', () => {
    expect(daysLeft(day(0, 23, 59))).toBe(0)
  })
  it('TC-D04 hạn là ngày mai -> 1', () => {
    expect(daysLeft(day(1))).toBe(1)
  })
  it('TC-D05 hạn hôm qua -> -1', () => {
    expect(daysLeft(day(-1))).toBe(-1)
  })
  it('TC-D06 hạn cách 66 ngày -> 66 (khớp màn hình thực tế "còn 66 ngày")', () => {
    expect(daysLeft(new Date(2026, 11, 31).toISOString())).toBe(66)
  })
})

describe('getPositionStatus - các mốc trước hạn / đúng hạn / quá hạn', () => {
  it('TC-S01 còn 4 ngày -> Đang tuyển (chưa cảnh báo)', () => {
    expect(getPositionStatus(open(day(4)))).toMatchObject({ key: 'open', label: 'Đang tuyển' })
  })
  it('TC-S02 còn đúng 3 ngày -> Sắp hết hạn (mốc bắt đầu cảnh báo)', () => {
    expect(getPositionStatus(open(day(3)))).toMatchObject({ key: 'closing-soon', severity: 'warn' })
  })
  it('TC-S03 còn 1 ngày -> Sắp hết hạn', () => {
    expect(getPositionStatus(open(day(1))).key).toBe('closing-soon')
  })
  it('TC-S04 đúng ngày hạn (còn 0 ngày) -> vẫn Sắp hết hạn, chưa quá hạn', () => {
    expect(getPositionStatus(open(day(0))).key).toBe('closing-soon')
  })
  it('TC-S05 quá hạn (backend gắn isExpired) -> Đã hết hạn', () => {
    expect(getPositionStatus(open(day(-1), { isExpired: true }))).toMatchObject({ key: 'expired', label: 'Đã hết hạn', severity: 'danger' })
  })
  it('TC-S06 không có hạn nộp -> Đang tuyển', () => {
    expect(getPositionStatus(open(null)).key).toBe('open')
  })
  it('TC-S07 đã đóng thủ công, còn hạn -> Đã đóng', () => {
    expect(getPositionStatus(open(day(10), { isOpen: false })).key).toBe('closed')
  })
  it('TC-S08 vừa đóng vừa quá hạn -> ưu tiên Đã hết hạn', () => {
    expect(getPositionStatus(open(day(-5), { isOpen: false, isExpired: true })).key).toBe('expired')
  })
  it('TC-S09 [biên] thiếu cờ isExpired nhưng ngày đã qua -> giao diện chỉ hiện Sắp hết hạn (phụ thuộc cờ của backend)', () => {
    expect(getPositionStatus(open(day(-2))).key).toBe('closing-soon')
  })
  it('TC-S10 dữ liệu rỗng không gây lỗi', () => {
    expect(() => getPositionStatus(undefined)).not.toThrow()
    expect(getPositionStatus(undefined).key).toBe('closed')
  })
})

describe('canApply - khóa nút Ứng tuyển, tránh gửi đơn không cần thiết', () => {
  it('TC-A01 đang tuyển, còn hạn -> được ứng tuyển', () => {
    expect(canApply(open(day(5)))).toBe(true)
  })
  it('TC-A02 đúng ngày hạn -> vẫn được ứng tuyển', () => {
    expect(canApply(open(day(0)))).toBe(true)
  })
  it('TC-A03 quá hạn -> bị khóa', () => {
    expect(canApply(open(day(-1), { isExpired: true }))).toBe(false)
  })
  it('TC-A04 đã đóng -> bị khóa', () => {
    expect(canApply(open(day(5), { isOpen: false }))).toBe(false)
  })
  it('TC-A05 không có hạn nộp -> được ứng tuyển', () => {
    expect(canApply(open(null))).toBe(true)
  })
  it('TC-A06 thiếu dữ liệu -> không được ứng tuyển', () => {
    expect(canApply(undefined)).toBe(false)
    expect(canApply({})).toBe(false)
  })
})

describe('formatDeadline', () => {
  it('TC-F01 định dạng DD/MM/YYYY', () => {
    expect(formatDeadline(new Date(2026, 9, 27).toISOString())).toBe('27/10/2026')
  })
  it('TC-F02 không có hạn -> "Không giới hạn"', () => {
    expect(formatDeadline(null)).toBe('Không giới hạn')
  })
})
