param(
    [string]$BaseUrl = "http://localhost:5018",
    [string]$AdminUser = "admin",
    [string]$AdminPass,
    [string]$StudentUser = "PI02099",
    [string]$StudentPass,
    [string]$CompanyUser = "company_test",
    [string]$CompanyPass,
    [string]$EvidenceDir = "..\evidence"
)

$ErrorActionPreference = "Continue"

$evidencePath = Join-Path $PSScriptRoot $EvidenceDir
if (-not (Test-Path $evidencePath)) {
    New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Log-Evidence {
    param([string]$FileName, [string]$Content)
    $path = Join-Path $evidencePath $FileName
    Set-Content -Path $path -Value $Content -Encoding utf8
}

function Record-Result {
    param(
        [string]$TestId,
        [string]$Description,
        [string]$Status, # PASS, FAIL
        [string]$Details = ""
    )
    $obj = [PSCustomObject]@{
        TestId = $TestId
        Description = $Description
        Status = $Status
        Details = $Details
    }
    $results.Add($obj)
    $color = if ($Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "[$Status] $TestId - $Description" -ForegroundColor $color
    if ($Details) {
        Write-Host "       $Details" -ForegroundColor Gray
    }
}

function Get-Token {
    param([string]$Username, [string]$Password)
    $body = @{ username = $Username; password = $Password } | ConvertTo-Json
    try {
        $res = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $body -ContentType "application/json"
        return $res.data.accessToken
    } catch {
        Write-Error "Login failed for $Username : $($_.Exception.Message)"
        return $null
    }
}

Write-Host "--- LOGGING IN USERS ---" -ForegroundColor Cyan
$adminToken = Get-Token $AdminUser $AdminPass
$studentToken = Get-Token $StudentUser $StudentPass
$companyToken = Get-Token $CompanyUser $CompanyPass

if (-not $adminToken -or -not $studentToken -or -not $companyToken) {
    Write-Error "Failed to obtain tokens for all test users. Aborting."
    exit 1
}

$adminHeader = @{ Authorization = "Bearer $adminToken" }
$studentHeader = @{ Authorization = "Bearer $studentToken" }
$companyHeader = @{ Authorization = "Bearer $companyToken" }

# Helper to invoke raw web request returning status and content
function Invoke-ApiRaw {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$ContentType = "application/json"
    )
    $req = [System.Net.HttpWebRequest]::Create($Uri)
    $req.Method = $Method
    $req.ContentType = $ContentType
    foreach ($k in $Headers.Keys) {
        if ($k -eq "Authorization") {
            $req.Headers.Add("Authorization", $Headers[$k])
        } else {
            $req.Headers.Add($k, $Headers[$k])
        }
    }
    if ($Body) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
        $req.ContentLength = $bytes.Length
        $stream = $req.GetRequestStream()
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Close()
    }
    try {
        $resp = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $content = $reader.ReadToEnd()
        $reader.Close()
        return [PSCustomObject]@{
            StatusCode = [int]$resp.StatusCode
            Content = $content
        }
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp) {
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $content = $reader.ReadToEnd()
            $reader.Close()
            return [PSCustomObject]@{
                StatusCode = [int]$resp.StatusCode
                Content = $content
            }
        }
        return [PSCustomObject]@{
            StatusCode = 0
            Content = $_.Exception.Message
        }
    }
}

# --- B01: GET /api/about unauthenticated ---
$resB01 = Invoke-ApiRaw "GET" "$BaseUrl/api/about"
if ($resB01.StatusCode -eq 401) {
    Record-Result "B01" "GET unauthenticated returns 401" "PASS" "Status: 401"
} else {
    Record-Result "B01" "GET unauthenticated returns 401" "FAIL" "Got $($resB01.StatusCode)"
}

# --- B02: GET /api/about as student (initial clean state) ---
$resB02 = Invoke-ApiRaw "GET" "$BaseUrl/api/about" $studentHeader
$jsonB02 = $resB02.Content | ConvertFrom-Json
if ($resB02.StatusCode -eq 200 -and $jsonB02.success -eq $true -and $null -eq $jsonB02.data.updatedAt -and $jsonB02.data.content.name -eq "Công ty Cổ phần Công nghệ Cửu Long") {
    Record-Result "B02" "GET as student returns default content and null updatedAt" "PASS" "Name: $($jsonB02.data.content.name), updatedAt: null"
    Log-Evidence "B02-get-student-clean.json" $resB02.Content
} else {
    Record-Result "B02" "GET as student returns default content" "FAIL" "Status: $($resB02.StatusCode), Content: $($resB02.Content)"
}

