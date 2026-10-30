# Checklist bảo mật

Phạm vi: mã nguồn hiện tại, các test có trong repository và test tích hợp mới. Ref `before-security-review` không có trong repository hiện tại nên không thể so sánh trước/sau theo tag đó. Các nhận định chưa có test hoặc chưa có cấu hình môi trường thật được đánh dấu riêng, không suy diễn thành kết quả đạt.

## Dữ liệu nhập

| Mã | Điều cần kiểm | Cách kiểm | Kết quả | Bằng chứng |
|---|---|---|---|---|
| SEC-01 | Trường bắt buộc của sinh viên từ chối giá trị trống hoặc toàn khoảng trắng. | `StudentValidatorTests`; `StudentValidators.cs` | Đạt | Test kiểm tra họ tên và lớp học toàn khoảng trắng bị validator từ chối. |
| SEC-02 | Chuỗi nhập có giới hạn độ dài và từ chối ký tự điều khiển không hợp lệ. | `SharedInputSafetyTests`; các DTO có `SafeString` | Đạt | Test kiểm tra vượt độ dài, tab và xuống dòng; DTO tạo/sửa sinh viên, doanh nghiệp và vị trí khai báo giới hạn. |
| SEC-03 | Email sai chính tả và số điện thoại không hợp lệ bị từ chối. | `SharedInputSafetyTests`, `StudentValidatorTests`, `CreatePlacementRequestDtoValidatorTests` | Đạt | Có ca email gõ nhầm tên miền, email sai cấu trúc và số điện thoại sai độ dài/định dạng. |
| SEC-04 | Đơn ứng tuyển từ chối trường bắt buộc toàn khoảng trắng, URL CV không an toàn và nội dung vượt độ dài. | `CreatePlacementRequestDtoValidatorTests` | Đạt | Có ca trắng, URL sai giao thức/khoảng trắng và giới hạn độ dài nội dung. |
| SEC-05 | Chuỗi `<script>alert(1)</script>` được lưu/trả nguyên văn như dữ liệu JSON và giao diện không chèn bằng HTML thô. | `AuthorizationMatrixTests`; tìm `v-html` trong `frontend/src` | Đạt | Test API xác nhận chuỗi không bị biến đổi ở sinh viên, doanh nghiệp, vị trí, đơn ứng tuyển và trang Giới thiệu. Không tìm thấy `v-html`; các chỗ hiển thị nội dung dùng nội suy Vue `{{ }}`. |
| SEC-06 | Giá trị bắt đầu bằng `=`, `+`, `-`, `@`, tab hoặc CR được escape khi xuất bảng tính. | `SharedInputSafetyTests`; `SpreadsheetSafety` | Đạt | Test kiểm tra bốn tiền tố công thức và hai ký tự điều khiển đầu trường. |

## Phân quyền

