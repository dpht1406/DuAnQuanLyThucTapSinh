# Student Internship Management - Backend

Backend .NET 8 theo Clean Architecture:

```text
StudentInternshipMgmt.Domain
StudentInternshipMgmt.Application
StudentInternshipMgmt.Infrastructure
StudentInternshipMgmt.Api
```

Solution hiện dùng bộ project `StudentInternshipMgmt.*`. Các thư mục skeleton cũ
`src/Api`, `src/Application`, `src/Domain`, `src/Infrastructure` không còn nằm trong
`InternshipManagement.sln`.

## Setup

```powershell
dotnet restore
dotnet build InternshipManagement.sln
```

## JWT signing key

The JWT signing key is intentionally not stored in `appsettings.json`. For local
development, configure a new key with User Secrets:

```powershell
dotnet user-secrets set "Jwt:SecretKey" "<new-random-key-at-least-32-characters>"
```

For production, provide the `Jwt__SecretKey` environment variable through the
deployment secret manager. The application refuses to start when the key is
missing or shorter than 32 characters. The key previously present in settings
was exposed and must be rotated; do not reuse it.

## Email notifications

Email notifications are disabled by default (`Smtp:Enabled=false`). When
enabled, SMTP settings are configured under `Smtp` in `appsettings.json` or
through environment variables such as `Smtp__Host`, `Smtp__Port`,
`Smtp__UseStartTls`, `Smtp__Username`, `Smtp__FromAddress`, and `Smtp__FromName`.
Keep the password out of `appsettings.json`; for local development use User
Secrets:

```powershell
dotnet user-secrets init --project src/StudentInternshipMgmt.Api
dotnet user-secrets set "Smtp:Password" "<smtp-password>" --project src/StudentInternshipMgmt.Api
dotnet user-secrets set "Smtp:Enabled" "true" --project src/StudentInternshipMgmt.Api
```

For production, provide `Smtp__Password` (and `Smtp__Enabled=true`) through the
deployment secret manager. Sending times out after 10 seconds. Failed email
delivery is logged without logging message contents and does not roll back
placement-request operations. The frontend link uses `App:FrontendBaseUrl`.

Connection string mặc định dùng SQL Server local với Windows Authentication:

```json
"DefaultConnection": "Server=localhost;Database=StudentInternshipMgmtDb;Integrated Security=True;TrustServerCertificate=True;"
```

## Database

Apply migration vào SQL Server:

```powershell
dotnet ef database update `
  --project src/StudentInternshipMgmt.Infrastructure `
  --startup-project src/StudentInternshipMgmt.Api
```

Tạo migration mới:

```powershell
dotnet ef migrations add TenMigration `
  --project src/StudentInternshipMgmt.Infrastructure `
  --startup-project src/StudentInternshipMgmt.Api
```

## Run

```powershell
dotnet run --project src/StudentInternshipMgmt.Api
```

## Test

```powershell
dotnet test InternshipManagement.sln
```

## Notes

- `Id` của entity dùng `int`.
- Enum được lưu dạng string trong DB.
- `Student.StudentCode` unique theo filter `[IsDeleted] = 0`.
- `Student` có global query filter cho soft delete.
- FK delete behavior dùng `Restrict`.
- Admin seed: username `admin`, password mặc định `Admin@123`, password đã được BCrypt hash.
- Phase hiện tại chưa có controller/endpoint nghiệp vụ.

## Triển khai

- `AllowedHosts` hiện là `*`; khi triển khai thật phải đổi sang tên miền được phép.
- Nếu triển khai sau reverse proxy, cần cấu hình `UseForwardedHeaders` để lấy đúng IP máy khách; mặc định API không tin header `X-Forwarded-For`.
- Security header cho file tĩnh frontend phải được cấu hình tại web server/CDN phục vụ frontend, không phải API.