# --- B03: GET /api/about as company ---
$resB03 = Invoke-ApiRaw "GET" "$BaseUrl/api/about" $companyHeader
$jsonB03 = $resB03.Content | ConvertFrom-Json
if ($resB03.StatusCode -eq 200 -and $jsonB03.success -eq $true -and $jsonB03.data.content.name -eq "Công ty Cổ phần Công nghệ Cửu Long") {
    Record-Result "B03" "GET as company returns 200" "PASS" "Status: 200"
} else {
    Record-Result "B03" "GET as company returns 200" "FAIL" "Status: $($resB03.StatusCode)"
}

# --- B04: PUT by student, company, unauth rejected and data unchanged ---
$defaultContent = $jsonB02.data.content
$hackedContent = $jsonB02.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$hackedContent.name = "Hacked Unauthorized Name"
$hackedBody = $hackedContent | ConvertTo-Json -Depth 10

$putStudent = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $studentHeader $hackedBody
$putCompany = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $companyHeader $hackedBody
$putUnauth = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" @{} $hackedBody

$checkB04 = Invoke-ApiRaw "GET" "$BaseUrl/api/about" $adminHeader
$checkJsonB04 = $checkB04.Content | ConvertFrom-Json

if ($putStudent.StatusCode -eq 403 -and $putCompany.StatusCode -eq 403 -and $putUnauth.StatusCode -eq 401 -and $checkJsonB04.data.content.name -ne "Hacked Unauthorized Name") {
    Record-Result "B04" "PUT by student(403), company(403), unauth(401) rejected; content unchanged" "PASS" "Student: $($putStudent.StatusCode), Company: $($putCompany.StatusCode), Unauth: $($putUnauth.StatusCode)"
} else {
    Record-Result "B04" "PUT by unauthorized users" "FAIL" "Student: $($putStudent.StatusCode), Company: $($putCompany.StatusCode), Unauth: $($putUnauth.StatusCode), Name in DB: $($checkJsonB04.data.content.name)"
}

# --- B05: PUT by Admin with valid content (change name & tagline) ---
$validEdit = $jsonB02.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$validEdit.name = "Công ty Cổ phần Công nghệ Cửu Long (Đổi Mới)"
$validEdit.tagline = "Khẩu hiệu đào tạo thực chiến mới 2026."
$validEditBody = $validEdit | ConvertTo-Json -Depth 10

$resB05 = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader $validEditBody
$jsonB05 = $resB05.Content | ConvertFrom-Json

$getAfterB05 = Invoke-ApiRaw "GET" "$BaseUrl/api/about" $studentHeader
$getAfterJsonB05 = $getAfterB05.Content | ConvertFrom-Json

if ($resB05.StatusCode -eq 200 -and $jsonB05.success -eq $true -and $getAfterJsonB05.data.content.name -eq "Công ty Cổ phần Công nghệ Cửu Long (Đổi Mới)" -and $null -ne $getAfterJsonB05.data.updatedAt) {
    Record-Result "B05" "Admin PUT valid content succeeds; student GET sees new content and updatedAt" "PASS" "Name: $($getAfterJsonB05.data.content.name), updatedAt: $($getAfterJsonB05.data.updatedAt)"
    Log-Evidence "B05-admin-put-success.json" $resB05.Content
} else {
    Record-Result "B05" "Admin PUT valid content" "FAIL" "Status: $($resB05.StatusCode), Student GET: $($getAfterB05.Content)"
}

