param(
    [switch]$VerifyOnly,
    [switch]$IUnderstand
)

$ErrorActionPreference = 'Stop'
$step = '06-restore-with-fallback'
$resultCode = 1
$started = Get-Date

try {
    . (Join-Path $PSScriptRoot '00-common.ps1')
    Initialize-BackupRestoreDirectories
    if (-not $VerifyOnly -and -not $IUnderstand) {
        throw 'Safety stop: use -IUnderstand to restore; use -VerifyOnly to select without restoring.'
    }

    $selected = $null
    foreach ($candidate in (Get-BackupFiles)) {
        $check = Test-BackupFile -Path $candidate.FullName
        if ($check.IsValid) {
            $selected = $candidate
            break
        }
        Write-Log -Level WARN -Step $step -Message "Skipping invalid backup: $($candidate.Name)"
    }

    if ($null -eq $selected) {
        Write-Log -Level ERROR -Step $step -Message 'No valid backup remains.'
        $resultCode = 2
    }
    elseif ($VerifyOnly) {
        Write-Log -Level INFO -Step $step -Message "Verify-only selected: $($selected.FullName)"
        Write-Output ("SELECTED_BACKUP={0}" -f $selected.FullName)
        $resultCode = 0
    }
    else {
        Write-Log -Level WARN -Step $step -Message "Delegating restore to 04-restore.ps1 for $($selected.FullName)"
        $childResult = Invoke-ChildPowerShell -ScriptPath (Join-Path $PSScriptRoot '04-restore.ps1') -Arguments @('-BackupFile', $selected.FullName, '-IUnderstand')
        if (-not [string]::IsNullOrWhiteSpace($childResult.Output)) {
            Write-Host $childResult.Output
        }
        $resultCode = [int]$childResult.ExitCode
        if ($resultCode -eq 0) {
            Write-Log -Level INFO -Step $step -Message 'Fallback restore completed successfully.'
        }
        else {
            Write-Log -Level ERROR -Step $step -Message "Restore subprocess failed with exit code $resultCode."
        }
    }
}
catch {
    if (Get-Command Write-Log -ErrorAction SilentlyContinue) {
        Write-Log -Level ERROR -Step $step -Message $_.Exception.Message
    }
    else {
        Write-Error $_.Exception.Message
    }
    $resultCode = 1
}
finally {
    if (Get-Command Add-Metric -ErrorAction SilentlyContinue) {
        Add-Metric -Step $step -StartTime $started -DurationSeconds ([math]::Round(((Get-Date) - $started).TotalSeconds, 3)) -SizeBytes 0 -Result $(if ($resultCode -eq 0) { 'PASS' } elseif ($resultCode -eq 2) { 'NO_VALID_BACKUP' } else { 'FAIL' }) -Note $(if ($VerifyOnly) { 'Verify-only selection' } else { 'Fallback restore workflow' })
    }
}

exit $resultCode
