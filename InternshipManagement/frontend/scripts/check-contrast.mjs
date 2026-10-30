import { readFile } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const css = await readFile(path.join(root, 'src/style.css'), 'utf8')
const login = await readFile(path.join(root, 'src/views/auth/LoginView.vue'), 'utf8')
const companyCreate = await readFile(path.join(root, 'src/views/companies/CompanyCreateView.vue'), 'utf8')
const tokens = new Map(
  [...css.matchAll(/--([\w-]+)\s*:\s*([^;]+);/g)].map(([, name, value]) => [`--${name}`, value.trim()])
)

function resolveColor(value, seen = new Set()) {
  const token = value.match(/^--[\w-]+$/)?.[0]
  if (token) {
    if (seen.has(token) || !tokens.has(token)) return null
    seen.add(token)
    return resolveColor(tokens.get(token), seen)
  }
  const variable = value.match(/^var\(\s*(--[\w-]+)/)
  if (variable) {
    if (seen.has(variable[1]) || !tokens.has(variable[1])) return null
    seen.add(variable[1])
    return resolveColor(tokens.get(variable[1]), seen)
  }
  const hex = value.match(/^#([0-9a-f]{3,8})$/i)?.[1]
  if (!hex || ![3, 6].includes(hex.length)) return null
  const full = hex.length === 3 ? [...hex].map((digit) => digit + digit).join('') : hex
  return [0, 2, 4].map((index) => Number.parseInt(full.slice(index, index + 2), 16) / 255)
}

function luminance(color) {
  const channels = color.map((channel) => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4)
  return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722
}

function contrast(foreground, background) {
  const fg = resolveColor(foreground)
  const bg = resolveColor(background)
  if (!fg || !bg) return null
  const values = [luminance(fg), luminance(bg)].sort((a, b) => b - a)
  return (values[0] + 0.05) / (values[1] + 0.05)
}

const loginPlaceholder = login.match(/[^{}]*p-inputtext::placeholder[^{}]*\{\s*color:\s*(#[\da-f]{3,8})/i)?.[1] ?? '#94a3b8'
const backButtonColor = companyCreate.match(/\.back-button\s*\{[^}]*color:\s*([^;]+);/i)?.[1]?.trim() ?? '#78716c'
const secondaryTextOverride = css.match(/\.p-button\.p-button-secondary\.p-button-text\s*\{\s*color:\s*([^;]+);/i)?.[1]?.trim()
const checks = [
  ['placeholder / trắng', '--ui-placeholder', '#FFFFFF', 4.5],
  ['placeholder LoginView / nền', loginPlaceholder, '#f8fafc', 4.5],
  ['muted / canvas', '--ui-muted', '--ui-canvas', 4.5],
  ['secondary text (quay lại) / canvas', backButtonColor, '--ui-canvas', 4.5],
  ['secondary text (override chung) / canvas', secondaryTextOverride ?? '', '--ui-canvas', 4.5],
  ['warning ink / warning bg', '--ui-warning', '--ui-warning-bg', 4.5],
  ['error / trắng', '--ui-error', '#FFFFFF', 4.5],
  ['primary-600 / trắng', '--ui-primary-600', '#FFFFFF', 4.5],
  ['ui-input-border / trắng (ô nhập)', '--ui-input-border', '#FFFFFF', 3],
  ['apply-border / trắng (ô nhập)', '--apply-border', '#FFFFFF', 3],
  ['focus ring / trắng', '--ui-primary-600', '#FFFFFF', 3]
]

let failures = 0
console.log('Cặp màu'.padEnd(43) + 'Tỉ lệ'.padStart(9) + '  Ngưỡng  Kết quả')
console.log('-'.repeat(75))
for (const [label, foreground, background, minimum] of checks) {
  const ratio = contrast(foreground, background)
  const passed = ratio !== null && ratio >= minimum
  if (!passed) failures += 1
  console.log(`${label.padEnd(43)}${ratio === null ? 'không đo được' : `${ratio.toFixed(2)}:1`.padStart(9)}  ${minimum.toFixed(1)}:1    ${passed ? 'ĐẠT' : 'KHÔNG ĐẠT'}`)
}
console.log(`\nSố cặp màu không đạt: ${failures}`)
if (failures > 0) process.exitCode = 1
