# Phiếu Hướng dẫn Kịch bản Kiểm thử UAT (UAT Scenarios Sheet)

> [!NOTE]
> Tài liệu này cung cấp các kịch bản thực nghiệm tiêu chuẩn (Test Scenarios) để phân phát cho các nhóm người dùng thực tế khi tham gia nghiệm thu hệ thống (UAT). Người tham gia sẽ làm theo từng bước và tích chọn kết quả tương ứng.

---

## 1. Kịch bản dành cho Vai trò Sinh viên (Student Scenarios)

### Kịch bản SV-SC-01: Đăng nhập lần đầu và Đổi mật khẩu bắt buộc
- **Mục tiêu**: Xác thực quy trình an toàn khi sinh viên nhận tài khoản mặc định và kích hoạt vào hệ thống.
- **Dữ liệu chuẩn bị**: Tài khoản sinh viên mới được cấp (ví dụ: mã SV `SV_TEST_01`, mật khẩu mặc định).
- **Các bước thực hiện**:
  1. Truy cập trang `/login`.
  2. Nhập mã sinh viên và mật khẩu ban đầu, bấm "Đăng nhập".
  3. Hệ thống phải tự động điều hướng sang trang `/change-password` và hiển thị thông báo yêu cầu đổi mật khẩu.
  4. Nhập mật khẩu hiện tại và mật khẩu mới đạt chuẩn (tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt).
  5. Bấm "Cập nhật mật khẩu".
- **Kết quả mong đợi**: Đổi mật khẩu thành công, hệ thống điều hướng vào trang Dashboard sinh viên.
- **Kết quả thực tế của bạn**:
  - [ ] Đạt (Pass) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Không đạt (Fail)
  - Ghi chú: ....................................................................................................

---

### Kịch bản SV-SC-02: Tìm kiếm và Nộp đơn Ứng tuyển Vị trí Thực tập (Kèm xử lý khoảng trắng Link CV)
- **Mục tiêu**: Xác thực sinh viên có thể duyệt danh sách vị trí đang mở, nhập hồ sơ và nộp đơn thành công ngay cả khi dán link CV bị thừa dấu cách ở đầu hoặc cuối.
- **Các bước thực hiện**:
  1. Vào menu "Vị trí thực tập" (`/job-positions`).
  2. Chọn một vị trí đang mở còn hạn nhận hồ sơ, bấm nút "Ứng tuyển".
  3. Kiểm tra thông tin cá nhân được tự động điền từ hồ sơ.
  4. Tại ô "Liên kết CV", dán một đường dẫn hợp lệ nhưng cố ý có dấu cách ở hai đầu (ví dụ: `"  https://drive.google.com/file/d/sample-cv/view  "`).
  5. Nhập lời giới thiệu bản thân (từ 20 đến 2000 ký tự).
  6. Bấm nút "Nộp đơn ứng tuyển".
- **Kết quả mong đợi**:
  - Hệ thống tự động cắt khoảng trắng ở hai đầu, không báo lỗi đỏ về định dạng URL.
  - Nút bấm chuyển sang trạng thái đang xử lý để chống bấm đúp.
  - Nhận thông báo nộp đơn thành công và đơn hiển thị trong danh sách "Đơn của tôi".
- **Kết quả thực tế của bạn**:
  - [ ] Đạt (Pass) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Không đạt (Fail)
  - Ghi chú: ....................................................................................................

---

### Kịch bản SV-SC-03: Kiểm tra Chặn Nộp Đơn Trùng lặp (Double Submission)
- **Mục tiêu**: Xác thực sinh viên không thể nộp 2 đơn mở cùng lúc cho cùng một vị trí tuyển dụng.
- **Các bước thực hiện**:
  1. Sau khi đã nộp đơn cho vị trí A ở kịch bản SV-SC-02, quay lại trang vị trí A.
  2. Thử nộp đơn một lần nữa cho vị trí A đó (hoặc bấm nhanh nút nộp đơn).
- **Kết quả mong đợi**: Hệ thống từ chối và thông báo rõ ràng: *"Bạn đã có đơn đang mở cho doanh nghiệp và vị trí này."*, không tạo bản ghi rác.
- **Kết quả thực tế của bạn**:
  - [ ] Đạt (Pass) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Không đạt (Fail)
  - Ghi chú: ....................................................................................................

---

## 2. Kịch bản dành cho Vai trò Doanh nghiệp (Company Scenarios)

