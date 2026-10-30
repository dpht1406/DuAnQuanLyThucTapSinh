# Nhật Ký Quản Lý Lỗi & Phát Hiện (Unified Bug Log)

Bảng quản lý tập trung toàn bộ các vấn đề phát hiện từ:
1. Các vấn đề tồn tại trước refactor (Known Issues - Tab 1)
2. Kết quả đánh giá chéo mã nguồn (Cross-Review - Tab 2)
3. Kết quả kiểm thử chấp nhận người dùng thực tế (UAT 10 kịch bản - Tab 2)

---

## Bảng Quản Lý Lỗi Tổng Hợp

| Mã | Mô Tả | Mức Ưu Tiên | Nguồn Phát Hiện | Người Xử Lý | Trạng Thái | Commit Sửa |
|:---:|---|:---:|:---:|:---:|:---:|:---:|
| **BUG-PRE-01** / **CR-01** | Tham số `preserveLineBreaks` trong hàm `CleanString` không được sử dụng thực tế, hàm luôn gọi `value.Trim()` | **P4** | Known Issue / Cross-Review | Tab 1 & Tab 2 | **Chấp nhận giữ nguyên** (Bảo toàn chữ ký và hành vi baseline; discard chỉ xử lý cảnh báo, tham số vẫn không đổi hành vi) | |
| **BUG-PRE-02** | Chuỗi lỗi trong `CancelAcceptedAsync` từng được viết cứng thay vì dùng hằng số | **P4** | Known Issue (Tab 1) | Tab 1 | **Đã sửa** (Chuyển thông báo lỗi sang hằng số `private const string`) | `898644e` |
| **BUG-PRE-03** | Constructor mặc định của `PlacementAcceptanceService` dùng `NoOpEmailSender` khi không truyền notifier | **P4** | Known Issue (Tab 1) | Tab 1 | **Chấp nhận giữ nguyên** (Cách ly môi trường dev/test khỏi dịch vụ SMTP ngoài) | |
| **CR-02** | `CancelAcceptedAsync` đọc summary để khóa dòng rồi đọc entity lần nữa (`GetRequestSummaryAsync` và `GetPlacementRequestByIdAsync`) | **P3** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Không sửa** (Giữ thứ tự khóa dòng trước khi ChangeTracker theo dõi entity) | |
| **CR-03** | Template log lỗi chính của Cancel thay đổi từ `Placement acceptance cancellation failed...` thành `Placement acceptance cancellation transaction failed...`; log Accept giữ nguyên câu baseline | **P4** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Chấp nhận khác biệt có chủ đích**; không thay đổi API/result contract | |
| **CR-04** | `AcceptAsync` kiểm tra công ty tồn tại nhưng không kiểm tra trạng thái hoạt động / khóa của công ty | **P3** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Còn mở** (Đề xuất backlog nếu domain bổ sung trạng thái công ty) | |
| **CR-05** | Email gửi sau transaction commit; tiến trình dừng sau commit có thể khiến email chưa được gửi | **P4** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Còn mở** (Outbox chưa được triển khai) | |
| **UAT-01..10** | Kết quả chạy lại E2E trên backend và SQL Server thật | N/A | UAT Suite (Tab 2) | QA Tab 2 | **Có cả PASS và FAIL**; xem kết quả thật tại `docs/evidence/uat/UAT-01.txt` đến `UAT-10.txt` | |

---

## Tổng Kết Tình Trạng Lỗi
- **Lỗi P1 (Nghiêm trọng)**: 0 phát hiện, 0 còn mở.
- **Lỗi P2 (Cao)**: 0 phát hiện, 0 còn mở.
- **Lỗi P3 (Trung bình)**: 2 phát hiện (1 không sửa kèm lý do an toàn concurrency, 1 còn mở theo backlog mở rộng domain).
- **Lỗi P4 (Thấp)**: 5 phát hiện (1 đã sửa; các phát hiện còn lại được chấp nhận, giữ nguyên hoặc đang mở).
- **Tình trạng nghiệm thu**: Chưa kết luận đạt UAT; lần E2E mới có 9 PASS, 14 FAIL và 1 SKIP. Các luồng workflow chính không qua được bước tạo vị trí tuyển dụng do API trả HTTP 403.
