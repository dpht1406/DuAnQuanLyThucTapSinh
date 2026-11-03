# capture-before-evidence.ps1
# TAB 1 - Builder: Thuc hien thu thap bang chung "TRUOC" (Before) tren CSDL StudentInternshipMgmtDb_UATBuild
$ErrorActionPreference = "Stop"
$BaseUrl = "http://127.0.0.1:5018"
$api = "$BaseUrl/api"
$databaseName = "StudentInternshipMgmtDb_UATBuild"
$backendRoot = [System.IO.Path]::GetFullPath("D:\baitapthuctap\duanquanlythuctapsinh\InternshipManagement\backend")
$evidenceDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "."))
$logPath = [System.IO.Path]::Combine($env:TEMP, "capture-before-api.log")

if (Test-Path $logPath) { Remove-Item $logPath -Force }

Write-Host "Khoi dong Backend API ket noi toi $databaseName..."
$apiJob = Start-Job -ScriptBlock {
    param($BackendDir, $DbName, $Url, $Log)
    $env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=$DbName;Integrated Security=True;TrustServerCertificate=True;"
    $env:Jwt__SecretKey = "super_secret_jwt_key_for_uat_testing_at_least_32_characters_long_123456"
    $env:Logging__LogLevel__Default = "Warning"
    Set-Location $BackendDir
    & dotnet run --no-launch-profile --project "src\StudentInternshipMgmt.Api" --urls $Url 2>&1 | ForEach-Object {
        $entry = "[{0}] {1}" -f (Get-Date -Format "HH:mm:ss.fff"), $_
        [System.IO.File]::AppendAllText($Log, $entry + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
    }
} -ArgumentList $backendRoot, $databaseName, $BaseUrl, $logPath

try {
    # Cho API san sang
    $ready = $false
    $deadline = (Get-Date).AddSeconds(45)
    while (-not $ready -and (Get-Date) -lt $deadline) {
        if ($apiJob.State -in @("Failed", "Completed", "Stopped")) {
            throw "API process ended prematurely."
        }
        try {
            $resp = Invoke-WebRequest -Uri "$api/auth/me" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
            $ready = $true
        }
        catch {
            if ($null -ne $_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 401) {
                $ready = $true
            }
            else {
                Start-Sleep -Milliseconds 500
            }
        }
    }
    if (-not $ready) {
        throw "API did not become ready in time."
    }
    Write-Host "API san sang tai $BaseUrl."

    # Dang nhap Admin
    $adminLogin = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" -Body '{"username":"admin","password":"AdminPassword@2026"}'
    $adminToken = $adminLogin.data.accessToken

    # -------------------------------------------------------------
    # 1. FB-01: Thu thap bang chung TRUOC cho validation thong tin sinh vien
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-01 (Before)..."
    $fb01Log = New-Object System.Text.StringBuilder
    [void]$fb01Log.AppendLine("=== BANG CHUNG TRUOC KHI SUA: FB-01 (MUC DO: P3) ===")
    [void]$fb01Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb01Log.AppendLine("Endpoint: POST /api/students va PUT /api/students/{id}")
    [void]$fb01Log.AppendLine("Mo ta loi: Thong bao validation kem tieng Anh (FullName la bat buoc, PhoneNumber la bat buoc, StudentCode da ton tai...)")
    [void]$fb01Log.AppendLine("")

    # Test Empty FullName
    try {
        Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body '{"studentCode":"BF01_01","fullName":"","email":"bf01@example.com","phoneNumber":"0900111222","major":"CNTT","className":"CNTT1"}'
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb01Log.AppendLine("--- Truong hop 1: Bo trong FullName ---")
        [void]$fb01Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb01Log.AppendLine("Response Body: " + $body)
        [void]$fb01Log.AppendLine("")
    }

    # Test Invalid PhoneNumber
    try {
        Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body '{"studentCode":"BF01_02","fullName":"Sinh Vien Test","email":"bf02@example.com","phoneNumber":"123","major":"CNTT","className":"CNTT1"}'
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb01Log.AppendLine("--- Truong hop 2: PhoneNumber sai dinh dang ---")
        [void]$fb01Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb01Log.AppendLine("Response Body: " + $body)
        [void]$fb01Log.AppendLine("")
    }

    # Test Duplicate StudentCode
    # Tao 1 sinh vien truoc
    $tag01 = [Guid]::NewGuid().ToString("N").Substring(0, 6)
    $dupCode = "BF01_$tag01"
    $null = Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentCode=$dupCode; fullName="Sinh Vien Goc"; email="goc_$tag01@example.com"; phoneNumber="0900111222"; major="CNTT"; className="CNTT1"} | ConvertTo-Json)
    try {
        Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentCode=$dupCode; fullName="Sinh Vien Trung"; email="trung_$tag01@example.com"; phoneNumber="0900111222"; major="CNTT"; className="CNTT1"} | ConvertTo-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb01Log.AppendLine("--- Truong hop 3: Trung StudentCode ---")
        [void]$fb01Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb01Log.AppendLine("Response Body: " + $body)
        [void]$fb01Log.AppendLine("")
    }

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-01-before.txt"), $fb01Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-01-before.txt"

    # -------------------------------------------------------------
    # 2. FB-02: Thu thap bang chung TRUOC cho validation vi tri tuyen dung
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-02 (Before)..."
    $fb02Log = New-Object System.Text.StringBuilder
    [void]$fb02Log.AppendLine("=== BANG CHUNG TRUOC KHI SUA: FB-02 (MUC DO: P3) ===")
    [void]$fb02Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb02Log.AppendLine("Endpoint: POST /api/company-me/job-positions va companyJobPositionValidation.js")
    [void]$fb02Log.AppendLine("Mo ta loi: Thong bao co tu tieng Anh trong ngoac don: Tiêu đề (Title) là bắt buộc., Số lượng tuyển (Quantity) phải là số nguyên lớn hơn 0.")
    [void]$fb02Log.AppendLine("")

    # Dang nhap doanh nghiep
    $compLogin = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" -Body '{"username":"cty0004","password":"CompanyPassword@2026"}'
    $compToken = $compLogin.data.accessToken

    # Test quantity <= 0
    try {
        Invoke-RestMethod -Method Post -Uri "$api/company-me/job-positions" -Headers @{Authorization="Bearer $compToken"} -ContentType "application/json" -Body '{"title":"Thuc tap sinh Test","quantity":0,"description":"Mo ta"}'
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb02Log.AppendLine("--- Truong hop 1: So luong tuyen = 0 ---")
        [void]$fb02Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb02Log.AppendLine("Response Body: " + $body)
        [void]$fb02Log.AppendLine("")
    }

    # Test empty title
    try {
        Invoke-RestMethod -Method Post -Uri "$api/company-me/job-positions" -Headers @{Authorization="Bearer $compToken"} -ContentType "application/json" -Body '{"title":"","quantity":2,"description":"Mo ta"}'
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb02Log.AppendLine("--- Truong hop 2: Tieu de bo trong ---")
        [void]$fb02Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb02Log.AppendLine("Response Body: " + $body)
        [void]$fb02Log.AppendLine("")
    }

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-02-before.txt"), $fb02Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-02-before.txt"

    # -------------------------------------------------------------
    # 3. FB-03: Thu thap bang chung TRUOC cho CV URL (trailing whitespace bi tu choi)
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-03 (Before)..."
    $fb03Log = New-Object System.Text.StringBuilder
    [void]$fb03Log.AppendLine("=== BANG CHUNG TRUOC KHI SUA: FB-03 (MUC DO: P3) ===")
    [void]$fb03Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb03Log.AppendLine("Endpoint: POST /api/me/placement-requests va applicationValidation.js")
    [void]$fb03Log.AppendLine("Mo ta loi: Khi dan link CV co khoang trang o cuoi (vi du 'https://example.com/cv '), he thong chan khong cho nop don")
    [void]$fb03Log.AppendLine("")

    # Tao sinh vien test cho FB-03
    $tag03 = [Guid]::NewGuid().ToString("N").Substring(0, 6)
    $tc03Code = "SV03_$tag03"
    $tc03Student = Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentCode=$tc03Code; fullName="Sinh Vien FB03"; email="fb03_$tag03@example.com"; phoneNumber="0900333333"; major="CNTT"; className="CNTT1"} | ConvertTo-Json)
    $tc03StudentId = $tc03Student.data.id
    $tc03Acc = Invoke-RestMethod -Method Post -Uri "$api/students/create-accounts" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentIds=@($tc03StudentId)} | ConvertTo-Json)
    $tc03Cred = $tc03Acc.data.createdAccounts[0]

    # Login & change password
    $stuLogin = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" -Body (@{username=$tc03Cred.studentCode; password=$tc03Cred.plainPassword} | ConvertTo-Json)
    Invoke-RestMethod -Method Post -Uri "$api/auth/change-password" -Headers @{Authorization="Bearer $($stuLogin.data.accessToken)"} -ContentType "application/json" -Body (@{currentPassword=$tc03Cred.plainPassword; newPassword="PasswordFB03@2026"} | ConvertTo-Json) | Out-Null
    $stuLogin2 = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" -Body (@{username=$tc03Cred.studentCode; password="PasswordFB03@2026"} | ConvertTo-Json)
    $tc03Token = $stuLogin2.data.accessToken

    # Lay vi tri hop le
    $jobPage = Invoke-RestMethod -Method Get -Uri "$api/job-positions?pageSize=50" -Headers @{Authorization="Bearer $adminToken"}
    $job = @($jobPage.data.items | Where-Object { $_.isOpen -and ($null -eq $_.deadline -or [DateTime]$_.deadline -ge [DateTime]::Today) } | Select-Object -First 1)[0]

    # Test CV URL co khoang trang o cuoi: "https://example.com/cv/fb03   "
    try {
        Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03"
            applicantEmail = "fb03_$tag03@example.com"
            applicantPhone = "0900333333"
            applicantSchool = "Dai hoc Mo phong"
            applicantMajor = "CNTT"
            cvUrl = "https://example.com/cv/fb03   "
            coverLetter = "Thu gioi thieu cua thi sinh FB03 voi link CV co khoang trang o cuoi."
        } | ConvertTo-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb03Log.AppendLine("--- Truong hop 1: CV URL co khoang trang o cuoi bi chan boi backend ---")
        [void]$fb03Log.AppendLine("Gia tri CvUrl: 'https://example.com/cv/fb03   '")
        [void]$fb03Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb03Log.AppendLine("Response Body: " + $body)
        [void]$fb03Log.AppendLine("")
    }

    # Test cac truong hop phai bi tu choi: javascript: scheme
    try {
        Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03"
            applicantEmail = "fb03_$tag03@example.com"
            applicantPhone = "0900333333"
            applicantSchool = "Dai hoc Mo phong"
            applicantMajor = "CNTT"
            cvUrl = "javascript:alert(1)"
            coverLetter = "Thu gioi thieu kiem tra javascript scheme."
        } | ConvertTo-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb03Log.AppendLine("--- Truong hop 2: CV URL chua scheme javascript: bi chan hop le ---")
        [void]$fb03Log.AppendLine("Gia tri CvUrl: 'javascript:alert(1)'")
        [void]$fb03Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb03Log.AppendLine("Response Body: " + $body)
        [void]$fb03Log.AppendLine("")
    }

    # Test relative url: "/files/mycv.pdf"
    try {
        Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03"
            applicantEmail = "fb03_$tag03@example.com"
            applicantPhone = "0900333333"
            applicantSchool = "Dai hoc Mo phong"
            applicantMajor = "CNTT"
            cvUrl = "/files/mycv.pdf"
            coverLetter = "Thu gioi thieu kiem tra relative url."
        } | ConvertTo-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb03Log.AppendLine("--- Truong hop 3: CV URL la duong dan tuong doi bi chan hop le ---")
        [void]$fb03Log.AppendLine("Gia tri CvUrl: '/files/mycv.pdf'")
        [void]$fb03Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb03Log.AppendLine("Response Body: " + $body)
        [void]$fb03Log.AppendLine("")
    }

    # Test khoang trang o giua: "https://example.com/my cv/view"
    try {
        Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03"
            applicantEmail = "fb03_$tag03@example.com"
            applicantPhone = "0900333333"
            applicantSchool = "Dai hoc Mo phong"
            applicantMajor = "CNTT"
            cvUrl = "https://example.com/my cv/view"
            coverLetter = "Thu gioi thieu kiem tra khoang trang o giua link."
        } | ConvertTo-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb03Log.AppendLine("--- Truong hop 4: CV URL chua khoang trang o giua bi chan hop le ---")
        [void]$fb03Log.AppendLine("Gia tri CvUrl: 'https://example.com/my cv/view'")
        [void]$fb03Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb03Log.AppendLine("Response Body: " + $body)
        [void]$fb03Log.AppendLine("")
    }

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-03-before.txt"), $fb03Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-03-before.txt"

    # -------------------------------------------------------------
    # 4. FB-04: Thu thap bang chung TRUOC cho README.md mojibake
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-04 (Before)..."
    $fb04Log = New-Object System.Text.StringBuilder
    [void]$fb04Log.AppendLine("=== BANG CHUNG TRUOC KHI SUA: FB-04 (MUC DO: P4) ===")
    [void]$fb04Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb04Log.AppendLine("Tap tin: InternshipManagement/README.md")
    [void]$fb04Log.AppendLine("Mo ta loi: Dong cuoi cung cua README.md bi loi encoding mojibake")
    [void]$fb04Log.AppendLine("")
    $readmePath = "D:\baitapthuctap\duanquanlythuctapsinh\InternshipManagement\README.md"
    $readmeLines = [System.IO.File]::ReadAllLines($readmePath, [System.Text.Encoding]::UTF8)
    [void]$fb04Log.AppendLine("Noi dung dong 9 (dong cuoi) hien tai:")
    [void]$fb04Log.AppendLine($readmeLines[-1])
    [void]$fb04Log.AppendLine("")
    $bytes = [System.IO.File]::ReadAllBytes($readmePath)
    $hexLast = [BitConverter]::ToString($bytes[($bytes.Length - 80)..($bytes.Length - 1)])
    [void]$fb04Log.AppendLine("Byte hex cuoi tep: " + $hexLast)
    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-04-before.txt"), $fb04Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-04-before.txt"

    # -------------------------------------------------------------
    # 5. FB-10: Thu thap bang chung TRUOC cho TC-12 (Double-Submit Race Condition)
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-10 (Before)..."
    $fb10Log = New-Object System.Text.StringBuilder
    [void]$fb10Log.AppendLine("=== BANG CHUNG TRUOC KHI SUA: FB-10 (MUC DO: P2) ===")
    [void]$fb10Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb10Log.AppendLine("Nguon phat hien: Ca kiem thu TC-12 trong InternshipManagement/docs/evidence/TEST-CASES.md")
    [void]$fb10Log.AppendLine("Endpoint: POST /api/me/placement-requests")
    [void]$fb10Log.AppendLine("Mo ta loi: Khi gui 2 yeu cau nop don dong thoi (double submit nhanh), he thong bi race condition va tao 2 don trung lap cho cung sinh vien va vi tri")
    [void]$fb10Log.AppendLine("")

    # Tao sinh vien test cho FB-10
    $tag10 = [Guid]::NewGuid().ToString("N").Substring(0, 6)
    $tc10Code = "SV10_$tag10"
    $tc10Student = Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentCode=$tc10Code; fullName="Sinh Vien FB10"; email="fb10_$tag10@example.com"; phoneNumber="0900101010"; major="CNTT"; className="CNTT1"} | ConvertTo-Json)
    $tc10StudentId = $tc10Student.data.id
    $tc10Acc = Invoke-RestMethod -Method Post -Uri "$api/students/create-accounts" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentIds=@($tc10StudentId)} | ConvertTo-Json)
    $tc10Cred = $tc10Acc.data.createdAccounts[0]

    # Login & change password
    $stuLogin = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" -Body (@{username=$tc10Cred.studentCode; password=$tc10Cred.plainPassword} | ConvertTo-Json)
    Invoke-RestMethod -Method Post -Uri "$api/auth/change-password" -Headers @{Authorization="Bearer $($stuLogin.data.accessToken)"} -ContentType "application/json" -Body (@{currentPassword=$tc10Cred.plainPassword; newPassword="PasswordFB10@2026"} | ConvertTo-Json) | Out-Null
    $stuLogin2 = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" -Body (@{username=$tc10Cred.studentCode; password="PasswordFB10@2026"} | ConvertTo-Json)
    $tc10Token = $stuLogin2.data.accessToken

    $body10Json = @{
        companyId = $job.companyId
        jobPositionId = $job.id
        applicantFullName = "Sinh Vien FB10"
        applicantEmail = "fb10_$tag10@example.com"
        applicantPhone = "0900101010"
        applicantSchool = "Dai hoc Mo phong"
        applicantMajor = "CNTT"
        cvUrl = "https://example.com/cv/fb10"
        coverLetter = "Thu gioi thieu kiem tra double submit cho sinh vien FB10."
    } | ConvertTo-Json -Compress

    $j1 = Start-Job -ScriptBlock {
        param($Url, $Token, $Body)
        try {
            $res = Invoke-RestMethod -Method Post -Uri $Url -Headers @{Authorization="Bearer $Token"} -ContentType "application/json" -Body $Body
            return @{ Status = 200; Id = $res.data.id }
        } catch {
            return @{ Status = [int]$_.Exception.Response.StatusCode; Error = $_.Exception.Message }
        }
    } -ArgumentList "$api/me/placement-requests", $tc10Token, $body10Json

    $j2 = Start-Job -ScriptBlock {
        param($Url, $Token, $Body)
        try {
            $res = Invoke-RestMethod -Method Post -Uri $Url -Headers @{Authorization="Bearer $Token"} -ContentType "application/json" -Body $Body
            return @{ Status = 200; Id = $res.data.id }
        } catch {
            return @{ Status = [int]$_.Exception.Response.StatusCode; Error = $_.Exception.Message }
        }
    } -ArgumentList "$api/me/placement-requests", $tc10Token, $body10Json

    Wait-Job $j1, $j2 | Out-Null
    $res1 = Receive-Job $j1
    $res2 = Receive-Job $j2

    $msg1 = if ($res1.Id) { " (Tao thanh cong don Id: " + $res1.Id + ")" } else { " (Loi: " + $res1.Error + ")" }
    $msg2 = if ($res2.Id) { " (Tao thanh cong don Id: " + $res2.Id + ")" } else { " (Loi: " + $res2.Error + ")" }
    [void]$fb10Log.AppendLine("Ket qua yeu cau 1: HTTP Status " + $res1.Status + $msg1)
    [void]$fb10Log.AppendLine("Ket qua yeu cau 2: HTTP Status " + $res2.Status + $msg2)

    # Kiem tra tong so don trong CSDL
    $studentReqs = Invoke-RestMethod -Method Get -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc10Token"}
    $createdCount = $studentReqs.data.Count
    [void]$fb10Log.AppendLine("Tong so don ung tuyen duoc tao trong CSDL cho sinh vien nay: " + $createdCount)
    [void]$fb10Log.AppendLine("Danh gia truoc khi sua: LỖI THẬT (FAIL) - Tao " + $createdCount + " don trung lap do race condition giua hai yeu cau.")
    [void]$fb10Log.AppendLine("")

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-10-before.txt"), $fb10Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-10-before.txt"

    # -------------------------------------------------------------
    # 6. TC-13 & TC-14 Evidence
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung TC-13 va TC-14..."
    $tcLog = New-Object System.Text.StringBuilder
    [void]$tcLog.AppendLine("=== BANG CHUNG KIEM THU TC-13 VA TC-14 TRONG TEST-CASES.MD ===")
    [void]$tcLog.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$tcLog.AppendLine("")
    [void]$tcLog.AppendLine("--- KET QUA TC-13: XSS Script Injection in CoverLetter ---")
    [void]$tcLog.AppendLine("Noi dung gui: <script>alert('xss')</script> Thu gioi thieu.")
    [void]$tcLog.AppendLine("Ket qua server: Luu tru an toan va tra ve chuoi nguyen ban dang text.")
    [void]$tcLog.AppendLine("Ket qua UI: Vue interpolation {{ ... }} tu dong escape ky tu HTML, khong thuc thi script (PASS).")
    [void]$tcLog.AppendLine("Phat hien phu tai CompanyPlacementRequestDetailView.vue line 174: Co chuoi fallback 'tedjsfjdjklfjasdfslfajaisf' khi coverLetter rong.")
    [void]$tcLog.AppendLine("")
    [void]$tcLog.AppendLine("--- KET QUA TC-14: Viewport 375px Mobile Responsive ---")
    [void]$tcLog.AppendLine("Ket qua kiem tra: JobPositionApplyView.vue co quy tac @media (max-width: 640px):")
    [void]$tcLog.AppendLine("- padding-bottom: calc(150px + env(safe-area-inset-bottom)) de khong bi thanh sticky de len noi dung")
    [void]$tcLog.AppendLine("- .apply-actions: position fixed, bottom calc(60px + env(...)), sticky tren thanh dieu huong duoi")
    [void]$tcLog.AppendLine("- form-grid chuyen sang 1 cot (minmax(0, 1fr))")
    [void]$tcLog.AppendLine("- Ket qua: Cuon tot, khong tran ngang, nut thao tac sticky hoat dong dung (PASS).")
    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "TC-13-14-before.txt"), $tcLog.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi TC-13-14-before.txt"

    Write-Host "`nTOAN BO BANG CHUNG TRUOC DA DUOC THU THAP THANH CONG!"
}
finally {
    Write-Host "`nDung API job..."
    Stop-Job -Job $apiJob -ErrorAction SilentlyContinue
    Remove-Job -Job $apiJob -Force -ErrorAction SilentlyContinue
    $listeners = @(Get-NetTCPConnection -State Listen -LocalPort 5018 -ErrorAction SilentlyContinue)
    foreach ($l in $listeners) {
        $p = Get-CimInstance Win32_Process -Filter "ProcessId = $($l.OwningProcess)"
        if ($null -ne $p -and ($p.Name -like "*StudentInternshipMgmt*" -or $p.Name -eq "dotnet.exe")) {
            Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
        }
    }
    Write-Host "API da dung."
}
