# Tiến độ — Chỉnh sửa trang Giới thiệu

## Giai đoạn đã xong
Giai đoạn A (Backend) đã hoàn thành vào lúc 16:04 ngày 19/10/2026.
Giai đoạn B (Frontend) đã hoàn thành vào lúc 16:35 ngày 19/10/2026.
Database đã được cập nhật migration AddAboutPage vào SQL Server theo yêu cầu của người dùng.

## Những file đã thêm hoặc sửa
- `backend/src/StudentInternshipMgmt.Domain/Entities/AboutPage.cs`: Thực thể lưu trữ cấu hình trang Giới thiệu kế thừa BaseEntity, chứa ContentJson, UpdatedAt và UpdatedByUserId.
- `backend/src/StudentInternshipMgmt.Infrastructure/Persistence/Configurations/AboutPageConfiguration.cs`: Cấu hình bảng AboutPages với Id không tự tăng (ValueGeneratedNever), ContentJson bắt buộc, không dùng HasData.
- `backend/src/StudentInternshipMgmt.Infrastructure/Persistence/AppDbContext.cs`: Đăng ký DbSet AboutPages và nạp AboutPageConfiguration.
- `backend/src/StudentInternshipMgmt.Application/Features/About/AboutDtos.cs`: Các DTO chi tiết phản ánh cấu trúc nội dung trang Giới thiệu và AboutPageDto.
- `backend/src/StudentInternshipMgmt.Application/Features/About/AboutIcons.cs`: Danh sách 30 biểu tượng PrimeIcons cho phép, đã kiểm tra tồn tại thực tế trong CSS.
- `backend/src/StudentInternshipMgmt.Application/Features/About/AboutContentDefaults.cs`: Dữ liệu mặc định chuyển nguyên văn từ companyProfileSeed.js (đã bỏ phần positions).
- `backend/src/StudentInternshipMgmt.Application/Features/About/AboutContentValidator.cs`: Bộ kiểm tra tính hợp lệ toàn diện theo các giới hạn đề ra và hàm làm sạch chuỗi (TrimContent).
- `backend/src/StudentInternshipMgmt.Application/Features/About/IAboutService.cs`: Giao diện dịch vụ nghiệp vụ gồm lấy thông tin, cập nhật và khôi phục mặc định.
- `backend/src/StudentInternshipMgmt.Infrastructure/Services/AboutService.cs`: Cài đặt dịch vụ nghiệp vụ, xử lý đọc/ghi JSON camelCase, khôi phục mặc định và bắt lỗi trùng khóa race condition.
- `backend/src/StudentInternshipMgmt.Api/Controllers/AboutController.cs`: Controller cung cấp GET /api/about, PUT /api/about và POST /api/about/reset theo đúng chuẩn phân quyền và định dạng ApiResponse.
- `backend/src/StudentInternshipMgmt.Api/Program.cs`: Đăng ký IAboutService dạng Scoped vào DI container.
- `backend/src/StudentInternshipMgmt.Infrastructure/Migrations/20261019090248_AddAboutPage.cs`: Migration AddAboutPage chỉ tạo bảng AboutPages.
- `backend/tests/StudentInternshipMgmt.Infrastructure.Tests/AboutServiceTests.cs`: 23 ca kiểm thử tự động xUnit bao quát các nghiệp vụ và ca biên giới hạn.
- `docs/about-edit/companyProfileSeed.original.js`: Bản sao lưu file seed gốc để TAB 2 đối chiếu.
- `frontend/src/api/about.js`: Hàm gọi API getAbout, updateAbout, resetAbout.
- `frontend/src/utils/aboutContent.js`: Hằng số giới hạn, danh sách 30 icons, hàm sinh id checklist, hàm kiểm tra tính hợp lệ toàn bộ form trả về map lỗi.
- `frontend/src/utils/aboutContent.test.js`: 10 ca kiểm thử tự động Vitest cho các trường hợp biên.
- `frontend/src/components/about/ItemCard.vue`: Thành phần giao diện dùng chung cho các mục danh sách có di chuyển lên/xuống và xóa.
- `frontend/src/views/about/AboutView.vue`: Chuyển sang đọc API getAbout, kết nối dữ liệu vị trí thực tập thật từ API /job-positions, thêm nút Chỉnh sửa nội dung cho Admin, hiển thị thời gian cập nhật lần cuối, ẩn các khối rỗng và đồng bộ mục lục.
- `frontend/src/views/about/AboutEditView.vue`: Form chỉnh sửa toàn diện nhiều phần theo mẫu FormPage/FormSection, hỗ trợ thêm/xóa/đổi thứ tự các danh sách, cuộn đến ô lỗi đầu tiên, khôi phục mặc định có hộp xác nhận, cảnh báo rời trang khi có thay đổi chưa lưu.
- `frontend/src/router/index.js`: Thêm route about/edit cho Admin và tiêu đề trang trong routeTitles.

## Cách chạy để kiểm tra
- Kiểm tra migration đã được cập nhật vào SQL Server (chạy tại thư mục `backend`):
  `dotnet ef database update --project src/StudentInternshipMgmt.Infrastructure --startup-project src/StudentInternshipMgmt.Api`
- Chạy backend:
  `dotnet run --project src/StudentInternshipMgmt.Api`
- Chạy frontend:
  `npm run dev` (tại thư mục `frontend`)
- Đăng nhập tài khoản Admin, mở trang `/about`, bấm nút "Chỉnh sửa nội dung", thực hiện sửa các trường và bấm "Lưu thay đổi".
- Đăng nhập tài khoản Sinh viên / Doanh nghiệp để xác nhận không thấy nút chỉnh sửa và không gọi được API cập nhật.

## Thay đổi so với hợp đồng
Không có.

## Việc chưa làm hoặc điểm chưa chắc chắn
Không có. Cả Backend và Frontend đều đã được xây dựng hoàn thiện, tuân thủ nghiêm ngặt hợp đồng API và các quy tắc accessibility.

## Kết quả test tự động
- Backend: Đạt 314/314 test khả dụng (`dotnet test InternshipManagement.sln`).
- Frontend: Đạt 143/143 test Vitest (`npm test`).
- Kiểm tra nhãn Form: Không có lỗi (`npm run check:labels`).
- Build Frontend: Thành công (`npm run build`).
