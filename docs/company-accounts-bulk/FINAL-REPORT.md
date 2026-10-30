# Báo cáo Tổng kết Hoàn tất Chức năng: Tạo tài khoản doanh nghiệp hàng loạt

## 1. Tổng quan đã làm gì
Hệ thống quản lý thực tập sinh trước đây chỉ hỗ trợ tạo tài khoản đơn lẻ cho từng doanh nghiệp bằng nút chìa khóa trong danh sách. Quản trị viên (Admin) chưa có công cụ tạo tài khoản hàng loạt cho doanh nghiệp, dẫn đến việc phải thao tác thủ công nhiều lần khi có số lượng lớn doanh nghiệp hợp tác.

Trong đợt nâng cấp này, chúng tôi đã xây dựng hoàn chỉnh tính năng **Tạo tài khoản doanh nghiệp hàng loạt** (mô hình tương tự tính năng tạo tài khoản hàng loạt cho sinh viên):
- Bổ sung trang riêng cho Admin tại đường dẫn `/companies/accounts` để tra cứu, lọc và chọn doanh nghiệp cần tạo tài khoản.
- Hỗ trợ 2 chế độ tạo tài khoản:
  1. **Tạo theo danh sách chọn**: Cho phép Admin tích chọn $N$ doanh nghiệp cụ thể trên bảng.
  2. **Tạo theo bộ lọc**: Cho phép Admin tạo tài khoản cho toàn bộ doanh nghiệp khớp bộ lọc hiện tại (kèm hộp thoại xác nhận số lượng và phạm vi).
- Tự động kiểm tra và bỏ qua an toàn với các trường hợp: doanh nghiệp đã có tài khoản, doanh nghiệp chưa có email liên hệ hoặc email sai định dạng, doanh nghiệp không tồn tại, tên đăng nhập bị trùng.
- Quy tắc sinh tài khoản nhất quán:
  - Tên đăng nhập: `cty` + mã doanh nghiệp định dạng 4 chữ số (`cty0001`, `cty0025`,...).
  - Mật khẩu tạm thời: Chuỗi ngẫu nhiên an toàn sinh từ `PasswordGenerator`, mã hóa bằng thuật toán BCrypt khi lưu vào cơ sở dữ liệu.
  - Role: `Company`, cờ `MustChangePassword = true`, kích hoạt sẵn `IsActive = true`.
- Trang hiển thị kết quả sau khi tạo, cung cấp chức năng **Xuất danh sách tài khoản ra file CSV** (có mã hóa chống công thức độc hại Spreadsheet Injection và tiền tố UTF-8 BOM để mở tiếng Việt trên Microsoft Excel không bị lỗi font).
- Bảo mật nghiêm ngặt: Mật khẩu dạng văn bản thuần (plain text) chỉ xuất hiện duy nhất một lần trong phản hồi API và màn hình kết quả, tuyệt đối không lưu vào Database, LocalStorage, SessionStorage hay URL; tự động xóa khỏi bộ nhớ khi Admin rời khỏi trang.

---

## 2. Danh sách tệp đã thay đổi

