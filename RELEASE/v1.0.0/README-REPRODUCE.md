# Hướng Dẫn Tái Lập & Kiểm Chứng Gói Phát Hành Release v1.0.0

Tài liệu này cung cấp chỉ dẫn chuẩn mực để bất kỳ kỹ sư phát hành (Release Engineer), QA lead hoặc giám định viên độc lập nào cũng có thể kiểm chứng, tái lập toàn bộ ứng dụng từ gói phát hành sạch **bằng đúng một lệnh duy nhất**.

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
Get-FileHash -Path RELEASE\v1.0.0\DuAnQuanLyThucTapSinh-v1.0.0.zip -Algorithm SHA256
```
Đối chiếu mã băm hiển thị với dòng tương ứng trong tệp `RELEASE/v1.0.0/checksums.sha256`.

---

## 3. Tái Lập Toàn Bộ Môi Trường Bằng Đúng 1 Lệnh Duy Nhất

Để đảm bảo tính độc lập và an toàn tuyệt đối, hệ thống cung cấp script tự động hóa toàn trình:

```powershell
# Chạy từ thư mục gốc của repository
powershell -ExecutionPolicy Bypass -File scripts\release\run-release.ps1
```

### Lệnh này tự động thực hiện tuần tự:
1. **Dựng môi trường sạch biệt lập**: Xuất mã nguồn sạch ra thư mục tạm mới `StudentInternshipMgmt_ReleaseDemo_<timestamp>` (loại bỏ hoàn toàn mọi file rác và secret), tạo database mới `StudentInternshipMgmt_ReleaseDemo_<timestamp>` trên SQL Server localhost, cài đặt dependency khóa theo lockfile, migrate và seed dữ liệu.
2. **Kiểm thử tự động song song**: Chạy 403 ca kiểm thử backend (`dotnet test`) và 194 ca kiểm thử frontend (`npm test`), kiểm tra độ tương phản (`check:contrast`) và nhãn truy cập (`check:labels`).
3. **Thực thi 10 kịch bản UAT nâng cao**: Đăng nhập đa vai trò, đổi mật khẩu lần đầu bắt buộc, nộp đơn, kiểm soát race condition, phỏng vấn, phê duyệt tiếp nhận, hủy tiếp nhận, tạo tài khoản doanh nghiệp hàng loạt, xuất dữ liệu và kiểm thử phân quyền Multi-Tenancy.
4. **Đóng gói và kiểm chứng tính tái lập**: Nén gói phát hành sạch, tự động giải nén sang một thư mục rỗng khác để build thử nghiệm, ghi nhận nhật ký băm và log kiểm tra.
5. **Dọn dẹp môi trường demo**: Dừng toàn bộ cây tiến trình (PID tree) qua cổng và drop database tạm, bảo vệ tuyệt đối CSDL chính `StudentInternshipMgmtDb`.

---

## 4. Hướng Dẫn Chạy Thủ Công Từng Bước (Nếu Cần Kiểm Tra Độc Lập)

Nếu giám định viên muốn thực hiện thủ công từng bước trên một thư mục trống:

### Bước 4.1: Giải nén gói mã nguồn sạch
Giải nén `RELEASE/v1.0.0/DuAnQuanLyThucTapSinh-v1.0.0.zip` vào một thư mục rỗng, ví dụ `C:\TestDemo\DuAnQuanLyThucTapSinh`.

### Bước 4.2: Cài đặt Dependencies & Biên dịch
```powershell
cd C:\TestDemo\DuAnQuanLyThucTapSinh

# 1. Khôi phục dependencies backend
dotnet restore DuAnQuanLyThucTapSinh.sln

# 2. Biên dịch solution backend
dotnet build DuAnQuanLyThucTapSinh.sln --no-restore -c Release

# 3. Cài đặt dependencies và build frontend
cd InternshipManagement\frontend
npm ci
npm run build
cd ..\..
```

### Bước 4.3: Chạy Toàn Bộ Kiểm Thử
```powershell
# Chạy 3 test projects backend (Tổng cộng 403 tests: 402 Passed, 1 Skipped theo thiết kế)
dotnet test tests\PlacementAcceptance.Tests\PlacementAcceptance.Tests.csproj --no-build -c Release
dotnet test InternshipManagement\backend\tests\StudentInternshipMgmt.Tests\StudentInternshipMgmt.Tests.csproj --no-build -c Release
dotnet test InternshipManagement\backend\tests\StudentInternshipMgmt.Infrastructure.Tests\StudentInternshipMgmt.Infrastructure.Tests.csproj --no-build -c Release

# Chạy kiểm thử frontend (194 tests Passed)
cd InternshipManagement\frontend
npm test
npm run check:contrast
npm run check:labels
cd ..\..
```

### Bước 4.4: Khởi Tạo Cơ Sở Dữ Liệu Demo Biệt Lập & Khởi Động
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
