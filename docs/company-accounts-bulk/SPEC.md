# Đặc tả: Tạo tài khoản doanh nghiệp hàng loạt (admin)

## 1. Phân quyền
- Mọi endpoint mới chỉ cho role Admin. Không đăng nhập → 401. Role Company hoặc User → 403.
- Trang frontend chỉ Admin vào được (meta roles: ['Admin']); role khác bị chuyển về trang mặc định của role đó như các route admin khác.

## 2. Backend

### 2.1 Danh sách công ty
- CompanyDto thêm HasAccount (bool): công ty đã có User role Company hay chưa.
- CompanyFilterDto thêm HasAccount (bool?): null = tất cả, true = đã có, false = chưa có.
- Không được tạo N+1 query. Các API và test công ty hiện có phải chạy y như cũ.

### 2.2 Tạo hàng loạt theo danh sách Id
POST /api/companies/accounts/bulk
Body: { "companyIds": [1,2,3] }
- Danh sách rỗng hoặc null → 400, thông báo tiếng Việt.
- Quá 500 Id → 400. Id trùng nhau trong danh sách chỉ xử lý một lần. Id không tồn tại → vào danh sách bỏ qua với lý do "Không tìm thấy doanh nghiệp.".
- Mỗi công ty xử lý độc lập, kết quả:
  - Tạo được: Username = "cty" + Id (D4), mật khẩu ngẫu nhiên (PasswordGenerator), role Company, IsActive = true, MustChangePassword = true, lưu BCrypt hash.
  - Bỏ qua (không tạo) với lý do tiếng Việt cụ thể: đã có tài khoản; thiếu ContactEmail; ContactEmail sai định dạng (dùng đúng quy tắc kiểm tra hiện có của CompanyAccountService); Username trùng với tài khoản khác.
- Một công ty bị bỏ qua hoặc lỗi KHÔNG được làm hỏng các công ty còn lại. Không được tạo 2 tài khoản cho cùng 1 công ty kể cả khi 2 admin bấm cùng lúc (dựa vào ràng buộc DB hiện có, xử lý DbUpdateException an toàn).
- Response 200: { successCount, skippedCount, created: [{ companyId, companyName, username, temporaryPassword, contactEmail }], skipped: [{ companyId, companyName, reason }] } bọc trong ApiResponse như các API khác.
- Header Cache-Control: no-store.

### 2.3 Tạo hàng loạt theo bộ lọc
POST /api/companies/accounts/bulk-by-filter
Body: bộ lọc giống danh sách công ty (Search, Industry, HasAccount; bỏ qua phân trang).
- Dùng đúng cùng logic lọc với danh sách công ty (tái sử dụng, không viết lại lần hai).
- Nếu số công ty khớp lớn hơn 500 → 400 với thông báo yêu cầu thu hẹp bộ lọc (không tự cắt bớt âm thầm).
- Kết quả cùng định dạng 2.2.

### 2.4 Xuất CSV
POST /api/companies/accounts/export
Body: { "accounts": [{ companyName, username, temporaryPassword, contactEmail }] }
- Danh sách rỗng → 400. Trả file text/csv, có BOM UTF-8, cột: Username,TemporaryPassword,CompanyName,ContactEmail.
- Mọi ô đi qua SpreadsheetSafety.EscapeCsvField (chống công thức độc như =, +, -, @ ở đầu ô, và xử lý dấu phẩy/nháy/xuống dòng).
- Dùng POST (không dùng GET) vì mật khẩu không được nằm trong URL.

### 2.5 Không làm
Không gửi email, không đổi schema/migration, không đổi cách đăng nhập của role Company, không đổi quy tắc đặt Username.

### 2.6 Routing
Các route mới nằm dưới api/companies/accounts/... không được xung đột với api/companies/{id} và api/companies/{companyId}/account hiện có. Phải kiểm tra bằng cách chạy thật.

## 3. Frontend