### Backend (ASP.NET Core 8)
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Accounts/CreateCompanyAccountsBulkDto.cs` *(Mới)*: Định nghĩa các DTO yêu cầu và phản hồi tạo hàng loạt (`CreateCompanyAccountsRequestDto`, `CreatedCompanyAccountDto`, `SkippedCompanyAccountDto`, `CreateCompanyAccountsResultDto`, `ExportCompanyAccountsRequestDto`).
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Dtos/CompanyDto.cs`: Bổ sung thuộc tính `HasAccount` (bool).
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Dtos/CompanyFilterDto.cs`: Bổ sung thuộc tính `HasAccount` (bool?).
- `backend/src/StudentInternshipMgmt.Application/Features/Companies/Accounts/ICompanyAccountService.cs`: Bổ sung chữ ký phương thức `CreateAccountsBulkAsync` và `CreateAccountsBulkByFilterAsync`.
- `backend/src/StudentInternshipMgmt.Infrastructure/Services/CompanyService.cs`: Tách hàm lọc dùng chung `ApplyCompanyFilter`, cập nhật ánh xạ `HasAccount` không gây N+1 query.
- `backend/src/StudentInternshipMgmt.Infrastructure/Services/CompanyAccountService.cs`: Tách lõi xác thực dùng chung với chức năng tạo đơn lẻ; cài đặt tạo hàng loạt tối ưu truy vấn (pre-fetch candidate usernames tránh N+1); bắt `DbUpdateException` để xử lý tranh chấp đồng thời an toàn.
- `backend/src/StudentInternshipMgmt.Api/Controllers/CompanyAccountsBulkController.cs` *(Mới)*: Controller chuyên biệt tại `api/companies/accounts` phục vụ các API bulk create và export CSV, phân quyền nghiêm ngặt chỉ Admin, thiết lập header `Cache-Control: no-store`.
- `backend/tests/StudentInternshipMgmt.Infrastructure.Tests/CompanyAccountsBulkTests.cs` *(Mới)*: Bộ unit test cho các trường hợp nghiệp vụ, biên, băm mật khẩu, chống N+1 và export CSV an toàn.

### Frontend (Vue 3 + PrimeVue 4)
- `frontend/src/api/companies.js`: Bổ sung các hàm gọi API: `createCompanyAccountsBulk`, `createCompanyAccountsByFilter`, `exportCompanyAccountsCsv`.
- `frontend/src/router/index.js`: Cấu hình route `/companies/accounts` (meta `roles: ['Admin']`, tiêu đề "Tạo tài khoản doanh nghiệp").
- `frontend/src/layouts/MainLayout.vue`: Thêm mục menu "Tài khoản DN" (icon `pi pi-id-card`) ngay sau mục "Doanh nghiệp" ở cả thanh điều hướng desktop và drawer "Thêm" trên mobile cho Admin; giữ nguyên menu của Role User và Company.
- `frontend/src/utils/companyAccountValidation.js` *(Mới)*: Logic thuần kiểm tra điều kiện chọn doanh nghiệp (`isCompanySelectable`, `getCompanyAccountWarning`).
- `frontend/src/utils/companyAccountValidation.test.js` *(Mới)*: Vitest unit tests cho logic kiểm tra điều kiện chọn doanh nghiệp.
- `frontend/src/views/companies/CompaniesAccountsView.vue` *(Mới)*: Giao diện quản lý tạo tài khoản doanh nghiệp hàng loạt (ô tìm kiếm debounce 400ms, dropdown ngành nghề, dropdown trạng thái tài khoản, phân trang server, bảo vệ chống bấm đúp, hộp thoại xác nhận).
- `frontend/src/views/accounts/AccountResultView.vue`: Mở rộng thêm nhánh xử lý `kind === 'company-bulk'` với bảng tài khoản thành công, bảng lý do không tạo được, nút tải CSV, bảo vệ xác nhận trước khi rời trang nếu chưa tải CSV, xử lý F5 an toàn không bị trắng màn hình.
- `frontend/src/stores/accountResult.test.js`: Bổ sung test case cho nhánh `company-bulk`.

---

## 3. Các API Endpoints mới

| Phương thức | Đường dẫn API | Phân quyền | Header đặc biệt | Chức năng |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/companies/accounts/bulk` | Admin | `Cache-Control: no-store` | Tạo tài khoản cho danh sách mã doanh nghiệp (`companyIds: number[]`). Giới hạn tối đa 500 ID/lần. |
| `POST` | `/api/companies/accounts/bulk-by-filter` | Admin | `Cache-Control: no-store` | Tạo tài khoản cho tất cả doanh nghiệp khớp bộ lọc (`Search`, `Industry`, `HasAccount`). Giới hạn tối đa 500 bản ghi. |
| `POST` | `/api/companies/accounts/export` | Admin | `Cache-Control: no-store` | Nhận danh sách tài khoản vừa tạo và trả về file `text/csv` có UTF-8 BOM, mã hóa chống CSV Injection. |

---

## 4. Hướng dẫn sử dụng cho Quản trị viên (Admin)

