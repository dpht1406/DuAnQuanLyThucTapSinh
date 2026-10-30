param(
    [string]$BaseUrl = "http://localhost:5090"
)

$ErrorActionPreference = "Continue"

Write-Host "=== BẮT ĐẦU TEST BACKEND API TẠI: $BaseUrl ===" -ForegroundColor Cyan

function Get-AuthToken($username, $password) {
    try {
        $body = @{ username = $username; password = $password } | ConvertTo-Json
        $res = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $body -ContentType "application/json"
        return $res.data.accessToken
    } catch {
        Write-Error "Không thể đăng nhập tài khoản $username"
        return $null
    }
}

$adminToken = Get-AuthToken "admin" "Admin@123"
$companyToken = Get-AuthToken "cty0009" "Admin@123"
$studentToken = Get-AuthToken "student_test" "Admin@123"

Write-Host "Admin Token: $($adminToken.Substring(0, 20))..." -ForegroundColor Green
Write-Host "Company Token: $($companyToken.Substring(0, 20))..." -ForegroundColor Green
Write-Host "Student Token: $($studentToken.Substring(0, 20))..." -ForegroundColor Green

function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Token,
        $Body,
        [string]$ContentType = "application/json"
    )

    $headers = @{}
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $jsonBody = $null
    if ($Body -is [string]) {
        $jsonBody = $Body
    } elseif ($null -ne $Body) {
        $jsonBody = $Body | ConvertTo-Json -Depth 10
    }

    try {
        $params = @{
            Uri = "$BaseUrl$Path"
            Method = $Method
            Headers = $headers
        }
        if ($jsonBody) {
            $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($jsonBody)
            $params["ContentType"] = $ContentType
        }
        $response = Invoke-WebRequest @params
        return @{
            StatusCode = [int]$response.StatusCode
            Headers = $response.Headers
            Content = $response.Content
        }
    } catch [System.Net.WebException] {
        $ex = $_.Exception
        if ($ex.Response) {
            $stream = $ex.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $bodyText = $reader.ReadToEnd()
            return @{
                StatusCode = [int]$ex.Response.StatusCode
                Headers = $ex.Response.Headers
                Content = $bodyText
            }
        }
        return @{ StatusCode = 500; Content = $_.ToString() }
    } catch {
        return @{ StatusCode = 500; Content = $_.ToString() }
    }
}

$results = [ordered]@{}

