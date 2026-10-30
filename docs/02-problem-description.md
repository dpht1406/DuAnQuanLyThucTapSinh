# Mô Tả Chi Tiết Vấn Đề Mã Nguồn (Before Refactor)

Module được phân tích:
**File**: `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs`  
**Interface**: `StudentInternshipMgmt.Application.Features.PlacementRequests.IPlacementAcceptanceService`  
**Tổng số dòng hiện tại**: 479 dòng.

Dưới đây là 7 vấn đề cụ thể được nhận diện trên mã nguồn trước khi refactor:

---

### Vấn đề 1: Phương thức quá dài vi phạm Single Responsibility Principle (SRP)
- **Vị trí**: 
  - `AcceptAsync`: Dòng 52 – 279 (228 dòng)
  - `CancelAcceptedAsync`: Dòng 281 – 466 (186 dòng)
- **Lý do khó đọc / rủi ro**:
  - Cả hai phương thức gánh vác quá nhiều trách nhiệm cùng một lúc: quản lý vòng đời transaction, chạy câu lệnh khóa bản ghi raw SQL, kiểm tra phân quyền tác nhân, kiểm tra điều kiện nghiệp vụ sinh viên/doanh nghiệp/vị trí tuyển dụng, cập nhật entity, từ chối đơn phụ của sinh viên, tạo lịch sử trạng thái, tạo thông báo hệ thống, gửi email thông báo và xử lý ngoại lệ database.
  - Người đọc phải theo dõi một luồng xử lý thẳng đuột dài hơn 200 dòng, rất khó nắm bắt tổng thể nghiệp vụ cấp cao và dễ bỏ sót tác động biên khi bảo trì.

---

### Vấn đề 2: Độ lồng sâu quá mức (Max Nesting Depth = 5)
- **Vị trí**:
  - Dòng 61 – 278 (`AcceptAsync`): Lồng ghép khối `try` -> `if (request.JobPositionId.HasValue)` -> `if (position is null...)` và trong khối `catch` lồng `if (transaction is not null)` -> `try` -> `catch`.
  - Dòng 285 – 465 (`CancelAcceptedAsync`): Lồng ghép khối `try` -> `if (request.JobPositionId.HasValue)` -> `if (position is not null...)` -> `if (remainingAcceptedCount < position.Quantity)`.
- **Lý do khó đọc / rủi ro**:
  - Không sử dụng guard clauses triệt để, các khối điều kiện lồng sâu làm tăng độ phức tạp chu trình (cyclomatic complexity), buộc lập trình viên phải ghi nhớ quá nhiều ngữ cảnh phân nhánh trước khi tiếp cận đoạn mã cốt lõi.

---

### Vấn đề 3: Trùng lặp mã nguồn nghiêm trọng (Code Duplication)
- **Vị trí**:
  1. **Xử lý ngoại lệ và Rollback Transaction** (Dòng 248 – 273 và Dòng 435 – 460): 25 dòng code bắt lỗi `DbUpdateException or DbException`, ghi log lỗi, kiểm tra transaction khác null, khối try-catch phụ để gọi `transaction.RollbackAsync()` kèm log lỗi rollback, và trả về `PlacementRequestOperationResult(PlacementRequestOperationStatus.Conflict, ...)` bị sao chép gần như 100% giữa hai phương thức.
  2. **Thực thi câu lệnh khóa dữ liệu quan hệ** (Dòng 86 – 93 và Dòng 305 – 312): Gọi lặp lại `_db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Students]...")` và `UPDATE [JobPositions]...`.
  3. **Kiểm tra đơn tồn tại và thuộc phạm vi doanh nghiệp** (Dòng 72 – 79, Dòng 100 – 107, Dòng 297 – 303, Dòng 319 – 325): Kiểm tra null và `scopeCompanyId` kèm trả về `RollbackWithResultAsync` lặp lại 4 lần cùng một thông báo lỗi.
  4. **Kiểm tra trạng thái sinh viên đã tiếp nhận** (Dòng 155 – 156 so với Dòng 413 – 415): Dòng 156 dùng `AcceptedStatuses.Contains(item.Status)`, nhưng dòng 413 – 415 lại viết thủ công 3 điều kiện OR rời rạc: `item.Status == StudentStatus.Accepted || item.Status == StudentStatus.Interning || item.Status == StudentStatus.Completed`.
