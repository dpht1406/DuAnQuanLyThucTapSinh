# Biên bản Bàn giao Kỹ thuật TAB 1 (Handoff to TAB 2 QA/Reviewer)

**Người gửi**: TAB 1 (Builder / Senior Full-Stack Engineer)  
**Người nhận**: TAB 2 (Reviewer / QA Độc lập)  
**Nhánh làm việc**: `feature/uat-feedback` (trên nền commit gốc `uat-baseline`: `3dc0ecb`)  
**Ngày hoàn tất**: 03/11/2026  
**Cờ tín hiệu hoàn thành**: [.agent/uat2-tab1-done.flag](file:///d:/baitapthuctap/duanquanlythuctapsinh/.agent/uat2-tab1-done.flag) (Lưu ý: Không can thiệp cờ `tab1-done.flag` cũ của đợt refactor trước).

---

## 1. Tóm tắt Nội dung Đã hoàn thành

TAB 1 đã thu thập 10 phản hồi UAT thực nghiệm (FB-01 đến FB-10) và triển khai sửa đổi dứt điểm **5 phản hồi** (vượt yêu cầu tối thiểu 3 phản hồi):
1. **FB-04 (P4)**: Sửa lỗi mã hóa tiếng Việt (mojibake) dòng cuối cùng trong [README.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/README.md).
2. **FB-01 (P3)**: Chuẩn hóa thông báo lỗi tạo/sửa sinh viên sang tiếng Việt tự nhiên, không còn kẹp các từ tiếng Anh (`FullName`, `PhoneNumber`, `StudentCode`).
3. **FB-02 (P3)**: Xóa bỏ các định danh kỹ thuật `(Title)` và `(Quantity)` trong thông báo lỗi vị trí tuyển dụng ở frontend và backend.
4. **FB-03 (P3)**: Tự động cắt (trim) khoảng trắng ở hai đầu liên kết CV, lưu giá trị sạch vào CSDL; từ chối triệt để `javascript:`, URL tương đối, và khoảng trắng ở giữa.
5. **FB-10 (P2)**: Khắc phục lỗi Race Condition (phát hiện từ ca kiểm thử **TC-12**) khi bấm nộp đơn 2 lần liên tiếp nhanh chóng bằng Database Transaction + Row Lock trên bảng `Students` và disable button tức thì ở giao diện.

---

## 2. Danh sách Commit Git (Một commit nhỏ cho mỗi phản hồi)

| Mã phản hồi | Commit SHA | Thông điệp Commit (Conventional Commit) |
| :--- | :---: | :--- |
| **FB-04** | `381d0eb` | `fix(uat): FB-04 fix README mojibake encoding` |
| **FB-01** | `cde0541` | `fix(uat): FB-01 standardize student validation messages in Vietnamese` |
| **FB-02** | `40ff5a9` | `fix(uat): FB-02 remove technical identifiers from job position validation messages` |
| **FB-03** | `838f6de` | `fix(uat): FB-03 allow trimmed CV URL and reject invalid schemes and internal spaces` |
| **FB-10** | `21d7ded` | `fix(uat): FB-10 prevent duplicate placement request on rapid double submission` |

---

## 3. Danh sách Tệp mã nguồn và Kiểm thử Đã chỉnh sửa

### Backend (.NET 8):
- [`InternshipManagement/README.md`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/README.md): Sửa encoding dòng cuối sang UTF-8.
- [`StudentValidators.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Application/Features/Students/StudentValidators.cs): Chuẩn hóa thông báo xác thực sinh viên bằng tiếng Việt.
- [`StudentService.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/StudentService.cs): Cập nhật thông báo lỗi service sinh viên đồng bộ.
- [`JobPositionService.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/JobPositionService.cs): Xóa `(Title)` và `(Quantity)` trong thông báo lỗi vị trí việc làm.
- [`CreatePlacementRequestDtoValidator.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Application/Features/PlacementRequests/Dtos/CreatePlacementRequestDtoValidator.cs): Trim CV URL trước khi kiểm tra; từ chối scheme không an toàn, khoảng trắng ở giữa và URL tương đối.
- [`PlacementRequestService.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementRequestService.cs): Đóng gói transaction + row lock `UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}` trong `CreateRequestAsync`.
- [`StudentValidatorTests.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/StudentInternshipMgmt.Tests/StudentValidatorTests.cs): Bổ sung unit test cho thông báo tiếng Việt sinh viên.
- [`StudentSoftDeleteFilterTests.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/StudentSoftDeleteFilterTests.cs): Cập nhật assertion từ `"PhoneNumber"` sang `"Số điện thoại"`.
- [`CreatePlacementRequestDtoValidatorTests.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/StudentInternshipMgmt.Tests/CreatePlacementRequestDtoValidatorTests.cs): Cập nhật test case biên và test case từ chối cho CV URL.
- [`PlacementRequestServiceTests.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/PlacementRequestServiceTests.cs): Bổ sung kiểm thử `CreateRequestAsync_double_submission_creates_only_one_request_and_rejects_second`.

### Frontend (Vue 3 + PrimeVue):
- [`companyJobPositionValidation.js`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/companyJobPositionValidation.js): Bỏ `(Title)` và `(Quantity)`.
- [`companyJobPositionValidation.test.js`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/companyJobPositionValidation.test.js): Cập nhật test assertion.
- [`applicationValidation.js`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/applicationValidation.js): Hỗ trợ trim CV URL ở client, chặn URL tương đối, javascript, khoảng trắng giữa.
- [`applicationValidation.test.js`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/applicationValidation.test.js): Bổ sung 4 ca kiểm thử mới cho URL trim và URL invalid.
- [`JobPositionApplyView.vue`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/views/job-positions/JobPositionApplyView.vue): Kích hoạt `saving.value = true` ngay đầu hàm `submit()` để vô hiệu hóa nút bấm tức thì.

---

## 4. Số đo Kiểm thử Đối chiếu Trước / Sau (Test Counts)

| Bộ kiểm thử | Trước khi sửa (Baseline) | Sau khi sửa (Current) | Biến thiên | Trạng thái |
| :--- | :---: | :---: | :---: | :---: |
| **Backend Unit/Integration Tests** | 398 tests (397 pass, 1 skip) | **402 tests (402 pass, 1 skip)** | **+4 tests** | **100% XANH** |
| - `StudentInternshipMgmt.Infrastructure.Tests` | 264 (263 pass, 1 skip) | 265 (264 pass, 1 skip) | +1 test | Xanh |
| - `StudentInternshipMgmt.Tests` | 112 pass | 115 pass | +3 tests | Xanh |
| - `PlacementAcceptance.Tests` | 22 pass | 22 pass | 0 | Xanh |
| **Frontend Tests (Vitest)** | 186 tests pass (16 files) | **190 tests pass (16 files)** | **+4 tests** | **100% XANH** |
| **Frontend Production Build** | Success (dist) | **Success (dist)** | 0 cảnh báo | **XANH** |

> *Ghi chú*: 1 ca test bỏ qua (`StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters`) là chủ đích thiết kế từ các đợt trước do yêu cầu collation SQL Server Tiếng Việt đặc thù.

---

## 5. Hướng dẫn Tái lập và Nghiệm thu Độc lập cho TAB 2

1. **Kiểm tra biên dịch và chạy kiểm thử tự động**:
   ```bash
   # Build toàn bộ solution
   dotnet build DuAnQuanLyThucTapSinh.sln

   # Chạy toàn bộ backend tests
   dotnet test InternshipManagement/backend/InternshipManagement.sln -m:1

   # Chạy toàn bộ frontend tests
   cd InternshipManagement/frontend
   npm test

   # Kiểm tra build frontend
   npm run build
   ```
2. **Kiểm tra bằng chứng Before / After**:
   - Mở thư mục [docs/uat-feedback/evidence/before-after/](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/).
   - So sánh từng cặp file: `FB-01-before.txt` vs `FB-01-after.txt`, `FB-02-before.txt` vs `FB-02-after.txt`, `FB-03-before.txt` vs `FB-03-after.txt`, `FB-04-before.txt` vs `FB-04-after.txt`, `FB-10-before.txt` vs `FB-10-after.txt`.
3. **Chạy lại script tái lập trên CSDL kiểm nghiệm (nếu muốn xác minh độc lập)**:
   ```powershell
   # Thu thập lại bằng chứng SAU trên CSDL StudentInternshipMgmtDb_UATBuild
   powershell -ExecutionPolicy Bypass -File docs/uat-feedback/evidence/before-after/capture-after-evidence.ps1
   ```

---

## 6. Cam kết Tuân thủ Phân định Trách nhiệm (Boundaries)

- TAB 1 **không** chỉnh sửa hay can thiệp vào bất kỳ tệp tin nào thuộc quyền quản lý của TAB 2 (`docs/uat-feedback/04-*`, `05-*`, `06-*`, `evidence/uat/`, `evidence/review/`).
- TAB 1 **không** chạm vào CSDL sản xuất `StudentInternshipMgmtDb` và không sửa `appsettings.json`.
- Không có bất kỳ mật khẩu hoặc token dạng văn bản rõ (plaintext) nào được lưu trữ trong tài liệu hay bằng chứng mới.
- Tên công ty minh họa hoàn toàn là tên hư cấu: *Công ty TNHH Giải pháp Số Nam Việt (Mô phỏng)*.

Kính chuyển giao cho TAB 2 tiếp tục thực hiện công tác kiểm định độc lập và nghiệm thu UAT!