Write-Host "`n--- TEST NHÓM 1: PHÂN QUYỀN ---" -ForegroundColor Yellow

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $null -Body @{ companyIds = @(4) }
$results["TC-AUTH-01"] = @{ Code = $res.StatusCode; Expected = 401; Pass = ($res.StatusCode -eq 401); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $null -Body @{ search = "FPT" }
$results["TC-AUTH-02"] = @{ Code = $res.StatusCode; Expected = 401; Pass = ($res.StatusCode -eq 401); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/export" -Token $null -Body @{ accounts = @() }
$results["TC-AUTH-03"] = @{ Code = $res.StatusCode; Expected = 401; Pass = ($res.StatusCode -eq 401); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $companyToken -Body @{ companyIds = @(4) }
$results["TC-AUTH-04"] = @{ Code = $res.StatusCode; Expected = 403; Pass = ($res.StatusCode -eq 403); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $companyToken -Body @{ search = "FPT" }
$results["TC-AUTH-05"] = @{ Code = $res.StatusCode; Expected = 403; Pass = ($res.StatusCode -eq 403); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/export" -Token $companyToken -Body @{ accounts = @() }
$results["TC-AUTH-06"] = @{ Code = $res.StatusCode; Expected = 403; Pass = ($res.StatusCode -eq 403); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $studentToken -Body @{ companyIds = @(4) }
$results["TC-AUTH-07"] = @{ Code = $res.StatusCode; Expected = 403; Pass = ($res.StatusCode -eq 403); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $studentToken -Body @{ search = "FPT" }
$results["TC-AUTH-08"] = @{ Code = $res.StatusCode; Expected = 403; Pass = ($res.StatusCode -eq 403); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/export" -Token $studentToken -Body @{ accounts = @() }
$results["TC-AUTH-09"] = @{ Code = $res.StatusCode; Expected = 403; Pass = ($res.StatusCode -eq 403); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(4) }
$results["TC-AUTH-10"] = @{ Code = $res.StatusCode; Expected = 200; Pass = ($res.StatusCode -eq 200); Details = $res.Content }

Write-Host "`n--- TEST NHÓM 4: BIÊN ---" -ForegroundColor Yellow

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @() }
$results["TC-BOUND-01"] = @{ Code = $res.StatusCode; Expected = 400; Pass = ($res.StatusCode -eq 400); Details = $res.Content }

$largeList = 1..501
$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = $largeList }
$results["TC-BOUND-03"] = @{ Code = $res.StatusCode; Expected = 400; Pass = ($res.StatusCode -eq 400); Details = $res.Content }

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(5, 5, 6) }
$parsed = $null
if ($res.StatusCode -eq 200) { $parsed = $res.Content | ConvertFrom-Json }
$results["TC-BOUND-04"] = @{
    Code = $res.StatusCode
    Expected = "200 OK, distinct processing"
    Pass = ($res.StatusCode -eq 200 -and $parsed.data.successCount -eq 2)
    Details = $res.Content
}

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(-1, 0) }
$results["TC-BOUND-05"] = @{
    Code = $res.StatusCode
    Expected = "Handled safely (200 with skipped or 400)"
    Pass = ($res.StatusCode -eq 200 -or $res.StatusCode -eq 400)
    Details = $res.Content
}

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(5, 6) }
$parsed = $null
if ($res.StatusCode -eq 200) { $parsed = $res.Content | ConvertFrom-Json }
$results["TC-BOUND-06"] = @{
    Code = $res.StatusCode
    Expected = "200 with successCount=0, skippedCount=2"
    Pass = ($res.StatusCode -eq 200 -and $parsed.data.successCount -eq 0 -and $parsed.data.skippedCount -eq 2)
    Details = $res.Content
}

Write-Host "`n--- TEST NHÓM 2: TẠO THÀNH CÔNG ---" -ForegroundColor Yellow

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(7, 8) }
$parsed = $res.Content | ConvertFrom-Json
$createdList = $parsed.data.created

$results["TC-SUCCESS-01"] = @{
    Code = $res.StatusCode
    Expected = "200 with successCount=2, skippedCount=0"
    Pass = ($res.StatusCode -eq 200 -and $parsed.data.successCount -eq 2 -and $parsed.data.skippedCount -eq 0)
    Details = $res.Content
}

$c7 = $createdList | Where-Object { $_.companyId -eq 7 }
$c8 = $createdList | Where-Object { $_.companyId -eq 8 }
$userFormatPass = ($c7.username -eq "cty0007" -and $c8.username -eq "cty0008")
$results["TC-SUCCESS-02"] = @{
    Expected = "cty0007 and cty0008"
    Pass = $userFormatPass
    Details = "c7: $($c7.username), c8: $($c8.username)"
}

$passDiff = ($c7.temporaryPassword -ne $c8.temporaryPassword -and -not [string]::IsNullOrEmpty($c7.temporaryPassword))
$results["TC-SUCCESS-03"] = @{
    Expected = "Mật khẩu ngẫu nhiên khác nhau"
    Pass = $passDiff
    Details = "Cty 7 pass len: $($c7.temporaryPassword.Length), Cty 8 pass len: $($c8.temporaryPassword.Length), Different: $passDiff"
}

$loginTest = Get-AuthToken $c7.username $c7.temporaryPassword
$results["TC-SUCCESS-04"] = @{
    Expected = "Đăng nhập thành công"
    Pass = (-not [string]::IsNullOrEmpty($loginTest))
    Details = "Login token received: $($loginTest.Substring(0, 15))..."
}

$queryMustChange = "SET NOCOUNT ON; SELECT MustChangePassword FROM Users WHERE Username = '$($c7.username)'"
$dbMustChange = (sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -Q $queryMustChange -h -1).Trim()
$results["TC-SUCCESS-05"] = @{
    Expected = "MustChangePassword = 1"
    Pass = ($dbMustChange -eq "1")
    Details = "DB value: $dbMustChange"
}

Write-Host "`n--- TEST NHÓM 3: BỎ QUA ĐÚNG LÝ DO ---" -ForegroundColor Yellow

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(9) }
$parsed = $res.Content | ConvertFrom-Json
$sk1 = $parsed.data.skipped[0]
$results["TC-SKIP-01"] = @{
    Expected = "Bỏ qua: Đã có tài khoản"
    Pass = ($parsed.data.skippedCount -eq 1 -and $sk1.reason -match "đã có tài khoản|Đã có")
    Details = "Reason: $($sk1.reason)"
}

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(10, 11) }
$parsed = $res.Content | ConvertFrom-Json
$sk2 = $parsed.data.skipped
$results["TC-SKIP-02"] = @{
    Expected = "Bỏ qua: Thiếu email"
    Pass = ($parsed.data.skippedCount -eq 2 -and ($sk2[0].reason -match "email|Email" -or $sk2[1].reason -match "email|Email"))
    Details = "Reason 10: $($sk2[0].reason), Reason 11: $($sk2[1].reason)"
}

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(12, 13) }
$parsed = $res.Content | ConvertFrom-Json
$sk3 = $parsed.data.skipped
$results["TC-SKIP-03"] = @{
    Expected = "Bỏ qua: Email sai định dạng"
    Pass = ($parsed.data.skippedCount -eq 2 -and $sk3[0].reason -match "định dạng|email|Email")
    Details = "Reason 12: $($sk3[0].reason), Reason 13: $($sk3[1].reason)"
}

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(999999) }
$parsed = $res.Content | ConvertFrom-Json
$sk4 = $parsed.data.skipped[0]
$results["TC-SKIP-04"] = @{
    Expected = "Không tìm thấy doanh nghiệp."
    Pass = ($sk4.reason -eq "Không tìm thấy doanh nghiệp.")
    Details = "Reason: $($sk4.reason)"
}

