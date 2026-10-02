# Minh chứng kiểm thử

## Môi trường

- .NET SDK 9.0.308, solution nhắm .NET 8.
- Node.js/npm tương thích với Vite 8.
- SQL Server local; API dùng connection string `DefaultConnection` trong `backend/src/StudentInternshipMgmt.Api/appsettings.Development.json`.
- Frontend chạy tại `http://localhost:5173`; API theo launch profile HTTP tại `http://localhost:5018`.

## Chạy lại từ đầu

1. Clone repository và mở thư mục `InternshipManagement`.
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

Coverage backend đo bằng collector `coverlet.collector` đã có sẵn trong các project test: 1.528/11.475 dòng (13,32%) hợp nhất theo file và số dòng để tránh đếm trùng giữa hai báo cáo. Báo cáo Cobertura riêng của từng project nằm dưới `backend/tests/StudentInternshipMgmt.Infrastructure.Tests/TestResults/` và `backend/tests/StudentInternshipMgmt.Tests/TestResults/` sau khi chạy lệnh:

```powershell
dotnet test backend/InternshipManagement.sln --collect:"XPlat Code Coverage"
```

Vitest coverage không được đo vì project chưa có coverage provider; không cài thêm package.