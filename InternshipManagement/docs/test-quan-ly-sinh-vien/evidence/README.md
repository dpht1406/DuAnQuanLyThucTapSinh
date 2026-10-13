# Minh chứng kiểm thử — Quản lý sinh viên thực tập

## Tóm tắt lần chạy cuối

- Backend: `dotnet test InternshipManagement.sln --nologo -v minimal` — 245 passed, 0 failed, 1 skipped (246 total).
- Frontend: `npm test -- --run` — 93 passed, 0 failed, 5 test files.
- Một test SQL Server bị skipped: `StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters`. Test yêu cầu `STUDENT_TEST_SQLSERVER` trỏ tới dedicated SQL Server test database; biến này chưa được cấu hình. Test được ghi là **Chưa chạy**, không tính Đạt.
- API/DB integration và các số đo hiệu năng chưa chạy. Không có `StudentInternshipMgmtDb_Test`, API không lắng nghe tại `localhost:5018`, và frontend không lắng nghe tại `localhost:5173`.
- Database `StudentInternshipMgmtDb` hiện hữu không được mở để kiểm thử, sửa schema, seed hay ghi dữ liệu.

## Môi trường đã quan sát

Thông tin đầy đủ và đã loại trừ dữ liệu nhạy cảm nằm trong `logs/environment-and-api-check.txt`: Windows 11, CPU Intel Core i5-13420H, RAM 31.7 GB, .NET SDK 9.0.308, Node 22.19.0 và SQL Server 2022 Developer 16.0.1200.5. SQL Server service đang chạy.

## Cấu trúc minh chứng

- `logs/dotnet-test-final.txt`: output nguyên văn của lần chạy backend cuối.
- `logs/vitest-final.txt`: output nguyên văn của lần chạy frontend cuối.
- `logs/pre-fix-regression-tests.txt`: lần thử regression test trên HEAD trước sửa trong worktree riêng. Testhost dừng trước assertions do `SocketException (10013)` khi bind socket; đây **không phải** kết quả test trước-sửa.
- `logs/environment-and-api-check.txt`: phiên bản runtime, trạng thái SQL/API/frontend và xác nhận không dùng DB mặc định.
- `logs/docx-page-count.txt`: Word COM render 8 trang; trang 8 bắt đầu bằng “BẢN TỰ ĐÁNH GIÁ”.
- `logs/deliverable-verification.txt`: kết quả mở lại workbook/report, kiểm đếm case, formulas và đường dẫn minh chứng.
- `perf/measurement-attempts.csv` và `perf/measurement-not-run.txt`: các phép đo yêu cầu, ngưỡng nhóm tự đề xuất và lý do chưa có kết quả. `min/median/max` cố ý để trống; không dùng thời gian test unit hay lỗi kết nối thay thế.
- `data/`: input fixtures và `generate-input-data.ps1`. Dữ liệu nhân tạo, prefix MSSV `TST`, seed cố định `20261012`.
- `screenshots/`: hiện trống; không có UI/browser session để tạo ảnh thật.

## Dữ liệu kiểm thử

Chạy từ thư mục này để tạo lại dữ liệu:

```powershell
powershell -ExecutionPolicy Bypass -File .\data\generate-input-data.ps1
```

Script tạo CSV hợp lệ 20 dòng; CSV trộn 9 dòng có ký tự tiếng Việt, thiếu giá trị, email/SĐT sai, MSSV lặp không phân biệt hoa thường và ô `=1+1`; CSV thiếu header `ClassName`; CSV 1.000/5.000 dòng; XLSX 5.000 dòng; tệp `.txt` sai định dạng và CSV rỗng. `TSTDB0001` chỉ là mẫu cần được seed vào **DB test riêng** trước khi xác minh trường hợp trùng MSSV trong DB. Không upload/import fixtures trong lần chạy này.

Nguồn là generator PowerShell dùng .NET `Random` với seed cố định, không lấy dữ liệu sinh viên thật. Mỗi lần tạo lại sẽ ghi đè các fixture cùng tên trong `data/`.

## Chạy lại trên máy mới

