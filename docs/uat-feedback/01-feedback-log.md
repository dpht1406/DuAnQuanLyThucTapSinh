# Sổ ghi nhận và Phân loại Phản hồi UAT (UAT Feedback Log)

> [!NOTE]
> **Tuyên bố tính chất dữ liệu**: Toàn bộ các phản hồi dưới đây là **phản hồi mẫu mô phỏng từ quan sát thực tế khi chạy thử ứng dụng trên môi trường thực nghiệm**, chưa phải từ người dùng thật. Dữ liệu khảo sát thực tế sẽ được thu thập sau thông qua [feedback-form.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/feedback-form.md) và [uat-scenarios-sheet.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/uat-scenarios-sheet.md).

---

## 1. Danh sách Người dùng Mẫu (Personas)

| Mã Persona | Vai trò | Thiết bị / Trình duyệt | Đặc điểm hành vi |
| :--- | :--- | :--- | :--- |
| **SV-01** | Sinh viên năm cuối | Laptop / Chrome Desktop | Tìm kiếm vị trí thực tập, nộp CV, theo dõi trạng thái đơn ứng tuyển. |
| **SV-02** | Sinh viên năm ba | Điện thoại iPhone 13 (Viewport 375px) / Safari Mobile | Thao tác trên thiết bị di động, hay copy-paste liên kết nhanh. |
| **DN-01** | Đại diện Doanh nghiệp tiếp nhận | Laptop / Edge | Quản lý tin tuyển dụng, xem danh sách ứng viên, phản hồi lịch phỏng vấn. Đại diện cho đơn vị: *Công ty TNHH Giải pháp Số Nam Việt (Mô phỏng)*. |
| **AD-01** | Cán bộ quản lý thực tập (Admin) | PC / Chrome Desktop | Quản trị sinh viên, tạo tài khoản hàng loạt, duyệt vị trí tuyển dụng, điều phối thực tập. |
| **NM-01** | Sinh viên mới sử dụng lần đầu | Laptop / Firefox | Chưa quen thao tác phần mềm, dễ nhầm lẫn quy trình đăng nhập, đổi mật khẩu lần đầu. |

---

## 2. Thang đánh giá Mức độ Ưu tiên (Priority Matrix)

- **P1 (Nghiêm trọng / Critical)**: Lỗi làm sai lệch dữ liệu, hổng bảo mật, crash hệ thống hoặc chặn hoàn toàn luồng nghiệp vụ cốt lõi không có cách khắc phục tạm thời.
- **P2 (Cao / High)**: Ảnh hưởng trực tiếp đến tính đúng đắn của luồng nghiệp vụ chính (ví dụ: bất đồng bộ, race condition, cho phép ghi đè/tạo lặp dữ liệu) hoặc cản trở nghiêm trọng trải nghiệm người dùng.
- **P3 (Trung bình / Medium)**: Lỗi xác thực dữ liệu (validation), thông báo chưa thân thiện, sai sót giao diện người dùng cục bộ nhưng không làm sai hỏng dữ liệu.
- **P4 (Thấp / Low)**: Vấn đề thẩm mỹ, định dạng tài liệu, lỗi chính tả, nợ kỹ thuật nhỏ không ảnh hưởng vận hành.

---

## 3. Sổ phản hồi chi tiết (FB-01 đến FB-10)

### FB-01: Thông báo lỗi tạo/sửa sinh viên hiển thị nửa tiếng Anh nửa tiếng Việt
- **Người phản hồi**: `AD-01` (Cán bộ quản lý thực tập).
- **Kênh / Màn hình quan sát**: Màn hình Quản lý sinh viên (`/students/create`, `/students/edit`) và API `POST /api/students`, `PUT /api/students/{id}`.
- **Cách phát hiện**: **Chạy thật trên hệ thống** (gửi payload rỗng hoặc trùng lặp mã sinh viên tới API/màn hình).
- **Lời người dùng phản hồi**:
  > *"Em ơi, bên khoa nhập sinh viên mới, lỡ tay bấm Lưu lúc chưa điền xong thì bảng báo đỏ chót hiện 'FullName là bắt buộc', 'PhoneNumber là bắt buộc'. Nghe nửa Tây nửa Ta kỳ lắm, sinh viên hay thầy cô nhìn vào lại tưởng web bị lỗi code."*
