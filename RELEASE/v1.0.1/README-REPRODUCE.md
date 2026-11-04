# Hướng Dẫn Tái Lập & Kiểm Chứng Gói Phát Hành Release v1.0.1

Tài liệu này cung cấp chỉ dẫn chuẩn mực để bất kỳ kỹ sư phát hành (Release Engineer), QA lead hoặc giám định viên độc lập nào cũng có thể kiểm chứng, tái lập toàn bộ ứng dụng từ gói phát hành sạch **bằng đúng một lệnh duy nhất** hoặc kiểm thử thủ công từng bước.

---

## 1. Yêu Cầu Môi Trường (Prerequisites)

| Công cụ / Nền tảng | Phiên bản Tối thiểu | Phiên bản Đã Kiểm Nghiệm Thực Tế | Ghi Chú |
| :--- | :---: | :---: | :--- |
| **Hệ điều hành** | Windows 10/11 x64 | Windows 11 Build 26200 | Khuyến nghị PowerShell 5.1 hoặc PowerShell 7 |
| **.NET SDK** | .NET 8.0 SDK trở lên | **.NET SDK 9.0.308** (Target `net8.0`) | Dùng cho biên dịch và chạy backend |
| **.NET EF CLI Tool** | 8.0.0 trở lên | **8.0.31** | Cài qua `dotnet tool install --global dotnet-ef` |
| **Node.js & npm** | Node 18+ LTS, npm 9+ | **Node v22.19.0**, **npm 10.9.3** | Dùng cho build và kiểm thử frontend |
| **Cơ sở Dữ liệu** | SQL Server 2019+ | **SQL Server 2022 Developer** (`localhost`) | Xác thực Windows (Integrated Security=True) |
| **Git Client** | Git 2.30+ | **Git 2.50.1.windows.1** | Phục vụ kiểm tra mã nguồn |

---

## 2. Kiểm Tra Tính Toàn Vẹn Của Gói (Checksum Verification)

Trước khi giải nén, kiểm tra mã băm SHA256 của gói lưu tại `checksums.sha256`:

```powershell
Get-FileHash -Path RELEASE\v1.0.1\DuAnQuanLyThucTapSinh-v1.0.1.zip -Algorithm SHA256
```
Đối chiếu mã băm hiển thị với dòng tương ứng trong tệp `RELEASE/v1.0.1/checksums.sha256`.

---

## 3. Tái Lập Toàn Bộ Môi Trường Bằng Đúng 1 Lệnh Duy Nhất

Để đảm bảo tính độc lập và an toàn tuyệt đối, hệ thống cung cấp script tự động hóa toàn trình:

```powershell
# Chạy từ thư mục gốc của repository
powershell -ExecutionPolicy Bypass -File scripts\release\run-release.ps1
```

### Lệnh này tự động thực hiện tuần tự:
1. **Dựng môi trường sạch biệt lập**: Xuất mã nguồn sạch ra thư mục tạm mới `StudentInternshipMgmt_ReleaseDemo_<timestamp>` (loại bỏ hoàn toàn mọi file rác và secret), tạo database mới `StudentInternshipMgmt_ReleaseDemo_<timestamp>` trên SQL Server localhost, cài đặt dependency khóa theo lockfile, migrate và seed dữ liệu.
2. **Kiểm thử tự động song song**: Chạy 403 ca kiểm thử backend (`dotnet test`) và 199 ca kiểm thử frontend (`npm test`), kiểm tra độ tương phản (`check:contrast`) và nhãn truy cập (`check:labels`).
3. **Thực thi 10 kịch bản UAT nâng cao**: Đăng nhập đa vai trò, đổi mật khẩu lần đầu bắt buộc, nộp đơn, kiểm soát race condition, phỏng vấn, phê duyệt tiếp nhận, hủy tiếp nhận, tạo tài khoản doanh nghiệp hàng loạt, xuất dữ liệu có UTF-8 BOM và kiểm thử phân quyền Multi-Tenancy.
4. **Kiểm tra chéo và sổ nhật ký lỗi**: Rà soát báo cáo đánh giá chéo độc lập, đối soát 3 rủi ro kỹ thuật và kiểm tra zero P1/P2 mở.
5. **Đóng gói và kiểm chứng tính tái lập điều khiển bởi tài liệu**: Nén gói phát hành sạch từ đúng commit được tag `v1.0.1`, xuất `PROVENANCE.md`, tự động trích xuất các câu lệnh từ chính tài liệu này để build và chạy test trên thư mục rỗng biệt lập.
6. **Dọn dẹp môi trường demo**: Dừng toàn bộ cây tiến trình (PID tree) qua cổng và drop database tạm, bảo vệ tuyệt đối CSDL chính `StudentInternshipMgmtDb`.

