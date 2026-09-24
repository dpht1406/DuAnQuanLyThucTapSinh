<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { useAuthStore } from '../../stores/auth.js'

const authStore = useAuthStore()
const router = useRouter()

const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMessage = ref('')

const canSubmit = computed(
  () => username.value.trim() !== '' && password.value !== '' && !loading.value
)

async function onSubmit() {
  if (!canSubmit.value) return

  errorMessage.value = ''
  loading.value = true

  try {
    await authStore.login(username.value.trim(), password.value)
    await router.push('/')
  } catch (err) {
    errorMessage.value =
      err?.message?.trim() || 'Đăng nhập thất bại, vui lòng thử lại.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <Card class="login-card">
      <template #title>Đăng nhập</template>
      <template #content>
        <form class="login-form" @submit.prevent="onSubmit">
          <Message v-if="errorMessage" severity="error" :closable="false">
            {{ errorMessage }}
          </Message>

          <div class="field">
            <label for="username">Tên đăng nhập</label>
            <InputText
              id="username"
              v-model="username"
              autocomplete="username"
              class="w-full"
              :disabled="loading"
            />
          </div>

          <div class="field">
            <label for="password">Mật khẩu</label>
            <Password
              id="password"
              v-model="password"
              :feedback="false"
              toggle-mask
              autocomplete="current-password"
              input-class="w-full"
              class="w-full"
              :disabled="loading"
            />
          </div>

          <Button
            type="submit"
            label="Đăng nhập"
            class="w-full"
            :loading="loading"
            :disabled="!canSubmit"
          />
        </form>
      </template>
    </Card>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.login-card {
  width: 100%;
  max-width: 28rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.field label {
  font-weight: 500;
}

.w-full {
  width: 100%;
}
</style>
