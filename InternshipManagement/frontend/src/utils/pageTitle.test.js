import { describe, expect, it } from 'vitest'
import { buildPageTitle, DEFAULT_PAGE_TITLE } from './pageTitle.js'

describe('buildPageTitle', () => {
  it('adds the product name after a page title', () => {
    expect(buildPageTitle('Đăng nhập')).toBe('Đăng nhập | QLTT')
  })

  it('uses the default title when a route has no title', () => {
    expect(buildPageTitle({ meta: {} })).toBe(DEFAULT_PAGE_TITLE)
  })

  it('uses the child title from the merged metadata of a nested route', () => {
    const nestedRoute = {
      matched: [{ meta: { title: 'Quản lý sinh viên thực tập' } }, { meta: { title: 'Thêm sinh viên' } }],
      meta: { title: 'Thêm sinh viên' }
    }
    expect(buildPageTitle(nestedRoute)).toBe('Thêm sinh viên | QLTT')
  })

  it('uses the default title for an empty title', () => {
    expect(buildPageTitle('  ')).toBe(DEFAULT_PAGE_TITLE)
  })
})
