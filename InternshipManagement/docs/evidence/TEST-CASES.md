# Ca kiểm thử form ứng tuyển

- AC-01: Tự điền hồ sơ và trạng thái tải hồ sơ.
- AC-02: Kiểm tra dữ liệu ở client và hiển thị lỗi theo trường.
- AC-03: Ánh xạ lỗi server và thông báo nghiệp vụ.
- AC-04: Gửi đơn, chống gửi lặp và cập nhật danh sách.
- AC-05: Hiển thị an toàn, truy cập được và tương thích màn hình nhỏ.

| Mã | Tiêu chí | Bước | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|
| TC-01 | AC-01 | Đăng nhập sinh viên, mở form ứng tuyển. | Họ tên, email, SĐT, ngành lấy từ hồ sơ; trường để trống. | CHƯA CHẠY: cần phiên đăng nhập sinh viên trên UI. | CHƯA CHẠY |
| TC-02 | AC-02 | Xóa toàn bộ trường rồi bấm Gửi. | Lỗi riêng cho 7 trường; không phát sinh request. | Tự động: validator báo lỗi cho cả 7 trường rỗng và toàn khoảng trắng. UI/Network: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-03 | AC-02 | Nhập email `abc`, `a@`, `a b@c.com` hoặc email 255 ký tự. | Email bị từ chối, lỗi hiển thị tại trường Email. | Tự động: backend và frontend từ chối các giá trị trong bảng test. UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-04 | AC-02 | Nhập SĐT `0123`, `1234567890`, `012345678a` hoặc `01234567890`. | SĐT bị từ chối; `0901234567` hợp lệ. | Tự động: các giá trị sai bị từ chối, số hợp lệ được chấp nhận. UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-05 | AC-02 | Nhập CV `javascript:alert(1)`. | URL CV bị từ chối. | Tự động: backend và frontend đều từ chối scheme javascript. UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-06 | AC-02 | Nhập `https://drive.google.com/file/d/abc/view`. | URL CV được chấp nhận. | Tự động: backend và frontend chấp nhận URL HTTPS. UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-07 | AC-02 | Nhập lời giới thiệu 19 ký tự, sau đó thử 20, 2000 và 2001 ký tự. | 19/2001 bị từ chối; 20/2000 hợp lệ. | Tự động: cả bốn biên được kiểm tra ở backend và frontend. UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-08 | AC-04 | Gửi form hợp lệ. | Đơn được lưu; dialog đóng, toast thành công và danh sách cập nhật. | Tự động: service lưu thành công snapshot đã trim; UI/toast/danh sách: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-09 | AC-03, AC-04 | Gửi lần nữa khi sinh viên còn đơn Pending. | Form không đóng; hiện lỗi nghiệp vụ từ server. | Tự động: service từ chối đơn thứ hai với lỗi Pending. UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-10 | AC-03 | Gửi tới vị trí đóng hoặc đã quá hạn. | Server từ chối và UI giữ form, hiển thị lỗi nghiệp vụ. | Tự động: service từ chối vị trí đóng và hết hạn; UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-11 | AC-04 | Đổi email trên form khác email hồ sơ rồi gửi. | Đơn lưu email mới; hồ sơ sinh viên không đổi. | Tự động: service lưu email form và giữ email hồ sơ nguyên trạng. UI: CHƯA CHẠY. | TỰ ĐỘNG PASS; UI CHƯA CHẠY |
| TC-12 | AC-04 | Bấm Gửi hai lần liên tiếp nhanh. | Chỉ tạo một đơn. | CHƯA CHẠY: chưa có kiểm thử tương tác UI/network cho thao tác nhấp đúp. | CHƯA CHẠY |
| TC-13 | AC-05 | Nhập `<script>` vào lời giới thiệu rồi hiển thị nội dung. | Nội dung hiện như văn bản, không thực thi script. | CHƯA CHẠY: cần kiểm tra trực tiếp trên UI. | CHƯA CHẠY |
| TC-14 | AC-05 | Mở form ở viewport rộng 375px. | Nội dung cuộn được, nút thao tác sticky và không tràn ngang. | CHƯA CHẠY: cần kiểm tra viewport mobile. | CHƯA CHẠY |

Các kết quả tự động ở trên được đối chiếu với `dotnet-test-output.txt` và `vitest-output.txt`. Các bước cần đăng nhập, kiểm tra Network, toast hoặc viewport thực tế chưa được đánh dấu hoàn tất.