import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth.js'
import MainLayout from '../layouts/MainLayout.vue'

function defaultRouteForRole(authStore) {
  return authStore.isAdmin ? '/dashboard' : '/profile'
}

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
        redirect: () => {
          const authStore = useAuthStore()
          return defaultRouteForRole(authStore)
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
        path: 'companies',
        name: 'companies',
        component: () => import('../views/companies/CompaniesView.vue'),
        meta: { requiresAuth: true }
      },
      {
        path: 'placement-requests',
        name: 'placement-requests',
        component: () => import('../views/placement-requests/PlacementRequestsView.vue'),
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
    return next(defaultRouteForRole(authStore))
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
    authStore.user?.role &&
    !roles.includes(authStore.user.role)
  ) {
    return next(defaultRouteForRole(authStore))
  }

  next()
})

export default router
