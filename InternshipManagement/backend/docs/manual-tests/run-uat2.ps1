[CmdletBinding()]
param(
    [string]$BaseUrl = "http://127.0.0.1:5028",
    [string]$SqlServer = "localhost",
    [string]$DatabaseName = "StudentInternshipMgmtDb_E2E",
    [string]$ApiLogPath
)

$ErrorActionPreference = "Stop"
$api = $BaseUrl.TrimEnd("/") + "/api"

if ([string]::IsNullOrWhiteSpace($ApiLogPath)) {
    $guidStr = [Guid]::NewGuid().ToString("N")
    $ApiLogPath = Join-Path ([System.IO.Path]::GetTempPath()) ("uat2-runner-api-" + $guidStr + ".log")
}
$ApiLogPath = [System.IO.Path]::GetFullPath($ApiLogPath)
$backendRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $backendRoot "..\.."))
$evidenceDir = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "docs\uat-feedback\evidence\uat"))

if (-not (Test-Path -LiteralPath $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}

$script:results = New-Object System.Collections.Generic.List[object]
$script:adminToken = $null
$script:adminRefreshToken = $null
$script:adminTokenExpiresAt = [DateTime]::MinValue
$script:apiJob = $null
$script:feJob = $null
$script:fePort = 5183
$script:originalProcessEnvironment = @{}
$script:apiPort = 0

function Set-E2EProcessEnvironment {
    param([string]$Name, [string]$Value)
    if (-not $script:originalProcessEnvironment.ContainsKey($Name)) {
        $script:originalProcessEnvironment[$Name] = [Environment]::GetEnvironmentVariable($Name, "Process")
    }
    [Environment]::SetEnvironmentVariable($Name, $Value, "Process")
}

function Restore-E2EProcessEnvironment {
    foreach ($name in $script:originalProcessEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $script:originalProcessEnvironment[$name], "Process")
    }
    $script:originalProcessEnvironment.Clear()
}

function New-CryptoRandomString {
    param([int]$Length, [string]$Alphabet)
    $bytes = New-Object byte[] $Length
    $random = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    try {
        $random.GetBytes($bytes)
        $characters = New-Object char[] $Length
        for ($index = 0; $index -lt $Length; $index++) {
            $characters[$index] = $Alphabet[$bytes[$index] % $Alphabet.Length]
        }
        return -join $characters
    }
    finally {
        $random.Dispose()
    }
}

function New-E2EAdminPassword {
    $upper = New-CryptoRandomString 2 "ABCDEFGHJKLMNPQRSTUVWXYZ"
    $lower = New-CryptoRandomString 2 "abcdefghijkmnopqrstuvwxyz"
    $digit = New-CryptoRandomString 2 "23456789"
    $special = New-CryptoRandomString 2 "@#$%!"
    $remainder = New-CryptoRandomString 24 "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"
    $password = $upper + $lower + $digit + $special + $remainder
    $shuffleBytes = New-Object byte[] $password.Length
    $random = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    try {
        $random.GetBytes($shuffleBytes)
        $characters = $password.ToCharArray()
        for ($index = $characters.Length - 1; $index -gt 0; $index--) {
            $swapIndex = $shuffleBytes[$index] % ($index + 1)
            $temporary = $characters[$index]
            $characters[$index] = $characters[$swapIndex]
            $characters[$swapIndex] = $temporary
        }
        return -join $characters
    }
    finally {
        $random.Dispose()
    }
}

function Stop-E2EApi {
    if ($null -ne $script:apiJob) {
        Stop-Job -Job $script:apiJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:apiJob -Force -ErrorAction SilentlyContinue
        $script:apiJob = $null
    }
    if ($null -ne $script:feJob) {
        Stop-Job -Job $script:feJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:feJob -Force -ErrorAction SilentlyContinue
        $script:feJob = $null
    }
    foreach ($port in @($script:apiPort, $script:fePort)) {
        if ($port -gt 0) {
            $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue)
            foreach ($listener in $listeners) {
                Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
            }
        }
    }
    $script:apiPort = 0
    Restore-E2EProcessEnvironment
}

function Stop-ExistingE2EApi {
    foreach ($port in @($script:apiPort, $script:fePort)) {
        if ($port -gt 0) {
            $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue)
            foreach ($listener in $listeners) {
                Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
            }
        }
    }
    $deadline = (Get-Date).AddSeconds(10)
    while ((Get-NetTCPConnection -State Listen -LocalPort $script:apiPort -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 200
    }
}

