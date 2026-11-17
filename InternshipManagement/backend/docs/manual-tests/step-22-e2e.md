# STEP 22 - SQL Server end-to-end integration test

## Environment

| Item | Actual |
|---|---|
| SQL Server | `localhost`, Windows Authentication |
| Database | `StudentInternshipMgmtDb_E2E` (separate from `StudentInternshipMgmtDb`) |
| Migrations | `dotnet ef database update` applied to the E2E database |
| API | `http://127.0.0.1:5018` |
| Expiry worker | Enabled, `PlacementRequestExpiry__IntervalMinutes=1` |
| Dashboard threshold | `OverdueRequestAlertDays=2` |
| Email | `Smtp__Enabled=false` |
| Runner | `docs/manual-tests/run-e2e.ps1`, creates fixture data through Admin/API; concurrency default 10 rounds per race |

The E2E runner refuses to proceed unless the connection string explicitly names
`StudentInternshipMgmtDb_E2E`, the JWT signing key is present and at least 32
characters, SMTP is disabled, and `sqlcmd` is available. Direct SQL updates are
limited to `DueAt` or nullable legacy fields in that E2E database.

Example PowerShell setup (from `backend`; use a fresh, private signing key):

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=StudentInternshipMgmtDb_E2E;Integrated Security=True;TrustServerCertificate=True;"
$env:Jwt__SecretKey = "<private-key-at-least-32-characters>"
$env:Smtp__Enabled = "false"
$env:PlacementRequestExpiry__Enabled = "true"
$env:PlacementRequestExpiry__IntervalMinutes = "1"
$env:OverdueRequestAlertDays = "2"
dotnet run --no-launch-profile --project src/StudentInternshipMgmt.Api
```

In another PowerShell window, set the same database/JWT/SMTP environment values.
For a fresh seeded E2E database, set `E2E_ADMIN_NEW_PASSWORD` to a private
password of at least 12 characters and run
`.\docs\manual-tests\run-e2e.ps1 -ConcurrencyIterations 10`. For later runs,
set `E2E_ADMIN_PASSWORD` to the Admin password established on the first run.
The runner adds random-suffixed test records through Admin endpoints.

## Scenario results

Full runner result: **48 PASS, 1 FAIL, 1 SKIP**; the runner exited 1 because race
i round 1 received one HTTP 200 and one response with no HTTP status (`0`),
rather than the required 400/409 conflict. The remaining nine rounds of race i
and all rounds of races ii-vi passed their response and consistency checks.

| Scenario | Preconditions | Steps | Expected | Actual | Result |
|---|---|---|---|---|---|
| a. Company position and application | E2E DB migrated; Admin, company, student accounts available | Company creates and submits a position; Admin approves; student applies; Company reads its requests | Position approved, application is visible in the owning Company list | Position 1082 approved; request 91 visible to owning Company | PASS |
| b. Interview report and Admin Passed | Approved positions; student has two open applications with different companies | Company invites; student reports interview; Admin confirms Passed with source | Student becomes Accepted; other open application becomes Rejected; StatusHistory actor is Admin | Request 91 Accepted; request 92 Rejected; StatusHistory actor Admin | PASS |
| c. Company Passed and Failed | Approved position and fresh student applications | Company records Passed; then attempt Failed without note and with note | Passed accepts student; Failed without note is rejected; Failed with required note is recorded | Direct Passed succeeded; Failed without note HTTP 400; Failed with note persisted | PASS |
| d1. Admin Failed | InterviewInvited application with student report | Admin confirms Failed with confirmation source and note | Request is Rejected and student remains without a company | Admin Failed persisted | PASS |
| d2. DismissReport blocks another report | InterviewInvited application with student report | Admin dismisses report; student tries to report the same interview again | Dismissal is recorded and repeat report is rejected | Dismissal persisted; report could not be resubmitted | PASS |
| d3. Cancel Accepted reopens capacity | Approved position with Quantity=1 | Fill final slot; Admin cancels Accepted result | Student returns to NoCompany; closed approved position reopens | Cancel returned NoCompany and reopened the full position | PASS |
| e. Full position auto-close | Approved position with Quantity=1 and one eligible student | Company records Passed for final slot | IsOpen becomes false and AcceptedCount does not exceed Quantity | Quantity=1; AcceptedCount=1; IsOpen=False | PASS |
| Authorization matrix | Valid Admin, User, Company and anonymous requests | Test GET on `/api/students`, `/api/companies`, `/api/job-positions`, `/api/placement-requests`, `/api/company-me`, `/api/me`, `/api/dashboard`, `/api/notifications` for all four identities | HTTP 200/401/403 matches endpoint role policy | All 32 role/token checks matched controller authorization design | PASS |
| Company tenant isolation | Two different Company accounts; position and request owned by Company A | Company B attempts reads and writes against A's position and request | Every cross-company operation returns 404 or 403 | Foreign job/request reads and writes returned HTTP 404/403 | PASS |
| Student cannot mutate workflow state | Valid User token | User calls status-change, Company result and Admin result endpoints | Every operation is rejected with 403 | Student state/result mutations returned HTTP 403 | PASS |
| SMTP disabled | API started with `Smtp__Enabled=false` | Execute placement, interview and result operations | Email no-op does not affect business operations | Application, approval, report, acceptance, and failure operations completed | PASS |
| SMTP fake-server delivery | smtp4dev or Papercut available and configured | Optional SMTP integration run | SMTP message received by fake server | Not run: no smtp4dev/Papercut process was available; real SMTP was not enabled | Chưa chạy |
| Legacy nullable fields | Fresh E2E request; seven workflow fields set null directly in E2E DB | Read via `/api/me/placement-requests` and Admin `/api/placement-requests` | Both endpoints return the request and all seven fields are null (no HTTP 500) | Request 99 returned all seven nullable fields as null on student/Admin endpoints | PASS |
| Expiry worker and dashboard | API worker interval 1 minute; dashboard alert threshold 2 days | Verify due-soon dashboard item; set Pending and InterviewInvited DueAt to past in E2E DB; wait up to 2 minutes; attempt resubmission | Pending becomes Expired; InterviewInvited stays unchanged; student can reapply; dashboard surfaces due-soon request | Pending 100 expired within 2 minutes; InterviewInvited 101 unchanged; reapplication 102 succeeded; dashboard included due-soon item | PASS |
| Concurrency i. Two Companies Passed one student | Two companies and open requests for the same NoCompany student | Start two `Start-Job` result calls; run 10 rounds | Each round: exactly one 200 and one 400/409; exactly one accepted; no open request remains for that student | Round 1 returned `0,200` instead of `400/409,200`; remaining rounds passed; student/request consistency checks passed | FAIL |
| Concurrency ii. Two students take final slot | Approved position with one vacancy; separate students' open requests | Start two `Start-Job` result calls; run 10 rounds | Each round: exactly one 200 and one 400/409; position closes; accepted count is at most Quantity; no accepted student retains an open request | All 10 rounds returned one 200 and one 400/409; post-state checks passed | PASS |
| Concurrency iii. Admin Failed + Failed | InterviewInvited request with student report | Submit two Admin Failed decisions concurrently; run 10 rounds | Each round: exactly one 200 and one 400/409; final data is consistent | All 10 rounds returned one 200 and one 400/409; post-state checks passed | PASS |
| Concurrency iv. Admin Failed + DismissReport | InterviewInvited request with student report | Submit Admin Failed and DismissReport concurrently; run 10 rounds | Each round: exactly one 200 and one 400/409; dismissal winner cannot be reported again | All 10 rounds returned one 200 and one 400/409; post-state checks passed | PASS |
| Concurrency v. Admin Passed + DismissReport | InterviewInvited request with student report | Submit Admin Passed and DismissReport concurrently; run 10 rounds | Each round: exactly one 200 and one 400/409; final result has an actor and no invalid accepted/open combination | All 10 rounds returned one 200 and one 400/409; post-state checks passed | PASS |
| Concurrency vi. Company Failed + Admin Passed | InterviewInvited request with student report | Submit Company Failed and Admin Passed concurrently; run 10 rounds | Each round: exactly one 200 and one 400/409; final state and position capacity remain consistent | All 10 rounds returned one 200 and one 400/409; post-state checks passed | PASS |

## Findings

| Severity | Finding | Disposition |
|---|---|---|
| Medium | Admin placement-request list omitted the `StudentReportDismissedAt` projection, so the dismissal timestamp was not returned there. | Fixed by adding the projection and a focused unit assertion; build, unit suite, and E2E legacy-field checks passed. |
| Chưa phân loại | In concurrency race i round 1, one `Start-Job` request produced no HTTP status (`0`) while its peer returned 200. Server/database post-state consistency checks passed, but the expected explicit 400/409 response was not observed. | Unresolved; no product change made because the cause is not established. Preserve as a failed check and investigate/reproduce the client/transport result. |
| Low (test tooling, fixed) | Initial runner SQL connection-string guard assigned through a property interpreted as an unsupported SQL connection-string keyword. | Changed the guard to construct `SqlConnectionStringBuilder` from the supplied string; rerun completed against the E2E database. |

## Build and test results

| Command | Actual result |
|---|---|
| `dotnet build` | PASS — Build succeeded, 0 warnings, 0 errors. |
| `dotnet test InternshipManagement.sln -m:1` | PASS — 285 passed, 1 skipped, 0 failed (201 infrastructure tests; 85 application tests). |
| `npm test` | PASS — 8 test files, 111 tests passed. |
| `npm run build` | PASS — Vite 8.3.0; 480 modules transformed; built in 942 ms. |

## Chạy lại (STEP 23)

Giữ nguyên toàn bộ kết quả STEP 22 ở trên. Runner được cập nhật để xác thực
chặt chẽ `StudentInternshipMgmtDb_E2E`, chỉ dừng API trên loopback port đã chọn,
chỉ drop/create database E2E, chạy `dotnet ef database update`, rồi tự khởi
động API với log có timestamp. Connection string, signing key và cấu hình SMTP
chỉ được thiết lập trong process của runner và được khôi phục khi kết thúc.
Mật khẩu Admin ngẫu nhiên 32 ký tự chỉ tồn tại trong bộ nhớ process; không ghi
vào log, file hay environment. Migration `ForceAdminPasswordChange` dùng
`UpdateData` để đặt `MustChangePassword=true` cho Admin seed. Runner chỉ đọc và
assert giá trị này sau khi migrations hoàn tất, không tự cập nhật cờ trong DB.
Đăng nhập bằng `Admin@123` phải trả `mustChangePassword=true` trước khi runner
thực hiện luồng đổi mật khẩu.

Nguyên nhân trực tiếp của giá trị `0` cũ là `catch` trong `Invoke-Race`: nếu
exception không có `Response.StatusCode`, script trả `0` và làm mất loại cùng
nội dung exception. `Token` đã được truyền vào đối số của mỗi job; không phải
lỗi do thiếu đối số. Lần chạy cũ không lưu exception hoặc log nên không thể
xác định hồi tố chính xác exception phía client nào đã tạo ra giá trị đó. Ở
STEP 23, runner gọi `GET /api/auth/me` với cả hai token trước mỗi cuộc đua; hai
job chờ cùng mốc `startAt` sau 3 giây; timeout cho từng request là 30 giây. Mỗi
job trả status, body, loại/lời nhắn exception và thời điểm bắt đầu/kết thúc có
độ chính xác mili giây. Khi không có HTTP status, kết quả hiện dạng
`ERR:<ExceptionType>`.

| Race | Kết quả |
|---|---|
| i. Hai doanh nghiệp cùng ghi Đạt cho một sinh viên | **50/50 đúng**: mỗi lượt đúng một `200` và một `400/409`; 0 lượt lạ. |
| ii. Hai sinh viên tranh vị trí cuối | **10/10 đúng**. |
| iii. Admin Failed + Failed | **10/10 đúng**. |
| iv. Admin Failed + DismissReport | **10/10 đúng**. |
| v. Admin Passed + DismissReport | **10/10 đúng**. |
| vi. Company Failed + Admin Passed | **10/10 đúng**. |

Kết quả `run-e2e.ps1`: **50 PASS, 0 FAIL, 1 SKIP**. Không tái hiện lỗi thiếu
HTTP status sau 50 lượt race (i); theo bằng chứng, **khả năng cao là khởi động
nguội ở lần chạy đầu**.

Log API ghi nhận 108 dòng `Error Number:1205`; không có `Error Number:1222`,
không có exception không được xử lý dẫn tới HTTP 500 và không có lỗi kết nối
đóng/reset. Ví dụ
lúc 09:17:55.244, request 13 gặp SQL deadlock 1205; log ghi `Outcome Conflict` cho
request 13 đồng thời request 14 thành công. Luồng hiện có chuyển lỗi cơ sở dữ
liệu thành `PlacementRequestOperationStatus.Conflict` và controller trả 409;
không có HTTP 500 nên không cần thay đổi code nghiệp vụ.

SMTP giả: **Chưa chạy** — không tìm thấy process smtp4dev hoặc Papercut; SMTP
thật vẫn tắt.

| Lệnh | Kết quả thực tế |
|---|---|
| `dotnet build` | PASS — Build succeeded, 0 warnings, 0 errors. |
| `dotnet test InternshipManagement.sln -m:1` | PASS — 285 passed, 1 skipped, 0 failed (200 Infrastructure.Tests; 85 StudentInternshipMgmt.Tests). |
| `npm test` | PASS — 8 test files, 111 tests passed. |
| `npm run build` | PASS — Vite 8.3.0; 480 modules transformed; built in 1.08 s. |

## Xác minh MustChangePassword (STEP 24)

Sau khi drop/create riêng `StudentInternshipMgmtDb_E2E` và chạy
`dotnet ef database update`, câu
`SELECT MustChangePassword FROM dbo.Users WHERE Username = N'admin';` trả về
`1` trước mọi câu `UPDATE` thủ công. Migration
`20261012073419_ForceAdminPasswordChange` đặt giá trị này qua `UpdateData`;
script hiện chỉ assert giá trị đọc được là `1`, sau đó xác nhận đăng nhập bằng
`Admin@123` trả `mustChangePassword=true`. Không có cập nhật thủ công để ép cờ.

Lần chạy đầy đủ đầu tiên: **44 PASS, 6 FAIL, 1 SKIP**. Kiểm tra bootstrap Admin
PASS; các lỗi xuất hiện trong những kịch bản race sau đó, với API log ghi nhận
SQL deadlock `1205`, timeout chờ connection pool và lỗi kết nối tới SQL Server.
Lần chạy lại không bắt đầu được: `sqlcmd` không kết nối được do service
`MSSQLSERVER` đang Stopped; không thể khởi động service bằng quyền hiện tại.
Do đó chưa có kết quả E2E đầy đủ thành công cho STEP 24. Không thay đổi các kết
quả STEP 22/23 ghi ở trên.

| Lệnh kiểm tra STEP 24 | Kết quả thực tế |
|---|---|
| `dotnet build` | PASS — Build succeeded, 0 warnings, 0 errors. |
| `dotnet test InternshipManagement.sln -m:1` | PASS — 285 passed, 1 skipped, 0 failed (200 Infrastructure.Tests; 85 StudentInternshipMgmt.Tests). |
| `npm test` | PASS — 8 test files, 111 tests passed. |
| `npm run build` | PASS — Vite 8.3.0; 480 modules transformed; built in 6.12 s. |
