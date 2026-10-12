# KPI và quy tắc dữ liệu

Tài liệu này được đối chiếu với `DashboardService`, `StudentService`,
`StudentsController`, `EmailAddressValidator` và `SpreadsheetSafety`.

## KPI dashboard

- **Tỷ lệ hoàn thành** = số sinh viên có trạng thái `Completed` / tổng số sinh
  viên chưa xóa mềm × 100, làm tròn 2 chữ số thập phân. Tổng bằng 0 thì tỷ lệ
  bằng 0. Truy vấn dùng global query filter nên không tính sinh viên đã xóa mềm.
- **Danh sách cần gán doanh nghiệp** gồm sinh viên trạng thái `NoCompany`, có
  tài khoản người dùng, tài khoản được tạo từ `PlacementReminderDays` ngày trở
  lên và không có đơn `Pending`. Nếu cấu hình không có giá trị, hệ thống dùng
  mặc định 5 ngày. Danh sách được sắp xếp theo thời điểm tạo tài khoản tăng dần.

## Nhập sinh viên

- Chỉ chấp nhận `.csv` và `.xlsx`; file phải có đủ sáu cột:
  `StudentCode`, `FullName`, `Email`, `PhoneNumber`, `Major`, `ClassName`.
- Giới hạn mặc định: 5 MiB (`Import:MaxFileSizeBytes = 5242880`) và 5.000 dòng
  dữ liệu (`Import:MaxRows = 5000`). File vượt kích thước bị trả HTTP 413; import
  vượt số dòng bị từ chối toàn bộ, không ghi dòng nào.
- Email được kiểm tra bằng `EmailAddressValidator`; thông báo lỗi lấy từ
  validator dùng chung.
- Số điện thoại phải khớp `^0\d{9}$`.
- `StudentCode` không được trùng với mã đã có hoặc mã xuất hiện trước đó trong
  cùng file; so sánh không phân biệt hoa thường.
- Các lỗi dữ liệu theo dòng được bỏ qua, các dòng hợp lệ vẫn được nhập; lỗi có
  số dòng CSV/Excel tương ứng. File sai định dạng, không đọc được hoặc CSV thiếu
  cột trả lỗi đọc file ở dòng 0.

## Xuất sinh viên

- Hỗ trợ `csv` và `xlsx`, gồm 10 cột: MSSV, Họ tên, Lớp, Ngành, Email, Số điện
  thoại, Trạng thái, Doanh nghiệp, Vị trí, Ngày tạo tài khoản.
- Áp dụng cùng bộ lọc tìm kiếm, trạng thái và doanh nghiệp; không áp dụng phân
  trang. Global query filter loại sinh viên đã xóa mềm.
- Trước khi ghi CSV/XLSX, giá trị bắt đầu bằng `=`, `+`, `-`, `@`, tab hoặc
  carriage return được bảo vệ bằng dấu nháy đơn (`'`) để giảm rủi ro formula
  injection. CSV còn escape dấu phẩy, dấu nháy kép và xuống dòng theo quy tắc
  CSV.
