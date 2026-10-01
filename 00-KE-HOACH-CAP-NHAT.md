# KẾ HOẠCH CẬP NHẬT: ROLE DOANH NGHIỆP + LUỒNG ỨNG TUYỂN MỚI

> Dự án: Hệ thống Quản lý Sinh viên Thực tập (StudentInternshipMgmt)
> Mục đích file này: (1) gửi cho Claude để hiểu toàn bộ dự án và hướng cập nhật, (2) làm căn cứ để Claude soạn LỆNH cho AI agent code theo từng bước, (3) làm mốc để Claude tạo file tiến độ `PROGRESS-STEP-XX.md` sau mỗi bước.
> Quy ước: mục nào ghi **[CẦN XÁC NHẬN]** là chưa chốt hoặc chưa kiểm chứng bằng code thật. AI agent phải đọc code thật để xác nhận trước khi làm.

---

## 0. VAI TRÒ VÀ QUY TRÌNH LÀM VIỆC

### 0.1. Vai trò

| Bên | Vai trò |
|---|---|
| **Phats** | Người ra lệnh (senior). Dán lệnh cho AI agent, build và chạy thử local, gửi kết quả cho Claude, chốt các quyết định mở. |
| **Claude** | Senior fullstack (.NET + Vue). Soạn **LỆNH** gửi AI agent và **CHECKLIST REVIEW** (giữ riêng), review diff theo checklist, cập nhật tài liệu. **Không tự viết code trực tiếp** trong quy trình này. Sau khi mỗi bước đạt thì tạo file `PROGRESS-STEP-XX.md`. |
| **AI agent code** (GitHub Copilot, Claude Code, Cursor...) | Thực thi từng lệnh. Bắt buộc **đọc file thật trước khi sửa**, không đoán namespace hay tên method, không làm ngoài phạm vi của bước, build phải chạy được, báo cáo rõ file đã thêm hoặc sửa. |

### 0.2. Quy trình mỗi bước

1. Claude soạn LỆNH cho bước đó (Phats gửi kèm các file thật mà Claude yêu cầu).
2. Phats dán LỆNH vào AI agent.
3. AI agent làm xong, Phats build và chạy thử local.
4. Phats gửi lại diff hoặc kết quả cho Claude.
5. Claude review theo CHECKLIST REVIEW. Nếu lỗi thì soạn lệnh sửa, lặp lại bước 2 đến 5.
6. Đạt thì Claude tạo file `PROGRESS-STEP-XX.md` (mỗi lần một file mới, không ghi đè file cũ).

### 0.3. Quy ước bắt buộc (đã dùng xuyên suốt dự án)

- Tách riêng **LỆNH** (gửi agent) và **CHECKLIST REVIEW** (Claude giữ).
- Giai đoạn lớn thì tách lệnh theo từng entity.
- Luôn đính kèm file mẫu thật (entity, DbContext, Controller, Service) trước khi soạn lệnh giai đoạn mới, vì agent hay đoán sai namespace và tên method khi thiếu file thật.
- Không đưa entity hoặc logic của bước sau vào lệnh của bước hiện tại.
- Migration: chạy `dotnet ef migrations add` để kiểm tra trước; nếu báo "No changes were detected" thì `dotnet ef migrations remove`.
- Không thêm package mới nếu chưa nêu trong lệnh.

---

## 1. BỐI CẢNH DỰ ÁN (TÓM TẮT)

### 1.1. Công nghệ

- **Backend:** .NET 8, kiến trúc 4 lớp `Domain ← Application ← Infrastructure ← Api`, namespace `StudentInternshipMgmt.*`, SQL Server, EF Core Code-First.
- **Frontend:** Vue 3 (Vite), Pinia, Vue Router, Axios, PrimeVue v4 (theme Aura, màu chủ đạo indigo), dayjs, chart.js.
- **Response chung:** `ApiResponse<T>` (`SuccessResponse` / `FailResponse`), `PagedResult<T>` qua `.ToPagedResultAsync(filter)`, `PaginationParams`.
- **Xác thực:** JWT access + refresh token. Claim có Role, StudentId, UniqueName. `GET /api/auth/me` trả `MeResponseDto` (UserId, Username, Role, StudentId, MustChangePassword). Frontend gọi `fetchMe()` khi khởi động.
- **Validate:** chưa đồng bộ. `StudentService` dùng FluentValidation; `CompanyService` và `JobPositionService` validate tay trả tuple `(success, error, data)`. Chưa quyết định có refactor hay không. **Không refactor trong đợt này** trừ khi lệnh yêu cầu.

