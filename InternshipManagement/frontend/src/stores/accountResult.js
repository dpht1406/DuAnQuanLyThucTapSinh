import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAccountResultStore = defineStore('accountResult', () => {
  const kind = ref(null)
  const payload = ref(null)

  function set(newKind, newPayload) {
    kind.value = newKind
    payload.value = newPayload
  }

  function clear() {
    kind.value = null
    payload.value = null
  }

  return {
    kind,
    payload,
    set,
    clear
  }
})
