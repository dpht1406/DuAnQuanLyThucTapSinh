$ErrorActionPreference = 'Stop'
$step = '05-verify'
$started = Get-Date
$timer = [Diagnostics.Stopwatch]::StartNew()
$result = 'FAIL'

try {
    . (Join-Path $PSScriptRoot '00-common.ps1')
    Initialize-BackupRestoreDirectories
    $evidenceDirectory = $script:BackupRestoreConfig.EvidenceDirectory
    $baselinePath = Join-Path $evidenceDirectory 'baseline-before.csv'
    $afterRestorePath = Join-Path $evidenceDirectory 'after-restore.csv'
    $afterLossPath = Join-Path $evidenceDirectory 'after-loss.csv'
    foreach ($requiredFile in @($baselinePath, $afterRestorePath)) {
        if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
            throw "Required evidence file not found: $requiredFile"
        }
    }

    $baselineRows = @(Import-Csv -LiteralPath $baselinePath -Encoding UTF8)
    $restoredRows = @(Import-Csv -LiteralPath $afterRestorePath -Encoding UTF8)
    $baselineByName = @{}
    $restoredByName = @{}
    foreach ($row in $baselineRows) { $baselineByName[[string]$row.TableName] = $row }
    foreach ($row in $restoredRows) { $restoredByName[[string]$row.TableName] = $row }

    $names = @($baselineByName.Keys + $restoredByName.Keys | Sort-Object -Unique)
    $comparison = New-Object System.Collections.ArrayList
    foreach ($name in $names) {
        $before = $baselineByName[[string]$name]
        $after = $restoredByName[[string]$name]
        $hasBoth = ($null -ne $before -and $null -ne $after)
        $rowsMatch = $hasBoth -and ([string]$before.TotalRows -eq [string]$after.TotalRows)
        $checksumAvailable = $hasBoth -and $before.DataChecksum -ne 'N/A' -and $after.DataChecksum -ne 'N/A'
        $checksumMatch = $checksumAvailable -and ([string]$before.DataChecksum -eq [string]$after.DataChecksum)
        $status = if ($rowsMatch -and $checksumMatch) { 'PASS' } elseif ($rowsMatch -and -not $checksumAvailable) { 'UNVERIFIABLE' } else { 'FAIL' }
        [void]$comparison.Add([pscustomobject]@{
            TableName = [string]$name
            BaselineRows = if ($hasBoth) { $before.TotalRows } else { '' }
            RestoredRows = if ($hasBoth) { $after.TotalRows } else { '' }
            BaselineChecksum = if ($hasBoth) { $before.DataChecksum } else { 'MISSING' }
            RestoredChecksum = if ($hasBoth) { $after.DataChecksum } else { 'MISSING' }
            RowCountMatch = $rowsMatch
            ChecksumMatch = $checksumMatch
            Result = $status
        })
    }

    $verifyPath = Join-Path $evidenceDirectory 'verify-result.csv'
    $comparison | Export-Csv -LiteralPath $verifyPath -NoTypeInformation -Encoding UTF8
    $comparison | Format-Table TableName, BaselineRows, RestoredRows, RowCountMatch, ChecksumMatch, Result -AutoSize | Out-Host

    if (Test-Path -LiteralPath $afterLossPath -PathType Leaf) {
        $lossRows = @(Import-Csv -LiteralPath $afterLossPath -Encoding UTF8)
        $lossByName = @{}
        foreach ($row in $lossRows) { $lossByName[[string]$row.TableName] = $row }
        $lossDifferences = New-Object System.Collections.ArrayList
        foreach ($name in @($baselineByName.Keys + $lossByName.Keys | Sort-Object -Unique)) {
            $before = $baselineByName[[string]$name]
            $afterLoss = $lossByName[[string]$name]
            $same = $null -ne $before -and $null -ne $afterLoss -and
                ([string]$before.TotalRows -eq [string]$afterLoss.TotalRows) -and
                ([string]$before.DataChecksum -eq [string]$afterLoss.DataChecksum)
            if (-not $same) {
                [void]$lossDifferences.Add([pscustomobject]@{
                    TableName = [string]$name
                    BaselineRows = if ($null -ne $before) { $before.TotalRows } else { 'MISSING' }
                    AfterLossRows = if ($null -ne $afterLoss) { $afterLoss.TotalRows } else { 'MISSING' }
                    BaselineChecksum = if ($null -ne $before) { $before.DataChecksum } else { 'MISSING' }
                    AfterLossChecksum = if ($null -ne $afterLoss) { $afterLoss.DataChecksum } else { 'MISSING' }
                })
            }
        }
        if ($lossDifferences.Count -gt 0) {
            Write-Host 'Tables changed after the simulated data loss:'
            $lossDifferences | Format-Table -AutoSize | Out-Host
            Write-Log -Level INFO -Step $step -Message "Loss comparison found $($lossDifferences.Count) changed table(s)."
        }
        else {
            Write-Log -Level WARN -Step $step -Message 'No differences were detected between baseline and after-loss snapshot.'
        }
    }
    else {
        Write-Log -Level WARN -Step $step -Message 'after-loss.csv not found; loss comparison skipped.'
    }

    $failed = @($comparison | Where-Object { $_.Result -ne 'PASS' })
    if ($failed.Count -gt 0) {
        Write-Log -Level ERROR -Step $step -Message "Restore verification failed or was unverifiable for $($failed.Count) table(s)."
        $result = 'FAIL'
    }
    else {
        Write-Log -Level INFO -Step $step -Message "All $($comparison.Count) table snapshots match."
        $result = 'PASS'
    }
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
        Add-Metric -Step $step -StartTime $started -DurationSeconds $timer.Elapsed.TotalSeconds -SizeBytes 0 -Result $result -Note 'Compare baseline, after-loss and after-restore snapshots'
    }
}

if ($result -eq 'PASS') { exit 0 } else { exit 1 }
