# Báo Cáo Đánh Giá Chéo Mã Nguồn Độc Lập (Independent Cross-Review - Release v1.0.0)

> [!IMPORTANT]
> **Vai trò thực hiện**: Kỹ sư Đánh giá Chéo Độc Lập & QA Lead.
> **Phạm vi thẩm định**: Toàn bộ thay đổi từ mốc gốc `baseline` đến `HEAD` (`git diff baseline..HEAD`).
> **Phương pháp**: Phân tích trực tiếp từ mã nguồn thực tế, kiểm chứng độc lập bằng bộ kiểm thử tự động, tuyệt đối không dựa vào ghi chú chủ quan của người lập trình.

---

## 1. Phương Pháp Luận & Tiêu Chí Phân Loại

Toàn bộ các phát hiện được phân cấp theo 4 mức độ nghiêm trọng:
- **P1 (Nghiêm trọng / Critical)**: Sai lệch logic nghiệp vụ, vỡ hợp đồng API (route, DTO, status code), phá vỡ lược đồ (schema) CSDL hoặc thay đổi ngoài phạm vi cho phép.
- **P2 (Cao / High)**: Lỗi chặn (blocker), crash hệ thống, rủi ro an ninh/bảo mật, tranh chấp tài nguyên đồng thời (race condition).
- **P3 (Trung bình / Medium)**: Lỗi xác thực dữ liệu (validation), chuỗi thông báo chưa thân thiện, sai sót giao diện người dùng nhưng không gây sai hỏng dữ liệu.
- **P4 (Thấp / Low)**: Vấn đề thẩm mỹ, định dạng tài liệu, code smells, nợ kỹ thuật chấp nhận được không ảnh hưởng đến vận hành.

---

## 2. Bảng Phân Tích & Đánh Giá Chi Tiết Các Thay Đổi (Baseline → HEAD)