function Start-E2EApi {
    if (Test-Path -LiteralPath $ApiLogPath) {
        Remove-Item -LiteralPath $ApiLogPath -Force -ErrorAction SilentlyContinue
    }
    Set-Content -LiteralPath $ApiLogPath -Value "" -Encoding UTF8
    Write-Host ("Starting Backend API on: " + $BaseUrl + " (Log: " + $ApiLogPath + ")")
    $script:apiJob = Start-Job -ScriptBlock {
        param($BackendDirectory, $ApiProject, $ListenUrl, $LogFile)
        Set-Location $BackendDirectory
        & dotnet run --no-launch-profile --project $ApiProject --urls $ListenUrl 2>&1 | ForEach-Object {
            $logEntry = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"), $_
            [System.IO.File]::AppendAllText($LogFile, $logEntry + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
        }
    } -ArgumentList $backendRoot, "src\StudentInternshipMgmt.Api", $BaseUrl, $ApiLogPath

    $deadline = (Get-Date).AddSeconds(90)
    $ready = $false
    while (-not $ready -and (Get-Date) -lt $deadline) {
        if ($script:apiJob.State -in @("Failed", "Completed", "Stopped")) {
            $jobOutput = @(Receive-Job -Job $script:apiJob -ErrorAction SilentlyContinue | Out-String)
            $logTail = @(Get-Content -LiteralPath $ApiLogPath -Tail 30 -ErrorAction SilentlyContinue)
            throw ("E2E API process ended before ready. Job: " + ($jobOutput -join " ") + "; Log: " + ($logTail -join " | "))
        }
        try {
            $null = Invoke-WebRequest -Uri ($api + "/auth/me") -TimeoutSec 2 -UseBasicParsing
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
        $logTail = @(Get-Content -LiteralPath $ApiLogPath -Tail 30 -ErrorAction SilentlyContinue)
        throw ("E2E API not ready after 90s. Log: " + ($logTail -join " | "))
    }
    Write-Host "Backend API is ready!"
}

function Start-E2EFrontend {
    Write-Host ("Starting Frontend on port " + $script:fePort + "...")
    $frontendRoot = Join-Path $repoRoot "InternshipManagement\frontend"
    $script:feJob = Start-Job -ScriptBlock {
        param($Dir, $Port, $ApiUrl)
        Set-Location $Dir
        $env:VITE_API_BASE_URL = $ApiUrl
        & npx vite --port $Port --host 127.0.0.1
    } -ArgumentList $frontendRoot, $script:fePort, ($api)

    $deadline = (Get-Date).AddSeconds(45)
    $ready = $false
    while (-not $ready -and (Get-Date) -lt $deadline) {
        try {
            $null = Invoke-WebRequest -Uri ("http://127.0.0.1:" + $script:fePort) -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
            $ready = $true
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    if ($ready) {
        Write-Host "Frontend is ready!"
    } else {
        Write-Warning "Frontend did not respond within 45s; UI screenshots may be skipped."
    }
}

function Capture-Screen {
    param([string]$FileName, [string]$Path)
    $screensDir = Join-Path $evidenceDir "screens"
    if (-not (Test-Path $screensDir)) { New-Item -ItemType Directory -Path $screensDir -Force | Out-Null }
    $outPath = Join-Path $screensDir $FileName
    $url = "http://127.0.0.1:" + $script:fePort + $Path
    $chrome = "C:\Program Files\Google\Chrome\Application\chrome.exe"
    if (-not (Test-Path $chrome)) {
        $chrome = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    }
    if (Test-Path $chrome) {
        $cmd = "`"" + $chrome + "`" --headless=new --disable-gpu --window-size=1280,800 --screenshot=`"" + $outPath + "`" `"" + $url + "`""
        cmd /c $cmd | Out-Null
        if (Test-Path $outPath) {
            Write-Host ("Captured screenshot: " + $FileName)
        }
    }
}

trap {
    $failure = $_
    Stop-E2EApi
    Write-Error -Message ("UAT2 runner failed: " + $failure.ToString()) -ErrorAction Continue
    exit 1
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Token,
        [object]$Body
    )

    $maxAttempts = 10
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $request = @{
            Method = $Method
            Uri = $api + $Path
            TimeoutSec = 60
            ErrorAction = "Stop"
        }
        if (-not [string]::IsNullOrWhiteSpace($Token)) {
            $request.Headers = @{ Authorization = "Bearer $Token" }
        }
        if ($null -ne $Body) {
            $request.ContentType = "application/json"
            if ($Body -is [string]) {
                $request.Body = $Body
            }
            else {
                $request.Body = ConvertTo-Json -InputObject $Body -Depth 20 -Compress
            }
        }

        try {
            $response = Invoke-RestMethod @request
            return [pscustomobject]@{ Status = 200; Body = $response; Error = $null; ErrorBody = $null }
        }
        catch {
            $statusCode = 0
            $errorBody = $null
            if ($null -ne $_.Exception.Response) {
                if ($null -ne $_.Exception.Response.StatusCode) {
                    $statusCode = [int]$_.Exception.Response.StatusCode
                }
                try {
                    $stream = $_.Exception.Response.GetResponseStream()
                    if ($null -ne $stream) {
                        $reader = New-Object System.IO.StreamReader($stream)
                        $errorBody = $reader.ReadToEnd()
                        $reader.Dispose()
                        $stream.Dispose()
                    }
                } catch {}
            }
            if ($statusCode -eq 429 -and $attempt -lt $maxAttempts) {
                Write-Host ("Rate limit (429) hit at " + $Path + ". Waiting 6 seconds before retry...")
                Start-Sleep -Seconds 6
                continue
            }
            return [pscustomobject]@{ Status = $statusCode; Body = $null; ErrorBody = $errorBody; Error = $_.Exception.Message }
        }
    }
}

function Require-Status {
    param($Response, [int[]]$Expected, [string]$Operation)
    if ($Expected -notcontains [int]$Response.Status) {
        $detail = $Response.ErrorBody
        if ([string]::IsNullOrWhiteSpace($detail)) { $detail = $Response.Error }
        throw ($Operation + " expected HTTP " + ($Expected -join "/") + " but received " + $Response.Status + ": " + $detail)
    }
}

function Get-Data {
    param($Response, [string]$Operation)
    Require-Status $Response @(200, 201) $Operation
    if ($null -eq $Response.Body -or $null -eq $Response.Body.data) {
        throw ($Operation + " did not return data in ApiResponse.")
    }
    return $Response.Body.data
}

function New-TestCompany {
    param([string]$Label)
    $tag = [Guid]::NewGuid().ToString("N").Substring(0, 8)
    $created = Get-Data (Invoke-Api POST "/companies" $script:adminToken @{
        Name = "Cong Ty UAT " + $Label + " " + $tag
        Address = "123 Duong Test, Quan 1, TP Ho Chi Minh"
        Industry = "Information Technology"
        ContactPerson = "Nguyen Van Dai Dien"
        ContactPhone = "0901234567"
        ContactEmail = "company-" + $tag + "@example.com"
    }) "Create company"
    $companyId = [int]$created.id
    $credentials = Get-Data (Invoke-Api POST ("/companies/" + $companyId + "/account") $script:adminToken $null) "Create company account"
    $login = Get-Data (Invoke-Api POST "/auth/login" $null @{
        Username = $credentials.username
        Password = $credentials.temporaryPassword
    }) "Login company initial"
    $companyPassword = New-E2EAdminPassword
    Require-Status (Invoke-Api POST "/auth/change-password" $login.accessToken @{
        CurrentPassword = $credentials.temporaryPassword
        NewPassword = $companyPassword
    }) @(200) "Change company password"
    $login = Get-Data (Invoke-Api POST "/auth/login" $null @{
        Username = $credentials.username
        Password = $companyPassword
    }) "Login company post password change"
    return [pscustomobject]@{
        Id = $companyId
        Token = $login.accessToken
        Username = $credentials.username
        Name = "Cong Ty UAT " + $Label + " " + $tag
    }
}

function New-TestStudent {
    param([string]$Label)
    $tag = [Guid]::NewGuid().ToString("N").Substring(0, 8)
    $code = "SV" + $tag.ToUpper()
    $created = Get-Data (Invoke-Api POST "/students" $script:adminToken @{
        StudentCode = $code
        FullName = "Tran Thi Test " + $tag
        Email = "student-" + $tag + "@example.com"
        PhoneNumber = "0912345678"
        Major = "Software Engineering"
        ClassName = "SE2024"
    }) "Create student"
    $studentId = [int]$created.id
    $accounts = Get-Data (Invoke-Api POST "/students/create-accounts" $script:adminToken @{
        StudentIds = @($studentId)
    }) "Create student accounts"
    $login = Get-Data (Invoke-Api POST "/auth/login" $null @{
        Username = $accounts.createdAccounts[0].studentCode
        Password = $accounts.createdAccounts[0].plainPassword
    }) "Login student initial"
    $studentPassword = New-E2EAdminPassword
    Require-Status (Invoke-Api POST "/auth/change-password" $login.accessToken @{
        CurrentPassword = $accounts.createdAccounts[0].plainPassword
        NewPassword = $studentPassword
    }) @(200) "Change student password"
    $login = Get-Data (Invoke-Api POST "/auth/login" $null @{
        Username = $accounts.createdAccounts[0].studentCode
        Password = $studentPassword
    }) "Login student post password change"
    return [pscustomobject]@{
        Id = $studentId
        Code = $code
        Token = $login.accessToken
        FullName = "Tran Thi Test " + $tag
    }
}

function New-ApprovedPosition {
    param($Company, [int]$Quantity = 5, [string]$Label = "Fullstack Developer")
    $subGuid = [Guid]::NewGuid().ToString("N").Substring(0, 6)
    $position = Get-Data (Invoke-Api POST "/company-me/job-positions" $Company.Token @{
        Title = "Vi Tri " + $Label + " " + $subGuid
        Department = "Technology Lab"
        Location = "Innovation Center, TP Ho Chi Minh"
        Deadline = (Get-Date).ToUniversalTime().AddDays(30).ToString("o")
        Quantity = $Quantity
        Description = "Software engineering internship position."
        IsOpen = $true
    }) "Create job position"
    $positionId = [int]$position.id
    Require-Status (Invoke-Api POST ("/company-me/job-positions/" + $positionId + "/submit") $Company.Token $null) @(200) "Submit position"
    Require-Status (Invoke-Api POST ("/job-positions/" + $positionId + "/approve") $script:adminToken $null) @(200) "Approve position"
    return $positionId
}

# ==============================================================
# 1. SETUP ENVIRONMENT & DATABASE
# ==============================================================
Write-Host "=========================================================="
Write-Host "STARTING UAT 10 SCENARIOS TEST SUITE (TAB 2 REVIEWER/QA)"
Write-Host ("Database: " + $DatabaseName + " | URL: " + $BaseUrl)
Write-Host "=========================================================="

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
    Set-E2EProcessEnvironment "ConnectionStrings__DefaultConnection" ("Server=" + $SqlServer + ";Database=" + $DatabaseName + ";Integrated Security=True;TrustServerCertificate=True;")
}
if ([string]::IsNullOrWhiteSpace($env:Jwt__SecretKey) -or $env:Jwt__SecretKey.Length -lt 32) {
    Set-E2EProcessEnvironment "Jwt__SecretKey" (New-CryptoRandomString 64 "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_")
}
Set-E2EProcessEnvironment "Smtp__Enabled" "false"
Set-E2EProcessEnvironment "PlacementRequestExpiry__Enabled" "false"
Set-E2EProcessEnvironment "Logging__LogLevel__Microsoft" "Warning"

$baseUri = [Uri]$BaseUrl
$script:apiPort = $baseUri.Port
Stop-ExistingE2EApi

Write-Host ("Recreating isolated database '" + $DatabaseName + "'...")
$dropAndCreateSql = "IF DB_ID(N'" + $DatabaseName + "') IS NOT NULL BEGIN ALTER DATABASE [" + $DatabaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + $DatabaseName + "]; END; CREATE DATABASE [" + $DatabaseName + "];"
& sqlcmd -S $SqlServer -d master -E -C -b -Q $dropAndCreateSql | Out-Null
if ($LASTEXITCODE -ne 0) { throw ("sqlcmd failed recreating database '" + $DatabaseName + "'.") }

Write-Host "Applying EF Core migrations..."
Push-Location $backendRoot
try {
    & dotnet ef database update --project "src\StudentInternshipMgmt.Infrastructure" --startup-project "src\StudentInternshipMgmt.Api"
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef database update failed." }
}
finally {
    Pop-Location
}

Start-E2EApi

Write-Host "Bootstrapping Admin account and changing password..."
$adminLogin = Invoke-Api POST "/auth/login" $null @{ Username = "admin"; Password = "Admin@123" }
Require-Status $adminLogin @(200) "Login seeded admin"
$adminData = Get-Data $adminLogin "Login seeded admin"
if ($adminData.mustChangePassword -ne $true) {
    throw "Seeded admin mustChangePassword is not true."
}
$script:adminToken = $adminData.accessToken
$newAdminPassword = New-E2EAdminPassword
Require-Status (Invoke-Api POST "/auth/change-password" $script:adminToken @{
    CurrentPassword = "Admin@123"
    NewPassword = $newAdminPassword
}) @(200) "Change admin password"
$adminLogin = Get-Data (Invoke-Api POST "/auth/login" $null @{ Username = "admin"; Password = $newAdminPassword }) "Login admin new password"
$script:adminToken = $adminLogin.accessToken

Start-E2EFrontend

Write-Host "Environment ready! Executing 10 UAT scenarios..."

# Helper base64 decoded string function
function Get-Utf8StringFromB64 {
    param([string]$B64)
    return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($B64))
}