- **Phân tích kỹ thuật**:
  - `StudentValidators.cs` và `StudentService.cs` dùng chuỗi cứng `FullName là bắt buộc.`, `PhoneNumber là bắt buộc.`, `StudentCode là bắt buộc.`, `StudentCode '{code}' đã tồn tại.`.
  - Frontend `StudentCreateView.vue` và `studentEditValidation.js` truyền nguyên văn chuỗi lỗi từ server nếu server trả về dạng dictionary.
- **Mức ưu tiên**: **P3** (Thông báo người dùng chưa chuẩn hóa, không gây sai dữ liệu).
- **Trạng thái**: **ĐÃ XỬ LÝ** (Commit: `cde0541`). Chuẩn hóa thành tiếng Việt tự nhiên: *"Họ tên không được để trống."*, *"Số điện thoại không được để trống."*, *"Mã số sinh viên không được để trống."*, *"Mã số sinh viên '{dto.StudentCode}' đã tồn tại."*.

---

### FB-02: Thông báo lỗi vị trí tuyển dụng đính kèm tên trường tiếng Anh trong ngoặc
- **Người phản hồi**: `DN-01` (Nhà tuyển dụng - Doanh nghiệp Nam Việt).
- **Kênh / Màn hình quan sát**: Màn hình Đăng tin tuyển dụng (`/company-me/job-positions`) và API `POST /api/company-me/job-positions`.
- **Cách phát hiện**: **Chạy thật trên hệ thống** (đăng nhập tài khoản công ty `cty0004`, nhập số lượng = 0 và để trống tiêu đề).
- **Lời người dùng phản hồi**:
  > *"Bên chị bấm tạo việc làm mà quên nhập số lượng thì nó báo 'Số lượng tuyển (Quantity) phải là số nguyên lớn hơn 0.', rồi tiêu đề cũng kẹp chữ (Title). Người dùng doanh nghiệp tụi chị đâu cần biết tên biến tiếng Anh của IT làm gì đâu em?"*
- **Phân tích kỹ thuật**:
  - `companyJobPositionValidation.js` (dòng 21, 31) và `JobPositionService.cs` (dòng 249, 280, 558, 560, 572, 575) kẹp `(Title)` và `(Quantity)` vào thông báo lỗi.
- **Mức ưu tiên**: **P3** (Thông báo giao diện chưa tối ưu).
- **Trạng thái**: **ĐÃ XỬ LÝ** (Commit: `40ff5a9`). Bỏ triệt để các mã định danh kỹ thuật `(Title)` và `(Quantity)`.

---

### FB-03: Dán link CV có khoảng trắng ở đầu hoặc cuối bị hệ thống chặn nộp đơn
- **Người phản hồi**: `SV-02` (Sinh viên dùng điện thoại di động).
- **Kênh / Màn hình quan sát**: Màn hình Ứng tuyển vị trí thực tập (`/job-positions/:id/apply`) và API `POST /api/placement-requests`, `POST /api/me/placement-requests`.
- **Cách phát hiện**: **Chạy thật trên hệ thống** (copy link Google Drive dán trên mobile thường dính khoảng trắng ở cuối).
- **Lời người dùng phản hồi**:
  > *"Em copy link Google Drive CV từ Zalo sang dán vào form, bấm Gửi nó cứ báo lỗi đỏ 'Liên kết CV phải là URL http:// hoặc https: hợp lệ và không chứa khoảng trắng'. Em xóa đi dán lại mấy lần mới phát hiện do bị dính dấu cách ở đuôi, app tự xóa hộ em khoảng trắng ở hai đầu được không?"*
- **Phân tích kỹ thuật**:
  - Frontend `applicationValidation.js` và backend `CreatePlacementRequestDtoValidator.cs` kiểm tra `value.Any(char.IsWhiteSpace)`. Dẫn đến URL hợp lệ nhưng dính dấu cách đầu/cuối bị chặn 400.
  - Tuy nhiên, khoảng trắng ở giữa URL, URL tương đối (`/cv/abc.pdf`) và scheme nguy hiểm (`javascript:`) vẫn phải bị từ chối triệt để vì lý do an toàn.
  - Backend phải lưu giá trị đã cắt sạch sẽ (trim) vào CSDL.
