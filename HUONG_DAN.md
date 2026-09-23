# Hướng dẫn áp file fix vào project

## 1. Xoá file DTO bị lạc chỗ (QUAN TRỌNG — làm trước, không sẽ bị lỗi trùng class)
Xoá file này:
```
backend/src/StudentInternshipMgmt.Application/Features/Auth/Dtos/ChangeStatusDto.cs
```
(nó đúng nội dung nhưng đặt sai thư mục — bản đúng đã có sẵn trong gói fix này,
ở đúng chỗ `Features/Students/`. Giữ cả 2 file sẽ bị lỗi CS0101 "trùng định nghĩa class".)

## 2. Copy các file trong gói fix này vào đúng vị trí (ghi đè nếu đã có)
- `backend/src/StudentInternshipMgmt.Application/Features/Students/*.cs` → copy nguyên
  8 file vào đúng thư mục này trong project thật (tạo thư mục `Students` nếu chưa có).
- `backend/src/StudentInternshipMgmt.Api/Controllers/StudentsController.cs` → copy vào
  `Api/Controllers/` (project thật của bạn tên là `StudentInternshipMgmt.Api`).
- `backend/src/StudentInternshipMgmt.Infrastructure/Services/StudentService.cs` → **ghi đè**
  file cũ (chỉ khác đúng 1 dòng `PagedResult`, nhưng cứ ghi đè cho chắc).

## 3. Sửa Program.cs — thêm 3 dòng đăng ký DI
Program.cs hiện tại đã có `AddAuthentication`, `AddAuthorization`, `AddControllers()` —
chỉ thiếu đăng ký `IStudentService` và FluentValidation. Thêm vào ngay sau dòng
`builder.Services.AddScoped<IAuthService, AuthService>();`:

```csharp
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();
```

Và thêm 2 using ở đầu file:
```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using StudentInternshipMgmt.Application.Features.Students;
```

## 4. Build lại
```
cd backend
dotnet build
```

## Dọn dẹp thêm (không bắt buộc, chỉ để đỡ rối)
File `.sln` của bạn **không** tham chiếu tới 4 project không-tiền-tố:
`backend/src/Api`, `backend/src/Application`, `backend/src/Domain`,
`backend/src/Infrastructure` (không có `StudentInternshipMgmt.` phía trước).
Chúng không ảnh hưởng build nhưng dễ gây nhầm lẫn khi tìm file — có thể xoá
nếu không dùng, hoặc nói mình biết nếu đó là structure bạn định dùng thay thế.
