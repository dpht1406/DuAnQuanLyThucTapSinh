export const POPULAR_EMAIL_DOMAINS = Object.freeze([
  'gmail.com', 'outlook.com', 'hotmail.com', 'yahoo.com', 'icloud.com', 'live.com'
])

const BLOCKED_EMAIL_DOMAIN_TYPOS = new Set([
  'gmail.co', 'gmail.con', 'gmail.cm', 'gmail.comm', 'gmial.com', 'gmai.com',
  'gmal.com', 'gamil.com', 'gnail.com', 'hotmal.com', 'hotmail.co', 'outlok.com',
  'outlook.co', 'yaho.com', 'yahoo.co'
])

const LOCAL_PART_PATTERN = /^[A-Za-z0-9._%+-]+$/
const DOMAIN_LABEL_PATTERN = /^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/
const TOP_LEVEL_DOMAIN_PATTERN = /^[A-Za-z]{2,}$/
const CONTROL_CHARACTER_PATTERN = /[\u0000-\u001F\u007F-\u009F]/u

export function validateEmailAddress(value, maximumLength = 254) {
  if (typeof value !== 'string') return 'Email không đúng định dạng.'
  if (CONTROL_CHARACTER_PATTERN.test(value)) return 'Email không được chứa ký tự điều khiển.'

  const email = value.trim()
  if (!email || email.length > maximumLength || (email.match(/@/g) || []).length !== 1 || /\s/u.test(email)) {
    return `Email không đúng định dạng hoặc vượt quá ${maximumLength} ký tự.`
  }

  const separator = email.indexOf('@')
  const localPart = email.slice(0, separator)
  const domain = email.slice(separator + 1)
  if (localPart.length > 64 || !LOCAL_PART_PATTERN.test(localPart) ||
      localPart.startsWith('.') || localPart.endsWith('.') || localPart.includes('..')) {
    return 'Phần trước @ của email không hợp lệ.'
  }

  const normalizedDomain = domain.toLowerCase()
  if (POPULAR_EMAIL_DOMAINS.includes(normalizedDomain)) return null

  const suggestionDomain = POPULAR_EMAIL_DOMAINS
    .map((candidate, index) => ({ candidate, index, distance: levenshteinDistance(normalizedDomain, candidate) }))
    .filter(({ distance }) => distance <= 2)
    .sort((left, right) => left.distance - right.distance || left.index - right.index)[0]?.candidate

  if (BLOCKED_EMAIL_DOMAIN_TYPOS.has(normalizedDomain) || suggestionDomain) {
    return `Email có vẻ gõ sai. Ý bạn là ${localPart}@${suggestionDomain || findBlockedDomainSuggestion(normalizedDomain)}?`
  }

  const labels = domain.split('.')
  if (labels.length < 2 || labels.some((label) =>
    label.length < 1 || label.length > 63 || !DOMAIN_LABEL_PATTERN.test(label)
  ) || !TOP_LEVEL_DOMAIN_PATTERN.test(labels.at(-1))) {
    return 'Tên miền email không đúng định dạng.'
  }

  return null
}

export function validateSafeText(value, maximumLength, allowLineBreaks = false) {
  if (typeof value !== 'string') return null
  if (value.length > maximumLength) return `Không được vượt quá ${maximumLength} ký tự.`

  const pattern = allowLineBreaks
    ? /[\u0000-\u0009\u000B\u000C\u000E-\u001F\u007F-\u009F]/u
    : CONTROL_CHARACTER_PATTERN
  return pattern.test(value) ? 'Không được chứa ký tự điều khiển không hợp lệ.' : null
}

function findBlockedDomainSuggestion(domain) {
  return POPULAR_EMAIL_DOMAINS
    .map((candidate, index) => ({ candidate, index, distance: levenshteinDistance(domain, candidate) }))
    .sort((left, right) => left.distance - right.distance || left.index - right.index)[0].candidate
}

function levenshteinDistance(left, right) {
  let previous = Array.from({ length: right.length + 1 }, (_, index) => index)
  let current = new Array(right.length + 1)

  for (let leftIndex = 1; leftIndex <= left.length; leftIndex += 1) {
    current[0] = leftIndex
    for (let rightIndex = 1; rightIndex <= right.length; rightIndex += 1) {
      const substitutionCost = left[leftIndex - 1] === right[rightIndex - 1] ? 0 : 1
      current[rightIndex] = Math.min(
        current[rightIndex - 1] + 1,
        previous[rightIndex] + 1,
        previous[rightIndex - 1] + substitutionCost
      )
    }
    ;[previous, current] = [current, previous]
  }

  return previous[right.length]
}