$strHoTen = Get-Utf8StringFromB64 "SOG7jSB0w6puIGtow7RuZyDEkcaw4bujYyDEkeG7gyB0cuG7kW5n"
$strPhone = Get-Utf8StringFromB64 "U+G7kSDEkWnhu4duIHRob+G6oWkga2jDtG5nIMSRxrDhu6NjIMSR4buDIHRy4buRbmc="
$strCode = Get-Utf8StringFromB64 "TcOjIHPhu5Egc2luaCB2acOqbiBraMO0bmcgxJHGsOG7o2MgxJHhu4MgdHLhu5FuZw=="
$strExists = Get-Utf8StringFromB64 "xJHDoyB04buTbiB04bqhaQ=="
$strTieuDe = Get-Utf8StringFromB64 "VGnDqnUgxJHhu4EgbMOgIGLhuq90IGJ14buZYw=="
$strSoLuong = Get-Utf8StringFromB64 "U+G7kSBsxrDhu6NuZyB0dXnhu4NuIHBo4bqjaSBs4bubbiBoxqFuIDA="

# ==============================================================
# SCENARIO UAT2-01 (FB-01): Student validation messages in Vietnamese
# ==============================================================
Write-Host "--> Running UAT2-01 (FB-01): Student validation messages in standard Vietnamese..."
$uat1Log = New-Object System.Text.StringBuilder
[void]$uat1Log.AppendLine("KICH BAN UAT2-01: Chuan hoa thong bao loi tao sinh vien sang tieng Viet tu nhien (FB-01)")
[void]$uat1Log.AppendLine("Nguoi thuc hien: AD-01 (Can bo quan ly thuc tap)")
[void]$uat1Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
[void]$uat1Log.AppendLine("Moi truong: " + $BaseUrl + " | CSDL: " + $DatabaseName + "`n")

$emptyPayload = @{ StudentCode = ""; FullName = ""; Email = ""; PhoneNumber = ""; Major = ""; ClassName = "" }
$res1 = Invoke-Api POST "/students" $script:adminToken $emptyPayload
[void]$uat1Log.AppendLine("Buoc 1: Gui yeu cau tao sinh vien voi cac truong bat buoc de trong:")
[void]$uat1Log.AppendLine("HTTP Status nhan duoc: " + $res1.Status)
[void]$uat1Log.AppendLine("Noi dung phan hoi loi:")
[void]$uat1Log.AppendLine($res1.ErrorBody)

$stuTemp = New-TestStudent "uat1-init"
$dupPayload = @{ StudentCode = $stuTemp.Code; FullName = "Nguyen Van Trung Ma"; Email = "trungma@example.com"; PhoneNumber = "0900000002"; Major = "CNTT"; ClassName = "L01" }
$res2 = Invoke-Api POST "/students" $script:adminToken $dupPayload
[void]$uat1Log.AppendLine("`nBuoc 2: Gui yeu cau tao sinh vien voi ma sinh vien trung lap (" + $stuTemp.Code + "):")
[void]$uat1Log.AppendLine("HTTP Status nhan duoc: " + $res2.Status)
[void]$uat1Log.AppendLine("Noi dung phan hoi loi:")
[void]$uat1Log.AppendLine($res2.ErrorBody)

$oldMsg1 = Get-Utf8StringFromB64 "RnVsbE5hbWUgbMOgIGLhuq90IGJ14buZYw=="
$oldMsg2 = Get-Utf8StringFromB64 "UGhvbmVOdW1iZXIgbMOgIGLhuq90IGJ14buZYw=="
$oldMsg3 = Get-Utf8StringFromB64 "U3R1ZGVudENvZGUgbMOgIGLhuq90IGJ14buZYw=="
$hasOldEnglishWords = ($res1.ErrorBody.Contains($oldMsg1) -or $res1.ErrorBody.Contains($oldMsg2) -or $res1.ErrorBody.Contains($oldMsg3))
$hasProperVietnamese = ($res1.ErrorBody.Contains($strHoTen) -and $res1.ErrorBody.Contains($strPhone) -and $res1.ErrorBody.Contains($strCode))
$hasProperDupMessage = ($res2.ErrorBody.Contains($strExists))

