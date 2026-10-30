[CmdletBinding()]
param(
    [string]$BaseUrl = "http://127.0.0.1:5018",
    [ValidateRange(10, 100)]
    [int]$ConcurrencyIterations = 10,
    [ValidateRange(50, 100)]
    [int]$RaceOneIterations = 50,
    [string]$ApiLogPath,
    [string]$SqlServer = "localhost"
)

$ErrorActionPreference = "Stop"
$api = $BaseUrl.TrimEnd("/") + "/api"
$databaseName = "StudentInternshipMgmtDb_E2E"
if ([string]::IsNullOrWhiteSpace($ApiLogPath)) {
    $ApiLogPath = Join-Path ([System.IO.Path]::GetTempPath()) ("studentinternshipmgmt-step23-api-{0}.log" -f [Guid]::NewGuid().ToString("N"))
}
$ApiLogPath = [System.IO.Path]::GetFullPath($ApiLogPath)
$backendRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$script:results = New-Object System.Collections.Generic.List[object]
$script:adminToken = $null
$script:adminRefreshToken = $null
$script:adminTokenExpiresAt = [DateTime]::MinValue
$script:sequence = [Guid]::NewGuid().ToString("N").Substring(0, 8)
$script:apiJob = $null
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
    $upper = New-CryptoRandomString 1 "ABCDEFGHJKLMNPQRSTUVWXYZ"
    $lower = New-CryptoRandomString 1 "abcdefghijkmnopqrstuvwxyz"
    $digit = New-CryptoRandomString 1 "23456789"
    $remainder = New-CryptoRandomString 29 "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"
    $password = $upper + $lower + $digit + $remainder
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
    if ($script:apiPort -gt 0) {
        $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $script:apiPort -ErrorAction SilentlyContinue)
        foreach ($listener in $listeners) {
            $process = Get-CimInstance Win32_Process -Filter "ProcessId = $($listener.OwningProcess)"
            if ($null -ne $process -and
                ($process.Name -eq "StudentInternshipMgmt.Api.exe" -or
                    ($process.Name -eq "dotnet.exe" -and $process.CommandLine -like "*StudentInternshipMgmt.Api*"))) {
                Stop-Process -Id $process.ProcessId -Force
            }
            elseif ($null -ne $process) {
                Write-Warning "API port $script:apiPort is still owned by unexpected process '$($process.Name)' (PID $($process.ProcessId)); it was not stopped."
            }
        }
        $script:apiPort = 0
    }
    $script:e2eAdminPassword = $null
    $script:adminPassword = $null
    Restore-E2EProcessEnvironment
}

function Stop-ExistingE2EApi {
    $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $script:apiPort -ErrorAction SilentlyContinue)
    foreach ($listener in $listeners) {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId = $($listener.OwningProcess)"
        $isApi = $null -ne $process -and (
            $process.Name -eq "StudentInternshipMgmt.Api.exe" -or
            ($process.Name -eq "dotnet.exe" -and $process.CommandLine -like "*StudentInternshipMgmt.Api*"))
        if (-not $isApi) {
            $name = if ($null -eq $process) { "unknown" } else { $process.Name }
            throw "Safety stop: port $script:apiPort is occupied by unexpected process '$name'; no process or database was changed."
        }
        Stop-Process -Id $process.ProcessId -Force
    }
    $deadline = (Get-Date).AddSeconds(10)
    while ((Get-NetTCPConnection -State Listen -LocalPort $script:apiPort -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 200
    }
    if (Get-NetTCPConnection -State Listen -LocalPort $script:apiPort -ErrorAction SilentlyContinue) {
        throw "API port $script:apiPort did not become available; the E2E database was not changed."
    }
}

function Start-E2EApi {
    if (Test-Path -LiteralPath $ApiLogPath) {
        throw "Refusing to overwrite the existing API log file '$ApiLogPath'."
    }
    Set-Content -LiteralPath $ApiLogPath -Value "" -Encoding UTF8
    Write-Host "API log: $ApiLogPath"
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
            throw "E2E API process ended before becoming ready. Job output: $($jobOutput -join ' '); API log: $($logTail -join ' | ')"
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
        throw "E2E API did not become ready within 90 seconds. API log: $($logTail -join ' | ')"
    }
}

trap {
    $failure = $_
    Stop-E2EApi
    Write-Error -Message ("E2E runner failed: " + $failure.ToString()) -ErrorAction Continue
    exit 1
}

function Set-AdminSession {
    param($LoginData)
    $script:adminToken = $LoginData.accessToken
    $script:adminRefreshToken = $LoginData.refreshToken
    $accessMinutes = 15
    if (-not [int]::TryParse($env:Jwt__AccessTokenExpiryMinutes, [ref]$accessMinutes) -or $accessMinutes -le 0) {
        $accessMinutes = 15
    }
    $script:adminTokenExpiresAt = (Get-Date).AddMinutes($accessMinutes)
}

function Add-Result {
    param([string]$Scenario, [string]$Status, [string]$Actual)
    $script:results.Add([pscustomobject]@{
        Scenario = $Scenario
        Status = $Status
        Actual = $Actual
    })
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Token,
        [object]$Body
    )

    if (-not [string]::IsNullOrWhiteSpace($Token) -and
        $Token -eq $script:adminToken -and
        (Get-Date) -ge $script:adminTokenExpiresAt.AddMinutes(-1)) {
        if ([string]::IsNullOrWhiteSpace($script:adminRefreshToken)) {
            throw "Admin access token expired and no refresh token is available."
        }
        try {
            $refreshBody = ConvertTo-Json -InputObject @{ RefreshToken = $script:adminRefreshToken } -Compress
            $refreshed = Invoke-RestMethod -Method Post -Uri ($api + "/auth/refresh") -ContentType "application/json" -Body $refreshBody -TimeoutSec 60 -ErrorAction Stop
        }
        catch {
            throw "Admin access-token refresh failed: $($_.Exception.Message)"
        }
        if ($null -eq $refreshed.data -or [string]::IsNullOrWhiteSpace($refreshed.data.accessToken)) {
            throw "Admin access-token refresh returned no token."
        }
        Set-AdminSession $refreshed.data
        $Token = $script:adminToken
    }

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
        return [pscustomobject]@{ Status = 200; Body = $response }
    }
    catch {
        $statusCode = 0
        if ($null -ne $_.Exception.Response -and $null -ne $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        return [pscustomobject]@{ Status = $statusCode; Body = $null; Error = $_.Exception.Message }
    }
}

function Require-Status {
    param($Response, [int[]]$Expected, [string]$Operation)
    if ($Expected -notcontains [int]$Response.Status) {
        $detail = $Response.Error
        if ([string]::IsNullOrWhiteSpace($detail)) {
            $detail = "HTTP $($Response.Status)"
        }
        throw "$Operation expected HTTP $($Expected -join '/') but got $($Response.Status): $detail"
    }
}

function Get-Data {
    param($Response, [string]$Operation)
    Require-Status $Response @(200, 201) $Operation
    if ($null -eq $Response.Body -or $null -eq $Response.Body.data) {
        throw "$Operation returned no ApiResponse.data."
    }
    return $Response.Body.data
}

function Is-Status {
    param($Value, [string]$Name, [int]$Number)
    return ("$Value" -eq $Name -or "$Value" -eq "$Number")
}

