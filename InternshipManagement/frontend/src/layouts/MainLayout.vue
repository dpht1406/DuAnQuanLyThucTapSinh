<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import dayjs from '../utils/dayjs.js'
import Avatar from 'primevue/avatar'
import Badge from 'primevue/badge'
import Button from 'primevue/button'
import Drawer from 'primevue/drawer'
import Menu from 'primevue/menu'
import Popover from 'primevue/popover'
import { useAuthStore } from '../stores/auth.js'
import { getNotifications, getUnreadCount, markAllNotificationsRead, markNotificationRead } from '../api/notifications.js'

const authState = useAuthStore()
const isAdmin = computed(() => {
  const role = Array.isArray(authState.user?.role) ? authState.user.role[0] : authState.user?.role
  return String(role ?? '').trim().toLowerCase() === 'admin' || authState.isAdmin === true
})
const isCompany = computed(() => {
  const role = Array.isArray(authState.user?.role) ? authState.user.role[0] : authState.user?.role
  return String(role ?? '').trim().toLowerCase() === 'company'
})
const authStore = new Proxy(authState, {
  get(target, property) {
    return property === 'isAdmin' ? isAdmin.value : Reflect.get(target, property, target)
  }
})
const router = useRouter()
const route = useRoute()
const hideMobileChrome = computed(() => route.meta?.hideMobileChrome === true)
const adminMenu = [{ label: 'Dashboard', icon: 'pi pi-home', route: '/dashboard' }, { label: 'Sinh viên', icon: 'pi pi-users', route: '/students' }, { label: 'Tạo tài khoản', icon: 'pi pi-user-plus', route: '/students/accounts' }, { label: 'Doanh nghiệp', icon: 'pi pi-building', route: '/companies' }, { label: 'Tài khoản DN', icon: 'pi pi-id-card', route: '/companies/accounts' }, { label: 'Vị trí thực tập', icon: 'pi pi-briefcase', route: '/job-positions' }, { label: 'Duyệt yêu cầu', icon: 'pi pi-inbox', route: '/placement-requests' }, { label: 'Giới thiệu', icon: 'pi pi-info-circle', route: '/about' }]
const userMenu = [{ label: 'Hồ sơ của tôi', icon: 'pi pi-user', route: '/profile' }, { label: 'Doanh nghiệp', icon: 'pi pi-building', route: '/companies' }, { label: 'Vị trí thực tập', icon: 'pi pi-briefcase', route: '/job-positions' }, { label: 'Giới thiệu', icon: 'pi pi-info-circle', route: '/about' }]
const companyMenu = [{ label: 'Trang chủ', icon: 'pi pi-home', route: '/company-home' }, { label: 'Vị trí tuyển dụng', icon: 'pi pi-briefcase', route: '/company/job-positions' }, { label: 'Đơn ứng tuyển', icon: 'pi pi-inbox', route: '/company/placement-requests' }]
const menuItems = computed(() => (isAdmin.value ? adminMenu : isCompany.value ? companyMenu : userMenu))
const mobileTabs = computed(() => isAdmin.value ? [{ label: 'Dashboard', icon: 'pi pi-home', route: '/dashboard' }, { label: 'Sinh viên', icon: 'pi pi-users', route: '/students' }, { label: 'Doanh nghiệp', icon: 'pi pi-building', route: '/companies' }, { label: 'Vị trí', icon: 'pi pi-briefcase', route: '/job-positions' }, { label: 'Duyệt', icon: 'pi pi-inbox', route: '/placement-requests' }] : isCompany.value ? [{ label: 'Trang chủ', icon: 'pi pi-home', route: '/company-home' }, { label: 'Vị trí tuyển dụng', icon: 'pi pi-briefcase', route: '/company/job-positions' }, { label: 'Đơn ứng tuyển', icon: 'pi pi-inbox', route: '/company/placement-requests' }] : [{ label: 'Hồ sơ', icon: 'pi pi-user', route: '/profile' }, { label: 'Doanh nghiệp', icon: 'pi pi-building', route: '/companies' }, { label: 'Vị trí', icon: 'pi pi-briefcase', route: '/job-positions' }])
const mobileMoreItems = computed(() => isAdmin.value ? [{ label: 'Tạo tài khoản', icon: 'pi pi-user-plus', route: '/students/accounts' }, { label: 'Tài khoản DN', icon: 'pi pi-id-card', route: '/companies/accounts' }, { label: 'Giới thiệu', icon: 'pi pi-info-circle', route: '/about' }] : isCompany.value ? [] : [{ label: 'Giới thiệu', icon: 'pi pi-info-circle', route: '/about' }])
const pickActiveRoute = (items) => items.filter((i) => route.path === i.route || route.path.startsWith(`${i.route}/`)).sort((a, b) => b.route.length - a.route.length)[0]?.route
const activeRoute = computed(() => pickActiveRoute(menuItems.value))
const mobileActiveRoute = computed(() => pickActiveRoute([...mobileTabs.value, ...mobileMoreItems.value]))

