import { describe, expect, it } from 'vitest'
import { formatServerDateTime, parseServerUtc } from './dateTime.js'

describe('parseServerUtc', () => {
  it('treats an offset-free server timestamp as UTC', () => {
    expect(parseServerUtc('2026-10-07T01:38:00.123')?.valueOf())
      .toBe(Date.UTC(2026, 9, 7, 1, 38, 0, 123))
  })

  it('preserves timestamps that already include a timezone', () => {
    const expected = Date.UTC(2026, 9, 7, 1, 38)

    expect(parseServerUtc('2026-10-07T01:38:00Z')?.valueOf()).toBe(expected)
    expect(parseServerUtc('2026-10-07T08:38:00+07:00')?.valueOf()).toBe(expected)
  })

  it.each([null, '', undefined, 'abc'])('returns null for invalid or empty values: %s', (value) => {
    expect(parseServerUtc(value)).toBeNull()
  })

  it('preserves the instant represented by a Date object', () => {
    const value = new Date(Date.UTC(2026, 9, 7, 1, 38, 0, 123))

    expect(parseServerUtc(value)?.valueOf()).toBe(value.valueOf())
  })
})

describe('formatServerDateTime', () => {
  it('formats the timestamp using the browser local timezone', () => {
    expect(formatServerDateTime('2026-10-07T01:38:00.123'))
      .toMatch(/^\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}$/)
  })

  it('returns null for invalid values', () => {
    expect(formatServerDateTime('abc')).toBeNull()
  })
})