### 3.1 Điều hướng
- Menu admin thêm mục "Tài khoản DN" (icon pi pi-id-card), đặt ngay sau "Doanh nghiệp", trỏ tới /companies/accounts. Có mặt ở menu desktop và mục "more" trên mobile như "Tạo tài khoản" của sinh viên. Menu của role Company và User không đổi.
- Route /companies/accounts đặt không xung đột với /companies/:id. Có tiêu đề trang tiếng Việt: "Tạo tài khoản doanh nghiệp".

### 3.2 Trang tạo hàng loạt (giống CreateAccountsView của sinh viên)
- Ô tìm kiếm theo tên (debounce như trang sinh viên), bộ lọc ngành, bộ lọc trạng thái tài khoản (Tất cả / Đã có tài khoản / Chưa có tài khoản).
- Bảng có phân trang phía server, cột chọn, tên, ngành, người liên hệ, email liên hệ, trạng thái tài khoản.
- Dòng đã có tài khoản không chọn được. Dòng thiếu email hoặc email sai định dạng: hiện nhãn cảnh báo "Thiếu email" / "Email sai định dạng" và không chọn được (dùng cùng hàm kiểm tra email phía frontend đã có: utils/emailValidation.js).
- Nút "Tạo tài khoản cho N doanh nghiệp đã chọn" (tắt khi N = 0). Nút "Tạo tài khoản cho tất cả theo bộ lọc (~M doanh nghiệp)" có hộp xác nhận ghi rõ là áp dụng cho TẤT CẢ kết quả khớp bộ lọc, không chỉ trang đang xem.
- Chống bấm đúp: nút bị khóa và hiện trạng thái đang xử lý trong lúc gọi API.
- Có trạng thái đang tải, trạng thái trống ("Không có doanh nghiệp phù hợp"), trạng thái lỗi kèm nút thử lại, thông báo lỗi lấy từ extractErrorMessage.
- Dùng được tốt trên điện thoại (bảng cuộn ngang hoặc dạng thẻ như quy ước hiện có), có label cho mọi ô nhập (dự án có script check:labels và check:contrast, phải qua).

### 3.3 Trang kết quả (mở rộng AccountResultView.vue)
- Thêm kind 'company-bulk', tiêu đề "Tài khoản doanh nghiệp đã tạo".
- Hiện số thành công và số bỏ qua. Bảng tài khoản đã tạo: tên doanh nghiệp, tên đăng nhập, mật khẩu tạm, email. Bảng "Không tạo được" với lý do từng công ty (chỉ hiện khi có).
- Cảnh báo rõ: mật khẩu chỉ hiển thị một lần, hãy tải CSV hoặc ghi lại ngay.
- Nút tải CSV gọi endpoint 2.4. Rời trang là xóa dữ liệu mật khẩu khỏi store (giữ cơ chế onBeforeRouteLeave như nhánh student-bulk). Tải lại trang (F5) khi store trống thì quay về /companies/accounts, không hiện trang trắng.
- Không lưu mật khẩu vào localStorage/sessionStorage/URL.

## 4. Kiểm thử bắt buộc phải có (Tab 1 tự viết)
- Backend (xUnit, theo kiểu CompanyAccountServiceTests hiện có): tạo thành công nhiều công ty; bỏ qua từng loại lý do; danh sách rỗng; vượt 500; Id trùng; Id không tồn tại; chạy lần 2 thì bỏ qua hết; lọc theo HasAccount; mật khẩu trong DB là hash BCrypt, không phải plain text; export CSV escape đúng chuỗi bắt đầu bằng "=".
- Frontend (vitest): chỉ cho logic thuần mới tách ra (ví dụ hàm xác định dòng có chọn được hay không, hàm dựng lý do cảnh báo).

## 5. Tiêu chí hoàn thành
Build sạch; test mới và toàn bộ test cũ đều qua; npm run build, check:labels, check:contrast qua; chức năng 1 công ty cũ vẫn chạy; Tab 2 PASS; Tab 3 PASS cho cùng REV.