function New-TestCompany {
    param([string]$Label)
    $tag = [Guid]::NewGuid().ToString("N").Substring(0, 10)
    $created = Get-Data (Invoke-Api POST "/companies" $script:adminToken @{
        Name = "E2E $Label $tag"
        Address = "E2E isolated test address"
        Industry = "Software"
        ContactPerson = "E2E contact"
        ContactPhone = "0900000001"
        ContactEmail = "e2e-$tag@example.test"
    }) "Create company"
    $companyId = [int]$created.id
    $credentials = Get-Data (Invoke-Api POST "/companies/$companyId/account" $script:adminToken $null) "Create company account"
    $login = Get-Data (Invoke-Api POST "/auth/login" $null @{
        Username = $credentials.username
        Password = $credentials.temporaryPassword
    }) "Login as company"
    return [pscustomobject]@{
        Id = $companyId
        Token = $login.accessToken
        Username = $credentials.username
    }
}

function New-TestStudent {
    param([string]$Label)
    $tag = [Guid]::NewGuid().ToString("N").Substring(0, 10)
    $code = "E2E$tag"
    $created = Get-Data (Invoke-Api POST "/students" $script:adminToken @{
        StudentCode = $code
        FullName = "E2E $Label $tag"
        Email = "student-$tag@example.test"
        PhoneNumber = "0900000001"
        Major = "Software Engineering"
        ClassName = "E2E"
    }) "Create student"
    $studentId = [int]$created.id
    $accounts = Get-Data (Invoke-Api POST "/students/create-accounts" $script:adminToken @{
        StudentIds = @($studentId)
    }) "Create student account"
    if ($accounts.createdAccounts.Count -ne 1) {
        throw "Student account creation did not return exactly one credential."
    }
    $login = Get-Data (Invoke-Api POST "/auth/login" $null @{
        Username = $accounts.createdAccounts[0].studentCode
        Password = $accounts.createdAccounts[0].plainPassword
    }) "Login as student"
    return [pscustomobject]@{
        Id = $studentId
        Code = $code
        Token = $login.accessToken
    }
}

function New-ApprovedPosition {
    param($Company, [int]$Quantity = 4, [string]$Label = "Position")
    $position = Get-Data (Invoke-Api POST "/company-me/job-positions" $Company.Token @{
        Title = "E2E $Label $([Guid]::NewGuid().ToString('N').Substring(0, 8))"
        Department = "E2E"
        Location = "E2E"
        Deadline = (Get-Date).ToUniversalTime().AddDays(30).ToString("o")
        Quantity = $Quantity
        Description = "Position created by run-e2e.ps1"
        IsOpen = $true
    }) "Create company position"
    $positionId = [int]$position.id
    Require-Status (Invoke-Api POST "/company-me/job-positions/$positionId/submit" $Company.Token $null) @(200) "Submit position"
    Require-Status (Invoke-Api POST "/job-positions/$positionId/approve" $script:adminToken $null) @(200) "Approve position"
    return $positionId
}

function New-TestRequest {
    param($Student, $Company, [int]$PositionId)
    $request = Get-Data (Invoke-Api POST "/me/placement-requests" $Student.Token @{
        CompanyId = $Company.Id
        JobPositionId = $PositionId
        ApplicantFullName = "E2E $($Student.Code)"
        ApplicantEmail = "$($Student.Code.ToLowerInvariant())@example.test"
        ApplicantPhone = "0900000001"
        ApplicantSchool = "E2E University"
        ApplicantMajor = "Software Engineering"
        CvUrl = "https://example.test/$($Student.Code)"
        CoverLetter = "Created by repeatable SQL Server E2E test."
        Note = "E2E"
    }) "Submit placement request"
    return [int]$request.id
}

function Get-StudentRequests {
    param($Student)
    return (Get-Data (Invoke-Api GET "/me/placement-requests" $Student.Token $null) "Get student's requests")
}

function Get-AdminRequest {
    param([int]$RequestId)
    $page = Get-Data (Invoke-Api GET "/placement-requests?PageNumber=1&PageSize=1000" $script:adminToken $null) "Get admin request list"
    $items = $page.items
    if ($null -eq $items) { $items = $page.data }
    $found = @($items | Where-Object { [int]$_.id -eq $RequestId } | Select-Object -First 1)
    if ($found.Count -ne 1) {
        throw "Request $RequestId was not found in the Admin list."
    }
    return $found[0]
}

function Set-RequestDueAt {
    param([int]$RequestId, [string]$SqlExpression, [ValidateSet("Pending", "InterviewInvited")][string]$ExpectedStatus)
    if ($RequestId -le 0 -or $SqlExpression -notin @("DATEADD(day, -1, SYSUTCDATETIME())", "DATEADD(day, 1, SYSUTCDATETIME())")) {
        throw "Refusing an unexpected SQL update for DueAt."
    }
    $query = "SET NOCOUNT ON; UPDATE dbo.PlacementRequests SET DueAt = $SqlExpression WHERE Id = $RequestId AND Status = N'$ExpectedStatus'; IF @@ROWCOUNT <> 1 THROW 51000, 'Expected one request in the required E2E status.', 1;"
    & sqlcmd -S $SqlServer -d $databaseName -E -C -b -Q $query | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed updating DueAt in $databaseName (exit $LASTEXITCODE)."
    }
}

