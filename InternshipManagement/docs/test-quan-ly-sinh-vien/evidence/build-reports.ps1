$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$evidence = Join-Path $root "evidence"
$dataDirectory = Join-Path $evidence "data"
$logDirectory = Join-Path $evidence "logs"
$performanceDirectory = Join-Path $evidence "perf"
$xlsxPath = Join-Path $root "TestCase-QuanLySinhVienThucTap.xlsx"
$docxPath = Join-Path $root "Bao-cao-Test-Case.docx"

$cases = [Collections.Generic.List[object]]::new()
function Add-Case(
    [string]$Id, [string]$Function, [string]$Type, [string]$Description,
    [string]$Preconditions, [string]$Steps, [string]$Data, [string]$Expected,
    [string]$Actual, [string]$Status, [string]$Evidence, [string]$SeverityAndNotes
) {
    $script:cases.Add([pscustomobject][ordered]@{
        "Mã TC" = $Id
        "Chức năng" = $Function
        "Loại" = $Type
        "Mô tả" = $Description
        "Điều kiện tiên quyết" = $Preconditions
        "Các bước thực hiện" = $Steps
        "Dữ liệu kiểm thử" = $Data
        "Kết quả mong đợi" = $Expected
        "Kết quả thực tế" = $Actual
        "Trạng thái" = $Status
        "Minh chứng" = $Evidence
        "Mức độ lỗi / Ghi chú" = $SeverityAndNotes
    })
}

$backendLog = "evidence/logs/dotnet-test-final.txt"
$frontendLog = "evidence/logs/vitest-final.txt"
$preFixLog = "evidence/logs/pre-fix-regression-tests.txt"

Add-Case "TC-A01" "Xác thực sinh viên" "Luồng chính" "Đăng nhập User thành công và nhận claim Student." "Tài khoản sinh viên có sẵn trong test DB." "1. POST /api/auth/login; 2. xác minh role và claim." "AuthService test user; không lưu mật khẩu/token." "Trả đăng nhập thành công, role User và StudentId claim." "Test Login_student_user_includes_student_claim_without_company_claim chạy trong suite." "Đạt" "$backendLog; AuthServiceCompanyRoleTests.Login_student_user_includes_student_claim_without_company_claim" "—"
Add-Case "TC-A02" "Xác thực doanh nghiệp" "Luồng chính" "Đăng nhập Company và gắn claim doanh nghiệp." "Company test user tồn tại." "1. POST /api/auth/login; 2. xác minh role/CompanyId." "AuthService company user." "Role Company và claim CompanyId hợp lệ." "Test Login_company_user_includes_company_claim_and_company_role chạy trong suite." "Đạt" "$backendLog; AuthServiceCompanyRoleTests.Login_company_user_includes_company_claim_and_company_role" "—"
Add-Case "TC-A03" "Xác thực" "Dữ liệu lỗi" "Đăng nhập sai mật khẩu, đổi mật khẩu bắt buộc và refresh token qua HTTP." "Cần API hoạt động và test DB." "1. Gọi login sai; 2. gọi refresh; 3. gọi change-password." "Không có thông tin đăng nhập an toàn cho test DB." "Thông tin sai bị từ chối; token được xoay vòng; cờ MustChangePassword được thực thi." "Chưa gọi API; localhost:5018 không lắng nghe và không có dedicated test DB." "Chưa chạy" "Chưa có response API; logs/environment-and-api-check.txt" "Không kết luận"

Add-Case "TC-B01" "Quản lý sinh viên" "Luồng chính" "Tạo mới, trim trường nhập và lưu giá trị đã chuẩn hóa." "EF InMemory test context." "1. Tạo DTO có khoảng trắng; 2. gọi CreateStudentAsync; 3. kiểm tra DTO." "TST001, email/phone có khoảng trắng." "Tạo thành công, các trường được trim." "Test CreateStudentAsync_trims_fields_and_rejects_case_insensitive_duplicate_codes xác nhận bản ghi đầu được tạo và trim." "Đạt" "$backendLog; StudentSoftDeleteFilterTests.CreateStudentAsync_trims_fields_and_rejects_case_insensitive_duplicate_codes" "—"
Add-Case "TC-B02" "Quản lý sinh viên" "Dữ liệu lỗi" "Từ chối email sai và SĐT không phải 10 số bắt đầu 0 ở cả service create/update." "EF InMemory test context." "1. Gửi email sai; 2. gửi email đúng nhưng phone sai cho Create và Update; 3. kiểm tra lỗi." "bad-email; 123456789." "Create và Update đều từ chối trường lỗi, không lưu dữ liệu không hợp lệ." "CreateStudentAsync_rejects_invalid_email_and_phone_number và UpdateStudentAsync_trims_fields_and_rejects_invalid_email_and_phone_number passed." "Đạt" "$backendLog; StudentSoftDeleteFilterTests.CreateStudentAsync_rejects_invalid_email_and_phone_number; StudentSoftDeleteFilterTests.UpdateStudentAsync_trims_fields_and_rejects_invalid_email_and_phone_number" "—"
Add-Case "TC-B03" "Quản lý sinh viên" "Biên" "Phát hiện MSSV trùng sau trim, không phân biệt hoa/thường." "EF InMemory test context." "1. Tạo TST001; 2. gửi ' tst001 '; 3. xác minh bị từ chối." "TST001 và tst001." "Lần hai không được tạo." "Test CreateStudentAsync_trims_fields_and_rejects_case_insensitive_duplicate_codes xác nhận từ chối." "Đạt" "$backendLog; StudentSoftDeleteFilterTests.CreateStudentAsync_trims_fields_and_rejects_case_insensitive_duplicate_codes" "—"
Add-Case "TC-B04" "Tìm kiếm, lọc, phân trang" "Luồng chính" "Tìm MSSV/email/tên, filter và pagination trên SQL Server." "Cần dedicated SQL Server test connection." "1. Chạy StudentSearch SQL Server fact; 2. kiểm tra dữ liệu các trang." "StudentSearchTests; DB test chưa cấu hình." "Tìm một trường đúng, literal LIKE được escape, phân trang ổn định." "Một test bị xUnit skip do thiếu STUDENT_TEST_SQLSERVER; assertion chưa chạy." "Chưa chạy" "$backendLog; lý do skip: Set STUDENT_TEST_SQLSERVER to a dedicated SQL Server test database." "Skipped bởi môi trường"
Add-Case "TC-B05" "Xóa mềm" "Luồng chính" "Không trả sinh viên đã xóa mềm trong query mặc định." "EF InMemory test context." "1. Lưu sinh viên active/deleted; 2. query Students." "SV001 active, SV002 IsDeleted=true." "Chỉ trả SV001; IgnoreQueryFilters đọc được cả hai." "Test Students_query_excludes_soft_deleted_students chạy trong suite." "Đạt" "$backendLog; StudentSoftDeleteFilterTests.Students_query_excludes_soft_deleted_students" "—"
Add-Case "TC-B06" "Lịch sử trạng thái" "Luồng chính" "Ghi StatusHistory khi Admin đổi trạng thái." "Student/Company và admin test entities." "1. Đổi trạng thái; 2. đọc history; 3. kiểm tra actor." "StudentStatusHistoryTests fixtures." "Ghi đúng From/To, actor Admin và doanh nghiệp." "StudentStatusHistoryTests chạy trong suite; không có assertion fail." "Đạt" "$backendLog; StudentStatusHistoryTests" "—"
Add-Case "TC-B07" "Đặt lại mật khẩu" "Luồng chính" "Đặt lại mật khẩu yêu cầu đổi lần đầu và thu hồi refresh token." "User/RefreshToken test entities." "1. Gọi ResetPasswordAsync; 2. xác minh hash, cờ và token." "StudentPasswordResetTests." "Hash đổi; MustChangePassword=true; token active bị revoke." "StudentPasswordResetTests chạy trong suite; không có assertion fail." "Đạt" "$backendLog; StudentPasswordResetTests" "—"

