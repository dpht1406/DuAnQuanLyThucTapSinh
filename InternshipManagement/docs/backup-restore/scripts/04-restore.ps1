param(
    [string]$BackupFile,
    [switch]$IUnderstand
)

$ErrorActionPreference = 'Stop'
$step = '04-restore'
$started = Get-Date
$timer = [Diagnostics.Stopwatch]::StartNew()
$result = 'FAIL'
$setSingleUserAttempted = $false
$preRestorePath = $null
$databaseName = 'StudentInternshipMgmtDb'

try {
    . (Join-Path $PSScriptRoot '00-common.ps1')
    Initialize-BackupRestoreDirectories
    $databaseName = $script:BackupRestoreConfig.DatabaseName
    if (-not $IUnderstand) {
        throw 'Safety stop: rerun with -IUnderstand after confirming the selected backup.'
    }
    if ([string]::IsNullOrWhiteSpace($BackupFile)) {
        $latest = Get-BackupFiles | Select-Object -First 1
        if ($null -eq $latest) {
            throw 'No backup files found.'
        }
        $BackupFile = $latest.FullName
    }
    $resolvedBackup = (Resolve-Path -LiteralPath $BackupFile -ErrorAction Stop).Path
    $verification = Test-BackupFile -Path $resolvedBackup
    if (-not $verification.IsValid) {
        throw "Selected backup failed RESTORE VERIFYONLY: $resolvedBackup"
    }
    Write-Log -Level INFO -Step $step -Message "Verified selected backup: $resolvedBackup"

    $preRestoreDirectory = Join-Path $script:BackupRestoreConfig.BackupDirectory 'pre-restore'
    if (-not (Test-Path -LiteralPath $preRestoreDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $preRestoreDirectory -Force | Out-Null
    }
    $preRestorePath = Join-Path $preRestoreDirectory ("StudentInternshipMgmtDb_PRE_RESTORE_{0}.bak" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))
    $preRestoreEscaped = $preRestorePath.Replace("'", "''")
    Invoke-BackupCommand -Query "BACKUP DATABASE [$databaseName] TO DISK = N'$preRestoreEscaped' WITH COPY_ONLY, COMPRESSION, CHECKSUM, INIT, STATS = 10"
    Write-Log -Level INFO -Step $step -Message "Pre-restore copy-only backup saved: $preRestorePath"

    $setSingleUserAttempted = $true
    Invoke-SqlNonQuery -Query "ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE" | Out-Null
    $restoreEscaped = $resolvedBackup.Replace("'", "''")
    Invoke-SqlNonQuery -Query "RESTORE DATABASE [$databaseName] FROM DISK = N'$restoreEscaped' WITH REPLACE, RECOVERY, STATS = 10" | Out-Null
    Write-Log -Level INFO -Step $step -Message 'RESTORE DATABASE completed.'

    Invoke-SqlNonQuery -Query "ALTER DATABASE [$databaseName] SET MULTI_USER WITH ROLLBACK IMMEDIATE" | Out-Null
    Write-Log -Level INFO -Step $step -Message 'Database access mode set to MULTI_USER before snapshot.'
    $snapshot = Get-DbSnapshot
    $afterRestorePath = Join-Path $script:BackupRestoreConfig.EvidenceDirectory 'after-restore.csv'
    Write-SnapshotCsv -Snapshot $snapshot -Path $afterRestorePath
    $snapshot | Format-Table TableName, TotalRows, DataChecksum -AutoSize | Out-Host
    Write-Log -Level INFO -Step $step -Message "After-restore snapshot saved: $afterRestorePath"
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
    if ($setSingleUserAttempted -and (Get-Command Invoke-SqlNonQuery -ErrorAction SilentlyContinue)) {
        try {
            Invoke-SqlNonQuery -Query "ALTER DATABASE [$databaseName] SET MULTI_USER WITH ROLLBACK IMMEDIATE" | Out-Null
            Write-Log -Level INFO -Step $step -Message 'Database access mode set to MULTI_USER.'
        }
        catch {
            $result = 'FAIL'
            Write-Log -Level ERROR -Step $step -Message "Failed to set MULTI_USER in finally: $($_.Exception.Message)"
        }
    }
    $timer.Stop()
    if (Get-Command Add-Metric -ErrorAction SilentlyContinue) {
        Add-Metric -Step $step -StartTime $started -DurationSeconds $timer.Elapsed.TotalSeconds -SizeBytes 0 -Result $result -Note "Measured restore workflow RTO; backup=$BackupFile"
    }
}

if ($result -eq 'PASS') { exit 0 } else { exit 1 }
