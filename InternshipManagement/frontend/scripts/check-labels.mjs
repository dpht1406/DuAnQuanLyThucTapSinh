import { readdir, readFile } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const sourceRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../src')

async function vueFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true })
  const nested = await Promise.all(entries.map(async (entry) => {
    const filePath = path.join(directory, entry.name)
    if (entry.isDirectory()) return vueFiles(filePath)
    return entry.isFile() && entry.name.endsWith('.vue') ? [filePath] : []
  }))
  return nested.flat()
}

const files = await vueFiles(sourceRoot)
const findings = []

for (const filePath of files) {
  const source = await readFile(filePath, 'utf8')
  const relativePath = path.relative(path.dirname(sourceRoot), filePath).replaceAll(path.sep, '/')
  const labels = [...source.matchAll(/<label\b([^>]*)>/gi)]
  const controls = [...source.matchAll(/<(input|textarea|select|InputText|Textarea|Select|Password|InputNumber|DatePicker|Checkbox|RadioButton|ToggleSwitch|AutoComplete|MultiSelect)\b([^>]*)>/gi)]

  for (const [, attributes, index] of labels) {
    const target = attributes.match(/(?<![:\w-])for\s*=\s*["']([^"']+)["']/i)?.[1]
    if (!target) continue
    const escapedTarget = target.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    const targetPattern = new RegExp(`\\b(?:id|input-id|inputId)\\s*=\\s*["']${escapedTarget}["']`, 'i')
    const formFieldPattern = new RegExp(`<FormField\\b[^>]*(?<![:\\w-])for\\s*=\\s*["']${escapedTarget}["']`, 'i')
    if (!targetPattern.test(source) && !formFieldPattern.test(source)) {
      const line = source.slice(0, index).split('\n').length
      findings.push(`${relativePath}:${line} orphan label for="${target}"`)
    }
  }

  const formFieldIds = [...source.matchAll(/<FormField\b([^>]*)>/gi)]
    .map(([, attributes]) => attributes.match(/(?<![:\w-])for\s*=\s*["']([^"']+)["']/i)?.[1])
    .filter(Boolean)

  for (const [control, tagName, attributes, index] of controls) {
    if (/\baria-label\s*=|\baria-labelledby\s*=/i.test(attributes)) continue
    const controlId = attributes.match(/\b(?:id|input-id|inputId)\s*=\s*["']([^"']+)["']/i)?.[1]
    const openingLabel = source.slice(0, index).lastIndexOf('<label')
    const closingLabel = source.slice(0, index).lastIndexOf('</label>')
    const wrappedByLabel = openingLabel > closingLabel && source.indexOf('</label>', index + control.length) >= 0
    if (!controlId) {
      if (!wrappedByLabel) {
        const line = source.slice(0, index).split('\n').length
        findings.push(`${relativePath}:${line} input without label/aria-label (${tagName}, no id)`)
      }
      continue
    }
    const escapedId = controlId.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    const labeled = labels.some(([, labelAttributes]) =>
      new RegExp(`\\bfor\\s*=\\s*["']${escapedId}["']`, 'i').test(labelAttributes)
    ) || formFieldIds.includes(controlId) || wrappedByLabel
    if (!labeled) {
      const line = source.slice(0, index).split('\n').length
      findings.push(`${relativePath}:${line} input without label/aria-label (${tagName}, id="${controlId}")`)
    }
  }
}

if (findings.length) {
  console.log(findings.join('\n'))
  console.log(`\nSố phát hiện: ${findings.length}`)
  process.exitCode = 1
} else {
  console.log(`Không phát hiện label mồ côi hoặc input thiếu label/aria-label (${files.length} file Vue).`)
}
