# PROGRESS STEP 24

## Đã làm

- Drop/create riêng `StudentInternshipMgmtDb_E2E`, chạy `dotnet ef database update`, rồi đọc `SELECT MustChangePassword FROM dbo.Users WHERE Username = N'admin';` trước mọi câu `UPDATE` thủ công: giá trị thực tế là `1`.
- Xác nhận migration `20261012073419_ForceAdminPasswordChange` đặt `MustChangePassword=true` bằng `UpdateData`; migration đã được áp dụng trong DB E2E vừa tạo.
- Sửa `run-e2e.ps1` để đọc và assert cờ là `1`, không cập nhật cờ. Giữ kiểm tra đăng nhập `Admin@123` phải trả `mustChangePassword=true`.
- Cập nhật `step-22-e2e.md` giải thích đúng nguồn của cờ và ghi lại kết quả xác minh STEP 24, giữ nguyên các kết quả STEP 22/23.
- Không tạo migration mới, không thêm package, không commit.

## Kết quả kiểm tra

| Lệnh | Kết quả |
|---|---|
| `dotnet build` | PASS — Build succeeded, 0 warnings, 0 errors. |
| `dotnet test InternshipManagement.sln -m:1` | PASS — 285 passed, 1 skipped, 0 failed (200 Infrastructure.Tests; 85 StudentInternshipMgmt.Tests). |
| `npm test` | PASS — 8 test files, 111 tests passed. |
| `npm run build` | PASS — Vite 8.3.0; 480 modules transformed; built in 6.12 s. |
| `run-e2e.ps1 -ConcurrencyIterations 10` | Chạy đầy đủ một lần; **44 PASS, 6 FAIL, 1 SKIP**. Bootstrap Admin PASS và script báo `MustChangePassword after migrations: 1`. Các lỗi race có deadlock SQL 1205, timeout connection pool và lỗi kết nối SQL Server. |
| E2E retry | Không bắt đầu được: `sqlcmd` không kết nối vì service `MSSQLSERVER` đang Stopped. Thử `Start-Service` thất bại do không mở được service bằng quyền hiện tại. |

## Việc còn tồn

- Chưa có lần chạy E2E đầy đủ thành công trong STEP 24 do SQL Server dừng trong/ sau lần chạy đầu và không thể khởi động service bằng quyền hiện tại.
- Sáu lỗi race ở lần chạy đầu cần được tái xác minh khi SQL Server hoạt động ổn định; không sửa code ngoài phạm vi trong STEP 24.

## Bước tiếp theo

- Khởi động hoặc khôi phục service SQL Server `MSSQLSERVER` bằng tài khoản có quyền phù hợp, sau đó chạy lại `docs/manual-tests/run-e2e.ps1 -ConcurrencyIterations 10` và cập nhật kết quả thực tế.
