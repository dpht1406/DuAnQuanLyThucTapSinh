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


---

## Release v1.0.0 Decisions (Release Engineer & QA Lead)
- **Thiết kế Môi trường Sạch Biệt Lập (Clean Environment Isolation)**:
  - Môi trường chạy thử demo phát hành được dựng hoàn toàn bên ngoài repository tại thư mục tạm `StudentInternshipMgmt_ReleaseDemo_<timestamp>` (trích xuất qua `git archive` để bảo đảm 0 file thừa, 0 binary, 0 node_modules).
  - Tên cơ sở dữ liệu tạm có tiền tố bắt buộc `StudentInternshipMgmt_ReleaseDemo_<timestamp>` và được bảo vệ bởi chốt chặn an toàn (Safety Interlock) từ chối thực thi nếu trùng tên với cơ sở dữ liệu chính `StudentInternshipMgmtDb`.
  - Toàn bộ chuỗi kết nối, khóa JWT bí mật tạm thời và cổng mạng đều được truyền qua biến môi trường của tiến trình (`Process Environment Variables`), tuyệt đối không chỉnh sửa hay ghi đè các tệp cấu hình đã commit (`appsettings*.json`).
- **Chiến lược Tái lập Không phụ thuộc Mạng Ngoài (Offline/Locked Reproducibility)**:
  - Do môi trường thực thi có thể bị cô lập mạng ngoài (không thể kết nối tới `api.nuget.org:443`), script tự động khai thác bộ đệm cục bộ (`local-cache` tại `C:\Users\PHAT\.nuget\packages\`) và cờ `npm ci --prefer-offline` nhằm đảm bảo tính tái lập 100% trong mọi điều kiện mạng.
  - Chạy từng dự án kiểm thử hoặc chỉ định file dự án cụ thể để tránh xung đột cổng socket nội bộ của trình điều phối VSTest trên Windows.
- **Phương Án Nghiệm Thu UAT & Dữ Liệu Khảo Sát**:
  - Tôn trọng nguyên tắc trung thực nghề nghiệp: Không giả tạo tên người dùng hoặc bịa đặt khảo sát. Toàn bộ 10 phản hồi mẫu người thật từ vòng UAT trước (FB-01 đến FB-10 trong `docs/uat-feedback`) được kế thừa làm căn cứ nghiệp vụ gốc.
  - 10 kịch bản UAT tự động hóa chuyên sâu (`scripts/release/run-uat-scenarios.ps1`) được sử dụng làm phương án kiểm chứng kỹ thuật toàn trình thay thế hợp lệ, kiểm tra tương tác API thật và chụp ảnh màn hình giao diện thật.
- **Quy Trình Xử Lý Lỗi & Tiêu Chuẩn Phát Hành**:
  - Áp dụng nguyên tắc zero P1/P2: Chỉ cho phép phát hành khi 0 lỗi P1 và 0 lỗi P2 còn mở. Lỗi P2 race condition duy nhất đã được khắc phục hoàn toàn bằng Row Lock và DB Transaction, kiểm chứng thực tế đạt 100%.
  - Quản lý phiên bản SemVer nghiêm ngặt: Nâng phiên bản toàn hệ thống lên `1.0.0`, sử dụng file `VERSION` làm nguồn duy nhất đồng bộ sang `Directory.Build.props` và `frontend/package.json`.