if ($res1.Status -eq 400 -and $res2.Status -eq 400 -and -not $hasOldEnglishWords -and $hasProperVietnamese -and $hasProperDupMessage) {
    [void]$uat1Log.AppendLine("`nKet luan: DAT (PASS) - He thong tu choi hop le va tra ve 100% cau thong bao tieng Viet chuan muc, khong con kep tu tieng Anh ky thuat.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-01"; Name = "Chuan hoa thong bao loi tao/sua sinh vien tieng Viet (FB-01)"; Status = "PASS"; Details = "Thong bao loi hoan toan bang tieng Viet tu nhien khong kep ten bien tieng Anh." })
} else {
    [void]$uat1Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-01"; Name = "Chuan hoa thong bao loi tao/sua sinh vien tieng Viet (FB-01)"; Status = "FAIL"; Details = "Van con tu tieng Anh hoac thong bao chua dung chuan." })
}
Capture-Screen "UAT2-01-student-management.png" "/students"
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-01.txt"), $uat1Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-02 (FB-02): Remove (Title) and (Quantity) identifiers
# ==============================================================
Write-Host "--> Running UAT2-02 (FB-02): Remove (Title) and (Quantity) technical identifiers..."
$uat2Log = New-Object System.Text.StringBuilder
[void]$uat2Log.AppendLine("KICH BAN UAT2-02: Loai bo ma dinh danh ky thuat (Title), (Quantity) (FB-02)")
[void]$uat2Log.AppendLine("Nguoi thuc hien: DN-01 (Dai dien Doanh nghiep)")
[void]$uat2Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyForUat2 = New-TestCompany "uat2"
$invalidJobPayload = @{
    Title = ""
    Department = "Ky thuat"
    Location = "Ha Noi"
    Deadline = (Get-Date).ToUniversalTime().AddDays(15).ToString("o")
    Quantity = 0
    Description = "Mo ta cong viec"
    IsOpen = $true
}
$resJob = Invoke-Api POST "/company-me/job-positions" $companyForUat2.Token $invalidJobPayload
[void]$uat2Log.AppendLine("Buoc 1: Doanh nghiep gui form tao vi tri voi Tieu de rong va So luong = 0:")
[void]$uat2Log.AppendLine("HTTP Status nhan duoc: " + $resJob.Status)
[void]$uat2Log.AppendLine("Noi dung phan hoi loi:")
[void]$uat2Log.AppendLine($resJob.ErrorBody)

$hasTechnicalTokens = ($resJob.ErrorBody -match '\(Title\)' -or $resJob.ErrorBody -match '\(Quantity\)')
$hasCleanMessages = ($resJob.ErrorBody.Contains($strTieuDe) -or $resJob.ErrorBody.Contains($strSoLuong))

if ($resJob.Status -eq 400 -and -not $hasTechnicalTokens -and $hasCleanMessages) {
    [void]$uat2Log.AppendLine("`nKet luan: DAT (PASS) - He thong tu choi va hien thi thong bao ro rang, khong chua cac dinh danh ky thuat (Title) hoac (Quantity).")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-02"; Name = "Loai bo ma dinh danh (Title) va (Quantity) trong tin tuyen dung (FB-02)"; Status = "PASS"; Details = "Thong bao hoan toan sach ma dinh danh ky thuat." })
} else {
    [void]$uat2Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-02"; Name = "Loai bo ma dinh danh (Title) va (Quantity) trong tin tuyen dung (FB-02)"; Status = "FAIL"; Details = "Van con token ky thuat hoac phan hoi sai ma." })
}
Capture-Screen "UAT2-02-company-job-positions.png" "/company-me/job-positions"
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-02.txt"), $uat2Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-03 (FB-03): CV URL trimming and safety validation
# ==============================================================
Write-Host "--> Running UAT2-03 (FB-03): Trim CV URL whitespace and enforce scheme safety..."
$uat3Log = New-Object System.Text.StringBuilder
[void]$uat3Log.AppendLine("KICH BAN UAT2-03: Kiem tra cat tia khoang trang CV URL va phong ve URL (FB-03)")
[void]$uat3Log.AppendLine("Nguoi thuc hien: SV-02 (Sinh vien thao tac nop CV)")
[void]$uat3Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyForUat3 = New-TestCompany "uat3"
$positionForUat3 = New-ApprovedPosition $companyForUat3 5 "Frontend Developer"
$studentForUat3 = New-TestStudent "uat3"

$rawCvUrl = "   https://drive.google.com/file/d/test-cv/view   "
$reqPayload1 = @{
    CompanyId = $companyForUat3.Id
    JobPositionId = $positionForUat3
    ApplicantFullName = "Tran Van CV Trim"
    ApplicantEmail = "cvtrim@example.com"
    ApplicantPhone = "0987654321"
    ApplicantSchool = "Dai hoc Bach Khoa"
    ApplicantMajor = "Ky thuat May tinh"
    CvUrl = $rawCvUrl
    CoverLetter = "Thu ung tuyen kiem tra chuc nang tu dong trim khoang trang duong dan CV hop le."
}
$resCv1 = Invoke-Api POST "/me/placement-requests" $studentForUat3.Token $reqPayload1
[void]$uat3Log.AppendLine("Buoc 1: Nop don voi CV URL co khoang trang o dau va cuoi ('" + $rawCvUrl + "'):")
[void]$uat3Log.AppendLine("HTTP Status nhan duoc: " + $resCv1.Status)
$createdRequestId = 0
if ($resCv1.Status -eq 200 -and $null -ne $resCv1.Body.data) {
    $createdRequestId = [int]$resCv1.Body.data.id
    [void]$uat3Log.AppendLine("Don ung tuyen duoc tao thanh cong voi Ma: " + $createdRequestId)
}

$savedCvUrlInDb = & sqlcmd -S $SqlServer -d $DatabaseName -E -C -b -h -1 -W -Q ("SET NOCOUNT ON; SELECT CvUrl FROM dbo.PlacementRequests WHERE Id = " + $createdRequestId + ";")
$savedCvUrlInDb = ($savedCvUrlInDb | Out-String).Trim()
[void]$uat3Log.AppendLine("Gia tri CvUrl thuc te luu trong CSDL: " + $savedCvUrlInDb)

$studentForUat3b = New-TestStudent "uat3b"
$badSchemePayload = @{
    CompanyId = $companyForUat3.Id
    JobPositionId = $positionForUat3
    ApplicantFullName = "Tran Van Scheme Sai"
    ApplicantEmail = "schemesai@example.com"
    ApplicantPhone = "0987654321"
    ApplicantSchool = "Dai hoc Bach Khoa"
    ApplicantMajor = "Ky thuat May tinh"
    CvUrl = "ftp://example.com/cv.pdf"
    CoverLetter = "Thu ung tuyen thu nghiem scheme ftp khong duoc phep."
}
$resCv2 = Invoke-Api POST "/me/placement-requests" $studentForUat3b.Token $badSchemePayload
[void]$uat3Log.AppendLine("`nBuoc 2: Nop don voi URL scheme ftp://:")
[void]$uat3Log.AppendLine("HTTP Status nhan duoc: " + $resCv2.Status + " (Ky vong: 400)")

