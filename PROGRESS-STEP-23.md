# STEP 23 — Xác minh race (i) và chạy lại E2E

## Đã thực hiện

- Xác định nguyên nhân trực tiếp giá trị `0` trong `Invoke-Race`: nhánh `catch`
  trả `0` khi exception không có `Response.StatusCode`, đồng thời làm mất thông
  tin exception. Token đã được truyền vào job; lần chạy cũ không lưu exception
  hoặc log nên không thể kết luận chính xác exception client nào đã xảy ra.
- Cập nhật runner để mỗi job báo cáo `Status`, `Body`, `ExceptionType`,
  `ExceptionMessage`, `StartedAt`, `EndedAt`; lỗi không có HTTP status hiện là
  `ERR:<ExceptionType>`.
- Thêm warm-up bằng cả hai token, mốc chạy chung sau 3 giây, timeout 30 giây,
  log API có timestamp và thống kê 50 lượt race (i).
- Giới hạn việc reset database vào đúng `StudentInternshipMgmtDb_E2E`, áp dụng
  các migration hiện có và không tạo migration mới.
- Chạy lại toàn bộ E2E và bổ sung mục “Chạy lại (STEP 23)” vào báo cáo STEP 22,
  giữ nguyên kết quả lần chạy trước.
- Không sửa code nghiệp vụ: 50 lượt race (i) đều trả đúng một `200` và một
  `400/409`. Log có deadlock SQL 1205; luồng hiện có trả Conflict/HTTP 409,
  không ghi nhận HTTP 500 hoặc lỗi kết nối. Bằng chứng phù hợp với nhận định
  **khả năng cao là khởi động nguội ở lần chạy đầu**.

## Kết quả kiểm tra

- `run-e2e.ps1`: **50 PASS, 0 FAIL, 1 SKIP**.
- Race (i): **50/50 đúng**, 0 lượt lạ.
- Các race (ii)–(vi): **10/10 đúng mỗi race**.
- Log API: 108 dòng `Error Number:1205`; không có `Error Number:1222`; không ghi
  nhận exception không xử lý thành HTTP 500 hoặc lỗi kết nối đóng/reset.
- SMTP giả: **Chưa chạy** — không tìm thấy smtp4dev hoặc Papercut.
- `dotnet build`: PASS — 0 warnings, 0 errors.
- `dotnet test InternshipManagement.sln -m:1`: PASS — 285 passed, 1 skipped,
  0 failed (200 Infrastructure.Tests; 85 StudentInternshipMgmt.Tests).
- `npm test`: PASS — 8 test files, 111 tests passed.
- `npm run build`: PASS — Vite 8.3.0, 480 modules transformed.

## Việc còn tồn

- SMTP giả chưa được kiểm tra do máy không có smtp4dev/Papercut.
- Không còn việc thay đổi code nghiệp vụ hoặc schema trong STEP này.

## Bước tiếp theo

- Nếu cần xác minh luồng gửi email, cài/chạy một SMTP giả có sẵn ngoài phạm vi
  STEP này rồi chạy lại kiểm thử SMTP.