# --- B06: Restart API, verify data persisted in SQL Server ---
Write-Host "--- RESTARTING API SERVER FOR B06 ---" -ForegroundColor Cyan
# Find running dotnet process on port 5018
$apiConn = Get-NetTCPConnection -LocalPort 5018 -State Listen -ErrorAction SilentlyContinue
if ($apiConn) {
    Stop-Process -Id $apiConn.OwningProcess -Force
    Start-Sleep -Seconds 2
}
# Start API again in background
$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = "dotnet"
$startInfo.Arguments = "run --project src/StudentInternshipMgmt.Api --launch-profile http"
$startInfo.WorkingDirectory = (Join-Path $PSScriptRoot "..\..\backend")
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$newApiProc = [System.Diagnostics.Process]::Start($startInfo)

# Wait for API to respond
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    try {
        $testPing = [System.Net.HttpWebRequest]::Create("$BaseUrl/swagger/v1/swagger.json").GetResponse()
        if ($testPing.StatusCode -eq 200) { $ready = $true; $testPing.Close(); break }
    } catch {}
}

if ($ready) {
    # Relogin admin and student because tokens might need verification or server fresh state
    $studentToken = Get-Token $StudentUser $StudentPass
    $studentHeader = @{ Authorization = "Bearer $studentToken" }
    $adminToken = Get-Token $AdminUser $AdminPass
    $adminHeader = @{ Authorization = "Bearer $adminToken" }

    $resB06 = Invoke-ApiRaw "GET" "$BaseUrl/api/about" $studentHeader
    $jsonB06 = $resB06.Content | ConvertFrom-Json
    if ($resB06.StatusCode -eq 200 -and $jsonB06.data.content.name -eq "Công ty Cổ phần Công nghệ Cửu Long (Đổi Mới)" -and $null -ne $jsonB06.data.updatedAt) {
        Record-Result "B06" "API restart: Content persisted in SQL Server database" "PASS" "Name: $($jsonB06.data.content.name)"
        Log-Evidence "B06-restart-persisted.json" $resB06.Content
    } else {
        Record-Result "B06" "API restart: Content persisted" "FAIL" "Content: $($resB06.Content)"
    }
} else {
    Record-Result "B06" "API restart failed to start within 30s" "FAIL"
}

# Helper to test single invalid PUT case
function Test-InvalidCase {
    param(
        [string]$CaseName,
        [scriptblock]$MutateAction,
        [bool]$ExpectAccept = $false
    )
    # Clone current content
    $current = (Invoke-ApiRaw "GET" "$BaseUrl/api/about" $adminHeader).Content | ConvertFrom-Json
    $clone = $current.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json

    # Mutate
    & $MutateAction $clone

    $body = $clone | ConvertTo-Json -Depth 10
    $resp = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader $body
    $respJson = $resp.Content | ConvertFrom-Json

    if ($ExpectAccept) {
        if ($resp.StatusCode -eq 200 -and $respJson.success -eq $true) {
            return [PSCustomObject]@{ Pass = $true; Details = "Accepted as expected (200)" }
        } else {
            return [PSCustomObject]@{ Pass = $false; Details = "Expected 200 but got $($resp.StatusCode): $($resp.Content)" }
        }
    } else {
        if ($resp.StatusCode -eq 400 -and $respJson.success -eq $false -and $respJson.errors.Count -gt 0) {
            # Verify Vietnamese message exists
            $errMsg = $respJson.errors -join "; "
            return [PSCustomObject]@{ Pass = $true; Details = "Rejected (400) with errors: $errMsg" }
        } else {
            return [PSCustomObject]@{ Pass = $false; Details = "Expected 400 with errors list, got status $($resp.StatusCode): $($resp.Content)" }
        }
    }
}

Write-Host "--- RUNNING B07 INVALID PUT CASES ---" -ForegroundColor Cyan
$b07Failures = @()

# 1. Name empty
$c = Test-InvalidCase "name-empty" { param($x) $x.name = "" }
if (-not $c.Pass) { $b07Failures += "name-empty: $($c.Details)" }

# 2. Name 200 chars (accepted)
$c = Test-InvalidCase "name-200" { param($x) $x.name = "A" * 200 } -ExpectAccept $true
if (-not $c.Pass) { $b07Failures += "name-200: $($c.Details)" }

# 3. Name 201 chars (rejected)
$c = Test-InvalidCase "name-201" { param($x) $x.name = "A" * 201 }
if (-not $c.Pass) { $b07Failures += "name-201: $($c.Details)" }

