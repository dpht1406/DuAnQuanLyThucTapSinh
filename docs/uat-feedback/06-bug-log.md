# Sổ Nhật Ký Quản Lý Lỗi & Phát Hiện (Unified Bug Log - Round UAT 2)

Sổ theo dõi và quản lý tập trung toàn bộ các vấn đề phát hiện từ:
1. Các phản hồi mẫu UAT do TAB 1 thu thập và xử lý ([01-feedback-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/01-feedback-log.md)).
2. Kết quả đánh giá mã nguồn chéo độc lập của TAB 2 ([04-cross-review.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/04-cross-review.md)).
3. Kết quả chạy kiểm thử chấp nhận người dùng thực tế 10 kịch bản của TAB 2 ([05-uat.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/05-uat.md)).

---

## 1. Bảng Quản Lý Lỗi & Phát Hiện Tổng Hợp

| Mã | Mô tả | Mức ưu tiên | Nguồn phát hiện | Người xử lý | Trạng thái | Commit sửa |
| :---: | :--- | :---: | :---: | :---: | :---: | :---: |
| **FB-10** / **TC-12** | Race condition khi bấm nộp đơn đúp liên tiếp tạo 2 đơn mở trùng lặp | **P2** | Chạy thật TC-12 / Phản hồi UAT | TAB 1 | **Đã sửa** (Bổ sung row lock `UPDATE [Students]...` + Database Transaction và vô hiệu hóa nút bấm tại client) | `21d7ded` |
| **FB-01** | Thông báo lỗi tạo/sửa sinh viên hiển thị nửa tiếng Anh nửa tiếng Việt (`FullName là bắt buộc.`) | **P3** | Phản hồi UAT (AD-01) | TAB 1 | **Đã sửa** (Chuẩn hóa toàn bộ sang tiếng Việt tự nhiên: "Họ tên không được để trống.", v.v.) | `cde0541` |
| **FB-02** | Thông báo lỗi vị trí tuyển dụng kẹp mã định danh tiếng Anh `(Title)` và `(Quantity)` | **P3** | Phản hồi UAT (DN-01) | TAB 1 | **Đã sửa** (Bỏ hoàn toàn các mã biến kỹ thuật trong thông báo ở cả backend và frontend) | `40ff5a9` |
| **FB-03** | Dán liên kết CV có khoảng trắng ở đầu hoặc cuối bị hệ thống chặn nộp đơn (lỗi 400) | **P3** | Phản hồi UAT (SV-02) | TAB 1 | **Đã sửa** (Tự động cắt tỉa khoảng trắng 2 đầu ở cả 2 tầng, lưu chuỗi sạch; kiên quyết chặn scheme lạ và khoảng trắng giữa) | `838f6de` |
| **FB-04** | Dòng cuối cùng tệp tài liệu `README.md` bị lỗi font chữ hiển thị biến dạng (mojibake) | **P4** | Phản hồi UAT (NM-01) | TAB 1 | **Đã sửa** (Chuyển đổi mã hóa sang UTF-8 chuẩn không BOM) | `381d0eb` |
| **CR-01** | Unit test `CreateRequestAsync_double_submission...` chỉ kiểm tra tuần tự trên InMemory DB | **P4** | Cross-Review (TAB 2) | TAB 2 ghi nhận | **Chấp nhận giữ nguyên** (Đã có kịch bản UAT2-05 kiểm chứng chạy đua song song trên SQL Server thật đạt PASS) | N/A |
| **CR-02** | Kiểm tra trùng mã sinh viên dùng `.ToLower()` hai vế trên CSDL không phân biệt hoa thường | **P4** | Cross-Review (TAB 2) | TAB 2 ghi nhận | **Chấp nhận / Backlog** (Dữ liệu hiện tại nhỏ, giữ nguyên để đảm bảo đa nền tảng CSDL) | N/A |
| **CR-03** | Cờ trạng thái `saving` ở hàm submit ứng tuyển nên được bọc bởi `try...finally` toàn diện | **P3** | Cross-Review (TAB 2) | TAB 1 | **Đã sửa** (Bọc toàn bộ hàm `submit()` trong `try...finally` ngay sau khi đặt `saving=true`, bổ sung 4 unit test trong `JobPositionApplyView.test.js` kiểm chứng cờ `saving` luôn reset về `false`) | `6588fb3` |
| **CR-04** | Xác nhận tệp `README.md` khớp 100% từng byte UTF-8 tiếng Việt | **P4** | Cross-Review (TAB 2) | TAB 2 kiểm chứng | **Đã đóng** (Khớp với kết quả xử lý FB-04) | `381d0eb` |
| **CR-05** | Đồng bộ hóa cơ chế cắt tỉa và xác thực an toàn CV URL 3 tầng phòng thủ | **P4** | Cross-Review (TAB 2) | TAB 2 kiểm chứng | **Đạt tiêu chuẩn** (Thiết kế phòng vệ theo chiều sâu chuẩn mực) | `838f6de` |
| **DOC-01** | Hướng dẫn tái lập `README-REPRODUCE-UAT.md` (Mục 2.3) thiếu lệnh `npm install` trước `npm test` trên clone sạch | **P3** | Kiểm tra Clone sạch (TAB 2) | TAB 1 | **Đã sửa** (Bổ sung chỉ dẫn `npm install` bắt buộc tại Mục 2.3 trong `README-REPRODUCE-UAT.md`, đã xác minh tái lập thành công 100% trên clone sạch) | `e8dec7b` |
| **UAT2-01..10** | 10 kịch bản kiểm thử UAT chạy thật trên backend API và SQL Server biệt lập | N/A | UAT Suite (TAB 2) | TAB 2 QA | **10/10 ĐẠT (PASS)** (Chi tiết bằng chứng tại `evidence/uat/UAT2-01.txt` đến `UAT2-10.txt`) | N/A |

