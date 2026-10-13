param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$TestDatabaseName
)

$ErrorActionPreference = "Stop"

if ($TestDatabaseName -cne "StudentInternshipMgmtDb_Test") {
    throw "Refusing to benchmark: TestDatabaseName must be exactly StudentInternshipMgmtDb_Test."
}

$confirmation = Read-Host "Confirm the API is configured to use StudentInternshipMgmtDb_Test by typing the name"
if ($confirmation -cne $TestDatabaseName) {
    throw "Database confirmation did not match. No requests were sent."
}

$baseUrl = $ApiBaseUrl.TrimEnd("/")
$username = Read-Host "Dedicated test Admin username"
$securePassword = Read-Host "Dedicated test Admin password" -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
$httpClient = [Net.Http.HttpClient]::new()
$httpClient.Timeout = [TimeSpan]::FromMinutes(5)
$runTag = [Guid]::NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant()
$runRecords = [Collections.Generic.List[object]]::new()
$summaryRecords = [Collections.Generic.List[object]]::new()
$encoding = [Text.UTF8Encoding]::new($true)
$dataDirectory = Join-Path $PSScriptRoot "..\data"

function Invoke-ApiRequest([string]$Method, [string]$Path, [Net.Http.HttpContent]$Content = $null) {
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($Method), "$baseUrl/$Path")
    if ($null -ne $Content) {
        $request.Content = $Content
    }

    $response = $httpClient.SendAsync($request).GetAwaiter().GetResult()
    try {
        $body = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) {
            throw "API request failed: $Method $Path returned HTTP $([int]$response.StatusCode). Response body was not logged."
        }
        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            ByteCount = $body.Length
            Text = [Text.Encoding]::UTF8.GetString($body)
        }
    }
    finally {
        $response.Dispose()
        $request.Dispose()
    }
}

function Add-Measurement([string]$Name, [scriptblock]$Action, [int]$Iterations = 5) {
    $elapsedValues = [Collections.Generic.List[double]]::new()
    for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
        $stopwatch = [Diagnostics.Stopwatch]::StartNew()
        $response = & $Action $iteration
        $stopwatch.Stop()
        $elapsed = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
        $elapsedValues.Add($elapsed)
        $runRecords.Add([pscustomobject]@{
            Operation = $Name
            Iteration = $iteration
            ElapsedMs = $elapsed
            HttpStatus = $response.StatusCode
            ResponseBytes = $response.ByteCount
        })
    }

    $sorted = @($elapsedValues | Sort-Object)
    $middle = [int][Math]::Floor($sorted.Count / 2)
    $median = if ($sorted.Count % 2 -eq 0) {
        ($sorted[$middle - 1] + $sorted[$middle]) / 2
    }
    else {
        $sorted[$middle]
    }
    $summaryRecords.Add([pscustomobject]@{
        Operation = $Name
        Runs = $Iterations
        MinMs = $sorted[0]
        MedianMs = [Math]::Round($median, 2)
        MaxMs = $sorted[-1]
    })
}

function ConvertTo-CsvText([object[]]$Rows) {
    $columns = @("StudentCode", "FullName", "Email", "PhoneNumber", "Major", "ClassName")
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add(($columns -join ","))
    foreach ($row in $Rows) {
        $fields = foreach ($column in $columns) {
            $value = [string]$row.$column
            if ($value.Contains(",") -or $value.Contains('"') -or $value.Contains("`n") -or $value.Contains("`r")) {
                '"' + $value.Replace('"', '""') + '"'
            }
            else {
                $value
            }
        }
        $lines.Add(($fields -join ","))
    }
    return ($lines -join "`r`n") + "`r`n"
}

function New-TestRows([object[]]$SourceRows, [string]$Prefix) {
    $rows = [Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $SourceRows.Count; $index++) {
        $source = $SourceRows[$index]
        $rows.Add([pscustomobject]@{
            StudentCode = "TST$runTag$Prefix{0:D5}" -f ($index + 1)
            FullName = [string]$source.FullName
            Email = [string]$source.Email
            PhoneNumber = [string]$source.PhoneNumber
            Major = [string]$source.Major
            ClassName = [string]$source.ClassName
        })
    }
    return $rows.ToArray()
}

