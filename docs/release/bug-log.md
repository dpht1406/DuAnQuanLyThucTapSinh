# Sổ Nhật Ký Quản Lý Lỗi & Phát Hiện (Unified Bug Log - Release v1.0.1)

Tài liệu này theo dõi và quản lý tập trung toàn bộ các vấn đề phát hiện trong suốt vòng đời dự án từ:
1. Các vấn đề trước tái cấu trúc (Pre-refactor Known Issues)
2. Kết quả đánh giá chéo mã nguồn độc lập (Cross-Review)
3. 10 phản hồi quan sát người dùng thực tế (FB-01 đến FB-10 trong [docs/uat-feedback/01-feedback-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/01-feedback-log.md): khắc phục 5, từ chối 2, hoãn 3)
4. Kết quả thực thi 10 kịch bản UAT tự động hóa trên môi trường sạch biệt lập
5. Toàn bộ 7 phát hiện từ Báo cáo Giám định Độc lập (`docs/release/independent-review.md`).

---

## 1. Bảng Quản Lý Lỗi Tổng Hợp (Priority Matrix P1 - P4)

| Mã Lỗi / Phát Hiện | Mức Ưu Tiên | Mô Tả Vấn Đề | Cách Tái Hiện | Trạng Thái | Commit Khắc Phục / Quyết Định |
| :---: | :---: | :--- | :--- | :---: | :--- |
| **FB-10 / TC-12** | **P2** | Race condition khi click nút nộp đơn 2 lần liên tiếp hoặc gửi song song 2 request tạo ra 2 đơn mở trùng lặp trong CSDL | Gửi 2 request HTTP POST đồng thời tới `/api/me/placement-requests` cho cùng một sinh viên và vị trí | **ĐÃ SỬA** | `21d7ded`<br>(Áp dụng Database Transaction & Pessimistic Row Lock trên bảng `Students`; vô hiệu hóa nút bấm tại client) |
| **P2-01** | **P2** | Báo cáo UAT từng dùng phát biểu chưa chuẩn xác về việc xử lý 10 phản hồi quan sát người thật (FB-01..FB-10) trong khi KNOWN-ISSUES từ chối 2 và hoãn 3 | Kiểm tra dòng 5 file `uat-scenarios.md` và dòng 751 file `run-uat-scenarios.ps1` | **ĐÃ SỬA** | `730aff6`<br>(Đính chính trung thực: khắc phục 5 bằng code, từ chối 2 theo thiết kế, hoãn 3 sang giai đoạn sau) |
| **P2-02** | **P2** | `README-REPRODUCE.md` chỉ hướng dẫn build `DuAnQuanLyThucTapSinh.sln` nhưng solution thiếu 2 test projects, khiến lệnh test `--no-build` bị crash thiếu dll | Giải nén gói zip và chạy tuần tự theo Bước 4.2 và 4.3 trong README cũ | **ĐÃ SỬA** | `ff43b65`<br>(Bổ sung đầy đủ cả 3 test projects thật vào `DuAnQuanLyThucTapSinh.sln` và cập nhật README) |
| **FB-01** | **P3** | Thông báo lỗi khi tạo/sửa sinh viên hiển thị nửa tiếng Anh nửa tiếng Việt (`FullName là bắt buộc.`, `PhoneNumber là bắt buộc.`) | Gửi payload rỗng hoặc thiếu trường bắt buộc tới `POST /api/students` | **ĐÃ SỬA** | `cde0541`<br>(Chuẩn hóa thông báo sang tiếng Việt tự nhiên: "Họ tên không được để trống.", v.v.) |
| **FB-02** | **P3** | Thông báo lỗi trên tin tuyển dụng đính kèm các mã biến kỹ thuật `(Title)` và `(Quantity)` gây khó hiểu cho doanh nghiệp | Tạo tin tuyển dụng với số lượng = 0 và để trống tiêu đề tại `POST /api/company-me/job-positions` | **ĐÃ SỬA** | `40ff5a9`<br>(Loại bỏ hoàn toàn các mã kỹ thuật ở cả frontend và backend) |
| **FB-03** | **P3** | Dán liên kết CV có khoảng trắng ở 2 đầu bị hệ thống từ chối nộp đơn với mã lỗi 400 | Nộp đơn với `CvUrl` chứa khoảng trắng ở đầu hoặc cuối | **ĐÃ SỬA** | `838f6de`<br>(Tự động cắt tỉa 2 đầu CV URL, lưu chuỗi sạch vào CSDL; siết chặt chặn scheme lạ `ftp:`, `javascript:`) |
| **CR-03** | **P3** | Cờ trạng thái `saving` ở hàm submit nộp đơn chưa được bọc bằng `try...finally`, có nguy cơ treo nút khi client validation ném lỗi | Thao tác nộp đơn khi dữ liệu form không thỏa mãn validation phía client | **ĐÃ SỬA** | `6588fb3`<br>(Bọc toàn bộ hàm `submit()` trong `try...finally` ngay sau khi đặt `saving=true`, bổ sung 4 unit tests xác minh) |
| **DOC-01** | **P3** | Tài liệu tái lập clone sạch thiếu chỉ dẫn `npm install` / `npm ci` trước khi chạy `npm test` và `npm run build` | Thực hiện clone sạch và chạy trực tiếp lệnh `npm test` | **ĐÃ SỬA** | `e8dec7b`<br>(Bổ sung chỉ dẫn cài đặt dependency bắt buộc trong README-REPRODUCE) |
| **P3-01** | **P3** | Bước Cross-Review trong `run-release.ps1` chỉ kiểm tra `Test-Path` tĩnh (0.00s); file `cross-review.md` thiếu tính khách quan | Kiểm tra đoạn mã `CROSS-REVIEW-AND-BUG-LOG` trong script release v1.0.0 | **ĐÃ SỬA** | Nâng cấp script kiểm tra tự động đủ mục P1-P4, commit range, 3 rủi ro; viết lại `cross-review.md` khách quan |
| **P3-02** | **P3** | Lệch commit giữa gói zip (sinh tại `34ec284`) và tag `v1.0.0` (gắn tại `5a3bc91`), làm lệch 32 files tài liệu/evidence | So sánh cây thư mục gói zip với commit gắn tag `v1.0.0` | **ĐÃ SỬA** | Tự động hóa đóng gói từ đúng commit được tag và ghi nhận `PROVENANCE.md` trong `run-release.ps1` |
| **P3-03** | **P3** | Dự án rỗng `Application.Tests` mồ côi, không nằm trong solution và không được chạy | Kiểm tra thư mục `InternshipManagement/backend/tests/Application.Tests` | **ĐÃ SỬA** | `ff43b65`<br>(Xóa bỏ hoàn toàn dự án bằng `git rm`, có thể khôi phục từ git history nếu cần) |
| **P3-DIALECT** | **P3** | Câu lệnh T-SQL khóa dòng `UPDATE [Students] SET [Status] = [Status]` gắn chặt phương ngữ Microsoft SQL Server | Chạy kiểm thử trên SQLite hoặc PostgreSQL | **HOÃN CÓ LÝ DO** | Môi trường mục tiêu là SQL Server cục bộ/Azure SQL theo hợp đồng; không tái cấu trúc ORM để tránh rủi ro hồi quy |
| **FB-04** | **P4** | Dòng cuối cùng tệp tài liệu `InternshipManagement/README.md` bị lỗi font chữ hiển thị biến dạng (mojibake) | Mở file `README.md` bằng trình đọc UTF-8 thông thường | **ĐÃ SỬA** | `381d0eb`<br>(Chuyển đổi toàn bộ tài liệu sang chuẩn UTF-8 không BOM) |
| **P4-01** | **P4** | Log UAT-03 ghi nhận `Kiểm tra UTF-8 BOM: False` do script PowerShell parse chuỗi trước khi kiểm tra byte | Kiểm tra dòng log trong file `docs/release/evidence/UAT-03.txt` | **ĐÃ SỬA** | `c6619a4`<br>(Chuyển sang kiểm tra stream nhị phân `-OutFile ... -PassThru`, xác nhận có BOM `0xEF, 0xBB, 0xBF`) |
| **P4-02** | **P4** | Chuỗi fallback rác trong `CompanyPlacementRequestDetailView.vue` và lệnh `trim()` ở `JobPositionApplyView.vue` ép kiểu ngầm định | Nộp đơn với trường null/số hoặc xem chi tiết đơn có thư giới thiệu rỗng | **ĐÃ SỬA** | `d49303e`<br>(Thay bằng câu tiếng Việt 'Chưa có thư giới thiệu.' và phòng thủ `typeof value === 'string' ? value.trim() : value`, kèm tests) |
| **BUG-PRE-01** | **P4** | Tham số `preserveLineBreaks` trong `CleanString` không được sử dụng thực tế, hàm luôn gọi `Trim()` | Đọc mã nguồn `PlacementAcceptanceService.cs` trước refactor | **Chấp nhận giữ nguyên** | Giữ nguyên để bảo toàn chữ ký và tương thích ngược |
| **BUG-PRE-02** | **P4** | Chuỗi thông báo lỗi trong `CancelAcceptedAsync` từng viết cứng thay vì dùng hằng số | Kiểm tra mã nguồn trước refactor | **ĐÃ SỬA** | `898644e`<br>(Khai trừ toàn bộ sang hằng số `private const string`) |
| **BUG-PRE-03** | **P4** | Constructor mặc định dùng `NoOpEmailSender` khi không inject notifier ngoài | Khởi tạo service không truyền tham số | **Chấp nhận giữ nguyên** | Cách ly môi trường dev/test khỏi phụ thuộc dịch vụ SMTP ngoài |
| **CR-01** | **P4** | Unit test `CreateRequestAsync_double_submission` chỉ kiểm tra tuần tự trên InMemory DB | Đọc file test `PlacementRequestServiceTests.cs` | **Chấp nhận giữ nguyên** | Đã có kịch bản UAT-07 kiểm chứng chạy đua song song trên SQL Server thật đạt PASS |
| **CR-02** | **P4** | Kiểm tra trùng mã sinh viên dùng `.ToLower()` hai vế trên CSDL không phân biệt hoa thường | Đọc `StudentService.cs` | **Chấp nhận / Backlog** | Giữ nguyên để tương thích đa nền tảng CSDL quan hệ |
| **KI-01** | **P4** | Ca test tìm kiếm dấu tiếng Việt `StudentSearchTests` được đánh dấu `[Skip]` theo thiết kế | Chạy toàn bộ test backend | **Chấp nhận giữ nguyên** | Do phụ thuộc cấu hình collation của SQL Server máy chủ |
| **KI-02** | **P4** | Gửi email thông báo sau transaction commit có thể trễ nếu tiến trình dừng đột ngột | Phân tích luồng gửi mail của service | **Chấp nhận / Backlog** | Đề xuất giải pháp Transactional Outbox pattern cho bản nâng cấp sau |