Add-Case "TC-C01" "Import sinh viên" "Luồng chính" "Import tệp hợp lệ và xác nhận số dòng nhập." "API + DB test riêng, account Admin." "1. POST multipart file; 2. kiểm tra SuccessCount và dữ liệu." "evidence/data/students-valid-20.csv." "20 dòng hợp lệ được nhập, không lỗi." "Chưa gọi endpoint; không có API listener/test DB." "Chưa chạy" "Fixture có sẵn; evidence/data/students-valid-20.csv" "Không kết luận"
Add-Case "TC-C02" "Import sinh viên" "Dữ liệu lỗi" "Import từng phần với dòng thiếu/invalid và special characters." "API + DB test riêng; seed row TSTDB0001." "1. Seed TSTDB0001; 2. import mixed CSV; 3. kiểm tra lỗi theo số dòng." "students-mixed-errors.csv; formula text =1+1." "Dòng đúng nhập; lỗi email/phone/trùng được đánh số; input formula lưu an toàn." "Chưa chạy; fixture được tạo nhưng import endpoint không chạy." "Chưa chạy" "evidence/data/students-mixed-errors.csv; precondition DB seed chưa thực hiện" "Không kết luận"
Add-Case "TC-C03" "Import sinh viên" "Dữ liệu lỗi" "Từ chối MSSV trùng trong file và với DB, không phân biệt case." "Dedicated test DB có seed TSTDB0001." "1. Import mixed CSV; 2. đối chiếu row errors và DB." "TST9005/tst9005 và TSTDB0001." "Dòng trùng bị bỏ; các dòng độc lập hợp lệ vẫn nhập." "Chưa chạy; không có dedicated DB." "Chưa chạy" "evidence/data/students-mixed-errors.csv; logs/environment-and-api-check.txt" "Không kết luận"
Add-Case "TC-C04" "Import sinh viên" "Dữ liệu lỗi" "Từ chối file sai extension." "API + DB test riêng." "1. Upload file .txt; 2. kiểm tra response/error." "students-invalid-extension.txt." "File không được nhập, trả lỗi định dạng." "Chưa chạy; API không hoạt động." "Chưa chạy" "evidence/data/students-invalid-extension.txt" "Không kết luận"
Add-Case "TC-C05" "Import sinh viên" "Biên" "Từ chối file rỗng hoặc CSV thiếu cột header." "API + DB test riêng." "1. Upload file empty; 2. upload CSV thiếu ClassName." "students-empty.csv; students-missing-column.csv." "File rỗng bị 400; thiếu cột được báo lỗi theo thiết kế." "Chưa chạy; endpoint không được gọi." "Chưa chạy" "evidence/data/students-empty.csv; evidence/data/students-missing-column.csv" "Không kết luận"

Add-Case "TC-D01" "Export CSV" "Luồng chính" "Áp dụng filter HasAccount khi xuất CSV, không phân trang." "EF InMemory test context có record có/không account." "1. Export filter HasAccount=false; 2. đọc CSV." "SV100 có account; SV101 không." "Chỉ SV101 nằm trong CSV." "Test ExportStudentsAsync_applies_has_account_filter xác nhận SV101 có và SV100 không có." "Đạt" "$backendLog; StudentSoftDeleteFilterTests.ExportStudentsAsync_applies_has_account_filter" "—"
Add-Case "TC-D02" "Spreadsheet formula safety" "Dữ liệu lỗi" "Escape prefix formula trong helper dùng chung." "Unit test không cần DB." "1. Chạy theory formula prefixes; 2. so output." "=1+1, +cmd, -2, @SUM(A1)." "Prefix được neutralize bằng apostrophe." "Spreadsheet_helper_escapes_formula_prefixes chạy và xác nhận các giá trị." "Đạt" "$backendLog; SharedInputSafetyTests.Spreadsheet_helper_escapes_formula_prefixes" "Chỉ helper; chưa xác nhận file export thật"
Add-Case "TC-D03" "Export CSV" "Biên" "Kiểm tra đủ 10 cột, CSV quote/escape, BOM UTF-8, tiếng Việt và filter." "API + DB test riêng." "1. Export csv; 2. parse header/rows; 3. mở CSV UTF-8." "Fixture chứa dấu phẩy/nháy kép/tiếng Việt/formula." "Đúng cột, escaping và Unicode; áp đúng filter." "Chưa chạy end-to-end; helper test không thay cho kiểm tra file." "Chưa chạy" "Fixture mixed; no API response" "Không kết luận"
Add-Case "TC-D04" "Export XLSX" "Luồng chính" "Kiểm tra workbook headers, định dạng, filter và dữ liệu UTF-8." "API + DB test riêng." "1. Export xlsx; 2. mở workbook; 3. so cột và rows." "CSV/xlsx test records." "Workbook hợp lệ, 10 cột, đúng bộ lọc." "Chưa gọi API; chưa chạy export XLSX service." "Chưa chạy" "Không có response; logs/environment-and-api-check.txt" "Không kết luận"
Add-Case "TC-D05" "Export formula injection" "Dữ liệu lỗi" "Xác nhận CSV và XLSX output neutralize =, +, -, @." "API + DB test riêng." "1. Seed formula-like fields; 2. export csv/xlsx; 3. inspect cell bytes/value." "=1+1; +cmd; -2; @SUM(A1)." "Mọi cell export an toàn, không executable formula." "Chỉ helper unit test đã chạy; file exports chưa được tạo từ API." "Chưa chạy" "SharedInputSafetyTests chỉ là helper; no API export artifact" "Không kết luận"

Add-Case "TC-E01" "Dashboard KPI" "Biên" "CompletionRate với 10 sinh viên, 3 Completed." "Dedicated DB seeded 10 students." "1. Seed 10 record, 3 Completed; 2. gọi summary." "10 total, 3 completed." "Rate=30.00; total=10." "DashboardService không được gọi qua DB/API trong lần kiểm thử này." "Chưa chạy" "Không có test DB/API response" "Không kết luận"
Add-Case "TC-E02" "Dashboard KPI" "Biên" "Tổng bằng 0 và sinh viên soft-deleted không tính." "Dedicated DB rỗng / fixture có deleted row." "1. Gọi summary DB rỗng; 2. seed deleted; 3. gọi lại." "Zero rows; then one deleted row." "Total=0, rate=0; soft-deleted không được tính." "Chưa chạy summary; global filter có test riêng nhưng không chứng minh DTO KPI." "Chưa chạy" "StudentSoftDeleteFilterTests không bao phủ DashboardSummary" "Không kết luận"
Add-Case "TC-E03" "Dashboard cảnh báo" "Biên" "Kiểm tra ngưỡng ngày thứ 5, thiếu một ngày, Pending exclusion và overdue interview." "Dedicated DB với CreatedAt/requests kiểm soát." "1. Seed boundary dates/status; 2. gọi need-assignment/overdue endpoints." "PlacementReminderDays=5, overdue=5, interview=3." "Ngày biên inclusive; loại Student có Pending; overdue status đúng." "Unit test có kiểm tra một số overdue defaults, nhưng dashboard full KPI boundary chưa chạy; DB/API unavailable." "Chưa chạy" "$backendLog có test overdue helper; chưa có phép thử đầy đủ TC này" "Không kết luận"

