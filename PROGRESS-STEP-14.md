# PROGRESS-STEP-14: Mở rộng thông tin đơn ứng tuyển
- Ngày cập nhật: 2026-10-02
- Trạng thái: Đang chờ xác minh trên database local

## Đã làm
- Mở rộng `PlacementRequest` với snapshot họ tên, email, số điện thoại, trường, ngành, CV và lời giới thiệu; cập nhật DTO, mapping, chuẩn hóa dữ liệu và logging không chứa thông tin nhạy cảm.
- Thêm FluentValidation cho payload tạo đơn và kiểm tra lỗi theo field. URL CV chỉ chấp nhận URI tuyệt đối HTTP/HTTPS, không cho phép khoảng trắng.
- Thêm migration `AddApplicationFormFields` gồm 7 `AddColumn` nullable đúng độ dài; migration chưa được áp dụng vào database local.
- Cập nhật form ứng tuyển và validate phía frontend; bổ sung test backend/frontend và tài liệu test evidence.

## Kết quả kiểm tra
- Backend validator: 49/49 test đạt.
- PlacementRequest service: 10/10 test đạt.
- Frontend Vitest: 41/41 test đạt.
- Build toàn solution: exit code 0, 0 error, 11 warning. Có warning dependency AutoMapper, xung đột phiên bản EF Core và warning hiện hữu trong service/test; validator sửa trong bước này không phát sinh warning.
- POST body `{}` trả HTTP 400 với `errors` dạng dictionary theo tên field. `CvUrl: "javascript:alert(1)"` trả HTTP 400 ở `errors.CvUrl`.
- GET `/api/me/placement-requests` trên DB hiện tại trả HTTP 500 vì 7 cột mới chưa tồn tại. Chưa thử POST hợp lệ để ghi đơn vào DB.

## Việc còn tồn
- Chạy `dotnet ef database update --project .\src\StudentInternshipMgmt.Infrastructure\StudentInternshipMgmt.Infrastructure.csproj --startup-project .\src\StudentInternshipMgmt.Api\StudentInternshipMgmt.Api.csproj` từ thư mục `InternshipManagement/backend`.
- Sau khi cập nhật DB, thử POST hợp lệ và xác nhận GET đọc được đơn mới cùng đơn cũ có 7 field null.
- Kiểm thử UI thủ công, gồm gửi form, chống gửi lặp và viewport mobile, chưa chạy.

## Hiện tại đang ở
- Đang thực hiện: STEP 14.
- Bước tiếp theo: áp dụng migration trên DB local và hoàn tất kiểm thử tay.