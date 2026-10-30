import { describe, expect, it } from 'vitest'
import {
  mapPlacementRequestDueStatus,
  mapPlacementRequestStatus,
  validateCompanyInterviewForm,
  validateCompanyPlacementResult,
  validateCompanyRejectReason
} from './companyPlacementRequest.js'

describe('mapPlacementRequestStatus', () => {
  it.each([
    [0, 'Chờ xử lý'],
    [3, 'Đã mời phỏng vấn'],
    [4, 'Đã nhận'],
    [2, 'Đã từ chối'],
    [5, 'Quá hạn'],
    ['InterviewInvited', 'Đã mời phỏng vấn']
  ])('maps status %s to its display label', (status, label) => {
    expect(mapPlacementRequestStatus(status).label).toBe(label)
  })

  it('maps every supported numeric request status to the Tag key and severity', () => {
    expect([0, 3, 4, 2, 5].map((status) => {
      const { key, severity } = mapPlacementRequestStatus(status)
      return { key, severity }
    })).toEqual([
      { key: 'Pending', severity: 'warn' },
      { key: 'InterviewInvited', severity: 'info' },
      { key: 'Accepted', severity: 'success' },
      { key: 'Rejected', severity: 'danger' },
      { key: 'Expired', severity: 'secondary' }
    ])
  })

  it('does not map the retired numeric status or unknown values to a valid status', () => {
    expect(mapPlacementRequestStatus(1)).toMatchObject({ value: null, key: 'Unknown' })
    expect(mapPlacementRequestStatus('Unexpected')).toMatchObject({ value: null, key: 'Unknown' })
    expect(mapPlacementRequestStatus(null)).toMatchObject({ value: null, key: 'Unknown' })
  })
})

describe('mapPlacementRequestDueStatus', () => {
  const now = new Date('2030-06-01T00:00:00Z')

  it('marks expired, soon, normal, and missing due dates distinctly', () => {
    expect(mapPlacementRequestDueStatus('2030-05-31T23:59:59Z', now)).toBe('overdue')
    expect(mapPlacementRequestDueStatus('2030-06-02T00:00:00Z', now)).toBe('soon')
    expect(mapPlacementRequestDueStatus('2030-06-04T00:00:01Z', now)).toBe('normal')
    expect(mapPlacementRequestDueStatus(null, now)).toBe('none')
  })
})

describe('placement request form validation', () => {
  const now = new Date('2030-06-01T00:00:00Z')

  it('requires a future interview date and location and enforces field limits', () => {
    expect(validateCompanyInterviewForm({
      interviewAt: '2029-01-01T10:00',
      interviewLocation: ' ',
      interviewNote: ''
    }, now)).toMatchObject({
      interviewAt: 'Thời gian phỏng vấn phải ở tương lai.',
      interviewLocation: 'Địa điểm phỏng vấn là bắt buộc.'
    })
    expect(validateCompanyInterviewForm({
      interviewAt: '2030-06-02T10:00',
      interviewLocation: 'L'.repeat(501),
      interviewNote: 'N'.repeat(1001)
    }, now)).toMatchObject({
      interviewLocation: 'Địa điểm phỏng vấn không được vượt quá 500 ký tự.',
      interviewNote: 'Ghi chú phỏng vấn không được vượt quá 1000 ký tự.'
    })
  })

  it('requires a rejection reason and a note for a failed interview result', () => {
    expect(validateCompanyRejectReason(' ')).toBe('Lý do từ chối là bắt buộc.')
    expect(validateCompanyRejectReason('Không phù hợp')).toBe('')
    expect(validateCompanyPlacementResult(false, ' ')).toBe('Ghi chú là bắt buộc khi sinh viên không đạt.')
    expect(validateCompanyPlacementResult(true, '')).toBe('')
    expect(validateCompanyPlacementResult(false, 'N'.repeat(1001)))
      .toBe('Ghi chú kết quả không được vượt quá 1000 ký tự.')
  })
})
