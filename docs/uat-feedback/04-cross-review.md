# Báo Cáo Đánh Giá Mã Nguồn Chéo Độc Lập (Cross-Review Report)

- **Người thực hiện**: Reviewer & QA Độc lập (TAB 2)
- **Mốc so sánh**: `uat-baseline` (`3dc0ecb`) so với `HEAD` (`feature/uat-feedback`)
- **Ngày thực hiện**: 03/11/2026
- **Tập tin bằng chứng thô đính kèm**:
  - [`evidence/review/git-diff.txt`](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/review/git-diff.txt) (Toàn bộ diff chi tiết)
  - [`evidence/review/git-diff-stat.txt`](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/review/git-diff-stat.txt) (Thống kê tệp thay đổi)
  - [`evidence/review/test-backend-independent.txt`](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/review/test-backend-independent.txt) (Kết quả chạy kiểm thử backend độc lập)
  - [`evidence/review/test-frontend-independent.txt`](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/review/test-frontend-independent.txt) (Kết quả chạy kiểm thử và build frontend độc lập)

---

## 1. Xác Nhận Phạm Vi Thay Đổi (Scope Integrity)

Đối chiếu `git diff --stat uat-baseline..HEAD` với [01-feedback-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/01-feedback-log.md):

| Nhóm phản hồi | Tệp tin thay đổi | Đánh giá phạm vi |
| :--- | :--- | :---: |
| **FB-01** (Chuẩn hóa thông báo lỗi sinh viên) | `StudentValidators.cs`, `StudentService.cs`, `StudentValidatorTests.cs`, `StudentSoftDeleteFilterTests.cs` | **Đúng phạm vi** (Chỉ thay đổi chuỗi thông báo sang tiếng Việt và cập nhật test tương ứng) |
| **FB-02** (Xóa định danh kỹ thuật Title/Quantity) | `JobPositionService.cs`, `companyJobPositionValidation.js`, `companyJobPositionValidation.test.js` | **Đúng phạm vi** (Bỏ `(Title)` và `(Quantity)` ở cả 2 đầu) |
| **FB-03** (Trim khoảng trắng CV URL) | `CreatePlacementRequestDtoValidator.cs`, `CreatePlacementRequestDtoValidatorTests.cs`, `applicationValidation.js`, `applicationValidation.test.js` | **Đúng phạm vi** (Cắt tỉa khoảng trắng đầu/cuối, giữ nguyên quy tắc chặn khoảng trắng giữa và scheme nguy hiểm) |
| **FB-04** (Sửa font tiếng Việt README) | `InternshipManagement/README.md` | **Đúng phạm vi** (Sửa lỗi mojibake dòng cuối) |
| **FB-10** (Khống chế race condition nộp đơn đúp) | `PlacementRequestService.cs`, `JobPositionApplyView.vue`, `PlacementRequestServiceTests.cs` | **Đúng phạm vi** (Bổ sung row-lock SQL Server, transaction bảo vệ và disable button) |

> [!NOTE]
> Toàn bộ các thay đổi trong mã nguồn đều thuộc 5 phản hồi đã chọn. Không có sửa đổi lan man, không có commit rác hay can thiệp vào các module khác ngoài phạm vi.

---

## 2. Kết Quả Chạy Kiểm Thử Độc Lập của TAB 2

TAB 2 tự biên dịch và thực thi toàn bộ test suites độc lập trên môi trường máy, không phụ thuộc vào báo cáo của TAB 1:

1. **Backend Tests (.NET 8)**:
   - Lệnh: `dotnet test InternshipManagement/backend/InternshipManagement.sln -m:1`
   - Kết quả: **402 Passed, 1 Skipped, 0 Failed** (khớp 100% với số lượng TAB 1 báo cáo).
   - Test bỏ qua: `StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters` (được thiết kế bỏ qua khi chạy unit test vì cần SQL Server Collation thật).
