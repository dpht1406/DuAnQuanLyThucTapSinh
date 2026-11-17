import { describe, expect, it } from 'vitest'
import { formatCoverLetter } from '../../utils/companyPlacementRequest.js'

describe('CompanyPlacementRequestDetailView coverLetter fallback (P4-02)', () => {
  it('returns valid cover letter when present', () => {
    expect(formatCoverLetter('Đơn ứng tuyển thực tập sinh.', null)).toBe('Đơn ứng tuyển thực tập sinh.')
  })

  it('falls back to note when cover letter is empty or null', () => {
    expect(formatCoverLetter('', 'Ghi chú từ ứng viên.')).toBe('Ghi chú từ ứng viên.')
    expect(formatCoverLetter(null, 'Ghi chú từ ứng viên.')).toBe('Ghi chú từ ứng viên.')
  })

  it('falls back to natural Vietnamese message when both are empty', () => {
    const fallback = formatCoverLetter(null, null)
    expect(fallback).toBe('Chưa có thư giới thiệu.')
    const junkMarker = ['ted', 'jsf', 'jdjklfjasdfslfajaisf'].join('')
    expect(fallback).not.toContain(junkMarker)
  })
})
