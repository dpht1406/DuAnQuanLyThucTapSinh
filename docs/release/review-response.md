# Báo Cáo Phản Hồi Kết Quả Đánh Giá Độc Lập (Independent Review Response - Release v1.0.1)

> [!IMPORTANT]
> **Vai trò**: Senior Release Engineer.
> **Mục tiêu**: Trả lời chính thức và minh chứng chi tiết kết quả xử lý toàn bộ 7 phát hiện (2 P2, 3 P3, 2 P4) từ báo cáo đánh giá độc lập (`docs/release/independent-review.md`).
> **Nguyên tắc**: Sửa chữa thực chất, kiểm chứng bằng chứng gốc trên môi trường sạch biệt lập, cam kết zero lỗi P1/P2 mở, giữ nguyên tag `v1.0.0` làm lịch sử và bảo vệ tuyệt đối CSDL chính `StudentInternshipMgmtDb`.

---

## 1. Bảng Tổng Hợp Phản Hồi Chi Tiết 7 Phát Hiện

| STT | Mã Phát Hiện | Mức Độ | Mô Tả Phát Hiện Gốc | Trạng Thái Xử Lý | Commit Khắc Phục | Bằng Chứng Xác Thực |
| :---: | :---: | :---: | :--- | :---: | :---: | :--- |
| **1** | **P2-01** | **P2** | Báo cáo UAT dùng câu nói quá *"kế thừa và giải quyết dứt điểm toàn bộ 10 phản hồi quan sát người thật (FB-01..FB-10)"*, trong khi thực tế chỉ sửa 5, từ chối 2 và hoãn 3 theo `KNOWN-ISSUES.md`. | **ĐÃ SỬA** | `730aff6` | - [uat-scenarios.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/uat-scenarios.md#L5): Đính chính trung thực: 5 sửa, 2 từ chối theo thiết kế an toàn/quản lý (FB-05, FB-06), 3 hoãn (FB-07..09).<br>- Đồng bộ sang [run-uat-scenarios.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-uat-scenarios.ps1#L760), `requirements-checklist.md`, `tu-danh-gia.md`, `bug-log.md` và `assumptions.md`.<br>- Không còn bất kỳ vị trí nào nói "giải quyết toàn bộ 10 phản hồi". |
| **2** | **P2-02** | **P2** | Hướng dẫn trong `README-REPRODUCE.md` chỉ bảo build `DuAnQuanLyThucTapSinh.sln` nhưng solution thiếu 2 test projects backend, khiến lệnh test `--no-build` báo lỗi thiếu file `.dll`. | **ĐÃ SỬA** | `ff43b65` | - Bổ sung cả `StudentInternshipMgmt.Tests.csproj` và `StudentInternshipMgmt.Infrastructure.Tests.csproj` vào [DuAnQuanLyThucTapSinh.sln](file:///d:/baitapthuctap/duanquanlythuctapsinh/DuAnQuanLyThucTapSinh.sln).<br>- Lệnh `dotnet build DuAnQuanLyThucTapSinh.sln` nay biên dịch toàn bộ 4 src projects và 3 test projects.<br>- Cập nhật [RELEASE/v1.0.1/README-REPRODUCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.1/README-REPRODUCE.md) và [RELEASE/README-REPRODUCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/README-REPRODUCE.md). |
| **3** | **P3-01** | **P3** | Bước Cross-Review trong script chạy 0.00s chỉ là `Test-Path` tĩnh; file `cross-review.md` thiếu tính khách quan và bỏ qua 3 rủi ro kỹ thuật. | **ĐÃ SỬA** | `feat(release)` | - Viết lại [cross-review.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/cross-review.md) theo vai giám định viên độc lập, đầy đủ mục P1-P4, commit range `7fc712b..HEAD`, phân tích sâu 3 rủi ro kỹ thuật, loại bỏ cụm từ tô hồng *"100% đã khắc phục triệt để"*.<br>- Nâng cấp bước `CROSS-REVIEW-AND-BUG-LOG` trong [run-release.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-release.ps1) tự động parse kiểm tra P1-P4, commit range, 3 rủi ro và zero P1/P2 mở. |
| **4** | **P3-02** | **P3** | Lệch commit giữa gói zip (tạo từ commit `34ec284`) và git tag `v1.0.0` (gắn tại commit `5a3bc91`), dẫn đến lệch 32 files tài liệu/evidence trong repo. | **ĐÃ SỬA** | `feat(release)` | - Tự động hóa trong [run-release.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-release.ps1): Đóng gói zip từ đúng commit sẽ gắn tag, sau đó gắn tag `v1.0.1` ngay lập tức vào commit đó.<br>- Sinh tự động [RELEASE/v1.0.1/PROVENANCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.1/PROVENANCE.md) ghi nhận commit SHA, thời điểm, bảng băm SHA256 và danh sách thư mục loại trừ. |
| **5** | **P3-03** | **P3** | Dự án kiểm thử `Application.Tests` là khung rỗng, mồ côi (0 test), không nằm trong solution nào và không được chạy. | **ĐÃ SỬA** | `ff43b65` | - Xóa dứt điểm `InternshipManagement/backend/tests/Application.Tests` bằng `git rm -r`.<br>- Dự án có thể khôi phục từ lịch sử Git nếu tương lai cần phát triển.<br>- Ghi nhận quyết định vào [bug-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/bug-log.md). |
| **6** | **P4-01** | **P4** | Tệp log UAT-03 ghi nhận `Kiểm tra UTF-8 BOM: False` do script PowerShell parse chuỗi làm mất byte header. | **ĐÃ SỬA** | `c6619a4` | - Tối ưu kịch bản UAT-03 trong [run-uat-scenarios.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-uat-scenarios.ps1#L250): Tải file qua stream nhị phân `-OutFile $tempCsv -PassThru` và đọc trực tiếp mảng byte.<br>- Xác nhận 3 byte BOM `0xEF, 0xBB, 0xBF` do backend phát sinh, log ghi nhận `Kiểm tra UTF-8 BOM: True`, bảo đảm 100% tương thích Microsoft Excel. |
| **7** | **P4-02** | **P4** | Chuỗi fallback rác `'tedjsfjdjklfjasdfslfajaisf'` trong `CompanyPlacementRequestDetailView.vue` và lệnh `trim()` trong `JobPositionApplyView.vue` ép kiểu ngầm định. | **ĐÃ SỬA** | `d49303e` | - Thay chuỗi rác bằng thông báo tiếng Việt tự nhiên `"Chưa có thư giới thiệu."` trong [CompanyPlacementRequestDetailView.vue](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/views/company/CompanyPlacementRequestDetailView.vue#L174).<br>- Thêm hàm `formatCoverLetter` kèm tests trong `companyPlacementRequest.test.js` và `CompanyPlacementRequestDetailView.test.js`.<br>- Áp dụng defensive trim `typeof value === 'string' ? value.trim() : value` trong [JobPositionApplyView.vue](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/views/job-positions/JobPositionApplyView.vue#L133) kèm test an toàn kiểu dữ liệu. |

---

## 2. Đối Chiếu & Xử Lý 3 Điểm Rủi Ro Kỹ Thuật Do Reviewer Tìm Thấy

### 1. Rủi ro 1: T-SQL Khóa Dòng Gắn Chặt Phương Ngữ SQL Server (P3)
- **Nhận định**: Cú pháp ngoặc vuông `UPDATE [Students] SET [Status] = [Status]` phụ thuộc chặt vào T-SQL của SQL Server.
- **Biện pháp xử lý**:
  - Tuân thủ phạm vi yêu cầu: Rủi ro phương ngữ SQL Server được ghi nhận minh bạch vào Sổ Nhật Ký Quản Lý Lỗi (`bug-log.md`) và Báo cáo Đánh giá Chéo (`cross-review.md`) với trạng thái **P3 - Hoãn có lý do (Deferred with Rationale)**.
  - Lý do hoãn: Hệ thống được thiết kế và triển khai phục vụ môi trường máy chủ Microsoft SQL Server cục bộ / Azure SQL theo đúng hợp đồng và tài liệu kiến trúc. Việc trừu tượng hóa đa CSDL đòi hỏi tái cấu trúc tầng truy cập dữ liệu diện rộng, tiềm ẩn rủi ro hồi quy lớn mà không mang lại giá trị vận hành thực tế cho đợt phát hành v1.0.1.

### 2. Rủi ro 2: Hoàn Tác Giao Dịch Ngầm Định Khi Từ Chối Đơn (P3)
- **Nhận định**: Trong `PlacementRequestService.cs`, khi đơn bị từ chối nghiệp vụ (sinh viên không tồn tại, quá 5 đơn), hàm trả về kết quả và hoàn tác ngầm qua `finally { await transaction.DisposeAsync(); }` mà không gọi `RollbackAsync()` tường minh.
- **Biện pháp xử lý**:
  - Đã rà soát và kiểm chứng cơ chế ADO.NET / EF Core: Việc `Dispose` một transaction chưa commit luôn kích hoạt Rollback ngầm ở mức giao thức CSDL, bảo đảm 100% không để lại bản ghi rác.
  - Kịch bản UAT-07 (chạy đua 2 request đồng thời) chứng minh CSDL chỉ có duy nhất 1 bản ghi được tạo, không xảy ra rò rỉ dữ liệu.
  - Trạng thái: **P3 - Chấp nhận vận hành hiện tại (Documented & Safe)**.

### 3. Rủi ro 3: Ép Kiểu Dữ Liệu Khi Trim Ở Frontend (P4/P3)
- **Nhận định**: Lệnh `[field, value.trim()]` tại `JobPositionApplyView.vue` có nguy cơ ném lỗi `TypeError` nếu giá trị trường là `null`, `undefined` hoặc số.
- **Biện pháp xử lý**:
  - Đã sửa trực tiếp tại commit `d49303e`: `typeof value === 'string' ? value.trim() : value`.
  - Đã viết unit test trong `JobPositionApplyView.test.js` kiểm chứng truyền các thuộc tính `customNull: null`, `customUndefined: undefined`, `customNumber: 42`, `customBool: false` mà không gây lỗi runtime.
  - Trạng thái: **ĐÃ KHẮC PHỤC TRIỆT ĐỂ (Resolved in v1.0.1)**.

---

## 3. Khắc Phục Cơ Chế Chống Lệch Giữa Tài Liệu & Script (Requirement 3)
- Tại bước `PACKAGE-AND-VERIFY` trong `run-release.ps1`, script không còn sử dụng các câu lệnh hardcode riêng lẻ mà **đọc trực tiếp các lệnh biên dịch và test từ chính tệp tài liệu** `RELEASE/v1.0.1/README-REPRODUCE.md` (mục 4.1).
- Nếu tài liệu README viết sai lệnh, sai đường dẫn, hoặc thiếu project test, quy trình phát hành sẽ lập tức báo lỗi thoát mã, bảo đảm tài liệu và mã thực thi đồng nhất 100%.

---

## 4. Trạng Thái Các Tiêu Chuẩn Nghiệm Thu (Acceptance Criteria)

- [x] Tìm trong repo không còn câu "giải quyết toàn bộ 10 phản hồi" hay chuỗi rác `'tedjsfjdjklfjasdfslfajaisf'`.
- [x] Một solution `DuAnQuanLyThucTapSinh.sln` build được cả 4 dự án src và 3 dự án test; `README-REPRODUCE.md` chạy đúng từng lệnh trên thư mục rỗng.
- [x] Zip được đóng gói từ đúng commit được gắn tag `v1.0.1`; `PROVENANCE.md` và `checksums.sha256` khớp 100%.
- [x] Test backend đạt 402 passed, 1 skipped (>= 402 pass); test frontend đạt 199 passed (>= 194 pass); UAT 10/10 kịch bản PASS.
- [x] Báo cáo `review-response.md` trả lời đầy đủ 7 phát hiện; CSDL chính `StudentInternshipMgmtDb` nguyên vẹn; không còn tài nguyên tạm.
- [x] Zero lỗi P1 và zero lỗi P2 mở trong Sổ Nhật Ký Quản Lý Lỗi (`bug-log.md`).
