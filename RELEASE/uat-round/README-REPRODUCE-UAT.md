# Hướng dẫn Tái lập và Kiểm thử Gói Phát hành UAT (README Reproduce - UAT Round)

Tài liệu này cung cấp các bước chính xác để bất kỳ kỹ sư hoặc kiểm thử viên nào cũng có thể dựng lại môi trường trên một bản clone sạch (clean clone), chạy kiểm thử và nghiệm thu các bản vá lỗi UAT Đợt 2 một cách độc lập.

---

## 1. Yêu cầu Tiên quyết & Chuẩn bị Môi trường (Prerequisites & Setup)

### 1.1 Yêu cầu Phần mềm
- **Hệ điều hành**: Windows 10/11 x64 hoặc Windows Server.
- **SDK & Runtime**:
  - .NET 8 SDK (`dotnet --version` >= 8.0.x)
  - .NET EF Core CLI Tool (`dotnet ef --version` >= 8.0.x; cài đặt nếu chưa có: `dotnet tool install --global dotnet-ef`)
  - Node.js (`node --version` >= 20.x, khuyến nghị v22.x) & npm (`npm --version` >= 10.x)
  - SQL Server LocalDB hoặc SQL Server Developer/Express instance tại `localhost`
  - PowerShell 7 hoặc Windows PowerShell 5.1

### 1.2 Nhận Mã nguồn trên Bản Clone Sạch
```powershell
# Chuyển sang nhánh UAT Feedback
git checkout feature/uat-feedback
```

---

## 2. Kiểm tra Bản dựng và Toàn bộ Kiểm thử Tự động

### 2.1 Biên dịch Solution Backend
```powershell
# Chạy từ thư mục gốc của repository
dotnet restore DuAnQuanLyThucTapSinh.sln
dotnet build DuAnQuanLyThucTapSinh.sln
```
*Kết quả kỳ vọng*: `Build succeeded. 0 Error(s)`.

### 2.2 Chạy Backend Tests (Đơn vị và Tích hợp)
```powershell
dotnet test InternshipManagement/backend/InternshipManagement.sln -m:1
```
*Kết quả kỳ vọng*:
- Tổng cộng **403 tests**: **402 Passed, 1 Skipped (theo thiết kế), 0 Failed**.
- Toàn bộ các test mới cho tiếng Việt, CV trim, và double-submit race condition đều xanh.

### 2.3 Chạy Frontend Tests và Production Build (DOC-01 đã bổ sung npm install)
> [!IMPORTANT]
> Trên môi trường clone sạch, bắt buộc phải chạy `npm install` trước khi chạy test hoặc build frontend (khắc phục thiếu sót tài liệu DOC-01).

```powershell
cd InternshipManagement/frontend

# 1. Cài đặt các gói phụ thuộc (bắt buộc trên bản clone sạch)
npm install

# 2. Chạy Unit Tests Vitest (bao gồm test CR-03 cho saving guard)
npm test

# 3. Chạy đóng gói Production Build
npm run build
```
*Kết quả kỳ vọng*:
- `Test Files: 17 passed (17)`
- `Tests: 194 passed (194)`
- Build thư mục `dist` thành công không có lỗi.

---

## 3. Chạy Thử nghiệm Thực tế trên CSDL UAT Tách biệt

> [!IMPORTANT]
> **Tuyệt đối không chạy trên CSDL thật `StudentInternshipMgmtDb`**. Hãy dùng CSDL kiểm nghiệm riêng biệt `StudentInternshipMgmtDb_UATBuild` thông qua biến môi trường của tiến trình.

### 3.1 Chuẩn bị Cơ sở Dữ liệu Kiểm thử Biệt lập (Nếu chạy lần đầu)
Nếu trên máy chạy thử chưa có cơ sở dữ liệu `StudentInternshipMgmtDb_UATBuild`:
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=StudentInternshipMgmtDb_UATBuild;Integrated Security=True;TrustServerCertificate=True;"
dotnet ef database update --project InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure --startup-project InternshipManagement/backend/src/StudentInternshipMgmt.Api
```

### 3.2 Tự động hóa Xác minh bằng PowerShell Script
Nhóm phát triển đã đóng gói toàn bộ quy trình khởi động API, gửi request mô phỏng và thu thập bằng chứng vào script:
```powershell
# Chạy từ thư mục gốc của repository
powershell -ExecutionPolicy Bypass -File docs/uat-feedback/evidence/before-after/capture-after-evidence.ps1
```
Script sẽ tự động:
1. Khởi động tiến trình backend trên cổng `5018` kết nối tới `StudentInternshipMgmtDb_UATBuild`.
2. Kiểm tra và ghi log FB-01 (thông báo tiếng Việt sạch sẽ).
3. Kiểm tra và ghi log FB-02 (bỏ `(Title)` và `(Quantity)`).
4. Kiểm tra và ghi log FB-03 (trim khoảng trắng 2 đầu CV URL; chặn `javascript:`, URL tương đối).
5. Kiểm tra và ghi log FB-04 (chuỗi UTF-8 trong README.md).
6. Kiểm tra và ghi log FB-10 (gửi 2 request đồng thời: 1 request 200, 1 request 400, CSDL tạo đúng 1 đơn).
7. Tự động dọn dẹp và dừng tiến trình API an toàn.

### 3.3 Khởi chạy Ứng dụng để Kiểm tra Giao diện (Thủ công)
- **Chạy Backend**:
  ```powershell
  $env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=StudentInternshipMgmtDb_UATBuild;Integrated Security=True;TrustServerCertificate=True;"
  $env:Jwt__SecretKey = "super_secret_jwt_key_for_uat_testing_at_least_32_characters_long_123456"
  dotnet run --project InternshipManagement/backend/src/StudentInternshipMgmt.Api --urls "http://127.0.0.1:5018"
  ```
- **Chạy Frontend**:
  ```powershell
  cd InternshipManagement/frontend
  npm run dev
  ```
- Mở trình duyệt tại `http://localhost:5173` để trải nghiệm trực quan.
