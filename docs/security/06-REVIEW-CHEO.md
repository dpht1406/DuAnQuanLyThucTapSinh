# Báo cáo review bảo mật chéo

## Phạm vi

Ref `before-security-review` không tồn tại trong repository, nên không thể chạy diff với ref đó. Review dựa trên `main...HEAD` và các thay đổi chưa commit; file `InMemoryLoginAttemptTracker.cs` đang là file untracked nên được kiểm tra trực tiếp vì `git diff` không bao gồm file mới. Phạm vi tập trung vào chính sách mật khẩu, khóa đăng nhập, thu hồi token, middleware lỗi, security headers và luồng đổi mật khẩu ở frontend.

## Tóm tắt phát hiện

| Mã | Ưu tiên | File:dòng | Tóm tắt |
|---|---|---|---|
| RV-01 | P1 nghiêm trọng | `InternshipManagement/frontend/src/utils/authNavigation.js:1-10`; `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Migrations/20261012073419_ForceAdminPasswordChange.cs:13-18` | Cờ buộc đổi mật khẩu chỉ được thực thi ở frontend; API vẫn cấp quyền truy cập bình thường. |
| RV-02 | P2 cao | `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/InMemoryLoginAttemptTracker.cs:83-84`; `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/AuthService.cs:38-39` | Có thể tạo bộ đếm khóa riêng bằng cách đổi IP hoặc thêm khoảng trắng cuối tên đăng nhập. |
| RV-03 | P2 cao | `InternshipManagement/backend/src/StudentInternshipMgmt.Api/Controllers/AuthController.cs:31,45-50`; `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/InMemoryLoginAttemptTracker.cs:22-25,45-48` | Các yêu cầu đăng nhập đồng thời có thể cùng vượt qua bước kiểm tra khóa trước khi lần thử thứ năm được ghi nhận. |
| RV-04 | P3 trung bình | `InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/LoginAttemptTrackerTests.cs:57-63` | Test chỉ xác nhận `Retry-After` lớn hơn 0, không kiểm tra thời lượng trả về có đúng với 15 phút hay không. |

## Chi tiết

### RV-01 — Có thể bỏ qua yêu cầu đổi mật khẩu bằng cách gọi API trực tiếp

- **Ưu tiên:** P1 nghiêm trọng
- **File:dòng:** `InternshipManagement/frontend/src/utils/authNavigation.js:1-10`; `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Migrations/20261012073419_ForceAdminPasswordChange.cs:13-18`; `InternshipManagement/backend/src/StudentInternshipMgmt.Api/Controllers/StudentsController.cs:14,29`
- **Mô tả:** `getPasswordChangeRedirect` chỉ chuyển hướng trên giao diện. Migration đặt `MustChangePassword=true` cho tài khoản seed nhưng không vô hiệu hóa thông tin đăng nhập đó. Backend không chặn token của tài khoản đang cần đổi mật khẩu khi gọi API có quyền Admin; cờ này không phải là ranh giới kiểm soát truy cập.
- **Tái hiện:** Trên CSDL mới đã áp dụng migration, đăng nhập bằng tài khoản Admin mặc định được seed. Dùng access token nhận được để gọi trực tiếp `GET /api/students` thay vì truy cập qua giao diện đổi mật khẩu. Nếu API trả dữ liệu Admin, yêu cầu đổi mật khẩu đã bị vượt qua.
- **Đề xuất:** Kiểm tra `MustChangePassword` ở backend trong luồng authorization, chỉ cho phép các API cần thiết để đổi mật khẩu cho đến khi đổi thành công. Không dựa vào route guard hoặc chuyển hướng frontend để hạn chế quyền API.

### RV-02 — Thay IP hoặc thêm khoảng trắng cuối tạo bộ đếm khóa mới

- **Ưu tiên:** P2 cao
- **File:dòng:** `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/InMemoryLoginAttemptTracker.cs:83-84`; `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/AuthService.cs:38-39`
- **Mô tả:** Khóa bộ đếm gồm username đã chuyển chữ thường và IP, nhưng không loại bỏ hoặc từ chối khoảng trắng đầu/cuối. Vì vậy, tên có dấu cách cuối tạo bucket khác; truy vấn tài khoản dùng SQL Server, nơi phép so sánh `=` thường coi chuỗi có dấu cách cuối tương đương chuỗi gốc. Đổi IP cũng tạo bucket khác cho cùng tài khoản. Chỉ đổi hoa/thường không tạo bucket mới vì tracker đã dùng `ToLowerInvariant()`.
- **Tái hiện:** Gửi năm lần đăng nhập sai cho một username từ cùng IP. Sau đó gửi lần thử với username thêm dấu cách cuối, hoặc giữ username nhưng gửi từ IP khác. Bộ đếm mới chưa bị khóa và yêu cầu vẫn được đưa tới xác thực tài khoản tương ứng.
- **Đề xuất:** Chuẩn hóa hoặc từ chối username có khoảng trắng đầu/cuối trước khi tra cứu và trước khi tạo khóa; bổ sung giới hạn theo tài khoản không phụ thuộc IP, đồng thời giữ giới hạn theo IP để hạn chế lạm dụng từ một nguồn.

### RV-03 — Race condition cho phép nhiều yêu cầu cùng qua ngưỡng khóa

