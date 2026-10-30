# Changelog - Gói Phát Hành Refactor Module PlacementAcceptanceService

Toàn bộ các thay đổi trong đợt tái cấu trúc (refactoring) được thực hiện tuần tự qua các commit nguyên tử (atomic commits) nhằm đảm bảo an toàn tuyệt đối, chức năng không đổi và có thể theo dõi nguồn gốc rõ ràng.

---

## Danh Sách Commits Refactor & Fix

### [5f52ca5] `test: characterization tests before refactor`
- **Mục đích**: Xây dựng bộ kiểm thử đặc tả hành vi (Characterization Tests) bao phủ 20 kịch bản cho module `PlacementAcceptanceService` trước khi tiến hành sửa đổi mã nguồn.
- **Tập tin thêm mới**:
  - `tests/PlacementAcceptance.Tests/PlacementAcceptance.Tests.csproj`
  - `tests/PlacementAcceptance.Tests/PlacementAcceptanceCharacterizationTests.cs`
- **Kết quả**: 20/20 test cases vượt qua trên bản mã nguồn gốc (baseline).

### [898644e] `refactor(placement-acceptance): extract constants and messages to eliminate magic values`
- **Mục đích**: Khai trừ các chuỗi ma thuật (magic strings) và số cứng (magic numbers) phân tán trong service thành các hằng số `private const string` và `private const int`.
- **Tập tin thay đổi**:
  - `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs`
- **Các hằng số được chuẩn hóa**:
  - `RequestNotFoundMessage`, `InterviewReportAlreadyHandledMessage`, `RequestAlreadyHandledMessage`
  - `JobPositionNotApprovedMessage`, `CompanyNotFoundMessage`, `OnlyAcceptedCanBeCanceledMessage`
  - `StudentNotFoundMessage`, `StudentDeletedMessage`, `StudentDataMismatchMessage`
  - `NoteTooLongMessage`, `DefaultCancellationNote`, `AdminCancelConfirmationSource`
  - `MaxCancellationNoteLength` (1000)

### [1514649] `refactor(placement-acceptance): consolidate transaction and locking logic`
- **Mục đích**: Hợp nhất đoạn mã trùng lặp về quản lý transaction (`BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`, `DisposeAsync`, bắt lỗi `DbUpdateException` / `DbException`) giữa `AcceptAsync` và `CancelAcceptedAsync`.
- **Phương thức mới**:
  - `ExecuteInTransactionAsync`: Bao đóng toàn bộ vòng đời của transaction và xử lý lỗi đồng bộ.
  - `LockRelationalRowsForAcceptanceAsync`: Tách riêng các câu lệnh SQL khóa hàng bi quan khi chạy trên CSDL quan hệ.
  - `LockRelationalRowsForCancellationAsync`: Tách riêng khóa dòng phục vụ hủy kết quả tiếp nhận.

### [48b2134] `refactor(placement-acceptance): decompose AcceptAsync into business validation and domain transition methods`
- **Mục đích**: Tách nhỏ phương thức `AcceptAsync` (từ 228 dòng xuống còn 76 dòng), giảm độ lồng sâu từ 5 tầng xuống 2 tầng, tuân thủ nguyên lý Đơn Trách Nhiệm (SRP).
- **Phương thức con được phân tách**:
  - `GetRequestSummaryAsync`: Đọc nhanh thông tin định danh phục vụ khóa dòng.
  - `ValidateAcceptanceActorPermission`: Kiểm tra phân quyền Admin vs Doanh nghiệp và trạng thái hợp lệ của đơn.
  - `ValidateJobPositionForAcceptanceAsync`: Kiểm tra phê duyệt và chỉ tiêu còn lại của vị trí tuyển dụng.
  - `ApplyRequestAcceptance`: Gán trạng thái và thông tin phê duyệt cho đơn ứng tuyển.
  - `ApplyStudentAcceptance`: Gán trạng thái `Accepted`, `CompanyId`, `JobPositionId` cho sinh viên.
  - `AddAcceptanceStatusHistory`: Ghi nhận lịch sử chuyển trạng thái sinh viên kèm ghi chú chuẩn hóa.
  - `AutoRejectOtherStudentRequestsAsync`: Tự động từ chối các đơn mở khác của sinh viên khi đã được nhận.
  - `CheckAndCloseJobPositionIfFull`: Tự động đóng vị trí tuyển dụng khi đã đủ số lượng tuyển.
  - `CreateAcceptanceNotification`: Tạo thông báo hệ thống cho sinh viên và doanh nghiệp.

### [bde2cea] `refactor(placement-acceptance): decompose CancelAcceptedAsync and use consistent status checks`
- **Mục đích**: Tách nhỏ phương thức `CancelAcceptedAsync` (từ 186 dòng xuống 74 dòng), sử dụng mảng hằng số trạng thái `AcceptedStatuses` đồng nhất.
- **Phương thức con được phân tách**:
  - `ValidateStudentForCancellation`: Kiểm tra sinh viên có tồn tại, chưa bị xóa mềm, và dữ liệu khớp với đơn đã nhận.
  - `ApplyStudentCancellation`: Hoàn tác sinh viên về trạng thái `NoCompany`.
  - `ApplyRequestCancellation`: Đổi trạng thái đơn sang `Rejected` kèm ghi chú hủy.
  - `AddCancellationStatusHistory`: Ghi nhận lịch sử hủy vào bảng `StatusHistories`.
  - `ReopenJobPositionIfCapacityAvailableAsync`: Tự động mở lại vị trí tuyển dụng nếu chỉ tiêu còn trống sau khi hủy.

### [baf748e] `docs: add before-after comparison, evidence metrics, and handoff report`
- **Mục đích**: Bổ sung đầy đủ báo cáo đối chiếu định lượng chỉ số trước/sau, output thô của lệnh đo lường và biên bản bàn giao từ Tab 1.