### 1.2. Dữ liệu chính

Bảng: `Students`, `Companies`, `JobPositions`, `Users`, `PlacementRequests`, `StatusHistory`.

Enum hiện có:
- `StudentStatus`: NoCompany → Introduced → Interviewed → Accepted → Interning → Completed (cho phép đi lùi). Enum được serialize dạng **số 0 đến 5**, không phải chuỗi (backend không cấu hình `JsonStringEnumConverter`).
- `RequestStatus`: Pending / Approved / Rejected.
- `UserRole`: Admin / User.

Bảng `Companies` hiện có các cột: Id, Name, Address, Industry, ContactPerson, ContactEmail, ContactPhone, ContactPosition. **Ba cột ContactEmail, ContactPhone, ContactPosition hiện NULL ở toàn bộ các dòng.** Có vài dòng dữ liệu test (Id 16, 17, 1016; dòng 1016 có email nằm nhầm ở cột ContactPerson) cần dọn.

### 1.3. Các giai đoạn đã hoàn thành

| Giai đoạn | Nội dung |
|---|---|
| 1 | Nền tảng: entities, DbContext, ApiResponse/PagedResult, middleware, seed Admin |
| 2 | Auth JWT access + refresh, đổi mật khẩu lần đầu, policy `SameStudentOnly` |
| 3 | API Sinh viên (Admin): CRUD, tìm kiếm/lọc/phân trang, xóa mềm, tạo tài khoản hàng loạt, import Excel/CSV, đổi trạng thái + `StatusHistory` |
| 4a | API Companies CRUD (chặn xóa nếu còn Student/JobPosition liên kết) |
| 4b | API JobPositions CRUD (có `AcceptedCount`, PUT chặn giảm `Quantity` dưới `AcceptedCount`, DELETE chặn nếu còn Student gắn, PATCH toggle-open) |
| 5A | PlacementRequest: tạo yêu cầu (chỉ khi NoCompany, chặn trùng Pending), Admin duyệt/từ chối, `AssignCompanyDirectAsync` (gán trực tiếp) |
| 5B | `ChangeStatusAsync(studentId, dto, changedByUserId, isAdmin)`, whitelist đổi trạng thái cho sinh viên, `MeController` (`api/me`, `Roles=User`) |
| 6A | Dashboard (summary, danh sách sinh viên cần gán DN theo `PlacementReminderDays`) |
| 6B | Export sinh viên CSV/XLSX |
| 7 | Frontend nền tảng: axios + interceptor refresh, Pinia auth store, router + guard theo role, layout PrimeVue |
| 8-1 trở đi | Frontend: danh sách sinh viên, chi tiết sinh viên, tạo tài khoản, doanh nghiệp, vị trí thực tập, duyệt yêu cầu, thông báo (admin), hồ sơ sinh viên, giới thiệu, đăng nhập theo thiết kế Figma |

**[CẦN XÁC NHẬN]** Trạng thái thật của repo backend (một số giai đoạn ghi "đang chờ build local"). Agent kiểm tra build trước khi bắt đầu STEP 01.

---

## 2. HIỆN TRẠNG CẦN THAY ĐỔI (AS-IS)

1. Hệ thống chỉ có 2 role (Admin, User = sinh viên). Doanh nghiệp không có tài khoản, mọi dữ liệu công ty và vị trí do Admin nhập.
2. Luồng cũ: sinh viên tạo `PlacementRequest` (Pending) → Admin duyệt/từ chối hoặc gán trực tiếp → sinh viên tự đổi trạng thái theo whitelist (Introduced ↔ Interviewed, Accepted → Interning) qua `MeController`.
3. Frontend phân quyền **nhị phân** admin / không phải admin:
   - `MainLayout.vue`: `isAdmin ? adminMenu : userMenu` (và tương tự cho tab mobile).
   - `router/index.js`: `defaultRouteForRole` chỉ trả `/dashboard` hoặc `/profile`.
   - Route `/companies`, `/job-positions`, `/about` chỉ có `requiresAuth`, không giới hạn role.
   - `stores/auth.js`: chỉ có `isAdmin`; `buildUserFromPayload` chưa có `companyId`.