| Mã | Điều cần kiểm | Cách kiểm | Kết quả | Bằng chứng |
|---|---|---|---|---|
| SEC-07 | Tất cả action API yêu cầu đăng nhập, ngoại trừ đúng đăng nhập và làm mới token. | `AuthorizationMatrixTests.Every_controller_endpoint_requires_authentication_except_login_and_refresh` | Đạt | Test lấy action từ `EndpointDataSource`; ngoại lệ được khai báo rõ là `POST /api/auth/login` và `POST /api/auth/refresh`. |
| SEC-08 | Sinh viên, doanh nghiệp và Admin không dùng được endpoint dành riêng cho vai trò khác. | `AuthorizationMatrixTests.Role_tokens_are_denied_by_endpoints_outside_their_role_and_admin_cannot_use_me_routes` | Đạt | Test duyệt metadata phân quyền của toàn bộ controller endpoint. |
| SEC-09 | Doanh nghiệp không đọc được đơn hoặc vị trí thuộc doanh nghiệp khác bằng cách đổi mã trên URL. | `AuthorizationMatrixTests.Company_cannot_read_another_company_request_or_position_and_student_cannot_report_another_students_request` | Đạt | Test Doanh nghiệp A nhận kết quả không tìm thấy khi hỏi đơn và vị trí của Doanh nghiệp B; truy vấn dịch vụ lọc thêm `CompanyId`. |
| SEC-10 | Sinh viên không thao tác trên đơn của sinh viên khác qua nhóm API tự phục vụ. | Cùng test IDOR; `MeController.cs`; `PlacementRequestService.cs` | Đạt | Thử báo phỏng vấn đơn của Sinh viên B bằng token Sinh viên A trả về không tìm thấy; dịch vụ lọc đồng thời theo mã đơn và `StudentId`. |
| SEC-11 | Tài khoản Admin không truy cập hai nhóm endpoint hồ sơ tự phục vụ. | `AuthorizationMatrixTests.Role_tokens_are_denied_by_endpoints_outside_their_role_and_admin_cannot_use_me_routes` | Đạt | Test duyệt mọi endpoint `/api/me/*` và `/api/company-me/*` với token Admin. |
| SEC-12 | Cờ buộc Admin đổi mật khẩu được thực thi ở backend trước khi cấp quyền quản trị. | `AuthorizationMatrixTests.Admin_who_must_change_password_cannot_use_management_endpoints`; `MustChangePasswordMiddleware.cs`; `AuthService.cs` | Đạt | Token có cờ buộc đổi mật khẩu bị middleware chặn khỏi API quản trị; test tích hợp xác nhận danh sách sinh viên bị từ chối. |

## Mật khẩu và phiên đăng nhập

| Mã | Điều cần kiểm | Cách kiểm | Kết quả | Bằng chứng |
|---|---|---|---|---|
| SEC-13 | Backend từ chối mật khẩu mới rỗng, chỉ có khoảng trắng hoặc không đạt chính sách. | `AuthPasswordChangeTests`; `PasswordPolicy.cs` | Đạt | Test mới xác nhận chuỗi rỗng bị từ chối và mật khẩu hiện tại không bị thay đổi; test hiện có kiểm tra chính sách khi gọi service trực tiếp. |
| SEC-14 | Nhiều lần đăng nhập sai tuần tự sẽ khóa tạm thời và phản hồi thời gian thử lại. | `LoginAttemptTrackerTests.Fifth_failure_locks_next_login_until_15_minutes_pass` | Đạt | Năm lần sai trong cùng bucket bị khóa và sau thời gian chờ có thể đăng nhập lại. |
| SEC-15 | Bộ đếm đăng nhập không thể né bằng cách thay IP hoặc thêm khoảng trắng tên đăng nhập. | `LoginAttemptTrackerTests.Failed_attempts_are_shared_across_ips_and_trim_username`; `InMemoryLoginAttemptTracker.cs` | Đạt | Bộ đếm theo tài khoản đã chuẩn hóa, dùng chung giữa các IP; bộ đếm IP được duy trì riêng. Test xác nhận lần thứ năm sai từ IP khác và username có khoảng trắng vẫn khóa tài khoản. |
| SEC-16 | Kiểm tra khóa và ghi nhận thất bại không để nhiều yêu cầu đồng thời cùng vượt ngưỡng. | `LoginAttemptTrackerTests.Concurrent_login_attempts_reserve_slots_before_authentication`; `AuthController.cs`; `InMemoryLoginAttemptTracker.cs` | Đạt | Mỗi yêu cầu giữ chỗ nguyên tử trước khi gọi xác thực; test đồng thời giữ các lần xác thực đang chờ và xác nhận chỉ năm yêu cầu đi tới dịch vụ xác thực. |
| SEC-17 | Đổi mật khẩu thu hồi toàn bộ refresh token đang hoạt động của tài khoản. | `AuthPasswordChangeTests.Changing_password_revokes_refresh_tokens_and_switches_password` | Đạt | Test service xác nhận token cũ sau đổi mật khẩu không thể làm mới phiên. |
| SEC-18 | Refresh token cũ không thể tái sử dụng sau khi đổi lấy cặp token mới. | `AuthService.RefreshTokenAsync` | Chưa kiểm chứng | Mã nguồn thu hồi token cũ trước khi phát token mới; chưa có test API riêng cho việc phát lại token sau refresh. |

