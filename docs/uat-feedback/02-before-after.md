# Báo cáo So sánh Trước và Sau khi Sửa lỗi (Before / After Evidence Report)

Tài liệu này tổng hợp chi tiết bằng chứng đối chiếu trước (Before) và sau (After) khi chỉnh sửa sản phẩm theo các phản hồi UAT được lựa chọn, kèm theo đường dẫn file bằng chứng gốc và các bước tái lập độc lập.

---

## 1. FB-04: Sửa lỗi mã hóa tiếng Việt (Mojibake) trong tài liệu README.md

### Thông tin chung
- **Mức ưu tiên**: P4 (Thấp / Nợ kỹ thuật nhỏ).
- **Mục tiêu**: Chuẩn hóa dòng giới thiệu cuối cùng trong [README.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/README.md#L9) hiển thị đúng chính tả tiếng Việt UTF-8.
- **Tệp bằng chứng Before**: [FB-04-before.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-04-before.txt)
- **Tệp bằng chứng After**: [FB-04-after.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-04-after.txt)
- **Mã commit**: `381d0eb` (`fix(uat): FB-04 fix README mojibake encoding`).

### Đối chiếu Before / After
| Nội dung | Trước khi sửa (Before) | Sau khi sửa (After) |
| :--- | :--- | :--- |
| **Hiển thị dòng 9** | `Há»‡ thá»‘ng quáº£n lÃ½ sinh viÃªn thá»±c táº­p (ASP.NET Core 8 + Vue 3).` | `Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3).` |
| **Chuỗi hex UTF-8** | Chuỗi ký tự bị giải mã nhầm kép Windows-1252 / UTF-8 | `48-E1-BB-87-20-74-68-E1-BB-91-6E-67-20...` (Chuẩn UTF-8 không BOM) |

### Cách tái lập kiểm chứng
1. Mở file [README.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/README.md) bằng trình soạn thảo văn bản hỗ trợ UTF-8.
2. Kiểm tra dòng 9 (dòng cuối cùng của file), xác nhận hiển thị tiếng Việt hoàn toàn rõ ràng, không xuất hiện các ký tự `Há»‡ thá»‘ng...`.

---

## 2. FB-01: Chuẩn hóa thông báo lỗi sinh viên sang tiếng Việt tự nhiên

### Thông tin chung
- **Mức ưu tiên**: P3 (Trung bình / Trải nghiệm người dùng).
- **Mục tiêu**: Loại bỏ chuỗi tiếng Anh nửa vời (`FullName là bắt buộc`, `PhoneNumber là bắt buộc`, `StudentCode là bắt buộc`), thay thế bằng thông báo tiếng Việt chuẩn mực cả ở FluentValidation backend và service logic.
- **Tệp bằng chứng Before**: [FB-01-before.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-01-before.txt)
- **Tệp bằng chứng After**: [FB-01-after.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-01-after.txt)
- **Mã commit**: `cde0541` (`fix(uat): FB-01 standardize student validation messages in Vietnamese`).

### Đối chiếu Before / After
| Trường dữ liệu | Thông báo Trước (Before) | Thông báo Sau (After) |
| :--- | :--- | :--- |
| `StudentCode` để trống | `StudentCode là bắt buộc.` | `Mã số sinh viên không được để trống.` |
| `FullName` để trống | `FullName là bắt buộc.` | `Họ tên không được để trống.` |
| `Email` để trống | `Email là bắt buộc.` | `Email không được để trống.` |
| `PhoneNumber` để trống | `PhoneNumber là bắt buộc.` | `Số điện thoại không được để trống.` |
| `PhoneNumber` sai định dạng | `PhoneNumber phải là số Việt Nam hợp lệ...` | `Số điện thoại phải là số Việt Nam hợp lệ (10 số, bắt đầu bằng 0).` |
| `Major` để trống | `Major là bắt buộc.` | `Ngành không được để trống.` |
| `ClassName` để trống | `ClassName là bắt buộc.` | `Lớp không được để trống.` |
| `StudentCode` trùng lặp | `StudentCode '{code}' đã tồn tại.` | `Mã số sinh viên '{code}' đã tồn tại.` |

### Phân tích phụ thuộc repo
- **Tests backend**: File [`StudentValidatorTests.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/StudentInternshipMgmt.Tests/StudentValidatorTests.cs) assert theo `error.PropertyName == nameof(...)`, được bổ sung thêm kiểm tra nội dung thông báo tiếng Việt mới. File [`StudentSoftDeleteFilterTests.cs`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/StudentSoftDeleteFilterTests.cs) dòng 87 và 174 được cập nhật từ `.Contain("PhoneNumber")` sang `.Contain("Số điện thoại")`.
- **Frontend**: [`studentEditValidation.js`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/studentEditValidation.js) và [`StudentCreateView.vue`](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/views/students/StudentCreateView.vue) ánh xạ server errors theo key và hiển thị thông điệp tiếng Việt đồng bộ.

### Cách tái lập kiểm chứng
1. Gửi request `POST /api/students` với body rỗng:
   ```json
   {"studentCode":"","fullName":"","email":"","phoneNumber":"","major":"","className":""}
   ```
2. Quan sát HTTP Status 400 và nội dung trường `errors`: tất cả thông báo đều bằng tiếng Việt thân thiện.
3. Gửi request `POST /api/students` với `studentCode` đã tồn tại: nhận được `{"success":false,"message":"Mã số sinh viên '...' đã tồn tại."}`.

---

## 3. FB-02: Bỏ định danh kỹ thuật trong thông báo lỗi vị trí tuyển dụng

### Thông tin chung
- **Mức ưu tiên**: P3 (Trung bình / Trải nghiệm doanh nghiệp).
- **Mục tiêu**: Xóa bỏ các định danh kỹ thuật `(Title)` và `(Quantity)` trong thông báo lỗi ở cả frontend validation và backend service.
- **Tệp bằng chứng Before**: [FB-02-before.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-02-before.txt)
- **Tệp bằng chứng After**: [FB-02-after.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-02-after.txt)
- **Mã commit**: `40ff5a9` (`fix(uat): FB-02 remove technical identifiers from job position validation messages`).

### Đối chiếu Before / After
| Tình huống kiểm thử | Thông báo Trước (Before) | Thông báo Sau (After) |
| :--- | :--- | :--- |
| **Tiêu đề để trống** | `Tiêu đề (Title) là bắt buộc.` | `Tiêu đề là bắt buộc.` |
| **Số lượng <= 0** | `Số lượng tuyển (Quantity) phải là số nguyên lớn hơn 0.` (FE) / `Số lượng tuyển (Quantity) phải lớn hơn 0.` (BE) | `Số lượng tuyển phải là số nguyên lớn hơn 0.` (FE) / `Số lượng tuyển phải lớn hơn 0.` (BE) |
| **Giảm số lượng dưới mức đã nhận** | `Không thể giảm số lượng tuyển (Quantity) xuống dưới số lượng đã nhận hiện tại ({count}).` | `Không thể giảm số lượng tuyển xuống dưới số lượng đã nhận hiện tại ({count}).` |
| **Mô tả quá ký tự** | `Mô tả (Description) không được quá 2000 ký tự.` | `Mô tả không được quá 2000 ký tự.` |

### Cách tái lập kiểm chứng
1. Đăng nhập tài khoản công ty (`POST /api/auth/login`).
2. Gửi request tạo vị trí tuyển dụng với `quantity = 0`:
   ```bash
   POST /api/company-me/job-positions
   Body: {"title":"Thực tập sinh","quantity":0,"description":"Mô tả công việc"}
   ```
   Xác nhận phản hồi: `{"success":false,"message":"Số lượng tuyển phải lớn hơn 0."}`.
3. Gửi request với `title = ""`:
   Xác nhận phản hồi: `{"success":false,"message":"Tiêu đề là bắt buộc."}`.

---

## 4. FB-03: Cắt khoảng trắng 2 đầu CV URL và bảo vệ an toàn scheme

### Thông tin chung
- **Mức ưu tiên**: P3 (Trung bình / Trải nghiệm người dùng di động).
- **Mục tiêu**:
  - Tự động cắt (trim) khoảng trắng ở hai đầu liên kết CV cả ở client và server, lưu giá trị đã trim vào CSDL.
  - Vẫn duy trì cơ chế bảo vệ nghiêm ngặt: từ chối các liên kết chứa khoảng trắng ở giữa, URL tương đối, và các scheme nguy hại (như `javascript:`).
- **Tệp bằng chứng Before**: [FB-03-before.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-03-before.txt)
- **Tệp bằng chứng After**: [FB-03-after.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-03-after.txt)
- **Mã commit**: `838f6de` (`fix(uat): FB-03 allow trimmed CV URL and reject invalid schemes and internal spaces`).

### Đối chiếu Before / After
| Trường hợp kiểm thử (CV URL) | Hành vi Trước (Before) | Hành vi Sau (After) |
| :--- | :--- | :--- |
| `"   https://example.com/cv.pdf   "` (Có khoảng trắng ở 2 đầu) | **HTTP 400 Bị từ chối** (`Liên kết CV phải là URL tuyệt đối... và không chứa khoảng trắng`) | **HTTP 200 Thành công**, backend lưu chuỗi đã cắt: `"https://example.com/cv.pdf"` |
| `"javascript:alert(1)"` (Scheme độc hại) | **HTTP 400 Bị từ chối** | **HTTP 400 Bị từ chối** (Giữ vững an toàn) |
| `"/uploads/cv/my-cv.pdf"` (URL tương đối) | **HTTP 400 Bị từ chối** | **HTTP 400 Bị từ chối** (Giữ vững an toàn) |
| `"https://example.com/my cv.pdf"` (Khoảng trắng ở giữa) | **HTTP 400 Bị từ chối** | **HTTP 400 Bị từ chối** (Giữ vững an toàn) |

### Cách tái lập kiểm chứng
1. Đăng nhập tài khoản sinh viên.
2. Gửi request `POST /api/me/placement-requests` với `cvUrl = "   https://example.com/cv/test_trim   "`.
3. Kiểm tra HTTP Status: nhận 200/201.
4. Truy vấn lại chi tiết đơn bằng `GET /api/me/placement-requests`: xác nhận trường `cvUrl` trong database là `"https://example.com/cv/test_trim"` (không còn khoảng trắng thừa).
5. Thử gửi với `cvUrl = "javascript:void(0)"` hoặc `"/relative/path"`: xác nhận nhận mã lỗi HTTP 400.

---

## 5. FB-10: Chống Race Condition gửi trùng lặp đơn ứng tuyển khi click đúp

### Thông tin chung
- **Mức ưu tiên**: P2 (Cao / Lỗi toàn vẹn dữ liệu nghiệp vụ chính).
- **Mục tiêu**:
  - Bảo vệ luồng nộp đơn ứng tuyển khỏi race condition khi người dùng bấm nút gửi 2 lần liên tiếp nhanh chóng hoặc mạng chập chờn gửi request song song.
  - Sử dụng cơ chế Database Transaction kết hợp Row Lock trên bảng `Students` (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}`).
  - Khóa tức thì nút bấm `saving = true` ngay đầu hàm `submit()` ở giao diện frontend.
- **Tệp bằng chứng Before**: [FB-10-before.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-10-before.txt)
- **Tệp bằng chứng After**: [FB-10-after.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/FB-10-after.txt)
- **Mã commit**: `21d7ded` (`fix(uat): FB-10 prevent duplicate placement request on rapid double submission`).

### Đối chiếu Before / After
| Tiêu chí | Trước khi sửa (Before) | Sau khi sửa (After) |
| :--- | :--- | :--- |
| **Request 1** | HTTP 200 OK (Tạo thành công đơn Id #1) | HTTP 200 OK (Tạo thành công đơn Id #1) |
| **Request 2 (song song)** | **HTTP 200 OK (Tạo thành công đơn Id #2 trùng lặp)** | **HTTP 400 Bị chặn** (`Bạn đã có đơn đang mở cho doanh nghiệp và vị trí này.`) |
| **Tổng số đơn trong CSDL** | **2 đơn mở trùng nhau** (FAIL - Sai hỏng tính toàn vẹn dữ liệu) | **Đúng 1 đơn duy nhất** (PASS - An toàn tuyệt đối) |
| **UI Button Behavior** | Trễ sau bước validate client mới disable | `saving.value = true` được gán ngay tại dòng đầu tiên của `submit()` |

### Cách tái lập kiểm chứng
1. Chuẩn bị tài khoản sinh viên có trạng thái `NoCompany`.
2. Sử dụng PowerShell script [capture-after-evidence.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/capture-after-evidence.ps1) (phần 5) hoặc công cụ curl/Postman gửi đồng thời 2 request `POST /api/me/placement-requests` cùng nội dung trong khoảng thời gian < 10ms.
3. Kiểm tra kết quả trả về:
   - Request 1 nhận HTTP 200.
   - Request 2 nhận HTTP 400 kèm thông điệp `"Bạn đã có đơn đang mở cho doanh nghiệp và vị trí này."`.
4. Kiểm tra CSDL: chỉ có đúng 1 bản ghi được tạo trong bảng `PlacementRequests`.

---

## 6. Ghi nhận kiểm thử bổ sung cho TC-13 và TC-14

Trong quá trình thực nghiệm theo chỉ đạo, 2 ca kiểm thử còn lại trong file `InternshipManagement/docs/evidence/TEST-CASES.md` đã được chạy thực tế:
- **TC-13 (XSS Script Injection trong CoverLetter)**: Nhập `<script>alert('xss')</script>`. Kết quả: Server lưu trữ an toàn dưới dạng plain text; Frontend Vue 3 tự động HTML-escape qua binding `{{ ... }}` nên không xảy ra lỗi XSS (Đạt).
- **TC-14 (Giao diện hiển thị trên màn hình hẹp 375px - iPhone SE / iPhone 13 mini)**: Kiểm tra layout form ứng tuyển và danh sách bảng biểu. Kết quả: PrimeVue DataTable và layout flexbox co giãn responsive tốt, không bị vỡ layout hay che khuất nút bấm (Đạt).
- Bằng chứng chi tiết: [TC-13-14-before.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/evidence/before-after/TC-13-14-before.txt).
