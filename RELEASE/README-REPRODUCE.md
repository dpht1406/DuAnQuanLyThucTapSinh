# Hướng Dẫn Tái Lập Môi Trường & Kiểm Thử (Reproduce Guide)

Tài liệu này cung cấp toàn bộ hướng dẫn để một lập trình viên hoặc kỹ sư QA độc lập có thể kiểm chứng, build, chạy test và khởi động API từ một bản sao chép (clone) hoàn toàn sạch.

---

## 1. Yêu Cầu Môi Trường (Prerequisites)
- **.NET SDK**: Phiên bản .NET 8.0 SDK trở lên (Hệ thống đã kiểm nghiệm thành công trên .NET SDK 9.0.308 với target net8.0).
- **Node.js**: Phiên bản 18 LTS trở lên (Hệ thống đã kiểm nghiệm trên Node v22.19.0 - phục vụ frontend nếu cần).
- **Hệ điều hành**: Windows 10/11, Linux hoặc macOS.
- **Git**: Đã cài đặt Git client.

---

## 2. Các Bước Thực Hiện Tuần Tự

### Bước 1: Khôi phục dependencies (Restore)
Từ thư mục gốc của repository, thực hiện lệnh:
```bash
dotnet restore DuAnQuanLyThucTapSinh.sln
```
*Kết quả mong đợi*: Toàn bộ các gói NuGet được restore thành công cho tất cả các dự án trong solution.

---

### Bước 2: Biên dịch toàn bộ giải pháp (Build)
```bash
dotnet build DuAnQuanLyThucTapSinh.sln --no-restore
```
*Kết quả mong đợi*:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

### Bước 3: Chạy bộ kiểm thử đặc tả của module refactor (20 Tests)
```bash
dotnet test tests/PlacementAcceptance.Tests/PlacementAcceptance.Tests.csproj --no-build -v normal
```
*Kết quả mong đợi*:
- **Tổng số test**: 20
- **Passed**: 20 (100%)
- **Failed**: 0
- **Skipped**: 0

---

### Bước 4: Chạy toàn bộ test suites của backend (395 Tests)
```bash
dotnet test InternshipManagement/backend/InternshipManagement.sln
```
*Kết quả mong đợi*:
- **Tổng số test**: 396
- **Passed**: 395
- **Failed**: 0
- **Skipped**: 1 (`StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters`)

---

### Bước 5: Khởi động API Backend
```bash
dotnet run --project InternshipManagement/backend/src/StudentInternshipMgmt.Api
```
*Kết quả mong đợi*:
- Ứng dụng ASP.NET Core lắng nghe tại cổng đã cấu hình (mặc định `http://localhost:5018` hoặc `https://localhost:7121`).
- Swagger UI (trong môi trường Development) sẵn sàng tại `http://localhost:5018/swagger`.