function Invoke-Race {
    param([string]$PathA, [string]$TokenA, [string]$JsonA, [string]$PathB, [string]$TokenB, [string]$JsonB)

    $adminTokenBeforeWarmup = $script:adminToken
    Require-Status (Invoke-Api GET "/auth/me" $TokenA $null) @(200) "Warm up race request A"
    if ($TokenA -eq $adminTokenBeforeWarmup) { $TokenA = $script:adminToken }
    $adminTokenBeforeWarmup = $script:adminToken
    Require-Status (Invoke-Api GET "/auth/me" $TokenB $null) @(200) "Warm up race request B"
    if ($TokenB -eq $adminTokenBeforeWarmup) { $TokenB = $script:adminToken }

    $urlA = $api + $PathA
    $urlB = $api + $PathB
    $startAtUtcTicks = [DateTimeOffset]::UtcNow.AddSeconds(3).UtcTicks
    $jobA = Start-Job -ScriptBlock {
        param($Uri, $Token, $Json, $StartAtUtcTicks)
        while ([DateTimeOffset]::UtcNow.UtcTicks -lt $StartAtUtcTicks) {
            $remainingMilliseconds = [int](($StartAtUtcTicks - [DateTimeOffset]::UtcNow.UtcTicks) / [TimeSpan]::TicksPerMillisecond)
            Start-Sleep -Milliseconds ([Math]::Max(1, [Math]::Min(100, $remainingMilliseconds)))
        }
        $startedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
        try {
            $body = Invoke-RestMethod -Method Post -Uri $Uri -Headers @{ Authorization = "Bearer $Token" } -ContentType "application/json" -Body $Json -TimeoutSec 30 -ErrorAction Stop
            return [pscustomobject]@{
                Status = 200
                Body = $body
                ExceptionType = $null
                ExceptionMessage = $null
                StartedAt = $startedAt
                EndedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
            }
        }
        catch {
            $exception = $_.Exception
            $status = "ERR:$($exception.GetType().FullName)"
            $responseBody = $null
            if ($null -ne $_.Exception.Response -and $null -ne $_.Exception.Response.StatusCode) {
                $status = [int]$_.Exception.Response.StatusCode
                try {
                    $responseStream = $_.Exception.Response.GetResponseStream()
                    if ($null -ne $responseStream) {
                        $reader = New-Object System.IO.StreamReader($responseStream)
                        $responseBody = $reader.ReadToEnd()
                        $reader.Dispose()
                        $responseStream.Dispose()
                    }
                }
                catch {
                    $responseBody = "Failed to read HTTP response body: $($_.Exception.GetType().FullName): $($_.Exception.Message)"
                }
            }
            return [pscustomobject]@{
                Status = $status
                Body = $responseBody
                ExceptionType = $exception.GetType().FullName
                ExceptionMessage = $exception.Message
                StartedAt = $startedAt
                EndedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
            }
        }
    } -ArgumentList $urlA, $TokenA, $JsonA, $startAtUtcTicks
    $jobB = Start-Job -ScriptBlock {
        param($Uri, $Token, $Json, $StartAtUtcTicks)
        while ([DateTimeOffset]::UtcNow.UtcTicks -lt $StartAtUtcTicks) {
            $remainingMilliseconds = [int](($StartAtUtcTicks - [DateTimeOffset]::UtcNow.UtcTicks) / [TimeSpan]::TicksPerMillisecond)
            Start-Sleep -Milliseconds ([Math]::Max(1, [Math]::Min(100, $remainingMilliseconds)))
        }
        $startedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
        try {
            $body = Invoke-RestMethod -Method Post -Uri $Uri -Headers @{ Authorization = "Bearer $Token" } -ContentType "application/json" -Body $Json -TimeoutSec 30 -ErrorAction Stop
            return [pscustomobject]@{
                Status = 200
                Body = $body
                ExceptionType = $null
                ExceptionMessage = $null
                StartedAt = $startedAt
                EndedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
            }
        }
        catch {
            $exception = $_.Exception
            $status = "ERR:$($exception.GetType().FullName)"
            $responseBody = $null
            if ($null -ne $_.Exception.Response -and $null -ne $_.Exception.Response.StatusCode) {
                $status = [int]$_.Exception.Response.StatusCode
                try {
                    $responseStream = $_.Exception.Response.GetResponseStream()
                    if ($null -ne $responseStream) {
                        $reader = New-Object System.IO.StreamReader($responseStream)
                        $responseBody = $reader.ReadToEnd()
                        $reader.Dispose()
                        $responseStream.Dispose()
                    }
                }
                catch {
                    $responseBody = "Failed to read HTTP response body: $($_.Exception.GetType().FullName): $($_.Exception.Message)"
                }
            }
            return [pscustomobject]@{
                Status = $status
                Body = $responseBody
                ExceptionType = $exception.GetType().FullName
                ExceptionMessage = $exception.Message
                StartedAt = $startedAt
                EndedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
            }
        }
    } -ArgumentList $urlB, $TokenB, $JsonB, $startAtUtcTicks
    try {
        Wait-Job -Job @($jobA, $jobB) -Timeout 40 | Out-Null
        $results = New-Object System.Collections.Generic.List[object]
        foreach ($job in @($jobA, $jobB)) {
            if ($job.State -eq "Completed") {
                $jobResult = @(Receive-Job -Job $job -ErrorAction Stop | Where-Object { $null -ne $_ } | Select-Object -Last 1)
                if ($jobResult.Count -eq 1) {
                    $results.Add($jobResult[0])
                    continue
                }
                $jobError = "Job completed without returning a result object."
                if ($job.ChildJobs[0].JobStateInfo.Reason) { $jobError = $job.ChildJobs[0].JobStateInfo.Reason.Message }
                $results.Add([pscustomobject]@{
                    Status = "ERR:System.InvalidOperationException"
                    Body = $null
                    ExceptionType = "System.InvalidOperationException"
                    ExceptionMessage = $jobError
                    StartedAt = $null
                    EndedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
                })
            }
            else {
                Stop-Job -Job $job -ErrorAction SilentlyContinue
                $results.Add([pscustomobject]@{
                    Status = "ERR:System.TimeoutException"
                    Body = $null
                    ExceptionType = "System.TimeoutException"
                    ExceptionMessage = "The client job did not finish within 40 seconds."
                    StartedAt = $null
                    EndedAt = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
                })
            }
        }
        return ,$results.ToArray()
    }
    finally {
        Remove-Job -Job @($jobA, $jobB) -Force -ErrorAction SilentlyContinue
    }
}

function Confirm-RaceStatuses {
    param([object[]]$Results, [string]$Context)
    $wins = @($Results | Where-Object { [string]$_.Status -eq "200" }).Count
    $conflicts = @($Results | Where-Object { [string]$_.Status -eq "400" -or [string]$_.Status -eq "409" }).Count
    if ($wins -ne 1 -or $conflicts -ne 1) {
        $details = @($Results | ForEach-Object { ConvertTo-Json -InputObject $_ -Depth 12 -Compress }) -join " | "
        throw "$Context expected exactly one HTTP 200 and one HTTP 400/409; results: $details"
    }
}