4. Chưa có cơ chế gửi email, chưa có tác vụ nền (BackgroundService).

---

## 3. QUYẾT ĐỊNH THIẾT KẾ

### 3.1. Đã chốt

| Mã | Quyết định |
|---|---|
| **D1** | Thêm role `Company`. Thêm `Users.CompanyId` (nullable, FK tới `Companies`). **1 doanh nghiệp = 1 tài khoản.** Tài khoản do **Admin tạo**, mật khẩu ngẫu nhiên, bắt đổi mật khẩu lần đầu (cùng cơ chế sinh viên). Không có tự đăng ký. |
| **D2** | `CompanyId` luôn lấy từ **JWT claim**, không nhận từ client. Thêm policy `SameCompanyOnly`. Controller riêng cho doanh nghiệp (dự kiến `api/company-me`, `Roles=Company`). |
| **D3** | **Sinh viên không tham gia vào trạng thái.** Bỏ endpoint và logic đổi trạng thái của sinh viên (`change-status` trong `MeController`, whitelist trong `ChangeStatusAsync`). Doanh nghiệp (hoặc Admin) quyết định. |
| **D4** | Doanh nghiệp xem được ai ứng tuyển vào vị trí của mình và có thể: từ chối, mời phỏng vấn, ghi kết quả phỏng vấn. Chỉ xem thông tin cần thiết của sinh viên (họ tên, ngành, lớp, trạng thái), không xem dữ liệu của công ty khác. |
| **D5** | **Đơn ứng tuyển = mở rộng `PlacementRequest`** (không tạo bảng mới). Trạng thái đơn: `Pending → InterviewInvited → Accepted`, hoặc `Rejected`, `Expired`. Doanh nghiệp có thể đi tắt từ Pending sang kết quả nếu phỏng vấn ngoài hệ thống. |
| **D6** | Khi doanh nghiệp ghi kết quả **"Đạt"**, hệ thống tự chạy cả chuỗi (trong 1 transaction): đơn → `Accepted`; `Student.Status` → `Accepted` và set `CompanyId`, `JobPositionId`; tự hủy (chuyển `Rejected`/hủy) các đơn mở khác của sinh viên; tăng `AcceptedCount`; đóng vị trí (`IsOpen = false`) nếu đủ chỉ tiêu; ghi `StatusHistory`; tạo thông báo cho sinh viên và Admin. **Không bao giờ tự nhận sinh viên chỉ vì quá hạn.** |
| **D7** | **`Student.Status` giữ `NoCompany` cho tới khi có doanh nghiệp nhận.** Tiến trình phỏng vấn nằm ở từng đơn. `Introduced` và `Interviewed` không còn do hệ thống ghi vào sinh viên (nếu cần hiển thị "tiến độ cao nhất" thì tính khi truy vấn từ các đơn đang mở). Lý do: sinh viên nộp nhiều đơn song song nên `Student` chỉ có một `CompanyId` không biểu diễn được tiến trình theo từng công ty. |
| **D8** | Sinh viên **được nộp nhiều doanh nghiệp cùng lúc**. Giới hạn số đơn đang mở mỗi sinh viên (mặc định 5, cấu hình `appsettings`). Chặn trùng chỉ áp dụng cho đơn đang mở (Pending, InterviewInvited); đơn Rejected/Expired được nộp lại. |
| **D9** | **Chống kẹt bằng hạn phản hồi.** Đơn có `DueAt`. Mốc mặc định (cấu hình được): ngày 3 nhắc doanh nghiệp; ngày 5 hiện ở mục "Đơn quá hạn" của Admin; ngày 7 đơn Pending tự chuyển `Expired` (người thực hiện: "Hệ thống", ghi `StatusHistory`). `Expired` là trạng thái cuối của **đơn**, sinh viên vẫn tự do nộp nơi khác. Tác vụ chạy bằng `BackgroundService`. Quá hạn được tính khi truy vấn (`hiện tại > DueAt`) cho Dashboard và nhắc nhở. |
| **D10** | **Sau phỏng vấn không tự hủy đơn.** Nếu doanh nghiệp đã mời phỏng vấn mà quá hạn ghi kết quả thì chỉ nhắc và báo Admin (mục "Chờ kết quả phỏng vấn"), không tự chuyển rớt hay đậu. |
| **D11** | **Link xác nhận qua email** để doanh nghiệp không cần đăng nhập: email có nút "Đạt" / "Không đạt" (và nút phản hồi cho đơn Pending). Token ký số, gắn đúng 1 đơn, có hạn, dùng 1 lần. Mở link chỉ hiện **trang xác nhận có nút gửi (POST)**, không xử lý ngay khi mở link (tránh phần mềm quét email tự bấm). Gửi mail bằng MailKit qua SMTP; môi trường dev ghi link ra log. |
| **D12** | **Đường lui khi doanh nghiệp im lặng:** sinh viên bấm "Báo đã phỏng vấn" (chỉ là **cờ báo hiệu**, không đổi bất kỳ trạng thái nào; mỗi đơn báo 1 lần; chỉ báo được khi đơn ở `InterviewInvited`) → đơn hiện ở mục "Chờ xác nhận" của Admin → Admin liên hệ doanh nghiệp rồi chọn **Đạt / Không đạt / Bác báo cáo**. Bắt buộc nhập **nguồn xác nhận**; `StatusHistory` ghi rõ "Admin thực hiện thay doanh nghiệp". |
| **D13** | Admin vẫn giữ quyền gán trực tiếp (`AssignCompanyDirectAsync`) và ghi đè. Chỉ Admin được hủy kết quả `Accepted`. |
| **D14** | Frontend chuyển từ logic nhị phân sang **map role → menu / trang mặc định / route được phép**. |

