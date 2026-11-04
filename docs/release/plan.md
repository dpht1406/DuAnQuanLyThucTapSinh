# Kế hoạch Thực hiện Phát hành Release v1.0.0 (Release Plan)

## 1. Mục tiêu
Thực hiện toàn bộ quy trình phát hành bản Release v1.0.0 cho dự án "Quản lý sinh viên thực tập" (ASP.NET Core 8 + Vue 3 + SQL Server) ở mức nâng cao, chạy tự động một mạch từ môi trường sạch:
- Không can thiệp thủ công giữa chừng, tự cập nhật heartbeat `.agent/release-heartbeat.txt`.
- Đảm bảo an toàn tuyệt đối cho cơ sở dữ liệu gốc `StudentInternshipMgmtDb`.
- Chứng minh hệ thống hoạt động chính xác trên môi trường demo sạch biệt lập.
- Thực hiện cross-review, 10 kịch bản UAT tự động hóa, quản lý bug theo mức P1-P4.
- Đóng gói bản phát hành tái lập, tự động nghiệm thu gói trong thư mục rỗng và báo cáo kết quả.

---

## 2. Kết quả Xác minh & Khảo sát Ban đầu
- **Kiến trúc giải pháp**:
  - Backend Solution: `DuAnQuanLyThucTapSinh.sln` (gốc) và `InternshipManagement/backend/InternshipManagement.sln`. Gồm 4 project source (`StudentInternshipMgmt.Domain`, `Application`, `Infrastructure`, `Api`) và 3 test project (`StudentInternshipMgmt.Tests`, `StudentInternshipMgmt.Infrastructure.Tests`, `PlacementAcceptance.Tests`).
  - Frontend: `InternshipManagement/frontend` (Vite 8, Vue 3.5, PrimeVue 4, Vitest 5).
- **Cơ chế Khởi tạo & CSDL**:
  - Dữ liệu seed tập trung tại `Infrastructure/Persistence/Seed/SeedData.cs` được tích hợp thẳng vào Entity Configurations (`HasData`) và áp dụng qua `dotnet ef database update`.
  - Tài khoản Admin mặc định khởi tạo: `admin` / `Admin@123`, cờ `MustChangePassword = true`.
  - Chuỗi kết nối và JWT được nạp qua cấu hình biến môi trường (`ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`).
  - Dịch vụ nền `PlacementRequestExpiryWorker` có thể tinh chỉnh hoặc tắt qua `PlacementRequestExpiry:Enabled`.
- **Mức sàn kiểm thử**:
  - Backend test baseline: Tối thiểu 395 tests (hiện tại 403 tests: 402 passed, 1 skipped theo thiết kế bộ test tìm kiếm dấu tiếng Việt).
  - Frontend Vitest: 194 unit tests.
- **Quy chuẩn bảo mật môi trường sạch**:
  - Tạo CSDL riêng dạng `StudentInternshipMgmt_ReleaseDemo_<timestamp>`, có chốt chặn từ chối nếu tên CSDL là `StudentInternshipMgmtDb`.
  - Cổng API và Frontend được cấp phát động hoặc kiểm tra cổng trống tự động.
  - Dọn dẹp triệt để tiến trình con (PID tree) và CSDL tạm sau khi hoàn tất.

---

## 3. Lộ trình Triển khai Chi tiết (A → F)

### Bước 1: Khởi tạo Nhánh & Đồng bộ Phiên bản (F.1, F.2)
- Tạo nhánh `release/v1.0.0`.
- Tạo file `VERSION` chứa `1.0.0` làm nguồn duy nhất (Single Source of Truth).
- Đồng bộ version vào `InternshipManagement/backend/Directory.Build.props` (`<Version>1.0.0</Version>`) và `InternshipManagement/frontend/package.json` (`"version": "1.0.0"`).
- Cập nhật `.agent/release-heartbeat.txt`.

### Bước 2: Xây dựng Script Môi trường Sạch (A.1 - A.5)
- Viết `scripts/release/prepare-clean-env.ps1`:
  - Xuất mã nguồn sạch (không `bin`, `obj`, `node_modules`, `dist`, `.git`, `.env`) vào thư mục tạm ngoài repo `[System.IO.Path]::GetTempPath()`.
  - Tạo CSDL demo riêng biệt `StudentInternshipMgmt_ReleaseDemo_<timestamp>`.
  - Chốt chặn: Nếu tên CSDL là `StudentInternshipMgmtDb` -> Dừng ngay lập tức.
  - Cài đặt dependency theo lockfile (`dotnet restore`, `npm ci`).
  - Chạy migration & seed vào CSDL demo.
  - Khởi động Backend API và Frontend trên cổng trống trong nền, ghi PID và log file.
  - Polling health check xác nhận sẵn sàng (timeout 90s cho backend, 45s cho frontend).
  - Xuất file mô tả môi trường `clean-env-descriptor.json`.
  - Cơ chế cleanup: Kill process tree bằng PID, drop CSDL demo, dọn thư mục tạm.
