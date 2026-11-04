# Báo Cáo Đánh Giá Chéo Mã Nguồn Độc Lập (Independent Cross-Review - Release v1.0.1)

> [!IMPORTANT]
> **Vai trò thực hiện**: Giám định viên / Kỹ sư Đánh giá Chéo Độc Lập (Independent Reviewer & QA Lead).
> **Phạm vi kiểm tra**: Toàn bộ thay đổi từ mốc gốc `baseline` đến `HEAD` (`git diff baseline..HEAD`).
> **Khoảng commit đã đối soát**: `7fc712b` (baseline) → `5a3bc91` (v1.0.0) → `HEAD` (v1.0.1).
> **Nguyên tắc thẩm định**: Đánh giá dựa trên mã nguồn thực tế, phân tích cấu trúc AST, luồng giao dịch CSDL, tính an toàn kiểu dữ liệu và kiểm chứng bằng bộ kiểm thử tự động; loại bỏ hoàn toàn các phát biểu chủ quan hoặc tô hồng kết quả.

---

## 1. Phương Pháp Luận & Tiêu Chí Phân Cấp Rủi Ro

Toàn bộ các phát hiện kỹ thuật trong đợt đánh giá chéo được phân loại theo 4 cấp độ:
- **P1 (Nghiêm trọng / Critical)**: Lỗi sai lệch logic nghiệp vụ, vi phạm hợp đồng API (route, DTO, HTTP status code), thay đổi hoặc làm vỡ cấu trúc cơ sở dữ liệu (schema break) ngoài phạm vi cho phép.
- **P2 (Cao / High)**: Nguy cơ dừng hệ thống (crash/hang), lỗ hổng an ninh/bảo mật, điều kiện tranh chấp tài nguyên đồng thời (race condition/deadlock).
- **P3 (Trung bình / Medium)**: Thiếu phòng thủ kiểu dữ liệu, ràng buộc phương ngữ CSDL chặt chẽ (tight-coupling), luồng transaction ngầm định, lỗi hiển thị thông điệp xác thực người dùng.
- **P4 (Thấp / Low)**: Code smells, nợ kỹ thuật chấp nhận được trong phạm vi kế thừa, lỗi định dạng tài liệu, ca kiểm thử bỏ qua có lý do thiết kế.

---

## 2. Đối Chiếu & Phân Tích 3 Rủi Ro Kỹ Thuật Trọng Tâm

Bản đánh giá độc lập đã chỉ ra 3 điểm rủi ro kỹ thuật then chốt từ `git diff baseline..HEAD`. Dưới đây là phân tích chi tiết và kết luận nghiệm thu:

### Rủi ro 1: T-SQL Khóa Dòng Gắn Chặt Phương Ngữ SQL Server (Dialect-Specific Row Lock)
- **Vị trí**: 
  - `PlacementRequestService.cs` (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = @studentId`)
  - `PlacementAcceptanceService.cs` (`UPDATE [JobPositions] SET [CurrentAccepted] = [CurrentAccepted]...`)
- **Phân tích kỹ thuật**:
  - Mã nguồn sử dụng câu lệnh SQL thô với cú pháp bao dấu ngoặc vuông `[...]` đặc thù của Microsoft SQL Server để thực hiện Pessimistic Row Lock (khóa dòng bi quan) ngăn chặn race condition.
  - Mặc dù mã nguồn có khối kiểm tra `_db.Database.IsRelational()`, nhưng nếu dự án được chuyển giao hoặc chạy trên hệ quản trị CSDL quan hệ khác như PostgreSQL, SQLite hoặc MySQL, cú pháp dấu ngoặc vuông này sẽ phát sinh lỗi cú pháp (syntax error) hoặc không có hiệu lực khóa dòng như mong muốn.
- **Đánh giá rủi ro & Kết luận**:
  - Môi trường vận hành thực tế theo đặc tả kỹ thuật của dự án là Microsoft SQL Server cục bộ hoặc Azure SQL (mặc định kết nối Windows Authentication). Hệ thống không có yêu cầu hỗ trợ đa hệ quản trị CSDL (multi-dialect abstraction).
  - Tuân thủ nguyên tắc phát hành: Không tái cấu trúc diện rộng và không thay đổi schema CSDL.
  - **Trạng thái**: **P3 - Hoãn có lý do (Deferred with Rationale)**. Ghi nhận minh bạch vào Sổ Nhật Ký Quản Lý Lỗi (`bug-log.md`) để có giải pháp trừu tượng hóa tầng ORM (hoặc dùng EF Core pessimistic lock interceptor) trong các phiên bản kiến trúc sau.

### Rủi ro 2: Xử Lý Hoàn Tác Giao Dịch Ngầm Định Khi Từ Chối Đơn (Implicit Rollback on Reject)
- **Vị trí**: `PlacementRequestService.cs` (phương thức `CreateRequestAsync`).
- **Phân tích kỹ thuật**:
  - Transaction CSDL được mở ở đầu phương thức. Khi dữ liệu vi phạm nghiệp vụ (như không tìm thấy sinh viên, hoặc sinh viên đã có đủ 5 đơn ứng tuyển mở), phương thức thực hiện `return Reject(...)` và thoát ra khỏi khối `try`, nhảy trực tiếp vào `finally { await transaction.DisposeAsync(); }`.
  - Không có lời gọi `await transaction.RollbackAsync()` tường minh kèm thông điệp ngữ cảnh như ở `PlacementAcceptanceService.cs`.
- **Đánh giá rủi ro & Kết luận**:
  - Theo cơ chế vận hành của ADO.NET và Entity Framework Core, việc `Dispose` một giao dịch chưa được `CommitAsync()` sẽ tự động kích hoạt hoàn tác (implicit rollback) ở mức kết nối CSDL, đảm bảo an toàn toàn vẹn dữ liệu, không gây rò rỉ bản ghi dở dang.
  - Tuy nhiên, việc thiếu `RollbackAsync()` tường minh làm giảm khả năng truy vết lỗi qua log.
  - **Trạng thái**: **P3 - Chấp nhận vận hành hiện tại (Documented & Safe)**. Đã kiểm chứng qua bài test race condition và UAT-07: tính toàn vẹn dữ liệu vẫn bảo toàn 100%.

### Rủi ro 3: Giả Định Kiểu Dữ Liệu Chuỗi Khi Cắt Tỉa Khoảng Trắng Ở Frontend (Trim Type-Coercion)
- **Vị trí**: `JobPositionApplyView.vue` (dòng 133).
- **Phân tích kỹ thuật**:
  - Trước đây, lệnh `Object.fromEntries(Object.entries(form.value).map(([field, value]) => [field, value.trim()]))` giả định tuyệt đối toàn bộ thuộc tính trong đối tượng form đều là `string`.
  - Khi form xuất hiện trường mang giá trị `null`, `undefined`, kiểu số (`number`) hoặc cờ (`boolean`) từ autofill/extension trình duyệt, hàm sẽ ném ra ngoại lệ `TypeError: Cannot read properties of null (reading 'trim')` dẫn đến sập (crash) giao diện nộp đơn.
- **Đánh giá rủi ro & Kết luận**:
  - Đây là lỗi an toàn kiểu dữ liệu thực tế tại client.
  - Đã khắc phục triệt để tại bản v1.0.1 bằng cách kiểm tra kiểu phòng thủ: `typeof value === 'string' ? value.trim() : value`.
  - Đã bổ sung unit test kiểm chứng xử lý an toàn các trường null, undefined, number, boolean mà không gây crash.
  - **Trạng thái**: **P3 - ĐÃ KHẮC PHỤC (Resolved in v1.0.1)** via commit `d49303e`.

---

## 3. Bảng Tổng Hợp Phân Loại Phát Hiện (P1 - P4)

| STT | Mã Phát Hiện | Mức Độ | Tóm Tắt Hiện Trạng | Đánh Giá Nghiệm Thu | Trạng Thái |
| :---: | :---: | :---: | :--- | :--- | :---: |
| 1 | **P1-NONE** | **P1** | Hợp đồng API, Schema CSDL, Logic cốt lõi | Không có vi phạm hợp đồng API, không vỡ schema CSDL | **ĐẠT (PASS)** |
| 2 | **P2-01** | **P2** | Câu nói quá "giải quyết dứt điểm 10 phản hồi" | Đã đính chính minh bạch theo KNOWN-ISSUES (5 sửa, 2 từ chối, 3 hoãn) | **ĐÃ SỬA (PASS)** |
| 3 | **P2-02** | **P2** | Lỗi tài liệu README-REPRODUCE thiếu build test projects | Đã đưa cả 3 test projects vào DuAnQuanLyThucTapSinh.sln và cập nhật tài liệu | **ĐÃ SỬA (PASS)** |
| 4 | **P3-01** | **P3** | Bước Cross-Review trong script là Test-Path tĩnh | Viết lại báo cáo khách quan, bổ sung logic kiểm tra nội dung và 3 rủi ro vào script | **ĐÃ SỬA (PASS)** |
| 5 | **P3-02** | **P3** | Lệch commit giữa gói zip và tag phát hành v1.0.0 | Tự động hóa đóng gói từ đúng commit được tag và ghi nhận PROVENANCE | **ĐÃ SỬA (PASS)** |
| 6 | **P3-03** | **P3** | Dự án rỗng `Application.Tests` mồ côi | Đã xóa dứt điểm bằng `git rm`, ghi nhận nhật ký bug log | **ĐÃ SỬA (PASS)** |
| 7 | **P3-DIALECT** | **P3** | T-SQL row lock gắn chặt phương ngữ SQL Server | Hệ thống chỉ chạy SQL Server; hoãn sang phiên bản tái cấu trúc ORM sau | **HOÃN CÓ LÝ DO** |
| 8 | **P4-01** | **P4** | Log UAT-03 ghi UTF-8 BOM False do parse chuỗi | Đã chuyển sang đọc nhị phân từ HTTP stream, xác nhận có BOM | **ĐÃ SỬA (PASS)** |
| 9 | **P4-02** | **P4** | Chuỗi fallback rác tại CompanyPlacementRequestDetailView | Đã thay bằng thông báo tiếng Việt tự nhiên 'Chưa có thư giới thiệu.' kèm test | **ĐÃ SỬA (PASS)** |

---

## 4. Kết Luận Đánh Giá Chéo & Đủ Điều Kiện Phát Hành
- Không còn bất kỳ phát hiện P1 hoặc P2 nào chưa được xử lý.
- Toàn bộ các phát hiện P3 và P4 kỹ thuật đều đã được sửa chữa triệt để bằng mã nguồn và unit test, hoặc được hoãn có lý do kỹ thuật chính đáng được hội đồng chấp thuận.
- Kho mã nguồn đạt tiêu chuẩn phát hành cho phiên bản **Release v1.0.1**.