1. **Truy cập chức năng**:
   - Đăng nhập tài khoản Admin vào hệ thống.
   - Trên thanh menu chính (Desktop) hoặc mục "Thêm" (Mobile), nhấp vào mục **"Tài khoản DN"** (biểu tượng thẻ ID).
2. **Lọc và tìm kiếm**:
   - Nhập tên doanh nghiệp vào ô **Tìm kiếm** (hệ thống tự động tìm sau 400ms dừng gõ).
   - Chọn **Ngành nghề** tương ứng từ danh sách gợi ý.
   - Chọn **Trạng thái tài khoản**: "Chưa có tài khoản" (khuyên dùng khi cần cấp mới) hoặc "Tất cả".
3. **Lựa chọn doanh nghiệp**:
   - Bảng hiển thị danh sách doanh nghiệp kèm cột trạng thái và email liên hệ.
   - Các doanh nghiệp đã có tài khoản hoặc thiếu email/email sai định dạng sẽ bị vô hiệu hóa checkbox (kèm nhãn cảnh báo đỏ/vàng).
   - Tích chọn một hoặc nhiều doanh nghiệp hợp lệ.
4. **Thực hiện tạo tài khoản**:
   - **Cách 1 (Theo lựa chọn)**: Nhấp nút *"Tạo tài khoản cho N doanh nghiệp đã chọn"*.
   - **Cách 2 (Theo bộ lọc)**: Nhấp nút *"Tạo tài khoản cho tất cả theo bộ lọc (~M doanh nghiệp)"*, xem kỹ số lượng trên hộp thoại xác nhận và bấm *"Tiếp tục"*.
5. **Xem kết quả và tải thông tin**:
   - Màn hình chuyển đến trang **Kết quả tạo tài khoản doanh nghiệp**.
   - Xem bảng danh sách tài khoản đã cấp (Tên DN, Tên đăng nhập, Mật khẩu tạm thời, Email liên hệ).
   - Nếu có doanh nghiệp bị bỏ qua, xem bảng *"Không tạo được"* kèm lý do cụ thể từng doanh nghiệp.
   - Nhấp nút **"Tải CSV"** để lưu file thông tin đăng nhập về máy và bàn giao cho doanh nghiệp qua kênh bảo mật nội bộ.
   - *Lưu ý*: Mật khẩu tạm chỉ hiển thị một lần duy nhất tại màn hình này.

---

## 5. Kết quả Kiểm thử & Đo lường chất lượng thực tế

Toàn bộ các số liệu dưới đây được đo lường thực tế trên môi trường máy trạm:

### 5.1. Backend
- **Lệnh build**: `dotnet build InternshipManagement\backend`
  - Kết quả: **Build Succeeded**
  - Số lượng Cảnh báo: **0 Warning**
  - Số lượng Lỗi: **0 Error**
  - Thời gian thực thi: **4.7 giây**
- **Lệnh test toàn bộ Solution**: `dotnet test InternshipManagement\backend`
  - `StudentInternshipMgmt.Infrastructure.Tests.dll`: **238 Passed**, **0 Failed**, **1 Skipped** *(Testcase có sẵn từ trước: `StudentSearchTests`)*
  - `StudentInternshipMgmt.Tests.dll`: **85 Passed**, **0 Failed**, **0 Skipped**
  - Tổng số test backend: **323 Passed**, **0 Failed**, **1 Skipped**

### 5.2. Frontend
- **Lệnh test toàn bộ Vitest**: `npm test -- --run`
  - Kết quả: **15/15 test files Passed (100%)**
  - Tổng số testcases: **173 Passed**, **0 Failed**
  - Thời gian thực thi: **6.15 giây**
- **Lệnh build production**: `npm run build`
  - Kết quả: **Vite build thành công (512 modules transformed)**
  - Thời gian build: **691 mili-giây**
- **Kiểm tra nhãn Form (Accessibility)**: `npm run check:labels`
  - Kết quả: **0 nhãn mồ côi**, **0 input thiếu nhãn** trên toàn bộ 48 file Vue.