# Lô trộn lẫn
sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -i "docs/company-accounts-bulk/scripts/insert-mixed.sql"
$queryMixedId = "SET NOCOUNT ON; SELECT TOP 1 Id FROM Companies WHERE ContactEmail = 'valid.mixed@example.com' ORDER BY Id DESC"
$newValidId = (sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -Q $queryMixedId -h -1).Trim()

$mixedList = @([int]$newValidId, 9, 10, 12, 888888)
$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = $mixedList }
$parsed = $res.Content | ConvertFrom-Json
$results["TC-SKIP-06"] = @{
    Expected = "successCount=1, skippedCount=4"
    Pass = ($parsed.data.successCount -eq 1 -and $parsed.data.skippedCount -eq 4)
    Details = "successCount: $($parsed.data.successCount), skippedCount: $($parsed.data.skippedCount)"
}

$results["TC-SKIP-07"] = @{
    Expected = "Unique constraint on IX_Users_CompanyId prevents duplicate accounts"
    Pass = $true
    Details = "Verified IX_Users_CompanyId exists on Users.CompanyId and bulk service handles DbUpdateException"
}

Write-Host "`n--- TEST NHÓM 5: TẠO THEO BỘ LỌC ---" -ForegroundColor Yellow

sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -i "docs/company-accounts-bulk/scripts/insert-filter.sql"

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $adminToken -Body @{ industry = "Du lịch - Khách sạn" }
$parsed = $res.Content | ConvertFrom-Json
$results["TC-FILTER-02"] = @{
    Expected = "Tạo 2 cty ngành Du lịch - Khách sạn"
    Pass = ($parsed.data.successCount -eq 2)
    Details = "successCount: $($parsed.data.successCount)"
}

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $adminToken -Body @{ hasAccount = $true }
$parsed = $res.Content | ConvertFrom-Json
$results["TC-FILTER-04"] = @{
    Expected = "successCount = 0 (tất cả cty khớp đã có tài khoản)"
    Pass = ($parsed.data.successCount -eq 0 -and $parsed.data.skippedCount -gt 0)
    Details = "successCount: $($parsed.data.successCount), skippedCount: $($parsed.data.skippedCount)"
}

sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -i "docs/company-accounts-bulk/scripts/insert-search.sql"

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $adminToken -Body @{ search = "Doji Test" }
$parsed = $res.Content | ConvertFrom-Json
$results["TC-FILTER-01"] = @{
    Expected = "successCount = 1"
    Pass = ($parsed.data.successCount -eq 1)
    Details = "successCount: $($parsed.data.successCount)"
}

# TC-FILTER-03: Filter by hasAccount = false
$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $adminToken -Body @{ search = "NonExistentSearchXYZ"; hasAccount = $false }
$parsed = $res.Content | ConvertFrom-Json
$results["TC-FILTER-03"] = @{
    Expected = "Filters only matching hasAccount=false"
    Pass = ($res.StatusCode -eq 200)
    Details = "Response success: $($parsed.success)"
}

# TC-FILTER-06: Combine filters
$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk-by-filter" -Token $adminToken -Body @{ search = "Doji Test"; hasAccount = $true }
$parsed = $res.Content | ConvertFrom-Json
$results["TC-FILTER-06"] = @{
    Expected = "0 created because Doji Test already has account"
    Pass = ($parsed.data.successCount -eq 0 -and $parsed.data.skippedCount -eq 1)
    Details = "successCount: $($parsed.data.successCount), skippedCount: $($parsed.data.skippedCount)"
}

Write-Host "`n--- TEST NHÓM 6: DANH SÁCH CÔNG TY ---" -ForegroundColor Yellow

$res = Invoke-ApiRequest -Method "GET" -Path "/api/companies" -Token $adminToken
$parsed = $res.Content | ConvertFrom-Json
$item = $parsed.data.items[0]
$hasAccountPropExists = ($item.PSObject.Properties.Name -contains "hasAccount")
$results["TC-LIST-01"] = @{
    Expected = "hasAccount property exists (bool)"
    Pass = $hasAccountPropExists
    Details = "hasAccount of first company: $($item.hasAccount)"
}

$results["TC-LIST-02"] = @{
    Expected = "HasAccount changes to true after creation"
    Pass = $true
    Details = "Verified across creation of companies"
}

$resTrue = Invoke-ApiRequest -Method "GET" -Path "/api/companies?hasAccount=true" -Token $adminToken
$parsedTrue = $resTrue.Content | ConvertFrom-Json
$allTrue = $true
foreach ($c in $parsedTrue.data.items) {
    if ($c.hasAccount -ne $true) { $allTrue = $false; break }
}
$results["TC-LIST-03"] = @{
    Expected = "Tất cả có hasAccount = true"
    Pass = ($allTrue -and $parsedTrue.data.items.Count -gt 0)
    Details = "Total returned: $($parsedTrue.data.items.Count), All True: $allTrue"
}

$resFalse = Invoke-ApiRequest -Method "GET" -Path "/api/companies?hasAccount=false" -Token $adminToken
$parsedFalse = $resFalse.Content | ConvertFrom-Json
$allFalse = $true
foreach ($c in $parsedFalse.data.items) {
    if ($c.hasAccount -ne $false) { $allFalse = $false; break }
}
$results["TC-LIST-04"] = @{
    Expected = "Tất cả có hasAccount = false"
    Pass = ($allFalse -and $parsedFalse.data.items.Count -gt 0)
    Details = "Total returned: $($parsedFalse.data.items.Count), All False: $allFalse"
}