const initialSidebarMode = () => {
  if (typeof window !== 'undefined' && window.innerWidth >= 768 && window.innerWidth < 1200) {
    return 'collapsed'
  }
  return 'expanded'
}
const sidebarMode = ref(initialSidebarMode())
const mobileNavVisible = ref(false)

const roleLabel = computed(() => {
  if (isAdmin.value) return 'Quản trị viên'
  if (isCompany.value) return 'Doanh nghiệp'
  return 'Sinh viên'
})

const drawerGroupLabel = computed(() => {
  if (isAdmin.value) return 'QUẢN TRỊ HỆ THỐNG'
  if (isCompany.value) return 'DOANH NGHIỆP'
  return 'SINH VIÊN'
})

function toggleSidebar() {
  sidebarMode.value = sidebarMode.value === 'collapsed' ? 'expanded' : 'collapsed'
}

function navigateTo(routePath) {
  mobileNavVisible.value = false
  router.push(routePath)
}

const userMenuRef = ref(null)
const userMenuOpen = ref(false)
const notificationPopover = ref(null)
const notifications = ref([])
const unreadCount = ref(0)
const notificationsLoading = ref(false)
const mobileMoreVisible = ref(false)
const mobileNotificationsVisible = ref(false)
let notificationPollTimer = null
const usernameInitial = computed(() => (authStore.user?.username || 'U').charAt(0).toUpperCase())
const isActive = (item) => item.route === activeRoute.value
const isMobileActive = (item) => item.route === mobileActiveRoute.value
const isMobileMoreActive = () => mobileMoreVisible.value || mobileMoreItems.value.some((item) => item.route === mobileActiveRoute.value)
const userMenuItems = computed(() => [{ label: 'Đổi mật khẩu', icon: 'pi pi-key', command: () => router.push({ name: 'change-password', query: { from: route.fullPath } }) }, { separator: true }, { label: 'Đăng xuất', icon: 'pi pi-sign-out', class: 'danger-menu-item', command: onLogout }])

function syncDesktopNavLabels() {
  document.querySelectorAll('.desktop-nav .nav-link').forEach((link) => {
    const label = link.querySelector('span:last-child')?.textContent?.trim()
    if (label) {
      link.setAttribute('title', label)
      link.setAttribute('aria-label', label)
    }
  })
}
watch([menuItems, sidebarMode], () => nextTick(syncDesktopNavLabels), { flush: 'post' })

function onLogout() { authStore.logout(); router.push('/login') }
function toggleUserMenu(event) { userMenuRef.value?.toggle(event) }
function openMobileMore() { mobileMoreVisible.value = true }
function closeMobileDrawers() { mobileMoreVisible.value = false; mobileNotificationsVisible.value = false; mobileNavVisible.value = false }
function goTo(routePath) { closeMobileDrawers(); router.push(routePath) }
function toggleNotifications(event) { notificationPopover.value?.toggle(event) }
function isRejection(notification) { const kind = String(notification.type || notification.notificationType || '').toLowerCase(); return kind.includes('reject') || kind.includes('return') || kind.includes('back') }
function notificationIcon(notification) { return isRejection(notification) ? 'pi pi-replay' : 'pi pi-exclamation' }
function notificationTone(notification) { return isRejection(notification) ? 'warning' : 'danger' }
async function refreshUnreadCount() { if (!isAdmin.value || document.hidden) return; try { unreadCount.value = Number((await getUnreadCount())?.unreadCount ?? 0) } catch { } }
async function loadNotifications() { notificationsLoading.value = true; try { notifications.value = (await getNotifications({ pageNumber: 1, pageSize: 10 }))?.items ?? [] } catch { notifications.value = [] } finally { notificationsLoading.value = false } }
async function openNotification(notification) { if (!notification.isRead) { try { await markNotificationRead(notification.id); notification.isRead = true; unreadCount.value = Math.max(0, unreadCount.value - 1) } catch { } } notificationPopover.value?.hide(); mobileNotificationsVisible.value = false; if (notification.studentId != null) router.push({ name: 'student-detail', params: { id: notification.studentId } }) }
async function markAllRead() { try { await markAllNotificationsRead(); notifications.value = notifications.value.map((item) => ({ ...item, isRead: true })); unreadCount.value = 0 } catch { } }
function stopNotificationPolling() { if (notificationPollTimer) { clearInterval(notificationPollTimer); notificationPollTimer = null } }
function onDocumentVisibilityChange() { stopNotificationPolling(); if (!isAdmin.value || document.hidden) return; refreshUnreadCount(); notificationPollTimer = setInterval(refreshUnreadCount, 30000) }
onMounted(() => { nextTick(syncDesktopNavLabels); if (isAdmin.value) { document.addEventListener('visibilitychange', onDocumentVisibilityChange); onDocumentVisibilityChange() } })
onBeforeUnmount(() => { stopNotificationPolling(); document.removeEventListener('visibilitychange', onDocumentVisibilityChange) })
const hideMobileTabs = computed(() => hideMobileChrome.value || route.name === 'company-placement-request-detail')
</script>

