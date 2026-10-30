$ErrorActionPreference = 'Stop'
$step = '07-test-fallback'
$started = Get-Date
$timer = [Diagnostics.Stopwatch]::StartNew()
$result = 'FAIL'
$corruptPath = $null

try {
    . (Join-Path $PSScriptRoot '00-common.ps1')
    Initialize-BackupRestoreDirectories
    $backupFiles = @(Get-BackupFiles)
    if ($backupFiles.Count -lt 2) {
        throw 'Fallback test requires at least two backup files.'
    }

    $validBackups = New-Object System.Collections.ArrayList
    foreach ($candidate in $backupFiles) {
        $check = Test-BackupFile -Path $candidate.FullName
        if ($check.IsValid) {
            [void]$validBackups.Add($candidate)
        }
        if ($validBackups.Count -ge 2) { break }
    }
    if ($validBackups.Count -lt 2) {
        throw 'Fallback test requires at least two verified, valid backups.'
    }

    $newestValid = $validBackups[0]
    $expectedSelection = $newestValid.FullName
    $stampTime = [DateTime]::UtcNow.AddSeconds(2)
    if ($stampTime -le $backupFiles[0].LastWriteTimeUtc) {
        $stampTime = $backupFiles[0].LastWriteTimeUtc.AddSeconds(2)
    }
    $timestamp = $stampTime.ToLocalTime().ToString('yyyyMMdd_HHmmss')
    $corruptPath = Join-Path $script:BackupRestoreConfig.BackupDirectory ("StudentInternshipMgmtDb_FULL_{0}_CORRUPTTEST.bak" -f $timestamp)
    if (Test-Path -LiteralPath $corruptPath) {
        throw "Corruption-test target already exists: $corruptPath"
    }
    Copy-Item -LiteralPath $newestValid.FullName -Destination $corruptPath
    (Get-Item -LiteralPath $corruptPath).LastWriteTimeUtc = $stampTime

    $fileStream = [System.IO.File]::Open($corruptPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        if ($fileStream.Length -lt 32) {
            throw 'Backup file is too small to corrupt safely for this test.'
        }
        $offset = [long][math]::Floor($fileStream.Length / 2)
        [void]$fileStream.Seek($offset, [System.IO.SeekOrigin]::Begin)
        $corruptBytes = [byte[]](0xA5, 0x5A, 0xC3, 0x3C, 0x96, 0x69, 0xF0, 0x0F)
        $fileStream.Write($corruptBytes, 0, $corruptBytes.Length)
        $fileStream.Flush()
    }
    finally {
        $fileStream.Dispose()
    }
    Write-Log -Level WARN -Step $step -Message "Created deliberately corrupted test copy: $([IO.Path]::GetFileName($corruptPath))"

    $childResult = Invoke-ChildPowerShell -ScriptPath (Join-Path $PSScriptRoot '06-restore-with-fallback.ps1') -Arguments @('-VerifyOnly')
    if (-not [string]::IsNullOrWhiteSpace($childResult.Output)) {
        Write-Host $childResult.Output
    }
    if ($childResult.ExitCode -ne 0) {
        throw "Fallback VerifyOnly subprocess failed with exit code $($childResult.ExitCode)."
    }
    $selectionMatch = [regex]::Match($childResult.Output, '(?m)^SELECTED_BACKUP=(.+?)\s*$')
    if (-not $selectionMatch.Success) {
        throw 'Fallback subprocess did not report SELECTED_BACKUP.'
    }
    $selectedPath = $selectionMatch.Groups[1].Value.Trim()
    if (-not [string]::Equals($selectedPath, $expectedSelection, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Fallback selected '$selectedPath'; expected '$expectedSelection'."
    }
    if ([string]::Equals($selectedPath, $corruptPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Fallback selected the deliberately corrupted file.'
    }

    $corruptDirectory = Join-Path $script:BackupRestoreConfig.EvidenceDirectory 'corrupt-sample'
    if (-not (Test-Path -LiteralPath $corruptDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $corruptDirectory -Force | Out-Null
    }
    $finalCorruptPath = Join-Path $corruptDirectory ([IO.Path]::GetFileName($corruptPath))
    Move-Item -LiteralPath $corruptPath -Destination $finalCorruptPath
    $corruptPath = $null
    Write-Log -Level INFO -Step $step -Message "PASS: invalid copy skipped; valid backup selected: $selectedPath"
    Write-Log -Level INFO -Step $step -Message "Corrupt sample moved to evidence: $finalCorruptPath"
    $result = 'PASS'
}
catch {
    if (Get-Command Write-Log -ErrorAction SilentlyContinue) {
        Write-Log -Level ERROR -Step $step -Message $_.Exception.Message
    }
    else {
        Write-Error $_.Exception.Message
    }
}
finally {
    if ($corruptPath -and (Test-Path -LiteralPath $corruptPath)) {
        try {
            $corruptDirectory = Join-Path $script:BackupRestoreConfig.EvidenceDirectory 'corrupt-sample'
            if (-not (Test-Path -LiteralPath $corruptDirectory -PathType Container)) {
                New-Item -ItemType Directory -Path $corruptDirectory -Force | Out-Null
            }
            Move-Item -LiteralPath $corruptPath -Destination (Join-Path $corruptDirectory ([IO.Path]::GetFileName($corruptPath))) -Force
            Write-Log -Level WARN -Step $step -Message 'Corrupt test copy moved to evidence during cleanup.'
        }
        catch {
            Write-Log -Level ERROR -Step $step -Message "Could not move corrupt test file: $($_.Exception.Message)"
            $result = 'FAIL'
        }
    }
    $timer.Stop()
    if (Get-Command Add-Metric -ErrorAction SilentlyContinue) {
        Add-Metric -Step $step -StartTime $started -DurationSeconds $timer.Elapsed.TotalSeconds -SizeBytes 0 -Result $result -Note 'Corrupt-copy fallback selection test; no restore performed'
    }
}

if ($result -eq 'PASS') { exit 0 } else { exit 1 }