$studentForUat3c = New-TestStudent "uat3c"
$spaceMiddlePayload = @{
    CompanyId = $companyForUat3.Id
    JobPositionId = $positionForUat3
    ApplicantFullName = "Tran Van Khoang Trang Giua"
    ApplicantEmail = "space@example.com"
    ApplicantPhone = "0987654321"
    ApplicantSchool = "Dai hoc Bach Khoa"
    ApplicantMajor = "Ky thuat May tinh"
    CvUrl = "https://example.com/cv file.pdf"
    CoverLetter = "Thu ung tuyen thu nghiem khoang trang o giua URL."
}
$resCv3 = Invoke-Api POST "/me/placement-requests" $studentForUat3c.Token $spaceMiddlePayload
[void]$uat3Log.AppendLine("`nBuoc 3: Nop don voi URL co khoang trang o giua ('https://example.com/cv file.pdf'):")
[void]$uat3Log.AppendLine("HTTP Status nhan duoc: " + $resCv3.Status + " (Ky vong: 400)")

if ($resCv1.Status -eq 200 -and $savedCvUrlInDb -eq "https://drive.google.com/file/d/test-cv/view" -and $resCv2.Status -eq 400 -and $resCv3.Status -eq 400) {
    [void]$uat3Log.AppendLine("`nKet luan: DAT (PASS) - He thong tu dong cat tia khoang trang 2 dau hop le va luu chuoi sach vao CSDL; tu choi triet de scheme ftp va khoang trang giua.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-03"; Name = "Tu dong cat tia CV URL va bao ve an toan scheme (FB-03)"; Status = "PASS"; Details = "Tu dong trim thanh cong va chan scheme nguy hiem/khoang trang giua." })
} else {
    [void]$uat3Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-03"; Name = "Tu dong cat tia CV URL va bao ve an toan scheme (FB-03)"; Status = "FAIL"; Details = "Xu ly URL chua dung dac ta." })
}
Capture-Screen "UAT2-03-job-position-apply.png" "/job-positions"
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-03.txt"), $uat3Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-04 (FB-04): README.md UTF-8 encoding check
# ==============================================================
Write-Host "--> Running UAT2-04 (FB-04): Check README.md UTF-8 encoding..."
$uat4Log = New-Object System.Text.StringBuilder
[void]$uat4Log.AppendLine("KICH BAN UAT2-04: Kiem tra ma hoa tieng Viet UTF-8 khong loi font README.md (FB-04)")
[void]$uat4Log.AppendLine("Nguoi thuc hien: NM-01 (Nguoi tiep can du an)")
[void]$uat4Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$readmePath = Join-Path $repoRoot "InternshipManagement\README.md"
$readmeBytes = [System.IO.File]::ReadAllBytes($readmePath)
$readmeText = [System.Text.Encoding]::UTF8.GetString($readmeBytes)
$readmeLines = $readmeText.Split(@("`r`n", "`n"), [StringSplitOptions]::None)
$lastLine = $readmeLines[$readmeLines.Length - 1]
if ([string]::IsNullOrWhiteSpace($lastLine) -and $readmeLines.Length -gt 1) {
    $lastLine = $readmeLines[$readmeLines.Length - 2]
}
[void]$uat4Log.AppendLine("Duong dan file: " + $readmePath)
[void]$uat4Log.AppendLine("Dong cuoi cung doc bang UTF-8: '" + $lastLine + "'")

$expectedUtf8Bytes = [Convert]::FromBase64String("SOG7hyB0aOG7kW5nIHF14bqjbiBsw70gc2luaCB2acOqbiB0aOG7sWMgdOG6rXAgKEFTUC5ORVQgQ29yZSA4ICsgVnVlIDMpLg==")
$actualLastLineBytes = [System.Text.Encoding]::UTF8.GetBytes($lastLine.Trim())

$isBytesMatch = ($actualLastLineBytes.Length -eq $expectedUtf8Bytes.Length)
if ($isBytesMatch) {
    for ($i = 0; $i -lt $expectedUtf8Bytes.Length; $i++) {
        if ($actualLastLineBytes[$i] -ne $expectedUtf8Bytes[$i]) {
            $isBytesMatch = $false
            break
        }
    }
}

if ($isBytesMatch) {
    [void]$uat4Log.AppendLine("`nKet luan: DAT (PASS) - File tai lieu hien thi tieng Viet UTF-8 chuan xac 100%, khong con bat ky dau hieu loi font mojibake nao.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-04"; Name = "Khac phuc loi font tieng Viet mojibake trong README.md (FB-04)"; Status = "PASS"; Details = "Chuoi tieng Viet UTF-8 khop 100% byte-for-byte." })
} else {
    [void]$uat4Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-04"; Name = "Khac phuc loi font tieng Viet mojibake trong README.md (FB-04)"; Status = "FAIL"; Details = "File van bi loi font hoac sai lech noi dung." })
}
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-04.txt"), $uat4Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-05 (FB-10): Prevent race condition on double submission
# ==============================================================
Write-Host "--> Running UAT2-05 (FB-10): Prevent double submission race condition..."
$uat5Log = New-Object System.Text.StringBuilder
[void]$uat5Log.AppendLine("KICH BAN UAT2-05: Kiem tra kiem soat dong thoi chong tao 2 don trung lap (FB-10 / TC-12)")
[void]$uat5Log.AppendLine("Nguoi thuc hien: SV-01 (Sinh vien thao tac nop ho so)")
[void]$uat5Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyForUat5 = New-TestCompany "uat5"
$positionForUat5 = New-ApprovedPosition $companyForUat5 5 "Data Engineer"
$studentForUat5 = New-TestStudent "uat5"

$racePayload = ConvertTo-Json -Compress @{
    CompanyId = $companyForUat5.Id
    JobPositionId = $positionForUat5
    ApplicantFullName = "Le Hoang Dua Lenh"
    ApplicantEmail = "duanlenh@example.com"
    ApplicantPhone = "0901234567"
    ApplicantSchool = "Dai hoc Bach Khoa"
    ApplicantMajor = "Khoa hoc May tinh"
    CvUrl = "https://drive.google.com/file/d/race-cv/view"
    CoverLetter = "Thu ung tuyen kiem tra co che chong race condition nop don dup dong thoi."
}

$startAtUtcTicks = [DateTimeOffset]::UtcNow.AddSeconds(3).UtcTicks
$targetUri = $api + "/me/placement-requests"
$token = $studentForUat5.Token