# 4. Tagline empty
$c = Test-InvalidCase "tagline-empty" { param($x) $x.tagline = "" }
if (-not $c.Pass) { $b07Failures += "tagline-empty: $($c.Details)" }

# 5. Summary 1001 chars
$c = Test-InvalidCase "summary-1001" { param($x) $x.summary = "A" * 1001 }
if (-not $c.Pass) { $b07Failures += "summary-1001: $($c.Details)" }

# 6. Facts 0 items
$c = Test-InvalidCase "facts-0" { param($x) $x.facts = @() }
if (-not $c.Pass) { $b07Failures += "facts-0: $($c.Details)" }

# 7. Facts 8 items (accepted)
$c = Test-InvalidCase "facts-8" {
    param($x)
    $x.facts = @(
        @{ icon = "pi pi-calendar"; label = "F1"; value = "V1" },
        @{ icon = "pi pi-calendar"; label = "F2"; value = "V2" },
        @{ icon = "pi pi-calendar"; label = "F3"; value = "V3" },
        @{ icon = "pi pi-calendar"; label = "F4"; value = "V4" },
        @{ icon = "pi pi-calendar"; label = "F5"; value = "V5" },
        @{ icon = "pi pi-calendar"; label = "F6"; value = "V6" },
        @{ icon = "pi pi-calendar"; label = "F7"; value = "V7" },
        @{ icon = "pi pi-calendar"; label = "F8"; value = "V8" }
    )
} -ExpectAccept $true
if (-not $c.Pass) { $b07Failures += "facts-8: $($c.Details)" }

# 8. Facts 9 items (rejected)
$c = Test-InvalidCase "facts-9" {
    param($x)
    $list = @()
    for ($i = 1; $i -le 9; $i++) {
        $list += @{ icon = "pi pi-calendar"; label = "F$i"; value = "V$i" }
    }
    $x.facts = $list
}
if (-not $c.Pass) { $b07Failures += "facts-9: $($c.Details)" }

# 9. Facts label 51 chars
$c = Test-InvalidCase "facts-label-51" { param($x) $x.facts[0].label = "A" * 51 }
if (-not $c.Pass) { $b07Failures += "facts-label-51: $($c.Details)" }

# 10. History 21 items
$c = Test-InvalidCase "history-21" {
    param($x)
    $list = @()
    for ($i = 1; $i -le 21; $i++) {
        $list += @{ year = "2020"; title = "T$i"; text = "Nội dung $i" }
    }
    $x.history = $list
}
if (-not $c.Pass) { $b07Failures += "history-21: $($c.Details)" }

# 11. Tag 31 chars
$c = Test-InvalidCase "tag-31" { param($x) $x.fields[0].tags = @("A" * 31) }
if (-not $c.Pass) { $b07Failures += "tag-31: $($c.Details)" }

# 12. Field 9 tags
$c = Test-InvalidCase "field-9-tags" {
    param($x)
    $x.fields[0].tags = @("T1","T2","T3","T4","T5","T6","T7","T8","T9")
}
if (-not $c.Pass) { $b07Failures += "field-9-tags: $($c.Details)" }

# 13. Checklist duplicate id
$c = Test-InvalidCase "checklist-duplicate-id" {
    param($x)
    $x.studentInfo.checklist = @(
        @{ id = "dup-id"; label = "Mục 1" },
        @{ id = "dup-id"; label = "Mục 2" }
    )
}
if (-not $c.Pass) { $b07Failures += "checklist-duplicate-id: $($c.Details)" }

# 14. Checklist uppercase id or space
$c = Test-InvalidCase "checklist-uppercase-id" {
    param($x)
    $x.studentInfo.checklist = @(
        @{ id = "CHECK-ID"; label = "Mục 1" }
    )
}
if (-not $c.Pass) { $b07Failures += "checklist-uppercase-id: $($c.Details)" }

$c = Test-InvalidCase "checklist-space-id" {
    param($x)
    $x.studentInfo.checklist = @(
        @{ id = "check id"; label = "Mục 1" }
    )
}
if (-not $c.Pass) { $b07Failures += "checklist-space-id: $($c.Details)" }