- Viết `scripts/release/run-release.ps1` làm điểm vào duy nhất điều phối toàn bộ chu trình A → F.

### Bước 3: Kiểm thử Tự động & Đo lường Tốc độ (B, D.1)
- Thực hiện song song các tác vụ độc lập:
  - Backend tests: `dotnet test --no-build` (yêu cầu >= 395 pass).
  - Frontend tests: `npm test` (Vitest).
  - Frontend accessibility & check scripts: `npm run check:contrast`, `npm run check:labels`.
  - Frontend build: `npm run build`.
- Ghi log thô vào `docs/release/evidence/`.
- Ghi lại mốc thời gian từng bước vào `docs/release/evidence/timing.txt`.

### Bước 4: Chạy Bộ Kịch bản UAT Tự động Hóa (D.2, D.3)
- Chạy tối thiểu 10 kịch bản UAT tự động qua API và giao diện thật:
  1. Đăng nhập các vai trò & Đổi mật khẩu lần đầu bắt buộc (`admin`, doanh nghiệp, sinh viên).
  2. Quản lý sinh viên (tạo mới, validation tiếng Việt thân thiện, xuất dữ liệu CSV/Excel).
  3. Quản lý doanh nghiệp & Tạo tài khoản doanh nghiệp hàng loạt (Bulk Accounts).
  4. Đăng tin vị trí thực tập (loại bỏ token kỹ thuật, validation số lượng/tiêu đề).
  5. Ứng tuyển thực tập & Cắt tỉa (trim) link CV an toàn 3 tầng.
  6. Kiểm soát đồng thời / Race condition: Chặn nộp đơn đúp (rapid double submit) bằng DB lock và transaction.
  7. Doanh nghiệp mời phỏng vấn & Ghi nhận trúng tuyển.
  8. Quản trị viên duyệt / tiếp nhận kết quả (Placement Acceptance) và hủy tiếp nhận (Cancellation).
  9. Kiểm thử phân quyền & Đa người thuê (Multi-Tenancy): Chặn 401 nặc danh, 403 sinh viên vào admin, chặn doanh nghiệp sửa chéo tin của nhau.
  10. Đường dẫn dữ liệu sai: Xác thực lỗi định dạng trả về tiếng Việt rõ ràng, chuẩn mực.
- Ghi nhận đầy đủ nguồn gốc các phản hồi người thật đã có (FB-01 đến FB-10 trong `docs/uat-feedback`).
- Xuất báo cáo UAT chi tiết `docs/release/uat-scenarios.md` kèm logs/ảnh chụp màn hình.

### Bước 5: Đánh giá Chéo (Cross-Review), An ninh & Quản lý Lỗi (D.4, D.5, D.6)
- Rà soát `git diff` độc lập từ baseline đến HEAD.
- Soát lỗi bảo mật nhanh: không hardcode secret, kiểm tra header an toàn, không lộ stack trace.
- Tạo `docs/release/cross-review.md` phân loại P1-P4.
- Tạo `docs/release/bug-log.md`: Đảm bảo 0 lỗi P1, 0 lỗi P2 còn mở.

### Bước 6: Đóng gói Phát hành & Tái lập Nghiệm thu (F.1 - F.7)
- Cập nhật `RELEASE/v1.0.0/CHANGELOG.md` theo chuẩn Keep a Changelog.
- Tạo gói nguồn sạch tái lập `RELEASE/v1.0.0/DuAnQuanLyThucTapSinh-v1.0.0.zip` kèm dữ liệu mẫu.
- Viết `RELEASE/v1.0.0/README-REPRODUCE.md` hướng dẫn chạy 1 lệnh duy nhất.
- Tạo tệp `RELEASE/v1.0.0/checksums.sha256` và danh sách phiên bản công cụ.
- Kiểm tra tính tái lập: Giải nén vào thư mục rỗng tạm thời, chạy script tái lập và ghi log chứng minh thành công.
- Biên soạn Bản tự đánh giá 1 trang `docs/release/tu-danh-gia.md`.
- Lập bảng đối chiếu nghiệm thu `docs/release/requirements-checklist.md`.
- Ghi nhận cờ kết thúc `.agent/release-done.flag` (`PASS`).
- Gắn thẻ git `v1.0.0`.
- Báo cáo tổng kết.
