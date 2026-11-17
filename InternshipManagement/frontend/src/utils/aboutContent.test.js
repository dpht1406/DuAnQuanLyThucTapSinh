import { describe, expect, it } from 'vitest'
import {
  ABOUT_LIMITS,
  ALLOWED_ICONS,
  generateChecklistId,
  isAllowedIcon,
  validateAboutContent
} from './aboutContent.js'

function createValidForm() {
  return {
    name: 'Công ty Cổ phần Công nghệ Cửu Long',
    shortName: 'Cửu Long Tech',
    tagline: 'Chúng tôi làm phần mềm cho doanh nghiệp vừa và nhỏ.',
    summary: 'Giới thiệu công ty tóm tắt.',
    footnote: 'Ghi chú cuối trang.',
    facts: [
      { icon: 'pi pi-calendar', label: 'Thành lập', value: '2014' }
    ],
    history: [
      { year: '2014', title: 'Thành lập', text: 'Nội dung thành lập công ty.' }
    ],
    fields: [
      { icon: 'pi pi-server', title: 'Phần mềm', text: 'Mô tả phần mềm', tags: ['C#', '.NET'] }
    ],
    environment: {
      highlights: [{ icon: 'pi pi-user', title: 'Mentor', text: 'Có mentor kèm cặp.' }],
      week: [{ day: 'Thứ Hai', text: 'Lập kế hoạch tuần.' }],
      tools: ['Git', 'Jira']
    },
    values: [
      { icon: 'pi pi-shield', title: 'Trung thực', text: 'Trung thực với số liệu.' }
    ],
    studentInfo: {
      eligibility: ['Sinh viên năm 3 trở lên'],
      documents: ['CV một trang'],
      process: [{ status: 'Đã nhận', text: 'Thông báo kết quả' }],
      checklist: [
        { id: 'cv', label: 'Cập nhật CV' },
        { id: 'transcript', label: 'Bảng điểm' }
      ],
      faq: [{ q: 'Có lương không?', a: 'Có hỗ trợ thực tập.' }]
    },
    contact: {
      person: 'Chị Lê Thu Hà',
      address: '12 Đường số 5, Cần Thơ',
      email: 'thuctap@cuulongtech.example',
      phone: '0292 000 0000',
      hours: '8:30 - 17:30'
    }
  }
}

describe('aboutContent validation', () => {
  it('accepts a completely valid form', () => {
    const form = createValidForm()
    const errors = validateAboutContent(form)
    expect(errors).toEqual({})
  })

  it('rejects empty name or name exceeding limit', () => {
    const form = createValidForm()
    form.name = '   '
    expect(validateAboutContent(form).name).toContain('không được để trống')

    form.name = 'A'.repeat(ABOUT_LIMITS.name + 1)
    expect(validateAboutContent(form).name).toContain(`Không được vượt quá ${ABOUT_LIMITS.name} ký tự`)
  })

  it('validates facts count boundary (1 to 8)', () => {
    const form = createValidForm()
    form.facts = []
    expect(validateAboutContent(form).facts).toContain('phải có từ 1 đến 8 mục')

    form.facts = Array.from({ length: 9 }, (_, i) => ({
      icon: 'pi pi-calendar',
      label: `L${i}`,
      value: `V${i}`
    }))
    expect(validateAboutContent(form).facts).toContain('phải có từ 1 đến 8 mục')
  })

  it('rejects duplicate checklist IDs', () => {
    const form = createValidForm()
    form.studentInfo.checklist = [
      { id: 'step-1', label: 'Bước 1' },
      { id: 'step-1', label: 'Bước trùng' }
    ]
    const errors = validateAboutContent(form)
    expect(errors['studentInfo.checklist.1.id']).toContain('bị trùng lặp')
  })

  it('rejects invalid checklist ID format', () => {
    const form = createValidForm()
    form.studentInfo.checklist = [
      { id: 'Invalid_ID!', label: 'Bước 1' }
    ]
    const errors = validateAboutContent(form)
    expect(errors['studentInfo.checklist.0.id']).toContain('chỉ chứa chữ thường, số, gạch ngang')
  })

  it('rejects unallowed icon', () => {
    const form = createValidForm()
    form.facts[0].icon = 'pi pi-non-existent-icon'
    const errors = validateAboutContent(form)
    expect(errors['facts.0.icon']).toContain('Biểu tượng không hợp lệ')
  })

  it('rejects invalid or typo email', () => {
    const form = createValidForm()
    form.contact.email = 'test@gmail.con'
    const errors = validateAboutContent(form)
    expect(errors['contact.email']).toBeDefined()
  })

  it('validates contact phone number format and digits count', () => {
    const form = createValidForm()
    form.contact.phone = '12345'
    expect(validateAboutContent(form)['contact.phone']).toContain('ít nhất 8 chữ số')

    form.contact.phone = '0292 000 0000'
    expect(validateAboutContent(form)['contact.phone']).toBeUndefined()
  })

  it('generates next unique checklist ID', () => {
    expect(generateChecklistId([])).toBe('item-1')
    expect(generateChecklistId(['item-1', 'item-2'])).toBe('item-3')
  })

  it('correctly checks isAllowedIcon', () => {
    expect(isAllowedIcon('pi pi-calendar')).toBe(true)
    expect(isAllowedIcon('pi pi-users')).toBe(true)
    expect(isAllowedIcon('pi pi-unknown')).toBe(false)
  })
})
