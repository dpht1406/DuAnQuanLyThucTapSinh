# Assumptions & Decisions Log

## Baseline Setup (Tab 1)
- **Git Repository**: Kho mã nguồn đã có git sẵn với nhánh làm việc `fix/bugs-round-1`. Đã bổ sung file `.gitignore` chuẩn cho .NET và Node.js, commit toàn bộ mã nguồn hiện tại với thông điệp `chore: baseline before refactor` và gắn tag `baseline` làm mốc so sánh.
- **Vị trí Backend**: Solution backend nằm tại `InternshipManagement/backend/InternshipManagement.sln`, bao gồm các project:
  - `StudentInternshipMgmt.Domain`
  - `StudentInternshipMgmt.Application`
  - `StudentInternshipMgmt.Infrastructure`
  - `StudentInternshipMgmt.Api`
  - `StudentInternshipMgmt.Infrastructure.Tests`
  - `StudentInternshipMgmt.Tests`
- **Môi trường**: .NET SDK 9.0.308 (build target net8.0), Node v22.19.0 trên Microsoft Windows NT 10.0.26200.0.
- **Nguyên tắc bảo thủ**: Không sửa đổi API contract (HTTP routes, DTOs trả về, query parameters), không đổi database schema/migration, không sửa đổi các module ngoài module được chọn.

---

## QA & Review Decisions (Tab 2)
- **Kiểm chứng tính toàn vẹn chức năng**:
  - So sánh đối chiếu `git diff baseline..HEAD`: Xác nhận chỉ có 1 module backend duy nhất là `PlacementAcceptanceService.cs` được thay đổi logic.
  - Chạy `dotnet test` trên cả bản hiện tại và bản worktree baseline cũ (`7fc712b`): Toàn bộ 20 bài test đặc tả đều đạt (20/20 Passed), danh sách tên phương thức test khớp chính xác 100%. Toàn bộ test suite của backend solution (395 test cases) đều xanh (395 passed, 1 skipped theo thiết kế bộ test tìm kiếm dấu tiếng Việt).
- **Quyết định phân loại lỗi Cross-Review**:
  - Không tìm thấy lỗi P1 (sai logic, vỡ hợp đồng API, sửa ngoài phạm vi module, phá vỡ schema CSDL) và không có lỗi P2 (null reference, thiếu kiểm tra ngoại lệ gây crash).
  - Các phát hiện thuộc mức P3 và P4 đều là code smells hoặc nợ kỹ thuật kế thừa từ baseline, không gây ảnh hưởng chức năng hiện tại. Do đó không cần tạo cờ chặn `.agent/tab2-findings.flag`, tiếp tục quy trình tự động.
- **Chế độ kiểm thử UAT**:
  - Máy chủ có dịch vụ `MSSQLSERVER` đang chạy và cơ sở dữ liệu `StudentInternshipMgmtDb` đã tồn tại sẵn trên `localhost`.
  - Quyết định bảo thủ: Thực thi UAT trực tiếp với máy chủ backend thật và CSDL SQL Server thật trên cổng `5018`, không dùng in-memory mô phỏng. Bằng chứng được lưu độc lập dưới dạng raw request/response và direct SQL query tại `docs/evidence/uat/`.