---

## 2. Tổng Kết Tình Trạng Lỗi Theo Mức Độ Ưu Tiên

- **Lỗi P1 (Nghiêm trọng)**: 0 phát hiện, **0 còn mở**.
- **Lỗi P2 (Cao)**: 1 phát hiện (`FB-10` race condition), **0 còn mở** (đã được sửa dứt điểm tại commit `21d7ded` và kiểm chứng thực tế đạt 100% tại `UAT2-05`).
- **Lỗi P3 (Trung bình)**: 5 phát hiện (5/5 đã sửa dứt điểm và đóng: `FB-01`, `FB-02`, `FB-03`, `CR-03` tại `6588fb3`, `DOC-01` tại `e8dec7b`), **0 còn mở**, **0 lỗi chặn phát hành**.
- **Lỗi P4 (Thấp)**: 5 phát hiện (2 đã sửa: `FB-04`, `CR-04`; 2 chấp nhận giữ nguyên / backlog: `CR-01`, `CR-02`; 1 đạt chuẩn: `CR-05`), **0 còn mở gây ảnh hưởng**.

---

## 3. Kết Luận Nghiệm Thu Chất Lượng
- **Không có bất kỳ lỗi P1 hoặc P2 nào đang mở**.
- **Không có bất kỳ lỗi P3 nào còn mở** (cả `CR-03` và `DOC-01` đều đã được TAB 1 khắc phục dứt điểm và TAB 2 xác minh thành công).
- Toàn bộ 5 phản hồi UAT do TAB 1 xử lý đều vượt qua các bài kiểm thử xác minh độc lập trên môi trường thật (10/10 PASS).
- Quy trình tái lập từ bản clone sạch đã được xác minh thành công 100% theo đúng `README-REPRODUCE-UAT.md`.
- **Đạt điều kiện phát hành và gắn thẻ phiên bản: `uat-v1.1`**.