### 3.2. Còn mở **[CẦN XÁC NHẬN]**

| Mã | Câu hỏi | Mặc định đề xuất |
|---|---|---|
| **O1** | Dữ liệu `PlacementRequest` cũ (Pending/Approved/Rejected) chuyển sang trạng thái mới thế nào? | Pending giữ nguyên (gán `DueAt` mới); Approved → `Accepted` nếu sinh viên đã có CompanyId; Rejected giữ nguyên. Agent đề xuất mapping cụ thể ở STEP 03 trước khi chạy migration. |
| **O2** | Tin tuyển dụng của doanh nghiệp có cần Admin duyệt trước khi hiện cho sinh viên không? | Có: `JobPosition.ApprovalStatus` (Draft/PendingApproval/Approved/Rejected), sinh viên chỉ thấy `Approved` và `IsOpen`. |
| **O3** | Ai bấm `Interning` và `Completed`? | Admin bấm; có thể mở thêm cho doanh nghiệp xác nhận kết thúc. |
| **O4** | Quá hạn 7 ngày thì tự chuyển `Expired` hay chỉ cảnh báo để Admin quyết định? | Tự chuyển `Expired` cho đơn **Pending** (không áp dụng cho đơn đã mời phỏng vấn, xem D10). |
| **O5** | Email nhận link xác nhận lấy từ đâu? | Dùng cột `Companies.ContactEmail` đã có. Bắt buộc có email hợp lệ khi tạo tài khoản doanh nghiệp. Dữ liệu cũ đang NULL nên Admin phải bổ sung. |

---

## 4. KẾ HOẠCH CÁC BƯỚC

Mỗi bước = 1 LỆNH cho AI agent + 1 CHECKLIST REVIEW + 1 file `PROGRESS-STEP-XX.md` khi đạt.

### Phần A: Backend

**STEP 01: Nền tảng role Company**
- Thêm `Company` vào `UserRole`. Thêm `Users.CompanyId` (nullable, FK `Companies`) + migration.
- JWT thêm claim `CompanyId`. `GET /api/auth/me` trả thêm `CompanyId`.
- Thêm policy `SameCompanyOnly`.
- Rà soát mọi chỗ dùng `Roles = "Admin"` / `"User"` và các nơi giả định chỉ có 2 role.
- Xong khi: build sạch, đăng nhập bằng tài khoản Company (tạo tay trong DB để thử) nhận được JWT đúng claim, `auth/me` đúng.

**STEP 02: Admin quản lý tài khoản doanh nghiệp**
- API Admin: tạo tài khoản cho một Company (mật khẩu ngẫu nhiên, `MustChangePassword = true`), đặt lại mật khẩu, vô hiệu hóa.
- Validate `ContactEmail` (bắt buộc, hợp lệ) khi tạo tài khoản. Quyết định O5.
- Dọn dữ liệu test trong `Companies`.
- Xong khi: Admin tạo được tài khoản Company, doanh nghiệp đăng nhập và bị bắt đổi mật khẩu lần đầu.

