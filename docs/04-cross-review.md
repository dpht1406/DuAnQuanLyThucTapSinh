# Báo Cáo Review Chéo Mã Nguồn Refactor (Cross-Review)

## 1. Thông Tin Chung
- **Module được review**: `PlacementAcceptanceService.cs`
- **Đường dẫn**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs`
- **Người thực hiện review**: Tab 2 (Reviewer độc lập + QA)
- **Mốc so sánh đối chiếu**: Tag `baseline` (`7fc712b`) so với `HEAD` (`baf748e`)
- **Bộ kiểm thử đặc tả (Characterization Tests)**: `tests/PlacementAcceptance.Tests/PlacementAcceptanceCharacterizationTests.cs` (20/20 test cases pass cả trên baseline và bản refactor)

---

## 2. Phương Pháp & Phạm Vi Đánh Giá

Quá trình kiểm chứng được tiến hành độc lập bằng việc:
1. So sánh `git diff baseline..HEAD` chi tiết từng dòng code.
2. Kiểm tra tính toàn vẹn của interface `IPlacementAcceptanceService` và các API contract/DTO liên quan.
3. Rà soát 11 khía cạnh an toàn mã nguồn:
   - Thay đổi ngầm logic nghiệp vụ (Hidden business logic change)
   - Điều kiện biên & phép so sánh (Boundary & comparison conditions)
   - Xử lý giá trị null và nullable reference types
   - Cơ chế xử lý ngoại lệ và an toàn transaction (DbException, DbUpdateException, Rollback)
   - Xác thực đầu vào (Input validation)
   - Kiểm soát quyền tác nhân (Company vs Admin vs Unauthorized)
   - Rò rỉ thông tin nhạy cảm qua log hoặc thông báo
   - Quy chuẩn đặt tên biến, tên hàm (Naming conventions & readability)
   - Loại bỏ trùng lặp mã nguồn (Code deduplication)
   - Độ dài phương thức & độ lồng sâu (Method length & cyclomatic complexity)
   - Chú thích mã nguồn (Comments & docstrings)

---

## 3. Bảng Tổng Hợp Các Điểm Phát Hiện (Findings)

| Mã | Vị trí (File:Dòng) | Mức ưu tiên | Phân loại | Mô tả tóm tắt | Đề xuất khắc phục |
|:---:|---|:---:|---|---|---|
| **CR-01** | `PlacementAcceptanceService.cs:572-577` | **P4** | Code Smell | Tham số `preserveLineBreaks` trong `CleanString` không được sử dụng thực tế (dùng discard `_ = preserveLineBreaks;`). Tồn tại từ baseline (`BUG-PRE-01`). | Chuẩn hóa hàm làm sạch chuỗi khi có thư viện tiện ích chung; hiện tại giữ nguyên để đảm bảo tương thích 100% chữ ký hàm. |
| **CR-02** | `PlacementAcceptanceService.cs:150-171` | **P3** | Hiệu năng | Trong luồng `CancelAcceptedAsync` trên CSDL quan hệ, có 2 lần truy vấn entity đơn ứng tuyển (`GetRequestSummaryAsync` và `GetPlacementRequestByIdAsync`). | Có thể tối ưu gom thành một lần đọc nếu không yêu cầu phân tách bước khóa dòng trước tracking. |
| **CR-03** | `PlacementAcceptanceService.cs:235-257` | **P4** | Khả năng bảo trì | Template log chung giữ nguyên câu lỗi của Accept nhưng thêm từ `transaction` vào log lỗi chính của Cancel so với baseline. | Chấp nhận khác biệt nếu hệ thống giám sát không phụ thuộc chuỗi cũ; kiểm tra matcher riêng cho luồng Cancel. |
| **CR-04** | `PlacementAcceptanceService.cs:112-117` | **P3** | Nghiệp vụ biên | `AcceptAsync` chỉ kiểm tra công ty tồn tại, chưa kiểm tra trạng thái hoạt động của doanh nghiệp (kế thừa từ baseline). | Bổ sung kiểm tra cờ hoạt động doanh nghiệp nếu domain entity hỗ trợ trong tương lai. |
| **CR-05** | `PlacementAcceptanceService.cs:138-141` | **P4** | Thiết kế | Bắn email thông báo sau commit transaction; nếu tiến trình chết đột ngột sau commit thì email chưa được gửi. | Tương lai có thể triển khai Outbox Pattern cho các tác vụ gửi thông báo/email. |

---

## 4. Chi Tiết Từng Phát Hiện

### CR-03: Đối chiếu log transaction ở cả hai luồng với baseline

Đối chiếu trực tiếp bằng `git show baseline:InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs`:

- **Accept baseline**: `Placement acceptance transaction failed: RequestId {RequestId}`.
- **Cancel baseline**: `Placement acceptance cancellation failed: RequestId {RequestId}`.
- **Sau refactor**: `ExecuteInTransactionAsync` dùng `"{OperationName} transaction failed: RequestId {RequestId}"`. Với operation name của Accept, câu log không đổi; với Cancel, câu log trở thành `Placement acceptance cancellation transaction failed: RequestId {RequestId}`.
- **Rollback**: baseline có hai template riêng `Placement acceptance rollback failed...` và `Placement acceptance cancellation rollback failed...`; template mới `"{OperationName} rollback failed..."` giữ nguyên cả hai câu sau khi nội suy operation name.

Vì vậy CR-03 chỉ áp dụng cho log lỗi chính của Cancel; mô tả cũ rằng cả template transaction được bổ sung là không chính xác.

### CR-01: Tham số `preserveLineBreaks` không được sử dụng trong `CleanString`
- **Tập tin**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs` (Dòng 572–577)
- **Mức ưu tiên**: P4 (Thấp)
- **Hiện trạng**:
  ```csharp
  private static string? CleanString(string? value, bool preserveLineBreaks = false)
  {
      // Parameter preserveLineBreaks is documented as known issue BUG-PRE-01 (see docs/05-bug-log.md)
      _ = preserveLineBreaks;
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
  ```
