import { describe, expect, it } from 'vitest'
import { getCompanyAccountWarning, isCompanySelectable } from './companyAccountValidation.js'

describe('companyAccountValidation', () => {
  it('returns false for null or undefined company', () => {
    expect(isCompanySelectable(null)).toBe(false)
    expect(isCompanySelectable(undefined)).toBe(false)
    expect(getCompanyAccountWarning(null)).toBeNull()
  })

  it('rejects selection when company already has account', () => {
    const company = {
      id: 1,
      name: 'Công ty A',
      hasAccount: true,
      contactEmail: 'contact@example.com'
    }
    expect(isCompanySelectable(company)).toBe(false)
    expect(getCompanyAccountWarning(company)).toBeNull()
  })

  it('detects missing contact email and marks as unselectable', () => {
    const companyNull = { id: 2, name: 'Công ty B', hasAccount: false, contactEmail: null }
    const companyEmpty = { id: 3, name: 'Công ty C', hasAccount: false, contactEmail: '   ' }

    expect(getCompanyAccountWarning(companyNull)).toBe('Thiếu email')
    expect(isCompanySelectable(companyNull)).toBe(false)

    expect(getCompanyAccountWarning(companyEmpty)).toBe('Thiếu email')
    expect(isCompanySelectable(companyEmpty)).toBe(false)
  })

  it('detects invalid email format and marks as unselectable', () => {
    const company = {
      id: 4,
      name: 'Công ty D',
      hasAccount: false,
      contactEmail: 'not-an-email'
    }
    expect(getCompanyAccountWarning(company)).toBe('Email sai định dạng')
    expect(isCompanySelectable(company)).toBe(false)
  })

  it('allows selection when company has valid email and no account', () => {
    const company = {
      id: 5,
      name: 'Công ty E',
      hasAccount: false,
      contactEmail: 'recruitment@company.com'
    }
    expect(getCompanyAccountWarning(company)).toBeNull()
    expect(isCompanySelectable(company)).toBe(true)
  })
})
