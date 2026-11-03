# Ghi chú Tự Đánh giá Kỹ thuật (Self-Assessment Notes - Builder)

**Người thực hiện**: TAB 1 (Builder / Senior Full-Stack Engineer)  
**Dự án**: Hệ thống Quản lý Sinh viên Thực tập (.NET 8 + Vue 3)  
**Giai đoạn**: Xử lý phản hồi UAT Đợt 2 (UAT Feedback Sprint)

---

## 1. Tổng quan Công việc Đã hoàn thành

1. **Thu thập phản hồi UAT thực nghiệm (FB-01 đến FB-10)**:
   - Thay vì bịa số liệu ảo, toàn bộ các phản hồi được xây dựng từ việc chạy ứng dụng thực tế trên cơ sở dữ liệu tách biệt `StudentInternshipMgmtDb_UATBuild`.
   - Bao quát 5 vai trò (personas) với phong cách phát biểu đời thường đa dạng.
   - Phân loại rõ ràng theo thang độ ưu tiên P1 -> P4 và đưa ra quyết định xử lý / từ chối có căn cứ kỹ thuật và nghiệp vụ xác đáng.
2. **Hiện thực hóa 5 bản vá lỗi hoàn chỉnh**:
   - Vượt chỉ tiêu tối thiểu 3 phản hồi (đã sửa 5 phản hồi: FB-04 [P4], FB-01 [P3], FB-02 [P3], FB-03 [P3], FB-10 [P2]).
   - Mỗi phản hồi được đóng gói trong đúng 1 commit git riêng biệt, tuân thủ conventional commit (`fix(uat): ...`).
   - Mọi thay đổi đều được kiểm chứng bằng unit test và integration test tương ứng.
3. **Bằng chứng Before / After thực tế**:
   - Thu thập đầy đủ log thực tế trước và sau khi sửa trên cùng một môi trường máy chủ và cơ sở dữ liệu.
   - Lưu trữ tự động trong `docs/uat-feedback/evidence/before-after/` bằng các script PowerShell có thể chạy lại bất kỳ lúc nào (`capture-before-evidence.ps1` và `capture-after-evidence.ps1`).
4. **Bảo toàn chất lượng toàn hệ thống (Green Suite)**:
   - Backend: Tăng từ 398 test (397 pass, 1 skip) lên **402 test (402 pass, 1 skip theo thiết kế, 0 fail)**.
   - Frontend: Tăng từ 186 test lên **190 test (190 pass, 0 fail)**.
   - Build frontend và backend đều thành công không có bất kỳ cảnh báo mới nào.

---

## 2. Các Quyết định Thiết kế và Đánh đổi Kỹ thuật (Tradeoffs)

### 2.1 Cơ chế chống Race Condition trong Nộp đơn (FB-10)
- **Vấn đề**: Người dùng click đúp hoặc 2 request gửi đồng thời có thể vượt qua bước kiểm tra `_db.PlacementRequests.AnyAsync(...)` do cả hai đều đọc DB trước khi bên kia kịp ghi.
- **Giải pháp lựa chọn**:
  - Tận dụng kỹ thuật Database Transaction kết hợp **Row Lock (Pessimistic Concurrency)** trên bảng `Students` (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}`).
  - Kỹ thuật này đã được kiểm chứng thành công ở dịch vụ `PlacementAcceptanceService.cs` trước đó. Khi request đầu tiên cập nhật dòng trạng thái của sinh viên trong transaction, SQL Server cấp phát khóa độc quyền (Exclusive Lock) trên hàng đó; request thứ hai sẽ phải chờ (wait) cho đến khi request đầu tiên commit xong. Ngay sau khi unblock, request thứ hai kiểm tra `hasDuplicateOpenRequest` và phát hiện ngay đơn đang mở, trả về lỗi 400.
- **Tính tương thích InMemory**:
  - Trong môi trường kiểm thử tự động, EF Core InMemory ném ngoại lệ `TransactionIgnoredWarning` nếu gọi `BeginTransactionAsync()`.
  - Chúng tôi giải quyết bằng cách kiểm tra `_db.Database.IsRelational()`: chỉ mở transaction và chạy câu lệnh khóa hàng khi kết nối CSDL quan hệ thực thụ (SQL Server), đảm bảo test suite chạy siêu nhanh với InMemory mà không vấp phải lỗi cảnh báo.
- **Lớp bảo vệ phía Client**:
  - Tại file `JobPositionApplyView.vue`, biến cờ `saving.value = true` được kích hoạt ngay tại dòng lệnh đầu tiên của hàm `submit()`, giúp nút bấm bị vô hiệu hóa (disabled) ngay ở khung hình đầu tiên của sự kiện click chuột.

### 2.2 Xử lý Trim liên kết CV (FB-03)
- **Vấn đề**: Người dùng thường copy link trên điện thoại dính khoảng trắng đầu/cuối.
- **Đánh đổi giữa Tiện ích và An toàn**:
  - Chúng tôi chỉ cho phép cắt (trim) khoảng trắng ở 2 đầu chuỗi.
  - Tuyệt đối **không** loại bỏ khoảng trắng ở giữa URL (vì URL chứa khoảng trắng ở giữa là URL hỏng hoặc có thể tiềm ẩn tấn công injection).
  - Vẫn chặn cứng các scheme nguy hiểm như `javascript:` hoặc URL tương đối (`/cv/...`) nhằm ngăn ngừa lỗ hổng XSS hoặc can thiệp đường dẫn cục bộ.

### 2.3 Chuẩn hóa thông báo lỗi tiếng Việt (FB-01, FB-02)
- **Vấn đề**: Mã định danh biến tiếng Anh (`FullName`, `PhoneNumber`, `(Title)`, `(Quantity)`) xuất hiện trong thông báo lỗi.
- **Giải pháp**:
  - Rà soát toàn bộ repo trước khi sửa để tránh làm gãy các assertion trong test suite cũ.
  - Tìm ra 2 dòng kiểm thử trong `StudentSoftDeleteFilterTests.cs` (dòng 87 và 174) kiểm tra `.Contain("PhoneNumber")`, đã được cập nhật thành `.Contain("Số điện thoại")`.
  - Phía frontend hiển thị tự nhiên, chuyên nghiệp và đồng bộ.

---

## 3. Các Giới hạn Hiện tại và Hướng Phát triển Tương lai

1. **Phụ thuộc vào Row Lock trên bảng Students**:
   - Cách khóa hàng `UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}` rất hiệu quả khi một sinh viên nộp đơn. Tuy nhiên, nếu sau này mở rộng hệ thống sang phân tán cao (High Throughput / Microservices), nên cân nhắc sử dụng Distributed Lock (Redis Redlock) hoặc Unique Composite Index có điều kiện trên bảng `PlacementRequests` (`(StudentId, CompanyId, JobPositionId) WHERE Status IN (Pending, InterviewInvited)`).
2. **Khảo sát người dùng thật**:
   - Hiện tại dữ liệu khảo sát là dữ liệu mô phỏng dựa trên quan sát thực nghiệm. Nhóm đã chuẩn bị sẵn [feedback-form.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/feedback-form.md) và [uat-scenarios-sheet.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/uat-scenarios-sheet.md) để chuyển giao cho các đợt UAT với sinh viên và nhà tuyển dụng thật trong thực tế.
3. **Các tính năng để lại sau**:
   - Tích hợp SMS thông báo (FB-07) và tích hợp API Tổng cục Thuế (FB-09) là các tính năng giá trị nhưng cần ngân sách và thời gian, được khuyến nghị đưa vào lộ trình phát triển phiên bản tiếp theo.
