import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAccountResultStore } from './accountResult.js'

describe('accountResult store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('starts with empty state', () => {
    const store = useAccountResultStore()
    expect(store.kind).toBeNull()
    expect(store.payload).toBeNull()
  })

  it('sets kind and payload for company', () => {
    const store = useAccountResultStore()
    store.set('company', { username: 'cty_abc', temporaryPassword: 'pwd', mode: 'create' })
    expect(store.kind).toBe('company')
    expect(store.payload).toEqual({ username: 'cty_abc', temporaryPassword: 'pwd', mode: 'create' })
  })

  it('sets kind and payload for company-bulk', () => {
    const store = useAccountResultStore()
    const payload = {
      successCount: 2,
      skippedCount: 1,
      created: [{ companyId: 1, companyName: 'CTY A', username: 'cty0001', temporaryPassword: 'pwd', contactEmail: 'a@example.com' }],
      skipped: [{ companyId: 2, companyName: 'CTY B', reason: 'Doanh nghiệp này đã có tài khoản.' }]
    }
    store.set('company-bulk', payload)
    expect(store.kind).toBe('company-bulk')
    expect(store.payload).toEqual(payload)
  })

  it('clears state', () => {
    const store = useAccountResultStore()
    store.set('student-reset', { studentCode: 'SV001', temporaryPassword: 'pwd' })
    expect(store.kind).toBe('student-reset')
    store.clear()
    expect(store.kind).toBeNull()
    expect(store.payload).toBeNull()
  })
})