function Get-RaceApiLogEvidence {
    param([object[]]$Results)
    if ([string]::IsNullOrWhiteSpace($ApiLogPath)) { return "API log extraction unavailable: pass -ApiLogPath." }
    if (-not (Test-Path -LiteralPath $ApiLogPath -PathType Leaf)) { return "API log extraction unavailable: file not found at '$ApiLogPath'." }

    $timestamps = @($Results | Where-Object { $_.StartedAt -and $_.EndedAt } | ForEach-Object {
        [pscustomobject]@{
            Start = [DateTimeOffset]::Parse($_.StartedAt).AddSeconds(-1)
            End = [DateTimeOffset]::Parse($_.EndedAt).AddSeconds(1)
        }
    })
    if ($timestamps.Count -eq 0) { return "API log extraction unavailable: request timestamps were not captured." }

    $windowStart = ($timestamps | Measure-Object -Property Start -Minimum).Minimum
    $windowEnd = ($timestamps | Measure-Object -Property End -Maximum).Maximum
    $windowLines = New-Object System.Collections.Generic.List[string]
    foreach ($line in Get-Content -LiteralPath $ApiLogPath) {
        if ($line -match '^(?:\[)?(?<timestamp>\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}\.\d{3})(?:Z|\])?') {
            $lineAt = [DateTimeOffset]::Parse($matches.timestamp)
            if ($lineAt -ge $windowStart -and $lineAt -le $windowEnd) { $windowLines.Add($line) }
        }
    }

    $evidence = @($windowLines | Where-Object {
        $_ -match '(?i)exception|SqlException|error|1205|1222|connection.{0,30}(closed|reset|broken)|closed.{0,30}connection'
    } | Select-Object -First 40)
    if ($evidence.Count -eq 0) {
        return "API log window $($windowStart.ToString('o'))..$($windowEnd.ToString('o')): no exception, SqlException 1205/1222, or closed-connection entries found."
    }
    return "API log window $($windowStart.ToString('o'))..$($windowEnd.ToString('o')):`n$($evidence -join "`n")"
}

function Confirm-StudentPlacementConsistency {
    param($Student, [int]$PositionId)
    $requests = Get-StudentRequests $Student
    $accepted = @($requests | Where-Object { Is-Status $_.status "Accepted" 4 }).Count
    $open = @($requests | Where-Object {
        (Is-Status $_.status "Pending" 0) -or (Is-Status $_.status "InterviewInvited" 3)
    }).Count
    if ($accepted -gt 1 -or ($accepted -gt 0 -and $open -gt 0)) {
        throw "Student $($Student.Code) has $accepted Accepted and $open open requests."
    }
    $studentData = Get-Data (Invoke-Api GET "/students/$($Student.Id)" $script:adminToken $null) "Verify student placement state"
    if (($accepted -eq 1 -and -not (Is-Status $studentData.status "Accepted" 3)) -or
        ($accepted -eq 0 -and (Is-Status $studentData.status "Accepted" 3))) {
        throw "Student status '$($studentData.status)' does not match the Accepted request count $accepted."
    }
    $position = Get-Data (Invoke-Api GET "/job-positions/$PositionId" $script:adminToken $null) "Verify position capacity"
    if ([int]$position.acceptedCount -gt [int]$position.quantity) {
        throw "Position $PositionId has AcceptedCount $($position.acceptedCount) above Quantity $($position.quantity)."
    }
}

function Start-Scenario {
    param([string]$Name, [scriptblock]$Action)
    try {
        $detail = & $Action
        Add-Result $Name "PASS" "$detail"
    }
    catch {
        Add-Result $Name "FAIL" $_.Exception.Message
    }
}

function Invoke-AuthorizationMatrix {
    param($User, $Company)
    $endpoints = @(
        @{ Name = "/api/students"; Path = "/students"; Expected = @(200, 403, 403, 401) },
        @{ Name = "/api/companies"; Path = "/companies"; Expected = @(200, 200, 403, 401) },
        @{ Name = "/api/job-positions"; Path = "/job-positions"; Expected = @(200, 200, 403, 401) },
        @{ Name = "/api/placement-requests"; Path = "/placement-requests"; Expected = @(200, 403, 403, 401) },
        @{ Name = "/api/company-me"; Path = "/company-me/profile"; Expected = @(403, 403, 200, 401) },
        @{ Name = "/api/me"; Path = "/me/profile"; Expected = @(403, 200, 403, 401) },
        @{ Name = "/api/dashboard"; Path = "/dashboard/summary"; Expected = @(200, 403, 403, 401) },
        @{ Name = "/api/notifications"; Path = "/notifications"; Expected = @(200, 403, 403, 401) }
    )
    $roles = @(
        @{ Name = "Admin"; Token = $script:adminToken },
        @{ Name = "User"; Token = $User.Token },
        @{ Name = "Company"; Token = $Company.Token },
        @{ Name = "Anonymous"; Token = $null }
    )
    foreach ($endpoint in $endpoints) {
        for ($index = 0; $index -lt $roles.Count; $index++) {
            $role = $roles[$index]
            $response = Invoke-Api GET $endpoint.Path $role.Token $null
            $expected = [int]$endpoint.Expected[$index]
            if ([int]$response.Status -ne $expected) {
                throw "$($endpoint.Name) as $($role.Name) expected HTTP $expected, got $($response.Status)."
            }
            Add-Result "Auth $($endpoint.Name) / $($role.Name)" "PASS" "HTTP $($response.Status)"
        }
    }
}

$sqlcmdCommand = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($null -eq $sqlcmdCommand) {
    throw "sqlcmd is required for the direct E2E DueAt checks."
}
if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
    Set-E2EProcessEnvironment "ConnectionStrings__DefaultConnection" "Server=$SqlServer;Database=$databaseName;Integrated Security=True;TrustServerCertificate=True;"
}
$connectionString = New-Object -TypeName System.Data.SqlClient.SqlConnectionStringBuilder -ArgumentList $env:ConnectionStrings__DefaultConnection
if ($connectionString.InitialCatalog -ne $databaseName -or
    $connectionString.DataSource -ne $SqlServer -or
    -not $connectionString.IntegratedSecurity -or
    -not $connectionString.TrustServerCertificate) {
    throw "Safety stop: the API connection must use Windows Authentication and TrustServerCertificate=True against $SqlServer/$databaseName."
}
if ([string]::IsNullOrWhiteSpace($env:Jwt__SecretKey) -or $env:Jwt__SecretKey.Length -lt 32) {
    Set-E2EProcessEnvironment "Jwt__SecretKey" (New-CryptoRandomString 64 "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_")
}
Set-E2EProcessEnvironment "Smtp__Enabled" "false"
Set-E2EProcessEnvironment "PlacementRequestExpiry__Enabled" "true"
Set-E2EProcessEnvironment "PlacementRequestExpiry__IntervalMinutes" "1"
Set-E2EProcessEnvironment "OverdueRequestAlertDays" "2"
Set-E2EProcessEnvironment "Logging__LogLevel__Microsoft" "Warning"
$baseUri = [Uri]$BaseUrl
if (-not $baseUri.IsLoopback) {
    throw "Safety stop: the E2E runner can recreate its database only for a loopback API URL."
}

$script:apiPort = $baseUri.Port
$null = & dotnet ef --version
if ($LASTEXITCODE -ne 0) { throw "dotnet ef is required to apply migrations to the E2E database." }
Stop-ExistingE2EApi

$dropAndCreateSql = "IF DB_ID(N'$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END; CREATE DATABASE [$databaseName];"
& sqlcmd -S $SqlServer -d master -E -C -b -Q $dropAndCreateSql | Out-Null
if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed recreating the isolated E2E database '$databaseName'." }

Push-Location $backendRoot
try {
    & dotnet ef database update --project "src\StudentInternshipMgmt.Infrastructure" --startup-project "src\StudentInternshipMgmt.Api"
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef database update failed for the isolated E2E database." }
}
finally {
    Pop-Location
}

$adminPasswordChangeFlag = & sqlcmd -S $SqlServer -d $databaseName -E -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT CONVERT(int, MustChangePassword) FROM dbo.Users WHERE Username = N'admin';"
if ($LASTEXITCODE -ne 0) { throw "Could not read MustChangePassword for the seeded E2E Admin." }
$adminPasswordChangeFlag = ($adminPasswordChangeFlag | Out-String).Trim()
if ($adminPasswordChangeFlag -ne "1") {
    throw "Expected migration ForceAdminPasswordChange to set MustChangePassword=1 for the seeded E2E Admin; observed '$adminPasswordChangeFlag'. No database update was performed."
}
Write-Host "MustChangePassword after migrations: $adminPasswordChangeFlag"

Start-E2EApi

$adminPassword = "Admin@123"
$admin = Invoke-Api POST "/auth/login" $null @{ Username = "admin"; Password = $adminPassword }
Require-Status $admin @(200) "Login seeded Admin"
$adminData = Get-Data $admin "Login seeded Admin"
if ($adminData.mustChangePassword -ne $true) {
    throw "Login with Admin@123 returned mustChangePassword=$($adminData.mustChangePassword); expected true from the migration-seeded E2E Admin."
}
$script:e2eAdminPassword = New-E2EAdminPassword
Set-AdminSession $adminData
Require-Status (Invoke-Api POST "/auth/change-password" $script:adminToken @{
    CurrentPassword = $adminPassword
    NewPassword = $script:e2eAdminPassword
}) @(200) "Change seeded Admin password"
$adminData = Get-Data (Invoke-Api POST "/auth/login" $null @{
    Username = "admin"
    Password = $script:e2eAdminPassword
}) "Login Admin after forced password change"
if ($adminData.mustChangePassword -eq $true) {
    throw "Admin still requires a password change after its new process-only password was set."
}
$script:e2eAdminPassword = $null
$adminPassword = $null
Set-AdminSession $adminData
if ([string]::IsNullOrWhiteSpace($script:adminToken)) {
    throw "Admin login returned no access token."
}
Add-Result "Fresh E2E database and Admin bootstrap" "PASS" "Only '$databaseName' was recreated; migrations applied; Admin changed from the seed password to a random process-only password."

# Scenario 22a: company-created position approval and student application.
$scenarioCompany = $null
$scenarioStudent = $null
$scenarioPosition = 0
$scenarioRequest = 0
Start-Scenario "a. Company position -> approve -> student application" {
    $script:scenarioCompany = New-TestCompany "approval"
    $script:scenarioStudent = New-TestStudent "application"
    $script:scenarioPosition = New-ApprovedPosition $script:scenarioCompany 5 "approval"
    $script:scenarioRequest = New-TestRequest $script:scenarioStudent $script:scenarioCompany $script:scenarioPosition
    $companyRequests = Get-Data (Invoke-Api GET "/company-me/placement-requests?PageNumber=1&PageSize=100" $script:scenarioCompany.Token $null) "Get company requests"
    $listed = @($companyRequests.items | Where-Object { [int]$_.id -eq $script:scenarioRequest }).Count -eq 1
    if (-not $listed) { throw "The submitted request was not visible in the owning Company's list." }
    "position $script:scenarioPosition approved; request $script:scenarioRequest visible to owning Company"
}

# Scenario 22b: student interview report, Admin acceptance, other open requests, and actor history.
$scenarioSecondCompany = $null
$scenarioSecondPosition = 0
$scenarioOtherRequest = 0
Start-Scenario "b. Interview report -> Admin Passed and cascade" {
    $script:scenarioSecondCompany = New-TestCompany "cascade"
    $script:scenarioSecondPosition = New-ApprovedPosition $script:scenarioSecondCompany 5 "cascade"
    $script:scenarioOtherRequest = New-TestRequest $script:scenarioStudent $script:scenarioSecondCompany $script:scenarioSecondPosition
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$script:scenarioRequest/invite-interview" $script:scenarioCompany.Token @{
        InterviewAt = (Get-Date).ToUniversalTime().AddDays(1).ToString("o")
        InterviewLocation = "E2E interview"
        InterviewNote = "E2E"
    }) @(200) "Invite student to interview"
    Require-Status (Invoke-Api POST "/me/placement-requests/$script:scenarioRequest/report-interviewed" $script:scenarioStudent.Token @{
        Note = "Student attended E2E interview."
    }) @(200) "Student reports interview"
    Require-Status (Invoke-Api POST "/placement-requests/$script:scenarioRequest/admin-confirm-result" $script:adminToken @{
        Decision = 0
        ConfirmationSource = "E2E interview confirmation"
        Note = "Passed in E2E"
    }) @(200) "Admin confirms Passed"
    $requests = Get-StudentRequests $script:scenarioStudent
    $accepted = @($requests | Where-Object { [int]$_.id -eq $script:scenarioRequest -and (Is-Status $_.status "Accepted" 4) }).Count -eq 1
    $rejected = @($requests | Where-Object { [int]$_.id -eq $script:scenarioOtherRequest -and (Is-Status $_.status "Rejected" 2) }).Count -eq 1
    if (-not $accepted -or -not $rejected) { throw "Student status or other-open-request rejection was incorrect." }
    $history = Get-Data (Invoke-Api GET "/students/$($script:scenarioStudent.Id)/status-history" $script:adminToken $null) "Get status history"
    $actorEntry = @($history | Where-Object { (Is-Status $_.toStatus "Accepted" 3) -and $_.changedByUsername -eq "admin" }).Count -gt 0
    if (-not $actorEntry) { throw "Accepted StatusHistory did not record Admin as the actor." }
    $historyActor = & sqlcmd -S $SqlServer -d $databaseName -E -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT TOP (1) ChangedByType FROM dbo.StatusHistories WHERE StudentId = $($script:scenarioStudent.Id) AND ToStatus = N'Accepted' ORDER BY Id DESC"
    if ($LASTEXITCODE -ne 0 -or "$($historyActor | Select-Object -Last 1)".Trim() -ne "Admin") {
        throw "StatusHistory ChangedByType was not Admin."
    }
    Confirm-StudentPlacementConsistency $script:scenarioStudent $script:scenarioPosition
    "request $script:scenarioRequest Accepted; request $script:scenarioOtherRequest Rejected; StatusHistory actor admin"
}

# Scenario 22c: company direct result and the required note on a failed result.
Start-Scenario "c. Company Passed and Failed (note required)" {
    $passCompany = New-TestCompany "company-pass"
    $passStudent = New-TestStudent "company-pass"
    $passPosition = New-ApprovedPosition $passCompany 2 "company-pass"
    $passRequest = New-TestRequest $passStudent $passCompany $passPosition
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$passRequest/result" $passCompany.Token @{
        IsPassed = $true
        Note = "Company direct Passed"
    }) @(200) "Company direct Passed"
    $passStudentData = Get-Data (Invoke-Api GET "/students/$($passStudent.Id)" $script:adminToken $null) "Get passed student"
    if (-not (Is-Status $passStudentData.status "Accepted" 3)) { throw "Company Passed did not set Student Accepted." }

    $failCompany = New-TestCompany "company-fail"
    $failStudent = New-TestStudent "company-fail"
    $failPosition = New-ApprovedPosition $failCompany 2 "company-fail"
    $failRequest = New-TestRequest $failStudent $failCompany $failPosition
    $missingNote = Invoke-Api POST "/company-me/placement-requests/$failRequest/result" $failCompany.Token @{
        IsPassed = $false
        Note = ""
    }
    Require-Status $missingNote @(400) "Company Failed without note"
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$failRequest/result" $failCompany.Token @{
        IsPassed = $false
        Note = "Required E2E failure note"
    }) @(200) "Company Failed with note"
    $failed = Get-AdminRequest $failRequest
    if (-not (Is-Status $failed.status "Rejected" 2) -or $failed.resultNote -ne "Required E2E failure note") {
        throw "Company Failed status/result note did not persist as expected."
    }
    "Company direct Passed succeeded; Failed without note HTTP 400; Failed with note persisted"
}

# Scenario 22d: Admin Failed, dismiss cannot be reported again, and cancel Accepted reopens capacity.
Start-Scenario "d. Admin Failed/DismissReport/cancel Passed" {
    $company = New-TestCompany "admin-decisions"
    $position = New-ApprovedPosition $company 1 "admin-cancel"
    $student = New-TestStudent "admin-failed"
    $requestId = New-TestRequest $student $company $position
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$requestId/invite-interview" $company.Token @{
        InterviewAt = (Get-Date).ToUniversalTime().AddDays(1).ToString("o")
        InterviewLocation = "E2E"
    }) @(200) "Invite Admin Failed student"
    Require-Status (Invoke-Api POST "/me/placement-requests/$requestId/report-interviewed" $student.Token @{ Note = "E2E" }) @(200) "Report Admin Failed interview"
    Require-Status (Invoke-Api POST "/placement-requests/$requestId/admin-confirm-result" $script:adminToken @{
        Decision = 1
        ConfirmationSource = "E2E verified"
        Note = "Admin Failed"
    }) @(200) "Admin Failed"
    $failed = Get-AdminRequest $requestId
    if (-not (Is-Status $failed.status "Rejected" 2)) { throw "Admin Failed did not reject the request." }

    $dismissStudent = New-TestStudent "dismiss"
    $dismissRequest = New-TestRequest $dismissStudent $company $position
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$dismissRequest/invite-interview" $company.Token @{
        InterviewAt = (Get-Date).ToUniversalTime().AddDays(1).ToString("o")
        InterviewLocation = "E2E"
    }) @(200) "Invite DismissReport student"
    Require-Status (Invoke-Api POST "/me/placement-requests/$dismissRequest/report-interviewed" $dismissStudent.Token @{ Note = "E2E" }) @(200) "Report DismissReport interview"
    Require-Status (Invoke-Api POST "/placement-requests/$dismissRequest/admin-confirm-result" $script:adminToken @{
        Decision = 2
        ConfirmationSource = "E2E report review"
        Note = "Dismissed in E2E"
    }) @(200) "Admin DismissReport"
    $reportAgain = Invoke-Api POST "/me/placement-requests/$dismissRequest/report-interviewed" $dismissStudent.Token @{ Note = "Second report must fail" }
    Require-Status $reportAgain @(400) "Reject repeated interview report"
    $dismissed = Get-AdminRequest $dismissRequest
    if ($null -eq $dismissed.studentReportDismissedAt) { throw "DismissReport timestamp was not persisted." }

    $acceptedStudent = New-TestStudent "cancel-accepted"
    $acceptedRequest = New-TestRequest $acceptedStudent $company $position
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$acceptedRequest/result" $company.Token @{
        IsPassed = $true
        Note = "Accept then cancel"
    }) @(200) "Company Passed at final capacity"
    $closedPosition = Get-Data (Invoke-Api GET "/job-positions/$position" $script:adminToken $null) "Get full position"
    if ($closedPosition.isOpen -ne $false) { throw "Position did not auto-close at its final slot." }
    Require-Status (Invoke-Api POST "/placement-requests/$acceptedRequest/cancel-accepted-result" $script:adminToken @{
        Note = "E2E cancel"
    }) @(200) "Admin cancels Accepted result"
    $afterCancel = Get-Data (Invoke-Api GET "/students/$($acceptedStudent.Id)" $script:adminToken $null) "Get student after cancel"
    $reopenedPosition = Get-Data (Invoke-Api GET "/job-positions/$position" $script:adminToken $null) "Get reopened position"
    if (-not (Is-Status $afterCancel.status "NoCompany" 0) -or $null -ne $afterCancel.companyId -or $reopenedPosition.isOpen -ne $true) {
        throw "Cancel did not clear Student company/status or reopen the position."
    }
    "Admin Failed persisted; dismissed report could not be resubmitted; cancel returned NoCompany and reopened full position"
}

# Scenario 22e: direct final-slot capacity closes the position.
Start-Scenario "e. Position closes when Quantity is reached" {
    $company = New-TestCompany "quantity"
    $position = New-ApprovedPosition $company 1 "quantity-one"
    $student = New-TestStudent "quantity"
    $requestId = New-TestRequest $student $company $position
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$requestId/result" $company.Token @{
        IsPassed = $true
        Note = "Final capacity"
    }) @(200) "Fill final position slot"
    $positionData = Get-Data (Invoke-Api GET "/job-positions/$position" $script:adminToken $null) "Verify closed position"
    if ($positionData.isOpen -ne $false -or [int]$positionData.acceptedCount -gt [int]$positionData.quantity) {
        throw "Position was not closed or AcceptedCount exceeded Quantity."
    }
    "Quantity=$($positionData.quantity); AcceptedCount=$($positionData.acceptedCount); IsOpen=$($positionData.isOpen)"
}

# Role-based authorization matrix plus company tenant isolation and student state changes.
Start-Scenario "f. Authorization matrix (8 endpoint groups x 4 identities)" {
    Invoke-AuthorizationMatrix $script:scenarioStudent $script:scenarioCompany
    "32 role/token checks matched controller authorization design"
}
Start-Scenario "f. Company A cannot read or modify Company B records" {
    $foreignJobRead = Invoke-Api GET "/company-me/job-positions/$script:scenarioPosition" $script:scenarioSecondCompany.Token $null
    Require-Status $foreignJobRead @(404, 403) "Read another company's job position"
    $foreignJobWrite = Invoke-Api PUT "/company-me/job-positions/$script:scenarioPosition" $script:scenarioSecondCompany.Token @{
        Title = "Unauthorized E2E edit"
        Department = "E2E"
        Location = "E2E"
        Quantity = 2
        Description = "Must be denied"
        IsOpen = $true
    }
    Require-Status $foreignJobWrite @(404, 403) "Modify another company's job position"
    $foreignRequestRead = Invoke-Api GET "/company-me/placement-requests/$script:scenarioRequest" $script:scenarioSecondCompany.Token $null
    Require-Status $foreignRequestRead @(404, 403) "Read another company's placement request"
    $foreignRequestWrite = Invoke-Api POST "/company-me/placement-requests/$script:scenarioRequest/invite-interview" $script:scenarioSecondCompany.Token @{
        InterviewAt = (Get-Date).ToUniversalTime().AddDays(1).ToString("o")
        InterviewLocation = "Unauthorized"
    }
    Require-Status $foreignRequestWrite @(404, 403) "Modify another company's placement request"
    "foreign job/request reads and writes rejected with HTTP 404/403"
}
Start-Scenario "f. Student cannot change status through administrative/result endpoints" {
    $changeStatus = Invoke-Api POST "/students/$($script:scenarioStudent.Id)/change-status" $script:scenarioStudent.Token @{
        NewStatus = 4
        Note = "Must be denied"
    }
    Require-Status $changeStatus @(403) "Student change-status request"
    $companyResult = Invoke-Api POST "/company-me/placement-requests/$script:scenarioRequest/result" $script:scenarioStudent.Token @{
        IsPassed = $true
        Note = "Must be denied"
    }
    Require-Status $companyResult @(403) "Student company-result request"
    $adminResult = Invoke-Api POST "/placement-requests/$script:scenarioRequest/admin-confirm-result" $script:scenarioStudent.Token @{
        Decision = 0
        ConfirmationSource = "Must be denied"
    }
    Require-Status $adminResult @(403) "Student Admin-result request"
    "student state/result mutations all rejected with HTTP 403"
}

# Disabled email must not affect the core operations already performed; SMTP integration is optional.
Start-Scenario "g. SMTP disabled does not block placement workflows" {
    if ($env:Smtp__Enabled -ne "false") { throw "SMTP was not explicitly disabled." }
    "Smtp__Enabled=false; application, approval, report, acceptance, and failure requests completed"
}
$smtpProcess = Get-Process -Name "smtp4dev", "Papercut", "PapercutSMTP" -ErrorAction SilentlyContinue
if ($null -eq $smtpProcess) {
    Add-Result "g. SMTP fake-server delivery" "SKIP" "smtp4dev/Papercut process not found; real SMTP was not enabled."
}
else {
    Add-Result "g. SMTP fake-server delivery" "SKIP" "A fake SMTP process exists but no E2E SMTP configuration was provided."
}

# E2E direct SQL check for old/nullable request fields; both student and Admin reads must serialize nulls.
Start-Scenario "h. Legacy nullable placement-request fields on student/Admin reads" {
    $legacyStudent = New-TestStudent "legacy"
    $legacyCompany = New-TestCompany "legacy"
    $legacyPosition = New-ApprovedPosition $legacyCompany 2 "legacy"
    $legacyRequest = New-TestRequest $legacyStudent $legacyCompany $legacyPosition
    $columns = "DueAt = NULL, InterviewAt = NULL, InterviewLocation = NULL, InterviewNote = NULL, ResultNote = NULL, StudentReportedInterviewAt = NULL, StudentInterviewNote = NULL"
    $query = "SET NOCOUNT ON; UPDATE dbo.PlacementRequests SET $columns WHERE Id = $legacyRequest AND Status = N'Pending'; IF @@ROWCOUNT <> 1 THROW 51000, 'Expected one legacy E2E request.', 1;"
    & sqlcmd -S $SqlServer -d $databaseName -E -C -b -Q $query | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed setting the nullable legacy fields." }
    $mine = Get-StudentRequests $legacyStudent
    $studentRow = @($mine | Where-Object { [int]$_.id -eq $legacyRequest } | Select-Object -First 1)
    $adminRow = Get-AdminRequest $legacyRequest
    if ($studentRow.Count -ne 1) { throw "Student endpoint failed to return the legacy request." }
    foreach ($field in @("dueAt", "interviewAt", "interviewLocation", "interviewNote", "resultNote", "studentReportedInterviewAt", "studentInterviewNote")) {
        if ($null -ne $studentRow[0].$field -or $null -ne $adminRow.$field) {
            throw "Expected null legacy field '$field' on both response surfaces."
        }
    }
    "request $legacyRequest returned all seven nullable fields as null on student/Admin endpoints"
}

# Worker and dashboard checks: first prove 2-day dashboard inclusion while pending, then expire.
Start-Scenario "i. Expiry worker, InterviewInvited immunity, reapply, dashboard alert" {
    $expiryCompany = New-TestCompany "expiry"
    $expiryPosition = New-ApprovedPosition $expiryCompany 5 "expiry"
    $expiryStudent = New-TestStudent "expiry-pending"
    $pendingId = New-TestRequest $expiryStudent $expiryCompany $expiryPosition
    $interviewStudent = New-TestStudent "expiry-interview"
    $interviewId = New-TestRequest $interviewStudent $expiryCompany $expiryPosition
    Require-Status (Invoke-Api POST "/company-me/placement-requests/$interviewId/invite-interview" $expiryCompany.Token @{
        InterviewAt = (Get-Date).ToUniversalTime().AddDays(1).ToString("o")
        InterviewLocation = "E2E"
    }) @(200) "Prepare InterviewInvited immunity request"
    Set-RequestDueAt $pendingId "DATEADD(day, 1, SYSUTCDATETIME())" "Pending"
    $dashboard = Get-Data (Invoke-Api GET "/dashboard/overdue-requests" $script:adminToken $null) "Get overdue dashboard"
    if (@($dashboard | Where-Object { [int]$_.id -eq $pendingId }).Count -ne 1) {
        throw "Overdue dashboard with OverdueRequestAlertDays=2 did not include the due-soon request."
    }
    Set-RequestDueAt $pendingId "DATEADD(day, -1, SYSUTCDATETIME())" "Pending"
    Set-RequestDueAt $interviewId "DATEADD(day, -1, SYSUTCDATETIME())" "InterviewInvited"
    $deadline = (Get-Date).AddMinutes(2)
    $expiredRow = $null
    do {
        Start-Sleep -Seconds 5
        $expiredRow = @((Get-StudentRequests $expiryStudent) | Where-Object { [int]$_.id -eq $pendingId } | Select-Object -First 1)
    } while (($expiredRow.Count -eq 0 -or -not (Is-Status $expiredRow[0].status "Expired" 5)) -and (Get-Date) -lt $deadline)
    if ($expiredRow.Count -ne 1 -or -not (Is-Status $expiredRow[0].status "Expired" 5)) {
        throw "Pending request was not Expired by the worker within two minutes."
    }
    $interviewRow = Get-AdminRequest $interviewId
    if (-not (Is-Status $interviewRow.status "InterviewInvited" 3)) {
        throw "InterviewInvited request changed status during expiry processing."
    }
    $resubmit = New-TestRequest $expiryStudent $expiryCompany $expiryPosition
    if ($resubmit -le 0) { throw "Student could not resubmit after expiry." }
    "Pending $pendingId -> Expired within 2 minutes; InterviewInvited $interviewId unchanged; reapplication $resubmit succeeded; dashboard included due-soon item"
}

# Six SQL Server concurrency races, with ten independent fixtures per race.
$raceFailedBody = ConvertTo-Json -Compress @{ Decision = 1; ConfirmationSource = "E2E race verification"; Note = "Failed race" }
$raceDismissBody = ConvertTo-Json -Compress @{ Decision = 2; ConfirmationSource = "E2E race verification"; Note = "Dismiss race" }
$racePassedBody = ConvertTo-Json -Compress @{ Decision = 0; ConfirmationSource = "E2E race verification"; Note = "Passed race" }
$raceCompanyFailBody = ConvertTo-Json -Compress @{ IsPassed = $false; Note = "Company failed race" }
$raceCompanyPassBody = ConvertTo-Json -Compress @{ IsPassed = $true; Note = "Company passed race" }

$raceNames = @(
    "i. Race: two companies Passed one student",
    "ii. Race: two students take final slot",
    "iii. Race: Admin Failed + Failed",
    "iv. Race: Admin Failed + DismissReport",
    "v. Race: Admin Passed + DismissReport",
    "vi. Race: Company Failed + Admin Passed"
)
$raceFailures = @{}
foreach ($raceName in $raceNames) { $raceFailures[$raceName] = New-Object System.Collections.Generic.List[string] }
$raceOneCorrectRounds = 0
$raceOneOddRounds = 0

for ($iteration = 1; $iteration -le [Math]::Max($ConcurrencyIterations, $RaceOneIterations); $iteration++) {
    if ($iteration -le $RaceOneIterations) {
        try {
            $companyA = New-TestCompany "race-i-a"
            $companyB = New-TestCompany "race-i-b"
            $positionA = New-ApprovedPosition $companyA 5 "race-i"
            $positionB = New-ApprovedPosition $companyB 5 "race-i"
            $student = New-TestStudent "race-i"
            $requestA = New-TestRequest $student $companyA $positionA
            $requestB = New-TestRequest $student $companyB $positionB
            $codes = Invoke-Race "/company-me/placement-requests/$requestA/result" $companyA.Token $raceCompanyPassBody "/company-me/placement-requests/$requestB/result" $companyB.Token $raceCompanyPassBody
        }
        catch {
            $raceOneOddRounds++
            $raceFailures[$raceNames[0]].Add("round ${iteration}: setup/race error: $($_.Exception.GetType().FullName): $($_.Exception.Message)")
            $codes = @()
        }
        if ($codes.Count -gt 0) {
            try {
                Confirm-RaceStatuses $codes "two companies Passed one student round $iteration"
                $rows = Get-StudentRequests $student
                $accepted = @($rows | Where-Object { Is-Status $_.status "Accepted" 4 }).Count
                $open = @($rows | Where-Object { (Is-Status $_.status "Pending" 0) -or (Is-Status $_.status "InterviewInvited" 3) }).Count
                if ($accepted -ne 1 -or $open -ne 0) { throw "Accepted=$accepted; open requests=$open." }
                Confirm-StudentPlacementConsistency $student $positionA
                $raceOneCorrectRounds++
            }
            catch {
                $raceOneOddRounds++
                $logEvidence = Get-RaceApiLogEvidence $codes
                $raceFailures[$raceNames[0]].Add("round ${iteration}: $($_.Exception.GetType().FullName): $($_.Exception.Message); $logEvidence")
            }
        }
    }

    if ($iteration -le $ConcurrencyIterations) {
    try {
        $company = New-TestCompany "race-ii"
        $position = New-ApprovedPosition $company 1 "race-ii-final-slot"
        $studentA = New-TestStudent "race-ii-a"
        $studentB = New-TestStudent "race-ii-b"
        $requestA = New-TestRequest $studentA $company $position
        $requestB = New-TestRequest $studentB $company $position
        $codes = Invoke-Race "/company-me/placement-requests/$requestA/result" $company.Token $raceCompanyPassBody "/company-me/placement-requests/$requestB/result" $company.Token $raceCompanyPassBody
        Confirm-RaceStatuses $codes "two students taking final slot round $iteration"
        $positionData = Get-Data (Invoke-Api GET "/job-positions/$position" $script:adminToken $null) "Verify concurrency final-slot count"
        if ([int]$positionData.acceptedCount -ne 1 -or [int]$positionData.acceptedCount -gt [int]$positionData.quantity -or $positionData.isOpen -ne $false) {
            throw "AcceptedCount=$($positionData.acceptedCount), Quantity=$($positionData.quantity), IsOpen=$($positionData.isOpen)."
        }
        foreach ($candidate in @($studentA, $studentB)) {
            $rows = Get-StudentRequests $candidate
            $accepted = @($rows | Where-Object { Is-Status $_.status "Accepted" 4 }).Count
            $open = @($rows | Where-Object { (Is-Status $_.status "Pending" 0) -or (Is-Status $_.status "InterviewInvited" 3) }).Count
            if ($accepted -gt 1 -or ($accepted -eq 1 -and $open -gt 0)) { throw "Student $($candidate.Code) has an inconsistent Accepted/open-request combination." }
            Confirm-StudentPlacementConsistency $candidate $position
        }
    }
    catch { $raceFailures[$raceNames[1]].Add("round ${iteration}: $($_.Exception.Message)") }

    foreach ($raceIndex in 2..5) {
        try {
            $company = New-TestCompany "race-$($raceIndex + 1)"
            $position = New-ApprovedPosition $company 3 "race-admin"
            $student = New-TestStudent "race-admin"
            $requestId = New-TestRequest $student $company $position
            Require-Status (Invoke-Api POST "/company-me/placement-requests/$requestId/invite-interview" $company.Token @{
                InterviewAt = (Get-Date).ToUniversalTime().AddDays(1).ToString("o")
                InterviewLocation = "E2E race"
            }) @(200) "Prepare Admin race InterviewInvited"
            Require-Status (Invoke-Api POST "/me/placement-requests/$requestId/report-interviewed" $student.Token @{ Note = "E2E race" }) @(200) "Prepare Admin race student report"
            if ($raceIndex -eq 5) {
                $racePath = "/company-me/placement-requests/$requestId/result"
                $codes = Invoke-Race $racePath $company.Token $raceCompanyFailBody "/placement-requests/$requestId/admin-confirm-result" $script:adminToken $racePassedBody
            }
            elseif ($raceIndex -eq 2) {
                $codes = Invoke-Race "/placement-requests/$requestId/admin-confirm-result" $script:adminToken $raceFailedBody "/placement-requests/$requestId/admin-confirm-result" $script:adminToken $raceFailedBody
            }
            elseif ($raceIndex -eq 3) {
                $codes = Invoke-Race "/placement-requests/$requestId/admin-confirm-result" $script:adminToken $raceFailedBody "/placement-requests/$requestId/admin-confirm-result" $script:adminToken $raceDismissBody
            }
            else {
                $codes = Invoke-Race "/placement-requests/$requestId/admin-confirm-result" $script:adminToken $racePassedBody "/placement-requests/$requestId/admin-confirm-result" $script:adminToken $raceDismissBody
            }
            Confirm-RaceStatuses $codes "$($raceNames[$raceIndex]) round $iteration"
            $final = Get-AdminRequest $requestId
            if ((Is-Status $final.status "Accepted" 4) -and $null -eq $final.resultActorType) {
                throw "Accepted request is missing its result actor."
            }
            if ((Is-Status $final.status "Rejected" 2) -and $null -eq $final.resultActorType) {
                throw "Rejected request is missing its result actor."
            }
            if ($raceIndex -eq 3 -and (Is-Status $final.status "InterviewInvited" 3)) {
                if ($null -eq $final.studentReportDismissedAt) { throw "DismissReport winner was not recorded." }
                $again = Invoke-Api POST "/me/placement-requests/$requestId/report-interviewed" $student.Token @{ Note = "Must not report again" }
                Require-Status $again @(400) "No repeat report after race DismissReport"
            }
            Confirm-StudentPlacementConsistency $student $position
        }
        catch { $raceFailures[$raceNames[$raceIndex]].Add("round ${iteration}: $($_.Exception.Message)") }
    }
    }
}

foreach ($raceName in $raceNames) {
    if ($raceName -eq $raceNames[0]) { continue }
    if ($raceFailures[$raceName].Count -eq 0) {
        Add-Result "$raceName ($ConcurrencyIterations rounds)" "PASS" "Each round returned one 200 and one 400/409; post-state checks passed."
    }
    else {
        Add-Result "$raceName ($ConcurrencyIterations rounds)" "FAIL" ($raceFailures[$raceName] -join " | ")
    }
}

$raceOneSummary = "Correct rounds: $raceOneCorrectRounds/$RaceOneIterations; unusual rounds: $raceOneOddRounds."
if ($raceFailures[$raceNames[0]].Count -eq 0) {
    Add-Result "$($raceNames[0]) ($RaceOneIterations rounds)" "PASS" "$raceOneSummary Every round returned one 200 and one 400/409; post-state checks passed."
}
else {
    Add-Result "$($raceNames[0]) ($RaceOneIterations rounds)" "FAIL" "$raceOneSummary $($raceFailures[$raceNames[0]] -join ' | ')"
}

$script:results | Format-Table -AutoSize -Wrap
$failureCount = @($script:results | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host ""
Write-Host ("PASS: {0}; FAIL: {1}; SKIP: {2}" -f
    @($script:results | Where-Object { $_.Status -eq "PASS" }).Count,
    $failureCount,
    @($script:results | Where-Object { $_.Status -eq "SKIP" }).Count)
Stop-E2EApi
if ($failureCount -gt 0) { exit 1 }
exit 0
