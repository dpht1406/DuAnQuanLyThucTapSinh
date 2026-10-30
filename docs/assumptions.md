# Assumptions & Decisions Log

## Baseline Setup
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
