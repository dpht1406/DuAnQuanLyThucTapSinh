# Đánh giá mã nguồn chức năng "Tạo tài khoản doanh nghiệp hàng loạt" (TAB 2 - Senior Reviewer)

## Thông tin đợt rà soát
- **REV**: 4
- **Ngày rà soát**: 2026-10-26
- **Người rà soát**: TAB 2 (Senior Reviewer)
- **Tài liệu đối chiếu**: `docs/company-accounts-bulk/SPEC.md`
- **Trạng thái T1-STATUS**: `REV = 4`, `STATE = READY_FOR_REVIEW`

---

## Danh sách phát hiện và Kết quả tái rà soát REV 4

### R-001
- **Mức**: Cao
- **Trạng thái**: Đã đóng (FIXED ở REV 4)
- **Vị trí**: `backend/src/StudentInternshipMgmt.Infrastructure/Services/CompanyAccountService.cs:154-159, 176, 204-205, 225`
- **Mô tả**: Vấn đề hiệu năng nghiêm trọng - N+1 Query trong vòng lặp tạo tài khoản hàng loạt (`CreateAccountsBulkAsync`).
- **Kịch bản gây lỗi ban đầu**:
  Vòng lặp `foreach (var companyId in distinctIds)` gọi `_db.Users.AnyAsync` ở cả dòng kiểm tra `CompanyId` và dòng kiểm tra `Username`, tạo ra tới 1,000 queries vào DB cho lô 500 công ty.
- **Kết quả kiểm tra REV 4**:
  - Dòng 154-159: Tab 1 đã nạp trước toàn bộ danh sách `candidateUsernames` vào `HashSet<string>` (`existingUsernames`) chỉ bằng 1 câu truy vấn SQL duy nhất trước vòng lặp.
  - Dòng 176: Đã loại bỏ hoàn toàn `await _db.Users.AnyAsync(u => u.CompanyId == company.Id...)`, tái sử dụng dữ liệu nạp sẵn qua `Include(c => c.User)` an toàn.
  - Dòng 204-205: Kiểm tra `existingUsernames.Contains(username)` trong bộ nhớ O(1), triệt tiêu hoàn toàn truy vấn DB đơn lẻ.
  - Dòng 225: Bổ sung `existingUsernames.Add(username)` khi tạo thành công.
  - Sửa đúng tận gốc, không còn N+1 queries.

---

### R-002
- **Mức**: Cao
- **Trạng thái**: Đã đóng (FIXED ở REV 4)
- **Vị trí**: `frontend/src/layouts/MainLayout.vue:32`
- **Mô tả**: Vi phạm phân quyền và đặc tả menu: mục "Tài khoản DN" bị thêm nhầm vào menu của sinh viên (`userMenu`).
- **Kịch bản gây lỗi ban đầu**:
  Sinh viên nhìn thấy "Tài khoản DN" trên desktop menu, bấm vào bị router guard chặn và redirect. SPEC 3.1 yêu cầu menu role User không đổi.
- **Kết quả kiểm tra REV 4**:
  Mục `{ label: 'Tài khoản DN', icon: 'pi pi-id-card', route: '/companies/accounts' }` đã được xóa sạch khỏi `userMenu` (dòng 32). Menu sinh viên giữ nguyên 4 mục ban đầu. Menu Admin vẫn có đầy đủ mục này trên desktop và mobile more items.
  Sửa đúng đặc tả SPEC 3.1.

---

### R-003
- **Mức**: Trung bình
- **Trạng thái**: Đã đóng (FIXED ở REV 4)
- **Vị trí**: `backend/src/StudentInternshipMgmt.Api/Controllers/CompanyAccountsBulkController.cs:90-93`
- **Mô tả**: Thiếu header `Cache-Control: no-store` ở endpoint xuất file CSV tài khoản (`POST /api/companies/accounts/export`).
- **Kịch bản gây lỗi ban đầu**:
  File CSV chứa plain password có nguy cơ bị lưu trong cache của proxy hoặc trình duyệt.
- **Kết quả kiểm tra REV 4**:
  Đã bổ sung `Response.Headers["Cache-Control"] = "no-store";` kèm kiểm tra `Response != null` an toàn ngay đầu phương thức `ExportAccounts` trước khi trả file.
  Đảm bảo dữ liệu mật khẩu tạm không bị lưu đệm.

---

### R-004
- **Mức**: Trung bình
- **Trạng thái**: Đã đóng (FIXED ở REV 4)
- **Vị trí**: `frontend/src/views/companies/CompaniesAccountsView.vue:124-127, 287`
- **Mô tả**: Thiếu thuộc tính `:is-data-selectable` trên `<DataTable>`, dẫn đến checkbox của các hàng không đủ điều kiện vẫn bấm được trên UI trước khi bị lọc.
- **Kịch bản gây lỗi ban đầu**:
  Người dùng bấm được checkbox của hàng không hợp lệ gây giật giao diện; checkbox "Chọn tất cả" trên header cột tính toán sai.
- **Kết quả kiểm tra REV 4**:
  Đã khai báo hàm `onCheckDataSelectable` (dòng 124-127) trích xuất dữ liệu an toàn và gắn `:is-data-selectable="onCheckDataSelectable"` vào thẻ `<DataTable>` (dòng 287). Checkbox của các dòng đã có tài khoản hoặc thiếu/sai email bị vô hiệu hóa triệt để theo cơ chế của PrimeVue.

---

### R-005
- **Mức**: Thấp
- **Trạng thái**: Đã đóng (FIXED ở REV 4)
- **Vị trí**: `frontend/src/views/companies/CompaniesAccountsView.vue:109, 117`
- **Mô tả**: Khi người dùng thay đổi bộ lọc (Search, Industry, HasAccount), mảng `selectedCompanies` không được reset, dẫn đến số lượng N trên nút vẫn giữ các doanh nghiệp đã chọn từ bộ lọc trước.
- **Kịch bản gây lỗi ban đầu**:
  Nút hiển thị số N của các doanh nghiệp đã chọn ở bộ lọc cũ đã biến mất khỏi bảng, có nguy cơ tạo nhầm tài khoản ngoài tầm nhìn của admin.
- **Kết quả kiểm tra REV 4**:
  Đã bổ sung `selectedCompanies.value = []` trong `onFilterChange()` (dòng 109) và callback debounce của `watch(searchInput)` (dòng 117). Khi chuyển trang (`onPage`), danh sách chọn vẫn được bảo toàn.

---

## Kết luận đợt rà soát
REVIEW REV 4: PASS
