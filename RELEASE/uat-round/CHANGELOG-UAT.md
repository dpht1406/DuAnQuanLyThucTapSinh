# Nhật ký Thay đổi Phiên bản UAT (CHANGELOG - UAT Round)

Tất cả các thay đổi đáng chú ý trong đợt phát hành UAT Sprint 2 được ghi nhận tại đây theo chuẩn [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [1.2.0-uat] - 2026-11-03

### Đã sửa (Fixed)
- **FB-04 (P4)**: Khắc phục lỗi mã hóa (mojibake) dòng cuối cùng trong file `InternshipManagement/README.md` sang chuẩn UTF-8 (*"Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3)."*).
- **FB-01 (P3)**: Chuẩn hóa toàn bộ thông báo xác thực lỗi thêm/sửa sinh viên sang tiếng Việt tự nhiên tại FluentValidation và StudentService; loại bỏ hoàn toàn các chuỗi nửa tiếng Anh như `FullName là bắt buộc`, `PhoneNumber là bắt buộc`, `StudentCode là bắt buộc`.
- **FB-02 (P3)**: Loại bỏ các mã định danh kỹ thuật `(Title)` và `(Quantity)` khỏi thông báo lỗi xác thực vị trí tuyển dụng ở cả frontend và backend.
- **FB-03 (P3)**: Hỗ trợ tự động cắt (trim) khoảng trắng ở hai đầu liên kết CV (`CvUrl`), backend lưu trữ URL đã cắt sạch sẽ vào cơ sở dữ liệu. Đồng thời siết chặt kiểm tra an toàn: từ chối các đường dẫn chứa khoảng trắng ở giữa, URL tương đối và scheme nguy hại (`javascript:`).
- **FB-10 (P2)**: Khắc phục triệt để lỗi Race Condition (phát hiện qua ca kiểm thử TC-12) khi người dùng bấm nút nộp đơn hai lần liên tiếp (double click) hoặc gửi request song song:
  - Backend: Bọc trong Database Transaction và áp dụng Pessimistic Row Lock trên bảng `Students` (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}`).
  - Frontend: Vô hiệu hóa nút bấm (`saving = true`) ngay tại dòng đầu tiên khi người dùng click nộp đơn.

### Đã thêm (Added)
- Bộ kiểm thử đơn vị và tích hợp mới:
  - `CreateRequestAsync_double_submission_creates_only_one_request_and_rejects_second` trong `PlacementRequestServiceTests.cs`.
  - Kiểm thử thông báo tiếng Việt chuẩn trong `StudentValidatorTests.cs`.
  - Các ca kiểm thử biên và kiểm thử từ chối URL CV trong `CreatePlacementRequestDtoValidatorTests.cs` và `applicationValidation.test.js`.
- Kịch bản tự động thu thập bằng chứng Before/After độc lập: `capture-before-evidence.ps1` và `capture-after-evidence.ps1`.
- Biểu mẫu UAT tiêu chuẩn: `feedback-form.md` và `uat-scenarios-sheet.md`.

### Đã kiểm tra và Xác nhận (Verified)
- Ca kiểm thử TC-13 (XSS injection vào CoverLetter): Đạt (Vue 3 template interpolation tự động escape, backend lưu an toàn dạng text).
- Ca kiểm thử TC-14 (Giao diện mobile 375px): Đạt (Responsive layout co giãn chuẩn xác trên màn hình nhỏ).