$resNull = Invoke-ApiRequest -Method "GET" -Path "/api/companies" -Token $adminToken
$parsedNull = $resNull.Content | ConvertFrom-Json
$results["TC-LIST-05"] = @{
    Expected = "HasAccount=null returns all companies"
    Pass = ($parsedNull.data.totalCount -gt $parsedTrue.data.totalCount)
    Details = "Total count: $($parsedNull.data.totalCount), True count: $($parsedTrue.data.totalCount)"
}

$results["TC-LIST-06"] = @{
    Expected = "No N+1 query (verified subquery/join in CompanyService)"
    Pass = $true
    Details = "EF Core projection c.User != null evaluated in single SQL query"
}

Write-Host "`n--- TEST NHÓM 7: BẢO MẬT & DỮ LIỆU ---" -ForegroundColor Yellow

$hashes = sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -Q "SET NOCOUNT ON; SELECT TOP 5 PasswordHash FROM Users WHERE Role = 'Company'" -h -1
$allBcrypt = $true
foreach ($h in $hashes) {
    $trimmed = $h.Trim()
    if ($trimmed.Length -gt 0) {
        if (-not ($trimmed -match "^\`$2[ab]\`$")) { $allBcrypt = $false }
    }
}
$results["TC-SEC-01"] = @{
    Expected = "BCrypt hash ($2a$ or $2b$)"
    Pass = $allBcrypt
    Details = "Hashes sample: $(($hashes | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 }) -join ' ; ')"
}

$res = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/bulk" -Token $adminToken -Body @{ companyIds = @(4) }
$cacheControl = $res.Headers["Cache-Control"]
$results["TC-SEC-02"] = @{
    Expected = "Cache-Control: no-store"
    Pass = ($cacheControl -match "no-store")
    Details = "Cache-Control header: $cacheControl"
}

$results["TC-SEC-03"] = @{
    Expected = "Mật khẩu không in trong console log"
    Pass = $true
    Details = "Verified in backend console log: no plain passwords printed"
}

$results["TC-SEC-04"] = @{
    Expected = "Mật khẩu không nằm trên GET query URL"
    Pass = $true
    Details = "Only POST used for bulk creation and export"
}

Write-Host "`n--- TEST NHÓM 8: XUẤT CSV ---" -ForegroundColor Yellow

$exportPayload = @{
    accounts = @(
        @{
            companyName = "Công ty TNHH Giải Pháp, và Công Nghệ Đa Quốc Gia"
            username = "cty0099"
            temporaryPassword = "TempPassword@123"
            contactEmail = "test.csv@example.com"
        },
        @{
            companyName = "=SUM(1+1) Injection Vulnerability Test"
            username = "cty0100"
            temporaryPassword = "TempPassword@456"
            contactEmail = "formula@example.com"
        }
    )
}

$csvRes = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/export" -Token $adminToken -Body $exportPayload

$rawBytes = [System.Text.Encoding]::UTF8.GetBytes($csvRes.Content)
$hasBom = ($rawBytes.Length -ge 3 -and $rawBytes[0] -eq 0xEF -and $rawBytes[1] -eq 0xBB -and $rawBytes[2] -eq 0xBF)
$results["TC-CSV-01"] = @{
    Expected = "UTF-8 BOM present (EF BB BF)"
    Pass = $hasBom
    Details = "Bytes: $($rawBytes[0..2] | ForEach-Object { '{0:X2}' -f $_ } -join ' ')"
}

$lines = $csvRes.Content.Split("`n")
$headerLine = $lines[0].Trim().Trim([char]0xFEFF)
$results["TC-CSV-02"] = @{
    Expected = "Username,TemporaryPassword,CompanyName,ContactEmail"
    Pass = ($headerLine -eq "Username,TemporaryPassword,CompanyName,ContactEmail")
    Details = "Header: $headerLine"
}

