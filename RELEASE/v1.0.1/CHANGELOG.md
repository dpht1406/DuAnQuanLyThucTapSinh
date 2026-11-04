# Nhật Ký Thay Đổi Phiên Bản (Changelog - Release v1.0.1)

Tất cả các thay đổi đáng chú ý trong bản phát hành vá lỗi và tái phát hành **v1.0.1** của hệ thống **Quản lý sinh viên thực tập** (ASP.NET Core 8 + Vue 3 + SQL Server) được ghi nhận chi tiết tại đây theo chuẩn [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) và tuân thủ định danh phiên bản ngữ nghĩa [Semantic Versioning (SemVer 2.0.0)](https://semver.org/).

> [!NOTE]
> Bản phát hành v1.0.1 là đợt phát hành lại chính thức nhằm khắc phục toàn diện 7 phát hiện từ báo cáo giám định độc lập (`docs/release/independent-review.md`).
> Toàn bộ lịch sử và tài liệu của bản phát hành v1.0.0 được lưu giữ nguyên vẹn tại [RELEASE/v1.0.0/](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/).

---

## [1.0.1] - 2026-11-04

### Đã sửa (Fixed)
- **P2-01 (Đính chính thông tin nghiệm thu UAT FB-01..FB-10)**: 
  - Khắc phục câu nói quá trong tài liệu `docs/release/uat-scenarios.md` và script `scripts/release/run-uat-scenarios.ps1`.
  - Phản ánh trung thực theo `KNOWN-ISSUES.md`: Kế thừa 10 phản hồi quan sát người thật, trong đó khắc phục 5 vấn đề bằng mã nguồn (FB-01..04, FB-10), từ chối 2 yêu cầu theo thiết kế an toàn/quản lý (FB-05, FB-06), và hoãn 3 yêu cầu mở rộng sang các giai đoạn sau (FB-07, FB-08, FB-09). Đồng bộ sang `tu-danh-gia.md`, `requirements-checklist.md`, `bug-log.md` và `assumptions.md`.
- **P2-02 (Giải pháp biên dịch toàn diện một Solution)**:
  - Bổ sung cả 2 dự án kiểm thử `StudentInternshipMgmt.Tests` và `StudentInternshipMgmt.Infrastructure.Tests` vào file giải pháp chính `DuAnQuanLyThucTapSinh.sln`.
  - Giờ đây, chỉ cần 1 lệnh `dotnet build DuAnQuanLyThucTapSinh.sln` là biên dịch toàn bộ mã nguồn backend và toàn bộ 3 dự án test (403 tests). Cập nhật tài liệu tái lập `README-REPRODUCE.md` để các lệnh `dotnet test ... --no-build` thực thi chính xác 100%.
- **P3-01 (Nâng cấp Cross-Review & Phân tích 3 rủi ro kỹ thuật)**:
  - Viết lại báo cáo `docs/release/cross-review.md` theo vai giám định viên độc lập với góc nhìn khách quan, loại bỏ cụm từ tô hồng "100% đã khắc phục triệt để".
  - Phân tích sâu 3 rủi ro kỹ thuật: (1) Ràng buộc phương ngữ SQL Server của T-SQL Row Lock; (2) Cơ chế hoàn tác giao dịch ngầm định khi từ chối đơn qua `DisposeAsync`; (3) An toàn kiểu dữ liệu khi cắt tỉa chuỗi tại client.
  - Nâng cấp bước `CROSS-REVIEW-AND-BUG-LOG` trong `scripts/release/run-release.ps1` từ kiểm tra `Test-Path` tĩnh sang bộ phân tích tự động kiểm tra đủ mục P1-P4, commit range đã đọc, đối chiếu 3 rủi ro và xác nhận zero P1/P2 mở.
- **P3-02 (Đồng bộ Commit gói phát hành và Tag Git v1.0.1)**:
  - Tự động hóa quy trình đóng gói: Gói zip phát hành được nén từ đúng commit sẽ được gắn tag `v1.0.1`, sau đó commit đó được gắn tag ngay lập tức.
  - Tự động xuất tệp nguồn gốc và nhật ký gói `RELEASE/v1.0.1/PROVENANCE.md` ghi nhận commit SHA, thời điểm phát hành, danh sách thư mục loại trừ và bảng băm SHA256.
- **P3-03 (Dọn dẹp dự án rỗng Application.Tests)**:
  - Xóa bỏ dự án mồ côi `InternshipManagement/backend/tests/Application.Tests` (không có test thực thi) bằng `git rm` và ghi nhận lịch sử vào `bug-log.md`.
- **P4-01 (Kiểm chứng UTF-8 BOM nhị phân trong UAT-03)**:
  - Tối ưu kịch bản UAT-03 trong `scripts/release/run-uat-scenarios.ps1` tải file qua stream nhị phân độc lập (`-OutFile ... -PassThru`) thay vì parse chuỗi.
  - Xác nhận trực tiếp 3 byte preamble `0xEF, 0xBB, 0xBF` do backend phát sinh, ghi nhận `Kiểm tra UTF-8 BOM: True`, bảo đảm 100% tương thích font tiếng Việt khi mở bằng Microsoft Excel.
- **P4-02 (Chuỗi hiển thị thân thiện & Phòng thủ kiểu dữ liệu Frontend)**:
  - Thay thế chuỗi fallback rác trong `CompanyPlacementRequestDetailView.vue` bằng thông điệp tiếng Việt tự nhiên chuẩn mực: `"Chưa có thư giới thiệu."`.
  - Bổ sung hàm tiện ích `formatCoverLetter` kèm unit test trong `companyPlacementRequest.test.js` và `CompanyPlacementRequestDetailView.test.js`.
  - Áp dụng kiểm tra kiểu phòng thủ tại `JobPositionApplyView.vue`: `typeof value === 'string' ? value.trim() : value`, bảo vệ form ứng tuyển không bị crash khi xuất hiện dữ liệu null, undefined, số hoặc boolean; kèm unit test xác minh trong `JobPositionApplyView.test.js`.

### Đã thay đổi (Changed)
- Đồng bộ phiên bản toàn hệ thống lên `1.0.1` (`VERSION`, `Directory.Build.props`, `frontend/package.json`).
- Quy trình kiểm thử tính tái lập của gói phát hành (`PACKAGE-AND-VERIFY`) trong `run-release.ps1` chuyển sang cơ chế điều khiển theo tài liệu (Doc-driven execution): Script tự động trích xuất và thực thi chính xác từng câu lệnh được viết trong `RELEASE/v1.0.1/README-REPRODUCE.md`, bảo đảm tính đồng bộ tuyệt đối giữa tài liệu và mã thực thi.

### Các vấn đề đã biết & Hoãn có lý do (Known Issues & Deferred)
- **P3-DIALECT (Hoãn có lý do)**: Cú pháp khóa dòng bi quan `UPDATE [Students] SET [Status] = [Status]` sử dụng dấu ngoặc vuông đặc thù của Microsoft SQL Server. Do sản phẩm nhắm mục tiêu vận hành trên SQL Server theo hợp đồng và thiết kế hiện tại, việc trừu tượng hóa đa phương ngữ CSDL được hoãn lại cho các phiên bản sau để tránh thay đổi kiến trúc và rủi ro hồi quy.