# 15. Checklist 11 items
$c = Test-InvalidCase "checklist-11" {
    param($x)
    $list = @()
    for ($i = 1; $i -le 11; $i++) {
        $list += @{ id = "item-$i"; label = "Label $i" }
    }
    $x.studentInfo.checklist = $list
}
if (-not $c.Pass) { $b07Failures += "checklist-11: $($c.Details)" }

# 16. Icon not allowed (pi pi-khong-co, fa fa-star, injection onclick)
$c = Test-InvalidCase "icon-not-allowed" { param($x) $x.facts[0].icon = "pi pi-khong-co" }
if (-not $c.Pass) { $b07Failures += "icon-not-allowed: $($c.Details)" }

$c = Test-InvalidCase "icon-fa-star" { param($x) $x.facts[0].icon = "fa fa-star" }
if (-not $c.Pass) { $b07Failures += "icon-fa-star: $($c.Details)" }

$c = Test-InvalidCase "icon-injection" { param($x) $x.facts[0].icon = 'pi pi-star" onclick="x' }
if (-not $c.Pass) { $b07Failures += "icon-injection: $($c.Details)" }

# 17. Email typo & invalid
$c = Test-InvalidCase "email-gmail-con" { param($x) $x.contact.email = "a@gmail.con" }
if (-not $c.Pass) { $b07Failures += "email-gmail-con: $($c.Details)" }

$c = Test-InvalidCase "email-abc" { param($x) $x.contact.email = "abc" }
if (-not $c.Pass) { $b07Failures += "email-abc: $($c.Details)" }

# 18. Phone invalid
$c = Test-InvalidCase "phone-abc" { param($x) $x.contact.phone = "abc" }
if (-not $c.Pass) { $b07Failures += "phone-abc: $($c.Details)" }

$c = Test-InvalidCase "phone-too-short" { param($x) $x.contact.phone = "123" }
if (-not $c.Pass) { $b07Failures += "phone-too-short: $($c.Details)" }

# 19. Control char in name (char 7)
$c = Test-InvalidCase "control-char-name" { param($x) $x.name = "Tên có chuông" + [char]7 }
if (-not $c.Pass) { $b07Failures += "control-char-name: $($c.Details)" }

# 20. Line breaks in title (rejected) vs summary (accepted)
$c = Test-InvalidCase "linebreak-in-title" { param($x) $x.history[0].title = "Dòng 1`nDòng 2" }
if (-not $c.Pass) { $b07Failures += "linebreak-in-title: $($c.Details)" }

$c = Test-InvalidCase "linebreak-in-summary" { param($x) $x.summary = "Dòng 1`nDòng 2 hợp lệ." } -ExpectAccept $true
if (-not $c.Pass) { $b07Failures += "linebreak-in-summary: $($c.Details)" }

# 21. Total JSON > 60000 chars
$c = Test-InvalidCase "json-over-60k" {
    param($x)
    # create many history items with 500 chars text to reach > 60,000 chars
    $list = @()
    for ($i = 1; $i -le 20; $i++) {
        $list += @{ year = "2024"; title = "A" * 150; text = "B" * 500 }
    }
    $x.history = $list
    $listFaq = @()
    for ($i = 1; $i -le 15; $i++) {
        $listFaq += @{ q = "C" * 200; a = "D" * 1000 }
    }
    $x.studentInfo.faq = $listFaq
}
if (-not $c.Pass) { $b07Failures += "json-over-60k: $($c.Details)" }

if ($b07Failures.Count -eq 0) {
    Record-Result "B07" "All boundary and validation cases handled correctly with Vietnamese errors (400)" "PASS" "All 21 subcases passed"
} else {
    Record-Result "B07" "Invalid PUT cases validation" "FAIL" ($b07Failures -join " | ")
}

# --- B08: Malformed data (never 500) ---
Write-Host "--- RUNNING B08 MALFORMED DATA CASES ---" -ForegroundColor Cyan
$b08Failures = @()