| Mã | Tệp Thay Đổi | Phân Loại | Mức Độ | Trạng Thái | Đánh Giá Kỹ Thuật Độc Lập |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **REV-01** | `PlacementAcceptanceService.cs` | Tái cấu trúc (Refactor) | P4 | **ĐẠT (PASS)** | - Khai trừ toàn bộ magic strings sang hằng số `private const`.<br>- Tách nhỏ `AcceptAsync` (228 -> 77 dòng) và `CancelAcceptedAsync` (186 -> 75 dòng) theo nguyên lý SRP.<br>- Cô lập logic Transaction và Pessimistic Row Lock (`ExecuteInTransactionAsync`, `LockRelationalRows...`).<br>- **Kiểm chứng**: 22/22 bài test đặc tả hành vi `PlacementAcceptanceCharacterizationTests` đạt 100%. Không đổi chữ ký phương thức hay hợp đồng trả về. |
| **REV-02** | `PlacementRequestService.cs` | Sửa lỗi Race Condition (FB-10) | P2 | **ĐÃ SỬA & KIỂM CHỨNG (PASS)** | - Khi sinh viên nộp đơn, service bọc luồng xử lý trong Database Transaction và áp dụng Pessimistic Row Lock trên dòng sinh viên (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = @studentId`).<br>- **Kiểm chứng**: Kịch bản chạy đua song song (UAT-07 / UAT2-05) với 2 request đồng thời chứng minh: đúng 1 request được duyệt (HTTP 200), request còn lại bị chặn (HTTP 400), CSDL chỉ tạo duy nhất 1 bản ghi. |
| **REV-03** | `JobPositionApplyView.vue` | Sửa cờ `saving` (CR-03 / FB-10) | P3 | **ĐÃ SỬA & KIỂM CHỨNG (PASS)** | - Vô hiệu hóa nút bấm ngay khi click (`saving = true`).<br>- Bọc toàn bộ phần thân hàm `submit()` trong khối `try...finally` đảm bảo cờ `saving` luôn được hoàn tác về `false`, không gây treo nút khi validation client ném ngoại lệ.<br>- **Kiểm chứng**: 4 unit tests mới trong `JobPositionApplyView.test.js` kiểm tra đầy đủ 4 trạng thái vòng đời đạt 100%. |
| **REV-04** | `StudentService.cs`, `StudentValidators.cs` | Chuẩn hóa Tiếng Việt (FB-01) | P3 | **ĐÃ SỬA & KIỂM CHỨNG (PASS)** | - Thay thế các chuỗi cứng nửa tiếng Anh (`FullName là bắt buộc`, `PhoneNumber là bắt buộc`) thành tiếng Việt tự nhiên (`Họ tên không được để trống`, `Số điện thoại không được để trống`).<br>- **Kiểm chứng**: Kiểm tra tự động tại UAT-02 và unit tests `StudentValidatorTests` đạt 100%. |
| **REV-05** | `JobPositionService.cs`, `companyJobPositionValidation.js` | Loại bỏ token kỹ thuật (FB-02) | P3 | **ĐÃ SỬA & KIỂM CHỨNG (PASS)** | - Bỏ hoàn toàn các chuỗi `(Title)` và `(Quantity)` khỏi thông báo lỗi ở cả backend và frontend.<br>- **Kiểm chứng**: Kịch bản UAT-05 kiểm tra regex không còn bất kỳ token nào xuất hiện trong response. |
| **REV-06** | `CreatePlacementRequestDtoValidator.cs`, `applicationValidation.js` | Cắt tỉa link CV (FB-03) | P3 | **ĐÃ SỬA & KIỂM CHỨNG (PASS)** | - Tự động cắt tỉa khoảng trắng 2 đầu CV URL; lưu URL sạch vào CSDL.<br>- Phòng thủ 3 tầng: Chặn scheme nguy hiểm (`javascript:`, `ftp:`) và URL có khoảng trắng ở giữa.<br>- **Kiểm chứng**: Kịch bản UAT-06 kiểm chứng direct query CSDL lưu URL sạch, từ chối scheme lạ với HTTP 400. |
| **REV-07** | `InternshipManagement/README.md` | Sửa mã hóa font (FB-04) | P4 | **ĐÃ SỬA & KIỂM CHỨNG (PASS)** | - Dòng cuối cùng được chuyển sang UTF-8 chuẩn không BOM, hiển thị tiếng Việt chính xác 100%. |
| **REV-08** | `VERSION`, `Directory.Build.props`, `package.json` | Đồng bộ phiên bản SemVer | P4 | **ĐẠT (PASS)** | - Nâng version thống nhất lên `1.0.0` đồng bộ toàn giải pháp. |
| **REV-09** | `scripts/release/prepare-clean-env.ps1`, `run-release.ps1` | Tự động hóa phát hành | P4 | **ĐẠT (PASS)** | - Dựng môi trường demo sạch ngoài repo, chốt chặn bảo vệ CSDL chính, dọn dẹp an toàn. |

---

## 3. Rà Soát An Ninh & Bảo Mật Nhanh (Security Sanity Check)

1. **Bảo mật bí mật thông tin (No Secrets in Repo/Package)**:
   - Rà soát toàn bộ cây thư mục và gói nén `DuAnQuanLyThucTapSinh-v1.0.0.zip`.
   - Xác nhận: Không có mật khẩu, token thật, private key, hay connection string production nào được commit.
   - Khóa ký JWT được sinh ngẫu nhiên theo từng phiên chạy demo và nạp qua biến môi trường tiến trình `$env:Jwt__SecretKey`.
2. **Kiểm soát phân quyền & Đa người thuê (Role Authorization & Multi-Tenancy)**:
   - Endpoint quản trị `/api/students`, `/api/companies/accounts/bulk` yêu cầu vai trò `Admin`. Người dùng nặc danh nhận 401 Unauthorized, sinh viên nhận 403 Forbidden.
   - Doanh nghiệp A không thể sửa đổi hoặc truy cập trái phép dữ liệu tuyển dụng của Doanh nghiệp B (UAT-10 kiểm chứng trả về 403/404).
3. **Phòng chống lộ thông tin lỗi (No Stack Traces)**:
   - `GlobalExceptionHandlingMiddleware` bắt toàn bộ exception chưa xử lý và trả về cấu trúc chuẩn `ApiResponse<T>.FailResponse(...)` với HTTP 500 hoặc 400, không lộ stack trace hay cấu trúc mã nguồn máy chủ.
4. **Chính sách mật khẩu tài khoản demo**:
   - Mọi tài khoản demo (kể cả admin ban đầu) đều gắn cờ `MustChangePassword = true`, buộc người dùng đổi mật khẩu ngay ở lần đăng nhập đầu tiên mới được cấp quyền thao tác trên hệ thống. Mật khẩu demo được ghi rõ chỉ sử dụng cho môi trường thử nghiệm.

---

## 4. Kết Luận Đánh Giá Chéo
- **Tổng số lỗi P1**: **0** (Không có lỗi logic, vỡ hợp đồng API hay vỡ schema).
- **Tổng số lỗi P2**: **1** phát hiện (Race condition nộp đơn) -> **ĐÃ SỬA VÀ KIỂM CHỨNG THÀNH CÔNG**. Hiện tại **0 còn mở**.
- **Tổng số lỗi P3**: **5** phát hiện (`FB-01`, `FB-02`, `FB-03`, `CR-03`, `DOC-01`) -> **100% ĐÃ ĐƯỢC KHẮC PHỤC TRIỆT ĐỂ**. Hiện tại **0 còn mở**.
- **Tổng số phát hiện P4**: Được ghi nhận vào mục Known Issues trong Changelog và Bug Log.
- **Kết luận**: Bản mã nguồn đạt chất lượng phát hành chính thức cho **Release v1.0.0**.
