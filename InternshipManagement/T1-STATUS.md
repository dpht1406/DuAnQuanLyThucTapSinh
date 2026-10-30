# T1-STATUS

REV = 4
STATE = READY_FOR_REVIEW

## Bug Fixes from Review
- Finding R-001: FIXED ở REV 4, sửa ở `backend/src/StudentInternshipMgmt.Infrastructure/Services/CompanyAccountService.cs` (loại bỏ truy vấn DB lẻ tẻ trong vòng lặp, nạp trước danh sách candidate usernames qua ToHashSet, triệt tiêu 100% N+1 queries).
- Finding R-002: FIXED ở REV 4, sửa ở `frontend/src/layouts/MainLayout.vue` (xóa mục "Tài khoản DN" khỏi `userMenu` của sinh viên, giữ nguyên 4 mục ban đầu của role User theo đúng SPEC 3.1).
- Finding R-003: FIXED ở REV 4, sửa ở `backend/src/StudentInternshipMgmt.Api/Controllers/CompanyAccountsBulkController.cs` (thêm header `Cache-Control: no-store` cho endpoint `POST /api/companies/accounts/export`).
- Finding R-004: FIXED ở REV 4, sửa ở `frontend/src/views/companies/CompaniesAccountsView.vue` (thêm `:is-data-selectable="onCheckDataSelectable"` vào thẻ `<DataTable>`, vô hiệu hóa triệt để checkbox của hàng đã có tài khoản hoặc thiếu/sai email).
- Finding R-005: FIXED ở REV 4, sửa ở `frontend/src/views/companies/CompaniesAccountsView.vue` (reset `selectedCompanies.value = []` khi thay đổi bộ lọc tìm kiếm hoặc dropdown).

## Files changed
### Slice A (Backend)
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Accounts/CreateCompanyAccountsBulkDto.cs` (new)
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Dtos/CompanyDto.cs`
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Dtos/CompanyFilterDto.cs`
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Accounts/ICompanyAccountService.cs`
- `backend/src/StudentInternshipMgmt.Infrastructure/Services/CompanyAccountService.cs`
- `backend/src/StudentInternshipMgmt.Infrastructure/Services/CompanyService.cs`
- `backend/src/StudentInternshipMgmt.Api/Controllers/CompanyAccountsBulkController.cs` (new)
- `backend/tests/StudentInternshipMgmt.Infrastructure.Tests/CompanyAccountsBulkTests.cs` (new)

### Slice B (Frontend Plumbing)
- `frontend/src/api/companies.js`
- `frontend/src/router/index.js`
- `frontend/src/layouts/MainLayout.vue`
- `frontend/src/stores/accountResult.test.js`

### Slice C (Frontend UI & Validation)
- `frontend/src/utils/companyAccountValidation.js` (new)
- `frontend/src/utils/companyAccountValidation.test.js` (new)
- `frontend/src/views/companies/CompaniesAccountsView.vue` (new)
- `frontend/src/views/accounts/AccountResultView.vue`

## Note
Completed Bug Fix Round 1 for REV 4.
- All 5 review findings (R-001, R-002, R-003, R-004, R-005) fixed at the root.
- Re-tested:
  - Backend: 78/78 tests passed (`dotnet test --filter "FullyQualifiedName~Company"`).
  - Frontend: 9/9 unit tests passed (`npm test -- src/utils/companyAccountValidation.test.js src/stores/accountResult.test.js --run`).
  - Accessibility: `npm run check:labels` passed (0 orphan labels).
  - Contrast: `npm run check:contrast` passed (0 failures).
  - Frontend Build: `npm run build` succeeded.
- Tab 2 and Tab 3 please review and verify fixes on REV 4.
