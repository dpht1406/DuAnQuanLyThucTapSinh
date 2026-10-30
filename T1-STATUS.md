# T1-STATUS

REV = 4
STATE = DONE

## Bug Fixes from Review
- Finding R-001: FIXED ở REV 4, sửa ở `backend/src/StudentInternshipMgmt.Infrastructure/Services/CompanyAccountService.cs` (loại bỏ truy vấn DB lẻ tẻ trong vòng lặp, nạp trước danh sách candidate usernames qua ToHashSet, triệt tiêu 100% N+1 queries).
- Finding R-002: FIXED ở REV 4, sửa ở `frontend/src/layouts/MainLayout.vue` (xóa mục "Tài khoản DN" khỏi `userMenu` của sinh viên, giữ nguyên 4 mục ban đầu của role User theo đúng SPEC 3.1).
- Finding R-003: FIXED ở REV 4, sửa ở `backend/src/StudentInternshipMgmt.Api/Controllers/CompanyAccountsBulkController.cs` (thêm header `Cache-Control: no-store` cho endpoint `POST /api/companies/accounts/export`).
- Finding R-004: FIXED ở REV 4, sửa ở `frontend/src/views/companies/CompaniesAccountsView.vue` (thêm `:is-data-selectable="onCheckDataSelectable"` vào thẻ `<DataTable>`, vô hiệu hóa triệt để checkbox của hàng đã có tài khoản hoặc thiếu/sai email).
- Finding R-005: FIXED ở REV 4, sửa ở `frontend/src/views/companies/CompaniesAccountsView.vue` (reset `selectedCompanies.value = []` khi thay đổi bộ lọc tìm kiếm hoặc dropdown).

## Final Verification
- Dotnet Build: 0 Warning, 0 Error (4.7s).
- Dotnet Test (Solution): 323 Passed, 0 Failed, 1 Skipped (test có sẵn `StudentSearchTests`).
- NPM Test (Vitest): 173 Passed, 0 Failed (15 test files).
- NPM Build: Vite build production thành công (691ms).
- Check Labels: 0 orphan labels trên 48 file Vue.
- Check Contrast: 0 failures (11/11 cặp màu đạt chuẩn).
- QA Tab 3 Test: 53/53 test cases Passed (TEST REV 4: PASS).
- Final Report: `docs/company-accounts-bulk/FINAL-REPORT.md`.