- **Kiểm tra độ tương phản màu (WCAG)**: `npm run check:contrast`
  - Kết quả: **11/11 cặp màu ĐẠT chuẩn**, **0 lỗi**.

### 5.3. Kết quả QA độc lập (Tab 3)
- Tổng số Test Cases kiểm thử chi tiết theo đặc tả: **53 / 53 Passed (100%)**
- Kết luận của QA: **`TEST REV 4: PASS`** *(Đính kèm đầy đủ log và ảnh chụp bằng chứng tại thư mục `docs/company-accounts-bulk/evidence/`)*.

---

## 6. Danh sách các lỗi đã khắc phục qua các vòng rà soát

| Mã phát hiện | Người báo | Mức độ | Nội dung lỗi | Nguyên nhân gốc rễ | Giải pháp đã khắc phục | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **R-001** | Tab 2 | Cao | Vấn đề hiệu năng N+1 queries trong vòng lặp bulk create | Trong vòng lặp `foreach`, code gọi `Users.AnyAsync` cho từng công ty để kiểm tra tài khoản và trùng username. | Lấy `hasAccount` từ navigation property đã nạp qua `Include`; nạp trước candidate usernames vào `HashSet` in-memory. Triệt tiêu 100% N+1 queries. | **Đã sửa & Đóng** |
| **R-002** | Tab 2 | Cao | Menu "Tài khoản DN" bị lộ ở thanh điều hướng sinh viên (`userMenu`) | Chuỗi thay thế vô tình chèn mục vào cả `userMenu` của sinh viên. | Đã xóa mục khỏi `userMenu`, khôi phục nguyên vẹn 4 mục mặc định của sinh viên theo SPEC 3.1. | **Đã sửa & Đóng** |
| **R-003** | Tab 2 | Trung bình | Endpoint xuất file CSV thiếu header `Cache-Control: no-store` | Thiếu thiết lập header chống cache ở endpoint `/api/companies/accounts/export`. | Bổ sung `Response.Headers["Cache-Control"] = "no-store"` ngay đầu phương thức xử lý CSV. | **Đã sửa & Đóng** |
| **R-004** | Tab 2 | Trung bình | Checkbox các hàng không hợp lệ vẫn có thể bấm chọn trước khi bị lọc ngầm | DataTable chưa khai báo thuộc tính `:is-data-selectable`. | Bổ sung `:is-data-selectable="onCheckDataSelectable"` vào thẻ `<DataTable>`, vô hiệu hóa triệt để checkbox của hàng không hợp lệ. | **Đã sửa & Đóng** |
| **R-005** | Tab 2 | Thấp | Danh sách chọn không được làm mới khi Admin đổi điều kiện lọc | Thiếu bước reset mảng `selectedCompanies` khi các ref filter thay đổi. | Bổ sung `selectedCompanies.value = []` trong hàm `onFilterChange` và watcher `searchInput`. | **Đã sửa & Đóng** |
| **T-001** | Tab 3 | Trung bình | Giao diện sinh viên hiển thị menu Admin "Tài khoản DN" (TC-UI-02) | Đồng nhất với lỗi R-002 ở trên. | Đã khắc phục ở REV 4, QA Tab 3 đã chạy test lại trên Chrome Headless và xác nhận Pass. | **Đã sửa & Đóng** |

---

## 7. Các điểm lưu ý và điều chưa làm
- **Hệ thống không tự gửi email mật khẩu**: Theo đúng đặc tả SPEC 2.5 ("Không làm: Không gửi email, không đổi schema..."), hệ thống bảo mật không kích hoạt dịch vụ gửi mail tự động cho mật khẩu tạm thời. Admin chủ động tải file CSV và bàn giao theo quy trình bảo mật nội bộ của trường/viện.
- **1 Testcase backend Skipped**: Testcase `StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters` có sẵn từ trước trong mã nguồn dự án (thuộc phân hệ quản lý sinh viên), không phát sinh từ đợt phát triển này và không làm ảnh hưởng đến tính đúng đắn của toàn bộ hệ thống.