<template>
  <div class="main-layout" :class="{ 'hide-mobile-chrome': hideMobileChrome }">
    <a class="skip-link" href="#main-content">Bỏ qua đến nội dung chính</a>

    <!-- Desktop Left Sidebar (>= 768px) -->
    <aside
      class="desktop-sidebar"
      :class="{ 'is-collapsed': sidebarMode === 'collapsed' }"
      aria-label="Điều hướng chính"
    >
      <div class="sidebar-header">
        <router-link to="/" class="sidebar-brand" aria-label="Trang chủ QLTT">
          <span class="brand-icon pi pi-graduation-cap" aria-hidden="true" />
          <span v-if="sidebarMode !== 'collapsed'" class="brand-name">QLTT</span>
        </router-link>
      </div>

      <div class="sidebar-content">
        <div v-if="sidebarMode !== 'collapsed'" class="nav-group-label">ĐIỀU HƯỚNG</div>
        <nav class="desktop-nav" aria-label="Điều hướng chính">
          <router-link
            v-for="item in menuItems"
            :key="item.route"
            :to="item.route"
            :class="['nav-link', { active: isActive(item) }]"
            :aria-current="isActive(item) ? 'page' : undefined"
            :title="item.label"
            :aria-label="item.label"
          >
            <span :class="item.icon" class="nav-icon" aria-hidden="true" />
            <span class="nav-text">{{ item.label }}</span>
          </router-link>
        </nav>
      </div>

      <div class="sidebar-footer">
        <button
          v-if="sidebarMode !== 'collapsed'"
          type="button"
          class="sidebar-user-card"
          aria-haspopup="menu"
          :aria-expanded="userMenuOpen"
          aria-label="Tài khoản người dùng"
          @click="toggleUserMenu"
        >
          <Avatar :label="usernameInitial" shape="circle" class="sidebar-avatar" />
          <div class="sidebar-user-info">
            <span class="sidebar-username">{{ authStore.user?.username ?? '' }}</span>
            <span class="sidebar-role">{{ roleLabel }}</span>
          </div>
          <span class="pi pi-chevron-down sidebar-caret" aria-hidden="true" />
        </button>
        <button
          v-else
          type="button"
          class="sidebar-user-avatar-btn"
          aria-haspopup="menu"
          :aria-expanded="userMenuOpen"
          aria-label="Tài khoản người dùng"
          @click="toggleUserMenu"
        >
          <Avatar :label="usernameInitial" shape="circle" class="sidebar-avatar" />
        </button>
      </div>
    </aside>

    <!-- Mobile Header (< 768px) -->
    <header v-if="!hideMobileChrome" class="mobile-header">
      <div class="mobile-header-left">
        <button
          type="button"
          class="icon-btn mobile-hamburger"
          aria-label="Mở/Đóng menu điều hướng"
          title="Mở/Đóng menu điều hướng"
          @click="mobileNavVisible = true"
        >
          <span class="pi pi-bars" aria-hidden="true" />
        </button>
        <router-link to="/" class="brand" aria-label="Trang chủ QLTT">
          <span class="brand-icon pi pi-graduation-cap" aria-hidden="true" />
          <span class="brand-name">QLTT</span>
        </router-link>
      </div>
      <div class="mobile-header-right">
        <div v-if="authStore.isAdmin" class="notification-trigger">
          <button
            type="button"
            class="icon-btn mobile-bell"
            :aria-label="unreadCount > 0 ? `Thông báo, ${unreadCount} chưa đọc` : 'Thông báo'"
            title="Thông báo"
            @click="mobileNotificationsVisible = true"
          >
            <span class="pi pi-bell" aria-hidden="true" />
            <Badge v-if="unreadCount > 0" :value="unreadCount > 9 ? (unreadCount > 99 ? '99+' : unreadCount) : ''" severity="danger" class="notification-badge" />
          </button>
        </div>
        <button
          type="button"
          class="mobile-avatar-btn"
          aria-label="Tài khoản của tôi"
          aria-haspopup="menu"
          :aria-expanded="userMenuOpen"
          @click="toggleUserMenu"
        >
          <Avatar :label="usernameInitial" shape="circle" class="mobile-avatar" />
        </button>
      </div>
    </header>

    <!-- Right column (Desktop) / Main column (Mobile) -->
    <div class="layout-body">
      <!-- Desktop Topbar (>= 768px) -->
      <header class="desktop-topbar">
        <div class="topbar-left">
          <button
            type="button"
            class="icon-btn topbar-hamburger"
            :aria-label="sidebarMode === 'collapsed' ? 'Mở rộng menu' : 'Thu gọn menu'"
            :title="sidebarMode === 'collapsed' ? 'Mở rộng menu' : 'Thu gọn menu'"
            :aria-expanded="sidebarMode !== 'collapsed'"
            @click="toggleSidebar"
          >
            <span class="pi pi-bars" aria-hidden="true" />
          </button>

          <div class="topbar-breadcrumb">
            <span class="breadcrumb-root">Bảng điều khiển</span>
            <template v-if="route.meta?.title">
              <span class="breadcrumb-separator" aria-hidden="true">/</span>
              <span class="breadcrumb-current">{{ route.meta.title }}</span>
            </template>
          </div>
        </div>

        <div class="topbar-right">
          <div v-if="authStore.isAdmin" class="topbar-notification">
            <button
              type="button"
              class="icon-btn topbar-bell"
              :aria-label="unreadCount > 0 ? `Thông báo, ${unreadCount} chưa đọc` : 'Thông báo'"
              title="Thông báo"
              @click="toggleNotifications"
            >
              <span class="pi pi-bell" aria-hidden="true" />
              <Badge v-if="unreadCount > 0" :value="unreadCount > 9 ? (unreadCount > 99 ? '99+' : unreadCount) : ''" severity="danger" class="notification-badge" />
            </button>
            <Popover ref="notificationPopover" @show="loadNotifications">
              <section class="notification-panel" aria-label="Danh sách thông báo">
                <div class="notification-panel-header">
                  <strong>Thông báo</strong>
                  <Button label="Đánh dấu tất cả đã đọc" text size="small" :disabled="unreadCount === 0" @click="markAllRead" />
                </div>
                <p v-if="notificationsLoading" class="notification-empty">Đang tải thông báo...</p>
                <p v-else-if="notifications.length === 0" class="notification-empty">Chưa có thông báo nào</p>
                <button
                  v-for="notification in notifications"
                  :key="notification.id"
                  type="button"
                  class="notification-item"
                  :class="{ unread: !notification.isRead }"
                  @click="openNotification(notification)"
                >
                  <span class="notification-type" :class="notificationTone(notification)">
                    <span :class="notificationIcon(notification)" aria-hidden="true" />
                  </span>
                  <span class="notification-copy">
                    <strong>{{ notification.title }}</strong>
                    <span>{{ notification.message }}</span>
                    <time>{{ dayjs(notification.createdAt).fromNow() }}</time>
                  </span>
                  <span v-if="!notification.isRead" class="notification-unread-dot" aria-hidden="true" />
                </button>
                <router-link
                  v-if="notifications.length"
                  to="/notifications"
                  class="notification-footer"
                  @click="notificationPopover?.hide()"
                >
                  Xem tất cả thông báo
                </router-link>
              </section>
            </Popover>
          </div>

          <div v-if="authStore.isAdmin" class="topbar-divider" aria-hidden="true" />

          <button
            type="button"
            class="topbar-user"
            aria-haspopup="menu"
            :aria-expanded="userMenuOpen"
            aria-label="Tài khoản người dùng"
            @click="toggleUserMenu"
          >
            <Avatar :label="usernameInitial" shape="circle" class="topbar-avatar" />
            <div class="topbar-user-info">
              <span class="topbar-username">{{ authStore.user?.username ?? '' }}</span>
              <span class="topbar-role">{{ roleLabel }}</span>
            </div>
            <span class="pi pi-chevron-down topbar-caret" aria-hidden="true" />
          </button>
        </div>
      </header>

      <!-- Main Content -->
      <main id="main-content" class="main-content" tabindex="-1">
        <router-view />
      </main>
    </div>

    <!-- User Popup Menu (shared for desktop sidebar, topbar, and mobile header) -->
    <Menu ref="userMenuRef" :model="userMenuItems" :popup="true" @show="userMenuOpen = true" @hide="userMenuOpen = false" />

    <!-- Mobile Navigation Drawer (Left) -->
    <Drawer
      v-model:visible="mobileNavVisible"
      position="left"
      class="mobile-nav-drawer"
      :modal="true"
      :dismissable="true"
      :close-on-escape="true"
      :show-close-icon="false"
    >
      <template #header>
        <div class="mobile-drawer-header-inner">
          <router-link to="/" class="brand" aria-label="Trang chủ QLTT" @click="mobileNavVisible = false">
            <span class="brand-icon pi pi-graduation-cap" aria-hidden="true" />
            <span class="brand-name">QLTT</span>
          </router-link>
          <button
            type="button"
            class="icon-btn drawer-close-btn"
            aria-label="Đóng menu điều hướng"
            title="Đóng menu điều hướng"
            @click="mobileNavVisible = false"
          >
            <span class="pi pi-times" aria-hidden="true" />
          </button>
        </div>
      </template>

      <div class="mobile-drawer-content">
        <div class="mobile-role-label">{{ drawerGroupLabel }}</div>
        <nav class="mobile-nav-menu" aria-label="Điều hướng chính di động">
          <button
            v-for="item in menuItems"
            :key="item.route"
            type="button"
            class="mobile-nav-item"
            :class="{ active: isActive(item) }"
            :aria-current="isActive(item) ? 'page' : undefined"
            @click="navigateTo(item.route)"
          >
            <span :class="item.icon" class="mobile-nav-icon" aria-hidden="true" />
            <span class="mobile-nav-text">{{ item.label }}</span>
            <span v-if="isActive(item)" class="mobile-active-dot" aria-hidden="true" />
          </button>
        </nav>
      </div>

      <template #footer>
        <div class="mobile-drawer-footer">
          <div class="mobile-drawer-user-row">
            <Avatar :label="usernameInitial" shape="circle" class="drawer-avatar" />
            <div class="drawer-user-info">
              <span class="drawer-username">{{ authStore.user?.username ?? '' }}</span>
              <span class="drawer-role">{{ roleLabel }}</span>
            </div>
            <span class="pi pi-chevron-down drawer-caret" aria-hidden="true" />
          </div>
          <div class="mobile-drawer-user-actions">
            <button
              type="button"
              class="mobile-drawer-action-btn"
              @click="mobileNavVisible = false; router.push({ name: 'change-password', query: { from: route.fullPath } })"
            >
              <span class="pi pi-key" aria-hidden="true" />
              <span>Đổi mật khẩu</span>
            </button>
            <button
              type="button"
              class="mobile-drawer-action-btn danger"
              @click="mobileNavVisible = false; onLogout()"
            >
              <span class="pi pi-sign-out" aria-hidden="true" />
              <span>Đăng xuất</span>
            </button>
          </div>
        </div>
      </template>
    </Drawer>

    <!-- Mobile Notifications Drawer (Bottom) -->
    <Drawer
      v-model:visible="mobileNotificationsVisible"
      position="bottom"
      class="mobile-drawer notification-drawer"
      :style="{ height: 'min(80vh, 620px)' }"
      @show="loadNotifications"
    >
      <section class="notification-panel mobile-panel" aria-label="Danh sách thông báo">
        <div class="notification-panel-header">
          <strong>Thông báo</strong>
          <Button label="Đánh dấu tất cả đã đọc" text size="small" :disabled="unreadCount === 0" @click="markAllRead" />
        </div>
        <p v-if="notificationsLoading" class="notification-empty">Đang tải thông báo...</p>
        <p v-else-if="notifications.length === 0" class="notification-empty">Chưa có thông báo nào</p>
        <button
          v-for="notification in notifications"
          :key="notification.id"
          type="button"
          class="notification-item"
          :class="{ unread: !notification.isRead }"
          @click="openNotification(notification)"
        >
          <span class="notification-type" :class="notificationTone(notification)">
            <span :class="notificationIcon(notification)" aria-hidden="true" />
          </span>
          <span class="notification-copy">
            <strong>{{ notification.title }}</strong>
            <span>{{ notification.message }}</span>
            <time>{{ dayjs(notification.createdAt).fromNow() }}</time>
          </span>
        </button>
        <router-link
          v-if="notifications.length"
          to="/notifications"
          class="notification-footer"
          @click="mobileNotificationsVisible = false"
        >
          Xem tất cả thông báo
        </router-link>
      </section>
    </Drawer>
  </div>
