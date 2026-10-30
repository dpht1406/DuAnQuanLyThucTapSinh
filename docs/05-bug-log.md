# Nhật Ký Lỗi & Phát Hiện (Bug Log)

Bảng theo dõi các lỗi sẵn có (known issues) hoặc phát hiện qua kiểm thử/review chéo.

| Mã Lỗi | Mô Tả | Mức Độ | Trạng Thái | Ghi Chú Kỹ Thuật / Lý Do |
|:---:|---|:---:|:---:|---|
| **BUG-PRE-01** | Tham số `preserveLineBreaks` trong hàm `CleanString` không bao giờ được sử dụng, hàm luôn gọi `value.Trim()` | P3 | Giữ nguyên hành vi (Known Issue) | Được ghi chú trong code và test khẳng định hành vi hiện tại không phụ thuộc vào `preserveLineBreaks`. Không thay đổi logic tránh rủi ro phá vỡ tương thích. |
| **BUG-PRE-02** | Dòng 459 trong `CancelAcceptedAsync` sử dụng chuỗi cứng `"Không thể hoàn tất, vui lòng thử lại."` thay vì hằng số `RetryConflict` | P4 | Đã ghi nhận | Chuỗi trả về giống hệt hằng số `RetryConflict` nên không gây sai khác runtime đối với caller. Trong đợt refactor sẽ quy về dùng hằng số chung. |
| **BUG-PRE-03** | Khởi tạo mặc định `PlacementRequestEmailNotifier` dùng `NoOpEmailSender` không gửi email thực tế trong một số constructor overload | P4 | Giữ nguyên hành vi | Đảm bảo tính an toàn khi unit test và độc lập môi trường không phụ thuộc máy chủ SMTP ngoài. |