**STEP 03: Thiết kế lại dữ liệu đơn ứng tuyển**
- Mở rộng `PlacementRequest` theo D5, D9, D12: trạng thái mới, `DueAt`, thông tin lịch phỏng vấn (thời gian, địa điểm/link, ghi chú), ghi chú kết quả, cờ sinh viên báo đã phỏng vấn (thời điểm + ghi chú), loại người thực hiện (Doanh nghiệp / Admin / Hệ thống) và nguồn xác nhận.
- `StatusHistory` thêm loại người thực hiện.
- Migration + chuyển dữ liệu cũ theo O1. Agent đề xuất mapping và **chờ Claude duyệt** trước khi chạy.
- Xong khi: migration chạy được trên DB hiện có, dữ liệu cũ không mất.

**STEP 04: Luồng ứng tuyển phía sinh viên (backend)**
- Nộp nhiều đơn, giới hạn số đơn mở, chặn trùng đơn mở (D8). Xem danh sách đơn của mình. "Báo đã phỏng vấn" (D12).
- **Gỡ** `change-status` của sinh viên khỏi `MeController` và bỏ whitelist sinh viên trong `ChangeStatusAsync` (D3). Cập nhật các nơi gọi.
- Xong khi: sinh viên không còn đổi được trạng thái, nộp nhiều đơn hoạt động đúng giới hạn.

**STEP 05: API doanh nghiệp**
- `CompanyMeController` (`Roles=Company`, CompanyId từ claim): hồ sơ doanh nghiệp, CRUD `JobPosition` của mình (kèm `ApprovalStatus` nếu chốt O2), danh sách đơn ứng tuyển vào vị trí của mình, từ chối / mời phỏng vấn / ghi kết quả.
- Chuỗi tự động khi "Đạt" (D6) chạy trong 1 transaction, có kiểm thử ca biên.
- Xong khi: doanh nghiệp A không đọc hay sửa được dữ liệu của doanh nghiệp B (kiểm tra bằng 2 tài khoản).

**STEP 06: API Admin cho luồng mới**
- Mục "Chờ xác nhận" và ghi nhận thay doanh nghiệp (D12), duyệt tin tuyển dụng (nếu chốt O2), danh sách đơn quá hạn cho Dashboard, hủy kết quả Accepted (D13).
- Điều chỉnh Admin duyệt `PlacementRequest` cũ theo luồng mới (bỏ khỏi luồng chính, giữ gán trực tiếp).
- Xong khi: Admin xử lý được toàn bộ ca "doanh nghiệp im lặng".

**STEP 07: Hạn phản hồi, nhắc nhở, tác vụ nền**
- `BackgroundService` chạy hằng ngày: nhắc, đánh dấu quá hạn, chuyển `Expired` theo O4. Mốc ngày cấu hình trong `appsettings` (cùng kiểu `PlacementReminderDays`).
- Tạo thông báo qua module Notifications hiện có (mở rộng cho doanh nghiệp).
- Xong khi: đổi mốc ngày xuống vài phút trong cấu hình thì thấy đúng hành vi.

**STEP 08: Email và link xác nhận**
- Dịch vụ gửi mail (MailKit, SMTP cấu hình được; dev ghi log). Token ký số (hết hạn, dùng 1 lần). Endpoint công khai hiển thị thông tin + endpoint POST xác nhận (D11).
- Xong khi: bấm link trong email ghi được kết quả; link dùng lại lần 2 hoặc quá hạn bị từ chối.

### Phần B: Frontend

**STEP 09: Nền frontend theo role**
- `auth.js`: thêm `isCompany`, `companyId`. `router`: map role → trang mặc định, `meta.roles` cho `/companies`, `/job-positions`, `/about`. `MainLayout.vue`: menu và tab mobile theo role (D14).
- Xong khi: đăng nhập 3 loại tài khoản đều vào đúng trang và đúng menu, không bị đẩy nhầm.

**STEP 10: Giao diện doanh nghiệp**
- Hồ sơ doanh nghiệp, quản lý vị trí, danh sách ứng viên với hành động (từ chối / mời phỏng vấn / ghi kết quả), trang xác nhận công khai cho link email.

**STEP 11: Giao diện sinh viên**
- Nộp đơn nhiều nơi, theo dõi đơn (hiện số ngày đã chờ, lịch phỏng vấn, kết quả, chỉ đọc), "Báo đã phỏng vấn". **Bỏ** các nút "Xác nhận đã phỏng vấn" và "Báo DN không phù hợp" trong `ProfileView`.

