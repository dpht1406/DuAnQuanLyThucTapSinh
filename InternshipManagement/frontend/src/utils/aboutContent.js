import { validateEmailAddress, validateSafeText } from './emailValidation.js'

export const ABOUT_LIMITS = Object.freeze({
  name: 200,
  shortName: 100,
  tagline: 300,
  summary: 1000,
  footnote: 300,
  facts: { min: 1, max: 8, label: 50, value: 100 },
  history: { max: 20, year: 20, title: 150, text: 500 },
  fields: { max: 8, title: 150, text: 400, maxTags: 8, tag: 30 },
  environment: {
    highlights: { max: 8, title: 150, text: 300 },
    week: { max: 7, day: 50, text: 300 },
    tools: { max: 12, item: 40 }
  },
  values: { max: 10, title: 150, text: 300 },
  studentInfo: {
    eligibility: { max: 10, item: 300 },
    documents: { max: 10, item: 150 },
    process: { max: 8, status: 50, text: 300 },
    checklist: { max: 10, id: 40, label: 200 },
    faq: { max: 15, q: 200, a: 1000 }
  },
  contact: {
    person: 150,
    address: 300,
    hours: 100,
    email: 150,
    phone: 20
  }
})

export const ALLOWED_ICONS = Object.freeze([
  { value: 'pi pi-calendar', label: 'Lịch (Calendar)' },
  { value: 'pi pi-users', label: 'Nhân sự / Nhóm (Users)' },
  { value: 'pi pi-briefcase', label: 'Công việc / Cặp (Briefcase)' },
  { value: 'pi pi-clock', label: 'Thời gian / Giờ (Clock)' },
  { value: 'pi pi-map-marker', label: 'Địa điểm (Map Marker)' },
  { value: 'pi pi-wallet', label: 'Hỗ trợ / Ví (Wallet)' },
  { value: 'pi pi-server', label: 'Máy chủ (Server)' },
  { value: 'pi pi-desktop', label: 'Ứng dụng web (Desktop)' },
  { value: 'pi pi-mobile', label: 'Ứng dụng di động (Mobile)' },
  { value: 'pi pi-check-circle', label: 'Kiểm thử / Thành công (Check Circle)' },
  { value: 'pi pi-user', label: 'Cá nhân / Mentor (User)' },
  { value: 'pi pi-sync', label: 'Quy trình / Sprint (Sync)' },
  { value: 'pi pi-code', label: 'Lập trình / Code (Code)' },
  { value: 'pi pi-shield', label: 'Bảo mật / Trung thực (Shield)' },
  { value: 'pi pi-book', label: 'Học tập / Sách (Book)' },
  { value: 'pi pi-verified', label: 'Chất lượng / Xác thực (Verified)' },
  { value: 'pi pi-heart', label: 'Tôn trọng / Trái tim (Heart)' },
  { value: 'pi pi-share-alt', label: 'Chia sẻ (Share)' },
  { value: 'pi pi-building', label: 'Tổ chức / Tòa nhà (Building)' },
  { value: 'pi pi-globe', label: 'Toàn cầu / Web (Globe)' },
  { value: 'pi pi-star', label: 'Nổi bật / Ngôi sao (Star)' },
  { value: 'pi pi-award', label: 'Thành tích (Award)' },
  { value: 'pi pi-graduation-cap', label: 'Sinh viên / Đào tạo (Graduation Cap)' },
  { value: 'pi pi-lightbulb', label: 'Ý tưởng / Đổi mới (Lightbulb)' },
  { value: 'pi pi-bolt', label: 'Tốc độ / Năng động (Bolt)' },
  { value: 'pi pi-compass', label: 'Định hướng (Compass)' },
  { value: 'pi pi-cog', label: 'Vận hành / Kỹ thuật (Cog)' },
  { value: 'pi pi-chart-bar', label: 'Số liệu / Báo cáo (Chart Bar)' },
  { value: 'pi pi-comments', label: 'Hỏi đáp / Trao đổi (Comments)' },
  { value: 'pi pi-folder', label: 'Hồ sơ / Tài liệu (Folder)' }
])

