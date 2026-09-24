<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import Menubar from 'primevue/menubar'
import Button from 'primevue/button'
import { useAuthStore } from '../stores/auth.js'

const authStore = useAuthStore()
const router = useRouter()

const adminMenu = [
  { label: 'Dashboard', icon: 'pi pi-home', route: '/dashboard' },
  { label: 'Sinh viên', icon: 'pi pi-users', route: '/students' },
  { label: 'Tạo tài khoản', icon: 'pi pi-user-plus', route: '/students/accounts' },
  { label: 'Doanh nghiệp', icon: 'pi pi-building', route: '/companies' },
  { label: 'Duyệt yêu cầu', icon: 'pi pi-inbox', route: '/placement-requests' },
  { label: 'Đổi mật khẩu', icon: 'pi pi-key', route: '/change-password' }
]

const userMenu = [
  { label: 'Hồ sơ của tôi', icon: 'pi pi-user', route: '/profile' },
  { label: 'Doanh nghiệp', icon: 'pi pi-building', route: '/companies' },
  { label: 'Đổi mật khẩu', icon: 'pi pi-key', route: '/change-password' }
]

const menuItems = computed(() => {
  const items = authStore.isAdmin ? adminMenu : userMenu
  return items.map((item) => ({
    label: item.label,
    icon: item.icon,
    command: () => router.push(item.route)
  }))
})

function onLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<template>
  <div class="main-layout">
    <Menubar :model="menuItems">
      <template #end>
        <span class="username">{{ authStore.user?.username ?? '' }}</span>
        <Button label="Đăng xuất" severity="secondary" text @click="onLogout" />
      </template>
    </Menubar>
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<style scoped>
.main-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.main-content {
  flex: 1;
  padding: 1rem 1.5rem;
}

.username {
  margin-right: 0.75rem;
  font-size: 0.95rem;
}
</style>