</template>

<style scoped>
.main-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  width: 100%;
  max-width: 100vw;
  overflow-x: clip;
  box-sizing: border-box;
}

@media (min-width: 768px) {
  .main-layout {
    flex-direction: row;
    align-items: stretch;
  }
}

.skip-link {
  position: absolute;
  top: 8px;
  left: 8px;
  z-index: 200;
  padding: 10px 14px;
  color: var(--ui-surface);
  background: var(--ui-primary-800);
  border-radius: 6px;
  transform: translateY(-150%);
}
.skip-link:focus-visible {
  transform: translateY(0);
}

.layout-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.main-content {
  flex: 1;
  background: var(--page-bg);
  width: 100%;
  max-width: 100vw;
  overflow-x: hidden;
  box-sizing: border-box;
}

/* Shared icon button */
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  padding: 0;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--surface);
  color: var(--text-h);
  cursor: pointer;
  position: relative;
  flex-shrink: 0;
  transition: var(--ui-transition);
}
.icon-btn:hover {
  background: var(--surface-soft);
}
.icon-btn .pi {
  font-size: 15px;
}

/* Brand */
.brand, .sidebar-brand {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: var(--text-h);
  text-decoration: none;
  font-size: 20px;
  font-weight: 700;
}
.brand-icon {
  display: grid;
  place-items: center;
  width: 36px;
  height: 36px;
  color: white;
  background: var(--primary);
  border-radius: 10px;
  font-size: 17px;
  flex-shrink: 0;
}
.brand-name {
  color: var(--text-h);
  font-size: 18px;
  font-weight: 700;
}