const ALLOWED_ICON_VALUES = new Set(ALLOWED_ICONS.map((i) => i.value))
const CHECKLIST_ID_PATTERN = /^[a-z0-9-]+$/

export function isAllowedIcon(icon) {
  return typeof icon === 'string' && ALLOWED_ICON_VALUES.has(icon.trim())
}

export function generateChecklistId(existingIds = []) {
  const set = new Set((existingIds || []).map((id) => String(id || '').trim().toLowerCase()))
  let index = 1
  while (set.has(`item-${index}`)) {
    index += 1
  }
  return `item-${index}`
}

export function validateAboutContent(form) {
  const errors = {}

  if (!form || typeof form !== 'object') {
    errors.root = 'Dữ liệu không hợp lệ.'
    return errors
  }

  // 1. Thông tin chung
  const name = String(form.name ?? '').trim()
  if (!name) {
    errors.name = 'Tên đơn vị tiếp nhận không được để trống.'
  } else {
    const err = validateSafeText(form.name, ABOUT_LIMITS.name)
    if (err) errors.name = err
  }

  if (form.shortName) {
    const err = validateSafeText(form.shortName, ABOUT_LIMITS.shortName)
    if (err) errors.shortName = err
  }

  const tagline = String(form.tagline ?? '').trim()
  if (!tagline) {
    errors.tagline = 'Khẩu hiệu không được để trống.'
  } else {
    const err = validateSafeText(form.tagline, ABOUT_LIMITS.tagline)
    if (err) errors.tagline = err
  }

  const summary = String(form.summary ?? '').trim()
  if (!summary) {
    errors.summary = 'Giới thiệu tóm tắt không được để trống.'
  } else {
    const err = validateSafeText(form.summary, ABOUT_LIMITS.summary, true)
    if (err) errors.summary = err
  }

  if (form.footnote) {
    const err = validateSafeText(form.footnote, ABOUT_LIMITS.footnote)
    if (err) errors.footnote = err
  }

  // 2. Facts (1 đến 8)
  const facts = form.facts || []
  if (facts.length < ABOUT_LIMITS.facts.min || facts.length > ABOUT_LIMITS.facts.max) {
    errors.facts = `Thông tin nhanh phải có từ ${ABOUT_LIMITS.facts.min} đến ${ABOUT_LIMITS.facts.max} mục.`
  }
  facts.forEach((fact, i) => {
    if (!isAllowedIcon(fact.icon)) {
      errors[`facts.${i}.icon`] = 'Biểu tượng không hợp lệ.'
    }
    const label = String(fact.label ?? '').trim()
    if (!label) {
      errors[`facts.${i}.label`] = 'Nhãn không được để trống.'
    } else {
      const err = validateSafeText(fact.label, ABOUT_LIMITS.facts.label)
      if (err) errors[`facts.${i}.label`] = err
    }
    const value = String(fact.value ?? '').trim()
    if (!value) {
      errors[`facts.${i}.value`] = 'Giá trị không được để trống.'
    } else {
      const err = validateSafeText(fact.value, ABOUT_LIMITS.facts.value)
      if (err) errors[`facts.${i}.value`] = err
    }
  })

  // 3. History (0 đến 20)
  const history = form.history || []
  if (history.length > ABOUT_LIMITS.history.max) {
    errors.history = `Lịch sử không được quá ${ABOUT_LIMITS.history.max} mục.`
  }
  history.forEach((item, i) => {
    const year = String(item.year ?? '').trim()
    if (!year) errors[`history.${i}.year`] = 'Năm không được để trống.'
    else {
      const err = validateSafeText(item.year, ABOUT_LIMITS.history.year)
      if (err) errors[`history.${i}.year`] = err
    }

    const title = String(item.title ?? '').trim()
    if (!title) errors[`history.${i}.title`] = 'Tiêu đề không được để trống.'
    else {
      const err = validateSafeText(item.title, ABOUT_LIMITS.history.title)
      if (err) errors[`history.${i}.title`] = err
    }

    const text = String(item.text ?? '').trim()
    if (!text) errors[`history.${i}.text`] = 'Nội dung không được để trống.'
    else {
      const err = validateSafeText(item.text, ABOUT_LIMITS.history.text)
      if (err) errors[`history.${i}.text`] = err
    }
  })

  // 4. Fields (0 đến 8)
  const fields = form.fields || []
  if (fields.length > ABOUT_LIMITS.fields.max) {
    errors.fields = `Lĩnh vực không được quá ${ABOUT_LIMITS.fields.max} mục.`
  }
  fields.forEach((item, i) => {
    if (!isAllowedIcon(item.icon)) {
      errors[`fields.${i}.icon`] = 'Biểu tượng không hợp lệ.'
    }
    const title = String(item.title ?? '').trim()
    if (!title) errors[`fields.${i}.title`] = 'Tiêu đề không được để trống.'
    else {
      const err = validateSafeText(item.title, ABOUT_LIMITS.fields.title)
      if (err) errors[`fields.${i}.title`] = err
    }

    const text = String(item.text ?? '').trim()
    if (!text) errors[`fields.${i}.text`] = 'Mô tả không được để trống.'
    else {
      const err = validateSafeText(item.text, ABOUT_LIMITS.fields.text)
      if (err) errors[`fields.${i}.text`] = err
    }

    const tags = item.tags || []
    if (tags.length > ABOUT_LIMITS.fields.maxTags) {
      errors[`fields.${i}.tags`] = `Không được quá ${ABOUT_LIMITS.fields.maxTags} thẻ.`
    }
    tags.forEach((tag, t) => {
      const trimmedTag = String(tag ?? '').trim()
      if (!trimmedTag) errors[`fields.${i}.tags.${t}`] = 'Thẻ không được để trống.'
      else {
        const err = validateSafeText(tag, ABOUT_LIMITS.fields.tag)
        if (err) errors[`fields.${i}.tags.${t}`] = err
      }
    })
  })

  // 5. Environment
  const env = form.environment || {}
  const highlights = env.highlights || []
  if (highlights.length > ABOUT_LIMITS.environment.highlights.max) {
    errors['environment.highlights'] = `Điểm nổi bật không được quá ${ABOUT_LIMITS.environment.highlights.max} mục.`
  }
  highlights.forEach((item, i) => {
    if (!isAllowedIcon(item.icon)) {
      errors[`environment.highlights.${i}.icon`] = 'Biểu tượng không hợp lệ.'
    }
    const title = String(item.title ?? '').trim()
    if (!title) errors[`environment.highlights.${i}.title`] = 'Tiêu đề không được để trống.'
    else {
      const err = validateSafeText(item.title, ABOUT_LIMITS.environment.highlights.title)
      if (err) errors[`environment.highlights.${i}.title`] = err
    }
    const text = String(item.text ?? '').trim()
    if (!text) errors[`environment.highlights.${i}.text`] = 'Mô tả không được để trống.'
    else {
      const err = validateSafeText(item.text, ABOUT_LIMITS.environment.highlights.text)
      if (err) errors[`environment.highlights.${i}.text`] = err
    }
  })

  const week = env.week || []
  if (week.length > ABOUT_LIMITS.environment.week.max) {
    errors['environment.week'] = `Lịch tuần không được quá ${ABOUT_LIMITS.environment.week.max} mục.`
  }
  week.forEach((item, i) => {
    const day = String(item.day ?? '').trim()
    if (!day) errors[`environment.week.${i}.day`] = 'Ngày/thời gian không được để trống.'
    else {
      const err = validateSafeText(item.day, ABOUT_LIMITS.environment.week.day)
      if (err) errors[`environment.week.${i}.day`] = err
    }
    const text = String(item.text ?? '').trim()
    if (!text) errors[`environment.week.${i}.text`] = 'Nội dung không được để trống.'
    else {
      const err = validateSafeText(item.text, ABOUT_LIMITS.environment.week.text)
      if (err) errors[`environment.week.${i}.text`] = err
    }
  })

  const tools = env.tools || []
  if (tools.length > ABOUT_LIMITS.environment.tools.max) {
    errors['environment.tools'] = `Công cụ không được quá ${ABOUT_LIMITS.environment.tools.max} công cụ.`
  }
  tools.forEach((tool, i) => {
    const trimmed = String(tool ?? '').trim()
    if (!trimmed) errors[`environment.tools.${i}`] = 'Tên công cụ không được để trống.'
    else {
      const err = validateSafeText(tool, ABOUT_LIMITS.environment.tools.item)
      if (err) errors[`environment.tools.${i}`] = err
    }
  })

  // 6. Values
  const values = form.values || []
  if (values.length > ABOUT_LIMITS.values.max) {
    errors.values = `Giá trị cốt lõi không được quá ${ABOUT_LIMITS.values.max} mục.`
  }
  values.forEach((item, i) => {
    if (!isAllowedIcon(item.icon)) {
      errors[`values.${i}.icon`] = 'Biểu tượng không hợp lệ.'
    }
    const title = String(item.title ?? '').trim()
    if (!title) errors[`values.${i}.title`] = 'Tiêu đề không được để trống.'
    else {
      const err = validateSafeText(item.title, ABOUT_LIMITS.values.title)
      if (err) errors[`values.${i}.title`] = err
    }
    const text = String(item.text ?? '').trim()
    if (!text) errors[`values.${i}.text`] = 'Mô tả không được để trống.'
    else {
      const err = validateSafeText(item.text, ABOUT_LIMITS.values.text)
      if (err) errors[`values.${i}.text`] = err
    }
  })

  // 7. Student Info
  const studentInfo = form.studentInfo || {}
  const eligibility = studentInfo.eligibility || []
  if (eligibility.length > ABOUT_LIMITS.studentInfo.eligibility.max) {
    errors['studentInfo.eligibility'] = `Điều kiện không được quá ${ABOUT_LIMITS.studentInfo.eligibility.max} mục.`
  }
  eligibility.forEach((item, i) => {
    const trimmed = String(item ?? '').trim()
    if (!trimmed) errors[`studentInfo.eligibility.${i}`] = 'Nội dung không được để trống.'
    else {
      const err = validateSafeText(item, ABOUT_LIMITS.studentInfo.eligibility.item)
      if (err) errors[`studentInfo.eligibility.${i}`] = err
    }
  })

  const documents = studentInfo.documents || []
  if (documents.length > ABOUT_LIMITS.studentInfo.documents.max) {
    errors['studentInfo.documents'] = `Hồ sơ không được quá ${ABOUT_LIMITS.studentInfo.documents.max} mục.`
  }
  documents.forEach((item, i) => {
    const trimmed = String(item ?? '').trim()
    if (!trimmed) errors[`studentInfo.documents.${i}`] = 'Tên giấy tờ không được để trống.'
    else {
      const err = validateSafeText(item, ABOUT_LIMITS.studentInfo.documents.item)
      if (err) errors[`studentInfo.documents.${i}`] = err
    }
  })

  const process = studentInfo.process || []
  if (process.length > ABOUT_LIMITS.studentInfo.process.max) {
    errors['studentInfo.process'] = `Quy trình không được quá ${ABOUT_LIMITS.studentInfo.process.max} bước.`
  }
  process.forEach((item, i) => {
    const status = String(item.status ?? '').trim()
    if (!status) errors[`studentInfo.process.${i}.status`] = 'Trạng thái không được để trống.'
    else {
      const err = validateSafeText(item.status, ABOUT_LIMITS.studentInfo.process.status)
      if (err) errors[`studentInfo.process.${i}.status`] = err
    }
    const text = String(item.text ?? '').trim()
    if (!text) errors[`studentInfo.process.${i}.text`] = 'Mô tả không được để trống.'
    else {
      const err = validateSafeText(item.text, ABOUT_LIMITS.studentInfo.process.text)
      if (err) errors[`studentInfo.process.${i}.text`] = err
    }
  })

  const checklist = studentInfo.checklist || []
  if (checklist.length > ABOUT_LIMITS.studentInfo.checklist.max) {
    errors['studentInfo.checklist'] = `Checklist không được quá ${ABOUT_LIMITS.studentInfo.checklist.max} mục.`
  }
  const seenIds = new Set()
  checklist.forEach((item, i) => {
    const id = String(item.id ?? '').trim()
    if (!id || id.length > ABOUT_LIMITS.studentInfo.checklist.id || !CHECKLIST_ID_PATTERN.test(id)) {
      errors[`studentInfo.checklist.${i}.id`] = 'Mã định danh (id) phải từ 1-40 ký tự và chỉ chứa chữ thường, số, gạch ngang.'
    } else if (seenIds.has(id.toLowerCase())) {
      errors[`studentInfo.checklist.${i}.id`] = `Mã định danh '${id}' bị trùng lặp.`
    } else {
      seenIds.add(id.toLowerCase())
    }

    const label = String(item.label ?? '').trim()
    if (!label) errors[`studentInfo.checklist.${i}.label`] = 'Nội dung không được để trống.'
    else {
      const err = validateSafeText(item.label, ABOUT_LIMITS.studentInfo.checklist.label)
      if (err) errors[`studentInfo.checklist.${i}.label`] = err
    }
  })

  const faq = studentInfo.faq || []
  if (faq.length > ABOUT_LIMITS.studentInfo.faq.max) {
    errors['studentInfo.faq'] = `Câu hỏi thường gặp không được quá ${ABOUT_LIMITS.studentInfo.faq.max} câu.`
  }
  faq.forEach((item, i) => {
    const q = String(item.q ?? '').trim()
    if (!q) errors[`studentInfo.faq.${i}.q`] = 'Câu hỏi không được để trống.'
    else {
      const err = validateSafeText(item.q, ABOUT_LIMITS.studentInfo.faq.q)
      if (err) errors[`studentInfo.faq.${i}.q`] = err
    }

    const a = String(item.a ?? '').trim()
    if (!a) errors[`studentInfo.faq.${i}.a`] = 'Câu trả lời không được để trống.'
    else {
      const err = validateSafeText(item.a, ABOUT_LIMITS.studentInfo.faq.a, true)
      if (err) errors[`studentInfo.faq.${i}.a`] = err
    }
  })

  // 8. Contact
  const contact = form.contact || {}
  if (contact.person) {
    const err = validateSafeText(contact.person, ABOUT_LIMITS.contact.person)
    if (err) errors['contact.person'] = err
  }
  if (contact.address) {
    const err = validateSafeText(contact.address, ABOUT_LIMITS.contact.address)
    if (err) errors['contact.address'] = err
  }
  if (contact.hours) {
    const err = validateSafeText(contact.hours, ABOUT_LIMITS.contact.hours)
    if (err) errors['contact.hours'] = err
  }
  const email = String(contact.email ?? '').trim()
  if (email) {
    const emailErr = validateEmailAddress(contact.email, ABOUT_LIMITS.contact.email)
    if (emailErr) errors['contact.email'] = emailErr
  }
  const phone = String(contact.phone ?? '').trim()
  if (phone) {
    if (phone.length > ABOUT_LIMITS.contact.phone) {
      errors['contact.phone'] = `Số điện thoại không được quá ${ABOUT_LIMITS.contact.phone} ký tự.`
    } else {
      const phoneCharsValid = /^[0-9+\-().\s]+$/.test(phone)
      const digitsCount = (phone.match(/\d/g) || []).length
      if (!phoneCharsValid || digitsCount < 8) {
        errors['contact.phone'] = 'Số điện thoại chỉ gồm chữ số, dấu cách, +, -, (, ), . và có ít nhất 8 chữ số.'
      }
    }
  }

  return errors
}
