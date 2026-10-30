# Các bước chưa chạy được

- Không chạy được test trên baseline `before-security-review`: `git worktree add ..\before-run before-security-review` trả về `fatal: invalid reference: before-security-review` (mã thoát 128). Git chỉ liệt kê worktree hiện tại; không có worktree tạm được tạo.
- Vì thiếu tag baseline, không có `dotnet-test.txt`, `vitest.txt`, `npm-build.txt` hoặc `dotnet-build.txt` đáng tin cậy cho thư mục `evidence/before`. Không dùng commit gần nhất hoặc source đang bẩn thay cho tag mà người dùng yêu cầu.
- Test `Admin_who_must_change_password_cannot_use_management_endpoints` được chạy trên working tree ngay trước khi sửa lỗi để xác nhận hiện trạng; log giới hạn ở `before/sec16-working-tree-before-fix.txt` và không được xem là bằng chứng từ tag baseline.
- Các lượt kiểm tra sau đã chạy và trả mã thoát thành công. Runner không lưu stdout/stderr đầy đủ vào tệp; `evidence/after/` chỉ có bản ghi kết quả tóm tắt, không phải log thô. Test runner VS Code xác nhận 375 test đạt, không có test lỗi.
- Chưa thử gửi email qua máy chủ SMTP thật; kiểm thử tắt SMTP theo cấu hình yêu cầu.
- Chưa thực hiện quét phụ thuộc/SCA trong phạm vi công việc này; build/test không thay thế cho quét đó.