- **Mức ưu tiên**: **P3** (Validation cản trở người dùng di động thao tác nhanh).
- **Trạng thái**: **ĐÃ XỬ LÝ** (Commit: `838f6de`). Hỗ trợ trim khoảng trắng ở hai đầu cả frontend và backend, lưu URL sạch vào CSDL; từ chối mọi trường hợp URL tương đối, `javascript:`, và khoảng trắng ở giữa.

---

### FB-04: Trang tài liệu README.md bị lỗi hiển thị tiếng Việt (Mojibake)
- **Người phản hồi**: `NM-01` (Lập trình viên / Người mới tiếp cận dự án).
- **Kênh / Màn hình quan sát**: File tài liệu `InternshipManagement/README.md` dòng cuối cùng.
- **Cách phát hiện**: **Đọc code và kiểm tra file trực tiếp** trong thư mục repo.
- **Lời người dùng phản hồi**:
  > *"Em mở file README.md đọc hướng dẫn cài đặt, thấy dòng cuối cùng bị lỗi font thành 'Há»‡ thá»‘ng quáº£n lÃ½ sinh viÃªn thá»±c táº­p...'. Nhìn thiếu chuyên nghiệp quá anh ơi."*
- **Phân tích kỹ thuật**:
  - Dòng 9 của file `InternshipManagement/README.md` bị lưu sai mã hóa (UTF-8 bị đọc nhầm thành Windows-1252 trước đó).
- **Mức ưu tiên**: **P4** (Thẩm mỹ tài liệu, nợ kỹ thuật nhỏ).
- **Trạng thái**: **ĐÃ XỬ LÝ** (Commit: `381d0eb`). Lưu lại đúng chuẩn UTF-8 không BOM: *"Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3)."*.

---

### FB-05: Đề xuất sinh viên được phép nộp đơn không giới hạn số lượng cùng lúc
- **Người phản hồi**: `SV-01` (Sinh viên năm cuối mong muốn rải hồ sơ).
- **Kênh / Màn hình quan sát**: Quy định nộp đơn tại `PlacementRequestService.cs` (`MaxOpenApplicationsPerStudent = 5`).
- **Cách phát hiện**: **Đọc code và quy tắc nghiệp vụ**.
- **Lời người dùng phản hồi**:
  > *"Cho em nộp thoải mái 20-30 công ty một lúc đi anh, giới hạn 5 công ty làm em phải chờ duyệt lâu quá, nhỡ rớt hết thì sao?"*
- **Phân tích kỹ thuật**:
  - Yêu cầu này **nằm ngoài phạm vi và đi ngược lại chính sách quản lý** của nhà trường. Việc giới hạn 5 đơn mở nhằm tránh tình trạng spam hồ sơ ảo, giữ uy tín với các doanh nghiệp tiếp nhận thực tập.
- **Mức ưu tiên**: **P4** (Yêu cầu tính năng không phù hợp).
- **Trạng thái**: **CHƯA XỬ LÝ (TỪ CHỐI)**. Lý do: Vi phạm chính sách đào tạo và quy chế quản lý thực tập sinh của trường; giới hạn 5 đơn là thiết kế có chủ đích.

---

### FB-06: Đòi bỏ bước bắt buộc đổi mật khẩu khi đăng nhập lần đầu
- **Người phản hồi**: `NM-01` (Người mới chưa quen hệ thống).
- **Kênh / Màn hình quan sát**: Luồng đăng nhập lần đầu (`/change-password` khi `mustChangePassword = true`).
- **Cách phát hiện**: **Chạy thật trên hệ thống** (đăng nhập bằng tài khoản sinh viên mới được cấp).
- **Lời người dùng phản hồi**:
  > *"Trường phát tài khoản thì cứ để em dùng luôn mật khẩu đó đi, sao bắt em phải đổi mật khẩu mới cho vào trang chủ? Phiền phức quá!"*
- **Phân tích kỹ thuật**:
  - Yêu cầu này **xung đột trực tiếp với chính sách an toàn thông tin** của hệ thống. Mật khẩu ban đầu được tạo tự động và cấp qua email/danh sách công khai; nếu không bắt buộc đổi, nguy cơ tài khoản bị chiếm đoạt rất cao.
- **Mức ưu tiên**: **P4** (Xung đột an toàn thông tin).
- **Trạng thái**: **CHƯA XỬ LÝ (TỪ CHỐI)**. Lý do: Vi phạm nguyên tắc an toàn bảo mật bắt buộc của hệ thống.

---

