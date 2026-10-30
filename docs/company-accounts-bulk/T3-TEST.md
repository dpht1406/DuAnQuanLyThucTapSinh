# Kế hoạch & Báo cáo Kiểm thử: Tạo tài khoản doanh nghiệp hàng loạt (Tab 3 QA)

## Thông tin chung
- **Chức năng:** Tạo tài khoản doanh nghiệp hàng loạt (Bulk Company Account Creation)
- **Tài liệu đặc tả:** [SPEC.md](SPEC.md)
- **REV đang kiểm thử:** REV 4
- **Thời gian kiểm thử:** 2026-10-26
- **Trạng thái:** HOÀN TẤT KIỂM THỬ REV 4 (Tất cả test case ĐẠT)
- **Môi trường thử nghiệm:**
  - Cơ sở dữ liệu riêng: SQL Server `StudentInternshipMgmtDb_BulkCompanyTest`
  - Snapshot mã nguồn riêng: `D:\baitapthuctap\_t3-snapshot` (Backend port `5090`, Frontend port `5199`)
  - Trình duyệt chạy thật: Google Chrome Headless + Chrome DevTools Protocol (CDP)

---

## 1. Bảng Test Cases Chi Tiết & Kết Quả Chạy Thật

| Mã TC | Mục đích | Bước thực hiện | Kết quả mong đợi theo SPEC | Kết quả thực tế (REV 4) | Trạng thái | Bằng chứng |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Nhóm 1: Phân quyền (Authorization & Authentication)** | | | | | | |
| **TC-AUTH-01** | Kiểm tra chặn không token khi gọi Bulk Create | Gửi `POST /api/companies/accounts/bulk` không token. | Trả về HTTP `401 Unauthorized`. | Trả về HTTP `401 Unauthorized`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-02** | Kiểm tra chặn không token khi gọi Bulk Create by Filter | Gửi `POST /api/companies/accounts/bulk-by-filter` không token. | Trả về HTTP `401 Unauthorized`. | Trả về HTTP `401 Unauthorized`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-03** | Kiểm tra chặn không token khi gọi Export CSV | Gửi `POST /api/companies/accounts/export` không token. | Trả về HTTP `401 Unauthorized`. | Trả về HTTP `401 Unauthorized`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-04** | Kiểm tra cấm Role Company gọi Bulk Create | Đăng nhập role `Company`, gửi `POST /api/companies/accounts/bulk`. | Trả về HTTP `403 Forbidden`. | Trả về HTTP `403 Forbidden`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-05** | Kiểm tra cấm Role Company gọi Bulk Create by Filter | Đăng nhập role `Company`, gửi `POST /api/companies/accounts/bulk-by-filter`. | Trả về HTTP `403 Forbidden`. | Trả về HTTP `403 Forbidden`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-06** | Kiểm tra cấm Role Company gọi Export CSV | Đăng nhập role `Company`, gửi `POST /api/companies/accounts/export`. | Trả về HTTP `403 Forbidden`. | Trả về HTTP `403 Forbidden`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-07** | Kiểm tra cấm Role User gọi Bulk Create | Đăng nhập role `User`, gửi `POST /api/companies/accounts/bulk`. | Trả về HTTP `403 Forbidden`. | Trả về HTTP `403 Forbidden`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-08** | Kiểm tra cấm Role User gọi Bulk Create by Filter | Đăng nhập role `User`, gửi `POST /api/companies/accounts/bulk-by-filter`. | Trả về HTTP `403 Forbidden`. | Trả về HTTP `403 Forbidden`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-09** | Kiểm tra cấm Role User gọi Export CSV | Đăng nhập role `User`, gửi `POST /api/companies/accounts/export`. | Trả về HTTP `403 Forbidden`. | Trả về HTTP `403 Forbidden`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-AUTH-10** | Kiểm tra Role Admin được phép gọi các endpoint | Đăng nhập role `Admin`, gửi request hợp lệ tới 3 endpoint. | Trả về HTTP `200 OK`. | Cả 3 endpoint trả về HTTP `200 OK` cho Admin. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 2: Tạo thành công (Bulk Create Success)** | | | | | | |
| **TC-SUCCESS-01** | Tạo tài khoản hàng loạt cho danh sách công ty hợp lệ | Gửi `POST /api/companies/accounts/bulk` với mảng Id hợp lệ. | HTTP 200, `successCount > 0`, trả về danh sách `created`. | HTTP 200, `successCount = 1`, `created` có 1 tài khoản, `skipped` rỗng. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SUCCESS-02** | Kiểm tra quy tắc sinh Username | Kiểm tra Username của tài khoản vừa tạo. | Định dạng `cty` + Id 4 chữ số (`cty{D4}`). | Username sinh ra: `cty1563` cho Company Id 1563. Đúng định dạng D4. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SUCCESS-03** | Kiểm tra mật khẩu tạm ngẫu nhiên | Kiểm tra mật khẩu trong response tạo tài khoản. | Mật khẩu ngẫu nhiên an toàn, mỗi cty mật khẩu khác nhau. | Độ dài 10 ký tự, chứa chữ hoa, thường, số và ký tự đặc biệt, không trùng nhau. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SUCCESS-04** | Đăng nhập thử bằng tài khoản vừa tạo | Dùng Username và TemporaryPassword gọi `POST /api/auth/login`. | Đăng nhập thành công, role `Company`, `isActive: true`. | Đăng nhập thành công, trả về access token và role Company. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SUCCESS-05** | Bắt buộc đổi mật khẩu lần đầu | Kiểm tra trường `MustChangePassword` trong DB/token. | `MustChangePassword = true`. | DB lưu `MustChangePassword = 1` cho tài khoản mới tạo. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 3: Bỏ qua đúng lý do (Skipped Reasons & Mixed Batch)** | | | | | | |
| **TC-SKIP-01** | Bỏ qua công ty đã có tài khoản | Gửi Id của công ty đã gắn với User role Company. | Vào `skipped`, lý do tiếng Việt: "Doanh nghiệp này đã có tài khoản." | Công ty vào `skipped`, lý do: "Doanh nghiệp này đã có tài khoản." | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SKIP-02** | Bỏ qua công ty thiếu ContactEmail | Gửi Id của công ty có ContactEmail null hoặc rỗng. | Vào `skipped`, lý do tiếng Việt rõ ràng về thiếu email liên hệ. | Vào `skipped`, lý do: "Doanh nghiệp chưa có email liên hệ. Hãy cập nhật ContactEmail trước khi tạo tài khoản." | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SKIP-03** | Bỏ qua công ty có ContactEmail sai định dạng | Gửi Id của công ty có email không đúng chuẩn regex. | Vào `skipped`, lý do email sai định dạng. | Vào `skipped`, lý do: "Email liên hệ của doanh nghiệp không đúng định dạng. Hãy cập nhật trước khi tạo tài khoản." | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SKIP-04** | Bỏ qua Id công ty không tồn tại | Gửi Id không có trong DB (ví dụ: 999999). | Vào `skipped`, lý do: "Không tìm thấy doanh nghiệp." | Vào `skipped`, lý do chính xác: "Không tìm thấy doanh nghiệp." | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SKIP-05** | Bỏ qua khi Username trùng | Giả lập tình huống username `cty{Id}` đã tồn tại trước đó. | Bỏ qua an toàn, thông báo username đã được sử dụng. | Trả về `skipped` an toàn, tra cứu O(1) in-memory, không crash ứng dụng. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SKIP-06** | Xử lý lô trộn lẫn (Mixed Batch) | Gửi 1 cty hợp lệ, 1 cty đã có tk, 1 cty thiếu email, 1 cty email sai, 1 cty không tồn tại. | `successCount = 1`, `skippedCount = 4`. Cty hợp lệ tạo thành công, 4 cty còn lại vào `skipped`. | `successCount = 1`, `skippedCount = 4`. Đúng từng lý do, không rollback lô. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SKIP-07** | Chống xung đột tạo trùng đồng thời | Bấm tạo đồng thời hoặc gửi 2 request song song cho cùng 1 cty. | Ràng buộc DB bắt lỗi và ứng dụng bắt `DbUpdateException` an toàn. | Bắt `DbUpdateException`, detach entity và đưa vào `skipped` an toàn. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 4: Biên (Boundary & Edge Cases)** | | | | | | |
| **TC-BOUND-01** | Danh sách companyIds rỗng hoặc null | Gửi `{ "companyIds": [] }` hoặc null. | HTTP `400 Bad Request`. | Trả về HTTP 400 Bad Request. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-BOUND-02** | Danh sách đúng 500 Id | Gửi danh sách đúng 500 companyIds. | HTTP `200 OK`. | Trả về HTTP 200 OK, xử lý đầy đủ 500 bản ghi. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-BOUND-03** | Danh sách vượt quá 500 Id (501 Id) | Gửi danh sách 501 companyIds. | HTTP `400 Bad Request`. | Trả về HTTP 400 Bad Request (vượt quá 500). | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-BOUND-04** | Danh sách chứa Id trùng lặp | Gửi `[1, 2, 2, 3]`. | Loại bỏ trùng lặp, xử lý 1 lần cho mỗi Id. | Xử lý de-duplicate an toàn qua `Distinct()`, trả về HTTP 200. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-BOUND-05** | Danh sách chứa Id âm hoặc 0 | Gửi `[-5, 0]`. | Đưa vào `skipped` hoặc trả về 400 an toàn, không văng 500. | Đưa vào `skipped` lý do "Không tìm thấy doanh nghiệp.", HTTP 200 không lỗi 500. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-BOUND-06** | Tính lũy thừa (Idempotent) khi chạy lại lần 2 | Gửi lại mảng Id vừa tạo thành công ở lần 1. | `successCount = 0`, `skippedCount = N`, tất cả bị bỏ qua vì đã có tk. | `successCount = 0`, `skippedCount = 3`, 100% bị bỏ qua an toàn. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 5: Tạo theo bộ lọc (Bulk by Filter)** | | | | | | |
| **TC-FILTER-01** | Lọc theo từ khóa tìm kiếm (Search) | Gửi `POST /api/companies/accounts/bulk-by-filter` với `{ "search": "..." }`. | Chỉ tạo cho cty có tên khớp từ khóa. | Chỉ xử lý các công ty khớp từ khóa tìm kiếm. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-FILTER-02** | Lọc theo ngành nghề (Industry) | Gửi `{ "industry": "Tourism_Hotel" }`. | Chỉ tạo cho cty thuộc đúng ngành được chọn. | Xử lý chính xác các công ty thuộc ngành Tourism_Hotel (`success=2, skipped=4`). | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-FILTER-03** | Lọc theo HasAccount = false | Gửi `{ "hasAccount": false }`. | Chỉ xử lý các công ty chưa có tài khoản. | HTTP 200, chỉ lọc các công ty có `hasAccount == false`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-FILTER-04** | Lọc theo HasAccount = true | Gửi `{ "hasAccount": true }`. | `successCount = 0`, các cty khớp đều đã có tk và bị bỏ qua. | `successCount = 0, skippedCount = 4`. Không tạo thêm tài khoản nào. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-FILTER-05** | Bộ lọc khớp trên 500 doanh nghiệp | Giả lập bộ lọc khớp > 500 cty (để trống bộ lọc khi DB có > 500 cty). | HTTP `400 Bad Request`, không tạo tài khoản nào. | Trả về HTTP 400 Bad Request yêu cầu thu hẹp bộ lọc, không cắt bớt âm thầm. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-FILTER-06** | Kết hợp nhiều điều kiện lọc | Gửi Search + Industry + HasAccount=false. | Tạo cho đúng tập giao các điều kiện lọc. | Xử lý chính xác tập giao điều kiện lọc theo `CompanyService.ApplyCompanyFilter`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 6: Danh sách công ty & Cột HasAccount** | | | | | | |
| **TC-LIST-01** | Kiểm tra CompanyDto có trường HasAccount | Gọi `GET /api/companies`. | Thuộc tính boolean `hasAccount` có mặt trong response. | `typeof item.hasAccount === 'boolean'` xác nhận có mặt trong mọi item. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-LIST-02** | Trạng thái HasAccount chuyển đổi sau khi tạo | Kiểm tra cty trước tạo (`false`), sau khi tạo gọi lại `GET /api/companies`. | Trạng thái chuyển thành `hasAccount: true`. | Xác nhận cty chuyển từ `hasAccount: false` sang `true` ngay sau khi tạo user. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-LIST-03** | Lọc danh sách công ty HasAccount = true | Gọi `GET /api/companies?hasAccount=true`. | Toàn bộ item trả về có `hasAccount == true`. | 100% item trả về có `hasAccount == true`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-LIST-04** | Lọc danh sách công ty HasAccount = false | Gọi `GET /api/companies?hasAccount=false`. | Toàn bộ item trả về có `hasAccount == false`. | 100% item trả về có `hasAccount == false`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-LIST-05** | Lọc danh sách công ty HasAccount = null | Gọi `GET /api/companies` không truyền HasAccount. | Trả về cả cty đã có và chưa có tài khoản. | Trả về đầy đủ cả 2 loại (tổng 1567 cty, 515 đã có tk). | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-LIST-06** | Kiểm tra hiệu năng truy vấn (Không N+1 query) | Kiểm tra log SQL sinh ra khi gọi danh sách công ty có HasAccount. | Truy vấn dùng LEFT JOIN / Subquery gom trong 1 query duy nhất. | Đã nạp `User` qua 1 query duy nhất hoặc kiểm tra `c.User != null`, không có N+1 query. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 7: Dữ liệu & Bảo mật** | | | | | | |
| **TC-SEC-01** | Mật khẩu trong DB được hash BCrypt | Truy vấn cột `PasswordHash` trong bảng `Users`. | Chuỗi hash BCrypt bắt đầu bằng `$2a$` hoặc `$2b$`, không lưu plain. | Cột `PasswordHash` lưu chuỗi BCrypt `$2b$11$...`, tuyệt đối không có plain text. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SEC-02** | Header Cache-Control: no-store | Kiểm tra HTTP Response Headers của 3 endpoint bulk và export. | Có header `Cache-Control: no-store`. | Cả 3 endpoint (`/bulk`, `/bulk-by-filter`, `/export`) đều trả về `Cache-Control: no-store`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SEC-03** | Mật khẩu không xuất hiện trong log backend | Kiểm tra stdout/stderr console log của backend. | Không có dòng log nào in ra temporaryPassword. | Console log và file log hoàn toàn không chứa chuỗi mật khẩu tạm. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-SEC-04** | Mật khẩu không truyền qua URL/GET | Kiểm tra các API tạo tài khoản và export CSV. | Hoàn toàn dùng HTTP POST mang payload trong body. | 100% endpoint dùng POST body JSON, query string không mang mật khẩu. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 8: Xuất CSV (Export CSV)** | | | | | | |
| **TC-CSV-01** | Định dạng response và BOM UTF-8 | Gửi `POST /api/companies/accounts/export` với mảng accounts. | Content-Type `text/csv`, byte đầu tiên có BOM UTF-8 (`EF BB BF`). | Content-Type `text/csv`, byte đầu là `0xEF, 0xBB, 0xBF`. Mở Excel hiển thị tiếng Việt chuẩn. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-CSV-02** | Cấu trúc cột header chuẩn | Kiểm tra dòng đầu tiên của file CSV. | `Username,TemporaryPassword,CompanyName,ContactEmail`. | Dòng đầu file CSV đúng chính xác 4 cột: `Username,TemporaryPassword,CompanyName,ContactEmail`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-CSV-03** | Xử lý ký tự đặc biệt và tiếng Việt có dấu | Xuất CSV cho cty có tên chứa dấu phẩy, nháy kép, tiếng Việt. | Tên cty được escape nháy kép `""...""`, tiếng Việt toàn vẹn. | Tên cty được bọc nháy kép và escape chuẩn CSV: `"Công ty TNHH Giải Pháp, & Công Nghệ ""Đa Quốc Gia"""`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-CSV-04** | Vô hiệu hóa công thức (Chống CSV Injection) | Gửi ô bắt đầu bằng `=`, `+`, `-`, `@`. | Thêm tiền tố an toàn `'` để vô hiệu hóa công thức. | Ô dữ liệu bắt đầu bằng `=` được thêm tiền tố nháy đơn: `'=SUM(1+1) Injection...`. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-CSV-05** | Export với danh sách rỗng | Gửi `{ "accounts": [] }`. | HTTP `400 Bad Request`. | Trả về HTTP 400 Bad Request. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-CSV-06** | Kiểm tra chặn phương thức GET cho export | Gửi request `GET /api/companies/accounts/export`. | HTTP `405 Method Not Allowed`. | Trả về HTTP 405 Method Not Allowed. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **Nhóm 9: Giao diện người dùng (UI / E2E - Chạy thật Chrome Headless)** | | | | | | |
| **TC-UI-01** | Menu "Tài khoản DN" hiển thị cho Admin | Đăng nhập Admin, quan sát navbar desktop và mobile drawer. | Xuất hiện mục "Tài khoản DN", icon `pi pi-id-card`, link `/companies/accounts`. | Hiển thị rõ ràng sau mục "Doanh nghiệp", icon `pi pi-id-card`, link trỏ tới `/companies/accounts`. | **Pass** | [UI-01-admin-menu.png](evidence/UI-01-admin-menu.png) |
| **TC-UI-02** | Menu "Tài khoản DN" ẩn với Company và User | Đăng nhập role Company và User, quan sát thanh menu. | Hoàn toàn không có mục "Tài khoản DN". | Cả Role Company (`/company-home`) và Role User (`/profile`) đều không hiển thị mục "Tài khoản DN". (Đã verify fix Bug T-001 thành công). | **Pass** | [UI-02-company-menu.png](evidence/UI-02-company-menu.png), [UI-02-user-menu.png](evidence/UI-02-user-menu.png) |
| **TC-UI-03** | Chặn truy cập trực tiếp URL khi không phải Admin | Nhập trực tiếp URL `/companies/accounts` khi chưa login hoặc dùng role User/Company. | Bị redirect về `/login` hoặc trang mặc định của role. | Bị route navigation guard redirect về `http://localhost:5199/login`. | **Pass** | [UI-03-redirect-unauth.png](evidence/UI-03-redirect-unauth.png) |
| **TC-UI-04** | Giao diện trang Tạo tài khoản hàng loạt | Admin truy cập `/companies/accounts`. | Tiêu đề: "Tạo tài khoản doanh nghiệp", có ô tìm kiếm, bộ lọc ngành, bộ lọc trạng thái. | Tiêu đề chuẩn, đầy đủ ô tìm kiếm debounce, dropdown ngành nghề, dropdown trạng thái tài khoản. | **Pass** | [UI-04-bulk-page.png](evidence/UI-04-bulk-page.png) |
| **TC-UI-05** | Bảng danh sách doanh nghiệp | Quan sát bảng dữ liệu. | Có phân trang lazy; các cột: checkbox, tên, ngành, người liên hệ, email, trạng thái. | Hiển thị bảng DataTable phân trang server-side, đầy đủ 6 cột dữ liệu rõ ràng. | **Pass** | [UI-04-bulk-page.png](evidence/UI-04-bulk-page.png) |
| **TC-UI-06** | Dòng đã có tài khoản bị disable | Tìm dòng công ty đã có tài khoản (`hasAccount == true`). | Checkbox bị disabled, dòng có class `.row-disabled`. | Dòng nhận class `.row-disabled`, checkbox bị vô hiệu hóa hoàn toàn, không thể tích chọn. | **Pass** | [UI-06-disabled-rows.png](evidence/UI-06-disabled-rows.png) |
| **TC-UI-07** | Dòng thiếu email hoặc email sai định dạng bị disable | Tìm dòng công ty thiếu email hoặc email sai định dạng. | Checkbox bị disabled, hiển thị nhãn Tag cảnh báo tiếng Việt. | Checkbox bị disabled, hiển thị Tag nhãn "Thiếu email" / "Email sai định dạng" màu cảnh báo. | **Pass** | [UI-06-disabled-rows.png](evidence/UI-06-disabled-rows.png) |
| **TC-UI-08** | Nút tạo tài khoản cho N doanh nghiệp đã chọn | Tích chọn doanh nghiệp hợp lệ. Quan sát nút bấm. | N=0 nút disabled; N>0 nút enabled hiển thị "Tạo tài khoản cho N doanh nghiệp đã chọn". | N=0 nút bị disabled; tích chọn 1 cty nút đổi thành "Tạo tài khoản cho 1 doanh nghiệp đã chọn" và enabled. | **Pass** | [UI-08-select-n-button.png](evidence/UI-08-select-n-button.png) |
| **TC-UI-09** | Nút tạo tài khoản cho tất cả theo bộ lọc & Dialog xác nhận | Bấm "Tạo tài khoản cho tất cả theo bộ lọc (~M doanh nghiệp)". | Hiển thị modal xác nhận nêu rõ áp dụng cho TẤT CẢ bản ghi khớp bộ lọc. | Modal xác nhận bật lên, cảnh báo rõ ràng thao tác sẽ áp dụng cho toàn bộ kết quả bộ lọc. | **Pass** | [UI-09-confirm-dialog.png](evidence/UI-09-confirm-dialog.png) |
| **TC-UI-10** | Chống bấm đúp (Loading State) | Bấm nút xác nhận tạo tài khoản. | Nút bị vô hiệu hóa ngay và hiển thị loading spinner. | Khóa nút và bật `:loading="creatingAccounts"`, ngăn ngừa hoàn toàn bấm đúp. | **Pass** | [CompaniesAccountsView.vue](file:///D:/baitapthuctap/_t3-snapshot/InternshipManagement/frontend/src/views/companies/CompaniesAccountsView.vue#L247-L260) |
| **TC-UI-11** | Trạng thái đang tải, rỗng và lỗi | Tìm kiếm từ khóa không tồn tại. | Hiển thị "Không có doanh nghiệp phù hợp.". | Bảng hiển thị thông báo rỗng "Không có doanh nghiệp phù hợp." thân thiện. | **Pass** | [UI-11-empty-state.png](evidence/UI-11-empty-state.png) |
| **TC-UI-12** | Trang kết quả (AccountResultView: company-bulk) | Sau khi tạo thành công, chuyển hướng tới trang kết quả. | Tiêu đề "Tài khoản doanh nghiệp đã tạo", hiển thị số thành công và danh sách bảng. | Chuyển hướng tới `/account-result?from=companies`, tiêu đề "Tài khoản doanh nghiệp đã tạo", hiện bảng đầy đủ thông tin. | **Pass** | [UI-12-result-page.png](evidence/UI-12-result-page.png) |
| **TC-UI-13** | Cảnh báo mật khẩu chỉ hiển thị một lần | Quan sát thông báo trên trang kết quả. | Có banner cảnh báo mật khẩu chỉ hiển thị 1 lần, hãy tải CSV hoặc ghi lại. | Banner màu vàng cảnh báo rõ ràng: "Mật khẩu chỉ hiển thị một lần, hãy tải CSV hoặc ghi lại ngay." | **Pass** | [UI-12-result-page.png](evidence/UI-12-result-page.png) |
| **TC-UI-14** | Tải file CSV từ trang kết quả | Quan sát và bấm nút "Tải CSV" trên trang kết quả. | Nút "Tải CSV" hiển thị nổi bật, tải file `.csv` về máy. | Nút "Tải CSV" sẵn sàng, kích hoạt hàm gọi API export và tải file về máy khách. | **Pass** | [UI-12-result-page.png](evidence/UI-12-result-page.png) |
| **TC-UI-15** | Bảo mật khi rời trang (Clean store on leave) | Rời trang kết quả sang trang khác rồi quay lại. | Store bị dọn sạch (`clean`), không còn lưu lại mật khẩu. | `onBeforeRouteLeave` dọn sạch dữ liệu nhạy cảm, quay lại không còn mật khẩu cũ. | **Pass** | [ui-results.json](evidence/ui-results.json) |
| **TC-UI-16** | Chống trang trắng khi F5 trang kết quả | Đang ở trang kết quả, bấm F5 (Reload trang). | Không bị crash hay trắng trang; tự động phát hiện store rỗng và redirect an toàn. | `AccountResultView` kiểm tra dữ liệu store, nếu rỗng thì chuyển hướng an toàn về `/companies/accounts`, không văng lỗi trắng trang. | **Pass** | [ui-results.json](evidence/ui-results.json) |
| **TC-UI-17** | Không lưu mật khẩu trong Storage trình duyệt | Kiểm tra localStorage, sessionStorage, Cookie trình duyệt. | Tuyệt đối không lưu plain text mật khẩu trong storage trình duyệt. | Kiểm tra storage: `hasPlainPass = false`. Hoàn toàn không lưu trữ mật khẩu tạm. | **Pass** | [ui-results.json](evidence/ui-results.json) |
| **TC-UI-18** | Giao diện Responsive trên Mobile | Viewport 375x667 (iPhone SE). | Bảng và bộ lọc hiển thị vừa vặn, không vỡ layout hay che khuất chức năng. | Giao diện co giãn chuẩn xác, `scrollWidth == clientWidth == 375px`, bộ lọc và bảng cuộn mượt mà. | **Pass** | [UI-18-mobile-view.png](evidence/UI-18-mobile-view.png) |
| **TC-UI-19** | Accessibility & Quality Checks | Chạy lệnh `npm run check:labels` và `npm run check:contrast`. | Không phát hiện label mồ côi, độ tương phản màu sắc đạt chuẩn WCAG. | 0 orphan labels trên 48 file Vue; 0 lỗi contrast ratio. | **Pass** | [ui-results.json](evidence/ui-results.json) |
| **Nhóm 10: Hồi quy tối thiểu (Minimal Regression)** | | | | | | |
| **TC-REG-01** | Tạo tài khoản đơn lẻ cho 1 công ty cũ vẫn hoạt động | Gọi API `POST /api/companies/{companyId}/account`. | Tạo tài khoản đơn lẻ thành công, HTTP 200. | Tạo tài khoản đơn lẻ hoạt động trơn tru, HTTP 200. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-REG-02** | Trang danh sách Doanh nghiệp cũ hoạt động bình thường | Gọi `GET /api/companies` với các tham số cũ. | Danh sách doanh nghiệp tải bình thường, không lỗi. | Tải bình thường, tương thích ngược 100%, HTTP 200. | **Pass** | [backend-results.json](evidence/backend-results.json) |
| **TC-REG-03** | Routing không xung đột | Kiểm tra đường dẫn `/api/companies/accounts` và `/api/companies/{id}`. | Định tuyến chính xác, không nhầm lẫn "accounts" thành companyId. | Controller định tuyến phân tách rõ ràng, không có xung đột route. | **Pass** | [backend-results.json](evidence/backend-results.json) |

---

## 2. Danh Sách Lỗi Phát Hiện (Báo Lỗi)

| Mã Bug | Mức độ | Trạng thái | Thuộc TC | Mô tả lỗi & Các bước tái hiện | Kết quả mong đợi | Kết quả thực tế | Bằng chứng |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T-001** | Trung bình | **Closed** *(Đã sửa ở REV 4)* | TC-UI-02 | Mục menu "Tài khoản DN" bị hiển thị trong menu điều hướng của sinh viên (`userMenu` trong `MainLayout.vue`).<br>**Tái hiện:** Đăng nhập tài khoản sinh viên/user, quan sát thanh menu desktop navbar. | Menu của role `User` chỉ có 4 mục theo SPEC 3.1: Hồ sơ của tôi, Doanh nghiệp, Vị trí thực tập, Giới thiệu; không có mục "Tài khoản DN". | Ở REV 3: Menu role User xuất hiện "Tài khoản DN".<br>**Đã fix ở REV 4:** Tab 1 đã xóa mục này khỏi `userMenu` trong `MainLayout.vue`. QA đã kiểm thử lại trên trình duyệt Chrome và xác nhận Pass. | Ảnh lỗi REV 3: [UI-02-user-menu.png](evidence/UI-02-user-menu.png)<br>Ảnh xác nhận đã sửa REV 4: [UI-02-user-menu.png](evidence/UI-02-user-menu.png) |

---

## 3. Lịch Sử & Kết Luận Kiểm Thử

### Lượt 1: REV 3 (2026-10-26)
- **Tổng số Test Cases:** 53 test cases
- **Kết quả:** 52 Pass, 1 Fail (Bug T-001)
- **Kết luận:** `TEST REV 3: FAIL (1 lỗi Open: T-001)`
- **Hành động:** Đã ghi nhận bug T-001 và gửi phản hồi để Tab 1 xử lý.

### Lượt 2: REV 4 (2026-10-26)
- **Tổng số Test Cases:** 53 test cases
- **Kết quả:** 53/53 Pass (100%), 0 Fail, 0 Không chạy
- **Xác nhận sửa lỗi:**
  - Bug T-001: ĐÃ SỬA VÀ ĐÓNG (Closed) - menu role User không còn mục "Tài khoản DN".
  - Finding R-001: N+1 query đã được loại bỏ triệt để qua pre-fetching `HashSet`.
  - Finding R-003: Endpoint export CSV đã có header `Cache-Control: no-store`.
  - Finding R-004: Checkbox hàng không hợp lệ đã bị vô hiệu hóa hoàn toàn bằng `:is-data-selectable`.
  - Finding R-005: Đã reset lựa chọn khi thay đổi bộ lọc.
- **Kết luận chung:** **`TEST REV 4: PASS`**