Add-Case "TC-F01" "Đơn thực tập" "Luồng chính" "Sinh viên nộp đơn đầy đủ; snapshot form lưu riêng khỏi profile." "PlacementRequest service test context." "1. Tạo request hợp lệ; 2. xác nhận request snapshot và profile." "CreatePlacementRequestDto hợp lệ." "Tạo request Pending với snapshot đã trim." "CreateRequestAsync_saves_trimmed_snapshots_without_changing_student và validator tests chạy." "Đạt" "$backendLog; PlacementRequestServiceTests; CreatePlacementRequestDtoValidatorTests" "—"
Add-Case "TC-F02" "Đơn thực tập" "Dữ liệu lỗi" "Chặn Pending trùng và vượt giới hạn đơn đang mở." "PlacementRequest service test context." "1. Tạo pending cùng công ty/vị trí; 2. đạt limit; 3. gửi thêm." "MaxOpenApplicationsPerStudent=5." "Request trùng/vượt limit bị từ chối." "CreateRequestAsync_rejects_duplicate_open_company_and_position và CreateRequestAsync_rejects_when_open_request_limit_is_reached chạy." "Đạt" "$backendLog; PlacementRequestServiceTests" "—"
Add-Case "TC-F03" "Đơn thực tập" "Biên" "Chặn vị trí đóng/hết hiệu lực/không thuộc đúng công ty." "PlacementRequest service test context." "1. Gửi request cho position không khả dụng; 2. kiểm tra kết quả." "Position closed/expired/mismatched." "Không nhận request; dữ liệu không bị mutate." "CreateRequestAsync_rejects_unavailable_or_mismatched_position chạy trong suite." "Đạt" "$backendLog; PlacementRequestServiceTests.CreateRequestAsync_rejects_unavailable_or_mismatched_position" "—"
Add-Case "TC-F04" "Mời phỏng vấn" "Luồng chính" "Company mời phỏng vấn cho request Pending." "Company request test context." "1. Invite future interview; 2. kiểm tra InterviewAt UTC/status." "InviteInterviewDto hợp lệ." "Lưu lịch phỏng vấn UTC; chỉ request đổi." "Invite_interview_updates_only_request_and_stores_interview_time_as_utc chạy." "Đạt" "$backendLog; CompanyPlacementRequestServiceTests" "—"
Add-Case "TC-F05" "Kết quả thực tập" "Luồng chính" "Company ghi Đạt, cập nhật Student và từ chối open requests khác." "Placement decision test context." "1. Record pass; 2. xác nhận accepted student/history/requests." "CompanyPlacementDecisionTests fixture." "Sinh viên chuyển Accepted; history/request khác được cập nhật nhất quán." "Passing_request_accepts_student_and_rejects_other_open_requests chạy." "Đạt" "$backendLog; CompanyPlacementDecisionTests.Passing_request_accepts_student_and_rejects_other_open_requests" "—"
Add-Case "TC-F06" "Từ chối đơn" "Luồng chính" "Company từ chối request Pending/InterviewInvited, không đổi Student." "Company placement request test context." "1. Reject request; 2. xác minh reason/status/student." "Pending và InterviewInvited." "Request bị từ chối; sinh viên không đổi trạng thái." "Company_can_reject_pending_or_interview_invited_request chạy trong suite." "Đạt" "$backendLog; CompanyPlacementRequestServiceTests" "—"

Add-Case "TC-G01" "Vị trí thực tập" "Luồng chính" "Company tạo draft và submit; Admin duyệt pending position." "JobPosition services test context." "1. Tạo draft/submit; 2. Admin filter/approve; 3. xác minh trạng thái." "Company draft, approval pending." "Draft chưa ở trạng thái approved; Admin approve chuyển trạng thái." "Company_can_create_draft_and_submit_it_only_once và Approve_pending_position... chạy; public-list visibility chưa bao phủ." "Đạt" "$backendLog; CompanyJobPositionServiceTests; AdminJobPositionApprovalTests" "Phạm vi service approval; student API visibility chưa chạy"
Add-Case "TC-G02" "Vị trí thực tập" "Biên" "Không cho giảm Quantity thấp hơn AcceptedCount." "CompanyJobPositionService test context." "1. Seed AcceptedCount; 2. PUT Quantity nhỏ hơn; 3. kiểm tra rejection." "Quantity < AcceptedCount." "Cập nhật bị chặn." "Company_cannot_reduce_quantity_below_accepted_count chạy." "Đạt" "$backendLog; CompanyJobPositionServiceTests.Company_cannot_reduce_quantity_below_accepted_count" "—"

Add-Case "TC-H01" "Phân quyền sinh viên" "Phân quyền" "Student A truy cập dữ liệu Student B." "Cần API + JWT của hai user." "1. Đăng nhập A; 2. gọi route profile/request B; 3. kiểm tra 403/404." "Hai user Student A/B." "Không lộ dữ liệu B." "Không gọi API; test DB/auth token chưa có." "Chưa chạy" "No API response; logs/environment-and-api-check.txt" "Không kết luận"
Add-Case "TC-H02" "Phân quyền Admin" "Phân quyền" "User gọi API chỉ dành Admin." "Cần API + JWT User." "1. Gọi GET /api/students bằng token User; 2. kiểm tra status." "User role token." "HTTP 403." "Không gọi endpoint; controller metadata không phải HTTP assertion." "Chưa chạy" "No API response" "Không kết luận"
Add-Case "TC-H03" "Phân quyền Company" "Phân quyền" "Company A không đọc/ghi đơn của Company B." "Company placement service tests." "1. Tạo request thuộc B; 2. truy cập qua A; 3. kiểm tra không mutation." "CompanyPlacementRequestService fixture." "Company A không nhận dữ liệu của B." "Company_cannot_read_or_mutate_another_company_request chạy." "Đạt" "$backendLog; CompanyPlacementRequestServiceTests.Company_cannot_read_or_mutate_another_company_request" "—"
Add-Case "TC-H04" "Xác thực API" "Phân quyền" "Gọi endpoint protected không có token." "API listener." "1. GET /api/students không Authorization; 2. kiểm tra status." "Không bearer token." "HTTP 401." "Chưa gọi API do listener không chạy." "Chưa chạy" "No API response; logs/environment-and-api-check.txt" "Không kết luận"

Add-Case "TC-I01" "Frontend validation" "Dữ liệu lỗi" "Form sinh viên báo required, email sai, SĐT sai, trims input." "Vitest frontend." "1. Chạy studentEditValidation.test.js; 2. xem assertion." "Input rỗng, malformed email, phone 9/11 digit." "Hiển thị field errors; valid trimmed form chấp nhận." "Student form validation suite chạy cùng 93 Vitest tests; passed." "Đạt" "$frontendLog; studentEditValidation.test.js" "Unit validation; UI E2E chưa chạy"
Add-Case "TC-I02" "Frontend route role" "Phân quyền" "Role Admin/User/Company ánh xạ tới default route đúng." "Vitest frontend." "1. Chạy roleRoutes.test.js; 2. kiểm tra role routes." "Admin, User, Company, unknown." "Route phù hợp; role lạ trả null." "roleRoutes.test.js passed trong suite." "Đạt" "$frontendLog; roleRoutes.test.js" "Không có Playwright/UI runtime"

