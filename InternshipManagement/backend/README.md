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
