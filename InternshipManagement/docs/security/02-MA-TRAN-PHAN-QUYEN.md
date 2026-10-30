# Ma trận phân quyền API

Ma trận được lập từ 12 controller hiện có trong `backend/src/StudentInternshipMgmt.Api/Controllers`. Mỗi dòng là một cặp phương thức và đường dẫn. Nhóm `User` trong JWT tương ứng với vai trò Sinh viên.

| Phương thức và đường dẫn | Vai trò được phép | Giới hạn dữ liệu và căn cứ trong mã |
|---|---|---|
| `POST /api/auth/login` | Chưa đăng nhập | Endpoint đăng nhập công khai; xác thực tài khoản trong dịch vụ. `AuthController.cs:12,24-55` |
| `POST /api/auth/refresh` | Chưa đăng nhập | Endpoint làm mới token công khai; cần refresh token hợp lệ trong nội dung yêu cầu. `AuthController.cs:57-64` |
| `POST /api/auth/change-password` | Admin, Sinh viên, Doanh nghiệp | Lấy mã tài khoản từ claim định danh của token; token có cờ buộc đổi mật khẩu chỉ được dùng endpoint này hoặc đăng xuất. `AuthController.cs:67-74,96-107`; `MustChangePasswordMiddleware.cs` |
| `POST /api/auth/logout` | Admin, Sinh viên, Doanh nghiệp | Yêu cầu đã đăng nhập; token thu hồi được tra theo refresh token. Token bị buộc đổi mật khẩu vẫn được phép đăng xuất. `AuthController.cs:77-84`; `MustChangePasswordMiddleware.cs` |
| `GET /api/auth/me` | Admin, Sinh viên, Doanh nghiệp | Lấy mã tài khoản từ claim định danh. `AuthController.cs:87-93,96-107`; `AuthService.cs:99-117` |
| `GET /api/about` | Admin, Sinh viên, Doanh nghiệp | Nội dung trang Giới thiệu dùng chung, không có bộ lọc theo người dùng. `AboutController.cs:11-12,23-28` |
| `PUT /api/about` | Admin | Chỉ Admin được cập nhật nội dung; mã người cập nhật lấy từ claim định danh. `AboutController.cs:31-41` |
| `POST /api/about/reset` | Admin | Chỉ Admin được khôi phục nội dung; mã người thao tác lấy từ claim định danh. `AboutController.cs:46-52` |
| `GET /api/companies` | Admin, Sinh viên | Danh sách doanh nghiệp dùng chung, không lọc theo tài khoản. Sinh viên nhận thêm dữ liệu liên hệ trong `CompanyDto`; cần xem đánh giá dữ liệu cá nhân ở checklist. `CompaniesController.cs:10-23`; `CompanyService.cs:52-78` |
| `GET /api/companies/industries` | Admin, Sinh viên | Danh mục ngành dùng chung, không chứa bộ lọc theo tài khoản. `CompaniesController.cs:31-37`; `CompanyService.cs:80-88` |
| `GET /api/companies/{id}` | Admin, Sinh viên | Tra theo mã doanh nghiệp trong đường dẫn; với Sinh viên chỉ trả các vị trí đã duyệt. `CompaniesController.cs:40-48`; `CompanyService.cs:90-119` |
| `POST /api/companies` | Admin | Tạo doanh nghiệp; không có giới hạn theo chủ tài khoản vì đây là chức năng quản trị. `CompaniesController.cs:52-61` |
| `PUT /api/companies/{id}` | Admin | Cập nhật theo mã doanh nghiệp được gửi; không có giới hạn theo chủ tài khoản. `CompaniesController.cs:64-75` |
| `DELETE /api/companies/{id}` | Admin | Xóa theo mã doanh nghiệp được gửi; không có giới hạn theo chủ tài khoản. `CompaniesController.cs:78-89` |
| `GET /api/companies/{companyId}/account` | Admin | Tra trạng thái tài khoản của doanh nghiệp theo mã được gửi; không phải quyền tự phục vụ của doanh nghiệp. `CompanyAccountsController.cs:11-23` |
| `POST /api/companies/{companyId}/account` | Admin | Tạo tài khoản cho doanh nghiệp theo mã được gửi; trả thông tin đăng nhập tạm thời. `CompanyAccountsController.cs:12,25-49` |
| `POST /api/companies/{companyId}/account/reset-password` | Admin | Đặt lại mật khẩu theo mã doanh nghiệp được gửi. `CompanyAccountsController.cs:12,52-68` |
| `POST /api/companies/{companyId}/account/deactivate` | Admin | Vô hiệu hóa tài khoản doanh nghiệp theo mã được gửi. `CompanyAccountsController.cs:12,71-85` |
| `POST /api/companies/{companyId}/account/activate` | Admin | Kích hoạt tài khoản doanh nghiệp theo mã được gửi. `CompanyAccountsController.cs:12,88-102` |
| `POST /api/companies/accounts/bulk` | Admin | Tạo hàng loạt theo danh sách mã doanh nghiệp trong nội dung yêu cầu. `CompanyAccountsBulkController.cs:13-14,25-55` |
| `POST /api/companies/accounts/bulk-by-filter` | Admin | Tạo hàng loạt theo bộ lọc do Admin gửi. `CompanyAccountsBulkController.cs:13-14,58-84` |
| `POST /api/companies/accounts/export` | Admin | Xuất tài khoản theo dữ liệu gửi trong yêu cầu; các ô CSV được xử lý qua `SpreadsheetSafety`. `CompanyAccountsBulkController.cs:13-14,87-119` |
| `GET /api/company-me/profile` | Doanh nghiệp | Mã `CompanyId` lấy từ claim; hồ sơ chỉ truy vấn doanh nghiệp gắn với claim đó. `CompanyMeController.cs:16-17,34-44`; `CompanyService.cs:90-107` |
| `GET /api/company-me/job-positions` | Doanh nghiệp | Mã `CompanyId` lấy từ claim và được truyền vào truy vấn lọc doanh nghiệp. `CompanyMeController.cs:47-56`; `JobPositionService.cs:63-69` |
| `GET /api/company-me/job-positions/{id}` | Doanh nghiệp | Kết hợp mã vị trí trên đường dẫn với `CompanyId` trong claim. `CompanyMeController.cs:59-70`; `JobPositionService.cs:157-161` |
| `POST /api/company-me/job-positions` | Doanh nghiệp | Tạo vị trí gắn với `CompanyId` trong claim, không nhận quyền sở hữu từ client. `CompanyMeController.cs:73-88` |
| `PUT /api/company-me/job-positions/{id}` | Doanh nghiệp | Cập nhật khi mã vị trí thuộc doanh nghiệp trong claim. `CompanyMeController.cs:91-106`; `JobPositionService.cs:268` |
| `POST /api/company-me/job-positions/{id}/submit` | Doanh nghiệp | Gửi duyệt vị trí thuộc doanh nghiệp trong claim. `CompanyMeController.cs:109-121`; `JobPositionService.cs:314` |
| `PATCH /api/company-me/job-positions/{id}/toggle-open` | Doanh nghiệp | Chỉ đổi trạng thái vị trí thuộc doanh nghiệp trong claim. `CompanyMeController.cs:124-138`; `JobPositionService.cs:377` |
| `DELETE /api/company-me/job-positions/{id}` | Doanh nghiệp | Chỉ xóa vị trí thuộc doanh nghiệp trong claim. `CompanyMeController.cs:141-153`; `JobPositionService.cs:442` |
| `GET /api/company-me/placement-requests` | Doanh nghiệp | Danh sách đơn được lọc theo `CompanyId` trong claim ở dịch vụ. `CompanyMeController.cs:156-165`; `PlacementRequestService.cs:253-259` |
| `GET /api/company-me/placement-requests/{id}` | Doanh nghiệp | Tra đơn theo cả mã đơn và `CompanyId` trong claim. `CompanyMeController.cs:168-179`; `PlacementRequestService.cs:298-303` |
| `POST /api/company-me/placement-requests/{id}/invite-interview` | Doanh nghiệp | Thao tác yêu cầu được giới hạn theo doanh nghiệp trong claim; người thao tác lấy từ claim định danh. `CompanyMeController.cs:182-201` |
| `POST /api/company-me/placement-requests/{id}/reject` | Doanh nghiệp | Thao tác yêu cầu được giới hạn theo doanh nghiệp trong claim; người thao tác lấy từ claim định danh. `CompanyMeController.cs:204-223` |
| `POST /api/company-me/placement-requests/{id}/result` | Doanh nghiệp | Ghi kết quả yêu cầu của doanh nghiệp trong claim; dịch vụ nhận `companyId` từ controller. `CompanyMeController.cs:226-253` |
| `GET /api/dashboard/summary` | Admin | Dữ liệu tổng hợp toàn hệ thống, chỉ Admin. `DashboardController.cs:10-11,22-27` |
| `GET /api/dashboard/need-assignment` | Admin | Danh sách cần phân công toàn hệ thống, chỉ Admin. `DashboardController.cs:30-35` |
| `GET /api/dashboard/recent-rejections` | Admin | Danh sách từ chối toàn hệ thống, chỉ Admin. `DashboardController.cs:38-43` |
| `GET /api/dashboard/overdue-requests` | Admin | Danh sách đơn quá hạn toàn hệ thống, chỉ Admin. `DashboardController.cs:46-51` |
| `GET /api/dashboard/pending-interview-results` | Admin | Danh sách kết quả phỏng vấn đang chờ toàn hệ thống, chỉ Admin. `DashboardController.cs:54-59` |
| `GET /api/job-positions` | Admin, Sinh viên | Danh sách vị trí đã duyệt cho Sinh viên; Admin nhận DTO quản trị. Không lọc theo tài khoản cá nhân. `JobPositionsController.cs:10-11,24-36`; `JobPositionService.cs:31-61` |
| `GET /api/job-positions/{id}` | Admin, Sinh viên | Tra vị trí theo mã; Sinh viên chỉ thấy vị trí đã duyệt, Admin thấy bản quản trị. `JobPositionsController.cs:39-57`; `JobPositionService.cs:95-146` |
| `POST /api/job-positions` | Admin | Tạo vị trí quản trị; mã doanh nghiệp nằm trong dữ liệu yêu cầu. `JobPositionsController.cs:60-69` |
| `PUT /api/job-positions/{id}` | Admin | Cập nhật vị trí theo mã; không giới hạn theo doanh nghiệp hiện tại. `JobPositionsController.cs:72-84` |
| `DELETE /api/job-positions/{id}` | Admin | Xóa vị trí theo mã; không giới hạn theo doanh nghiệp hiện tại. `JobPositionsController.cs:87-98` |
| `POST /api/job-positions/{id}/approve` | Admin | Duyệt vị trí theo mã. `JobPositionsController.cs:101-112` |
| `POST /api/job-positions/{id}/reject` | Admin | Từ chối vị trí theo mã. `JobPositionsController.cs:115-128` |
| `PATCH /api/job-positions/{id}/toggle-open` | Admin | Đổi trạng thái mở theo mã. `JobPositionsController.cs:131-143` |
| `GET /api/me/profile` | Sinh viên | Lấy `StudentId` từ claim; không nhận mã hồ sơ tùy ý. `MeController.cs:10-11,24-36` |
| `GET /api/me/status-history` | Sinh viên | Lấy `StudentId` từ claim; lịch sử được truy vấn cho hồ sơ đó. `MeController.cs:39-51` |
| `GET /api/me/placement-requests` | Sinh viên | Lấy `StudentId` từ claim; dịch vụ chỉ trả đơn có mã sinh viên đó. `MeController.cs:54-63`; `PlacementRequestService.cs:535-541` |
| `POST /api/me/placement-requests` | Sinh viên | Danh tính sinh viên lấy từ claim; mã sinh viên không lấy từ nội dung yêu cầu. `MeController.cs:66-78` |
| `POST /api/me/placement-requests/{id}/report-interviewed` | Sinh viên | Dịch vụ tra theo cả mã đơn và `StudentId` trong claim. `MeController.cs:81-96`; `PlacementRequestService.cs:595` |
| `GET /api/notifications` | Admin | Danh sách thông báo quản trị, chỉ Admin. `NotificationsController.cs:11-12,22-27` |
| `GET /api/notifications/unread-count` | Admin | Số thông báo chưa đọc quản trị, chỉ Admin. `NotificationsController.cs:30-34` |
| `POST /api/notifications/{id}/read` | Admin | Mã người thao tác lấy từ claim định danh; dịch vụ nhận cả mã thông báo và người dùng. `NotificationsController.cs:37-50` |
| `POST /api/notifications/read-all` | Admin | Đánh dấu thông báo cho người dùng lấy từ claim định danh. `NotificationsController.cs:53-64` |
| `GET /api/placement-requests` | Admin | Danh sách đơn toàn hệ thống, chỉ Admin. `PlacementRequestsController.cs:13-14,25-30` |
| `GET /api/placement-requests/{id}` | Admin | Tra đơn theo mã trên đường dẫn; chỉ Admin. `PlacementRequestsController.cs:33-41` |
| `GET /api/placement-requests/pending-confirmation` | Admin | Danh sách chờ xác nhận toàn hệ thống, chỉ Admin. `PlacementRequestsController.cs:44-50` |
| `POST /api/placement-requests/{id}/admin-confirm-result` | Admin | Mã Admin thao tác lấy từ claim định danh. `PlacementRequestsController.cs:53-86` |
| `POST /api/placement-requests/{id}/cancel-accepted-result` | Admin | Mã Admin thao tác lấy từ claim định danh. `PlacementRequestsController.cs:89-120` |
| `GET /api/students` | Admin | Danh sách sinh viên toàn hệ thống, chỉ Admin. `StudentsController.cs:13-14,29-34` |
| `GET /api/students/{id}` | Admin | Tra hồ sơ theo mã, chỉ Admin. `StudentsController.cs:37-45` |
| `POST /api/students` | Admin | Tạo hồ sơ; không nhận mã hồ sơ do client chọn. `StudentsController.cs:48-56` |
| `PUT /api/students/{id}` | Admin | Cập nhật hồ sơ theo mã, chỉ Admin. `StudentsController.cs:59-67` |
| `DELETE /api/students/{id}` | Admin | Xóa mềm hồ sơ theo mã, chỉ Admin. `StudentsController.cs:70-78` |
| `POST /api/students/create-accounts` | Admin | Tạo tài khoản theo danh sách mã do Admin gửi. `StudentsController.cs:81-91` |
| `POST /api/students/create-accounts/by-filter` | Admin | Tạo tài khoản cho các hồ sơ khớp bộ lọc do Admin gửi. `StudentsController.cs:94-101` |
| `POST /api/students/accounts/export` | Admin | Xuất thông tin đăng nhập gửi trong nội dung yêu cầu; ô CSV được escape. `StudentsController.cs:105-121` |
| `GET /api/students/export` | Admin | Xuất hồ sơ theo bộ lọc quản trị, chỉ Admin. `StudentsController.cs:124-136` |
| `POST /api/students/import` | Admin | Nhập hồ sơ toàn hệ thống, chỉ Admin; giới hạn tệp theo `ImportSettings`. `StudentsController.cs:139-158` |
| `POST /api/students/{id}/change-status` | Admin | Mã người thao tác lấy từ claim định danh. `StudentsController.cs:161-173` |
| `POST /api/students/{id}/reset-password` | Admin | Mã người thao tác lấy từ claim định danh; phản hồi không được cache. `StudentsController.cs:176-194` |
| `GET /api/students/{id}/status-history` | Admin | Xem lịch sử theo mã hồ sơ, chỉ Admin. `StudentsController.cs:197-205` |
| `POST /api/students/{id}/assign-company` | Admin | Mã người thao tác lấy từ claim định danh; hồ sơ đích do Admin chọn. `StudentsController.cs:208-218` |

## Kết quả kiểm thử phân quyền

- `AuthorizationMatrixTests` lấy các action từ `EndpointDataSource`, không duy trì danh sách route riêng trong test.
- Ngoại lệ công khai được khai báo rõ trong test: `POST /api/auth/login` và `POST /api/auth/refresh`.
- Test xác nhận chặn truy cập chưa đăng nhập, chặn sai vai trò, chặn Admin vào hai nhóm tự phục vụ, và kiểm tra truy cập chéo giữa hai doanh nghiệp/hai sinh viên.
- Nhóm Sinh viên chỉ có một thao tác trên đơn theo mã là báo đã phỏng vấn; controller hiện không có API đọc hoặc sửa đơn bằng `GET/PUT /api/me/placement-requests/{id}`. Test xác nhận thao tác báo phỏng vấn của sinh viên A không tác động đơn của sinh viên B.
- Test quét mọi phản hồi JSON của các endpoint `GET` để tìm thuộc tính `passwordHash`, `token` và `refreshToken`; phản hồi đăng nhập/làm mới là `POST` nên nằm ngoài tập quét theo yêu cầu.
