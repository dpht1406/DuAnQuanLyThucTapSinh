[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$stopwatchTotal = [System.Diagnostics.Stopwatch]::StartNew()
$stepTimings = [ordered]@{}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$versionFile = Join-Path $repoRoot "VERSION"
$version = if (Test-Path $versionFile) { (Get-Content $versionFile -Raw).Trim() } else { "1.0.1" }
$evidenceDir = Join-Path $repoRoot "docs\release\evidence"
$releaseDir = Join-Path $repoRoot "RELEASE\v$version"
$screensDir = Join-Path $evidenceDir "screens"

foreach ($dir in @($evidenceDir, $releaseDir, $screensDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

function Write-Heartbeat {
    param([string]$Step, [string]$Status)
    $now = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$now] [$Step] Status: $Status"
    Write-Host $line -ForegroundColor Cyan
    $heartbeatFile = Join-Path $repoRoot ".agent\release-heartbeat.txt"
    $line | Out-File -FilePath $heartbeatFile -Append -Encoding UTF8
}

function Measure-Step {
    param([string]$Name, [scriptblock]$Action)
    Write-Host "`n==========================================================" -ForegroundColor Green
    Write-Host "STARTING STEP: $Name" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action
        $sw.Stop()
        $stepTimings[$Name] = [Math]::Round($sw.Elapsed.TotalSeconds, 2)
        Write-Heartbeat $Name "PASS ($($stepTimings[$Name])s)"
    } catch {
        $sw.Stop()
        $stepTimings[$Name] = [Math]::Round($sw.Elapsed.TotalSeconds, 2)
        Write-Heartbeat $Name "FAIL ($($stepTimings[$Name])s)"
        throw
    }
}

$descriptorFile = Join-Path $evidenceDir "clean-env-descriptor.json"

try {
    # -------------------------------------------------------------
    # STEP 1: PREPARE CLEAN DEMO ENVIRONMENT
    # -------------------------------------------------------------
    Measure-Step "CLEAN-ENV-PREPARE" {
        $prepScript = Join-Path $repoRoot "scripts\release\prepare-clean-env.ps1"
        & powershell.exe -ExecutionPolicy Bypass -File $prepScript -Action Setup
        if ($LASTEXITCODE -ne 0) { throw "prepare-clean-env.ps1 failed." }
        if (-not (Test-Path $descriptorFile)) {
            throw "Descriptor file not created at: $descriptorFile"
        }
    }

    $desc = Get-Content -LiteralPath $descriptorFile -Raw | ConvertFrom-Json
    $tempRoot = $desc.TempDirectory

    # -------------------------------------------------------------
    # STEP 2: RUN BACKEND & FRONTEND TESTS (ISOLATED CLEAN ENV)
    # -------------------------------------------------------------
    Measure-Step "AUTOMATED-TESTS" {
        Write-Host "Running backend tests on clean environment..."
        $backendLog = New-Object System.Text.StringBuilder
        $totalBackendPass = 0
        $totalBackendSkip = 0
        $totalBackendFail = 0

        # Execute all 3 test projects
        $testProjects = @(
            "tests\PlacementAcceptance.Tests\PlacementAcceptance.Tests.csproj",
            "InternshipManagement\backend\tests\StudentInternshipMgmt.Tests\StudentInternshipMgmt.Tests.csproj",
            "InternshipManagement\backend\tests\StudentInternshipMgmt.Infrastructure.Tests\StudentInternshipMgmt.Infrastructure.Tests.csproj"
        )

        foreach ($projRel in $testProjects) {
            $projFull = Join-Path $tempRoot $projRel
            Write-Host "Testing project: $projRel..."
            $testOut = (& dotnet test $projFull --no-build -c Release -v normal 2>&1 | Out-String)
            [void]$backendLog.AppendLine("=== TEST PROJECT: $projRel ===")
            [void]$backendLog.AppendLine($testOut)

            if ($testOut -match 'Passed:\s*(\d+)') {
                $p = [int]$matches[1]
                $totalBackendPass += $p
                Write-Host "  -> Project $projRel passed: $p"
            }
            if ($testOut -match 'Skipped:\s*(\d+)') {
                $s = [int]$matches[1]
                $totalBackendSkip += $s
            }
            if ($testOut -match 'Failed:\s*(\d+)') {
                $f = [int]$matches[1]
                $totalBackendFail += $f
            }
        }

        $backendLogPath = Join-Path $evidenceDir "backend-test.log"
        [System.IO.File]::WriteAllText($backendLogPath, $backendLog.ToString(), [System.Text.Encoding]::UTF8)

        Write-Host "Backend Test Results: Passed=$totalBackendPass, Skipped=$totalBackendSkip, Failed=$totalBackendFail"
        if ($totalBackendPass -lt 395) {
            throw "Backend tests below floor! Expected >= 395, got $totalBackendPass"
        }
        if ($totalBackendFail -gt 0) {
            throw "Backend test failures detected: $totalBackendFail"
        }

        # Frontend Tests (Vitest, contrast, labels)
        Write-Host "Running frontend tests & accessibility checks in clean env..."
        Push-Location (Join-Path $tempRoot "InternshipManagement\frontend")
        try {
            $feTestLog = Join-Path $evidenceDir "frontend-test.log"
            $contrastLog = Join-Path $evidenceDir "frontend-contrast.log"
            $labelsLog = Join-Path $evidenceDir "frontend-labels.log"

            # Execute via cmd with redirection to avoid PowerShell NativeCommandError on stderr (Vue warnings)
            cmd.exe /c "npm test > `"$feTestLog`" 2>&1"
            if ($LASTEXITCODE -ne 0) {
                throw "Frontend vitest failed with exit code $LASTEXITCODE! Check $feTestLog"
            }
            $feTestOut = [System.IO.File]::ReadAllText($feTestLog, [System.Text.Encoding]::UTF8)
            if ($feTestOut -notmatch "passed") {
                throw "Frontend vitest did not report passed tests!"
            }

            cmd.exe /c "npm run check:contrast > `"$contrastLog`" 2>&1"
            if ($LASTEXITCODE -ne 0) {
                throw "Frontend color contrast check failed with exit code $LASTEXITCODE! Check $contrastLog"
            }

            cmd.exe /c "npm run check:labels > `"$labelsLog`" 2>&1"
            if ($LASTEXITCODE -ne 0) {
                throw "Frontend labels check failed with exit code $LASTEXITCODE! Check $labelsLog"
            }
        } finally {
            Pop-Location
        }
        Write-Host "Frontend tests, contrast check and label checks all PASSED!"
    }

    # -------------------------------------------------------------
    # STEP 3: RUN AUTOMATED UAT SUITE (10 SCENARIOS)
    # -------------------------------------------------------------
    Measure-Step "UAT-SCENARIOS" {
        $uatScript = Join-Path $repoRoot "scripts\release\run-uat-scenarios.ps1"
        & powershell.exe -ExecutionPolicy Bypass -File $uatScript -DescriptorPath $descriptorFile
        if ($LASTEXITCODE -ne 0) {
            throw "run-uat-scenarios.ps1 failed!"
        }
    }

    # -------------------------------------------------------------
    # STEP 4: CROSS-REVIEW & BUG LOG
    # -------------------------------------------------------------
    Measure-Step "CROSS-REVIEW-AND-BUG-LOG" {
        Write-Host "Performing Cross-Review and checking Bug Log..."
        
        $bugLogFile = Join-Path $repoRoot "docs\release\bug-log.md"
        $crossReviewFile = Join-Path $repoRoot "docs\release\cross-review.md"

        if (-not (Test-Path $crossReviewFile)) {
            throw "Cross-review document not found at: $crossReviewFile"
        }
        if (-not (Test-Path $bugLogFile)) {
            throw "Bug-log document not found at: $bugLogFile"
        }

        $crText = [System.IO.File]::ReadAllText($crossReviewFile, [System.Text.Encoding]::UTF8)

        # 1. Verify P1-P4 sections are present
        foreach ($level in @("P1", "P2", "P3", "P4")) {
            if ($crText -notmatch "\b$level\b") {
                throw "cross-review.md lacks section or mention of severity level: $level"
            }
        }

        # 2. Verify reviewed commit range is stated
        if ($crText -notmatch "(Khoảng commit|baseline\.\.HEAD|commit.*đã đối soát|7fc712b)") {
            throw "cross-review.md does not specify reviewed commit range!"
        }

        # 3. Verify comparison of 3 technical risks found by independent reviewer
        # Risk 1: T-SQL row lock dialect specific
        if ($crText -notmatch "(T-SQL.*[Kk]hóa dòng|phương ngữ.*SQL Server|Dialect-Specific)") {
            throw "cross-review.md missing required analysis of T-SQL dialect row lock risk!"
        }
        # Risk 2: Implicit rollback on Reject
        if ($crText -notmatch "([Hh]oàn tác giao dịch ngầm định|Implicit Rollback|DisposeAsync)") {
            throw "cross-review.md missing required analysis of implicit transaction rollback on Reject!"
        }
        # Risk 3: Trim type-coercion in frontend
        if ($crText -notmatch "([Tt]rim.*Frontend|[Tt]rim [Tt]ype-[Cc]oercion|kiểu dữ liệu.*trim)") {
            throw "cross-review.md missing required analysis of frontend trim type-coercion risk!"
        }

        # 4. Verify no unobjective claims
        if ($crText -match "100% đã khắc phục triệt để") {
            throw "cross-review.md contains unobjective phrase '100% đã khắc phục triệt để'!"
        }

        # 5. Verify no open P1/P2 issues in bug log
        $blText = [System.IO.File]::ReadAllText($bugLogFile, [System.Text.Encoding]::UTF8)
        if ($blText -match '\|\s*\*\*P1[^\n\|]*\*\*\s*\|\s*[^\|\n]+\|\s*[^\|\n]+\|\s*[^\|\n]+\|\s*\*\*(Còn Mở|Mở|OPEN)\*\*') {
            throw "Open P1 bug detected in bug-log.md!"
        }
        if ($blText -match '\|\s*\*\*P2[^\n\|]*\*\*\s*\|\s*[^\|\n]+\|\s*[^\|\n]+\|\s*[^\|\n]+\|\s*\*\*(Còn Mở|Mở|OPEN)\*\*') {
            throw "Open P2 bug detected in bug-log.md!"
        }
        Write-Host "Cross-review verified: P1-P4 present, commit range stated, 3 technical risks reviewed, zero open P1/P2." -ForegroundColor Green
    }

    # -------------------------------------------------------------
    # STEP 5: PACKAGING & REPRODUCIBILITY VERIFICATION
    # -------------------------------------------------------------
    Measure-Step "PACKAGE-AND-VERIFY" {
        Write-Host "Packaging clean source code for Release v$version..."
        $packageZip = Join-Path $releaseDir "DuAnQuanLyThucTapSinh-v$version.zip"
        if (Test-Path $packageZip) {
            Remove-Item -LiteralPath $packageZip -Force
        }

        # Determine target commit to tag
        $currentCommit = (& git rev-parse HEAD).Trim()
        $commitIso = (& git log -1 --format=%cI $currentCommit).Trim()
        $tagName = "v$version"

        Write-Host "Creating git archive package from commit $currentCommit..."
        & git archive --format=zip -o $packageZip $currentCommit
        if ($LASTEXITCODE -ne 0) { throw "Failed to create release zip package from commit $currentCommit." }
        $sizeMb = [Math]::Round(((Get-Item $packageZip).Length / 1048576), 2)
        Write-Host "Package created: $packageZip ($sizeMb MB)"

        # Tag the exact commit immediately (Requirement 5 / P3-02)
        Write-Host "Tagging commit $currentCommit with tag $tagName..."
        & git tag -f -a $tagName $currentCommit -m "Release $tagName"
        if ($LASTEXITCODE -ne 0) { throw "Failed to tag commit $currentCommit with $tagName." }
        Write-Host "Tagged $tagName -> $currentCommit successfully."

        # Package sample data into RELEASE/v$version/data
        $dataDir = Join-Path $releaseDir "data"
        if (-not (Test-Path $dataDir)) { New-Item -ItemType Directory -Path $dataDir -Force | Out-Null }
        Copy-Item -Path (Join-Path $repoRoot "docs\company-accounts-bulk\scripts\*.sql") -Destination $dataDir -Force

        # Generate Checksums (SHA256)
        $checksumLog = New-Object System.Text.StringBuilder
        $hashZip = (Get-FileHash -Path $packageZip -Algorithm SHA256).Hash
        [void]$checksumLog.AppendLine("$hashZip  DuAnQuanLyThucTapSinh-v$version.zip")
        foreach ($sql in (Get-ChildItem -Path $dataDir -Filter "*.sql")) {
            $h = (Get-FileHash -Path $sql.FullName -Algorithm SHA256).Hash
            [void]$checksumLog.AppendLine("$h  data/$($sql.Name)")
        }
        $checksumFile = Join-Path $releaseDir "checksums.sha256"
        [System.IO.File]::WriteAllText($checksumFile, $checksumLog.ToString(), [System.Text.Encoding]::UTF8)
        Write-Host "Generated checksums file: $checksumFile"

        # Record PROVENANCE into RELEASE/v$version/PROVENANCE.md (Requirement 5 / P3-02)
        Write-Host "Recording provenance at RELEASE/v$version/PROVENANCE.md..."
        $provenanceFile = Join-Path $releaseDir "PROVENANCE.md"
        $zipEntries = @()
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zipObj = [System.IO.Compression.ZipFile]::OpenRead($packageZip)
        try {
            $zipEntries = $zipObj.Entries | ForEach-Object { $_.FullName }
        } finally {
            $zipObj.Dispose()
        }

        $nowUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
        $provContent = @"
# Bản Ghi Nguồn Gốc Gói Phát Hành (Release Provenance - v$version)

> [!IMPORTANT]
> Gói phát hành ``DuAnQuanLyThucTapSinh-v$version.zip`` được đóng gói tự động từ đúng commit mã nguồn được gắn tag ``$tagName``.

## 1. Thông Tin Nguồn Gốc (Metadata)
- **Phiên bản phát hành**: ``v$version``
- **Git Tag**: ``$tagName``
- **Commit SHA**: ``$currentCommit``
- **Thời điểm commit**: ``$commitIso``
- **Thời điểm đóng gói**: ``$nowUtc``
- **Công cụ đóng gói**: ``git archive --format=zip``
- **Tệp nén**: ``DuAnQuanLyThucTapSinh-v$version.zip`` ($sizeMb MB)
- **Mã băm SHA256**: ``$hashZip``
- **Tổng số file trong zip**: $($zipEntries.Count) files

## 2. Danh Sách Thư Mục Chỉ Có Trong Repo (Không Nằm Trong Zip)
Theo thiết kế đóng gói mã nguồn sạch qua git archive, các thư mục sau chỉ tồn tại trong kho lưu trữ phục vụ quản trị và nghiệm thu mà không được đưa vào gói nguồn triển khai:
- ``RELEASE/`` (Lưu trữ các bản zip, checksum, tài liệu phát hành)
- ``docs/release/evidence/`` (Nhật ký thực thi test, UAT log, ảnh chụp màn hình)
- ``.agent/`` (Cờ trạng thái và heartbeat của quy trình phát hành)
- ``.git/`` (Lịch sử phiên bản git cục bộ)

## 3. Xác Nhận Tính Đồng Nhất
- Cam kết tag ``$tagName`` và tệp nén ``DuAnQuanLyThucTapSinh-v$version.zip`` cùng xuất phát từ commit ``$currentCommit``.
- Mã băm của gói khớp hoàn toàn với ``checksums.sha256``.
"@
        [System.IO.File]::WriteAllText($provenanceFile, $provContent, [System.Text.Encoding]::UTF8)
        Write-Host "Generated PROVENANCE: $provenanceFile"

        # VERIFY PACKAGE: Extract into fresh empty temp dir and run reproduction test
        # REQUIREMENT 3: Must execute EXACT commands written in README-REPRODUCE.md
        Write-Host "Verifying reproducible package driven by README-REPRODUCE.md..."
        $verifyTimestamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
        $verifyDir = Join-Path ([System.IO.Path]::GetTempPath()) "StudentInternshipMgmt_PackageVerify_$verifyTimestamp"
        $verifyDb = "StudentInternshipMgmt_PackageVerify_$verifyTimestamp"

        New-Item -ItemType Directory -Path $verifyDir -Force | Out-Null
        $verifyLog = New-Object System.Text.StringBuilder
        [void]$verifyLog.AppendLine("=== PACKAGE REPRODUCIBILITY VERIFICATION REPORT (DOC-DRIVEN) ===")
        [void]$verifyLog.AppendLine("Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
        [void]$verifyLog.AppendLine("Target Version: v$version")
        [void]$verifyLog.AppendLine("Verify Directory: $verifyDir")
        [void]$verifyLog.AppendLine("Verify Database: $verifyDb`n")

        # Parse commands from README-REPRODUCE.md
        $readmePath = Join-Path $releaseDir "README-REPRODUCE.md"
        if (-not (Test-Path $readmePath)) {
            $readmePath = Join-Path $repoRoot "RELEASE\README-REPRODUCE.md"
        }
        if (-not (Test-Path $readmePath)) {
            throw "README-REPRODUCE.md not found at: $readmePath"
        }

        Write-Host "Reading commands from: $readmePath"
        $readmeContent = [System.IO.File]::ReadAllText($readmePath, [System.Text.Encoding]::UTF8)
        $codeBlockRegex = '(?ms)```(?:powershell|bash)\r?\n(.*?)```'
        $allMatches = [regex]::Matches($readmeContent, $codeBlockRegex)
        $reproduceCommands = @()
        foreach ($m in $allMatches) {
            $blockText = $m.Groups[1].Value
            if ($blockText -match "dotnet restore DuAnQuanLyThucTapSinh\.sln") {
                $rawLines = $blockText -split "\r?\n"
                foreach ($line in $rawLines) {
                    $trimmedLine = $line.Trim()
                    if ($trimmedLine -and -not $trimmedLine.StartsWith("#")) {
                        $reproduceCommands += $trimmedLine
                    }
                }
                break
            }
        }

        if ($reproduceCommands.Count -eq 0) {
            throw "Failed to extract reproduce commands from $readmePath!"
        }
        Write-Host "Extracted $($reproduceCommands.Count) commands from README-REPRODUCE.md:"
        $reproduceCommands | ForEach-Object { Write-Host "   > $_" -ForegroundColor DarkGray }

        try {
            Expand-Archive -Path $packageZip -DestinationPath $verifyDir -Force
            [void]$verifyLog.AppendLine("Step 1: Unpacked package zip successfully.")

            # Configure local package cache for offline locked restore
            $globalPkg = (& dotnet nuget locals global-packages -l).Split(':', 2)[1].Trim()
            $ncfg = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-cache" value="$globalPkg" />
  </packageSources>
</configuration>
"@
            Set-Content -Path (Join-Path $verifyDir "nuget.config") -Value $ncfg -Encoding UTF8

            Push-Location $verifyDir
            try {
                $cmdIdx = 1
                foreach ($cmd in $reproduceCommands) {
                    [void]$verifyLog.AppendLine("`n--- Executing README command #$($cmdIdx): '$cmd' ---")
                    Write-Host "Executing README command #$($cmdIdx): $cmd" -ForegroundColor Yellow

                    if ($cmd -match '^\s*cd\s+(.+)$') {
                        $targetFolder = $matches[1].Trim()
                        # Normalize slashes for Windows
                        $targetFolder = $targetFolder.Replace('/', '\')
                        Set-Location $targetFolder
                        [void]$verifyLog.AppendLine("Changed directory to: $(Get-Location)")
                    } else {
                        $output = cmd.exe /c "$cmd 2>&1"
                        $exitCode = $LASTEXITCODE
                        [void]$verifyLog.AppendLine($output)
                        if ($exitCode -ne 0) {
                            throw "README command failed with exit code $($exitCode): '$cmd'."
                        }
                    }
                    $cmdIdx++
                }
            } finally {
                Pop-Location
            }
            [void]$verifyLog.AppendLine("`nConclusion: PASS - Package verified completely reproducible using EXACT commands from README-REPRODUCE.md.")
        } finally {
            if (Test-Path $verifyDir) {
                Remove-Item -LiteralPath $verifyDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        $packageVerifyLogPath = Join-Path $releaseDir "package-verification.log"
        $evidenceVerifyLogPath = Join-Path $evidenceDir "package-verification.log"
        [System.IO.File]::WriteAllText($packageVerifyLogPath, $verifyLog.ToString(), [System.Text.Encoding]::UTF8)
        [System.IO.File]::WriteAllText($evidenceVerifyLogPath, $verifyLog.ToString(), [System.Text.Encoding]::UTF8)
        Write-Host "Package reproduction test PASSED!"
    }

    # -------------------------------------------------------------
    # STEP 6: TIMING & REPORTING
    # -------------------------------------------------------------
    $stopwatchTotal.Stop()
    $totalSeconds = [Math]::Round($stopwatchTotal.Elapsed.TotalSeconds, 2)
    $stepTimings["TOTAL"] = $totalSeconds

    $timingReport = New-Object System.Text.StringBuilder
    [void]$timingReport.AppendLine("=== RELEASE V$version EXECUTION TIMING REPORT ===")
    [void]$timingReport.AppendLine("Execution Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$timingReport.AppendLine("Total Duration: $totalSeconds seconds`n")
    [void]$timingReport.AppendLine("Step Breakdowns:")
    foreach ($k in $stepTimings.Keys) {
        $timingLine = "  - {0,-28}: {1,8} s" -f $k, $stepTimings[$k]
        [void]$timingReport.AppendLine($timingLine)
    }
    $timingFilePath = Join-Path $evidenceDir "timing.txt"
    [System.IO.File]::WriteAllText($timingFilePath, $timingReport.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "`n$($timingReport.ToString())"

    # Mark done flag
    $doneFlagFile = Join-Path $repoRoot ".agent\release-done.flag"
    $flagContent = "PASS`nRelease v$version successfully completed and verified on clean environment at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'). Total time: $totalSeconds s."
    [System.IO.File]::WriteAllText($doneFlagFile, $flagContent, [System.Text.Encoding]::UTF8)
    Write-Host "SUCCESS: Marked release-done.flag as PASS" -ForegroundColor Green

} finally {
    # -------------------------------------------------------------
    # ALWAYS CLEAN UP DEMO ENVIRONMENT
    # -------------------------------------------------------------
    Write-Host "`n=========================================================="
    Write-Host "CLEANING UP DEMO ENVIRONMENT (FINALLY BLOCK)"
    Write-Host "=========================================================="
    $prepScript = Join-Path $repoRoot "scripts\release\prepare-clean-env.ps1"
    & powershell.exe -ExecutionPolicy Bypass -File $prepScript -Action Cleanup -DescriptorPath $descriptorFile
    Write-Heartbeat "CLEANUP" "COMPLETED"
}
