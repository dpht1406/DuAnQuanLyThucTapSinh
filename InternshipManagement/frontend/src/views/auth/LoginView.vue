<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import Checkbox from 'primevue/checkbox'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { useAuthStore } from '../../stores/auth.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { validateSafeText } from '../../utils/emailValidation.js'

const authStore = useAuthStore()
const router = useRouter()

const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMessage = ref('')
const showPassword = ref(false)
// TODO: Logic ghi nhớ đăng nhập để sau.
const rememberMe = ref(false)

const canSubmit = computed(
  () => username.value.trim() !== '' && password.value !== '' && !loading.value
)

async function onSubmit() {
  if (!canSubmit.value) return

  const usernameError = validateSafeText(username.value, 100)
  const passwordError = validateSafeText(password.value, 300)
  if (usernameError || passwordError) {
    errorMessage.value = usernameError || passwordError
    return
  }

  errorMessage.value = ''
  loading.value = true

  try {
    await authStore.login(username.value.trim(), password.value)
    await router.push('/')
  } catch (err) {
    const responseMessage = extractErrorMessage(err, '')

    if (responseMessage === 'Tài khoản đã bị vô hiệu hóa.') {
      errorMessage.value = responseMessage
    } else if (err?.response?.status === 401) {
      errorMessage.value = 'Tên đăng nhập hoặc mật khẩu không đúng.'
    } else if (!err?.response) {
      errorMessage.value = 'Không kết nối được máy chủ. Vui lòng thử lại sau.'
    } else {
      errorMessage.value = extractErrorMessage(err)
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <section class="login-panel">
      <header class="brand">
        <div class="brand-mark" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M3 9.25 12 5l9 4.25L12 13 3 9.25Z" fill="currentColor" />
            <path d="M6.5 11.2v4.35c0 1.05 2.46 2.45 5.5 2.45s5.5-1.4 5.5-2.45V11.2L12 13.8l-5.5-2.6Z" fill="currentColor" />
            <path d="M21 9.5v5.25" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
          </svg>
        </div>
        <div>
          <div class="brand-name">Quản lý Thực tập</div>
          <div class="brand-subtitle">Cổng thông tin sinh viên</div>
        </div>
      </header>

      <form class="login-form" @submit.prevent="onSubmit">
        <div class="form-heading">
          <h1>Chào mừng trở lại</h1>
          <p>Đăng nhập để quản lý quá trình thực tập của bạn.</p>
        </div>

        <div class="fields">
          <div class="field">
            <label for="username">Tên đăng nhập</label>
            <IconField>
              <InputIcon class="pi pi-user" />
              <InputText
                id="username"
                v-model="username"
                maxlength="100"
                fluid
                autocomplete="username"
                placeholder="Nhập tên đăng nhập"
                :disabled="loading"
              />
            </IconField>
          </div>

          <div class="field">
            <label for="password">Mật khẩu</label>
            <IconField>
              <InputIcon class="pi pi-lock" />
              <InputText
                id="password"
                v-model="password"
                maxlength="300"
                fluid
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                placeholder="Nhập mật khẩu"
                :disabled="loading"
              />
              <InputIcon
                :class="showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'"
                class="password-toggle"
                role="button"
                tabindex="0"
                aria-label="Hiện hoặc ẩn mật khẩu"
                @click="showPassword = !showPassword"
                @keydown.enter.prevent="showPassword = !showPassword"
              />
            </IconField>
          </div>
        </div>

        <div class="remember-row">
          <Checkbox v-model="rememberMe" input-id="remember-me" binary :disabled="loading" />
          <label for="remember-me">Ghi nhớ đăng nhập</label>
        </div>

        <Message v-if="errorMessage" severity="error" :closable="false">
          {{ errorMessage }}
        </Message>

        <Button
          type="submit"
          label="Đăng nhập"
          icon="pi pi-arrow-right"
          icon-pos="right"
          class="submit-button"
          :loading="loading"
          :disabled="!canSubmit"
        />

        <p class="forgot-password">
          <span>Quên mật khẩu? </span><a href="#">Liên hệ quản trị viên</a>
        </p>
      </form>

      <footer class="login-footer">© 2026 Hệ thống Quản lý Sinh viên Thực tập</footer>
    </section>

    <aside class="feature-panel">
      <div class="decor-circle decor-circle-top" aria-hidden="true"></div>
      <div class="decor-circle decor-circle-bottom" aria-hidden="true"></div>
      <div class="decor-circle decor-circle-right" aria-hidden="true"></div>

      <div class="feature-content">
        <span class="feature-badge">Nền tảng thực tập số</span>
        <h2>Kết nối sinh viên với cơ hội thực tập</h2>
        <p class="feature-description">
          Theo dõi đơn vị tiếp nhận, tiến độ và đánh giá thực tập trong một hệ thống thống nhất.
        </p>

        <div class="stats-grid">
          <div class="stat-card">
            <span class="stat-label">Sinh viên thực tập</span>
            <strong>248</strong>
            <span class="stat-change">+12 tuần này</span>
          </div>
          <div class="stat-card">
            <span class="stat-label">Đơn vị tiếp nhận</span>
            <strong>36</strong>
            <span class="stat-change">+3 tháng này</span>
          </div>
        </div>

        <div class="progress-card">
          <div class="progress-heading">
            <strong>Tiến độ đợt thực tập học kỳ 1</strong>
            <span>72%</span>
          </div>
          <div class="progress-track"><div class="progress-value"></div></div>
        </div>
      </div>
    </aside>
  </div>
</template>

<style scoped>
:global(*) {
  box-sizing: border-box;
}

.login-page {
  min-height: 100vh;
  display: grid;
  grid-template-columns: 1fr 1fr;
  --login-primary: #1d4ed8;
  --login-text: #0f172a;
  --login-muted: #64748b;
  font-family: 'Inter', system-ui, 'Segoe UI', sans-serif;
}

.login-panel {
  position: relative;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  padding: 52px;
  overflow: hidden;
  background: #fff;
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
}

.brand-mark {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  border-radius: 10px;
  color: #fff;
  background: var(--login-primary);
}

.brand-mark svg {
  width: 21px;
  height: 21px;
}

.brand-name {
  color: var(--login-text);
  font-size: 15px;
  font-weight: 700;
}

.brand-subtitle {
  margin-top: 2px;
  color: var(--login-muted);
  font-size: 12px;
}

.login-form {
  width: 100%;
  max-width: 330px;
  margin: auto;
  color: var(--login-text);
}

.form-heading h1 {
  margin: 0;
  color: var(--login-text);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.25;
}

.form-heading p {
  margin: 8px 0 0;
  color: var(--login-muted);
  font-size: 14px;
  line-height: 1.5;
}

.fields {
  display: flex;
  flex-direction: column;
  gap: 20px;
  margin-top: 28px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field label {
  color: #1e293b;
  font-size: 13px;
  font-weight: 500;
}

.login-form :deep(.p-iconfield) {
  width: 100%;
}

.login-form :deep(.p-inputtext) {
  height: 40px;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  background: #f8fafc;
  color: #1e293b;
  font-size: 14px;
  box-shadow: none;
}

.login-form :deep(.p-inputtext::placeholder) {
  color: #94a3b8;
}

.login-form :deep(.p-inputtext:enabled:focus) {
  border-color: var(--login-primary);
  box-shadow: 0 0 0 3px rgba(29, 78, 216, 0.15);
}

.login-form :deep(.p-inputicon) {
  color: #94a3b8;
}

.password-toggle {
  cursor: pointer;
  pointer-events: auto;
}

.remember-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 16px;
  color: #334155;
  font-size: 13px;
}

.login-form :deep(.p-checkbox) {
  width: 16px;
  height: 16px;
}

.login-form :deep(.p-checkbox-box) {
  width: 16px;
  height: 16px;
  border-radius: 4px;
}

.login-form :deep(.p-message) {
  margin-top: 18px;
}

.submit-button {
  width: 100%;
  height: 42px;
  margin-top: 20px;
  border: 0;
  border-radius: 8px;
  background: var(--login-primary);
  font-size: 14px;
  font-weight: 600;
  box-shadow: 0 6px 14px rgba(29, 78, 216, 0.25);
}

.submit-button:not(:disabled):hover {
  background: #1e40af;
}

.submit-button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.forgot-password {
  margin: 20px 0 0;
  color: var(--login-muted);
  font-size: 13px;
  text-align: center;
}

.forgot-password a {
  color: var(--login-primary);
  font-weight: 600;
  text-decoration: none;
}

.login-footer {
  color: #94a3b8;
  font-size: 12px;
}

.feature-panel {
  position: relative;
  display: flex;
  align-items: center;
  min-height: 100vh;
  overflow: hidden;
  color: #fff;
  background: linear-gradient(135deg, #2f6df0 0%, #1d4ed8 45%, #0f1e5c 100%);
}

.decor-circle {
  position: absolute;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.1);
  pointer-events: none;
}

.decor-circle-top {
  top: -150px;
  left: -140px;
  width: 300px;
  height: 300px;
}

.decor-circle-bottom {
  bottom: -210px;
  left: -170px;
  width: 380px;
  height: 380px;
  background: rgba(255, 255, 255, 0.08);
}

.decor-circle-right {
  top: 42%;
  right: -75px;
  width: 150px;
  height: 150px;
  background: rgba(255, 255, 255, 0.12);
}

.feature-content {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 460px;
  padding-left: 66px;
}

.feature-badge {
  display: inline-flex;
  padding: 4px 12px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.15);
  font-size: 12px;
}

.feature-content h2 {
  max-width: 440px;
  margin: 16px 0 0;
  font-size: 34px;
  font-weight: 700;
  line-height: 1.25;
}

.feature-description {
  margin: 16px 0 0;
  color: rgba(255, 255, 255, 0.75);
  font-size: 14px;
  line-height: 1.6;
}

.stats-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin-top: 32px;
}

.stat-card,
.progress-card {
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(8px);
}

.stat-card {
  display: flex;
  flex-direction: column;
  padding: 16px 18px;
}

.stat-label {
  color: rgba(255, 255, 255, 0.7);
  font-size: 12px;
}

.stat-card strong {
  margin-top: 7px;
  font-size: 28px;
  line-height: 1;
}

.stat-change {
  margin-top: 8px;
  color: #86efac;
  font-size: 12px;
}

.progress-card {
  margin-top: 12px;
  padding: 16px 18px;
}

.progress-heading {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  font-size: 13px;
}

.progress-track {
  height: 6px;
  margin-top: 14px;
  overflow: hidden;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.2);
}

.progress-value {
  width: 72%;
  height: 100%;
  border-radius: inherit;
  background: #93c5fd;
}

@media (max-width: 899px) {
  .login-page {
    display: block;
  }

  .login-panel {
    min-height: 100vh;
    padding: 32px 24px;
  }

  .login-form {
    max-width: 360px;
  }

  .feature-panel {
    display: none;
  }
}
</style>