### FB-07: Yêu cầu thông báo qua tin nhắn SMS về điện thoại khi có lịch phỏng vấn
- **Người phản hồi**: `SV-02` (Sinh viên hay quên kiểm tra email).
- **Kênh / Màn hình quan sát**: Luồng tiếp nhận thông báo ứng tuyển.
- **Cách phát hiện**: **Đọc code và khảo sát tính năng**.
- **Lời người dùng phản hồi**:
  > *"Em ít khi check mail lắm, công ty hẹn phỏng vấn thì bắn SMS qua số điện thoại của em cho nhanh được không?"*
- **Phân tích kỹ thuật**:
  - Tính năng SMS Gateway tích hợp mạng viễn thông đòi hỏi chi phí dịch vụ, đăng ký Brandname và nằm ngoài phạm vi đồ án hiện tại.
- **Mức ưu tiên**: **P4** (Tính năng lớn, ngoài phạm vi đề tài).
- **Trạng thái**: **CHƯA XỬ LÝ (ĐỂ LẠI LÀM SAU)**. Lý do: Sẽ tạo phiếu theo dõi (backlog ticket) trong tương lai nếu có ngân sách và yêu cầu mở rộng từ nhà trường.

---

### FB-08: Giao diện màn hình tạo sinh viên nhìn "hơi đơn điệu"
- **Người phản hồi**: `AD-01` (Admin).
- **Kênh / Màn hình quan sát**: Giao diện chung.
- **Cách phát hiện**: **Quan sát giao diện**.
- **Lời người dùng phản hồi**:
  > *"Nhìn giao diện form này cứ thấy nó sao sao á, chưa được thuận mắt lắm, thêm hiệu ứng gì cho nó hiện đại xịn sò lên xem nào."*
- **Phân tích kỹ thuật**:
  - Phản hồi **mơ hồ**, không nêu rõ cần thay đổi thuộc tính CSS, layout, màu sắc hay hành vi cụ thể nào; phong cách thiết kế hiện tại đang tuân thủ Design System PrimeVue Aura.
- **Mức ưu tiên**: **P4** (Mơ hồ, thẩm mỹ chủ quan).
- **Trạng thái**: **CHƯA XỬ LÝ (CẦN LÀM RÕ)**. Lý do: Phản hồi quá mơ hồ, cần phỏng vấn thêm để xác định mockup hoặc yêu cầu cụ thể trước khi thực hiện.

---

### FB-09: Tự động điền dữ liệu doanh nghiệp từ Mã số thuế qua cổng thông tin quốc gia
- **Người phản hồi**: `DN-01` (Doanh nghiệp).
- **Kênh / Màn hình quan sát**: Màn hình Đăng ký / Cập nhật hồ sơ doanh nghiệp.
- **Cách phát hiện**: **Khảo sát luồng nhập liệu doanh nghiệp**.
- **Lời người dùng phản hồi**:
  > *"Chị nhập Mã số thuế rồi, hệ thống có tự gọi API sang Tổng cục Thuế để lấy tên công ty, địa chỉ, người đại diện luôn không, đỡ phải gõ tay?"*
- **Phân tích kỹ thuật**:
  - Yêu cầu hợp lý về mặt trải nghiệm nhưng tốn công tích hợp bên thứ ba (third-party taxpayer API), cần chi phí duy trì token API và xử lý độ trễ mạng.
- **Mức ưu tiên**: **P3** (Trải nghiệm nâng cao, khối lượng công việc lớn).
- **Trạng thái**: **CHƯA XỬ LÝ (ĐỂ LẠI LÀM SAU)**. Lý do: Hợp lý nhưng tốn nhiều thời gian và công sức, xếp vào kế hoạch nâng cấp Phase 3.

---

### FB-10: Bấm nộp đơn hai lần liên tiếp (double click) gây race condition tạo 2 đơn trùng lặp
- **Người phản hồi**: `SV-01` (Sinh viên thao tác chuột nhanh hoặc mạng chập chờn).
- **Kênh / Màn hình quan sát**: Ca kiểm thử **TC-12** trong `InternshipManagement/docs/evidence/TEST-CASES.md`, endpoint `POST /api/placement-requests` và `POST /api/me/placement-requests`.
- **Cách phát hiện**: **Chạy thật trên hệ thống** (gửi đồng thời 2 HTTP POST request từ 2 tiến trình song song).
- **Lời người dùng phản hồi**:
  > *"Hôm nọ em bấm nộp đơn, thấy nút chưa chuyển ngay nên em click đúp chuột 2 lần. Lúc vào danh sách đơn của em thì thấy hiện 2 dòng giống hệt nhau cùng một vị trí của công ty đó luôn!"*