Add-Case "TC-J01" "Dữ liệu lớn/hiệu năng" "Hiệu năng" "Đo import 100/1,000/5,000 rows và export CSV/XLSX 1,000/5,000, 5 lần." "API và DB test riêng đã seed/reset giữa trial." "1. Run each operation 5 times; 2. collect min/median/max." "CSV 1k/5k và XLSX 5k fixtures." "Measured latency compared to thresholds." "Chưa đo: dedicated DB và API không sẵn có; không ghi số đo thay thế." "Chưa chạy" "evidence/perf/measurement-attempts.csv; measurement-not-run.txt" "Không kết luận"
Add-Case "TC-J02" "Dữ liệu lớn/hiệu năng" "Hiệu năng" "Đo paging trên DB 5,000, Dashboard summary và login 5 lần." "DB test 5k và API hoạt động." "1. Seed 5,000; 2. lặp mỗi API 5 lần; 3. tổng hợp median." "Dataset bulk seed." "Thời gian nằm trong ngưỡng nhóm đặt." "Chưa đo vì DB/API không sẵn có." "Chưa chạy" "evidence/perf/measurement-attempts.csv; measurement-not-run.txt" "Không kết luận"

$caseHeaders = @($cases[0].PSObject.Properties.Name)
$caseRows = foreach ($case in $cases) { ,@($caseHeaders | ForEach-Object { [string]$case.$_ }) }
$passCount = @($cases | Where-Object { $_."Trạng thái" -eq "Đạt" }).Count
$failCount = @($cases | Where-Object { $_."Trạng thái" -eq "Không đạt" }).Count
$unrunCount = @($cases | Where-Object { $_."Trạng thái" -eq "Chưa chạy" }).Count

$groups = @(
    [pscustomobject]@{ Group = "A - Xác thực"; Total = 3; Passed = 2; Failed = 0; Unrun = 1; Detail = "User/Company login service tests pass; invalid login/password change/refresh via API not run." },
    [pscustomobject]@{ Group = "B - Quản lý sinh viên"; Total = 7; Passed = 6; Failed = 0; Unrun = 1; Detail = "CRUD validation, normalized duplicate, soft delete, status history/password reset pass; SQL search fact skipped." },
    [pscustomobject]@{ Group = "C - Import"; Total = 5; Passed = 0; Failed = 0; Unrun = 5; Detail = "API/DB unavailable; deterministic input files were generated." },
    [pscustomobject]@{ Group = "D - Export"; Total = 5; Passed = 2; Failed = 0; Unrun = 3; Detail = "HasAccount CSV filter and shared formula helper pass; end-to-end CSV/XLSX not run." },
    [pscustomobject]@{ Group = "E - Dashboard/KPI"; Total = 3; Passed = 0; Failed = 0; Unrun = 3; Detail = "No dedicated DB/API; KPI boundaries not executed." },
    [pscustomobject]@{ Group = "F - Đơn ứng tuyển"; Total = 6; Passed = 6; Failed = 0; Unrun = 0; Detail = "Validator, create/limit/duplicate/position checks, invitation, decisions/rejection service tests pass." },
    [pscustomobject]@{ Group = "G - Vị trí"; Total = 2; Passed = 2; Failed = 0; Unrun = 0; Detail = "Draft/approval and quantity constraints service tests pass; public listing E2E not run." },
    [pscustomobject]@{ Group = "H - Phân quyền"; Total = 4; Passed = 1; Failed = 0; Unrun = 3; Detail = "Company-to-company service isolation passes; HTTP/JWT cases not run." },
    [pscustomobject]@{ Group = "I - Frontend"; Total = 2; Passed = 2; Failed = 0; Unrun = 0; Detail = "Vitest form validation and role route helper tests pass; no Playwright." },
    [pscustomobject]@{ Group = "J - Dữ liệu lớn/hiệu năng"; Total = 2; Passed = 0; Failed = 0; Unrun = 2; Detail = "Files generated, requested API timings not run." }
)

$rules = @(
    @("Quy tắc", "Định nghĩa dùng trong test", "Nguồn/ghi chú"),
    @("Sinh viên tổng", "Count sinh viên IsDeleted=false.", "StudentConfiguration global query filter; DashboardService.CountAsync."),
    @("Tỷ lệ hoàn thành", "Completed / total * 100; Round 2; total 0 => 0.", "DashboardService.GetSummaryAsync."),
    @("Có/không doanh nghiệp", "CompanyId != null; không có = tổng - có.", "DashboardService.GetSummaryAsync."),
    @("Cần gán doanh nghiệp", "Status=NoCompany; User.CreatedAt <= now - PlacementReminderDays; không có Pending.", "DashboardService; default 5 ngày nếu không cấu hình."),
    @("Đơn quá hạn", "Pending và CreatedAt <= now - OverdueRequestAlertDays.", "DashboardService; default 5 ngày; giá trị <=0 fallback 5."),
    @("Kết quả phỏng vấn trễ", "InterviewInvited, có InterviewAt <= now - InterviewResultOverdueDays.", "DashboardService; default 3 ngày; <=0 fallback 3."),
    @("Ngưỡng ứng tuyển", "MaxOpenApplicationsPerStudent=5; ApplicationResponseDueDays=7.", "backend appsettings.json; service tests xác nhận giới hạn/default."),
    @("Import", "6 headers; email regex; phone 10 digit bắt đầu 0; MSSV không trùng case-insensitive DB/file; partial import.", "StudentService implementation; endpoint runtime chưa thực hiện."),
    @("Export", "10 cột gồm nhãn status tiếng Việt; filter Search/Status/CompanyId/HasAccount; không paginate; BOM/CSV escaping/formula escaping.", "StudentService; regression test mới cho HasAccount; end-to-end chưa chạy."),
    @("Status", "NoCompany -> Introduced -> Interviewed -> Accepted -> Interning -> Completed; history; Admin thao tác trạng thái.", "Enum, StudentService, controller authorize. HTTP role chưa chạy."),
    @("Role boundary", "Admin routes yêu cầu Admin; company request query scope theo CompanyId; student data ownership qua identity claim.", "Controller/service code. Chỉ company service boundary có test evidence; HTTP/JWT chưa chạy."),
    @("Vị trí", "Company draft cần Admin approval trước khi xuất hiện; Quantity không giảm dưới AcceptedCount.", "Service tests xác nhận workflow/quantity; public student-list E2E chưa chạy."),
    @("Giới hạn xác nhận", "Kết quả mong đợi từ đặc tả + code; không coi việc đọc code là test runtime.", "Mọi case runtime chưa có bằng chứng được đánh Chưa chạy.")
)
$ruleRows = @($rules | Select-Object -Skip 1)

