# Hướng Dẫn Tái Lập Môi Trường & Kiểm Thử (Reproduce Guide - Release v1.0.1)

Tài liệu này cung cấp toàn bộ hướng dẫn để một lập trình viên hoặc kỹ sư QA độc lập có thể kiểm chứng, build, chạy test và khởi động API từ một bản sao chép (clone) hoặc giải nén hoàn toàn sạch.

---

## 1. Yêu Cầu Môi Trường (Prerequisites)
- **.NET SDK**: Phiên bản .NET 8.0 SDK trở lên (Hệ thống đã kiểm nghiệm thành công trên .NET SDK 9.0.308 với target net8.0).
- **Node.js**: Phiên bản 18 LTS trở lên (Hệ thống đã kiểm nghiệm trên Node v22.19.0 - phục vụ frontend).
- **Hệ điều hành**: Windows 10/11, Linux hoặc macOS.
- **Git**: Đã cài đặt Git client.

---

## 2. Các Bước Thực Hiện Tuần Tự

### Bước 1: Khôi phục dependencies (Restore)
Từ thư mục gốc của repository, thực hiện lệnh:
```bash
dotnet restore DuAnQuanLyThucTapSinh.sln
```
*Kết quả mong đợi*: Toàn bộ các gói NuGet được restore thành công cho tất cả các dự án trong solution (bao gồm cả 4 dự án src và 3 dự án tests).

---

### Bước 2: Biên dịch toàn bộ giải pháp (Build)
```bash
dotnet build DuAnQuanLyThucTapSinh.sln --no-restore -c Release
```
*Kết quả mong đợi*:
```text
Build succeeded.
    0 Error(s)
```
Tất cả 4 dự án mã nguồn và 3 dự án kiểm thử đều được biên dịch hoàn tất trong 1 lệnh duy nhất.

---

### Bước 3: Chạy toàn bộ test suites của backend (403 Tests)
Do toàn bộ test projects đã được biên dịch ở Bước 2, các lệnh chạy test dưới đây dùng cờ `--no-build`:

```bash
# 1. PlacementAcceptance.Tests (22 tests)
dotnet test tests/PlacementAcceptance.Tests/PlacementAcceptance.Tests.csproj --no-build -c Release

# 2. StudentInternshipMgmt.Tests (116 tests)
dotnet test InternshipManagement/backend/tests/StudentInternshipMgmt.Tests/StudentInternshipMgmt.Tests.csproj --no-build -c Release

# 3. StudentInternshipMgmt.Infrastructure.Tests (265 tests: 264 passed, 1 skipped)
dotnet test InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/StudentInternshipMgmt.Infrastructure.Tests.csproj --no-build -c Release
```
*Kết quả tổng hợp*:
- **Tổng số test**: 403 tests
- **Passed**: 402 (100% test thực thi)
- **Failed**: 0
- **Skipped**: 1 (`StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters` - do yêu cầu máy chủ SQL Server collation tiếng Việt có biến môi trường `STUDENT_TEST_SQLSERVER`).

---

### Bước 4: Chạy kiểm thử Frontend (Vitest & Accessibility)
```bash
cd InternshipManagement/frontend
npm ci
npm test
npm run check:contrast
npm run check:labels
npm run build
cd ../..
```
*Kết quả mong đợi*:
- **Vitest**: 18 test files, 199 passed, 0 failed.
- **check:contrast**: 0 cặp màu không đạt (100% PASS).
- **check:labels**: Không phát hiện label mồ côi (100% PASS).
- **build**: Đóng gói frontend thành công trong thư mục `dist`.

---

### Bước 5: Khởi động API Backend
```bash
dotnet run --project InternshipManagement/backend/src/StudentInternshipMgmt.Api -c Release
```
*Kết quả mong đợi*:
- Ứng dụng ASP.NET Core lắng nghe tại cổng đã cấu hình (mặc định `http://localhost:5018`).
- Swagger UI sẵn sàng tại `http://localhost:5018/swagger`.
