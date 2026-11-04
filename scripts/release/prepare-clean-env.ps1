[CmdletBinding()]
param(
    [ValidateSet("Setup", "Cleanup")]
    [string]$Action = "Setup",
    [string]$DescriptorPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$evidenceDir = Join-Path $repoRoot "docs\release\evidence"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}

function Get-FastAvailablePort {
    param([int]$PreferredPort = 5028)
    $port = $PreferredPort
    while ($true) {
        try {
            $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
            $listener.Start()
            $listener.Stop()
            return $port
        } catch {
            $port++
        }
    }
}

function Kill-ProcessTree {
    param([int]$ProcessId)
    if ($ProcessId -gt 0) {
        Write-Host "Terminating process tree for PID $ProcessId..."
        cmd.exe /c "taskkill /PID $ProcessId /T /F >nul 2>nul"
    }
}

function Stop-PortListeners {
    param([int]$Port)
    if ($Port -gt 0) {
        $connections = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
        foreach ($c in $connections) {
            if ($c.OwningProcess -gt 0) {
                Write-Host "Stopping listener PID $($c.OwningProcess) on port $Port..."
                cmd.exe /c "taskkill /PID $($c.OwningProcess) /T /F >nul 2>nul"
            }
        }
    }
}

if ($Action -eq "Cleanup") {
    Write-Host "=========================================================="
    Write-Host "CLEANING UP RELEASE DEMO ENVIRONMENT"
    Write-Host "=========================================================="
    $cleanupLog = New-Object System.Text.StringBuilder
    [void]$cleanupLog.AppendLine("=== CLEANUP REPORT: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===")

    if ([string]::IsNullOrWhiteSpace($DescriptorPath)) {
        $defaultDesc = Join-Path $evidenceDir "clean-env-descriptor.json"
        if (Test-Path $defaultDesc) {
            $DescriptorPath = $defaultDesc
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($DescriptorPath) -and (Test-Path $DescriptorPath)) {
        $desc = Get-Content -LiteralPath $DescriptorPath -Raw | ConvertFrom-Json
        
        # 1. Kill API process tree
        if ($desc.ApiProcessId) {
            Kill-ProcessTree $desc.ApiProcessId
            [void]$cleanupLog.AppendLine("Killed Api Process PID: $($desc.ApiProcessId)")
        }
        if ($desc.FrontendProcessId) {
            Kill-ProcessTree $desc.FrontendProcessId
            [void]$cleanupLog.AppendLine("Killed Frontend Process PID: $($desc.FrontendProcessId)")
        }
        if ($desc.ApiPort) {
            Stop-PortListeners $desc.ApiPort
        }
        if ($desc.FrontendPort) {
            Stop-PortListeners $desc.FrontendPort
        }

        # 2. Drop Demo Database (Safety interlock check)
        $dbName = $desc.DatabaseName
        if ($dbName -eq "StudentInternshipMgmtDb") {
            [void]$cleanupLog.AppendLine("SAFETY ALERT: Database name is production DB! Skipping drop.")
            Write-Warning "Refusing to drop production database 'StudentInternshipMgmtDb'!"
        } elseif ($dbName -like "StudentInternshipMgmt_ReleaseDemo_*") {
            Write-Host "Dropping temporary demo database '$dbName'..."
            $dropSql = "IF DB_ID(N'$dbName') IS NOT NULL BEGIN ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$dbName]; END;"
            & sqlcmd -S localhost -d master -E -C -b -Q $dropSql | Out-Null
            [void]$cleanupLog.AppendLine("Dropped temporary database: $dbName")
        } else {
            [void]$cleanupLog.AppendLine("Database name '$dbName' did not match demo pattern; skipped.")
        }

        # 3. Remove temp directory
        $tempDir = $desc.TempDirectory
        if (-not [string]::IsNullOrWhiteSpace($tempDir) -and (Test-Path $tempDir)) {
            Write-Host "Removing temporary directory '$tempDir'..."
            Start-Sleep -Milliseconds 500
            Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
            [void]$cleanupLog.AppendLine("Removed temp directory: $tempDir")
        }
    } else {
        [void]$cleanupLog.AppendLine("Descriptor file not found; skipping descriptor-based cleanup.")
    }

    [void]$cleanupLog.AppendLine("Cleanup completed successfully.")
    $logPath = Join-Path $evidenceDir "cleanup.log"
    [System.IO.File]::WriteAllText($logPath, $cleanupLog.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Cleanup completed. Log saved to $logPath"
    exit 0
}

# =====================================================================
# ACTION: SETUP
# =====================================================================
Write-Host "=========================================================="
Write-Host "PREPARING CLEAN DEMO ENVIRONMENT (ISOLATED & AUTOMATED)"
Write-Host "=========================================================="

$timestamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
$tempDirName = "StudentInternshipMgmt_ReleaseDemo_$timestamp"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) $tempDirName
$dbName = "StudentInternshipMgmt_ReleaseDemo_$timestamp"

# SAFETY INTERLOCK
if ($dbName -eq "StudentInternshipMgmtDb") {
    throw "SAFETY INTERLOCK: Target database cannot match production 'StudentInternshipMgmtDb'!"
}

Write-Host "1. Target temporary directory: $tempRoot"
Write-Host "2. Target demo database: $dbName"

# Step 1: Clean clone/export at current commit
Write-Host "Exporting clean repository archive at commit HEAD..."
if (Test-Path $tempRoot) {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

$archiveZip = Join-Path ([System.IO.Path]::GetTempPath()) "git_archive_$timestamp.zip"
try {
    & git archive --format=zip -o $archiveZip HEAD
    if ($LASTEXITCODE -ne 0) { throw "git archive failed with exit code $LASTEXITCODE" }
    Expand-Archive -Path $archiveZip -DestinationPath $tempRoot -Force
} finally {
    if (Test-Path $archiveZip) {
        Remove-Item -LiteralPath $archiveZip -Force -ErrorAction SilentlyContinue
    }
}
Write-Host "Export complete. Extracted clean source code."

# Configure local package cache in temp directory for offline/locked reproducibility
$globalPackages = ""
try {
    $rawLocals = (& dotnet nuget locals global-packages -l | Out-String)
    if ($rawLocals -match 'global-packages:\s*(.+)') {
        $globalPackages = $matches[1].Trim()
    }
} catch {}
if ([string]::IsNullOrWhiteSpace($globalPackages) -or -not (Test-Path $globalPackages)) {
    $globalPackages = "C:\Users\PHAT\.nuget\packages\"
}

$nugetConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-cache" value="$globalPackages" />
  </packageSources>
</configuration>
"@
Set-Content -Path (Join-Path $tempRoot "nuget.config") -Value $nugetConfigContent -Encoding UTF8

# Step 2: Create isolated database in SQL Server
Write-Host "Creating fresh demo database '$dbName' on SQL Server localhost..."
$createDbSql = @"
IF DB_ID(N'$dbName') IS NOT NULL
BEGIN
    ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$dbName];
END;
CREATE DATABASE [$dbName];
"@
& sqlcmd -S localhost -d master -E -C -b -Q $createDbSql
if ($LASTEXITCODE -ne 0) { throw "Failed to create database '$dbName'." }
Write-Host "Database '$dbName' created successfully."

# Step 3: Find available ports
$apiPort = Get-FastAvailablePort 5028
$fePort = Get-FastAvailablePort 5183
Write-Host "Allocated dynamic free ports: Backend=$apiPort, Frontend=$fePort"

# Generate isolated secret JWT key (at least 32 bytes)
$jwtSecret = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
$connString = "Server=localhost;Database=$dbName;Integrated Security=True;TrustServerCertificate=True;"

# Set process environment variables (DO NOT modify committed appsettings.json)
$env:ConnectionStrings__DefaultConnection = $connString
$env:Jwt__SecretKey = $jwtSecret
$env:Smtp__Enabled = "false"
$env:PlacementRequestExpiry__Enabled = "false"
$env:Cors__AllowedOrigins__0 = "http://127.0.0.1:$fePort"
$env:Cors__AllowedOrigins__1 = "http://localhost:$fePort"
$env:VITE_API_BASE_URL = "http://127.0.0.1:$apiPort/api"

# Step 4: Restore dependencies and migrate
Write-Host "Restoring .NET dependencies locked to solution..."
Push-Location $tempRoot
try {
    & dotnet restore DuAnQuanLyThucTapSinh.sln
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    Write-Host "Restoring Frontend npm dependencies via npm ci..."
    Push-Location "InternshipManagement\frontend"
    try {
        cmd.exe /c "npm ci --prefer-offline"
        if ($LASTEXITCODE -ne 0) { throw "npm ci failed." }

        Write-Host "Building Frontend production bundle..."
        cmd.exe /c "npm run build"
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed." }
    } finally {
        Pop-Location
    }

    Write-Host "Applying EF Core migrations and initial seed data into '$dbName'..."
    & dotnet ef database update --project "InternshipManagement\backend\src\StudentInternshipMgmt.Infrastructure" --startup-project "InternshipManagement\backend\src\StudentInternshipMgmt.Api"
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef database update failed." }

    Write-Host "Building Backend solution in Release configuration..."
    & dotnet build DuAnQuanLyThucTapSinh.sln --no-restore -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
} finally {
    Pop-Location
}

# Step 5: Start Backend API and Frontend in background
$logsDir = Join-Path $tempRoot "logs"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
$apiStdout = Join-Path $logsDir "api-stdout.log"
$apiStderr = Join-Path $logsDir "api-stderr.log"
$feStdout = Join-Path $logsDir "frontend-stdout.log"
$feStderr = Join-Path $logsDir "frontend-stderr.log"

$apiDll = Join-Path $tempRoot "InternshipManagement\backend\src\StudentInternshipMgmt.Api\bin\Release\net8.0\StudentInternshipMgmt.Api.dll"
Write-Host "Starting Backend API: $apiDll on http://127.0.0.1:$apiPort..."
$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "`"$apiDll`" --urls `"http://127.0.0.1:$apiPort`"" `
    -WorkingDirectory (Join-Path $tempRoot "InternshipManagement\backend\src\StudentInternshipMgmt.Api") `
    -RedirectStandardOutput $apiStdout `
    -RedirectStandardError $apiStderr `
    -PassThru -NoNewWindow

Write-Host "Backend API started with PID: $($apiProcess.Id)"

Write-Host "Starting Frontend Vite on port $fePort..."
$feProcess = Start-Process -FilePath "cmd.exe" `
    -ArgumentList "/c npx vite --port $fePort --host 127.0.0.1" `
    -WorkingDirectory (Join-Path $tempRoot "InternshipManagement\frontend") `
    -RedirectStandardOutput $feStdout `
    -RedirectStandardError $feStderr `
    -PassThru -NoNewWindow

Write-Host "Frontend started with PID: $($feProcess.Id)"

# Step 6: Polling Health Check
Write-Host "Performing health check for Backend API (timeout 90s)..."
$apiDeadline = (Get-Date).AddSeconds(90)
$apiReady = $false
while (-not $apiReady -and (Get-Date) -lt $apiDeadline) {
    if ($apiProcess.HasExited) {
        $errText = Get-Content -LiteralPath $apiStderr -Raw -ErrorAction SilentlyContinue
        $outText = Get-Content -LiteralPath $apiStdout -Raw -ErrorAction SilentlyContinue
        throw "Backend API process exited unexpectedly! Error: $errText | Stdout: $outText"
    }
    try {
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$apiPort/api/auth/me" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
        $apiReady = $true
    } catch {
        if ($null -ne $_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 401) {
            # 401 Unauthorized is expected for /auth/me without token -> API is UP!
            $apiReady = $true
        } else {
            Start-Sleep -Milliseconds 500
        }
    }
}
if (-not $apiReady) {
    throw "Backend API failed to become ready within 90 seconds."
}
Write-Host "Backend API is READY at http://127.0.0.1:$apiPort"

Write-Host "Performing health check for Frontend (timeout 45s)..."
$feDeadline = (Get-Date).AddSeconds(45)
$feReady = $false
while (-not $feReady -and (Get-Date) -lt $feDeadline) {
    if ($feProcess.HasExited) {
        $errText = Get-Content -LiteralPath $feStderr -Raw -ErrorAction SilentlyContinue
        Write-Warning "Frontend process exited. Log: $errText"
        break
    }
    try {
        $null = Invoke-WebRequest -Uri "http://127.0.0.1:$fePort" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
        $feReady = $true
    } catch {
        Start-Sleep -Milliseconds 500
    }
}
if ($feReady) {
    Write-Host "Frontend is READY at http://127.0.0.1:$fePort"
} else {
    Write-Warning "Frontend health check did not respond in 45s, but continuing as API is operational."
}

# Step 7: Record Tool Versions & Export Descriptor
$dotnetVer = (& dotnet --version).Trim()
$nodeVer = (& node --version).Trim()
$npmVer = (& npm --version).Trim()
$sqlcmdRaw = (& sqlcmd -?).Split([Environment]::NewLine)[1]

$descriptor = [ordered]@{
    Timestamp = $timestamp
    TempDirectory = $tempRoot
    DatabaseName = $dbName
    SqlServer = "localhost"
    ApiPort = $apiPort
    FrontendPort = $fePort
    ApiBaseUrl = "http://127.0.0.1:$apiPort"
    FrontendBaseUrl = "http://127.0.0.1:$fePort"
    ApiProcessId = $apiProcess.Id
    FrontendProcessId = $feProcess.Id
    ToolVersions = [ordered]@{
        DotNet = $dotnetVer
        Node = $nodeVer
        Npm = $npmVer
        SqlCmd = $sqlcmdRaw
    }
    LogPaths = [ordered]@{
        ApiStdout = $apiStdout
        ApiStderr = $apiStderr
        FrontendStdout = $feStdout
        FrontendStderr = $feStderr
    }
}

$descJson = $descriptor | ConvertTo-Json -Depth 5
$tempDescFile = Join-Path $tempRoot "clean-env-descriptor.json"
$evidenceDescFile = Join-Path $evidenceDir "clean-env-descriptor.json"

[System.IO.File]::WriteAllText($tempDescFile, $descJson, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText($evidenceDescFile, $descJson, [System.Text.Encoding]::UTF8)

Write-Host "Environment descriptor saved to:"
Write-Host "  - $tempDescFile"
Write-Host "  - $evidenceDescFile"
Write-Host "Clean environment setup completed successfully!"