1. Mở PowerShell tại thư mục `InternshipManagement`.
2. Cấu hình SQL Server local; tạo database riêng `StudentInternshipMgmtDb_Test` hoặc dùng quyền của EF để tạo database mới. **Không** trỏ tới `StudentInternshipMgmtDb` hay database production.
3. Đặt connection string riêng qua biến môi trường, không sửa/commit `appsettings.json`:

```powershell
$testConnection = 'Server=localhost;Database=StudentInternshipMgmtDb_Test;Integrated Security=True;TrustServerCertificate=True;'
$env:ConnectionStrings__DefaultConnection = $testConnection
$env:STUDENT_TEST_SQLSERVER = $testConnection
```

4. Restore, migrate và chạy backend:

```powershell
Set-Location .\backend
dotnet restore
dotnet ef database update --project src/StudentInternshipMgmt.Infrastructure --startup-project src/StudentInternshipMgmt.Api
dotnet test InternshipManagement.sln --nologo -v minimal
```

`dotnet ef database update` sẽ tạo/cập nhật **database được chỉ định bởi `ConnectionStrings__DefaultConnection`**. Xác nhận connection string đang chứa chính xác `StudentInternshipMgmtDb_Test` trước khi chạy. Test SQL Server dùng `STUDENT_TEST_SQLSERVER`. Cần khởi tạo tài khoản Admin test trong DB riêng bằng quy trình seed/fixture được phê duyệt; không tái sử dụng credential thật.

5. Chạy frontend:

```powershell
Set-Location ..\frontend
npm ci
npm test -- --run
```

6. Tạo lại fixtures nếu cần:

```powershell
powershell -ExecutionPolicy Bypass -File ..\docs\test-quan-ly-sinh-vien\evidence\data\generate-input-data.ps1
```

7. API/benchmark chỉ được chạy sau khi khởi động API trỏ cùng DB test, có test Admin, và kiểm tra lại cấu hình trước khi gửi bất kỳ POST/import nào. Import làm phát sinh record; chỉ dùng MSSV `TST*` và xóa database test riêng sau khi hoàn tất. Không ghi access token, refresh token, mật khẩu hoặc JWT secret vào log/báo cáo.

Sau khi seed tài khoản Admin test và xác nhận API dùng DB test, có thể chạy benchmark bằng:

```powershell
Set-Location .\docs\test-quan-ly-sinh-vien\evidence\perf
powershell -ExecutionPolicy Bypass -File .\measure-api.ps1 -ApiBaseUrl 'http://localhost:5018' -TestDatabaseName 'StudentInternshipMgmtDb_Test'
```

Script yêu cầu gõ lại tên DB chính xác và hỏi credential Admin test bằng secure prompt. Nó gửi 5 lần login, import CSV 100/1.000/5.000 dòng, export CSV/XLSX 1.000/5.000 sinh viên, danh sách phân trang và Dashboard summary; tạo dữ liệu chỉ trong test DB. Tệp `runtime-runs-*.csv` ghi từng latency/status/bytes; `runtime-summary-*.csv` ghi min/median/max. Không lưu body/token/mật khẩu. Import/seed tạo dữ liệu TST trong DB test; drop riêng DB test sau khi lưu kết quả. Script này **chưa được chạy** trên môi trường hiện tại vì các điều kiện an toàn/API ở trên chưa thỏa; cần kiểm tra cấu hình DB trước khi chạy.

## Kết quả và giới hạn

Workbook có 39 case, 12 cột/case; công thức `COUNTIF` tính 21 Đạt / 0 Không đạt / 18 Chưa chạy. Ba lỗi được sửa trước giai đoạn bàn giao và có regression tests sau sửa. Không có assertion output trước sửa: lần thử trên source HEAD bị testhost abort do socket permission, chi tiết ở pre-fix log. Không được hiểu các test suite pass như bằng chứng cho những API/import/export/KPI cases chưa chạy.

Thresholds hiệu năng trong workbook là mục tiêu nhóm tự đặt, không phải SLA. Chưa có min/median/max hoặc so sánh trước-sau. Warnings còn lại ở output build: NU1903 AutoMapper 13.0.1 advisory mức high và MSB3277 conflict EF Core 8.0.10/8.0.31; không thay dependency trong phạm vi bàn giao.