- **Ưu tiên:** P2 cao
- **File:dòng:** `InternshipManagement/backend/src/StudentInternshipMgmt.Api/Controllers/AuthController.cs:31,45-50`; `InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/InMemoryLoginAttemptTracker.cs:22-25,45-48`; test tuần tự tại `InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/LoginAttemptTrackerTests.cs:38-70`
- **Mô tả:** Controller kiểm tra khóa trước, sau đó mới `await LoginAsync`; chỉ khi xác thực trả về sai mật khẩu thì mới ghi nhận thất bại. Tracker khóa riêng từng thao tác cache, không làm cho kiểm tra khóa, xác thực và cập nhật số lần thử trở thành một thao tác nguyên tử. Nhiều yêu cầu đồng thời có thể cùng thấy tài khoản chưa khóa và cùng bắt đầu xác thực; các yêu cầu đã bắt đầu không bị dừng khi một yêu cầu khác làm bộ đếm đạt năm. Giới hạn 20 yêu cầu/phút/IP hiện tại vẫn cho phép một nhóm yêu cầu như vậy.
- **Tái hiện:** Gửi đồng thời ít nhất sáu yêu cầu sai mật khẩu với cùng username và IP, sao cho tất cả đều qua kiểm tra khóa trước khi xác thực hoàn tất. Các yêu cầu đó đều vào `LoginAsync`; khóa chỉ chặn những yêu cầu đến sau khi trạng thái khóa được ghi.
- **Kiểm thử:** Test hiện tại `await` từng lần thử tuần tự nên không kiểm tra tình huống cạnh tranh. Dùng barrier để giữ các lần xác thực đang chờ cho tới khi toàn bộ yêu cầu đã qua bước kiểm tra khóa, rồi cho chúng trả về thất bại để tái hiện race.
- **Đề xuất:** Dùng cơ chế nguyên tử theo khóa tài khoản để giới hạn việc bắt đầu xác thực và cập nhật trạng thái; nếu chạy nhiều instance, dùng kho trạng thái/chính sách khóa dùng chung. Bổ sung test đồng thời có barrier để xác nhận không yêu cầu vượt ngưỡng được phép bắt đầu xác thực.

### RV-04 — Assertion `Retry-After` không kiểm tra đúng thời lượng

- **Ưu tiên:** P3 trung bình
- **File:dòng:** `InternshipManagement/backend/tests/StudentInternshipMgmt.Infrastructure.Tests/LoginAttemptTrackerTests.cs:57-63`
- **Mô tả:** Test kiểm tra nội dung phản hồi có chữ “15 phút”, nhưng với header `Retry-After` chỉ kiểm tra giá trị lớn hơn 0. Giá trị sai như 1 giây hoặc 3600 giây vẫn làm test xanh, nên regression ở thời lượng chờ HTTP có thể lọt qua. Test riêng vẫn xác nhận luồng khóa tuần tự mở lại sau khi đồng hồ tiến đúng 15 phút; phát hiện này chỉ liên quan đến assertion của header.
- **Tái hiện:** Thay giá trị `Retry-After` trong phản hồi thành một số dương bất kỳ nhưng không khớp thời gian khóa. Assertion hiện tại vẫn đạt.
- **Đề xuất:** Assert giá trị `Retry-After` bằng số giây còn lại mong đợi tại thời điểm bị khóa, hoặc kiểm tra một khoảng sai số nhỏ nếu thời gian được tính động.

## Các điểm đã kiểm tra, không ghi nhận phát hiện khác

- **Độ dài mật khẩu:** Chính sách chấp nhận độ dài đúng 8 và 128 ký tự; test có kiểm tra các độ dài biên và từ chối mật khẩu vượt 128 ký tự.
- **Ngưỡng khóa và thời hạn:** Với yêu cầu tuần tự, bốn lần sai chưa khóa, lần thứ năm khóa và tracker mở lại sau đúng 15 phút. Điều này không loại trừ race đồng thời tại RV-03.
- **Tên đăng nhập và IP:** Tracker đã chuẩn hóa hoa/thường; không thấy bằng chứng rằng header do client tự gửi được tin cậy làm địa chỉ IP. Tuy vậy, phân vùng theo IP và khoảng trắng cuối vẫn có thể tạo bucket mới như RV-02.
- **Thu hồi token:** Luồng đổi mật khẩu thu hồi refresh token đang hoạt động; test đổi mật khẩu xác nhận token cũ bị từ chối.
- **Middleware lỗi và security headers:** Middleware trả thông báo chung cho lỗi 500; test xác nhận không lộ chi tiết exception. Middleware security headers đặt các header được kiểm tra, gồm ngoại lệ CSP cho Swagger; không ghi nhận lỗi bảo mật xác thực được trong phần này.
- **Log và thông báo lỗi:** Log đăng nhập thất bại dùng hash username thay vì username thô; test kiểm tra thông tin định danh không xuất hiện trong log. Không ghi nhận rò rỉ dữ liệu nhạy cảm khác trong phản hồi lỗi đã xem.
- **Mock/test:** Test controller dùng `TestAuthService` để kiểm tra luồng controller/tracker; test đổi mật khẩu dùng service thật với EF InMemory để kiểm tra thu hồi token. Không có bằng chứng mock bị đặt sai chỗ; điểm yếu assertion đã xác định được nêu riêng tại RV-04.
