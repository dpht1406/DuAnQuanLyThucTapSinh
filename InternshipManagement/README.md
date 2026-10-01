# Internship Management

## Trang Vị trí thực tập

Tạo migration từ thư mục `backend` bằng lệnh `dotnet ef migrations add AddJobPositionListFields --project src/StudentInternshipMgmt.Infrastructure --startup-project src/StudentInternshipMgmt.Api`. Sau khi kiểm tra migration, cập nhật cơ sở dữ liệu bằng `dotnet ef database update --project src/StudentInternshipMgmt.Infrastructure --startup-project src/StudentInternshipMgmt.Api`, rồi chạy API với `dotnet run --project src/StudentInternshipMgmt.Api`.

Chạy giao diện từ thư mục `frontend` bằng `npm install` và `npm run dev`. Trang đăng nhập `/job-positions` yêu cầu tài khoản Admin hoặc User.

Há»‡ thá»‘ng quáº£n lÃ½ sinh viÃªn thá»±c táº­p (ASP.NET Core 8 + Vue 3).
