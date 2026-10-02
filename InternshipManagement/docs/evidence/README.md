# Minh chứng kiểm thử

## Môi trường

- .NET SDK 9.0.308, solution nhắm .NET 8.
- Node.js/npm tương thích với Vite 8.
- SQL Server local; API dùng connection string `DefaultConnection` trong `backend/src/StudentInternshipMgmt.Api/appsettings.Development.json`.
- Frontend chạy tại `http://localhost:5173`; API theo launch profile HTTP tại `http://localhost:5018`.

## Chạy lại từ đầu

1. Clone repository (thay `<repository-url>` bằng URL Git thực tế) và vào thư mục dự án:

```powershell
git clone <repository-url> InternshipManagement
Set-Location InternshipManagement
```

2. Cấu hình SQL Server và connection string `DefaultConnection` trong `backend/src/StudentInternshipMgmt.Api/appsettings.Development.json`. Không đưa thông tin đăng nhập thật vào Git.
3. Trong thư mục `backend`, khôi phục package, cập nhật database, rồi chạy API:

```powershell
dotnet restore
dotnet ef database update --project src/StudentInternshipMgmt.Infrastructure --startup-project src/StudentInternshipMgmt.Api
dotnet run --project src/StudentInternshipMgmt.Api
```

4. Mở terminal khác, vào `frontend`, cài dependency khóa trong `package-lock.json` và chạy Vite:

```powershell
npm ci
npm run dev
```

5. Chạy test từ thư mục `InternshipManagement`:

```powershell
dotnet test backend/InternshipManagement.sln
Set-Location frontend
npm ci
npm run test
```

## Tệp minh chứng

- `dotnet-test-output.txt`: output chạy toàn solution backend.
- `vitest-output.txt`: output chạy toàn bộ test frontend.
- `TEST-CASES.md`: ca kiểm thử form, kết quả tự động và phần UI còn chờ chạy.
- `sample-logs.txt`: log Information/Warning định dạng bởi logger thật của service trong test.
- `screenshots/`: ảnh kiểm thử giao diện.

Ảnh dùng tên `TC-xx-before.png` và `TC-xx-after.png`, với `xx` là mã ca trong `TEST-CASES.md`.

## Kết quả lần chạy

- Backend solution: 113/113 pass, 0 fail; 7,527 giây wall-clock. Gồm `StudentInternshipMgmt.Tests` 54/54 và `StudentInternshipMgmt.Infrastructure.Tests` 59/59.
- Frontend: 48/48 pass, 0 fail; Vitest duration 627 ms.
- Backend build/test output có warning NU1903 (AutoMapper 13.0.1 advisory) và MSB3277 (xung đột phiên bản EF Core 8.0.10/8.0.31); đây là warning dependency/build đang tồn tại, không do test assertion mới.

Coverage backend được thu bằng `coverlet.collector` đã có sẵn trong các project test, không cài thêm package. Báo cáo Cobertura riêng của từng project được tạo dưới `backend/tests/StudentInternshipMgmt.Infrastructure.Tests/TestResults/` và `backend/tests/StudentInternshipMgmt.Tests/TestResults/` khi chạy lệnh:

```powershell
dotnet test backend/InternshipManagement.sln --collect:"XPlat Code Coverage"
```

Lần chạy evidence hiện tại ghi nhận line coverage: `StudentInternshipMgmt.Tests` 84/11.470 (0,73%) và `StudentInternshipMgmt.Infrastructure.Tests` 1.449/11.470 (12,63%). Đây là coverage riêng theo từng project, không cộng gộp vì các assembly được instrument có phần mã trùng nhau.

Vitest coverage không được đo vì project chưa có coverage provider; không cài thêm package.