2. **Frontend Tests (Vitest)**:
   - Lệnh: `npm test` trong thư mục `InternshipManagement/frontend`
   - Kết quả: **16 test files passed, 190 passed, 0 failed** (khớp 100% với số lượng TAB 1 báo cáo).
3. **Frontend Production Build**:
   - Lệnh: `npm run build`
   - Kết quả: Biên dịch Vite hoàn tất thành công trong 706ms - 967ms, 0 lỗi, 0 cảnh báo nghiêm trọng.

---

## 3. Danh Sách Phát Hiện Đánh Giá Chéo (Cross-Review Findings)

### CR-01: Unit test bắt trùng lặp đơn đúp chỉ kiểm tra tuần tự trên InMemory Database
- **Vị trí**: [`PlacementRequestServiceTests.cs:302-317`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/PlacementRequestServiceTests.cs#L302-L317)
- **Mức ưu tiên**: **P4 (Thấp)**
- **Phân loại**: *Test Scope & Realism*
- **Mô tả**:
  Unit test mới `CreateRequestAsync_double_submission_creates_only_one_request_and_rejects_second` thực thi tuần tự 2 lần gọi `await service.CreateRequestAsync(...)` trên CSDL EF Core InMemory. Do InMemory DB không phải CSDL quan hệ (`_db.Database.IsRelational() == false`), khối khóa dòng SQL `UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}` và `BeginTransactionAsync()` không thực sự được chạy trong test này. Test này chỉ chứng minh logic kiểm tra `hasDuplicateOpenRequest` hoạt động khi gọi tuần tự.
- **Bằng chứng**:
  Dòng 100-112 trong `PlacementRequestService.cs` có kiểm tra `if (isRelational)` trước khi gọi transaction và raw SQL update.
- **Đề xuất khắc phục**:
  Chấp nhận unit test này cho môi trường InMemory của CI/CD; bắt buộc phải có ca kiểm thử UAT/E2E chạy đua song song (concurrency race condition) trên SQL Server thật để kiểm chứng row-lock (đã được TAB 2 đưa vào kịch bản UAT2-05).
- **Độ chắc chắn**: **Chắc chắn**.

---

### CR-02: Kiểm tra trùng mã sinh viên dùng `.ToLower()` hai vế trên CSDL Case-Insensitive
- **Vị trí**: [`StudentService.cs:144-147`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/StudentService.cs#L144-L147)
- **Mức ưu tiên**: **P4 (Thấp)**
- **Phân loại**: *Performance / Code Convention*
- **Mô tả**:
  Khi kiểm tra trùng lặp `StudentCode`, hàm sử dụng `s.StudentCode.ToLower() == normalizedCode.ToLower()`. Trong SQL Server với Collation mặc định không phân biệt hoa thường (`SQL_Latin1_General_CP1_CI_AS`), việc gọi hàm `LOWER()` trên cột CSDL có thể làm suy giảm hiệu năng Index Seek nếu số lượng bản ghi lớn.
- **Bằng chứng**:
  Mã nguồn tại dòng 144 `StudentService.cs`.
- **Đề xuất khắc phục**:
  Giữ nguyên vì hiện tại số lượng sinh viên vừa phải và cách này bảo đảm an toàn trên mọi hệ quản trị CSDL; ghi nhận backlog tối ưu hiệu năng sau này.
- **Độ chắc chắn**: **Chắc chắn**.

---

### CR-03: Cờ trạng thái `saving` ở hàm submit nộp đơn cần bao bọc bảo vệ toàn diện
- **Vị trí**: [`JobPositionApplyView.vue:122-135`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/views/job-positions/JobPositionApplyView.vue#L122-L135)
- **Mức ưu tiên**: **P3 (Trung bình)**
- **Phân loại**: *Frontend Exception Handling*
- **Mô tả**:
  Hàm `submit()` gán `saving.value = true` ngay từ đầu để vô hiệu hóa nút bấm và reset `saving.value = false` nếu phát hiện lỗi validation client. Tuy nhiên, khối `try ... finally` chỉ bao bọc đoạn gọi API bên dưới. Nếu trong tương lai có thêm logic hoặc ngoại lệ phát sinh trong quá trình validate/focus trước khối `try`, nút bấm có thể bị kẹt ở trạng thái loading.
- **Bằng chứng**:
  Dòng 122 đặt `saving.value = true`, sau đó dòng 126 kiểm tra `if (invalidFieldCount.value > 0) { saving.value = false; ... }` rồi mới vào `try { ... } finally { saving.value = false; }`.
- **Đề xuất khắc phục**:
  Chuyển toàn bộ phần thân hàm `submit()` vào trong `try ... finally` để luôn bảo đảm `saving.value` được giải phóng kể cả khi có ngoại lệ ngoài ý muốn.
- **Độ chắc chắn**: **Chắc chắn**.

---

### CR-04: Sửa triệt để lỗi mã hóa UTF-8 trong tài liệu hướng dẫn
- **Vị trí**: [`InternshipManagement/README.md:9`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/README.md#L9)
- **Mức ưu tiên**: **P4 (Thấp)**
- **Phân loại**: *Internationalization & Documentation*
- **Mô tả**:
  Dòng cuối cùng của file README đã được chuyển sang UTF-8 chuẩn xác: *"Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3)."*. Đã xác minh byte sequence không còn bất kỳ ký tự mojibake nào.
- **Bằng chứng**:
  Đọc file nhị phân và hiển thị UTF-8 không BOM.
- **Đề xuất khắc phục**:
  Đạt yêu cầu; chấp thuận đóng lỗi FB-04.
- **Độ chắc chắn**: **Chắc chắn**.

---

### CR-05: Đảm bảo cắt tỉa khoảng trắng đồng bộ giữa Frontend, Validator và Service
- **Vị trí**: [`CreatePlacementRequestDtoValidator.cs:88-95`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Application/Features/PlacementRequests/Dtos/CreatePlacementRequestDtoValidator.cs#L88-L95), [`applicationValidation.js:36-44`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/applicationValidation.js#L36-L44), [`PlacementRequestService.cs:189`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementRequestService.cs#L189)
- **Mức ưu tiên**: **P4 (Thấp)**
- **Phân loại**: *Data Integrity & Security*
- **Mô tả**:
  Cơ chế xử lý liên kết CV (FB-03) đã được thực hiện phòng vệ theo chiều sâu (defense-in-depth):
  1. Frontend validate cho phép dán URL có khoảng trắng ở 2 đầu nhưng chặn URL chứa khoảng trắng ở giữa hoặc scheme không phải http/https.
  2. Backend FluentValidation cắt tỉa `value.Trim()` trước khi kiểm tra scheme và độ dài tối đa 500 ký tự.
  3. Service lưu vào CSDL gọi `CleanString(dto.CvUrl)` bảo đảm chuỗi lưu trong CSDL là chuỗi sạch đã được trim.
- **Bằng chứng**:
  Kiểm tra luồng xử lý qua mã nguồn 3 tầng Application, Infrastructure và Frontend.
- **Đề xuất khắc phục**:
  Thiết kế rất chặt chẽ, đạt tiêu chuẩn an toàn dữ liệu.
- **Độ chắc chắn**: **Chắc chắn**.

---

## 4. Tổng Kết Đánh Giá Chéo
- **Tổng số phát hiện**: 5 (0 lỗi P1, 0 lỗi P2, 1 lưu ý P3, 4 lưu ý P4).
- **Đánh giá giải quyết phản hồi**:
  - FB-01, FB-02, FB-03, FB-04: Đã xử lý tận gốc vấn đề, không che triệu chứng.
  - FB-10: Đã giải quyết bằng kiến trúc Transaction + Row Lock vững chắc trên SQL Server, đi kèm chặn bấm nhanh ở Frontend.
- **Kết luận**: Mã nguồn của TAB 1 đạt chất lượng tốt, không vi phạm an toàn, sẵn sàng cho việc chạy kiểm thử UAT 10 kịch bản thực tế.