## Dữ liệu cá nhân và nhật ký

| Mã | Điều cần kiểm | Cách kiểm | Kết quả | Bằng chứng |
|---|---|---|---|---|
| SEC-19 | Phản hồi JSON của endpoint không chứa `passwordHash`, `token` hoặc `refreshToken`, ngoại trừ đăng nhập và làm mới token. | `AuthorizationMatrixTests.Json_responses_from_all_controller_endpoints_do_not_expose_secret_fields` | Đạt | Test phát hiện endpoint từ metadata, gọi với token đúng vai trò, quét đệ quy mọi phản hồi JSON và bỏ qua riêng đăng nhập/làm mới token. Các phản hồi thành công khi tạo sinh viên, doanh nghiệp, vị trí, đơn ứng tuyển và cập nhật trang Giới thiệu cũng được quét. |
| SEC-20 | Hồ sơ và đơn của Sinh viên chỉ lộ cho chính Sinh viên ở nhóm `/api/me/*`. | `MeController.cs`; `PlacementRequestService.cs`; test IDOR | Đạt | `StudentId` lấy từ claim và được truyền vào truy vấn hồ sơ, lịch sử, danh sách đơn và thao tác báo phỏng vấn. |
| SEC-21 | Email và số điện thoại liên hệ của doanh nghiệp có thực sự cần trả cho vai trò Sinh viên hay không. | `CompaniesController.cs`; `CompanyService.cs:GetCompaniesAsync` | Chưa kiểm chứng | `GET /api/companies` trả `ContactPhone` và `ContactEmail` cho Sinh viên. Chưa tìm thấy yêu cầu nghiệp vụ xác nhận đây là chủ đích; với luồng nộp đơn nội bộ, các trường này có thể thừa. Đề xuất người phụ trách quyết định có ẩn hai trường ở DTO dành cho Sinh viên hay không. |
| SEC-22 | Doanh nghiệp chỉ nhận thông tin ứng viên của các đơn thuộc doanh nghiệp đó. | `CompanyMeController.cs`; `PlacementRequestService.cs`; test IDOR | Đạt | DTO đơn của doanh nghiệp chứa họ tên, email, số điện thoại, trường/ngành học, liên kết CV và thư ứng tuyển; truy vấn giới hạn theo `CompanyId` trong claim. Các trường liên hệ và hồ sơ cần thiết để xem xét ứng viên, nhưng nên tiếp tục bảo vệ theo quyền hiện tại. |
| SEC-23 | Nhật ký đăng nhập thất bại không ghi username nguyên văn. | `LoginAttemptTrackerTests.Unknown_username_is_counted_and_failed_login_log_omits_username`; `AuthService.cs` | Đạt | Test xác nhận log chỉ chứa một phần hash username và không chứa username đầu vào. |
| SEC-24 | Frontend không lưu dữ liệu cá nhân vào `localStorage`; cần phân biệt token với tùy chọn giao diện. | Tìm `localStorage` và `console.*` trong `frontend/src` | Đạt | Store xác thực lưu access/refresh token; các chỗ khác lưu chế độ xem danh sách doanh nghiệp và tiến độ checklist Giới thiệu. Không thấy mã sinh viên, email, số điện thoại hoặc hồ sơ ứng viên được lưu. Frontend có `console.warn`/`console.error`; chưa thấy `console.log` chứa dữ liệu cá nhân. |

## Cấu hình và phụ thuộc

