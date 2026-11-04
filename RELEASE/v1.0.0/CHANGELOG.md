# Nhật Ký Thay Đổi Phiên Bản (Changelog - Release v1.0.0)

Tất cả các thay đổi đáng chú ý trong bản phát hành chính thức **v1.0.0** của hệ thống **Quản lý sinh viên thực tập** (ASP.NET Core 8 + Vue 3 + SQL Server) được ghi nhận chi tiết tại đây theo chuẩn [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) và tuân thủ định danh phiên bản ngữ nghĩa [Semantic Versioning (SemVer 2.0.0)](https://semver.org/).

> [!NOTE]
> Bản phát hành v1.0.0 này kế thừa, hoàn thiện và đóng gói toàn bộ thành quả từ các đợt phát triển trước:
> - Đợt tái cấu trúc dịch vụ tiếp nhận thực tập: Xem chi tiết tại [RELEASE/CHANGELOG.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/CHANGELOG.md).
> - Đợt xử lý phản hồi người dùng UAT Đợt 2: Xem chi tiết tại [RELEASE/uat-round/CHANGELOG-UAT.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/uat-round/CHANGELOG-UAT.md).

---

## [1.0.0] - 2026-11-04

### Đã thêm (Added)
- **Cơ chế Triển khai & Kiểm thử Môi trường Sạch Tự động**:
  - `scripts/release/prepare-clean-env.ps1`: Tự động trích xuất mã nguồn sạch từ commit hiện tại (loại bỏ hoàn toàn `bin`, `obj`, `node_modules`, `dist`, `.env`), tạo cơ sở dữ liệu demo biệt lập `StudentInternshipMgmt_ReleaseDemo_<timestamp>`, cài đặt dependency khóa theo lockfile (`dotnet restore`, `npm ci`), áp dụng EF Core migrations và seed dữ liệu mẫu, khởi chạy backend và frontend ở chế độ nền trên cổng trống động, thực hiện polling health check và dọn dẹp an toàn khi kết thúc.
  - `scripts/release/run-uat-scenarios.ps1`: Bộ 10 kịch bản UAT nâng cao chạy tự động trên môi trường thật, bao phủ đầy đủ các luồng nghiệp vụ: đăng nhập đa vai trò, đổi mật khẩu lần đầu, quản lý sinh viên/doanh nghiệp, đăng tuyển dụng, nộp hồ sơ, xử lý phỏng vấn, phê duyệt tiếp nhận và hủy tiếp nhận, tạo tài khoản hàng loạt, xuất dữ liệu CSV, kiểm soát đồng thời (race condition) và phân quyền Multi-Tenancy.
  - `scripts/release/run-release.ps1`: Điểm vào duy nhất (Single Entry Point) điều phối toàn bộ quy trình phát hành từ môi trường sạch, chạy test, UAT, đánh giá chéo, đóng gói, kiểm thử gói độc lập và thu thập bằng chứng.
- **Bộ Kiểm thử Đơn vị & Đặc tả Mở rộng**:
  - 403 ca kiểm thử tự động backend (402 Passed, 1 Skipped theo thiết kế đối với bộ lọc dấu tiếng Việt).
  - 194 ca kiểm thử đơn vị frontend (17 test suites với Vitest).
  - Bổ sung bộ kiểm thử đặc tả `PlacementAcceptanceCharacterizationTests` (22 tests) bao phủ 100% các nhánh logic của dịch vụ tiếp nhận và hủy tiếp nhận thực tập.
  - Bổ sung kiểm thử race condition nộp đơn song song `CreateRequestAsync_double_submission_creates_only_one_request_and_rejects_second`.
  - Bổ sung 4 ca kiểm thử kiểm chứng vòng đời cờ trạng thái `saving` trong `JobPositionApplyView.test.js`.
- **Hạ tầng Tái lập & Kiểm tra Tính toàn vẹn**:
  - Tệp `VERSION` làm nguồn duy nhất (Single Source of Truth), đồng bộ tự động với `Directory.Build.props` và `frontend/package.json`.
  - Tệp băm kiểm tra toàn vẹn `checksums.sha256` cho toàn bộ gói phát hành.
  - Kịch bản xác minh gói phát hành trong thư mục rỗng biệt lập `package-verification.log`.

### Đã sửa (Fixed)
- **FB-10 / TC-12 (P2 - Race Condition)**: Khắc phục triệt để lỗi tạo đơn ứng tuyển trùng lặp khi người dùng click đúp nút nộp đơn hoặc gửi request song song:
  - Backend: Bọc luồng nộp đơn trong Database Transaction và áp dụng Pessimistic Row Lock trên bảng `Students` (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = @studentId`).
  - Frontend: Vô hiệu hóa nút nộp đơn (`saving = true`) ngay khi sự kiện click phát sinh.
- **CR-03 (P3 - Trạng thái Nút bấm)**: Tái cấu trúc hàm `submit()` trong `JobPositionApplyView.vue` với khối `try...finally` toàn diện, đảm bảo cờ `saving` luôn được giải phóng về `false` trong mọi tình huống (kể cả khi validation client ném lỗi).
- **FB-01 (P3 - Bản địa hóa Tiếng Việt)**: Chuẩn hóa 100% các thông báo xác thực lỗi của module sinh viên sang tiếng Việt tự nhiên chuẩn mực; loại bỏ hoàn toàn các chuỗi lai tạp nửa tiếng Anh như `FullName là bắt buộc`, `PhoneNumber là bắt buộc`, `StudentCode là bắt buộc`.
- **FB-02 (P3 - Giao diện Thân thiện)**: Loại bỏ các mã định danh biến kỹ thuật `(Title)` và `(Quantity)` khỏi thông báo lỗi của màn hình đăng tin tuyển dụng ở cả backend và frontend.
- **FB-03 (P3 - Cắt tỉa Liên kết CV)**: Tự động cắt tỉa (trim) khoảng trắng ở 2 đầu liên kết CV (`CvUrl`) trước khi lưu trữ vào CSDL; thiết lập cơ chế phòng thủ 3 tầng chặn đứng scheme nguy hiểm (`javascript:`, `ftp:`) và URL chứa khoảng trắng ở giữa.
- **FB-04 (P4 - Lỗi font Mojibake)**: Khắc phục lỗi mã hóa ký tự dòng cuối cùng của tệp `InternshipManagement/README.md` sang định dạng UTF-8 chuẩn (*"Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3)."*).
- **DOC-01 (P3 - Tài liệu Tái lập)**: Bổ sung chỉ dẫn cài đặt dependency bắt buộc `npm install` / `npm ci` trên môi trường clone sạch vào tài liệu tái lập.

### Đã thay đổi (Changed)
- Tái cấu trúc toàn diện `PlacementAcceptanceService.cs`:
  - Khai trừ toàn bộ chuỗi ma thuật (magic strings) và số cứng thành các hằng số rõ nghĩa.
  - Phân tách phương thức lớn `AcceptAsync` (từ 228 dòng xuống 77 dòng) và `CancelAcceptedAsync` (từ 186 dòng xuống 75 dòng) thành các phương thức nghiệp vụ đơn trách nhiệm (SRP).
  - Tách biệt hoàn toàn logic quản lý Transaction và câu lệnh khóa dòng SQL Server quan hệ sang các hàm dùng chung `ExecuteInTransactionAsync`, `LockRelationalRowsForAcceptanceAsync`, `LockRelationalRowsForCancellationAsync`.
- Tách biệt cấu hình môi trường chạy demo thông qua biến môi trường tiến trình (`ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`), tuyệt đối không sửa đổi các tệp cấu hình đã commit (`appsettings.json`).

### Các vấn đề đã biết & Hạn chế (Known issues)
- **KI-01 (P4)**: Phương thức `StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters` được đánh dấu tạm bỏ qua (`[Skip]`) theo thiết kế vì phụ thuộc vào collation Accent-Insensitive đặc thù của máy chủ cơ sở dữ liệu.
- **KI-02 (P4)**: Cơ chế gửi email thông báo sau khi duyệt đơn hiện chạy trực tiếp sau khi commit transaction thay vì dùng bảng Outbox pattern. Trong trường hợp tiến trình bị tắt đột ngột đúng thời điểm giữa commit và gửi mail, email thông báo có thể bị trễ.
- **KI-03 (P4)**: Khi chuyển giao cho người dùng cuối kiểm thử thực tế tại các đơn vị trường học và doanh nghiệp, cần quản trị viên hỗ trợ giải thích quy trình đổi mật khẩu lần đầu bắt buộc (`MustChangePassword`) để tránh nhầm lẫn về phân quyền.
