# Báo Cáo Quét Mã Nguồn và Lựa Chọn Module Refactor

## 1. Phương Pháp Quét

Script PowerShell quét toàn bộ mã nguồn C# của backend (`InternshipManagement/backend/src`), loại trừ thư mục `obj/`, `bin/`, `Migrations/` và các file cấu hình máy sinh.

Các tiêu chí đo lường:
- **LOC (Lines of Code)**: Tổng số dòng của file.
- **Method Count**: Số lượng phương thức trong class.
- **Longest Method**: Tên và số dòng của phương thức dài nhất.
- **Max Nesting**: Độ lồng sâu tối đa của khối lệnh điều kiện/lặp (`if`, `for`, `try/catch`).
- **Code Duplication**: Số đoạn code lặp logic hoặc lặp cú pháp rõ rệt.

## 2. Bảng Xếp Hạng Top 5 Ứng Viên

| Hạng | Tên File / Module | LOC | Số Method | Method Dài Nhất | Độ Lồng Sâu | Đoạn Code Lặp Rõ Ràng | Ghi Chú Nghiệp Vụ |
|:---:|---|:---:|:---:|---|:---:|:---:|---|
| **1** | `PlacementAcceptanceService.cs` | **479** | 4 | `AcceptAsync` (228 dòng) | **5** | **5 đoạn** | Nghiệp vụ duyệt/tiếp nhận và hủy tiếp nhận sinh viên thực tập |
| **2** | `CompanyAccountService.cs` | 364 | 4 | `CreateAccountsBulkAsync` (119 dòng) | 5 | 3 đoạn | Nghiệp vụ cấp tài khoản doanh nghiệp hàng loạt |
| **3** | `JobPositionService.cs` | 579 | 13 | `ProjectAdminPositions` (32 dòng) | 2 | 2 đoạn | Quản lý vị trí tuyển dụng, nhiều phương thức CRUD |
| **4** | `CompanyService.cs` | 287 | 7 | `ValidateFields` (38 dòng) | 3 | 2 đoạn | CRUD doanh nghiệp và kiểm tra hợp lệ thông tin |
| **5** | `AuthService.cs` | 229 | 9 | `GenerateAccessToken` (44 dòng) | 3 | 1 đoạn | Đăng nhập, cấp JWT, quản lý refresh token |

## 3. Module Được Chọn và Lý Do

### Module được chọn: `PlacementAcceptanceService.cs`
- **Đường dẫn**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs`
- **Interface tương ứng**: `IPlacementAcceptanceService` nằm tại `StudentInternshipMgmt.Application.Features.PlacementRequests.IPlacementAcceptanceService`

### Lý do lựa chọn:
1. **Quy mô phù hợp chuẩn yêu cầu (150–500 dòng)**: File có tổng cộng 479 dòng mã nguồn, nằm trọn vẹn trong khoảng khuyến nghị (150–500 dòng). Đủ nhỏ để bao phủ kiểm thử 100% các nhánh nghiệp vụ nhưng đủ lớn và phức tạp để mang lại giá trị tái cấu trúc rõ ràng.
2. **Logic nghiệp vụ lõi (Core Business Workflow)**: Chịu trách nhiệm thực thi hai hành động quan trọng nhất trong chu trình thực tập:
   - Tiếp nhận sinh viên (`AcceptAsync`): Xử lý phân quyền tác nhân (Doanh nghiệp trực tiếp duyệt hoặc Admin duyệt thay), kiểm tra chỉ tiêu vị trí tuyển dụng, xác nhận trạng thái sinh viên, cập nhật trạng thái đơn, tự động từ chối các đơn mở khác của sinh viên, tạo lịch sử trạng thái (`StatusHistory`), bắn thông báo hệ thống và gửi email thông báo kết quả.
   - Hủy kết quả tiếp nhận (`CancelAcceptedAsync`): Hoàn tác trạng thái sinh viên về `NoCompany`, chuyển đơn sang `Rejected`, mở lại vị trí tuyển dụng nếu chỉ tiêu còn trống, ghi log lịch sử.
3. **Mức độ phức tạp và nợ kỹ thuật cao**:
   - Phương thức `AcceptAsync` dài tới 228 dòng; `CancelAcceptedAsync` dài 186 dòng. Cả hai đều vi phạm nguyên lý Single Responsibility (SRP).
   - Độ lồng sâu lên tới 5 tầng do lồng ghép khối `try-catch`, kiểm tra điều kiện đa tầng và khối khóa dữ liệu cơ sở dữ liệu.
   - Code lặp nghiêm trọng giữa hai phương thức về xử lý transaction, bắt lỗi `DbUpdateException`/`DbException`, rollback transaction và khóa dòng dữ liệu.
   - Trộn lẫn logic điều phối dữ liệu (database query/locks) với quy tắc nghiệp vụ (business rules) và chuẩn bị thông báo (notification/history).
4. **Khả năng kiểm thử độc lập cao**: Service phụ thuộc vào `AppDbContext`, `ILogger<PlacementAcceptanceService>` và `PlacementRequestEmailNotifier`. Tất cả dependencies đều có thể khởi tạo dễ dàng trong môi trường xUnit với EF Core InMemory, cho phép chạy test độc lập mà không cần hạ tầng SQL Server thật.
