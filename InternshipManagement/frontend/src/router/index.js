import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth.js'
import MainLayout from '../layouts/MainLayout.vue'
import { defaultRouteForRole } from '../utils/roleRoutes.js'
import { getPasswordChangeRedirect } from '../utils/authNavigation.js'
import { buildPageTitle } from '../utils/pageTitle.js'

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
        path: 'company/job-positions',
        name: 'company-job-positions',
        component: () => import('../views/company/CompanyJobPositionsView.vue'),
        meta: { requiresAuth: true, roles: ['Company'] }
      },
      {
        path: 'company/placement-requests',
        name: 'company-placement-requests',
        component: () => import('../views/company/CompanyPlacementRequestsView.vue'),
        meta: { requiresAuth: true, roles: ['Company'] }
      },
      {
        path: 'company/placement-requests/:id(\\d+)',
        name: 'company-placement-request-detail',
        component: () => import('../views/company/CompanyPlacementRequestDetailView.vue'),
        meta: { requiresAuth: true, roles: ['Company'] }
      },
      {
        path: 'company/placement-requests/:id(\\d+)/invite',
        name: 'company-placement-request-invite',
        component: () => import('../views/company/CompanyPlacementRequestInviteView.vue'),
        meta: { requiresAuth: true, roles: ['Company'], hideMobileChrome: true }
      },
      {
        path: 'company/placement-requests/:id(\\d+)/reject',
        name: 'company-placement-request-reject',
        component: () => import('../views/company/CompanyPlacementRequestRejectView.vue'),
        meta: { requiresAuth: true, roles: ['Company'], hideMobileChrome: true }
      },
      {
        path: 'company/placement-requests/:id(\\d+)/result',
        name: 'company-placement-request-result',
        component: () => import('../views/company/CompanyPlacementRequestResultView.vue'),
        meta: { requiresAuth: true, roles: ['Company'], hideMobileChrome: true }
      },
      {
        path: 'company/job-positions/new',
        name: 'company-job-position-create',
        component: () => import('../views/company/CompanyJobPositionFormView.vue'),
        meta: { requiresAuth: true, roles: ['Company'], hideMobileChrome: true }
      },
      {
        path: 'company/job-positions/:id(\\d+)/edit',
        name: 'company-job-position-edit',
        component: () => import('../views/company/CompanyJobPositionFormView.vue'),
        meta: { requiresAuth: true, roles: ['Company'], hideMobileChrome: true }
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
        path: 'account-result',
        name: 'account-result',
        component: () => import('../views/accounts/AccountResultView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'students/import',
        name: 'students-import',
        component: () => import('../views/students/StudentImportView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
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
        path: 'students/:id(\\d+)/status',
        name: 'student-status',
        component: () => import('../views/students/StudentStatusView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'students/:id(\\d+)/assign-company',
        name: 'student-assign-company',
        component: () => import('../views/students/StudentAssignCompanyView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'about',
        name: 'about',
        component: () => import('../views/about/AboutView.vue'),
        meta: { requiresAuth: true }
      },
      {
        path: 'about/edit',
        name: 'about-edit',
        component: () => import('../views/about/AboutEditView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'companies',
        name: 'companies',
        component: () => import('../views/companies/CompaniesView.vue'),
        meta: { requiresAuth: true, roles: ['Admin', 'User'] }
      },
      {
        path: 'companies/accounts',
        name: 'companies-accounts',
        component: () => import('../views/companies/CompaniesAccountsView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'companies/new',
        name: 'company-create',
        component: () => import('../views/companies/CompanyCreateView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'companies/:id(\\d+)/edit',
        name: 'company-edit',
        component: () => import('../views/companies/CompanyEditView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'companies/:id(\\d+)/account',
        name: 'company-account',
        component: () => import('../views/companies/CompanyAccountView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'companies/:companyId(\\d+)/job-positions/new',
        name: 'admin-job-position-create',
        component: () => import('../views/job-positions/AdminJobPositionFormView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'job-positions/:id(\\d+)/edit',
        name: 'admin-job-position-edit',
        component: () => import('../views/job-positions/AdminJobPositionFormView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'job-positions',
        name: 'job-positions',
        component: () => import('../views/job-positions/JobPositionsView.vue'),
        meta: { requiresAuth: true, roles: ['Admin', 'User'] }
      },
      {
        path: 'job-positions/:id(\\d+)/apply',
        name: 'job-position-apply',
        component: () => import('../views/job-positions/JobPositionApplyView.vue'),
        meta: { requiresAuth: true, roles: ['User'] }
      },
      {
        path: 'job-positions/:id(\\d+)/reject',
        name: 'job-position-reject',
        component: () => import('../views/job-positions/JobPositionRejectView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'placement-requests',
        name: 'placement-requests',
        component: () => import('../views/placement-requests/PlacementRequestsView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'] }
      },
      {
        path: 'placement-requests/:id(\\d+)/confirm-result',
        name: 'placement-request-confirm-result',
        component: () => import('../views/placement-requests/PlacementRequestConfirmResultView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
      },
      {
        path: 'placement-requests/:id(\\d+)/cancel-accepted',
        name: 'placement-request-cancel-accepted',
        component: () => import('../views/placement-requests/PlacementRequestCancelAcceptedView.vue'),
        meta: { requiresAuth: true, roles: ['Admin'], hideMobileChrome: true }
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

const routeTitles = {
  login: 'Đăng nhập',
  'change-password': 'Đổi mật khẩu',
  dashboard: 'Bảng điều khiển',
  'company-home': 'Trang chủ doanh nghiệp',
  'company-job-positions': 'Vị trí tuyển dụng',
  'company-placement-requests': 'Yêu cầu thực tập',
  'company-placement-request-detail': 'Chi tiết yêu cầu thực tập',
  'company-placement-request-invite': 'Mời phỏng vấn',
  'company-placement-request-reject': 'Từ chối yêu cầu thực tập',
  'company-placement-request-result': 'Kết quả yêu cầu thực tập',
  'company-job-position-create': 'Tạo vị trí tuyển dụng',
  'company-job-position-edit': 'Chỉnh sửa vị trí tuyển dụng',
  students: 'Danh sách sinh viên',
  'students-accounts': 'Tạo tài khoản sinh viên',
  'account-result': 'Kết quả tạo tài khoản',
  'students-import': 'Nhập danh sách sinh viên',
  'students-new': 'Thêm sinh viên',
  'student-detail': 'Chi tiết sinh viên',
  'student-edit': 'Chỉnh sửa sinh viên',
  'student-status': 'Cập nhật trạng thái sinh viên',
  'student-assign-company': 'Phân công doanh nghiệp',
  about: 'Giới thiệu',
  'about-edit': 'Chỉnh sửa giới thiệu',
  companies: 'Danh sách doanh nghiệp',
  'companies-accounts': 'Tạo tài khoản doanh nghiệp',
  'company-create': 'Thêm doanh nghiệp',
  'company-edit': 'Chỉnh sửa doanh nghiệp',
  'company-account': 'Tài khoản doanh nghiệp',
  'admin-job-position-create': 'Tạo vị trí thực tập',
  'admin-job-position-edit': 'Chỉnh sửa vị trí thực tập',
  'job-positions': 'Danh sách vị trí thực tập',
  'job-position-apply': 'Ứng tuyển thực tập',
  'job-position-reject': 'Từ chối ứng tuyển',
  'placement-requests': 'Danh sách yêu cầu thực tập',
  'placement-request-confirm-result': 'Xác nhận kết quả thực tập',
  'placement-request-cancel-accepted': 'Hủy phân công đã xác nhận',
  notifications: 'Thông báo',
  profile: 'Hồ sơ cá nhân',
  'not-found': 'Không tìm thấy trang'
}

function applyRouteTitles(records) {
  for (const record of records) {
    const title = routeTitles[record.name] ?? (record.path === '/' ? 'Quản lý sinh viên thực tập' : undefined)
    if (title) record.meta = { ...record.meta, title }
    if (record.children) applyRouteTitles(record.children)
  }
}

applyRouteTitles(routes)

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

router.afterEach((to) => {
  document.title = buildPageTitle(to)
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

  const passwordChangeRedirect = getPasswordChangeRedirect(
    authStore.isAuthenticated,
    authStore.mustChangePassword,
    to.path
  )
  if (passwordChangeRedirect) {
    return next(passwordChangeRedirect)
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
