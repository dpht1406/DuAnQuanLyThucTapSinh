# PROGRESS - STEP 22

## Đã làm

- Tạo `InternshipManagement/backend/docs/manual-tests/run-e2e.ps1`, dùng API để tạo fixture không phụ thuộc ID cố định, kiểm tra guard kết nối riêng tới `StudentInternshipMgmtDb_E2E`, role matrix, worker, email-disabled flow, dữ liệu nullable và sáu race bằng `Start-Job`.
- Tạo `InternshipManagement/backend/docs/manual-tests/step-22-e2e.md` với điều kiện chạy, từng kịch bản, kết quả E2E thật, findings và kết quả build/test.
- Áp dụng migrations lên DB `StudentInternshipMgmtDb_E2E` riêng bằng Windows Authentication; không dùng DB dev. Khởi động API tại `127.0.0.1:5018`, xác minh API và dừng đúng tiến trình sau kiểm thử.
- Sửa projection `StudentReportDismissedAt` trong Admin placement-request list và thêm assertion unit test.
- Sửa SQL connection-string guard trong runner sau khi lần chạy đầu báo lỗi.
- Không tạo migration mới, không thêm package, không commit.

## Kết quả kiểm tra

- SQL Server E2E: **48 PASS, 1 FAIL, 1 SKIP**. Luồng nghiệp vụ chính, authorization (32 tổ hợp), cách ly Company, quyền User, SMTP disabled, bảy trường nullable, expiry/dashboard đều PASS.
- Năm loại race (ii-vi) PASS đủ 10 lượt mỗi loại. Race (i) có một lượt nhận trạng thái `0,200` thay vì `400/409,200`; các lượt còn lại và kiểm tra nhất quán dữ liệu sau race PASS. Nguyên nhân chưa xác định; không sửa phỏng đoán.
- SMTP giả: chưa chạy vì không phát hiện smtp4dev/Papercut trên máy; SMTP thật không bật.
- `dotnet build`: PASS, 0 warnings, 0 errors.
- `dotnet test InternshipManagement.sln -m:1`: PASS, 285 passed, 1 skipped, 0 failed.
- `npm test`: PASS, 8 files / 111 tests passed.
- `npm run build`: PASS, Vite 8.3.0, 480 modules transformed.

## Việc còn tồn

- Điều tra nguyên nhân race (i) round 1 không quan sát được HTTP status ở một `Start-Job`; kết quả kiểm tra hậu trạng thái nhất quán nhưng tiêu chí phản hồi 400/409 chưa đạt ở lượt đó.
- Chưa kiểm thử SMTP delivery qua smtp4dev/Papercut.

## Bước tiếp theo

- Chẩn đoán/reproduce phản hồi `0` ở race (i), bổ sung thu thập lỗi client/job cụ thể nếu cần, rồi chạy lại tối thiểu race này và ghi nhận kết quả mới.
- Chỉ chạy SMTP fake khi có server giả được cài đặt/cấu hình.
