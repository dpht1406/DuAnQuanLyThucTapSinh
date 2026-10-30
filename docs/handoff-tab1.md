# Biên Bản Bàn Giao Tab 1 Sang Tab 2 (Handoff Tab 1)

## 1. Module Được Chọn Refactor
- **Tên File**: `PlacementAcceptanceService.cs`
- **Đường dẫn**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs`
- **Interface**: `IPlacementAcceptanceService` (`StudentInternshipMgmt.Application.Features.PlacementRequests.IPlacementAcceptanceService`)
- **Nghiệp vụ**: Xử lý logic tiếp nhận sinh viên thực tập (`AcceptAsync`) và hủy kết quả tiếp nhận sinh viên (`CancelAcceptedAsync`).

---

## 2. Danh Sách Các File Đã Thay Đổi và Tạo Mới

### File mã nguồn refactor:
- `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs` (Giữ nguyên toàn bộ public interface và API contract)

### Project kiểm thử đặc tả (Characterization Tests):
- `tests/PlacementAcceptance.Tests/PlacementAcceptance.Tests.csproj`
- `tests/PlacementAcceptance.Tests/PlacementAcceptanceCharacterizationTests.cs` (22 test cases: 20 characterization tests và 2 test mới cho lỗi lưu dữ liệu/email)

### Solution files:
- `DuAnQuanLyThucTapSinh.sln` (Solution ở thư mục gốc hỗ trợ chạy `dotnet test` trực tiếp không cần tham số)
- `InternshipManagement/backend/InternshipManagement.sln` (Bổ sung tham chiếu test project mới)

### Tài liệu & Bằng chứng thực nghiệm:
- `docs/01-module-selection.md`: Báo cáo quét tự động top 5 module và lý do chọn `PlacementAcceptanceService`.
- `docs/02-problem-description.md`: Liệt kê 7 vấn đề cụ thể kèm số dòng trước refactor.
- `docs/03-before-after.md`: Bảng so sánh chỉ số đo lường thực tế và code trích xuất từ git so sánh cũ - mới.
- `docs/05-bug-log.md`: Nhật ký lỗi và các điểm known issues.
- `docs/assumptions.md`: Các giả định kiến trúc và môi trường thực thi.
- `docs/evidence/env.txt`: Thông tin môi trường (.NET 9.0.308, Node v22.19.0, Windows 11).
- `docs/evidence/metrics-before.txt`: Số đo thô trước refactor.
- `docs/evidence/test-before.txt`: Output thô của `dotnet test` trước refactor (20/20 passed).
- `docs/evidence/metrics-after.txt`: Số đo thô sau refactor.
- `docs/evidence/test-after.txt`: Output thô của `dotnet test` sau refactor (20/20 passed).
- `docs/evidence/diff-stat.txt`: Output thô của `git diff --stat baseline..HEAD`.

---

## 3. Lệnh Build và Test

Tab 2 có thể chạy độc lập các lệnh sau tại thư mục gốc:

1. **Build toàn bộ giải pháp**:
   ```powershell
   dotnet build DuAnQuanLyThucTapSinh.sln
   ```
   *(Hoặc: `dotnet build InternshipManagement/backend/InternshipManagement.sln`)*

2. **Chạy bộ kiểm thử đặc tả của module (22 test cases hiện tại; 20 test trên baseline)**:
   ```powershell
   dotnet test tests/PlacementAcceptance.Tests/PlacementAcceptance.Tests.csproj
   ```
   *(Hoặc chạy lệnh ngắn gọn: `dotnet test`)*

3. **Chạy toàn bộ test suites của backend solution**:
   ```powershell
   dotnet test InternshipManagement/backend/InternshipManagement.sln
   ```

### Số đo đã xác nhận

- `AcceptAsync`: baseline 228 dòng, hiện tại 77 dòng.
- `CancelAcceptedAsync`: baseline 186 dòng, hiện tại 75 dòng.
- Độ lồng block sâu nhất trong class: baseline 5 tầng, hiện tại 4 tầng (bỏ qua comment/chuỗi và không tính block class ngoài).
- Còn 2 nhóm logic lặp: kiểm tra `null` + `scopeCompanyId` hai lần trong `AcceptAsync`; trả kết quả `NotFound` qua `RollbackWithResultAsync` tại bốn nhánh tra cứu request.
- Bộ test module: baseline 20/20; hiện tại 22/22.

---

## 4. Những Điểm Cần Chú Ý & Chưa Chắc Chắn (Cho Tab 2 Review)

1. **EF Core InMemory Database**:
   - Bộ test trong `PlacementAcceptanceCharacterizationTests.cs` chạy với EF Core InMemory, loại bỏ cảnh báo transaction ignored (`InMemoryEventId.TransactionIgnoredWarning`).
   - Do InMemory không chạy câu lệnh SQL thô, nhánh khóa dòng SQL chỉ được kiểm bằng UAT trên SQL Server thật; các test InMemory không xác minh nhánh này.
2. **Các Known Issues (Đã ghi trong `docs/05-bug-log.md`)**:
   - `CleanString`: Tham số `preserveLineBreaks` vốn không được sử dụng ở code cũ. Chúng tôi đã giữ nguyên chữ ký hàm kèm discard `_ = preserveLineBreaks;` để tránh breaking changes.
   - Các chuỗi trả về lỗi được giữ nguyên 100% để đảm bảo không sai lệch JSON response và API contract với Frontend.