$dataRows = @(
    @("students-valid-20.csv", "20", "CSV hợp lệ UTF-8 BOM, tên tiếng Việt, MSSV TST.", "CSV", "Generated by deterministic PowerShell script; seed 20261012.", "Ready; not imported."),
    @("students-mixed-errors.csv", "9", "Thiếu giá trị; email sai; phone sai; duplicate case-insensitive; MSSV seed DB; formula-like field; Vietnamese quotes/commas.", "CSV", "Seeded generator; TSTDB0001 must be seeded in dedicated test DB to verify DB conflict.", "Ready; not imported."),
    @("students-missing-column.csv", "1", "Header thiếu ClassName.", "CSV", "Generated by deterministic PowerShell script.", "Ready; not uploaded."),
    @("students-large-1000.csv", "1,000", "Dữ liệu bulk hợp lệ.", "CSV", "Seed 20261012; generated deterministic.", "Ready; not imported/performance measured."),
    @("students-large-5000.csv", "5,000", "Dữ liệu bulk hợp lệ.", "CSV", "Seed 20261012; generated deterministic.", "Ready; not imported/performance measured."),
    @("students-large-5000.xlsx", "5,000", "Workbook fixture with 6 import columns.", "XLSX", "Generated from same deterministic 5,000-row dataset.", "Created; no API import."),
    @("students-invalid-extension.txt", "—", "Unsupported file extension.", "TXT", "Generated by script.", "Ready; not uploaded."),
    @("students-empty.csv", "0", "Zero-byte edge case.", "CSV", "Generated by script.", "Ready; not uploaded.")
)

$thresholds = @(
    @("Import CSV 100 rows", "≤1,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Import CSV 1,000 rows", "≤5,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Import CSV 5,000 rows", "≤20,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Export CSV 1,000", "≤2,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Export CSV 5,000", "≤8,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Export XLSX 1,000", "≤5,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Export XLSX 5,000", "≤20,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Paged student list with 5,000 rows", "≤1,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Dashboard summary", "≤500 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API."),
    @("Admin login", "≤1,000 ms", "5", "0", "—", "Chưa chạy", "No dedicated test DB/API.")
)

$defects = @(
    @("BUG-001", "TC-B02", "Tạo/cập nhật sinh viên ở service không trim/validate email và phone; API validator alone was not enough to protect direct service calls.", "Trung bình", "Đã sửa", "StudentService.cs; StudentSoftDeleteFilterTests.cs", "Trước: HEAD code omitted service-level checks. Pre-fix test attempt aborted at testhost socket bind (10013) before any assertion; no pre-fix pass/fail result. Sau: CreateStudentAsync_rejects_invalid_email_and_phone_number and UpdateStudentAsync_trims_fields_and_rejects_invalid_email_and_phone_number passed in final suite."),
    @("BUG-002", "TC-B01/TC-B03", "MSSV duplicate check was exact string equality and did not trim, so casing/whitespace variants were not normalized.", "Trung bình", "Đã sửa", "StudentService.cs; StudentSoftDeleteFilterTests.cs", "Trước: HEAD had exact equality and no trim. Pre-fix test attempt aborted before assertion; no pre-fix result. Sau: CreateStudentAsync_trims_fields_and_rejects_case_insensitive_duplicate_codes passed."),
    @("BUG-003", "TC-D01", "ExportStudentsAsync omitted HasAccount from filters although list query supports it.", "Thấp", "Đã sửa", "StudentService.cs; StudentSoftDeleteFilterTests.cs", "Trước: export applied search/status/company only; no HasAccount. Pre-fix test attempt aborted before assertion; no pre-fix result. Sau: ExportStudentsAsync_applies_has_account_filter passed."),
    @("OBS-004", "Build baseline", "NU1903 reports AutoMapper 13.0.1 has a known high-severity advisory (GHSA-rvv3-g6hj-g44x). Advisory was not independently assessed here.", "Cao", "Chưa sửa", "StudentInternshipMgmt.Application.csproj (unchanged)", "Observed as warning during final dotnet test. No package/source change made per instruction."),
    @("OBS-005", "Build baseline", "MSB3277 reports EF Core reference conflict between 8.0.10 and 8.0.31.", "Trung bình", "Chưa sửa", "Infrastructure/Application project references (unchanged)", "Observed as warning during final dotnet test; not investigated/fixed in this task.")
)

function Get-SheetRows($Items, [string[]]$Columns) {
    foreach ($item in $Items) {
        ,@($Columns | ForEach-Object { [string]$item.$_ })
    }
}

function Format-Sheet($Sheet, [string]$Title, [string[]]$Headers, $Rows, [double[]]$Widths) {
    $Sheet.Cells.Clear() | Out-Null
    $Sheet.Cells.Font.Name = "Aptos"
    $Sheet.Cells.Font.Size = 10
    $Sheet.Cells.VerticalAlignment = -4160
    $Sheet.Cells.WrapText = $true
    $lastColumn = $Headers.Count
    $Sheet.Range($Sheet.Cells.Item(1, 1), $Sheet.Cells.Item(1, $lastColumn)).Merge() | Out-Null
    $Sheet.Cells.Item(1, 1).Value2 = $Title
    $Sheet.Cells.Item(1, 1).Font.Bold = $true
    $Sheet.Cells.Item(1, 1).Font.Size = 16
    $Sheet.Cells.Item(1, 1).Interior.Color = 0x604217
    $Sheet.Cells.Item(1, 1).Font.Color = 0xFFFFFF
    for ($column = 1; $column -le $lastColumn; $column++) {
        $cell = $Sheet.Cells.Item(2, $column)
        $cell.NumberFormat = "@"
        $cell.Value2 = $Headers[$column - 1]
        $cell.Font.Bold = $true
        $cell.Font.Color = 0xFFFFFF
        $cell.Interior.Color = 0x604217
        $cell.WrapText = $true
        $Sheet.Columns.Item($column).ColumnWidth = $Widths[$column - 1]
    }
    for ($rowIndex = 0; $rowIndex -lt $Rows.Count; $rowIndex++) {
        $excelRow = $rowIndex + 3
        for ($column = 0; $column -lt $Headers.Count; $column++) {
            $cell = $Sheet.Cells.Item($excelRow, $column + 1)
            $cell.NumberFormat = "@"
            $cell.Value2 = [string]$Rows[$rowIndex][$column]
        }
    }
    $lastRow = [Math]::Max(2, $Rows.Count + 2)
    $range = $Sheet.Range($Sheet.Cells.Item(1, 1), $Sheet.Cells.Item($lastRow, $lastColumn))
    $range.WrapText = $true
    $range.VerticalAlignment = -4160
    $range.Borders.LineStyle = 1
    $range.Borders.Color = 0xD9E2F3
    $Sheet.Rows.Item(1).RowHeight = 28
    $Sheet.Rows.Item(2).RowHeight = 34
    if ($Rows.Count -gt 0) {
        $Sheet.Rows("3:$lastRow").EntireRow.AutoFit() | Out-Null
    }
    $Sheet.PageSetup.Orientation = 2
    $Sheet.PageSetup.PaperSize = 9
    $Sheet.PageSetup.Zoom = $false
    $Sheet.PageSetup.FitToPagesWide = 1
    $Sheet.PageSetup.FitToPagesTall = 0
    $Sheet.PageSetup.PrintTitleRows = '$1:$2'
}

