import { describe, expect, it } from 'vitest'
import { extractErrorMessage } from './apiError.js'

describe('extractErrorMessage', () => {
  it('returns a localized message for network errors', () => {
    expect(extractErrorMessage({ message: 'Network Error' })).toBe(
      'Không kết nối được máy chủ. Vui lòng kiểm tra mạng hoặc thử lại sau.'
    )
  })

  it('returns a localized message for timeouts', () => {
    expect(extractErrorMessage({ code: 'ECONNABORTED', message: 'timeout of 15000ms exceeded' })).toBe(
      'Không kết nối được máy chủ. Vui lòng kiểm tra mạng hoặc thử lại sau.'
    )
  })

  it('returns a localized message for server errors', () => {
    expect(extractErrorMessage({ response: { status: 500, data: { message: 'internal details' } } })).toBe(
      'Hệ thống đang gặp sự cố. Vui lòng thử lại sau.'
    )
  })

  it('prefers the server message for client errors', () => {
    expect(extractErrorMessage({ response: { status: 400, data: { message: 'Dữ liệu không hợp lệ.' } } })).toBe(
      'Dữ liệu không hợp lệ.'
    )
  })

  it('displays the server message for rate-limit responses', () => {
    expect(extractErrorMessage({
      response: {
        status: 429,
        data: { message: 'Bạn đã nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút.' }
      }
    })).toBe('Bạn đã nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút.')
  })

  it('prefers validation errors for client errors', () => {
    expect(extractErrorMessage({
      response: { status: 400, data: { errors: { Name: ['Tên bắt buộc.'], Email: ['Email sai.'] } } }
    })).toBe('Tên bắt buộc. Email sai.')
  })

  it('does not expose Axios English messages for other failures', () => {
    expect(extractErrorMessage({ response: { status: 403 }, message: 'Request failed with status code 403' })).toBe(
      'Có lỗi xảy ra, vui lòng thử lại.'
    )
  })
})