| Mã | Điều cần kiểm | Cách kiểm | Kết quả | Bằng chứng |
|---|---|---|---|---|
| SEC-25 | Khóa ký JWT được kiểm tra độ dài tối thiểu và không đặt trong `appsettings.json`. | `JwtSettingsValidatorTests`; `JwtSettingsValidator.cs`; `appsettings.json` | Đạt | Startup từ chối khóa thiếu hoặc ngắn; file cấu hình không chứa giá trị khóa. Khóa test chỉ được đặt trong factory kiểm thử. |
| SEC-26 | API có security header và phản hồi API không bị cache. | `SecurityHeadersMiddlewareTests`; `SecurityHeadersMiddleware.cs` | Đạt | Test kiểm tra `nosniff`, chống nhúng frame, referrer policy, permissions policy, CSP theo môi trường và no-cache cho API. |
| SEC-27 | Refresh token được băm trước khi lưu trong cơ sở dữ liệu. | `AuthService.cs`; `RefreshToken` entity | Chưa đạt | Đã chấp nhận rủi ro trong phạm vi này: token hiện được lưu dạng thô. Thay đổi cần thiết kế migration; đề xuất lưu SHA-256 ở phiên bản sau. |
| SEC-28 | Access token và refresh token không được lưu trong `localStorage`. | `frontend/src/stores/auth.js` | Chưa đạt | Đã chấp nhận rủi ro trong phạm vi này: cả hai token hiện được lưu trong `localStorage`. Đề xuất chuyển sang cookie `HttpOnly`, `Secure`, `SameSite` sau khi thiết kế lại luồng xác thực. |
| SEC-29 | `AllowedHosts` được giới hạn theo tên miền triển khai. | `backend/src/StudentInternshipMgmt.Api/appsettings.json` | Chưa đạt | Giá trị hiện tại là `*`; cần thay bằng danh sách host hợp lệ theo môi trường triển khai. |
| SEC-30 | SMTP thật và quét lỗ hổng phụ thuộc đã được kiểm tra trước phát hành. | `Program.cs`; lệnh build/test; cấu hình triển khai | Chưa kiểm chứng | Test factory tắt SMTP; chưa chạy gửi thư tới máy chủ thật. Chưa có bằng chứng về một đợt quét phụ thuộc/SCA trong phạm vi rà soát này. |

## Kiểm kê DTO và đánh giá dữ liệu cá nhân theo vai trò

