export const DEFAULT_PAGE_TITLE = 'QLTT - Quản lý sinh viên thực tập'

export function buildPageTitle(routeOrTitle) {
  const title = typeof routeOrTitle === 'string'
    ? routeOrTitle.trim()
    : routeOrTitle?.meta?.title?.trim()

  return title ? `${title} | QLTT` : DEFAULT_PAGE_TITLE
}