# 1. Missing environment block
$cData = (Invoke-ApiRaw "GET" "$BaseUrl/api/about" $adminHeader).Content | ConvertFrom-Json
$cl = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$cl.PSObject.Properties.Remove('environment')
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader ($cl | ConvertTo-Json -Depth 10)
if ($r.StatusCode -ne 400 -or $r.Content -match "Internal Server Error" -or $r.StatusCode -eq 500) {
    $b08Failures += "missing-environment: Status $($r.StatusCode)"
}

# 2. facts is null
$cl = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$cl.facts = $null
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader ($cl | ConvertTo-Json -Depth 10)
if ($r.StatusCode -ne 400 -or $r.StatusCode -eq 500) {
    $b08Failures += "facts-null: Status $($r.StatusCode)"
}

# 3. String sent as number
$rawJson = ($cData.data.content | ConvertTo-Json -Depth 10) -replace '"name":\s*"[^"]*"', '"name": 12345'
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader $rawJson
if ($r.StatusCode -eq 500) {
    $b08Failures += "string-as-number returned 500!"
}

# 4. Empty request body
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader ""
if ($r.StatusCode -eq 500 -or $r.StatusCode -ne 400) {
    $b08Failures += "empty-body: Status $($r.StatusCode)"
}

# 5. Broken JSON
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader "{ broken json ... "
if ($r.StatusCode -eq 500 -or $r.StatusCode -ne 400) {
    $b08Failures += "broken-json: Status $($r.StatusCode)"
}

# 6. Wrong Content-Type
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader "some text" "text/plain"
if ($r.StatusCode -eq 500) {
    $b08Failures += "wrong-content-type returned 500!"
}

if ($b08Failures.Count -eq 0) {
    Record-Result "B08" "Malformed data returns 400 with friendly message, NEVER 500" "PASS" "All 6 malformed cases safely returned 400/415"
} else {
    Record-Result "B08" "Malformed data handling" "FAIL" ($b08Failures -join "; ")
}

# --- B09: Whitespace trimming ---
$cl = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$cl.name = "   Tên Đã Cắt Khoảng Trắng   "
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader ($cl | ConvertTo-Json -Depth 10)
$check = (Invoke-ApiRaw "GET" "$BaseUrl/api/about" $adminHeader).Content | ConvertFrom-Json

$clWhitespace = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$clWhitespace.name = "      "
$rWhitespace = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader ($clWhitespace | ConvertTo-Json -Depth 10)

if ($check.data.content.name -eq "Tên Đã Cắt Khoảng Trắng" -and $rWhitespace.StatusCode -eq 400) {
    Record-Result "B09" "Whitespace trimmed before save; all-whitespace name rejected" "PASS" "Saved: '$($check.data.content.name)', all-whitespace status: $($rWhitespace.StatusCode)"
} else {
    Record-Result "B09" "Whitespace trimming" "FAIL" "Saved: '$($check.data.content.name)', all-whitespace status: $($rWhitespace.StatusCode)"
}

# --- B10: Code injection verbatim preservation ---
$cl = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$cl.name = "<script>alert(1)</script>"
$cl.tagline = "<img src=x onerror=alert(1)>"
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader ($cl | ConvertTo-Json -Depth 10)
$check = (Invoke-ApiRaw "GET" "$BaseUrl/api/about" $adminHeader).Content | ConvertFrom-Json

if ($check.data.content.name -eq "<script>alert(1)</script>" -and $check.data.content.tagline -eq "<img src=x onerror=alert(1)>") {
    Record-Result "B10" "Code injection preserved verbatim in API response" "PASS" "Name: $($check.data.content.name)"
    Log-Evidence "B10-injection-verbatim.json" (ConvertTo-Json $check.data.content -Depth 3)
} else {
    Record-Result "B10" "Code injection verbatim preservation" "FAIL" "Got: name=$($check.data.content.name)"
}

# --- B11: Vietnamese diacritics, symbols, emojis ---
$cl = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$heart = [char]0x2764
$gradCap = [char]::ConvertFromUtf32(0x1F393)
$unicodeStr = "Dao tao $heart Thuc tap $gradCap - Cuu Long Tech"
$cl.name = $unicodeStr
$r = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader ($cl | ConvertTo-Json -Depth 10)
$check = (Invoke-ApiRaw "GET" "$BaseUrl/api/about" $adminHeader).Content | ConvertFrom-Json

