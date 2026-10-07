import dayjs from './dayjs.js'

const serverTimeZoneSuffix = /(?:z|[+-]\d{2}:?\d{2})$/i

export function parseServerUtc(value) {
  if (value == null) return null

  if (value instanceof Date) {
    const parsed = dayjs(value)
    return parsed.isValid() ? parsed : null
  }

  if (typeof value !== 'string') return null

  const input = value.trim()
  if (!input) return null

  const timestamp = serverTimeZoneSuffix.test(input) ? input : `${input}Z`
  const parsed = dayjs(timestamp)
  return parsed.isValid() ? parsed : null
}

export function formatServerDateTime(value, pattern = 'DD/MM/YYYY HH:mm') {
  const parsed = parseServerUtc(value)
  return parsed ? parsed.format(pattern) : null
}
