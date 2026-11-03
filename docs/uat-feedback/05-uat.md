# Báo Cáo Kiểm Thử Chấp Nhận Người Dùng (UAT Suite 2 - 10 Kịch Bản Chạy Thật)

- **Người thực hiện**: Reviewer & QA Độc lập (TAB 2)
- **Môi trường backend**: ASP.NET Core 8 API nội bộ cổng biệt lập `http://127.0.0.1:5028` kết nối cơ sở dữ liệu SQL Server biệt lập `StudentInternshipMgmtDb_E2E`.
- **Môi trường frontend**: Vue 3 + PrimeVue 4 chạy trên cổng biệt lập `http://127.0.0.1:5183`.
- **Script điều phối kiểm thử**: [`run-uat2.ps1`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/docs/manual-tests/run-uat2.ps1) (độc lập, không sửa `run-e2e.ps1`, không can thiệp CSDL chính `StudentInternshipMgmtDb`).
- **Thư mục lưu trữ bằng chứng**:
  - Bằng chứng thô từng kịch bản: [`evidence/uat/UAT2-01.txt`](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/uat/UAT2-01.txt) đến [`evidence/uat/UAT2-10.txt`](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/uat/UAT2-10.txt)
  - Ảnh chụp màn hình giao diện thực tế: [`evidence/uat/screens/`](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/uat/screens/)

> [!NOTE]
> **Tuyên bố tính chất phản hồi**: Các phản hồi người dùng trong [01-feedback-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/01-feedback-log.md) là phản hồi mẫu mô phỏng từ quan sát thực nghiệm (chưa có người dùng thật). "10 kịch bản" trong báo cáo này được tính dựa trên 10 kịch bản kiểm thử chạy thật hoàn toàn trên backend và CSDL SQL Server thật, không tính theo số lượng người dùng.

---

## 1. Bảng Tổng Hợp 10 Kịch Bản Kiểm Thử Thực Tế (UAT2-01 .. UAT2-10)

