# Danh sách Vấn đề Đã biết và Hạn chế (Known Issues - UAT Round)

Tài liệu này ghi nhận công khai các vấn đề còn tồn đọng, các tính năng được hoãn lại (deferred) hoặc từ chối có chủ đích trong đợt phát hành UAT Sprint 2.

---

## 1. Các Phản hồi UAT Từ chối theo Thiết kế (Rejected by Design)

### 1.1 FB-05: Đề xuất sinh viên được phép nộp đơn không giới hạn số lượng cùng lúc
- **Hiện trạng**: Hệ thống giới hạn mỗi sinh viên chỉ được phép có tối đa 5 đơn ứng tuyển ở trạng thái mở (`Pending` hoặc `InterviewInvited`) cùng lúc.
- **Lý do từ chối**: Đây là quy định quản lý bắt buộc từ Ban Quản lý Đào tạo nhằm ngăn ngừa tình trạng sinh viên rải hồ sơ hàng loạt ("spam"), gây quá tải và làm giảm uy tín của nhà trường với các doanh nghiệp đối tác tiếp nhận thực tập sinh. Giới hạn 5 đơn là chủ đích thiết kế.

### 1.2 FB-06: Đòi bỏ bước bắt buộc đổi mật khẩu khi đăng nhập lần đầu
- **Hiện trạng**: Người dùng mới được cấp tài khoản mặc định bắt buộc phải chuyển sang trang `/change-password` trước khi được truy cập các tính năng khác.
- **Lý do từ chối**: Vi phạm chính sách an toàn thông tin bắt buộc. Mật khẩu ban đầu được tạo tự động và chuyển giao qua kênh hành chính; việc bắt buộc đổi mật khẩu giúp đảm bảo chỉ có chủ sở hữu hợp pháp mới nắm giữ quyền truy cập tài khoản.

---

## 2. Các Phản hồi UAT Tồn đọng Chuyển sang Giai đoạn Sau (Deferred)

### 2.1 FB-07: Gửi tin nhắn SMS về điện thoại khi có lịch phỏng vấn
- **Hiện trạng**: Hệ thống chỉ gửi thông báo qua giao diện web và qua kênh Email.
- **Kế hoạch**: Sẽ tạo phiếu theo dõi (backlog issue) cho phiên bản sau khi nhà trường phê duyệt ngân sách và hợp đồng tích hợp cổng tin nhắn SMS Brandname của nhà mạng.

### 2.2 FB-08: Góp ý giao diện "hơi đơn điệu"
- **Hiện trạng**: Màn hình quản trị tuân theo phong cách tối giản của PrimeVue Aura Design System.
- **Kế hoạch**: Cần thực hiện phỏng vấn sâu hoặc lấy bản vẽ thiết kế (mockup/Figma) chi tiết từ người dùng trước khi tiến hành điều chỉnh style.

### 2.3 FB-09: Tự động tra cứu thông tin doanh nghiệp từ Mã số thuế
- **Hiện trạng**: Người dùng nhập thông tin công ty thủ công.
- **Kế hoạch**: Đã được xếp vào kế hoạch nâng cấp Phase 3 do khối lượng tích hợp API bên thứ ba (Third-party Tax Service) và xử lý độ khả dụng mạng tương đối lớn.

---

## 3. Các Vấn đề Phát hiện trong Đánh giá Chéo Đã Được Khắc Phục (Resolved from Review)

> [!NOTE]
> Các vấn đề dưới đây đã được xử lý triệt để và **không còn nằm trong danh sách mở**:

### 3.1 CR-03: Cờ trạng thái `saving` ở hàm submit ứng tuyển chưa bọc `try...finally` toàn diện
- **Mức ưu tiên**: P3.
- **Tình trạng**: **ĐÃ SỬA DỨT ĐIỂM** (Closed).
- **Giải pháp**: Hàm `submit()` tại `JobPositionApplyView.vue` đã được bọc trọn vẹn trong `try...finally` ngay sau khi đặt `saving.value = true`. Đã bổ sung 4 unit test trong `JobPositionApplyView.test.js` kiểm chứng cờ `saving` luôn trở về `false` trong mọi tình huống (kể cả khi kiểm tra tính hợp lệ phía client ném lỗi).
- **Commit**: `6588fb3` (`fix(uat): CR-03 ensure saving flag resets via try-finally in submit`).

### 3.2 DOC-01: Tài liệu hướng dẫn tái lập thiếu bước `npm install` trên bản clone sạch
- **Mức ưu tiên**: P3.
- **Tình trạng**: **ĐÃ SỬA DỨT ĐIỂM** (Closed).
- **Giải pháp**: Đã bổ sung chỉ dẫn `npm install` bắt buộc tại Mục 2.3 trong `README-REPRODUCE-UAT.md`, đồng thời hoàn thiện các bước hướng dẫn chuẩn bị môi trường clone sạch.

---

## 4. Các Lưu ý Kỹ thuật Khác (Technical Observations)

### 4.1 Ca kiểm thử bỏ qua theo thiết kế (Design Skipped Test)
- **Tên test**: `StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters` trong `StudentInternshipMgmt.Infrastructure.Tests`.
- **Lý do**: Test yêu cầu collation SQL Server Tiếng Việt đặc thù (`Vietnamese_CI_AS`) để tìm kiếm LIKE không dấu / có dấu tiếng Việt chính xác. EF Core InMemory không hỗ trợ collation này nên được đánh dấu `[Fact(Skip = "...")]` từ trước.

### 4.2 Chuỗi dự phòng tại CompanyPlacementRequestDetailView.vue
- **Vị trí**: Dòng 174 file `CompanyPlacementRequestDetailView.vue`.
- **Mô tả**: Khi `coverLetter` của đơn ứng tuyển bị rỗng, template hiển thị chuỗi fallback `'tedjsfjdjklfjasdfslfajaisf'`. Mặc dù không ảnh hưởng đến luồng vận hành chính (do validation bắt buộc coverLetter có tối thiểu 20 ký tự), đây là một nợ kỹ thuật nhỏ cần được thay bằng chuỗi hiển thị thân thiện hơn (ví dụ: `"(Không có lời giới thiệu)"`) ở đợt dọn dẹp tiếp theo.