function Invoke-ImportRows([object[]]$Rows) {
    $csvBytes = $encoding.GetBytes((ConvertTo-CsvText $Rows))
    $multipart = [Net.Http.MultipartFormDataContent]::new()
    $fileContent = [Net.Http.ByteArrayContent]::new($csvBytes)
    $multipart.Add($fileContent, "file", "benchmark.csv")
    try {
        return Invoke-ApiRequest "POST" "api/students/import" $multipart
    }
    finally {
        $multipart.Dispose()
    }
}

try {
    $loginBody = @{ Username = $username; Password = $password } | ConvertTo-Json -Compress
    Add-Measurement "Admin login" {
        $content = [Net.Http.StringContent]::new($loginBody, [Text.Encoding]::UTF8, "application/json")
        try {
            $result = Invoke-ApiRequest "POST" "api/auth/login" $content
            $payload = $result.Text | ConvertFrom-Json
            $accessToken = $payload.data.accessToken
            if ([string]::IsNullOrWhiteSpace($accessToken)) {
                throw "Login response did not include data.accessToken. Token response was not logged."
            }
            $script:benchmarkToken = $accessToken
            return $result
        }
        finally {
            $content.Dispose()
        }
    }
    $httpClient.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $benchmarkToken)

    $source1000 = @(Import-Csv -LiteralPath (Join-Path $dataDirectory "students-large-1000.csv") -Encoding UTF8)
    $source5000 = @(Import-Csv -LiteralPath (Join-Path $dataDirectory "students-large-5000.csv") -Encoding UTF8)
    foreach ($size in @(100, 1000, 5000)) {
        $source = if ($size -eq 5000) { $source5000 } else { $source1000 | Select-Object -First $size }
        $trialPrefix = "I$size"
        $trialBatches = [Collections.Generic.List[object[]]]::new()
        for ($iteration = 1; $iteration -le 5; $iteration++) {
            $trialBatches.Add([object[]](New-TestRows $source "${trialPrefix}${iteration}"))
        }
        Add-Measurement "Import CSV $size rows" {
            param($iteration)
            Invoke-ImportRows $trialBatches[$iteration - 1]
        }
    }

    $export1000 = New-TestRows $source1000 "X100"
    $export5000 = New-TestRows $source5000 "X500"
    $seed1000 = Invoke-ImportRows $export1000
    $seed5000 = Invoke-ImportRows $export5000

    foreach ($item in @(
        @{ Label = "Export CSV 1,000"; Prefix = "TST$runTag" + "X100" },
        @{ Label = "Export CSV 5,000"; Prefix = "TST$runTag" + "X500" },
        @{ Label = "Export XLSX 1,000"; Prefix = "TST$runTag" + "X100" },
        @{ Label = "Export XLSX 5,000"; Prefix = "TST$runTag" + "X500" }
    )) {
        $format = if ($item.Label -like "*XLSX*") { "xlsx" } else { "csv" }
        $search = [Uri]::EscapeDataString($item.Prefix)
        Add-Measurement $item.Label {
            $result = Invoke-ApiRequest "GET" "api/students/export?format=$format&Search=$search"
            return $result
        }
    }

    $pageSearch = [Uri]::EscapeDataString("TST$runTag" + "X500")
    Add-Measurement "Paged student list with 5,000 rows" {
        Invoke-ApiRequest "GET" "api/students?Search=$pageSearch&PageNumber=1&PageSize=20"
    }
    Add-Measurement "Dashboard summary" {
        Invoke-ApiRequest "GET" "api/dashboard/summary"
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $runsPath = Join-Path $PSScriptRoot "runtime-runs-$timestamp.csv"
    $summaryPath = Join-Path $PSScriptRoot "runtime-summary-$timestamp.csv"
    $runRecords | Export-Csv -LiteralPath $runsPath -NoTypeInformation -Encoding UTF8
    $summaryRecords | Export-Csv -LiteralPath $summaryPath -NoTypeInformation -Encoding UTF8
    Write-Output "Completed benchmark requests. Raw timings: $runsPath; min/median/max: $summaryPath."
    Write-Output "No tokens or response bodies were written. Benchmark-created TST records remain in the dedicated test DB; drop that DB after reviewing evidence."
}
finally {
    $httpClient.Dispose()
    $password = $null
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    $securePassword.Dispose()
}