| Mã | Tên kịch bản | Người dùng mẫu | Các bước thực hiện | Kết quả mong đợi | Kết quả thực tế | Đánh giá | Bằng chứng |
| :---: | :--- | :---: | :--- | :--- | :--- | :---: | :---: |
| **UAT2-01** | Kiểm chứng phản hồi FB-01: Chuẩn hóa thông báo lỗi tạo sinh viên tiếng Việt | `AD-01` (Admin) | 1. Quản trị viên để trống toàn bộ form tạo sinh viên và gửi đi.<br>2. Gửi tạo sinh viên với mã sinh viên đã tồn tại. | Hệ thống từ chối lưu và hiển thị thông báo lỗi bằng tiếng Việt tự nhiên rõ ràng ("Họ tên không được để trống.", "Số điện thoại không được để trống.", "Mã số sinh viên không được để trống."), không còn kẹp các từ tiếng Anh kỹ thuật như "FullName là bắt buộc". | Hệ thống từ chối yêu cầu và phản hồi toàn bộ thông báo lỗi chuẩn tiếng Việt tự nhiên, xác nhận không còn từ tiếng Anh kỹ thuật nào trong thông báo. | **PASS** | [UAT2-01.txt](evidence/uat/UAT2-01.txt)<br>[Ảnh chụp](evidence/uat/screens/UAT2-01-student-management.png) |
| **UAT2-02** | Kiểm chứng phản hồi FB-02: Bỏ định danh (Title) và (Quantity) trong tin tuyển dụng | `DN-01` (Doanh nghiệp) | 1. Doanh nghiệp tạo tin tuyển dụng nhưng bỏ trống tiêu đề và nhập số lượng tuyển bằng 0.<br>2. Bấm Lưu tin. | Hệ thống từ chối tạo tin và hiển thị thông báo lỗi thân thiện ("Tiêu đề là bắt buộc.", "Số lượng tuyển phải lớn hơn 0."), không còn chứa các mã định danh kỹ thuật trong ngoặc đơn như (Title) hay (Quantity). | Hệ thống từ chối tạo tin tuyển dụng không hợp lệ và trả về câu thông báo hoàn toàn sạch mã biến kỹ thuật. | **PASS** | [UAT2-02.txt](evidence/uat/UAT2-02.txt)<br>[Ảnh chụp](evidence/uat/screens/UAT2-02-company-job-positions.png) |
| **UAT2-03** | Kiểm chứng phản hồi FB-03: Tự động cắt tỉa khoảng trắng 2 đầu CV URL và an toàn URL | `SV-02` (Sinh viên) | 1. Sinh viên dán đường dẫn CV có dính khoảng trắng ở đầu và cuối.<br>2. Nộp đơn và kiểm tra bản ghi CSDL.<br>3. Thử nghiệm nộp URL scheme không an toàn (ftp://) hoặc chứa khoảng trắng ở giữa. | Hệ thống chấp nhận đường dẫn CV có khoảng trắng ở hai đầu, tự động cắt tỉa sạch sẽ và lưu URL chuẩn vào cơ sở dữ liệu; đồng thời kiên quyết từ chối các URL dùng giao thức ftp:// hoặc chứa khoảng trắng ở giữa. | Hệ thống lưu đơn thành công, giá trị CV URL trong cơ sở dữ liệu được cắt tỉa sạch sẽ; các trường hợp URL giao thức lạ hoặc có khoảng trắng ở giữa đều bị chặn lại đúng như thiết kế. | **PASS** | [UAT2-03.txt](evidence/uat/UAT2-03.txt)<br>[Ảnh chụp](evidence/uat/screens/UAT2-03-job-position-apply.png) |
| **UAT2-04** | Kiểm chứng phản hồi FB-04: Hiển thị tiếng Việt UTF-8 chuẩn xác trong README.md | `NM-01` (Người mới) | 1. Đọc và phân tích trực tiếp chuỗi nhị phân byte của tệp README.md.<br>2. Kiểm tra dòng mô tả cuối cùng. | Tệp tài liệu hiển thị văn bản tiếng Việt đúng chuẩn UTF-8, không bị lỗi font biến dạng (mojibake) ký tự. | Dòng cuối cùng của tài liệu khớp 100% từng byte UTF-8 với câu tiếng Việt chuẩn: "Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3).", hoàn toàn sạch lỗi mã hóa. | **PASS** | [UAT2-04.txt](evidence/uat/UAT2-04.txt) |
| **UAT2-05** | Kiểm chứng phản hồi FB-10: Chống race condition nộp đơn đúp (Double-Submit) | `SV-01` (Sinh viên) | 1. Chuẩn bị sinh viên và vị trí thực tập đang mở.<br>2. Mô phỏng bấm đúp chuột bằng cách gửi 2 yêu cầu nộp đơn cùng thời điểm mili-giây.<br>3. Kiểm tra số bản ghi trong CSDL. | Hệ thống sử dụng khóa dòng và giao dịch dữ liệu để xử lý: đúng 1 yêu cầu được tạo thành công và yêu cầu còn lại bị từ chối do trùng lặp; trong CSDL chỉ xuất hiện đúng 1 đơn ứng tuyển duy nhất. | Khóa dòng trên bảng dữ liệu đã kích hoạt thành công: 1 yêu cầu được chấp thuận tiếp nhận, 1 yêu cầu bị chặn với lỗi đã có đơn mở; trong CSDL kiểm tra thực tế chỉ có đúng 1 bản ghi được tạo. | **PASS** | [UAT2-05.txt](evidence/uat/UAT2-05.txt) |
| **UAT2-06** | Hồi quy luồng Sinh viên: Tra cứu việc, nộp đơn và theo dõi trạng thái | `SV-01` (Sinh viên) | 1. Sinh viên đăng nhập vào hệ thống.<br>2. Xem danh sách vị trí việc làm đang mở.<br>3. Nộp hồ sơ ứng tuyển với CV hợp lệ.<br>4. Kiểm tra danh sách đơn cá nhân. | Sinh viên nộp đơn thuận lợi và hồ sơ hiển thị đầy đủ trong danh sách đơn cá nhân ở trạng thái Đang chờ duyệt (Pending). | Toàn bộ quy trình từ xem việc làm, gửi hồ sơ đến kiểm tra tiến độ diễn ra mượt mà; đơn ứng tuyển hiển thị chính xác ở trạng thái Chờ xử lý. | **PASS** | [UAT2-06.txt](evidence/uat/UAT2-06.txt)<br>[Ảnh chụp](evidence/uat/screens/UAT2-06-student-jobs-portal.png) |
| **UAT2-07** | Hồi quy luồng Doanh nghiệp: Tiếp nhận hồ sơ, mời phỏng vấn và đánh giá kết quả | `DN-01` (Doanh nghiệp) | 1. Doanh nghiệp đăng nhập và tra cứu danh sách ứng viên nộp vào công ty.<br>2. Gửi thư mời phỏng vấn với thời gian và địa điểm cụ thể.<br>3. Cập nhật kết quả tuyển dụng Đạt (Passed). | Đơn ứng tuyển chuyển từ trạng thái Chờ xử lý sang Đã mời phỏng vấn, sau đó chuyển sang Đã tiếp nhận (Accepted); lịch sử thay đổi ghi nhận đầy đủ người thực hiện. | Doanh nghiệp thực hiện trơn tru các bước duyệt đơn, gửi lịch hẹn phỏng vấn và phê duyệt tiếp nhận ứng viên thành công; hệ thống ghi nhận trạng thái tiếp nhận chính xác. | **PASS** | [UAT2-07.txt](evidence/uat/UAT2-07.txt) |
| **UAT2-08** | Hồi quy luồng Quản trị viên: Duyệt tin tuyển dụng và theo dõi Dashboard | `AD-01` (Admin) | 1. Doanh nghiệp gửi yêu cầu phê duyệt tin tuyển dụng mới.<br>2. Quản trị viên đăng nhập và phê duyệt tin.<br>3. Kiểm tra số liệu thống kê tổng quan trên Dashboard. | Tin tuyển dụng được chuyển sang trạng thái Đang mở (IsOpen = True); bảng điều khiển Dashboard cập nhật các chỉ số tổng quan nhanh chóng và chính xác. | Tin tuyển dụng được mở công khai cho sinh viên ứng tuyển ngay sau khi duyệt; số liệu thống kê sinh viên và doanh nghiệp trên Dashboard hiển thị nhất quán. | **PASS** | [UAT2-08.txt](evidence/uat/UAT2-08.txt)<br>[Ảnh chụp](evidence/uat/screens/UAT2-08-admin-dashboard.png) |
| **UAT2-09** | Đường dẫn lỗi 1: Xác thực dữ liệu đầu vào thiếu hoặc sai định dạng | `SV-01` / `DN-01` | 1. Sinh viên nộp đơn thiếu họ tên, email sai cấu trúc, số điện thoại không hợp lệ, thư ứng tuyển quá ngắn.<br>2. Doanh nghiệp tạo tin tuyển dụng với chỉ tiêu số lượng âm (-5). | Hệ thống từ chối toàn bộ các yêu cầu nhập liệu không hợp lệ ở cả hai vai trò và hiển thị lý do chi tiết dưới từng ô nhập. | Hệ thống từ chối các yêu cầu vi phạm quy tắc xác thực dữ liệu và trả về thông báo lỗi cụ thể, dễ hiểu cho người dùng. | **PASS** | [UAT2-09.txt](evidence/uat/UAT2-09.txt) |
| **UAT2-10** | Đường dẫn lỗi 2: Phân quyền vai trò và cách ly dữ liệu chéo (Multi-Tenancy) | `SV-01` / `DN-01` / Khách | 1. Sinh viên cố tình gọi xem danh bạ sinh viên của Admin.<br>2. Người dùng chưa đăng nhập gọi tài nguyên quản trị.<br>3. Doanh nghiệp B cố tình sửa tin tuyển dụng của Doanh nghiệp A. | Hệ thống nhận diện đúng quyền hạn: từ chối sinh viên (Cấm truy cập - 403), từ chối khách chưa đăng nhập (Yêu cầu xác thực - 401), và ngăn chặn doanh nghiệp can thiệp dữ liệu của đơn vị khác (403/404). | Mọi hành vi truy cập vượt quyền hoặc can thiệp dữ liệu giữa các doanh nghiệp đều bị hệ thống phát hiện và chặn đứng an toàn tuyệt đối. | **PASS** | [UAT2-10.txt](evidence/uat/UAT2-10.txt) |

---

## 2. So Sánh Trước / Sau Trên uat-baseline và HEAD

Đối chiếu thực nghiệm giữa phiên bản gốc trước khi sửa (`uat-baseline`) và phiên bản hiện tại (`HEAD`):

```text
+-------------------+-----------------------------------------+-----------------------------------------+
| Mã phản hồi / Ca  | Hành vi trên uat-baseline (Trước sửa)   | Hành vi trên HEAD (Sau khi sửa)        |
+-------------------+-----------------------------------------+-----------------------------------------+
| FB-01 (UAT2-01)   | Trả về lỗi kẹp tên biến tiếng Anh cứng: | Trả về thông báo tiếng Việt tự nhiên:   |
|                   | "FullName là bắt buộc."                 | "Họ tên không được để trống."           |
|                   | "PhoneNumber là bắt buộc."              | "Số điện thoại không được để trống."    |
+-------------------+-----------------------------------------+-----------------------------------------+
| FB-02 (UAT2-02)   | Thông báo lỗi kẹp mã biến trong ngoặc:  | Thông báo lỗi bỏ hoàn toàn mã biến:     |
|                   | "Tiêu đề (Title) là bắt buộc."          | "Tiêu đề là bắt buộc."                  |
|                   | "Số lượng tuyển (Quantity)..."          | "Số lượng tuyển phải lớn hơn 0."        |
+-------------------+-----------------------------------------+-----------------------------------------+
| FB-03 (UAT2-03)   | Dán URL CV dính khoảng trắng 2 đầu bị   | Tự động cắt tỉa khoảng trắng 2 đầu;     |
|                   | từ chối lỗi 400; gây phiền toái mobile. | lưu URL sạch; kiên quyết chặn ftp/space.|
+-------------------+-----------------------------------------+-----------------------------------------+
| FB-04 (UAT2-04)   | Dòng cuối README.md bị lỗi font chữ     | Mã hóa tiếng Việt chuẩn UTF-8 không BOM,|
|                   | mojibake biến dạng ký tự.               | hiển thị chữ tròn trịa, chuẩn xác 100%.|
+-------------------+-----------------------------------------+-----------------------------------------+
| FB-10 (UAT2-05)   | Bấm nhanh nộp đơn tạo ra 2 bản ghi đơn  | Khóa dòng SQL + Transaction bảo vệ;     |
|                   | mở song song cho cùng 1 sinh viên/vị trí| chỉ có đúng 1 bản ghi được tạo duy nhất.|
+-------------------+-----------------------------------------+-----------------------------------------+
```

---

## 3. Tổng Kết Đánh Giá Nghiệm Thu UAT

- **Tổng số kịch bản kiểm thử thực tế**: 10 kịch bản (`UAT2-01` đến `UAT2-10`).
- **Số kịch bản ĐẠT (PASS)**: **10/10 (100%)**
- **Số kịch bản KHÔNG ĐẠT (FAIL)**: **0/10 (0%)**
- **Số kịch bản CHƯA CHẠY**: **0/10 (0%)**
- **Đánh giá chất lượng vận hành**: Toàn bộ các phản hồi người dùng đã chọn (FB-01, FB-02, FB-03, FB-04, FB-10) đều được kiểm chứng giải quyết triệt để trên môi trường chạy thật. Các luồng hồi quy cho Sinh viên, Doanh nghiệp, Quản trị viên và các chốt chặn an toàn bảo mật, phân quyền đa người thuê đều vận hành chính xác và ổn định.