| Vai trò/đường dẫn | DTO trả về | Trường dữ liệu cá nhân | Đánh giá cần thiết |
|---|---|---|---|
| Sinh viên — `GET /api/auth/me` | `MeResponseDto` | `Username`, `UserId`, `StudentId` | Cần để giao diện xác định tài khoản và hồ sơ đang đăng nhập; không phải mật khẩu. |
| Sinh viên — `GET /api/me/profile` | `StudentDto` | Mã sinh viên, họ tên, email, số điện thoại | Cần trong hồ sơ của chính sinh viên để hiển thị và cập nhật thông tin. |
| Sinh viên — `GET /api/me/status-history` | `StatusHistoryDto` | `ChangedByUsername` | Lịch sử trạng thái cần thiết; username người thao tác có thể thay bằng vai trò/tên hiển thị tối thiểu nếu không cần truy vết cá nhân ở giao diện sinh viên. |
| Sinh viên — `GET /api/me/placement-requests` | `PlacementRequestDto` | Mã sinh viên, họ tên, tên/email/số điện thoại ứng viên, trường, ngành, CV và thư ứng tuyển | Đơn của chính sinh viên nên cần để xem lại nội dung ứng tuyển và trạng thái. |
| Sinh viên — `GET /api/companies` và `GET /api/companies/{id}` | `CompanyDto`, `CompanyDetailDto` | Người liên hệ, điện thoại, email, chức danh liên hệ | `ContactPerson` và `ContactPosition` có thể hỗ trợ tìm hiểu nơi thực tập; `ContactPhone`/`ContactEmail` chưa được chứng minh cần cho luồng nộp đơn trong hệ thống. Hiện API trả chúng cho Sinh viên một cách chủ đích trong mapping, nhưng chưa có tài liệu nghiệp vụ xác nhận; đề xuất DTO riêng cho Sinh viên bỏ hai trường liên hệ trực tiếp nếu không bắt buộc. |
| Sinh viên — `GET /api/job-positions` và `GET /api/job-positions/{id}` | `JobPositionDto` | Không có họ tên, email, số điện thoại hoặc mã sinh viên | Tên doanh nghiệp, vị trí và mô tả công việc cần để chọn nơi ứng tuyển; các trường này không phải thông tin liên hệ cá nhân trong DTO. |
| Doanh nghiệp — `GET /api/auth/me` | `MeResponseDto` | `Username`, `UserId`, `CompanyId` | Cần để xác định tài khoản và doanh nghiệp hiện tại. |
| Doanh nghiệp — `GET /api/company-me/profile` | `CompanyDto` | Người liên hệ, điện thoại, email, chức danh liên hệ | Cần để doanh nghiệp tự xem hồ sơ và thông tin liên hệ của chính mình. |
| Doanh nghiệp — `GET /api/company-me/job-positions` và `GET /api/company-me/job-positions/{id}` | `JobPositionAdminDto` | Không có dữ liệu cá nhân của sinh viên | Thông tin vị trí thuộc doanh nghiệp hiện tại; cần cho quản lý tin tuyển dụng. |
| Doanh nghiệp — `GET /api/company-me/placement-requests` và `GET /api/company-me/placement-requests/{id}` | `CompanyPlacementRequestDto` | Họ tên, lớp/ngành, email/số điện thoại ứng viên, trường học, CV và thư ứng tuyển | Cần thiết để liên hệ và đánh giá ứng viên; dịch vụ giới hạn theo `CompanyId` trong claim. |
| Sinh viên/Doanh nghiệp — `GET /api/about` | `AboutPageDto` chứa `AboutContactDto` | Người liên hệ, email, điện thoại công khai của đơn vị | Đây là nội dung trang Giới thiệu do quản trị nhập, không phải dữ liệu lấy từ hồ sơ tài khoản; việc công khai là chủ đích của trang nhưng chưa được xác nhận bằng yêu cầu ngoài mã nguồn. |

### Nhật ký và lưu trữ phía trình duyệt

- Tìm kiếm `console.log` không thấy lời gọi tương ứng trong mã frontend hiện tại. Có `console.warn` về cấu hình/retry và `console.error` cho lỗi render/tải người dùng; các thông báo này không được chứng minh là chứa PII, nhưng error object có thể mang dữ liệu phản hồi nên nên tránh ghi toàn bộ đối tượng lỗi ra console ở production.
- `localStorage` có access token, refresh token, chế độ hiển thị danh sách doanh nghiệp và tiến độ checklist trang Giới thiệu. Hai mục cuối là trạng thái giao diện, không phải hồ sơ hay dữ liệu cá nhân theo mã hiện có.

## Hạn chế đã biết và chưa thể kiểm chứng

- Refresh token đang lưu thô trong DB; xử lý bằng hash SHA-256 cần migration, được chấp nhận rủi ro cho lần rà soát này.
- Access token và refresh token đang lưu trong `localStorage`; đề xuất chuyển sang cookie `HttpOnly` ở bản sau.
- `AllowedHosts` đang là `*`.
- SMTP được tắt trong factory/test; chưa thử gửi thư qua máy chủ thật.
- Tag `before-security-review` không tồn tại trong repository hiện tại; không thể tạo worktree hoặc lấy bằng chứng chạy trước từ tag.
- Chưa có cấu hình triển khai thật trong phạm vi làm việc để kiểm tra host, key rotation, TLS, SMTP và hành vi đa instance của bộ đếm đăng nhập.
- Bộ đếm đăng nhập lưu trong bộ nhớ của tiến trình; chưa có cơ chế chia sẻ giữa nhiều instance và trạng thái mất khi tiến trình khởi động lại.