- **Đánh giá**: Tab 1 đã phát hiện vấn đề này từ baseline và chủ động đánh dấu discard `_ = preserveLineBreaks;` kèm chú thích dẫn chiếu đến `docs/05-bug-log.md`. Không gây ảnh hưởng đến tính đúng đắn của ứng dụng vì các chuỗi ghi chú thông thường vẫn giữ nguyên các ký tự ngắt dòng bên trong chuỗi sau lệnh `.Trim()`.

### CR-02: Truy vấn lặp lại trong `CancelAcceptedAsync` trên Relational DB
- **Tập tin**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs` (Dòng 150–171)
- **Mức ưu tiên**: P3 (Trung bình)
- **Hiện trạng**:
  Khi `_db.Database.IsRelational()` là true:
  1. `GetRequestSummaryAsync(requestId)` được gọi để lấy `StudentId` và `JobPositionId` phục vụ câu lệnh `UPDATE [Students] SET [Status] = [Status]` và `UPDATE [JobPositions] SET [IsOpen] = [IsOpen]` khóa bi quan (pessimistic lock).
  2. Ngay sau đó, `GetPlacementRequestByIdAsync(requestId)` lại được gọi để lấy toàn bộ entity `PlacementRequest` phục vụ cập nhật trạng thái.
- **Đánh giá**: Logic này phản ánh chính xác thiết kế gốc của baseline nhằm tránh nạp entity vào context trước khi hoàn tất khóa hàng. Tuy nhiên, việc thực thi 2 truy vấn đọc cho 1 bản ghi trong cùng transaction là một cơ hội có thể tối ưu thêm.

### CR-03: Thay đổi nhẹ định dạng chuỗi Log lỗi Transaction
- **Tập tin**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs` (Dòng 235–257)
- **Mức ưu tiên**: P4 (Thấp)
- **Hiện trạng**:
  Hàm tái sử dụng `ExecuteInTransactionAsync` ghi log theo format:
  `"{OperationName} transaction failed: RequestId {RequestId}"`
  Với `operationName = "Placement acceptance"`, chuỗi sinh ra là: `"Placement acceptance transaction failed: RequestId 123"`, trong khi ở code cũ là `"Placement acceptance failed: RequestId 123"`.
- **Đánh giá**: Thông điệp log mới rõ ràng và chính xác hơn về ngữ cảnh lỗi transaction, không ảnh hưởng chức năng.

### CR-04: Chưa kiểm tra trạng thái kích hoạt của Doanh nghiệp
- **Tập tin**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs` (Dòng 112–117)
- **Mức ưu tiên**: P3 (Trung bình)
- **Hiện trạng**:
  `var company = await _db.Companies.FirstOrDefaultAsync(item => item.Id == request.CompanyId);`
  Chỉ xác minh thực thể `company` có tồn tại trong CSDL hay không (`company is null`).
- **Đánh giá**: Hoàn toàn bảo toàn logic từ baseline. Nếu tương lai bảng Doanh nghiệp có bổ sung cờ trạng thái (Active/Inactive), cần đưa thêm điều kiện này vào validation nghiệp vụ.

### CR-05: Tách biệt side-effect Email sau khi Commit Transaction
- **Tập tin**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs` (Dòng 138–141)
- **Mức ưu tiên**: P4 (Thấp)
- **Hiện trạng**:
  Email thông báo kết quả tiếp nhận được gọi sau khi transaction đã commit thành công:
  ```csharp
  if (result.Status == PlacementRequestOperationStatus.Success && acceptedRequest is not null)
  {
      await _emailNotifier.NotifyResultAsync(acceptedRequest, passed: true, acceptedRequest.ResultNote);
  }
  ```
- **Đánh giá**: Đây là một cải tiến tích cực so với việc đặt lệnh gửi email trong transaction scope. Bên trong `NotifyResultAsync`, exception đã được bọc an toàn (`NotifySafelyAsync`), tránh gây crash luồng API chính.

---

## 5. Kết Luận Về Trạng Thái P1/P2
- **Số lỗi P1 phát hiện**: 0
- **Số lỗi P2 phát hiện**: 0
- **Tổng số phát hiện**: 5 (0 P1, 0 P2, 2 P3, 3 P4)
- **Quyết định chuyển bước**: Không có lỗi nghiêm trọng (P1/P2) vi phạm tính toàn vẹn hệ thống hoặc làm gián đoạn API contract. Do đó, không cần dừng quy trình để yêu cầu Tab 1 fix (không cần tạo `.agent/tab2-findings.flag`), chuyển thẳng sang **BƯỚC 3 – UAT 10 Kịch Bản Chạy Thật**.