$excel = $null
$workbook = $null
try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    $workbook = $excel.Workbooks.Add(-4167)
    $sheetDefinitions = @(
        @("Tổng quan", @("Hạng mục", "Kết quả"), @(
            @("Mục tiêu", "Đánh giá theo đặc tả quản lý sinh viên thực tập; không sửa dữ liệu thật."),
            @("Phạm vi", "Backend .NET 8 services/tests, API route source review, Vue 3 Vitest, data fixtures; API/DB E2E và hiệu năng bị chặn."),
            @("Môi trường", "Windows 11; .NET SDK 9.0.308; Node v22.19.0; SQL Server 2022 Developer; CPU i5-13420H; RAM 31.7 GB."),
            @("Dedicated test DB/API", "Không có StudentInternshipMgmtDb_Test; STUDENT_TEST_SQLSERVER unset; API :5018/frontend :5173 không listen. DB mặc định không bị truy cập/ghi."),
            @("Test case tổng", "=COUNTA('Test case'!A3:A41)"),
            @("Đạt", "=COUNTIF('Test case'!J3:J41,""Đạt"")"),
            @("Không đạt", "=COUNTIF('Test case'!J3:J41,""Không đạt"")"),
            @("Chưa chạy", "=COUNTIF('Test case'!J3:J41,""Chưa chạy"")"),
            @("Backend final", "245 passed; 0 failed; 1 skipped (246 total)."),
            @("Frontend final", "93 passed; 0 failed; 5 test files."),
            @("Skipped", "StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters — needs STUDENT_TEST_SQLSERVER dedicated test DB."),
            @("Trước/sau", "3 lỗi đã sửa; không có kết quả assertion pre-fix do testhost aborted with SocketException 10013 before running. Không quy baseline pass thành kết quả trước."),
            @("Cách đếm", "39 test cases được thiết kế độc lập với framework executions; case status formulas count 21/0/18; framework unit suite totals reported separately.")
        ), @(30, 100)),
        @("Quy tắc & KPI", @("Quy tắc", "Định nghĩa dùng trong test", "Nguồn/ghi chú"), $ruleRows, @(28, 55, 78)),
        @("Test case", $caseHeaders, $caseRows, @(12, 20, 15, 34, 30, 32, 28, 35, 40, 14, 36, 36)),
        @("Dữ liệu lỗi & lớn", @("Tệp", "Số dòng dữ liệu", "Mục tiêu kiểm thử", "Loại", "Nguồn/tạo dữ liệu", "Trạng thái"), $dataRows, @(34, 16, 55, 12, 57, 37)),
        @("Hiệu năng (trước-sau)", @("Thao tác", "Ngưỡng nhóm tự đặt", "Lần dự kiến", "Lần chạy", "Min/Median/Max ms", "Trạng thái", "Lý do/so sánh"), $thresholds, @(36, 22, 14, 13, 25, 16, 65)),
        @("Danh sách lỗi", @("Mã lỗi", "TC liên quan", "Mô tả", "Mức độ", "Trạng thái sửa", "File đã đổi/nguồn", "Trước/Sau và minh chứng"), $defects, @(16, 20, 60, 16, 18, 55, 85))
    )
    for ($index = 0; $index -lt $sheetDefinitions.Count; $index++) {
        if ($index -eq 0) { $sheet = $workbook.Worksheets.Item(1) } else { $sheet = $workbook.Worksheets.Add([Type]::Missing, $workbook.Worksheets.Item($workbook.Worksheets.Count)) }
        $definition = $sheetDefinitions[$index]
        $sheet.Name = $definition[0]
        Format-Sheet $sheet $definition[0] $definition[1] $definition[2] $definition[3]
        $sheet.Activate()
        $excel.ActiveWindow.SplitRow = 2
        $excel.ActiveWindow.FreezePanes = $true
        if ($sheet.Name -eq "Test case") {
            for ($row = 3; $row -le ($cases.Count + 2); $row++) {
                $statusCell = $sheet.Cells.Item($row, 10)
                switch ([string]$statusCell.Value2) {
                    "Đạt" { $statusCell.Interior.Color = 0xC6EFCE; $statusCell.Font.Color = 0x006100 }
                    "Không đạt" { $statusCell.Interior.Color = 0xC6C6FF; $statusCell.Font.Color = 0x0000C0 }
                    "Chưa chạy" { $statusCell.Interior.Color = 0xD9D9D9; $statusCell.Font.Color = 0x404040 }
                }
            }
        }
    }
    $overview = $workbook.Worksheets.Item("Tổng quan")
    for ($row = 3; $row -le 15; $row++) { $overview.Cells.Item($row, 2).NumberFormat = "@" }
    $overview.Cells.Item(8, 2).NumberFormat = "General"
    $overview.Cells.Item(8, 2).Formula = "=COUNTIF('Test case'!J3:J41,""Đạt"")"
    $overview.Cells.Item(9, 2).NumberFormat = "General"
    $overview.Cells.Item(9, 2).Formula = "=COUNTIF('Test case'!J3:J41,""Không đạt"")"
    $overview.Cells.Item(10, 2).NumberFormat = "General"
    $overview.Cells.Item(10, 2).Formula = "=COUNTIF('Test case'!J3:J41,""Chưa chạy"")"
    $overview.Cells.Item(7, 2).NumberFormat = "General"
    $overview.Cells.Item(7, 2).Formula = "=COUNTA('Test case'!A3:A41)"
    $overview.Cells.Item(15, 2).Value2 = "39 case: 21 Đạt / 0 Không đạt / 18 Chưa chạy. Backend 245 pass + 1 skipped; frontend 93 pass."
    $excel.CalculateFull()
    $workbook.SaveAs($xlsxPath, 51)
}
finally {
    if ($workbook -ne $null) { $workbook.Close($true) | Out-Null }
    if ($excel -ne $null) { $excel.Quit() }
    if ($workbook -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($workbook) | Out-Null }
    if ($excel -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null }
}

function Add-WordParagraph($Selection, [string]$Text, [int]$Size = 10, [bool]$Bold = $false, [int]$Color = 0) {
    $Selection.Font.Name = "Aptos"
    $Selection.Font.Size = $Size
    $Selection.Font.Bold = $Bold
    $Selection.Font.Color = $Color
    $Selection.ParagraphFormat.SpaceAfter = 5
    $Selection.TypeText($Text)
    $Selection.TypeParagraph()
}

function Add-WordTable($Document, $Selection, [object[]]$Rows, [int]$FontSize = 8) {
    $range = $Document.Range($Document.Content.End - 1, $Document.Content.End - 1)
    $table = $Document.Tables.Add($range, $Rows.Count, $Rows[0].Count)
    $table.Borders.Enable = 1
    $table.Range.Font.Name = "Aptos"
    $table.Range.Font.Size = $FontSize
    $table.Range.ParagraphFormat.SpaceAfter = 1
    for ($row = 0; $row -lt $Rows.Count; $row++) {
        for ($column = 0; $column -lt $Rows[$row].Count; $column++) {
            $cellValue = [string]$Rows[$row][$column]
            try {
                $table.Cell($row + 1, $column + 1).Range.Text = $cellValue
            }
            catch {
                throw "Word table cell assignment failed at row $($row + 1), column $($column + 1), value '$cellValue': $($_.Exception.Message)"
            }
        }
    }
    $table.Rows.Item(1).Range.Font.Bold = $true
    $table.Rows.Item(1).Shading.BackgroundPatternColor = 0x604217
    $table.Rows.Item(1).Range.Font.Color = 0xFFFFFF
    $table.AutoFitBehavior(1)
    $table.Range.InsertParagraphAfter()
    $Selection.SetRange($Document.Content.End - 1, $Document.Content.End - 1)
}