- **Lý do khó đọc / rủi ro**:
  - Khi cần sửa đổi logic xử lý ngoại lệ hoặc điều chỉnh câu lệnh khóa DB, lập trình viên buộc phải nhớ sửa ở cả hai nơi, dễ dẫn đến hiện tượng thiếu đồng bộ và phát sinh lỗi tiềm ẩn.

---

### Vấn đề 4: Chuỗi ma thuật (Magic Strings) và sử dụng không nhất quán
- **Vị trí**:
  - Dòng 459: `return new PlacementRequestOperationResult(PlacementRequestOperationStatus.Conflict, "Không thể hoàn tất, vui lòng thử lại.");` — Sử dụng chuỗi cứng thay vì dùng hằng số `RetryConflict` đã được khai báo ở dòng 18.
  - Dòng 78, 106, 302, 324: Chuỗi `"Không tìm thấy đơn ứng tuyển."` xuất hiện cứng 4 lần ở các vị trí khác nhau mà không có hằng số đại diện.
  - Dòng 374: Chuỗi `"Admin hủy kết quả tiếp nhận sinh viên."` là chuỗi cứng trong logic nghiệp vụ.
  - Dòng 388: Chuỗi `"admin-cancel-accepted"` ghi nhận nguồn xác nhận (`ConfirmationSource`) dạng chuỗi ma thuật.
  - Dòng 231: Chuỗi `"Doanh nghiệp nhận sinh viên"` làm tiêu đề thông báo.
  - Dòng 191 – 195: Các chuỗi ghi chú lịch sử trạng thái được nối bằng toán tử 3 ngôi lồng nhau (`Doanh nghiệp xác nhận Đạt...`, `Admin xác nhận Đạt thay doanh nghiệp...`).
- **Lý do khó đọc / rủi ro**:
  - Khó kiểm soát việc đồng bộ thông báo người dùng, dễ gõ sai chính tả và gây khó khăn khi cần quốc tế hóa (i18n) hoặc tái sử dụng.

---

### Vấn đề 5: Tham số không sử dụng (Dead / Unused Parameter)
- **Vị trí**: Dòng 477 – 478:
  ```csharp
  private static string? CleanString(string? value, bool preserveLineBreaks = false) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  ```
- **Lý do khó đọc / rủi ro**:
  - Tham số `preserveLineBreaks` được khai báo với giá trị mặc định `false`, và ở dòng 375 được gọi với `CleanString(note, preserveLineBreaks: true)!`, nhưng thân hàm hoàn toàn không sử dụng tham số này mà chỉ gọi `value.Trim()`.
  - Điều này gây hiểu lầm cho người đọc mã nguồn rằng hàm có xử lý riêng cho ngắt dòng, trong khi thực tế hành vi hoàn toàn giống nhau.

---

### Vấn đề 6: Số ma thuật (Magic Number) trong kiểm tra hợp lệ
- **Vị trí**: Dòng 365:
  ```csharp
  if (note is not null && note.Trim().Length > 1000)
  ```
- **Lý do khó đọc / rủi ro**:
  - Con số `1000` được nhúng trực tiếp trong mã logic điều kiện thay vì được đặt tên bằng một hằng số có ý nghĩa (ví dụ `MaxCancellationNoteLength = 1000`). Nếu giới hạn độ dài của cột cơ sở dữ liệu thay đổi, con số này không có mối liên kết rõ ràng.

---

### Vấn đề 7: Trộn lẫn các mức độ trừu tượng (Mixed Levels of Abstraction)
- **Vị trí**: Toàn bộ class `PlacementAcceptanceService.cs`.
- **Lý do khó đọc / rủi ro**:
  - Phương thức nghiệp vụ cấp cao (`AcceptAsync`) trực tiếp gọi mã SQL thô tầng thấp (`UPDATE [PlacementRequests] SET [Status] = [Status] WHERE [Id] = ...`), xen kẽ với điều kiện miền nghiệp vụ (Domain rules), rồi tiếp tục xen kẽ với việc định dạng nội dung thông báo và cấu hình email.
  - Thiếu sự phân tách giữa:
    1. Lớp điều phối giao dịch & khóa bản ghi (Concurrency / Transaction Orchestration)
    2. Lớp kiểm định điều kiện nghiệp vụ (Business Validation & Policy)
    3. Lớp chuyển đổi trạng thái và tác dụng phụ (Domain State Transition & Side Effects)
