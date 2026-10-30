const passwordPolicyError =
  'Mật khẩu phải có từ 8 đến 128 ký tự, gồm ít nhất một chữ cái và một chữ số, không chứa ký tự điều khiển.'

export function validatePassword(value) {
  if (typeof value !== 'string' || value.trim().length === 0) {
    return 'Mật khẩu không được để trống hoặc chỉ chứa khoảng trắng.'
  }
  if (value.length < 8 || value.length > 128) {
    return passwordPolicyError
  }
  if (/[\u0000-\u001f\u007f-\u009f]/u.test(value)) {
    return 'Mật khẩu không được chứa ký tự điều khiển.'
  }
  if (!/\p{L}/u.test(value) || !/\p{Nd}/u.test(value)) {
    return 'Mật khẩu phải có ít nhất một chữ cái và một chữ số.'
  }
  return ''
}
