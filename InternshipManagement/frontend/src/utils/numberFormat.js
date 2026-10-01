const vietnameseNumber = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 1 })
const vietnameseInteger = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 })

export function formatNumberVi(value) {
  return vietnameseNumber.format(Number(value) || 0)
}

export function formatIntegerVi(value) {
  return vietnameseInteger.format(Number(value) || 0)
}

export function formatPercentVi(value) {
  return `${formatNumberVi(value)}%`
}
