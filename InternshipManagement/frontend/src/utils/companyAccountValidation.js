import { validateEmailAddress } from './emailValidation.js'

/**
 * Xác định nhãn cảnh báo cho doanh nghiệp chưa thể tạo tài khoản:
 * - 'Thiếu email': chưa cập nhật ContactEmail
 * - 'Email sai định dạng': ContactEmail không hợp lệ theo quy tắc chung
 * - null: công ty đã có tài khoản hoặc hợp lệ để tạo
 */
export function getCompanyAccountWarning(company) {
  if (!company) return null
  if (company.hasAccount) return null

  const rawEmail = company.contactEmail
  if (!rawEmail || !String(rawEmail).trim()) {
    return 'Thiếu email'
  }

  const emailError = validateEmailAddress(String(rawEmail).trim())
  if (emailError) {
    return 'Email sai định dạng'
  }

  return null
}

/**
 * Xác định dòng doanh nghiệp có thể chọn trong bảng tạo tài khoản hay không.
 * Điều kiện chọn được: chưa có tài khoản, có email và email hợp lệ.
 */
export function isCompanySelectable(company) {
  if (!company) return false
  if (company.hasAccount) return false
  return getCompanyAccountWarning(company) === null
}
