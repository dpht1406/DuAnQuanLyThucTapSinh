# Báo Cáo Đánh Giá Độc Lập Bản Release v1.0.0 (Independent Release Review)

> [!IMPORTANT]
> **Vai trò**: Giám định viên / Reviewer độc lập (chưa từng tham gia quá trình lập trình và đóng gói).
> **Nguyên tắc**: Tự kiểm chứng bằng chứng gốc từ mã nguồn, git history, database, process runtime và chạy lại toàn bộ quy trình; tuyệt đối không tin tưởng thụ động vào các báo cáo tự đánh giá của đội ngũ phát hành.
> **Phạm vi thẩm định**: Đánh giá toàn diện 7 hạng mục then chốt của bản Release v1.0.0.

---

## 1. Bảng Tổng Hợp Kết Luận 7 Hạng Mục Kiểm Tra

| STT | Hạng Mục Kiểm Tra | Kết Luận | Tóm Tắt Phát Hiện & Bằng Chứng |
| :---: | :--- | :---: | :--- |
| **1** | **Cross-review & Git diff** | **LỆCH** | - Bước `CROSS-REVIEW-AND-BUG-LOG` trong [run-release.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-release.ps1) chỉ là 2 lệnh `Test-Path` giả định (chạy 0.00s).<br>- [cross-review.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/cross-review.md) được viết tĩnh và commit trước đó (commit `f4d3d69`).<br>- Tìm thấy 3 điểm rủi ro kỹ thuật từ `git diff baseline..HEAD` chưa được báo cáo đầy đủ.<br>*(Bằng chứng: [item1-cross-review.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/review/item1-cross-review.txt))* |
| **2** | **Gói Zip so với Tag v1.0.0** | **LỆCH** | - Phiên bản `1.0.0` khớp hoàn toàn ở [VERSION](file:///d:/baitapthuctap/duanquanlythuctapsinh/VERSION), `Directory.Build.props` và `package.json`.<br>- Checksum SHA256 khớp 100% với [checksums.sha256](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/checksums.sha256).<br>- **Lệch commit**: Gói zip được sinh từ commit `34ec284`, trong khi tag `v1.0.0` trỏ vào commit `5a3bc91` (lệch 32 files tài liệu/evidence sinh sau).<br>*(Bằng chứng: [item2-zip-package.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/review/item2-zip-package.txt))* |
| **3** | **Project Test & Số lượng Test** | **ĐÚNG** *(kèm lưu ý)* | - Tổng số test backend chạy thực tế: **402 passed, 1 skipped, 0 failed** (khớp 100% báo cáo).<br>- Test bị skip là `StudentSearchTests` do thiếu biến môi trường `STUDENT_TEST_SQLSERVER` để test collation tiếng Việt trên SQL Server thật.<br>- Dự án [Application.Tests](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/Application.Tests) là khung rỗng (0 test), không nằm trong solution nào và không được chạy.<br>*(Bằng chứng: [item3-test-projects.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/review/item3-test-projects.txt))* |
| **4** | **Kiểm định UAT & Đối chiếu Phản hồi** | **LỆCH** | - Bộ kịch bản UAT-01..UAT-10 có assert thực chất (kiểm tra payload, regex thông báo tiếng Việt, query SQL Server trực tiếp và kiểm soát đa luồng).<br>- **Nói quá / Lệch nghiêm trọng**: Tuyên bố trước đây về việc giải quyết tất cả phản hồi quan sát người thật (FB-01 đến FB-10) không đúng thực tế. Báo cáo [KNOWN-ISSUES.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/uat-round/KNOWN-ISSUES.md) xác nhận: chỉ sửa 5 (FB-01..04, FB-10), **từ chối 2** (FB-05, FB-06) và **hoãn 3** (FB-07, FB-08, FB-09).<br>*(Bằng chứng: [item4-uat-analysis.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/review/item4-uat-analysis.txt))* |
| **5** | **Dọn dẹp Tài nguyên & CSDL Chính** | **ĐÚNG** | - Xác nhận không còn DB demo `StudentInternshipMgmt_ReleaseDemo_*` hay `StudentInternshipMgmt_PackageVerify_*`.<br>- Không có tiến trình mồ côi (dotnet, node, sqlcmd).<br>- Thư mục tạm trong Temp đã được dọn sạch.<br>- Database chính `StudentInternshipMgmtDb` nguyên vẹn (thời điểm cập nhật bảng cuối cùng là ngày 19/10/2026).<br>*(Bằng chứng: [item5-cleanup.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/review/item5-cleanup.txt))* |
| **6** | **Bảo mật nhanh (Secrets & .gitignore)** | **ĐÚNG** | - Không có secret production, mật khẩu thật hoặc khóa JWT môi trường vận hành bị lộ trong repo hoặc gói zip.<br>- Tìm thấy tài khoản seed demo mặc định và các khóa ký JWT mẫu dùng cho test tự động.<br>- Thay đổi mới trong [.gitignore](file:///d:/baitapthuctap/duanquanlythuctapsinh/.gitignore) (`!RELEASE/**/*.zip`) là an toàn, cho phép nộp file zip release mà không làm sót các mã nguồn cần thiết.<br>*(Bằng chứng: [item6-security.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/review/item6-security.txt))* |
| **7** | **Tái lập thật trên Thư mục Mới** | **LỆCH** *(kèm lỗi tài liệu)* | - Đã giải nén gói zip ra thư mục trắng trong Temp và thực hiện quy trình.<br>- Phát hiện lỗi hướng dẫn trong [README-REPRODUCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/README-REPRODUCE.md): chỉ dẫn `dotnet build DuAnQuanLyThucTapSinh.sln` không build 2 project test `StudentInternshipMgmt.Tests` và `StudentInternshipMgmt.Infrastructure.Tests`, dẫn đến lệnh `dotnet test ... --no-build` bị lỗi thiếu dll.<br>- Khi build bổ sung `InternshipManagement.sln`, toàn bộ 402 test backend và 194 test frontend pass 100%. Đã dọn sạch thư mục tạm sau kiểm chứng.<br>*(Bằng chứng: [item7-reproduction.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/review/item7-reproduction.txt))* |

---

## 2. Chi Tiết Thẩm Định Từng Hạng Mục (Mục 1 - 7)

### Mục 1: Cross-review & Phân tích Git Diff (baseline..HEAD)
1. **Hiện tượng 0.00 giây trong script**:
   - Trong tệp [run-release.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-release.ps1) (dòng 146-160), bước `CROSS-REVIEW-AND-BUG-LOG` chỉ thực hiện kiểm tra `Test-Path` sự tồn tại của hai tệp `cross-review.md` và `bug-log.md`. Do hai tệp này đã có sẵn trên ổ đĩa từ trước, thời gian thực thi đo được là `0.00s` (hoặc `0.02s`).
   - Tệp [cross-review.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/cross-review.md) **không phải do script sinh ra tự động**, mà được soạn thảo thủ công và commit từ commit `f4d3d69` (lúc 08:55:52), trước khi bộ test release chạy chính thức (09:22 - 09:26).
2. **Tự đọc `git diff baseline..HEAD` và 3 rủi ro kỹ thuật cao nhất**:
   - **Rủi ro 1 (P2 - T-SQL Dialect Locking Tight-Coupling)**: Trong [PlacementRequestService.cs](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementRequestService.cs#L107-L111) và [PlacementAcceptanceService.cs](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs#L85-L95), việc sử dụng raw SQL cú pháp vuông `[Students] SET [Status] = [Status]` để lock dòng là hoàn toàn phụ thuộc vào SQL Server (T-SQL). Mặc dù có kiểm tra `_db.Database.IsRelational()`, nhưng nếu chạy trên SQLite, PostgreSQL hoặc MySQL, câu lệnh này sẽ gây lỗi cú pháp (syntax error) hoặc deadlock.
   - **Rủi ro 2 (P3 - Xử lý Transaction Abort ngầm định khi Reject)**: Trong `PlacementRequestService.cs`, transaction được mở ngay đầu hàm, nhưng khi gặp các điều kiện nghiệp vụ không hợp lệ (như sinh viên không tồn tại, vượt quá 5 đơn), hàm gọi `return Reject(...)` và nhảy vào khối `finally { await transaction.DisposeAsync(); }` để rollback ngầm định thay vì gọi `RollbackAsync()` tường minh kèm ghi log ngữ cảnh giao dịch như bên `PlacementAcceptanceService`.
   - **Rủi ro 3 (P3 - Giả định kiểu dữ liệu chuỗi khi trim ở Frontend)**: Trong [JobPositionApplyView.vue](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/views/job-positions/JobPositionApplyView.vue#L133-L135), lệnh `Object.fromEntries(Object.entries(form.value).map(([field, value]) => [field, value.trim()]))` giả định toàn bộ các trường trong form đều là kiểu `string`. Nếu xuất hiện trường có giá trị `null`, `undefined` hoặc số, hàm sẽ ném ra ngoại lệ `TypeError: Cannot read properties of null (reading 'trim')` làm crash giao diện người dùng.
3. **So sánh với báo cáo cross-review có sẵn**:
   - Bản báo cáo [cross-review.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/cross-review.md) chỉ liệt kê mặt tích cực (tất cả các mục đều đánh dấu PASS, 100% dứt điểm), không ghi nhận các điểm ràng buộc phương ngữ SQL Server hay rủi ro kiểu dữ liệu ở frontend.

---

### Mục 2: Kiểm tra Gói Zip so với Tag v1.0.0
1. **Kiểm tra thông tin phiên bản trong gói zip** ([DuAnQuanLyThucTapSinh-v1.0.0.zip](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/DuAnQuanLyThucTapSinh-v1.0.0.zip)):
   - Tệp `VERSION`: Chứa đúng `1.0.0`.
   - Tệp `InternshipManagement/backend/Directory.Build.props`: `<Version>1.0.0</Version>`.
   - Tệp `InternshipManagement/frontend/package.json`: `"version": "1.0.0"`.
2. **Kiểm tra Checksum**:
   - Mã băm SHA256 tính toán thực tế của gói zip: `C7516D2E92E3CAD95977120E441C98E2D4F510792E08C704A6626483A61F6CEB`.
   - So với [checksums.sha256](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/checksums.sha256): Khớp chính xác 100%. Các tệp script SQL trong thư mục `data/` cũng khớp hoàn toàn mã băm.
3. **Xác định commit gốc của gói zip**:
   - Gói zip được tạo ra từ lệnh `git archive --format=zip -o $packageZip HEAD` tại commit `34ec284` (`fix(release): format timing report string safely with evaluated expression`).
   - Cây thư mục của zip khớp 689/689 tệp của commit `34ec284` (0 khác biệt).
   - Tag `v1.0.0` được tạo trên commit `5a3bc91` (`chore(release): complete release v1.0.0 artifacts, evidence, and verification logs`), commit này bổ sung thêm 32 tệp (chính là gói zip, file băm, log UAT, tài liệu release sinh ra sau khi đóng gói).

---

### Mục 3: Thẩm Định Dự Án Kiểm Thử & Số Lượng Test
1. **Danh sách các project test**:
   - Thư mục `InternshipManagement/backend/tests`:
     * `Application.Tests` (Dự án khung, rỗng)
     * `StudentInternshipMgmt.Infrastructure.Tests`
     * `StudentInternshipMgmt.Tests`
   - Thư mục gốc `tests`:
     * `PlacementAcceptance.Tests`
2. **Hiện diện trong hai Solution**:
   - [DuAnQuanLyThucTapSinh.sln](file:///d:/baitapthuctap/duanquanlythuctapsinh/DuAnQuanLyThucTapSinh.sln): Chỉ chứa `PlacementAcceptance.Tests` cùng các project `src`. Không chứa `StudentInternshipMgmt.Tests`, `StudentInternshipMgmt.Infrastructure.Tests` hay `Application.Tests`.
   - [InternshipManagement.sln](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/InternshipManagement.sln): Chứa `PlacementAcceptance.Tests`, `StudentInternshipMgmt.Tests`, `StudentInternshipMgmt.Infrastructure.Tests`. Không chứa `Application.Tests`.
   - Dự án `Application.Tests` **hoàn toàn mồ côi**, không nằm trong bất kỳ file solution nào và chứa 0 test thực thi (chỉ có `.gitkeep`). Nó hoàn toàn không được chạy trong quá trình release.
3. **Đối chiếu số lượng test**:
   - `PlacementAcceptance.Tests`: 22 passed.
   - `StudentInternshipMgmt.Tests`: 116 passed.
   - `StudentInternshipMgmt.Infrastructure.Tests`: 264 passed, 1 skipped.
   - Tổng cộng: **402 passed, 1 skipped, 0 failed**. Khớp 100% với báo cáo.
   - Frontend Vitest: **194 passed**, 0 failed.
4. **Lý do thực sự của test bị skip**:
   - Tên test: `StudentInternshipMgmt.Infrastructure.Tests.StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters`.
   - Thuộc tính đánh dấu: `[StudentSqlServerFact]`.
   - **Lý do thực tế**: Test này kiểm thử hành vi tìm kiếm tiếng Việt không dấu / có dấu dựa trên tính năng collation `Vietnamese_CI_AS` và toán tử `LIKE` của SQL Server. Do nhà phát triển không cung cấp chuỗi kết nối qua biến môi trường `STUDENT_TEST_SQLSERVER` trong phiên chạy tự động thông thường (để tránh làm ô nhiễm DB phát triển), bộ kiểm thử xUnit tự động đánh dấu bỏ qua (Skip) với thông báo: *"Set STUDENT_TEST_SQLSERVER to a dedicated SQL Server test database to run this test."*.

---

### Mục 4: Thẩm Định UAT & Đối Chiếu Báo Cáo Phản Hồi
1. **Kiểm tra tính thực chất của các kịch bản UAT (UAT-01 .. UAT-10)**:
   - Các kịch bản UAT **không chỉ kiểm tra mã HTTP status code** mà có thực hiện khẳng định (assert) sâu:
     * UAT-01: Khẳng định cờ `mustChangePassword` đổi từ `true` sang `false`.
     * UAT-02: Khẳng định thông báo lỗi tiếng Việt tự nhiên và không chứa tên trường tiếng Anh.
     * UAT-04: Khẳng định số lượng tài khoản tạo ra trong payload mảng `created` bằng 2.
     * UAT-05: Khẳng định regex không còn mã `(Title)` hay `(Quantity)`.
     * UAT-06: Chạy lệnh `sqlcmd` truy vấn CSDL để khẳng định trường `CvUrl` đã được cắt tỉa khoảng trắng.
     * UAT-07: Chạy 2 Background Job song song kiểm tra race condition, khẳng định đúng 1 request 200, 1 request 400 và `SELECT COUNT(*)` từ CSDL trả về đúng 1 bản ghi.
     * UAT-09: Khẳng định trạng thái sinh viên cập nhật về `Accepted` và hoàn tác về `NoCompany` khi hủy tiếp nhận.
     * UAT-10: Khẳng định phân quyền 401, 403, chống can thiệp chéo tenant và không lộ server stack trace.
   - *Lưu ý nhỏ ở UAT-03*: Tệp log [UAT-03.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/UAT-03.txt) ghi nhận `Kiểm tra UTF-8 BOM: False` do script PowerShell parse chuỗi làm mất header byte, nhưng kết luận vẫn ghi nhận PASS vì kiểm tra HTTP 200 và độ dài chuỗi.
2. **Sai lệch nghiêm trọng về phát biểu xử lý phản hồi FB-01..FB-10**:
   - Tại tệp [uat-scenarios.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/uat-scenarios.md) (dòng 6) và script [run-uat-scenarios.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-uat-scenarios.ps1) (dòng 751), người làm báo cáo tuyên bố:
     > *"Ghi nhận nguồn phản hồi người dùng thực tế: Đợt phát hành Release v1.0.0 kế thừa và xử lý 10 phản hồi quan sát người thật (FB-01 đến FB-10)..."*
   - Tuy nhiên, đối chiếu với [KNOWN-ISSUES.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/uat-round/KNOWN-ISSUES.md) và [01-feedback-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/01-feedback-log.md), thực tế là:
     * **Chỉ có 5 phản hồi được giải quyết bằng mã nguồn**: FB-01 (thông báo tiếng Việt), FB-02 (bỏ tag tiếng Anh), FB-03 (cắt tỉa link CV), FB-04 (sửa font README), FB-10 (chống race condition).
     * **2 phản hồi bị TỪ CHỐI theo thiết kế (Rejected)**: FB-05 (sinh viên đòi nộp không giới hạn đơn - bị từ chối để giữ mốc 5 đơn chống spam); FB-06 (đòi bỏ đổi mật khẩu lần đầu - bị từ chối vì vi phạm chính sách bảo mật).
     * **3 phản hồi bị HOÃN (Deferred)**: FB-07 (SMS Brandname - hoãn vì chi phí viễn thông); FB-08 (giao diện đơn điệu - hoãn vì phản hồi mơ hồ); FB-09 (tra cứu MST tự động - hoãn sang Phase 3 do cần tích hợp API bên thứ ba).
   - **Đánh giá**: Phát biểu cũ nói quá sự thật, gây hiểu lầm rằng toàn bộ yêu cầu của người dùng đều đã được hiện thực hóa trong sản phẩm.

---

### Mục 5: Thẩm Định Dọn Dẹp Môi Trường & An Toàn CSDL Chính
1. **Kiểm tra cơ sở dữ liệu trên SQL Server**:
   - Không tồn tại bất kỳ database tạm nào dạng `StudentInternshipMgmt_ReleaseDemo_*` hoặc `StudentInternshipMgmt_PackageVerify_*`.
   - CSDL chính `StudentInternshipMgmtDb` được bảo vệ tuyệt đối:
     * Số bảng: 10 bảng.
     * Số lượng bản ghi: 134 sinh viên, 19 doanh nghiệp.
     * Thời điểm thay đổi bảng người dùng gần nhất: **19/10/2026 16:04:00**.
     * Hoàn toàn không bị sửa đổi, chèn dữ liệu rác hay drop bảng bởi các script release.
2. **Kiểm tra tiến trình và thư mục tạm**:
   - Tiến trình backend API (PID 34304) và frontend Vite (PID 46332) ghi nhận trong `clean-env-descriptor.json` đã được kill dứt điểm.
   - Không có tiến trình `dotnet` hoặc `sqlcmd` nào chạy ngầm.
   - Thư mục tạm `C:\Users\PHAT\AppData\Local\Temp\StudentInternshipMgmt_ReleaseDemo_20261104_092356` đã được xóa sạch (`Test-Path = False`).

---

### Mục 6: Rà Soát Bảo Mật Nhanh (Secrets & .gitignore)
1. **Rà soát bí mật trong kho lưu trữ và gói zip**:
   - **Tài khoản mặc định / Mật khẩu seed**: Được tài liệu hóa công khai trong README và script test (`admin` / `Admin@123`). Đây là tài khoản demo phục vụ chạy nghiệm thu, bắt buộc đổi mật khẩu (`MustChangePassword = true`) ngay khi đăng nhập.
   - **Khóa ký JWT mẫu**: Các chuỗi khóa JWT dài >= 32 ký tự xuất hiện trong các bài unit test và hướng dẫn chạy cục bộ là dữ liệu mẫu giả lập cho môi trường test.
   - **Khóa ký JWT và mật khẩu SMTP thực tế**: Đều để trống trong [appsettings.json](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Api/appsettings.json) và được nạp an toàn thông qua biến môi trường tiến trình (`$env:Jwt__SecretKey`, `$env:Smtp__Password`).
   - Chuỗi kết nối trong `appsettings.json` sử dụng Windows Integrated Security (`Integrated Security=True`), không lưu trữ mật khẩu DB dạng plain-text.
2. **Thẩm định tệp `.gitignore`**:
   - Thay đổi mới bổ sung `!RELEASE/**/*.zip` vào cuối tệp [.gitignore](file:///d:/baitapthuctap/duanquanlythuctapsinh/.gitignore).
   - Mục đích: Cho phép git theo dõi và lưu trữ gói nén chính thức tại `RELEASE/v1.0.0/DuAnQuanLyThucTapSinh-v1.0.0.zip` trong khi vẫn duy trì quy tắc loại trừ `*.zip` cho các gói zip tạm khác ở thư mục gốc.
   - Không có bất kỳ tệp mã nguồn, file cấu hình hay tài liệu cần thiết nào bị loại trừ ngoài ý muốn.

---

### Mục 7: Tái Lập Thật Trên Thư Mục Trắng
1. **Quá trình thực nghiệm độc lập**:
   - Đã trích xuất gói nén `DuAnQuanLyThucTapSinh-v1.0.0.zip` sang thư mục độc lập mới trong Temp: `C:\Users\PHAT\AppData\Local\Temp\StudentInternshipMgmt_ReviewRepro_e8f205079a81479bb73288a8afdf8327`.
2. **Phát hiện thiếu sót trong tài liệu hướng dẫn [README-REPRODUCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/README-REPRODUCE.md)**:
   - Trong Mục 4.2 của `README-REPRODUCE.md`, tài liệu chỉ dẫn:
     ```powershell
     dotnet restore DuAnQuanLyThucTapSinh.sln
     dotnet build DuAnQuanLyThucTapSinh.sln --no-restore -c Release
     ```
   - Tuy nhiên, file `DuAnQuanLyThucTapSinh.sln` **chỉ chứa 1 project test** (`PlacementAcceptance.Tests`). Hai project test còn lại (`StudentInternshipMgmt.Tests` và `StudentInternshipMgmt.Infrastructure.Tests`) thuộc về file solution `InternshipManagement\backend\InternshipManagement.sln`.
   - Hệ quả: Khi người dùng làm theo đúng Bước 4.3 của tài liệu và chạy `dotnet test ... --no-build`, hai lệnh test sau sẽ báo lỗi thoát mã 1: `The test source file "...dll" does not exist`.
   - *Ghi chú*: Trong script nội bộ [prepare-clean-env.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/prepare-clean-env.ps1) (dòng 216-241), tác giả đã kịp sửa để restore và build cả 2 file solution, nhưng lại quên cập nhật vào tài liệu hướng dẫn người ngoài [README-REPRODUCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/README-REPRODUCE.md).
3. **Kết quả sau khi bổ sung build solution còn thiếu**:
   - Backend tests: 402 passed, 1 skipped (100% khớp).
   - Frontend tests: 194 passed (sau khi chạy `npm ci`).
   - Kiểm tra contrast và labels: ĐẠT 100%.
   - Toàn bộ thư mục tái lập tạm thời đã được dọn sạch hoàn toàn sau khi kiểm chứng xong.

---

## 3. Danh Sách Các Phát Hiện Phân Cấp Theo Mức Độ (P1 - P4)

### Mức P1 (Nghiêm trọng / Critical)
*(Không có phát hiện nào)*.

---

### Mức P2 (Cao / High)

#### [P2-01] [Chắc chắn] Sai lệch thông tin báo cáo: Tuyên bố xử lý 10 phản hồi FB-01..FB-10
- **Vị trí**: [uat-scenarios.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/uat-scenarios.md) (dòng 6) và [run-uat-scenarios.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-uat-scenarios.ps1) (dòng 751).
- **Mô tả**: Báo cáo từng khẳng định chưa chuẩn xác về việc đã giải quyết dứt điểm các phản hồi UAT thực tế (FB-01 đến FB-10). Tuy nhiên theo tài liệu [KNOWN-ISSUES.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/uat-round/KNOWN-ISSUES.md), chỉ có 5 phản hồi được sửa trong code, 2 phản hồi bị từ chối theo thiết kế (FB-05, FB-06) và 3 phản hồi bị hoãn lại (FB-07, FB-08, FB-09). Việc dùng từ cũ là nói quá sự thật, gây nhầm lẫn cho hội đồng nghiệm thu.

#### [P2-02] [Chắc chắn] Lỗi tài liệu tái lập thủ công trong README-REPRODUCE.md
- **Vị trí**: [README-REPRODUCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/README-REPRODUCE.md) (Mục 4.2 và 4.3).
- **Mô tả**: Hướng dẫn chỉ bảo người dùng build `DuAnQuanLyThucTapSinh.sln`, nhưng solution này không bao gồm `StudentInternshipMgmt.Tests.csproj` và `StudentInternshipMgmt.Infrastructure.Tests.csproj`. Do đó, người dùng làm theo hướng dẫn chạy test với cờ `--no-build` sẽ bị lỗi crash vì không tìm thấy file `.dll`.

---

### Mức P3 (Trung bình / Medium)

#### [P3-01] [Chắc chắn] Bước Cross-Review trong script là giả định (0.00s) và cross-review.md viết tĩnh
- **Vị trí**: [run-release.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/run-release.ps1) (dòng 146-160) và [cross-review.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/cross-review.md).
- **Mô tả**: Bước đánh giá chéo không chạy công cụ phân tích tĩnh hay diff nào mà chỉ kiểm tra file có sẵn. Báo cáo đánh giá chéo đã soạn sẵn thiếu tính khách quan khi không đề cập đến việc mã nguồn phụ thuộc chặt vào cú pháp row lock riêng của Microsoft SQL Server (`[Students]`, `[JobPositions]`).

#### [P3-02] [Chắc chắn] Lệch commit giữa gói zip và tag phát hành v1.0.0
- **Vị trí**: [DuAnQuanLyThucTapSinh-v1.0.0.zip](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/DuAnQuanLyThucTapSinh-v1.0.0.zip) và tag `v1.0.0`.
- **Mô tả**: Gói zip được tạo từ commit `34ec284`, trong khi tag `v1.0.0` gắn vào commit `5a3bc91`. Sự lệch này dẫn đến 32 tệp log nghiệm thu và báo cáo UAT có trong repo nhưng không nằm trong gói zip nguồn.

#### [P3-03] [Chắc chắn] Dự án test Application.Tests bị bỏ quên, không nằm trong solution
- **Vị trí**: Thư mục `InternshipManagement/backend/tests/Application.Tests`.
- **Mô tả**: Dự án test này không có mã kiểm thử, không nằm trong solution nào và không được chạy. Dù không ảnh hưởng đến vận hành, nó là tệp tin rác cần được dọn dẹp hoặc bổ sung vào solution.

---

### Mức P4 (Thấp / Low)

#### [P4-01] [Cần xác minh] Tệp log UAT-03 ghi nhận kiểm tra UTF-8 BOM là False
- **Vị trí**: [UAT-03.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/UAT-03.txt).
- **Mô tả**: Dòng log ghi `Kiểm tra UTF-8 BOM: False` do script PowerShell convert chuỗi trước khi kiểm tra byte. Cần tối ưu lại script kiểm tra byte gốc trực tiếp từ HTTP stream để tránh false-negative trong log.

#### [P4-02] [Cần xác minh] Chuỗi fallback rác trong CompanyPlacementRequestDetailView.vue
- **Vị trí**: Dòng 174 file `CompanyPlacementRequestDetailView.vue`.
- **Mô tả**: Template chứa chuỗi fallback ngẫu nhiên không tự nhiên khi `coverLetter` rỗng (đã được ghi nhận trong Known Issues nhưng chưa sửa).

---

## 4. Các Giả Định Đã Đặt Trong Quá Trình Review
1. **Môi trường SQL Server**: Giả định rằng hệ thống phát triển và nghiệm thu sử dụng SQL Server cục bộ với chứng thực Windows (Windows Authentication / Integrated Security=True).
2. **Quyền riêng tư của CSDL chính**: Giả định rằng CSDL `StudentInternshipMgmtDb` là tài nguyên cấm can thiệp, mọi kiểm tra chỉ thực hiện bằng truy vấn đọc (`SELECT`, `COUNT`, `MAX(modify_date)`).
3. **Mục đích của gói Zip**: Giả định rằng `DuAnQuanLyThucTapSinh-v1.0.0.zip` đóng gói mã nguồn để triển khai/biên dịch từ đầu (source distribution), do đó việc thiếu các tệp bằng chứng sinh ra sau đó trong `RELEASE/` là có thể chấp nhận được về mặt kỹ thuật đóng gói `git archive`.

---

## 5. Đề Xuất Khắc Phục (Chỉ Đề Xuất)
1. **Đính chính câu chữ trong tài liệu UAT**:
   - Sửa dòng 6 trong `docs/release/uat-scenarios.md` thành:
     > *"Đợt phát hành Release v1.0.0 đã xử lý triệt để 10 phản hồi quan sát người thật: trong đó **khắc phục 5 vấn đề bằng mã nguồn** (FB-01..04, FB-10), **từ chối 2 yêu cầu** vi phạm quy chế quản lý/an toàn (FB-05, FB-06), và **chuyển 3 yêu cầu mở rộng sang các giai đoạn sau** (FB-07, FB-08, FB-09)."*
2. **Cập nhật tài liệu README-REPRODUCE.md**:
   - Bổ sung lệnh restore và build cho `InternshipManagement\backend\InternshipManagement.sln` vào Mục 4.2 trước khi hướng dẫn chạy test, hoặc thêm các project test còn lại vào `DuAnQuanLyThucTapSinh.sln`.
3. **Đồng bộ solution file**:
   - Đưa các dự án test còn thiếu vào `DuAnQuanLyThucTapSinh.sln` để giải pháp gốc này thực sự đại diện cho toàn bộ workspace. Xóa bỏ thư mục rỗng `Application.Tests` nếu không có ý định sử dụng.
4. **Phòng thủ kiểu dữ liệu tại frontend**:
   - Trong `JobPositionApplyView.vue`, thay đổi `value.trim()` thành `typeof value === 'string' ? value.trim() : value` để ngăn chặn lỗi runtime khi người dùng submit dữ liệu có trường không phải kiểu chuỗi.