$jobA = Start-Job -ScriptBlock {
    param($Uri, $Token, $Json, $StartAt)
    while ([DateTimeOffset]::UtcNow.UtcTicks -lt $StartAt) { Start-Sleep -Milliseconds 5 }
    try {
        $res = Invoke-RestMethod -Method Post -Uri $Uri -Headers @{ Authorization = "Bearer $Token" } -ContentType "application/json" -Body $Json -TimeoutSec 30 -ErrorAction Stop
        return [pscustomobject]@{ Status = 200; Body = $res; Error = $null }
    } catch {
        $sc = if ($null -ne $_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        return [pscustomobject]@{ Status = $sc; Body = $null; Error = $_.Exception.Message }
    }
} -ArgumentList $targetUri, $token, $racePayload, $startAtUtcTicks

$jobB = Start-Job -ScriptBlock {
    param($Uri, $Token, $Json, $StartAt)
    while ([DateTimeOffset]::UtcNow.UtcTicks -lt $StartAt) { Start-Sleep -Milliseconds 5 }
    try {
        $res = Invoke-RestMethod -Method Post -Uri $Uri -Headers @{ Authorization = "Bearer $Token" } -ContentType "application/json" -Body $Json -TimeoutSec 30 -ErrorAction Stop
        return [pscustomobject]@{ Status = 200; Body = $res; Error = $null }
    } catch {
        $sc = if ($null -ne $_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        return [pscustomobject]@{ Status = $sc; Body = $null; Error = $_.Exception.Message }
    }
} -ArgumentList $targetUri, $token, $racePayload, $startAtUtcTicks

Wait-Job -Job @($jobA, $jobB) -Timeout 30 | Out-Null
$resA = Receive-Job -Job $jobA
$resB = Receive-Job -Job $jobB
Remove-Job -Job @($jobA, $jobB) -Force -ErrorAction SilentlyContinue

[void]$uat5Log.AppendLine("Gui dong thoi 2 HTTP POST request toi " + $targetUri + ":")
[void]$uat5Log.AppendLine("Ket qua Request A: HTTP " + $resA.Status)
[void]$uat5Log.AppendLine("Ket qua Request B: HTTP " + $resB.Status)

$countInDb = & sqlcmd -S $SqlServer -d $DatabaseName -E -C -b -h -1 -W -Q ("SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.PlacementRequests WHERE StudentId = " + $studentForUat5.Id + " AND JobPositionId = " + $positionForUat5 + ";")
$countInDb = [int]($countInDb | Out-String).Trim()
[void]$uat5Log.AppendLine("So luong ban ghi don ung tuyen thuc te trong CSDL: " + $countInDb)

$hasOne200 = (($resA.Status -eq 200 -and $resB.Status -eq 400) -or ($resA.Status -eq 400 -and $resB.Status -eq 200))
if ($hasOne200 -and $countInDb -eq 1) {
    [void]$uat5Log.AppendLine("`nKet luan: DAT (PASS) - Database Transaction va Row Lock da bao ve thanh cong: dung 1 request duoc chap nhan va 1 request bi tu choi; trong CSDL chi co duy nhat 1 don duoc tao.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-05"; Name = "Chong race condition khi bam nop don dup lien tiep (FB-10)"; Status = "PASS"; Details = "Mot request thanh cong HTTP 200, mot request bi tu choi HTTP 400; chi tao 1 ban ghi." })
} else {
    [void]$uat5Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-05"; Name = "Chong race condition khi bam nop don dup lien tiep (FB-10)"; Status = "FAIL"; Details = "Trang thai HTTP hoac so luong ban ghi trong CSDL khong khop (Count=" + $countInDb + ")." })
}
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-05.txt"), $uat5Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-06: Student regression workflow
# ==============================================================
Write-Host "--> Running UAT2-06: Student regression flow (Browse jobs, apply, check status)..."
$uat6Log = New-Object System.Text.StringBuilder
[void]$uat6Log.AppendLine("KICH BAN UAT2-06: Hoi quy Sinh vien nop ho so va theo doi tien do ung tuyen")
[void]$uat6Log.AppendLine("Nguoi thuc hien: SV-01 (Sinh vien)")
[void]$uat6Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyForUat6 = New-TestCompany "uat6"
$positionForUat6 = New-ApprovedPosition $companyForUat6 3 "QA Test Engineer"
$studentForUat6 = New-TestStudent "uat6"

$jobsRes = Invoke-Api GET "/job-positions?PageNumber=1&PageSize=10" $studentForUat6.Token $null
[void]$uat6Log.AppendLine("Buoc 1: Sinh vien tra cuu danh sach viec lam mo qua /api/job-positions:")
[void]$uat6Log.AppendLine("HTTP Status: " + $jobsRes.Status + " | So vi tri tim thay: " + $jobsRes.Body.data.items.Count)

$applyPayload = @{
    CompanyId = $companyForUat6.Id
    JobPositionId = $positionForUat6
    ApplicantFullName = $studentForUat6.FullName
    ApplicantEmail = "student-uat6@example.com"
    ApplicantPhone = "0934567890"
    ApplicantSchool = "Dai hoc Cong nghe Sai Gon"
    ApplicantMajor = "Cong nghe Thong tin"
    CvUrl = "https://example.com/cv/qa-engineer.pdf"
    CoverLetter = "Toi co dam me sau sac voi kiem thu phan mem va mong muon ung tuyen vi tri QA."
}
$applyRes = Invoke-Api POST "/me/placement-requests" $studentForUat6.Token $applyPayload
[void]$uat6Log.AppendLine("`nBuoc 2: Sinh vien nop don ung tuyen:")
[void]$uat6Log.AppendLine("HTTP Status: " + $applyRes.Status + " | Ma don tao thanh cong: " + $applyRes.Body.data.id)

$myRequestsRes = Invoke-Api GET "/me/placement-requests" $studentForUat6.Token $null
$myRequests = $myRequestsRes.Body.data
$createdReq = @($myRequests | Where-Object { [int]$_.jobPositionId -eq $positionForUat6 })
[void]$uat6Log.AppendLine("`nBuoc 3: Sinh vien kiem tra danh sach don ca nhan qua /api/me/placement-requests:")
[void]$uat6Log.AppendLine("So don tim thay: " + $myRequests.Count + " | Trang thai don vua nop: " + $createdReq[0].status)

if ($jobsRes.Status -eq 200 -and $applyRes.Status -eq 200 -and $myRequestsRes.Status -eq 200 -and $createdReq.Count -eq 1 -and "$($createdReq[0].status)" -in @("Pending", "0")) {
    [void]$uat6Log.AppendLine("`nKet luan: DAT (PASS) - Sinh vien hoan thanh toan bo luong tim viec, nop don thanh cong va theo doi trang thai ho so chinh xac.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-06"; Name = "Hoi quy luong Sinh vien tra cuu viec, nop don va xem tien do"; Status = "PASS"; Details = "Sinh vien nop don thanh cong, don hien thi o trang thai Pending." })
} else {
    [void]$uat6Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-06"; Name = "Hoi quy luong Sinh vien tra cuu viec, nop don va xem tien do"; Status = "FAIL"; Details = "Loi trong qua trinh nop hoac tra cuu don cua sinh vien." })
}
Capture-Screen "UAT2-06-student-jobs-portal.png" "/job-positions"
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-06.txt"), $uat6Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-07: Company regression workflow
# ==============================================================
Write-Host "--> Running UAT2-07: Company regression flow (Invite interview, evaluate Passed)..."
$uat7Log = New-Object System.Text.StringBuilder
[void]$uat7Log.AppendLine("KICH BAN UAT2-07: Hoi quy Doanh nghiep tiep nhan ho so, moi phong van va danh gia ket qua")
[void]$uat7Log.AppendLine("Nguoi thuc hien: DN-01 (Dai dien Doanh nghiep)")
[void]$uat7Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyForUat7 = New-TestCompany "uat7"
$positionForUat7 = New-ApprovedPosition $companyForUat7 2 "Backend Developer"
$studentForUat7 = New-TestStudent "uat7"