---

## 4. Hướng Dẫn Chạy Thủ Công Từng Bước Trên Thư Mục Rỗng

Nếu giám định viên muốn thực hiện thủ công từng bước sau khi giải nén `RELEASE/v1.0.1/DuAnQuanLyThucTapSinh-v1.0.1.zip` vào một thư mục rỗng:

### Bước 4.1: Các Lệnh Biên Dịch & Chạy Bộ Kiểm Thử (Verification Commands)

Các câu lệnh dưới đây là chỉ dẫn chuẩn mực, được script phát hành tự động trích xuất và thực thi trực tiếp để kiểm chứng tính toàn vẹn:

```powershell
dotnet restore DuAnQuanLyThucTapSinh.sln
dotnet build DuAnQuanLyThucTapSinh.sln --no-restore -c Release
dotnet test tests\PlacementAcceptance.Tests\PlacementAcceptance.Tests.csproj --no-build -c Release
dotnet test InternshipManagement\backend\tests\StudentInternshipMgmt.Tests\StudentInternshipMgmt.Tests.csproj --no-build -c Release
dotnet test InternshipManagement\backend\tests\StudentInternshipMgmt.Infrastructure.Tests\StudentInternshipMgmt.Infrastructure.Tests.csproj --no-build -c Release
cd InternshipManagement\frontend
npm ci --prefer-offline
npm test
npm run check:contrast
npm run check:labels
npm run build
cd ..\..
```

*Kết quả mong đợi*:
- Solution build thành công với 0 lỗi (toàn bộ 4 src và 3 test projects).
- Backend test: 402 Passed, 1 Skipped theo thiết kế, 0 Failed.
- Frontend test: 199 Passed, 0 Failed.
- Contrast & Labels: 100% ĐẠT.
- Frontend build: Tạo thư mục `dist` thành công.

---

### Bước 4.2: Khởi Tạo Cơ Sở Dữ Liệu Demo Biệt Lập & Khởi Động Ứng Dụng

> [!CAUTION]
> Tuyệt đối không dùng tên database chính `StudentInternshipMgmtDb`. Hãy dùng tên riêng biệt như ví dụ dưới đây:

```powershell
# Thiết lập biến môi trường kết nối tới DB demo
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=StudentInternshipMgmt_ManualDemo;Integrated Security=True;TrustServerCertificate=True;"
$env:Jwt__SecretKey = "super_secure_jwt_secret_key_at_least_32_characters_long_123456"
$env:Smtp__Enabled = "false"
$env:PlacementRequestExpiry__Enabled = "false"

# Chạy migration và seed dữ liệu ban đầu
dotnet ef database update --project InternshipManagement\backend\src\StudentInternshipMgmt.Infrastructure --startup-project InternshipManagement\backend\src\StudentInternshipMgmt.Api

# Khởi động Backend API
dotnet run --no-build -c Release --project InternshipManagement\backend\src\StudentInternshipMgmt.Api --urls "http://127.0.0.1:5018"
```

Mở một cửa sổ PowerShell khác để chạy Frontend:
```powershell
cd InternshipManagement\frontend
$env:VITE_API_BASE_URL = "http://127.0.0.1:5018/api"
npm run dev -- --port 5173 --host 127.0.0.1
```

Truy cập ứng dụng tại `http://127.0.0.1:5173`.

---

## 5. Danh Sách Tài Khoản Dữ Liệu Mẫu (Demo Accounts)

> [!NOTE]
> Mọi tài khoản mới hoặc tài khoản seed dưới đây đều kích hoạt cơ chế bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên (`MustChangePassword = true`) nhằm tuân thủ quy chuẩn an toàn thông tin của hệ thống.

| Vai Trò | Tên Đăng Nhập | Mật Khẩu Khởi Tạo | Yêu Cầu Đổi Mật Khẩu Lần Đầu | Mô Tả |
| :--- | :---: | :---: | :---: | :--- |
| **Quản trị viên (Admin)** | `admin` | `Admin@123` | **Có** | Cán bộ quản lý thực tập trường |
| **Doanh nghiệp mẫu 1** | Sinh qua API bulk | Tự động tạo | **Có** | Công ty Giải pháp Phần mềm ABC |
| **Doanh nghiệp mẫu 2** | Sinh qua API bulk | Tự động tạo | **Có** | Công ty Cổ phần Công nghệ XYZ |
| **Sinh viên mẫu** | Sinh qua API student | Tự động tạo | **Có** | Sinh viên năm cuối ứng tuyển thực tập |