/* Desktop Sidebar */
.desktop-sidebar {
  position: sticky;
  top: 0;
  align-self: flex-start;
  height: 100vh;
  height: 100dvh;
  overflow-y: auto;
  flex-shrink: 0;
  width: 240px;
  min-width: 240px;
  background: var(--surface);
  border-right: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  z-index: 90;
  transition: width 0.15s ease, min-width 0.15s ease;
}
.desktop-sidebar.is-collapsed {
  width: 64px;
  min-width: 64px;
}

.sidebar-header {
  height: 72px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  box-sizing: border-box;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}
.desktop-sidebar.is-collapsed .sidebar-header {
  height: 72px;
  padding: 0;
  justify-content: center;
  border-bottom: 1px solid var(--border);
}

.sidebar-content {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 16px 12px;
  box-sizing: border-box;
}
.desktop-sidebar.is-collapsed .sidebar-content {
  padding: 16px 8px;
}

.nav-group-label {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--text);
  letter-spacing: 0.5px;
  padding: 0 8px 8px;
  margin-bottom: 4px;
}

.desktop-nav {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.desktop-nav .nav-link {
  display: flex;
  align-items: center;
  gap: 12px;
  height: 40px;
  padding: 0 12px;
  border-radius: 8px;
  color: var(--text);
  text-decoration: none;
  font-size: 14px;
  font-weight: 500;
  position: relative;
  transition: var(--ui-transition);
  box-sizing: border-box;
}
.desktop-nav .nav-link:hover {
  background: var(--surface-soft);
  color: var(--text-h);
}
.desktop-nav .nav-link.active {
  background: var(--primary-tint);
  color: var(--primary);
  font-weight: 600;
}
.desktop-nav .nav-link.active::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: var(--primary);
  border-radius: 3px 0 0 3px;
}
.desktop-nav .nav-icon {
  font-size: 16px;
  flex-shrink: 0;
}

