# Hướng dẫn Tái lập và Kiểm thử Gói Phát hành UAT (README Reproduce - UAT Round)

Tài liệu này cung cấp các bước chính xác để bất kỳ kỹ sư hoặc kiểm thử viên nào cũng có thể dựng lại môi trường, chạy kiểm thử và nghiệm thu các bản vá lỗi UAT Đợt 2 một cách độc lập.

---

## 1. Yêu cầu Tiên quyết (Prerequisites)

- **Hệ điều hành**: Windows 10/11 x64 hoặc Windows Server.
- **SDK & Runtime**:
  - .NET 8 SDK (`dotnet --version` >= 8.0.x)
  - Node.js (`node --version` >= 20.x, khuyến nghị v22.x) & npm
  - SQL Server LocalDB hoặc SQL Server Developer instance tại `localhost`
  - PowerShell 7 hoặc Windows PowerShell 5.1

---

## 2. Kiểm tra Bản dựng và Toàn bộ Kiểm thử Tự động

### 2.1 Biên dịch Solution
```powershell
# Chạy từ thư mục gốc của repository
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

### 2.3 Chạy Frontend Tests và Production Build
```powershell
cd InternshipManagement/frontend

# Chạy Unit Tests Vitest
npm test

# Chạy đóng gói Production Build
npm run build
```
*Kết quả kỳ vọng*:
- `Test Files: 16 passed (16)`
- `Tests: 190 passed (190)`
- Build thư mục `dist` thành công không có lỗi.

---

## 3. Chạy Thử nghiệm Thực tế trên CSDL UAT Tách biệt

> [!IMPORTANT]
> **Tuyệt đối không chạy trên CSDL thật `StudentInternshipMgmtDb`**. Hãy dùng CSDL kiểm nghiệm `StudentInternshipMgmtDb_UATBuild` thông qua biến môi trường.

### 3.1 Tự động hóa bằng PowerShell Script
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

### 3.2 Khởi chạy Ứng dụng để Kiểm tra Giao diện (Thủ công)
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
