# Nhật Ký Quản Lý Lỗi & Phát Hiện (Unified Bug Log)

Bảng quản lý tập trung toàn bộ các vấn đề phát hiện từ:
1. Các vấn đề tồn tại trước refactor (Known Issues - Tab 1)
2. Kết quả đánh giá chéo mã nguồn (Cross-Review - Tab 2)
3. Kết quả kiểm thử chấp nhận người dùng thực tế (UAT 10 kịch bản - Tab 2)

---

## Bảng Quản Lý Lỗi Tổng Hợp

| Mã | Mô Tả | Mức Ưu Tiên | Nguồn Phát Hiện | Người Xử Lý | Trạng Thái | Commit Sửa |
|:---:|---|:---:|:---:|:---:|:---:|:---:|
| **BUG-PRE-01** / **CR-01** | Tham số `preserveLineBreaks` trong hàm `CleanString` không được sử dụng thực tế, hàm luôn gọi `value.Trim()` | **P4** | Known Issue / Cross-Review | Tab 1 & Tab 2 | **Không sửa kèm lý do** (Giữ nguyên chữ ký hàm và hành vi baseline để tránh breaking changes; đã bổ sung discard `_ = preserveLineBreaks;` và dẫn chiếu tài liệu) | `898644e` |
| **BUG-PRE-02** | Dòng 459 trong `CancelAcceptedAsync` sử dụng chuỗi cứng thay vì hằng số tập trung | **P4** | Known Issue (Tab 1) | Tab 1 | **Đã sửa** (Đã chuyển toàn bộ chuỗi thông báo và trạng thái lỗi sang hằng số `private const string`) | `898644e` |
| **BUG-PRE-03** | Khởi tạo mặc định `PlacementRequestEmailNotifier` dùng `NoOpEmailSender` trong constructor overload khi không truyền sender | **P4** | Known Issue (Tab 1) | Tab 1 | **Không sửa kèm lý do** (Chủ đích thiết kế nhằm cách ly kiểm thử tự động và môi trường dev cục bộ khỏi phụ thuộc dịch vụ SMTP ngoài) | N/A |
| **CR-02** | Trong luồng `CancelAcceptedAsync` trên CSDL quan hệ, có 2 lần truy vấn entity đơn ứng tuyển (`GetRequestSummaryAsync` và `GetPlacementRequestByIdAsync`) | **P3** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Không sửa kèm lý do** (Bảo toàn thiết kế concurrency lock của baseline: bắt buộc khóa dòng thành công trước khi ChangeTracker theo dõi thực thể) | N/A |
| **CR-03** | Template log lỗi transaction trong `ExecuteInTransactionAsync` bổ sung từ khóa "transaction", khác nhẹ so với câu log gốc của baseline | **P4** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Không sửa kèm lý do** (Log rõ nghĩa hơn về ngữ cảnh giao dịch thất bại, không ảnh hưởng API contract hay mã lỗi) | `1514649` |
| **CR-04** | Phương thức `AcceptAsync` chỉ kiểm tra công ty tồn tại, chưa kiểm tra trạng thái hoạt động / khóa của công ty | **P3** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Còn mở** (Kế thừa từ baseline; đề xuất đưa vào backlog khi domain entity `Company` được bổ sung cờ trạng thái `IsActive`) | N/A |
| **CR-05** | Gửi email thông báo nằm ngoài transaction commit; nếu server sập đúng thời điểm commit xong thì email không gửi được | **P4** | Cross-Review (Tab 2) | Tab 2 ghi nhận | **Còn mở** (Đã bọc an toàn trong `NotifySafelyAsync`; đề xuất chuyển sang Outbox Pattern trong giai đoạn mở rộng microservices) | `48b2134` |
| **UAT-01..10** | 10 kịch bản kiểm thử chấp nhận người dùng chạy trên backend và cơ sở dữ liệu thật SQL Server | N/A | UAT Suite (Tab 2) | QA Tab 2 | **Đã kiểm chứng (10/10 Đạt)** (Không phát hiện bất kỳ lỗi hồi quy hay vi phạm nghiệp vụ nào) | N/A |

---

## Tổng Kết Tình Trạng Lỗi
- **Lỗi P1 (Nghiêm trọng)**: 0 phát hiện, 0 còn mở.
- **Lỗi P2 (Cao)**: 0 phát hiện, 0 còn mở.
- **Lỗi P3 (Trung bình)**: 2 phát hiện (1 không sửa kèm lý do an toàn concurrency, 1 còn mở theo backlog mở rộng domain).
- **Lỗi P4 (Thấp)**: 4 phát hiện (1 đã sửa, 2 không sửa kèm lý do tương thích/an toàn, 1 còn mở theo kiến trúc tương lai).
- **Tình trạng nghiệm thu**: **100% ĐẠT TIÊU CHUẨN PHÁT HÀNH** (Không có bất kỳ lỗi P1 hoặc P2 nào đang mở).