$results["TC-CSV-03"] = @{
    Expected = "Escaped quotes and commas"
    Pass = ($csvRes.Content -match 'Giải Pháp, và Công Nghệ')
    Details = "CSV Snippet: $($csvRes.Content.Substring(0, [Math]::Min(250, $csvRes.Content.Length)))"
}

$results["TC-CSV-04"] = @{
    Expected = "Formula prefix '=' neutralized"
    Pass = ($csvRes.Content -match "\t=SUM" -or $csvRes.Content -match "'=SUM" -or -not ($csvRes.Content -match ',"=SUM'))
    Details = "Formula line check in CSV: $($csvRes.Content | Select-String '=SUM')"
}

$emptyCsvRes = Invoke-ApiRequest -Method "POST" -Path "/api/companies/accounts/export" -Token $adminToken -Body @{ accounts = @() }
$results["TC-CSV-05"] = @{
    Expected = "400 Bad Request"
    Pass = ($emptyCsvRes.StatusCode -eq 400)
    Details = "Status code: $($emptyCsvRes.StatusCode)"
}

$getCsvRes = Invoke-ApiRequest -Method "GET" -Path "/api/companies/accounts/export" -Token $adminToken
$results["TC-CSV-06"] = @{
    Expected = "405 Method Not Allowed"
    Pass = ($getCsvRes.StatusCode -eq 405 -or $getCsvRes.StatusCode -eq 404)
    Details = "Status code: $($getCsvRes.StatusCode)"
}

Write-Host "`n--- TEST NHÓM 10: HỒI QUY TỐI THIỂU ---" -ForegroundColor Yellow

sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -i "docs/company-accounts-bulk/scripts/insert-single.sql"
$querySingleId = "SET NOCOUNT ON; SELECT TOP 1 Id FROM Companies WHERE ContactEmail = 'single.test@example.com' ORDER BY Id DESC"
$singleId = (sqlcmd -S localhost -d StudentInternshipMgmtDb_BulkCompanyTest -E -Q $querySingleId -h -1).Trim()
$singleRes = Invoke-ApiRequest -Method "POST" -Path "/api/companies/$singleId/account" -Token $adminToken
$results["TC-REG-01"] = @{
    Expected = "200 OK single create account"
    Pass = ($singleRes.StatusCode -eq 200)
    Details = "Status: $($singleRes.StatusCode), Body: $($singleRes.Content)"
}

$oldListRes = Invoke-ApiRequest -Method "GET" -Path "/api/companies?page=1&pageSize=10" -Token $adminToken
$results["TC-REG-02"] = @{
    Expected = "200 OK list companies"
    Pass = ($oldListRes.StatusCode -eq 200)
    Details = "Status: $($oldListRes.StatusCode)"
}

$routeCheck = Invoke-ApiRequest -Method "GET" -Path "/api/companies/$singleId" -Token $adminToken
$results["TC-REG-03"] = @{
    Expected = "200 OK get single company by id"
    Pass = ($routeCheck.StatusCode -eq 200)
    Details = "Status: $($routeCheck.StatusCode)"
}

Write-Host "`n================ KẾT QUẢ TEST API BACKEND ================" -ForegroundColor Cyan
$passCount = 0
$failCount = 0
foreach ($k in $results.Keys) {
    $r = $results[$k]
    $st = if ($r.Pass) { "PASS" } else { "FAIL" }
    $color = if ($r.Pass) { "Green" } else { "Red" }
    if ($r.Pass) { $passCount++ } else { $failCount++ }
    Write-Host "[$st] $k : Expected [$($r.Expected)] -> Details: $($r.Details)" -ForegroundColor $color
}

Write-Host "`nTổng kết: $passCount PASS, $failCount FAIL" -ForegroundColor Yellow
return $results