.desktop-sidebar.is-collapsed .desktop-nav .nav-link {
  justify-content: center;
  padding: 0;
  width: 44px;
  height: 40px;
  margin: 0 auto;
}
.desktop-sidebar.is-collapsed .desktop-nav .nav-link .nav-text {
  display: none;
}

.sidebar-footer {
  padding: 12px;
  border-top: 1px solid var(--border);
  box-sizing: border-box;
  margin-top: auto;
  flex-shrink: 0;
}
.desktop-sidebar.is-collapsed .sidebar-footer {
  padding: 12px 0;
  display: flex;
  justify-content: center;
}

.sidebar-user-card {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: var(--text-h);
  box-sizing: border-box;
  transition: var(--ui-transition);
}
.sidebar-user-card:hover {
  background: var(--surface-soft);
}
.sidebar-user-card .sidebar-avatar,
.sidebar-user-avatar-btn .sidebar-avatar {
  width: 32px;
  height: 32px;
  color: var(--primary);
  background: var(--primary-tint);
  font-size: 13px;
  font-weight: 600;
  flex-shrink: 0;
}
.sidebar-user-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
  flex: 1;
}
.sidebar-username {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-h);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  line-height: 1.25;
}
.sidebar-role {
  font-size: 11px;
  color: var(--text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  line-height: 1.25;
}
.sidebar-caret {
  font-size: 11px;
  color: var(--text);
  margin-left: auto;
}
.sidebar-user-avatar-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  padding: 0;
  background: transparent;
  border: 0;
  border-radius: 50%;
  cursor: pointer;
}
.sidebar-user-avatar-btn:hover {
  opacity: 0.85;
}