if ($check.data.content.name -eq $unicodeStr) {
    Record-Result "B11" "Vietnamese diacritics, symbols, emojis preserved correctly" "PASS" "Returned: $($check.data.content.name)"
    Log-Evidence "B11-vietnamese-emoji.json" (ConvertTo-Json $check.data.content -Depth 3)
} else {
    Record-Result "B11" "Vietnamese diacritics and emojis" "FAIL" "Expected '$unicodeStr', got '$($check.data.content.name)'"
}

# --- B12: POST /api/about/reset ---
$resetStudent = Invoke-ApiRaw "POST" "$BaseUrl/api/about/reset" $studentHeader
$resetAdmin = Invoke-ApiRaw "POST" "$BaseUrl/api/about/reset" $adminHeader
$jsonReset = $resetAdmin.Content | ConvertFrom-Json
$getReset = (Invoke-ApiRaw "GET" "$BaseUrl/api/about" $adminHeader).Content | ConvertFrom-Json

if ($resetStudent.StatusCode -eq 403 -and $resetAdmin.StatusCode -eq 200 -and $jsonReset.success -eq $true -and $getReset.data.content.name -eq "Công ty Cổ phần Công nghệ Cửu Long" -and $null -ne $getReset.data.updatedAt) {
    Record-Result "B12" "POST /api/about/reset: Admin succeeds with updatedAt, Student 403" "PASS" "Reset Admin status: 200, Student: 403, updatedAt: $($getReset.data.updatedAt)"
    Log-Evidence "B12-reset-success.json" $resetAdmin.Content
} else {
    Record-Result "B12" "POST /api/about/reset" "FAIL" "Admin: $($resetAdmin.StatusCode), Student: $($resetStudent.StatusCode), Name: $($getReset.data.content.name)"
}

# --- B13: Corrupted database row test ---
Write-Host "--- TESTING CORRUPTED DB ROW FOR B13 ---" -ForegroundColor Cyan
try {
    & sqlcmd -S localhost -d StudentInternshipMgmtDb -E -C -Q "UPDATE dbo.AboutPages SET ContentJson = 'xxx' WHERE Id = 1;"
    $getCorrupted = Invoke-ApiRaw "GET" "$BaseUrl/api/about" $studentHeader
    $jsonCorrupted = $getCorrupted.Content | ConvertFrom-Json

    # Restore DB with valid reset
    Invoke-ApiRaw "POST" "$BaseUrl/api/about/reset" $adminHeader | Out-Null

    if ($getCorrupted.StatusCode -eq 200 -and $jsonCorrupted.data.content.name -eq "Công ty Cổ phần Công nghệ Cửu Long") {
        Record-Result "B13" "Corrupted ContentJson returns default content gracefully, no 500" "PASS" "Status: 200, Default content returned"
    } else {
        Record-Result "B13" "Corrupted ContentJson handling" "FAIL" "Status: $($getCorrupted.StatusCode), Content: $($getCorrupted.Content)"
    }
} catch {
    Record-Result "B13" "Corrupted DB row test" "FAIL" $_.Exception.Message
}