- **Phân tích kỹ thuật**:
  - Trong `PlacementRequestService.CreateRequestAsync`, thao tác kiểm tra đơn trùng `hasDuplicateOpenRequest` chạy bên ngoài transaction và không có row lock.
  - Khi 2 request đến cùng thời điểm mili-giây, cả hai đều vượt qua bước kiểm tra trùng lặp và cùng gọi `_db.PlacementRequests.Add(request)` dẫn đến tạo 2 bản ghi đơn mở cho cùng 1 sinh viên tại 1 vị trí.
- **Mức ưu tiên**: **P2** (Lỗi bất đồng bộ / race condition làm sai lệch dữ liệu bảng nghiệp vụ chính).
- **Trạng thái**: **ĐÃ XỬ LÝ** (Commit: `21d7ded`). Đóng gói transaction, thực hiện row lock trên bảng `Students` (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}`) tương tự như `PlacementAcceptanceService`, đồng thời khóa trạng thái nút bấm `saving` ngay lập tức ở frontend.

---

## 4. Bảng tổng hợp Phân loại và Quyết định Xử lý

| Mã FB | Người phản hồi | Mức ưu tiên | Bản chất phát hiện | Quyết định | Lý do / Biện pháp |
| :--- | :--- | :---: | :--- | :---: | :--- |
| **FB-01** | `AD-01` | **P3** | Chạy thật API & UI | **ĐÃ SỬA** | Chuẩn hóa thông báo lỗi sang tiếng Việt thân thiện, không kẹp tên biến tiếng Anh. |
| **FB-02** | `DN-01` | **P3** | Chạy thật API | **ĐÃ SỬA** | Xóa bỏ `(Title)` và `(Quantity)` khỏi thông báo xác thực. |
| **FB-03** | `SV-02` | **P3** | Chạy thật API & Mobile | **ĐÃ SỬA** | Trim khoảng trắng 2 đầu CV URL; chặn an toàn `javascript:`, URL tương đối, khoảng trắng ở giữa. |
| **FB-04** | `NM-01` | **P4** | Đọc code & file | **ĐÃ SỬA** | Sửa triệt để lỗi mã hóa mojibake tiếng Việt dòng cuối README.md về UTF-8 chuẩn. |
| **FB-05** | `SV-01` | **P4** | Đọc quy tắc | **CHƯA SỬA** | Từ chối: Vi phạm chính sách giới hạn 5 đơn để tránh spam doanh nghiệp. |
| **FB-06** | `NM-01` | **P4** | Chạy thật luồng login | **CHƯA SỬA** | Từ chối: Xung đột với chính sách an toàn bảo mật bắt buộc đổi mật khẩu lần đầu. |
| **FB-07** | `SV-02` | **P4** | Khảo sát nhu cầu | **CHƯA SỬA** | Để lại sau: Tốn chi phí viễn thông, ngoài phạm vi đồ án; sẽ tạo phiếu theo dõi sau. |
| **FB-08** | `AD-01` | **P4** | Quan sát giao diện | **CHƯA SỬA** | Cần làm rõ: Phản hồi quá mơ hồ, cần phỏng vấn thêm để có yêu cầu chi tiết. |
| **FB-09** | `DN-01` | **P3** | Khảo sát luồng nhập | **CHƯA SỬA** | Để lại sau: Khối lượng công việc lớn (tích hợp API Thuế), dời sang Phase 3. |
| **FB-10** | `SV-01` | **P2** | Chạy thật ca TC-12 | **ĐÃ SỬA** | Sửa lỗi race condition bằng Database Transaction + Row Lock và disable button tại frontend. |

**Thống kê kết quả**:
- Tổng số phản hồi thu thập: **10 phản hồi** (FB-01 đến FB-10).
- Số phản hồi đã chọn và xử lý dứt điểm: **5 phản hồi** (FB-01, FB-02, FB-03, FB-04, FB-10) — vượt mục tiêu tối thiểu 3 phản hồi.
- Số phản hồi từ chối / để lại sau kèm lý do minh bạch: **5 phản hồi** (FB-05, FB-06, FB-07, FB-08, FB-09).