/* Desktop Topbar */
.desktop-topbar {
  position: sticky;
  top: 0;
  z-index: 80;
  height: 72px;
  background: var(--surface);
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  box-sizing: border-box;
}
.topbar-left {
  display: flex;
  align-items: center;
  gap: 16px;
}
.topbar-breadcrumb {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}
.breadcrumb-root {
  color: var(--text);
  font-weight: 500;
}
.breadcrumb-separator {
  color: var(--border);
  font-weight: 400;
}
.breadcrumb-current {
  color: var(--text-h);
  font-weight: 700;
}
.topbar-right {
  display: flex;
  align-items: center;
  gap: 16px;
}
.topbar-divider {
  width: 1px;
  height: 28px;
  background: var(--border);
}
.topbar-user {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 4px 8px;
  background: transparent;
  border: 0;
  border-radius: 8px;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: var(--text-h);
  transition: var(--ui-transition);
}
.topbar-user:hover {
  background: var(--surface-soft);
}
.topbar-user .topbar-avatar {
  width: 34px;
  height: 34px;
  color: var(--primary);
  background: var(--primary-tint);
  font-size: 13px;
  font-weight: 600;
}
.topbar-user-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.topbar-username {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-h);
  line-height: 1.25;
}
.topbar-role {
  font-size: 11px;
  color: var(--text);
  line-height: 1.25;
}
.topbar-caret {
  font-size: 11px;
  color: var(--text);
  margin-left: 2px;
}

/* Notifications */
.notification-trigger, .topbar-notification {
  position: relative;
}
.notification-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  min-width: 8px;
  height: 8px;
  padding: 0;
  border: 2px solid var(--surface);
  border-radius: 999px;
  font-size: 10px;
  pointer-events: none;
}
.notification-badge:deep(.p-badge-label:empty) {
  display: none;
}
.notification-badge:deep(.p-badge-label:not(:empty)) {
  min-width: 18px;
  height: 16px;
  padding: 0 4px;
  line-height: 16px;
}
.notification-panel {
  width: min(380px, calc(100vw - 32px));
}
.notification-panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border);
}
.notification-panel-header strong {
  color: var(--text-h);
  font-size: 16px;
}
.notification-panel-header :deep(.p-button) {
  padding: 0;
  color: var(--primary);
  font-size: 12px;
}
.notification-item {
  position: relative;
  display: flex;
  width: 100%;
  gap: 12px;
  padding: 13px 8px;
  text-align: left;
  color: inherit;
  background: transparent;
  border: 0;
  border-bottom: 1px solid var(--border);
  cursor: pointer;
}
.notification-item:hover, .notification-item.unread {
  background: var(--primary-tint);
}
.notification-type {
  flex: 0 0 30px;
  display: grid;
  place-items: center;
  width: 30px;
  height: 30px;
  color: white;
  border-radius: 50%;
}
.notification-type.danger {
  background: var(--danger);
}
.notification-type.warning {
  background: var(--warning);
}
.notification-copy {
  display: grid;
  min-width: 0;
  gap: 4px;
}
.notification-copy strong {
  color: var(--text-h);
  font-size: 14px;
}
.notification-copy > span {
  display: -webkit-box;
  overflow: hidden;
  color: var(--text);
  font-size: 13px;
  line-height: 1.45;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 3;
}
.notification-copy time {
  color: var(--text);
  font-size: 12px;
}
.notification-unread-dot {
  flex: 0 0 7px;
  width: 7px;
  height: 7px;
  margin-top: 5px;
  background: var(--primary);
  border-radius: 50%;
}
.notification-empty {
  margin: 24px 0;
  color: var(--text);
  text-align: center;
  font-size: 14px;
}
.notification-footer {
  display: block;
  padding-top: 13px;
  color: var(--primary);
  text-align: center;
  text-decoration: none;
  font-size: 13px;
  font-weight: 600;
}

/* Mobile Header */
.mobile-header {
  display: none;
}

/* Mobile Left Drawer */
:deep(.mobile-nav-drawer.p-drawer) {
  width: 80vw !important;
  max-width: 320px !important;
  background: var(--surface);
}
:deep(.mobile-nav-drawer .p-drawer-header) {
  padding: 14px 16px;
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
}
:deep(.mobile-nav-drawer .p-drawer-content) {
  padding: 0;
  display: flex;
  flex-direction: column;
}
:deep(.mobile-nav-drawer .p-drawer-footer) {
  padding: 0;
  border-top: 1px solid var(--border);
  margin-top: auto;
}