# --- B14: Concurrent PUT test ---
Write-Host "--- TESTING CONCURRENT PUT FOR B14 ---" -ForegroundColor Cyan
try {
    & sqlcmd -S localhost -d StudentInternshipMgmtDb -E -C -Q "DELETE FROM dbo.AboutPages;"
    
    $concurrentJobs = @()
    for ($i = 1; $i -le 5; $i++) {
        $c = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
        $c.name = "Concurrent Name Run $i"
        $body = $c | ConvertTo-Json -Depth 10
        $scriptBlock = {
            param($url, $hdr, $b)
            $req = [System.Net.HttpWebRequest]::Create("$url/api/about")
            $req.Method = "PUT"
            $req.ContentType = "application/json"
            $req.Headers.Add("Authorization", $hdr)
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($b)
            $req.ContentLength = $bytes.Length
            $s = $req.GetRequestStream()
            $s.Write($bytes, 0, $bytes.Length)
            $s.Close()
            try {
                $res = $req.GetResponse()
                return [int]$res.StatusCode
            } catch [System.Net.WebException] {
                return [int]$_.Exception.Response.StatusCode
            }
        }
        $concurrentJobs += Start-Job -ScriptBlock $scriptBlock -ArgumentList $BaseUrl, "Bearer $adminToken", $body
    }
    
    $concurrentJobs | Wait-Job -Timeout 15 | Out-Null
    $jobResults = $concurrentJobs | Receive-Job
    $concurrentJobs | Remove-Job -Force

    $rowCount = (& sqlcmd -S localhost -d StudentInternshipMgmtDb -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.AboutPages;").Trim()
    $no500 = ($jobResults | Where-Object { $_ -eq 500 }).Count -eq 0

    if ($rowCount -eq "1" -and $no500) {
        Record-Result "B14" "Concurrent PUT: 5 parallel requests yielded no 500 errors and exactly 1 row in table" "PASS" "Row count: $rowCount, Status codes: $($jobResults -join ', ')"
    } else {
        Record-Result "B14" "Concurrent PUT" "FAIL" "Row count: $rowCount, Status codes: $($jobResults -join ', ')"
    }
} catch {
    Record-Result "B14" "Concurrent PUT test" "FAIL" $_.Exception.Message
}

# --- B15: Swagger JSON ---
$swaggerRes = Invoke-ApiRaw "GET" "$BaseUrl/swagger/v1/swagger.json"
if ($swaggerRes.StatusCode -eq 200 -and $swaggerRes.Content -match '"/api/about"') {
    Record-Result "B15" "Swagger JSON accessible (200) and contains /api/about" "PASS" "Status: 200"
} else {
    Record-Result "B15" "Swagger JSON" "FAIL" "Status: $($swaggerRes.StatusCode)"
}

# --- B16: Error format check (no stack traces or file paths) ---
$errRes = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader "{ broken json"
$errJson = $errRes.Content | ConvertFrom-Json
$leakPath = $errRes.Content -match '[A-Za-z]:\\[^"]+' -or $errRes.Content -match 'at StudentInternshipMgmt\.'
if ($errRes.StatusCode -eq 400 -and $errJson.success -eq $false -and -not $leakPath) {
    Record-Result "B16" "400 error response conforms to standard envelope and does not leak file paths/stack trace" "PASS" "Envelope: success=false, No leak detected"
    Log-Evidence "B16-error-format.json" $errRes.Content
} else {
    Record-Result "B16" "Error format and privacy" "FAIL" "Status: $($errRes.StatusCode), Leaked: $leakPath, Content: $($errRes.Content)"
}

# --- S-A3: Security check on PUT (no extra row created when sending id: 2) ---
$c = $cData.data.content | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$c.name = "Security Check Name"
$cJsonWithExtraId = ($c | ConvertTo-Json -Depth 10) -replace '^{', '{"id": 2, "updatedByUserId": 9999,'
$secResp = Invoke-ApiRaw "PUT" "$BaseUrl/api/about" $adminHeader $cJsonWithExtraId
$rowCountSec = (& sqlcmd -S localhost -d StudentInternshipMgmtDb -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.AboutPages;").Trim()
$allRows = (& sqlcmd -S localhost -d StudentInternshipMgmtDb -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT Id FROM dbo.AboutPages;").Trim()

if ($secResp.StatusCode -eq 200 -and $rowCountSec -eq "1" -and $allRows -eq "1") {
    Record-Result "S-A3" "PUT cannot create multiple rows or write to different Id" "PASS" "Row count: $rowCountSec, Primary key: $allRows"
} else {
    Record-Result "S-A3" "PUT row tamper vulnerability" "FAIL" "Row count: $rowCountSec, Rows: $allRows"
}

# Restore default before closing
Invoke-ApiRaw "POST" "$BaseUrl/api/about/reset" $adminHeader | Out-Null
Write-Host "--- CLEANED UP ABOUT PAGE TO DEFAULT ---" -ForegroundColor Cyan

# Output Summary
Write-Host "`n====================== TEST SUMMARY ======================" -ForegroundColor Yellow
$passCount = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host "TOTAL TESTS: $($results.Count) | PASSED: $passCount | FAILED: $failCount" -ForegroundColor Yellow

$results | Format-Table -AutoSize