$word = $null
$document = $null
try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $document = $word.Documents.Add()
    $document.PageSetup.TopMargin = $word.InchesToPoints(0.55)
    $document.PageSetup.BottomMargin = $word.InchesToPoints(0.55)
    $document.PageSetup.LeftMargin = $word.InchesToPoints(0.65)
    $document.PageSetup.RightMargin = $word.InchesToPoints(0.65)
    $selection = $word.Selection

    Add-WordParagraph $selection "BÁO CÁO TEST CASE" 22 $true 0x604217
    Add-WordParagraph $selection "Quản lý sinh viên thực tập — InternshipManagement" 16 $true
    Add-WordParagraph $selection "Ngày lập: 12/10/2026 | Bộ kiểm thử theo đặc tả và code hiện tại" 10
    Add-WordParagraph $selection "Kết luận định lượng" 13 $true 0x604217
    $summaryTable = @(
        @("Nhóm số liệu", "Đạt", "Không đạt", "Chưa chạy / skipped"),
        @("Test case thiết kế (39)", "$passCount", "$failCount", "$unrunCount"),
        @("Backend dotnet test", "245", "0", "1 skipped"),
        @("Frontend Vitest", "93", "0", "0")
    )
    Add-WordTable -Document $document -Selection $selection -Rows $summaryTable -FontSize 9
    Add-WordParagraph $selection "Bằng chứng là output cuối được lưu trong evidence/logs. Các case Chưa chạy không được tính là Đạt. API/DB E2E và phép đo thời gian không chạy do không có dedicated test DB/API." 10
    Add-WordParagraph $selection "Không sử dụng StudentInternshipMgmtDb mặc định; không có migration, seed hoặc ghi dữ liệu vào DB hiện hữu." 10

    $selection.InsertBreak(7)
    Add-WordParagraph $selection "1. Mục tiêu, phạm vi và môi trường" 16 $true 0x604217
    Add-WordParagraph $selection "Mục tiêu: kiểm tra các quy tắc nhập/xuất, trạng thái sinh viên, KPI, đơn thực tập, phân quyền và frontend. Mong đợi được suy ra từ quy tắc được giao và implementation; code inspection không được coi là runtime test." 10
    Add-WordParagraph $selection "Phạm vi tự động chạy: backend .NET solution tests (245 passed, 0 failed, 1 skipped) và frontend Vitest (93 passed, 0 failed). Có 39 case truy vết theo mã trong workbook; 21 Đạt, 0 Không đạt, 18 Chưa chạy." 10
    Add-WordParagraph $selection "Môi trường: Windows 11 Home Single Language 10.0.26200; Intel Core i5-13420H; RAM 31.7 GB; .NET SDK 9.0.308 (solution target .NET 8); Node v22.19.0; SQL Server 2022 Developer 16.0.1200.5." 10
    Add-WordParagraph $selection "SQL Server service chạy nhưng chỉ database StudentInternshipMgmtDb được liệt kê. StudentInternshipMgmtDb_Test không tồn tại; STUDENT_TEST_SQLSERVER chưa cấu hình; API :5018 và frontend :5173 không có listener. Không cài Playwright. Chi tiết: evidence/logs/environment-and-api-check.txt." 10
    Add-WordParagraph $selection "Quy tắc chính: total loại soft-delete; tỷ lệ Completed/total * 100, làm tròn 2 chữ số, total=0 thì 0; gán doanh nghiệp/overdue lần lượt dùng ngưỡng cấu hình mặc định 5/5/3 ngày. Ngưỡng ứng tuyển 5 đơn mở, response due 7 ngày." 10
    Add-WordParagraph $selection "Import yêu cầu 6 cột, email đúng định dạng, phone 10 số bắt đầu 0, MSSV duy nhất case-insensitive, dòng lỗi bỏ qua theo partial import. Export dự kiến 10 cột tiếng Việt, có filter, không phân trang, CSV escaping/BOM và formula escaping." 10

    $selection.InsertBreak(7)
    Add-WordParagraph $selection "2. Thiết kế test case và kết quả" 16 $true 0x604217
    Add-WordParagraph $selection "39 case gồm các nhóm A–J; cột cuối workbook ghép Mức độ lỗi và Ghi chú để đáp ứng 12 cột bắt buộc mà vẫn giữ đủ trường thông tin." 10
    $groupRows = [Collections.Generic.List[object]]::new()
    $groupRows.Add([object[]]@("Nhóm", "Tổng", "Đạt", "Không đạt", "Chưa chạy"))
    foreach ($group in $groups) {
        $groupRows.Add([object[]]@($group.Group, "$($group.Total)", "$($group.Passed)", "$($group.Failed)", "$($group.Unrun)"))
    }
    Add-WordTable -Document $document -Selection $selection -Rows $groupRows.ToArray() -FontSize 8
    Add-WordParagraph $selection "Các test service xác nhận tạo đơn, giới hạn/trùng request, invitation, decision, company isolation, vị trí draft/approval/quantity, status history, password reset, form validation và role route helper." 10
    Add-WordParagraph $selection "Ca bị skipped: StudentSearchTests.Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters. Lý do test source ghi: cần STUDENT_TEST_SQLSERVER trỏ đến dedicated SQL Server test database; biến môi trường chưa thiết lập. Ca này được tính Chưa chạy, không tính Đạt." 10

    $selection.InsertBreak(7)
    Add-WordParagraph $selection "3. Dữ liệu lỗi, nhập/xuất và KPI" 16 $true 0x604217
    Add-WordParagraph $selection "Đã sinh fixtures có seed cố định 20261012: 20 dòng hợp lệ; 9 dòng trộn lỗi; header thiếu ClassName; 1,000/5,000 dòng CSV; XLSX 5,000 dòng; file TXT và file rỗng. Nguồn là script evidence/data/generate-input-data.ps1; không dùng dữ liệu cá nhân." 10
    Add-WordParagraph $selection "Tệp mixed có MSSV TSTDB0001 dùng làm mẫu trùng DB khi được seed trong DB riêng; fixture chưa upload. Formula-like =1+1 chỉ là text test input, không thực thi." 10
    Add-WordParagraph $selection 'Shared SpreadsheetSafety helper đã được unit test với =1+1, +cmd, -2, @SUM(A1); regression unit test xác nhận export áp HasAccount. Chưa kiểm tra HTTP import, file CSV/XLSX hoàn chỉnh hoặc font tiếng Việt bằng API.' 10
    Add-WordParagraph $selection "KPI summary và các mốc thời gian boundary chưa được chạy với dữ liệu DB biết trước. Mặc dù code implements công thức, không gán trạng thái Đạt cho các case KPI chưa có assertion." 10
    Add-WordParagraph $selection "Danh sách file, số dòng, mục đích và trạng thái có trong sheet 'Dữ liệu lỗi & lớn'; thư mục screenshots trống vì không có browser test session." 10

    $selection.InsertBreak(7)
    Add-WordParagraph $selection "4. Lỗi tìm được và trước/sau" 16 $true 0x604217
    Add-WordParagraph $selection "Ba lỗi đã được sửa trước khi người dùng yêu cầu dừng sửa source. Chỉ thêm regression coverage trong test project ở lượt này; không sửa thêm backend/src hoặc frontend/src." 10
    $defectTable = @(
        @("Mã", "Mô tả", "Trước", "Sau / test"),
        @("BUG-001", "Create/update service không trim/validate email/phone.", "Old code thiếu service checks. Pre-fix testhost aborted (SocketException 10013) trước assertions; không có kết quả test trước.", "Đã sửa; CreateStudentAsync_rejects_invalid_email_and_phone_number và UpdateStudentAsync_trims_fields_and_rejects_invalid_email_and_phone_number pass."),
        @("BUG-002", "Duplicate MSSV check exact-only; thiếu trim/case-insensitive.", "Old code so sánh chính xác. Pre-fix testhost aborted trước assertions.", "Đã sửa; CreateStudentAsync_trims_fields_and_rejects_case_insensitive_duplicate_codes pass."),
        @("BUG-003", "Export bỏ qua HasAccount.", "Old query chỉ apply search/status/company. Pre-fix testhost aborted trước assertions.", "Đã sửa; ExportStudentsAsync_applies_has_account_filter pass.")
    )
    Add-WordTable -Document $document -Selection $selection -Rows $defectTable -FontSize 8
    Add-WordParagraph $selection "Không ghi giả định thành kết quả pre-fix: log thật cho lần chạy code HEAD ở evidence/logs/pre-fix-regression-tests.txt cho biết test run aborted vì socket bind access denied, nên không có assertion result. Suite baseline trước đây đã pass nhưng không có các regression test này." 9
    Add-WordParagraph $selection "Điểm tồn chưa sửa: NU1903 báo AutoMapper 13.0.1 có advisory mức high (chưa đánh giá độc lập); MSB3277 cảnh báo reference EF Core 8.0.10/8.0.31. Hai mục được liệt kê OBS-004/005, không đổi dependency theo phạm vi." 9

    $selection.InsertBreak(7)
    Add-WordParagraph $selection "5. Hiệu năng — bảng trước/sau" 16 $true 0x604217
    Add-WordParagraph $selection "Không phát sinh thay đổi mã hiệu năng trong giai đoạn này. Không có phép đo trước hoặc sau: DB test riêng và API không sẵn sàng. Mỗi thao tác yêu cầu 5 lần nhưng số lần hoàn tất là 0; min/median/max được để trống, không thay bằng thời gian connect lỗi hay duration test unit." 10
    $perfRows = [Collections.Generic.List[object]]::new()
    $perfRows.Add([object[]]@("Thao tác", "Ngưỡng nhóm đặt", "Lần chạy", "Kết quả"))
    foreach ($row in $thresholds) { $perfRows.Add([object[]]@($row[0], $row[1], "$($row[3])/$($row[2])", "Chưa chạy")) }
    Add-WordTable -Document $document -Selection $selection -Rows $perfRows.ToArray() -FontSize 8
    Add-WordParagraph $selection "Threshold chỉ là target do nhóm tự đặt để đánh giá sau này; không phải SLA. CSV raw measurement-attempts.csv có 10 nhóm, 0 lần hoàn tất. Không thể kết luận pass/fail hay so sánh 100→1,000→5,000 trên runtime." 9

    $selection.InsertBreak(7)
    Add-WordParagraph $selection "6. Tái lập và minh chứng" 16 $true 0x604217
    Add-WordParagraph $selection "Dùng evidence/README.md để chạy lại trên môi trường sạch. Trước mọi E2E, tạo StudentInternshipMgmtDb_Test riêng, cấu hình ConnectionStrings__DefaultConnection và STUDENT_TEST_SQLSERVER bằng environment variables; không dùng connection string production/appsettings.json." 10
    Add-WordParagraph $selection "Tạo input: powershell -ExecutionPolicy Bypass -File .\\evidence\\data\\generate-input-data.ps1. Chạy backend: Set-Location backend; dotnet restore; dotnet test InternshipManagement.sln. Chạy frontend: Set-Location ..\\frontend; npm ci; npm test -- --run." 10
    Add-WordParagraph $selection "Để chạy SQL search test, STUDENT_TEST_SQLSERVER phải trỏ tới DB test dùng được và chỉ định rõ quyền đọc/ghi. API cần chạy với ConnectionStrings__DefaultConnection trỏ cùng DB riêng. Hướng dẫn migration an toàn và không ghi secrets có trong README." 10
    Add-WordParagraph $selection "Minh chứng chính: logs/dotnet-test-final.txt; logs/vitest-final.txt; logs/pre-fix-regression-tests.txt; logs/environment-and-api-check.txt; perf/measurement-attempts.csv; perf/measurement-not-run.txt; data/*.csv và data/*.xlsx." 10
    Add-WordParagraph $selection "Không có response API hoặc ảnh chụp UI. Output test build chứa warning dependency; không chứa token/mật khẩu. Cần kiểm tra lại DB setup, HTTP auth, import/export, KPI, route guards UI và hiệu năng trên môi trường QA riêng." 10

    $selection.InsertBreak(7)
    Add-WordParagraph $selection "BẢN TỰ ĐÁNH GIÁ" 18 $true 0x604217
    Add-WordParagraph $selection "Quyết định kiểm thử: dùng xUnit hiện có và Vitest vì đây là toolchain sẵn trong repository; fixture InMemory dùng cho service logic và test SQL Server chỉ chạy khi dedicated connection được cung cấp. Không khởi động test lên DB thật để tránh thay đổi dữ liệu ngoài phạm vi." 10
    Add-WordParagraph $selection "Xác định mong đợi: đối chiếu quy tắc được giao với implementation/DTO/controller/config. Khi test không thực thi trực tiếp (ví dụ API status, Excel tải về, KPI), không suy diễn từ code và đánh dấu Chưa chạy. Bộ test xác nhận những hành vi cụ thể có assertion." 10
    Add-WordParagraph $selection "Lý do còn ca Chưa chạy: không có database StudentInternshipMgmtDb_Test, biến STUDENT_TEST_SQLSERVER không cấu hình, API/frontend không lắng nghe; Playwright không cài. Test SQL search duy nhất bị skipped; import/export, dashboard, phân quyền HTTP và benchmark đều chưa thực thi." 10
    Add-WordParagraph $selection "Hạn chế: chỉ một máy Windows; dữ liệu benchmark và performance baseline chưa có; các ngưỡng do nhóm đề xuất; môi trường API chưa hoạt động. Lần chạy pre-fix trong worktree bị testhost chặn socket 10013 nên không tạo được bằng chứng assertion trước sửa. Kết quả trước chỉ mô tả code cũ, không ghi khống là test fail." 10
    Add-WordParagraph $selection "Cải tiến tiếp theo: cấu hình dedicated SQL Server trong CI; chạy API integration tests/TestServer và E2E route UI với Playwright; chạy tải k6 cho import/export/list/dashboard/login trên DB snapshot; thu min/median/max; rà advisory AutoMapper và đồng bộ EF Core package versions." 10
    Add-WordParagraph $selection "Kết quả cuối hiện tại: 39 case (21 Đạt, 0 Không đạt, 18 Chưa chạy); backend 245 passed, 0 failed, 1 skipped; frontend 93 passed. Không bao gồm case skipped trong số Đạt." 10

    $document.SaveAs2($docxPath, 12)
    $document.Repaginate()
    $pages = $document.ComputeStatistics(2)
    $document.Save()
    [IO.File]::WriteAllText((Join-Path $logDirectory "docx-page-count.txt"), "Word COM rendered pages: $pages`r`nFinal self-assessment begins on a manual page break and is constrained to the final page; inspect visual pagination before delivery.", [Text.UTF8Encoding]::new($false))
}
finally {
    if ($document -ne $null) { $document.Close(0) | Out-Null }
    if ($word -ne $null) { $word.Quit() }
    if ($document -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($document) | Out-Null }
    if ($word -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null }
}

Write-Output "Created $xlsxPath and $docxPath. Test-case rows=$($cases.Count); statuses=$passCount/$failCount/$unrunCount."