**STEP 12: Giao diện Admin cho luồng mới**
- Mục "Chờ xác nhận", "Đơn quá hạn", duyệt tin tuyển dụng, quản lý tài khoản doanh nghiệp, điều chỉnh màn "Duyệt yêu cầu".

### Phần C: Hoàn thiện

**STEP 13: Kiểm thử tổng thể và dọn dẹp**
- Chạy toàn bộ kịch bản mục 5. Dọn code thừa, cập nhật `PROJECT_CONTEXT.md` (18 quyết định thiết kế cũ cộng các quyết định D1 đến D14).

---

## 5. CA BIÊN BẮT BUỘC KIỂM THỬ

1. Sinh viên có 3 đơn mở; doanh nghiệp A ghi "Đạt" → 2 đơn còn lại tự hủy, `Student.Status = Accepted`, `CompanyId` đúng.
2. Hai doanh nghiệp ghi "Đạt" cho cùng sinh viên gần như cùng lúc → chỉ một bên thành công (xử lý đồng thời), bên kia nhận lỗi rõ ràng.
3. Vị trí còn 1 chỉ tiêu, hai sinh viên cùng được ghi "Đạt" → chỉ một thành công; đạt chỉ tiêu thì `IsOpen = false`.
4. Doanh nghiệp A gọi API với id đơn/vị trí của doanh nghiệp B → bị từ chối (403/404), không lộ dữ liệu.
5. Gửi `CompanyId` khác trong body hay query → bị bỏ qua, luôn dùng claim.
6. Đơn Pending quá 7 ngày → `Expired`; sinh viên nộp lại được. Đơn `InterviewInvited` quá hạn ghi kết quả → không tự đổi, chỉ báo Admin.
7. Sinh viên báo "đã phỏng vấn" 2 lần cho cùng đơn → lần 2 bị chặn. Báo khi đơn chưa ở `InterviewInvited` → bị chặn.
8. Link email dùng lần 2, hết hạn hoặc bị sửa token → bị từ chối.
9. Sinh viên gọi API đổi trạng thái cũ → không còn (404/405 hoặc 403).
10. Tài khoản doanh nghiệp vào route của Admin hay của sinh viên → bị chặn đúng.
11. Admin hủy kết quả `Accepted` → trạng thái sinh viên, `AcceptedCount`, vị trí (`IsOpen`) được hoàn lại nhất quán.
12. Xóa mềm sinh viên hoặc vô hiệu hóa tài khoản doanh nghiệp khi còn đơn mở → đơn xử lý đúng, không lỗi.

---

## 6. MẪU FILE TIẾN ĐỘ (Claude tạo sau mỗi bước đạt)

Tên: `PROGRESS-STEP-XX.md` (mỗi lần một file mới).

```
# PROGRESS-STEP-XX: <tên bước>
- Ngày hoàn thành:
- Trạng thái: Đạt / Đạt có ghi chú
## Đã làm
- (danh sách file thêm/sửa, migration, endpoint)
## Kết quả review
- (checklist đạt, lỗi đã phát hiện và đã sửa)
## Quyết định phát sinh
- (nếu có, kèm cập nhật mục O của kế hoạch)
## Việc còn tồn
- (nếu có)
## Hiện tại đang ở
- Đã xong: STEP 01 đến STEP XX
- Bước tiếp theo: STEP XX+1
```

---

## 7. FILE CLAUDE CẦN TRƯỚC KHI SOẠN LỆNH STEP 01

Backend (Claude chưa thấy code backend, chỉ thấy frontend):
- `UserRole` (enum) và entity `User`
- `AppDbContext` (hoặc tên tương ứng) và cấu hình EF của `User`
- `AuthService` và phần sinh JWT (nơi thêm claim)
- `AuthController` (action `login`, `me`)
- Nơi đăng ký policy `SameStudentOnly` (để viết `SameCompanyOnly` đúng kiểu)
- `MeController`
- `appsettings.json` (cấu trúc các section cấu hình hiện có)
- Entity `Company`, `JobPosition`, `PlacementRequest`, `StatusHistory`

Frontend: đã có `frontend.zip` (Claude đã đọc `App.vue`, `main.js`, `router`, `MainLayout.vue`, `auth.js`).
