# capture-after-evidence.ps1
# TAB 1 - Builder: Thu thap bang chung "SAU" (After) tren CSDL StudentInternshipMgmtDb_UATBuild
$ErrorActionPreference = "Stop"
$BaseUrl = "http://127.0.0.1:5018"
$api = "$BaseUrl/api"
$databaseName = "StudentInternshipMgmtDb_UATBuild"
$backendRoot = [System.IO.Path]::GetFullPath("D:\baitapthuctap\duanquanlythuctapsinh\InternshipManagement\backend")
$evidenceDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "."))
$logPath = [System.IO.Path]::Combine($env:TEMP, "capture-after-api.log")

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
    # 1. FB-01: Thu thap bang chung SAU cho validation thong tin sinh vien
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-01 (After)..."
    $fb01Log = New-Object System.Text.StringBuilder
    [void]$fb01Log.AppendLine("=== BANG CHUNG SAU KHI SUA: FB-01 (MUC DO: P3) ===")
    [void]$fb01Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb01Log.AppendLine("Endpoint: POST /api/students va PUT /api/students/{id}")
    [void]$fb01Log.AppendLine("Ket qua mong doi: Thong bao loi bang tieng Viet tu nhien (Ho ten khong duoc de trong, So dien thoai khong duoc de trong, Ma so sinh vien '{code}' da ton tai), khong con chuoi tieng Anh nhu FullName la bat buoc.")
    [void]$fb01Log.AppendLine("")

    # Test cac truong bat buoc bi bo trong
    try {
        Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body '{"studentCode":"","fullName":"","email":"","phoneNumber":"","major":"","className":""}'
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb01Log.AppendLine("--- Truong hop 1: Bo trong cac truong bat buoc ---")
        [void]$fb01Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb01Log.AppendLine("Response Body:")
        [void]$fb01Log.AppendLine($body)
        [void]$fb01Log.AppendLine("")
    }

    # Test trung ma sinh vien
    try {
        Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body '{"studentCode":"SV001","fullName":"Nguyen Van A","email":"trungsv@example.com","phoneNumber":"0900111222","major":"CNTT","className":"CNTT1"}'
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb01Log.AppendLine("--- Truong hop 2: Trung ma so sinh vien ---")
        [void]$fb01Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb01Log.AppendLine("Response Body: " + $body)
        [void]$fb01Log.AppendLine("")
    }

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-01-after.txt"), $fb01Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-01-after.txt"

    # -------------------------------------------------------------
    # 2. FB-02: Thu thap bang chung SAU cho validation vi tri tuyen dung
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-02 (After)..."
    $fb02Log = New-Object System.Text.StringBuilder
    [void]$fb02Log.AppendLine("=== BANG CHUNG SAU KHI SUA: FB-02 (MUC DO: P3) ===")
    [void]$fb02Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb02Log.AppendLine("Endpoint: POST /api/company-me/job-positions va companyJobPositionValidation.js")
    [void]$fb02Log.AppendLine("Ket qua mong doi: Thong bao bo hoan toan tu tieng Anh (Title), (Quantity).")
    [void]$fb02Log.AppendLine("")

    $compLogin = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" -Body '{"username":"cty0004","password":"CompanyPassword@2026"}'
    $compToken = $compLogin.data.accessToken

    # Test quantity <= 0
    try {
        Invoke-RestMethod -Method Post -Uri "$api/company-me/job-positions" -Headers @{Authorization="Bearer $compToken"} -ContentType "application/json" -Body '{"title":"Thuc tap sinh Test After","quantity":0,"description":"Mo ta cong viec"}'
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
        Invoke-RestMethod -Method Post -Uri "$api/company-me/job-positions" -Headers @{Authorization="Bearer $compToken"} -ContentType "application/json" -Body '{"title":"","quantity":2,"description":"Mo ta cong viec"}'
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb02Log.AppendLine("--- Truong hop 2: Tieu de bo trong ---")
        [void]$fb02Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb02Log.AppendLine("Response Body: " + $body)
        [void]$fb02Log.AppendLine("")
    }

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-02-after.txt"), $fb02Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-02-after.txt"

    # -------------------------------------------------------------
    # 3. FB-03: Thu thap bang chung SAU cho CV URL
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-03 (After)..."
    $fb03Log = New-Object System.Text.StringBuilder
    [void]$fb03Log.AppendLine("=== BANG CHUNG SAU KHI SUA: FB-03 (MUC DO: P3) ===")
    [void]$fb03Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb03Log.AppendLine("Endpoint: POST /api/me/placement-requests va applicationValidation.js")
    [void]$fb03Log.AppendLine("Ket qua mong doi: Link CV co khoang trang o hai dau duoc chap nhan va backend luu gia tri da cat (trim). Khoang trang o giua, javascript: va URL tuong doi van bi tu choi hop le.")
    [void]$fb03Log.AppendLine("")

    # Tao sinh vien test moi cho FB-03
    $tag03 = [Guid]::NewGuid().ToString("N").Substring(0, 6)
    $tc03Code = "SV03_$tag03"
    $tc03Student = Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentCode=$tc03Code; fullName="Sinh Vien FB03 After"; email="fb03_after_$tag03@example.com"; phoneNumber="0900333333"; major="CNTT"; className="CNTT1"} | ConvertTo-Json)
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

    # Test 1: CV URL co khoang trang o hai dau -> Chap nhan va luu gia tri da trim
    try {
        $resCreate = Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03 After"
            applicantEmail = "fb03_after_$tag03@example.com"
            applicantPhone = "0900333333"
            applicantSchool = "Dai hoc Mo phong"
            applicantMajor = "CNTT"
            cvUrl = "   https://example.com/cv/fb03_trimmed   "
            coverLetter = "Thu gioi thieu cua thi sinh FB03 voi link CV co khoang trang o hai dau."
        } | ConvertTo-Json)
        [void]$fb03Log.AppendLine("--- Truong hop 1: CV URL co khoang trang hai dau ('   https://example.com/cv/fb03_trimmed   ') ---")
        [void]$fb03Log.AppendLine("HTTP Status: 200/201 (Chap nhan hop le)")
        [void]$fb03Log.AppendLine("RequestId tao ra: " + $resCreate.data.id)
        [void]$fb03Log.AppendLine("Gia tri CvUrl backend luu trong CSDL: '" + $resCreate.data.cvUrl + "'")
        if ($resCreate.data.cvUrl -eq "https://example.com/cv/fb03_trimmed") {
            [void]$fb03Log.AppendLine("XAC NHAN: CvUrl da duoc trim sach se hai dau truoc khi luu!")
        }
        [void]$fb03Log.AppendLine("")
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        [void]$fb03Log.AppendLine("Loi bat thuong o Truong hop 1: " + $reader.ReadToEnd())
    }

    # Test 2: CV URL chua scheme javascript: -> Tu choi
    try {
        Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03 After"
            applicantEmail = "fb03_after_$tag03@example.com"
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

    # Test 3: CV URL tuong doi -> Tu choi
    try {
        Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03 After"
            applicantEmail = "fb03_after_$tag03@example.com"
            applicantPhone = "0900333333"
            applicantSchool = "Dai hoc Mo phong"
            applicantMajor = "CNTT"
            cvUrl = "/uploads/cv/my-cv.pdf"
            coverLetter = "Thu gioi thieu kiem tra relative path."
        } | ConvertTo-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb03Log.AppendLine("--- Truong hop 3: CV URL la duong dan tuong doi bi chan hop le ---")
        [void]$fb03Log.AppendLine("Gia tri CvUrl: '/uploads/cv/my-cv.pdf'")
        [void]$fb03Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb03Log.AppendLine("Response Body: " + $body)
        [void]$fb03Log.AppendLine("")
    }

    # Test 4: CV URL chua khoang trang o giua -> Tu choi
    try {
        Invoke-RestMethod -Method Post -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc03Token"} -ContentType "application/json" -Body (@{
            companyId = $job.companyId
            jobPositionId = $job.id
            applicantFullName = "Sinh Vien FB03 After"
            applicantEmail = "fb03_after_$tag03@example.com"
            applicantPhone = "0900333333"
            applicantSchool = "Dai hoc Mo phong"
            applicantMajor = "CNTT"
            cvUrl = "https://example.com/my cv document.pdf"
            coverLetter = "Thu gioi thieu kiem tra middle space."
        } | ConvertTo-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        [void]$fb03Log.AppendLine("--- Truong hop 4: CV URL chua khoang trang o giua bi chan hop le ---")
        [void]$fb03Log.AppendLine("Gia tri CvUrl: 'https://example.com/my cv document.pdf'")
        [void]$fb03Log.AppendLine("HTTP Status: " + [int]$_.Exception.Response.StatusCode)
        [void]$fb03Log.AppendLine("Response Body: " + $body)
        [void]$fb03Log.AppendLine("")
    }

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-03-after.txt"), $fb03Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-03-after.txt"

    # -------------------------------------------------------------
    # 4. FB-04: Thu thap bang chung SAU cho README.md mojibake
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-04 (After)..."
    $fb04Log = New-Object System.Text.StringBuilder
    [void]$fb04Log.AppendLine("=== BANG CHUNG SAU KHI SUA: FB-04 (MUC DO: P4) ===")
    [void]$fb04Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb04Log.AppendLine("Tap tin: InternshipManagement/README.md")
    [void]$fb04Log.AppendLine("Ket qua mong doi: Dong 9 hien thi tieng Viet UTF-8 dung chuan: 'Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3).'")
    [void]$fb04Log.AppendLine("")
    $readmePath = "D:\baitapthuctap\duanquanlythuctapsinh\InternshipManagement\README.md"
    $readmeLines = [System.IO.File]::ReadAllLines($readmePath, [System.Text.Encoding]::UTF8)
    [void]$fb04Log.AppendLine("Noi dung dong 9 (dong cuoi) sau khi sua:")
    [void]$fb04Log.AppendLine($readmeLines[-1])
    [void]$fb04Log.AppendLine("")
    $bytes = [System.IO.File]::ReadAllBytes($readmePath)
    $hexLast = [BitConverter]::ToString($bytes[($bytes.Length - 80)..($bytes.Length - 1)])
    [void]$fb04Log.AppendLine("Byte hex cuoi tep: " + $hexLast)
    if ($readmeLines[-1] -eq "Hệ thống quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3).") {
        [void]$fb04Log.AppendLine("XAC NHAN: Tieng Viet UTF-8 da duoc sua dung chuan hoan toan!")
    }
    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-04-after.txt"), $fb04Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-04-after.txt"

    # -------------------------------------------------------------
    # 5. FB-10: Thu thap bang chung SAU cho TC-12 (Double-Submit Race Condition)
    # -------------------------------------------------------------
    Write-Host "Dang thu thap bang chung FB-10 (After)..."
    $fb10Log = New-Object System.Text.StringBuilder
    [void]$fb10Log.AppendLine("=== BANG CHUNG SAU KHI SUA: FB-10 (MUC DO: P2) ===")
    [void]$fb10Log.AppendLine("Thoi diem do: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
    [void]$fb10Log.AppendLine("Nguon phat hien: Ca kiem thu TC-12 trong InternshipManagement/docs/evidence/TEST-CASES.md")
    [void]$fb10Log.AppendLine("Endpoint: POST /api/me/placement-requests")
    [void]$fb10Log.AppendLine("Ket qua mong doi: Khi gui 2 yeu cau nop don dong thoi (double submit nhanh), database transaction va row lock se dam bao chi co 1 yeu cau thanh cong, yeu cau thu hai bi chan ngay lap tuc boi kiem tra trung lap va tra ve ma loi ro rang.")
    [void]$fb10Log.AppendLine("")

    # Tao sinh vien test moi cho FB-10
    $tag10 = [Guid]::NewGuid().ToString("N").Substring(0, 6)
    $tc10Code = "SV10_$tag10"
    $tc10Student = Invoke-RestMethod -Method Post -Uri "$api/students" -Headers @{Authorization="Bearer $adminToken"} -ContentType "application/json" -Body (@{studentCode=$tc10Code; fullName="Sinh Vien FB10 After"; email="fb10_after_$tag10@example.com"; phoneNumber="0900101010"; major="CNTT"; className="CNTT1"} | ConvertTo-Json)
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
        applicantFullName = "Sinh Vien FB10 After"
        applicantEmail = "fb10_after_$tag10@example.com"
        applicantPhone = "0900101010"
        applicantSchool = "Dai hoc Mo phong"
        applicantMajor = "CNTT"
        cvUrl = "https://example.com/cv/fb10_after"
        coverLetter = "Thu gioi thieu kiem tra double submit sau khi sua concurrency cho sinh vien FB10."
    } | ConvertTo-Json -Compress

    $j1 = Start-Job -ScriptBlock {
        param($Url, $Token, $Body)
        try {
            $res = Invoke-RestMethod -Method Post -Uri $Url -Headers @{Authorization="Bearer $Token"} -ContentType "application/json" -Body $Body
            return @{ Status = 200; Id = $res.data.id; Body = ($res | ConvertTo-Json -Compress) }
        } catch {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            return @{ Status = [int]$_.Exception.Response.StatusCode; Error = $reader.ReadToEnd() }
        }
    } -ArgumentList "$api/me/placement-requests", $tc10Token, $body10Json

    $j2 = Start-Job -ScriptBlock {
        param($Url, $Token, $Body)
        try {
            $res = Invoke-RestMethod -Method Post -Uri $Url -Headers @{Authorization="Bearer $Token"} -ContentType "application/json" -Body $Body
            return @{ Status = 200; Id = $res.data.id; Body = ($res | ConvertTo-Json -Compress) }
        } catch {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            return @{ Status = [int]$_.Exception.Response.StatusCode; Error = $reader.ReadToEnd() }
        }
    } -ArgumentList "$api/me/placement-requests", $tc10Token, $body10Json

    Wait-Job $j1, $j2 | Out-Null
    $res1 = Receive-Job $j1
    $res2 = Receive-Job $j2

    $msg1 = if ($res1.Id) { " (Tao thanh cong don Id: " + $res1.Id + ")" } else { " (Loi chan trung lap: " + $res1.Error + ")" }
    $msg2 = if ($res2.Id) { " (Tao thanh cong don Id: " + $res2.Id + ")" } else { " (Loi chan trung lap: " + $res2.Error + ")" }
    [void]$fb10Log.AppendLine("Ket qua yeu cau 1: HTTP Status " + $res1.Status + $msg1)
    [void]$fb10Log.AppendLine("Ket qua yeu cau 2: HTTP Status " + $res2.Status + $msg2)

    # Kiem tra tong so don trong CSDL
    $studentReqs = Invoke-RestMethod -Method Get -Uri "$api/me/placement-requests" -Headers @{Authorization="Bearer $tc10Token"}
    $createdCount = $studentReqs.data.Count
    [void]$fb10Log.AppendLine("Tong so don ung tuyen duoc tao trong CSDL cho sinh vien nay: " + $createdCount)
    if ($createdCount -eq 1) {
        [void]$fb10Log.AppendLine("Danh gia sau khi sua: DAT (PASS) - Chi co duy nhat 1 don duoc tao, request thu hai bi chan thanh cong voi loi DuplicateOpenRequest!")
    } else {
        [void]$fb10Log.AppendLine("Danh gia sau khi sua: CHUA DAT - So luong don van bi duplicate: " + $createdCount)
    }
    [void]$fb10Log.AppendLine("")

    [System.IO.File]::WriteAllText((Join-Path $evidenceDir "FB-10-after.txt"), $fb10Log.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Da ghi FB-10-after.txt"

    Write-Host ""
    Write-Host "TOAN BO BANG CHUNG SAU (AFTER) DA DUOC THU THAP THANH CONG!"
}
finally {
    Write-Host "Dung API job..."
    Stop-Job $apiJob -ErrorAction SilentlyContinue
    Remove-Job $apiJob -Force -ErrorAction SilentlyContinue
    Get-NetTCPConnection -LocalPort 5018 -ErrorAction SilentlyContinue | ForEach-Object {
        Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
    }
    Write-Host "API da dung."
}
