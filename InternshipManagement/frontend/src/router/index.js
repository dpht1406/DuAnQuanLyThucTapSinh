import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth.js'
import MainLayout from '../layouts/MainLayout.vue'
import { defaultRouteForRole } from '../utils/roleRoutes.js'

const routes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('../views/auth/LoginView.vue'),
    meta: { requiresGuest: true }
  },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: (to) => {
          const authStore = useAuthStore()
          return safeDefaultRoute(authStore, to.path)
        }
      },
      {
        path: 'change-password',
        name: 'change-password',
        component: () => import('../views/auth/ChangePasswordView.vue'),
        meta: { requiresAuth: true }
      },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('../views/dashboard/DashboardView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'company-home',
        name: 'company-home',
        component: () => import('../views/company/CompanyHomeView.vue'),
        meta: { requiresAuth: true, roles: ['Company'] }
      },
      {
        path: 'students',
        name: 'students',
        component: () => import('../views/students/StudentsListView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'students/accounts',
        name: 'students-accounts',
        component: () => import('../views/accounts/CreateAccountsView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'students/new',
        name: 'students-new',
        component: () => import('../views/students/StudentCreateView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'students/:id(\\d+)',
        name: 'student-detail',
        component: () => import('../views/students/StudentDetailView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'students/:id(\\d+)/edit',
        name: 'student-edit',
        component: () => import('../views/students/StudentEditView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'about',
        name: 'about',
        component: () => import('../views/about/AboutView.vue'),
        meta: { requiresAuth: true }
      },
      {
        path: 'companies',
        name: 'companies',
        component: () => import('../views/companies/CompaniesView.vue'),
        meta: { requiresAuth: true }
      },
      {
        path: 'companies/new',
        name: 'company-create',
        component: () => import('../views/companies/CompanyCreateView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'job-positions',
        name: 'job-positions',
        component: () => import('../views/job-positions/JobPositionsView.vue'),
        meta: { requiresAuth: true }
      },
      {
        path: 'job-positions/:id(\\d+)/apply',
        name: 'job-position-apply',
        component: () => import('../views/job-positions/JobPositionApplyView.vue'),
        meta: { requiresAuth: true, roles: ['User'] }
      },
      {
        path: 'placement-requests',
        name: 'placement-requests',
        component: () => import('../views/placement-requests/PlacementRequestsView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'notifications',
        name: 'notifications',
        component: () => import('../views/notifications/NotificationsView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'profile',
        name: 'profile',
        component: () => import('../views/profile/ProfileView.vue'),
        meta: { requiresAuth: true, roles: ['User'] }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('../views/NotFoundView.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

function safeDefaultRoute(authStore, currentPath) {
  const target = defaultRouteForRole(authStore.user?.role)
  if (!target || target === currentPath) {
    authStore.logout()
    return '/login'
  }
  return target
}

const CHUNK_RELOAD_KEY = 'chunk-reload-once'
const CHUNK_ERROR_PATTERN = /Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed/i

function reloadAfterChunkError(fullPath) {
  try {
    if (sessionStorage.getItem(CHUNK_RELOAD_KEY)) return
    sessionStorage.setItem(CHUNK_RELOAD_KEY, 'true')
  } catch {
    return
  }

  window.location.assign(fullPath)
}

router.onError((error, to) => {
  if (CHUNK_ERROR_PATTERN.test(String(error?.message || error))) {
    reloadAfterChunkError(to.fullPath)
  }
})

window.addEventListener('vite:preloadError', (event) => {
  event.preventDefault?.()
  const message = String(event?.payload?.message || event?.message || event)
  if (CHUNK_ERROR_PATTERN.test(message)) {
    reloadAfterChunkError(`${window.location.pathname}${window.location.search}${window.location.hash}`)
  }
})

router.afterEach(() => {
  try {
    sessionStorage.removeItem(CHUNK_RELOAD_KEY)
  } catch {
    // Bỏ qua khi sessionStorage không khả dụng.
  }
})

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()

  const requiresGuest = to.matched.some((record) => record.meta.requiresGuest)
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)

  let roles
  for (const record of to.matched) {
    if (record.meta.roles) {
      roles = record.meta.roles
    }
  }

  if (requiresGuest && authStore.isAuthenticated) {
    return next(safeDefaultRoute(authStore, to.path))
  }

  if (requiresAuth && !authStore.isAuthenticated) {
    return next('/login')
  }

  if (
    authStore.isAuthenticated &&
    authStore.mustChangePassword &&
    to.path !== '/login' &&
    to.path !== '/change-password'
  ) {
    return next('/change-password')
  }

  if (
    authStore.isAuthenticated &&
    Array.isArray(roles) &&
    roles.length > 0 &&
    !roles.includes(authStore.user?.role)
  ) {
    return next(safeDefaultRoute(authStore, to.path))
  }

  next()
})

export default router