### Kịch bản DN-SC-01: Đăng tin Tuyển dụng Vị trí Thực tập Mới
- **Mục tiêu**: Doanh nghiệp tạo tin tuyển dụng mới với đầy đủ xác thực thông tin rõ ràng.
- **Dữ liệu chuẩn bị**: Tài khoản doanh nghiệp (ví dụ: đại diện Công ty Nam Việt).
- **Các bước thực hiện**:
  1. Đăng nhập vào trang quản trị doanh nghiệp.
  2. Vào mục "Vị trí tuyển dụng" (`/company-me/job-positions`), bấm "Tạo vị trí mới".
  3. Thử bấm "Lưu" khi chưa điền Tiêu đề hoặc để số lượng = 0: xác nhận thông báo lỗi tiếng Việt sạch sẽ (*"Tiêu đề là bắt buộc."*, *"Số lượng tuyển phải lớn hơn 0."*).
  4. Điền tiêu đề hợp lệ, số lượng > 0, hạn nộp hồ sơ, địa điểm và mô tả công việc.
  5. Bấm "Lưu vị trí".
- **Kết quả mong đợi**: Vị trí mới được tạo ở trạng thái Chờ duyệt (Pending) hoặc Nháp (Draft).
- **Kết quả thực tế của bạn**:
  - [ ] Đạt (Pass) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Không đạt (Fail)
  - Ghi chú: ....................................................................................................

---

### Kịch bản DN-SC-02: Tiếp nhận Hồ sơ và Phản hồi Kết quả Phỏng vấn
- **Mục tiêu**: Doanh nghiệp duyệt danh sách đơn ứng tuyển, mời phỏng vấn và gửi kết quả tiếp nhận.
- **Các bước thực hiện**:
  1. Vào menu "Hồ sơ ứng tuyển" (`/company-me/placement-requests`).
  2. Chọn một đơn của sinh viên đang ở trạng thái `Pending`.
  3. Bấm "Mời phỏng vấn", nhập thời gian và địa điểm phỏng vấn, bấm xác nhận.
  4. Sau khi phỏng vấn, bấm "Đồng ý tiếp nhận" hoặc "Từ chối".
- **Kết quả mong đợi**: Trạng thái đơn được cập nhật chính xác, sinh viên và quản trị viên nhìn thấy trạng thái mới.
- **Kết quả thực tế của bạn**:
  - [ ] Đạt (Pass) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Không đạt (Fail)
  - Ghi chú: ....................................................................................................

---

## 3. Kịch bản dành cho Vai trò Cán bộ Quản lý (Admin Scenarios)

### Kịch bản AD-SC-01: Thêm mới Sinh viên với Xác thực Dữ liệu Chuẩn hóa
- **Mục tiêu**: Cán bộ quản lý nhập sinh viên mới, xác thực thông báo lỗi tiếng Việt thân thiện khi nhập thiếu hoặc trùng mã.
- **Các bước thực hiện**:
  1. Đăng nhập tài khoản Admin, vào mục "Quản lý Sinh viên" (`/students`).
  2. Bấm "Thêm sinh viên", thử bấm "Lưu" khi để trống toàn bộ form.
  3. Xác nhận các thông báo hiển thị: *"Họ tên không được để trống."*, *"Số điện thoại không được để trống."*, *"Mã số sinh viên không được để trống."* (không còn từ tiếng Anh).
  4. Nhập đầy đủ thông tin nhưng dùng mã sinh viên đã tồn tại: xác nhận thông báo *"Mã số sinh viên '...' đã tồn tại."*.
  5. Nhập mã sinh viên mới và lưu thành công.
- **Kết quả mong đợi**: Thao tác trơn tru, thông báo chuẩn mực, sinh viên mới xuất hiện trong danh sách.
- **Kết quả thực tế của bạn**:
  - [ ] Đạt (Pass) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Không đạt (Fail)
  - Ghi chú: ....................................................................................................

---

### Kịch bản AD-SC-02: Duyệt Vị trí Tuyển dụng của Doanh nghiệp
- **Mục tiêu**: Admin kiểm duyệt nội dung tin tuyển dụng trước khi công khai cho sinh viên ứng tuyển.
- **Các bước thực hiện**:
  1. Vào menu "Duyệt tin tuyển dụng" (`/admin/job-positions`).
  2. Xem chi tiết tin do doanh nghiệp gửi lên.
  3. Bấm "Phê duyệt" (Approve).
- **Kết quả mong đợi**: Trạng thái tin chuyển sang "Đã duyệt" và lập tức xuất hiện trên bảng tin tuyển dụng của sinh viên.
- **Kết quả thực tế của bạn**:
  - [ ] Đạt (Pass) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Không đạt (Fail)
  - Ghi chú: ....................................................................................................