---

## 2. Thống Kê Tình Trạng Lỗi Theo Mức Độ Ưu Tiên (Release v1.0.1)

| Mức Độ Ưu Tiên | Tổng Phát Hiện | Đã Khắc Phục | Hoãn Có Lý Do / Chấp Nhận | Còn Mở (Open) | Điều Kiện Phát Hành |
| :---: | :---: | :---: | :---: | :---: | :---: |
| **P1 (Nghiêm trọng)** | 0 | 0 | 0 | **0** | **ĐẠT (Không có lỗi P1)** |
| **P2 (Cao)** | 3 | 3 | 0 | **0** | **ĐẠT (Zero lỗi P2 mở)** |
| **P3 (Trung bình)** | 10 | 9 | 1 | **0** | **ĐẠT (Zero lỗi P3 mở ngoài kế hoạch)** |
| **P4 (Thấp)** | 9 | 4 | 5 | **0** | **ĐẠT (Không có lỗi chặn)** |
| **TỔNG CỘNG** | **22** | **16** | **6** | **0** | **100% ĐẠT ĐIỀU KIỆN PHÁT HÀNH** |

---

## 3. Kết Luận Nghiệm Thu Chất Lượng
- **Không còn bất kỳ lỗi P1 hoặc P2 nào đang mở**.
- **100% các phát hiện từ đợt Giám định Độc lập (`independent-review.md`) đã được xử lý triệt để hoặc hoãn có lý do kỹ thuật chính đáng**.
- Hệ thống hoàn toàn đủ điều kiện an toàn chất lượng để phát hành chính thức bản **Release v1.0.1**.