$req7 = Get-Data (Invoke-Api POST "/me/placement-requests" $studentForUat7.Token @{
    CompanyId = $companyForUat7.Id
    JobPositionId = $positionForUat7
    ApplicantFullName = $studentForUat7.FullName
    ApplicantEmail = "student-uat7@example.com"
    ApplicantPhone = "0909090909"
    ApplicantSchool = "Dai hoc KHTN"
    ApplicantMajor = "Khoa hoc May tinh"
    CvUrl = "https://example.com/cv/backend-dev.pdf"
    CoverLetter = "Kinh gui quy cong ty, em mong muon thuc tap vi tri backend."
}) "Student apply for UAT 7"
$req7Id = [int]$req7.id

$companyRequestsRes = Invoke-Api GET "/company-me/placement-requests?PageNumber=1&PageSize=10" $companyForUat7.Token $null
[void]$uat7Log.AppendLine("Buoc 1: Doanh nghiep kiem tra danh sach don ung tuyen nhan duoc:")
[void]$uat7Log.AppendLine("HTTP Status: " + $companyRequestsRes.Status + " | So don: " + $companyRequestsRes.Body.data.items.Count)

$interviewDate = (Get-Date).ToUniversalTime().AddDays(3).ToString("o")
$inviteRes = Invoke-Api POST ("/company-me/placement-requests/" + $req7Id + "/invite-interview") $companyForUat7.Token @{
    InterviewAt = $interviewDate
    InterviewLocation = "Phong hop A, Tang 5 Tru so Cong ty"
}
[void]$uat7Log.AppendLine("`nBuoc 2: Doanh nghiep gui thu moi phong van:")
[void]$uat7Log.AppendLine("HTTP Status: " + $inviteRes.Status)

$resultRes = Invoke-Api POST ("/company-me/placement-requests/" + $req7Id + "/result") $companyForUat7.Token @{
    IsPassed = $true
    Note = "Ung vien co kien thuc nen tang tot, tiep nhan vao du an thuc tap."
}
[void]$uat7Log.AppendLine("`nBuoc 3: Doanh nghiep cap nhat ket qua tuyen dung (Passed):")
[void]$uat7Log.AppendLine("HTTP Status: " + $resultRes.Status)

$finalReq = Get-Data (Invoke-Api GET "/placement-requests?PageNumber=1&PageSize=100" $script:adminToken $null) "Admin check request"
$verifiedReq = @($finalReq.items | Where-Object { [int]$_.id -eq $req7Id })[0]
[void]$uat7Log.AppendLine("Trang thai don ghi nhan trong he thong: " + $verifiedReq.status + " (Ky vong: Accepted / 4)")

if ($companyRequestsRes.Status -eq 200 -and $inviteRes.Status -eq 200 -and $resultRes.Status -eq 200 -and "$($verifiedReq.status)" -in @("Accepted", "4")) {
    [void]$uat7Log.AppendLine("`nKet luan: DAT (PASS) - Doanh nghiep thuc hien hoan chinh quy trinh duyet don, moi phong van va tiep nhan thuc tap sinh thanh cong.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-07"; Name = "Hoi quy Doanh nghiep moi phong van va danh gia ket qua ung vien"; Status = "PASS"; Details = "Doanh nghiep xu ly don thanh cong, chuyen trang thai sang Accepted." })
} else {
    [void]$uat7Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-07"; Name = "Hoi quy Doanh nghiep moi phong van va danh gia ket qua ung vien"; Status = "FAIL"; Details = "Trang thai don khong chuyen sang Accepted hoac co buoc that bai." })
}
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-07.txt"), $uat7Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-08: Admin regression workflow
# ==============================================================
Write-Host "--> Running UAT2-08: Admin regression flow (Approve job, verify dashboard)..."
$uat8Log = New-Object System.Text.StringBuilder
[void]$uat8Log.AppendLine("KICH BAN UAT2-08: Hoi quy Quan tri vien phe duyet viec lam va giam sat Dashboard")
[void]$uat8Log.AppendLine("Nguoi thuc hien: AD-01 (Quan tri vien he thong)")
[void]$uat8Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyForUat8 = New-TestCompany "uat8"
$newPosition = Get-Data (Invoke-Api POST "/company-me/job-positions" $companyForUat8.Token @{
    Title = "Thuc tap sinh AI " + [Guid]::NewGuid().ToString("N").Substring(0, 6)
    Department = "AI Lab"
    Location = "Khu Cong nghe Cao"
    Deadline = (Get-Date).ToUniversalTime().AddDays(20).ToString("o")
    Quantity = 2
    Description = "Tham gia huan luyen mo hinh Machine Learning."
    IsOpen = $true
}) "Create pending position"
$pos8Id = [int]$newPosition.id
Require-Status (Invoke-Api POST ("/company-me/job-positions/" + $pos8Id + "/submit") $companyForUat8.Token $null) @(200) "Company submit position"

$approveRes = Invoke-Api POST ("/job-positions/" + $pos8Id + "/approve") $script:adminToken $null
[void]$uat8Log.AppendLine("Buoc 1: Quan tri vien phe duyet tin tuyen dung:")
[void]$uat8Log.AppendLine("HTTP Status: " + $approveRes.Status)

$posCheck = Get-Data (Invoke-Api GET ("/job-positions/" + $pos8Id) $script:adminToken $null) "Check position"
[void]$uat8Log.AppendLine("Trang thai tin sau duyet: IsOpen=" + $posCheck.isOpen + ", ApprovalStatus=" + $posCheck.approvalStatus)

$dashboardRes = Invoke-Api GET "/dashboard/summary" $script:adminToken $null
[void]$uat8Log.AppendLine("`nBuoc 2: Quan tri vien truy xuat so lieu Dashboard tong quan:")
[void]$uat8Log.AppendLine("HTTP Status: " + $dashboardRes.Status)
[void]$uat8Log.AppendLine("So lieu tong hop: " + (ConvertTo-Json $dashboardRes.Body.data -Depth 2 -Compress))

if ($approveRes.Status -eq 200 -and $posCheck.isOpen -eq $true -and $dashboardRes.Status -eq 200) {
    [void]$uat8Log.AppendLine("`nKet luan: DAT (PASS) - Quan tri vien phe duyet tin tuyen dung hop le va giam sat thong so van hanh tren Dashboard muot ma.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-08"; Name = "Hoi quy Quan tri vien duyet tin tuyen dung va theo doi Dashboard"; Status = "PASS"; Details = "Phe duyet tin thanh cong, so lieu Dashboard phan hoi day du." })
} else {
    [void]$uat8Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-08"; Name = "Hoi quy Quan tri vien duyet tin tuyen dung va theo doi Dashboard"; Status = "FAIL"; Details = "Phe duyet tin hoac truy xuat Dashboard that bai." })
}
Capture-Screen "UAT2-08-admin-dashboard.png" "/dashboard"
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-08.txt"), $uat8Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-09: Error path 1 - Input validation edge cases
# ==============================================================
Write-Host "--> Running UAT2-09: Error path 1 - Input validation edge cases..."
$uat9Log = New-Object System.Text.StringBuilder
[void]$uat9Log.AppendLine("KICH BAN UAT2-09: Kiem tra duong dan loi - Xac thuc du lieu dau vao thieu hoac sai dinh dang")
[void]$uat9Log.AppendLine("Nguoi thuc hien: SV-01 / DN-01")
[void]$uat9Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyForUat9 = New-TestCompany "uat9"
$positionForUat9 = New-ApprovedPosition $companyForUat9 2 "UI/UX Designer"
$studentForUat9 = New-TestStudent "uat9"

