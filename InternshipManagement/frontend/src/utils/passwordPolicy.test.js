import { describe, expect, it } from 'vitest'
import { validatePassword } from './passwordPolicy.js'

describe('validatePassword', () => {
  it.each([
    '',
    '1234567',
    'abcdefgh',
    '12345678',
    '        ',
    'Abc1234\n',
    `A1${'b'.repeat(127)}`
  ])('rejects invalid password %j', (password) => {
    expect(validatePassword(password)).not.toBe('')
  })

  it.each([
    'Matkhau2026',
    'Abc12345',
    `A1${'b'.repeat(126)}`
  ])('accepts valid password %j', (password) => {
    expect(validatePassword(password)).toBe('')
  })
})
