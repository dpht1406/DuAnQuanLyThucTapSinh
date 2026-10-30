$ErrorActionPreference = 'Stop'
$step = '01-baseline'
$started = Get-Date
$timer = [Diagnostics.Stopwatch]::StartNew()
$result = 'FAIL'
$sizeBytes = 0

try {
    . (Join-Path $PSScriptRoot '00-common.ps1')
    Initialize-BackupRestoreDirectories
    Write-Log -Level INFO -Step $step -Message 'Bat dau chup baseline database.'
    $snapshot = Get-DbSnapshot
    Write-SnapshotCsv -Snapshot $snapshot -Path (Join-Path $script:BackupRestoreConfig.EvidenceDirectory 'baseline-before.csv')
    $databaseSizeMb = $snapshot.ExtendedProperties['DatabaseSizeMB']
    if ($null -ne $databaseSizeMb) {
        $sizeBytes = [long]([decimal]$databaseSizeMb * 1MB)
    }
    $snapshot | Format-Table TableName, TotalRows, DataChecksum, DatabaseSizeMB, UnallocatedSpaceMB -AutoSize | Out-Host
    Write-Log -Level INFO -Step $step -Message "Baseline saved. User tables: $($snapshot.Rows.Count)."
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
    $timer.Stop()
    if (Get-Command Add-Metric -ErrorAction SilentlyContinue) {
        Add-Metric -Step $step -StartTime $started -DurationSeconds $timer.Elapsed.TotalSeconds -SizeBytes $sizeBytes -Result $result -Note 'Snapshot to baseline-before.csv'
    }
}

if ($result -eq 'PASS') { exit 0 } else { exit 1 }