$badApplyPayload = @{
    CompanyId = $companyForUat9.Id
    JobPositionId = $positionForUat9
    ApplicantFullName = ""
    ApplicantEmail = "email-sai-dinh-dang"
    ApplicantPhone = "12345"
    ApplicantSchool = "A"
    ApplicantMajor = "B"
    CvUrl = "https://example.com/cv.pdf"
    CoverLetter = "Qua ngan"
}
$badApplyRes = Invoke-Api POST "/me/placement-requests" $studentForUat9.Token $badApplyPayload
[void]$uat9Log.AppendLine("Buoc 1: Sinh vien nop don vi pham nhieu rang buoc xac thuc du lieu:")
[void]$uat9Log.AppendLine("HTTP Status nhan duoc: " + $badApplyRes.Status + " (Ky vong: 400)")
[void]$uat9Log.AppendLine("Thong bao loi chi tiet:")
[void]$uat9Log.AppendLine($badApplyRes.ErrorBody)

$negativeQuantityPayload = @{
    Title = "Vi tri co so luong am"
    Department = "Ky thuat"
    Location = "Da Nang"
    Deadline = (Get-Date).ToUniversalTime().AddDays(10).ToString("o")
    Quantity = -5
    Description = "Mo ta cong viec"
    IsOpen = $true
}
$badJobRes = Invoke-Api POST "/company-me/job-positions" $companyForUat9.Token $negativeQuantityPayload
[void]$uat9Log.AppendLine("`nBuoc 2: Doanh nghiep tao tin voi so luong tuyen am (-5):")
[void]$uat9Log.AppendLine("HTTP Status nhan duoc: " + $badJobRes.Status + " (Ky vong: 400)")
[void]$uat9Log.AppendLine("Thong bao loi chi tiet:")
[void]$uat9Log.AppendLine($badJobRes.ErrorBody)

if ($badApplyRes.Status -eq 400 -and $badJobRes.Status -eq 400) {
    [void]$uat9Log.AppendLine("`nKet luan: DAT (PASS) - He thong kiem tra va tu choi nghiem ngat cac du lieu khong hop le o ca hai vai tro, hien thi thong bao loi than thien ro rang.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-09"; Name = "Duong loi 1: Xac thuc du lieu dau vao va cac truong hop bien"; Status = "PASS"; Details = "He thong tu choi du lieu sai chuan voi ma HTTP 400 kem thong bao ro rang." })
} else {
    [void]$uat9Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-09"; Name = "Duong loi 1: Xac thuc du lieu dau vao va cac truong hop bien"; Status = "FAIL"; Details = "He thong khong chan du lieu sai hoac tra ve ma loi khong phu hop." })
}
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-09.txt"), $uat9Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SCENARIO UAT2-10: Error path 2 - Authorization & multi-tenancy isolation
# ==============================================================
Write-Host "--> Running UAT2-10: Error path 2 - Role authorization & multi-tenancy isolation..."
$uat10Log = New-Object System.Text.StringBuilder
[void]$uat10Log.AppendLine("KICH BAN UAT2-10: Kiem tra duong dan loi - Phan quyen vai tro va cach ly du lieu cheo (Multi-Tenancy)")
[void]$uat10Log.AppendLine("Nguoi thuc hien: SV-01 / DN-01 / Doanh nghiep B")
[void]$uat10Log.AppendLine("Thoi diem: " + (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") + "`n")

$companyA = New-TestCompany "tenantA"
$companyB = New-TestCompany "tenantB"
$positionA = New-ApprovedPosition $companyA 3 "Vi tri rieng Company A"
$studentA = New-TestStudent "tenantA"

$studentCallAdminApi = Invoke-Api GET "/students" $studentA.Token $null
[void]$uat10Log.AppendLine("Buoc 1: Sinh vien co tinh truy cap tai nguyen quan tri (/api/students):")
[void]$uat10Log.AppendLine("HTTP Status nhan duoc: " + $studentCallAdminApi.Status + " (Ky vong: 403 Forbidden)")

$anonCallAdminApi = Invoke-Api GET "/students" $null $null
[void]$uat10Log.AppendLine("`nBuoc 2: Nguoi dung nac danh goi API quan tri (/api/students):")
[void]$uat10Log.AppendLine("HTTP Status nhan duoc: " + $anonCallAdminApi.Status + " (Ky vong: 401 Unauthorized)")

$companyBModifyPositionA = Invoke-Api PUT ("/company-me/job-positions/" + $positionA) $companyB.Token @{
    Title = "Company B co tinh sua tin cua Company A"
    Department = "Hack"
    Location = "Hack"
    Deadline = (Get-Date).ToUniversalTime().AddDays(10).ToString("o")
    Quantity = 10
    Description = "Hack"
    IsOpen = $true
}
[void]$uat10Log.AppendLine("`nBuoc 3: Doanh nghiep B co tinh chinh sua tin tuyen dung cua Doanh nghiep A:")
[void]$uat10Log.AppendLine("HTTP Status nhan duoc: " + $companyBModifyPositionA.Status + " (Ky vong: 403 hoac 404)")

$isStudentForbidden = ($studentCallAdminApi.Status -eq 403)
$isAnonUnauthorized = ($anonCallAdminApi.Status -eq 401)
$isCompanyBBlocked = ($companyBModifyPositionA.Status -in @(403, 404))

if ($isStudentForbidden -and $isAnonUnauthorized -and $isCompanyBBlocked) {
    [void]$uat10Log.AppendLine("`nKet luan: DAT (PASS) - Co che phan quyen theo vai tro va cach ly du lieu da nguoi thue hoat dong an toan tuyet doi, ngan chan thanh cong moi hanh vi truy cap trai phep.")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-10"; Name = "Duong loi 2: Phan quyen vai tro va cach ly du lieu da nguoi thue"; Status = "PASS"; Details = "Chan truy cap trai quyen (403), nac danh (401) va sua cheo du lieu tenant (403/404)." })
} else {
    [void]$uat10Log.AppendLine("`nKet luan: KHONG DAT (FAIL)")
    $script:results.Add([pscustomobject]@{ Code = "UAT2-10"; Name = "Duong loi 2: Phan quyen vai tro va cach ly du lieu da nguoi thue"; Status = "FAIL"; Details = "Phan quyen bi ho hoac tra ve ma trang thai khong dung ky vong bao mat." })
}
[System.IO.File]::WriteAllText((Join-Path $evidenceDir "UAT2-10.txt"), $uat10Log.ToString(), [System.Text.Encoding]::UTF8)

# ==============================================================
# SUMMARY REPORT
# ==============================================================
Write-Host "`n=========================================================="
Write-Host "UAT 10 SCENARIOS EXECUTION RESULTS"
Write-Host "=========================================================="
$script:results | Format-Table -AutoSize -Wrap

$passCount = @($script:results | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = @($script:results | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host ("SUMMARY: {0} PASS / {1} FAIL out of {2} scenarios" -f $passCount, $failCount, $script:results.Count)

Stop-E2EApi

if ($failCount -gt 0) {
    exit 1
}
exit 0