.mobile-drawer-header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}
.mobile-drawer-content {
  flex: 1;
  overflow-y: auto;
  padding: 12px 0;
}
.mobile-role-label {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--text);
  letter-spacing: 0.5px;
  padding: 4px 16px 8px;
}
.mobile-nav-menu {
  display: flex;
  flex-direction: column;
}
.mobile-nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  height: 44px;
  padding: 0 16px;
  background: transparent;
  border: 0;
  color: var(--text-h);
  font-size: 14px;
  font-weight: 500;
  text-align: left;
  cursor: pointer;
  font: inherit;
  position: relative;
  width: 100%;
  box-sizing: border-box;
  transition: var(--ui-transition);
}
.mobile-nav-item:hover {
  background: var(--surface-soft);
}
.mobile-nav-item.active {
  background: var(--primary-tint);
  color: var(--primary);
  font-weight: 600;
}
.mobile-nav-item.active::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: var(--primary);
}
.mobile-nav-icon {
  font-size: 16px;
  flex-shrink: 0;
}
.mobile-nav-text {
  flex: 1;
}
.mobile-active-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--primary);
  flex-shrink: 0;
}

.mobile-drawer-footer {
  padding: 12px 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  background: var(--surface);
}
.mobile-drawer-user-row {
  display: flex;
  align-items: center;
  gap: 10px;
}
.drawer-avatar {
  width: 32px;
  height: 32px;
  color: var(--primary);
  background: var(--primary-tint);
  font-size: 13px;
  font-weight: 600;
  flex-shrink: 0;
}
.drawer-user-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
  flex: 1;
}
.drawer-username {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-h);
  line-height: 1.25;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.drawer-role {
  font-size: 11px;
  color: var(--text);
  line-height: 1.25;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.drawer-caret {
  font-size: 11px;
  color: var(--text);
}
.mobile-drawer-user-actions {
  display: flex;
  gap: 8px;
  width: 100%;
}
.mobile-drawer-action-btn {
  flex: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  height: 36px;
  padding: 0 8px;
  background: transparent;
  border: 1px solid var(--border);
  border-radius: 8px;
  font-size: 12px;
  font-weight: 500;
  color: var(--text-h);
  cursor: pointer;
  font: inherit;
  transition: var(--ui-transition);
}
.mobile-drawer-action-btn:hover {
  background: var(--surface-soft);
}
.mobile-drawer-action-btn.danger {
  color: var(--danger);
}
.mobile-drawer-action-btn .pi {
  font-size: 13px;
}

/* Notification Drawer */
.notification-drawer :deep(.p-drawer-content) {
  overflow-y: auto;
}
.notification-drawer .notification-panel-header {
  position: sticky;
  top: 0;
  padding-top: 2px;
  background: var(--surface);
}
.mobile-panel {
  width: 100%;
}
.mobile-panel .notification-item {
  padding-left: 0;
  padding-right: 0;
}

/* User Menu styling */
:deep(.danger-menu-item .p-menu-item-link) {
  color: var(--danger) !important;
}
:deep(.danger-menu-item .p-menu-item-icon) {
  color: var(--danger) !important;
}

/* A11y focus rings */
:is(.icon-btn, .sidebar-user-card, .sidebar-user-avatar-btn, .topbar-user, .mobile-drawer-action-btn, .mobile-nav-item, .mobile-avatar-btn):focus-visible {
  outline: 2px solid var(--ui-primary-600);
  outline-offset: 2px;
}

/* Responsive Media Queries */
@media (max-width: 767px) {
  .desktop-sidebar, .desktop-topbar {
    display: none !important;
  }
  .mobile-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    height: 60px;
    padding: 0 16px;
    background: var(--surface);
    border-bottom: 1px solid var(--border);
    width: 100%;
    max-width: 100vw;
    box-sizing: border-box;
  }
  .mobile-header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .mobile-header-right {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .mobile-avatar-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    background: transparent;
    border: 0;
    border-radius: 50%;
    cursor: pointer;
  }
  .mobile-avatar-btn .mobile-avatar {
    width: 32px;
    height: 32px;
    color: var(--primary);
    background: var(--primary-tint);
    font-size: 13px;
    font-weight: 600;
  }
  .main-content {
    padding-bottom: 0 !important;
  }
  .hide-mobile-chrome .mobile-header {
    display: none !important;
  }
}

@media (min-width: 768px) {
  .mobile-header, :deep(.mobile-nav-drawer) {
    display: none !important;
  }
}
</style>
