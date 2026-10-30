$ErrorActionPreference = 'Stop'
$step = '02-backup'
$started = Get-Date
$timer = [Diagnostics.Stopwatch]::StartNew()
$result = 'FAIL'
$backupPath = $null
$backupBytes = 0L
$verifySeconds = 0.0
$packageStage = $null

function Set-JsonRedactions {
    param(
        [Parameter(Mandatory = $true)]$Node,
        [string[]]$Path = @(),
        [switch]$AllValues
    )

    if ($null -eq $Node) { return $Node }
    if ($Node -is [System.Array]) {
        for ($index = 0; $index -lt $Node.Length; $index++) {
            $Node[$index] = Set-JsonRedactions -Node $Node[$index] -Path ($Path + "[$index]") -AllValues:$AllValues
        }
        return ,$Node
    }
    if ($Node -is [System.Management.Automation.PSCustomObject]) {
        foreach ($property in @($Node.PSObject.Properties)) {
            $property.Value = Set-JsonRedactions -Node $property.Value -Path ($Path + $property.Name) -AllValues:$AllValues
        }
        return $Node
    }
    if ($AllValues) { return '***REDACTED***' }

    $pathText = ($Path -join '.')
    if ($pathText -match '(?i)(^|\.)Jwt\.SecretKey$' -or $pathText -match '(?i)(^|\.)Smtp\.Password$') {
        return '***REDACTED***'
    }
    if ($Node -is [string] -and $pathText -match '(?i)(^|\.)ConnectionStrings(\.|$)') {
        return [regex]::Replace($Node, '(?i)(\b(?:Password|Pwd)\s*=\s*)([^;]*)', '$1***REDACTED***')
    }
    return $Node
}

function New-ConfigZip {
    param(
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][bool]$Redact
    )

    $stageRoot = Join-Path $packageStage ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
    $projectRoot = $script:BackupRestoreConfig.ProjectRoot
    $relativeFiles = @(
        'backend\src\StudentInternshipMgmt.Api\appsettings.json',
        'backend\src\StudentInternshipMgmt.Api\appsettings.Development.json',
        'frontend\.env.development'
    )
    $secretsFile = Join-Path $env:APPDATA ('Microsoft\UserSecrets\{0}\secrets.json' -f $script:BackupRestoreConfig.UserSecretsId)
    if (Test-Path -LiteralPath $secretsFile -PathType Leaf) {
        $relativeFiles += 'secrets\secrets.json'
    }

    foreach ($relativeFile in $relativeFiles) {
        $source = if ($relativeFile -eq 'secrets\secrets.json') { $secretsFile } else { Join-Path $projectRoot $relativeFile }
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            if ($relativeFile -ne 'secrets\secrets.json') {
                throw "Required config file not found: $relativeFile"
            }
            continue
        }
        $targetRelative = if ($relativeFile -eq 'secrets\secrets.json') { 'secrets\secrets.json' } else { $relativeFile }
        $target = Join-Path $stageRoot $targetRelative
        $targetParent = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetParent -PathType Container)) {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        }

        if (-not $Redact) {
            Copy-Item -LiteralPath $source -Destination $target
            continue
        }

        if ([IO.Path]::GetExtension($source) -eq '.json') {
            $jsonText = Get-Content -LiteralPath $source -Raw -Encoding UTF8
            $jsonObject = $jsonText | ConvertFrom-Json
            $isSecrets = $relativeFile -eq 'secrets\secrets.json'
            $jsonObject = Set-JsonRedactions -Node $jsonObject -AllValues:$isSecrets
            $jsonObject | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $target -Encoding UTF8
        }
        elseif ([IO.Path]::GetFileName($source) -eq '.env.development') {
            $envText = Get-Content -LiteralPath $source -Raw -Encoding UTF8
            $envText = [regex]::Replace($envText, '(?im)^(\s*(?:\w*SECRET\w*|\w*PASSWORD\w*|\w*TOKEN\w*|\w*API_KEY\w*)\s*=\s*).+$', '$1***REDACTED***')
            Set-Content -LiteralPath $target -Value $envText -Encoding UTF8 -NoNewline
        }
        else {
            Copy-Item -LiteralPath $source -Destination $target
        }
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Force
    }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stageRoot, $Destination, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
    return [pscustomobject]@{
        FileName = [IO.Path]::GetFileName($Destination)
        SizeBytes = (Get-Item -LiteralPath $Destination).Length
        SHA256 = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash
    }
}

try {
    . (Join-Path $PSScriptRoot '00-common.ps1')
    Initialize-BackupRestoreDirectories
    Write-Log -Level INFO -Step $step -Message 'Bat dau tao baseline va backup FULL.'
    $snapshot = Get-DbSnapshot
    $baselinePath = Join-Path $script:BackupRestoreConfig.EvidenceDirectory 'baseline-before.csv'
    Write-SnapshotCsv -Snapshot $snapshot -Path $baselinePath

    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupPath = Join-Path $script:BackupRestoreConfig.BackupDirectory ("StudentInternshipMgmtDb_FULL_{0}.bak" -f $timestamp)
    if (Test-Path -LiteralPath $backupPath) {
        do {
            Start-Sleep -Seconds 1
            $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
            $backupPath = Join-Path $script:BackupRestoreConfig.BackupDirectory ("StudentInternshipMgmtDb_FULL_{0}.bak" -f $timestamp)
        } while (Test-Path -LiteralPath $backupPath)
    }

    $escapedPath = $backupPath.Replace("'", "''")
    $databaseName = $script:BackupRestoreConfig.DatabaseName
    $backupTimer = [Diagnostics.Stopwatch]::StartNew()
    Invoke-BackupCommand -Query "BACKUP DATABASE [$databaseName] TO DISK = N'$escapedPath' WITH COMPRESSION, CHECKSUM, INIT, STATS = 10"
    $backupTimer.Stop()
    $backupSeconds = [math]::Round($backupTimer.Elapsed.TotalSeconds, 3)
    $backupBytes = (Get-Item -LiteralPath $backupPath).Length

    $dataSizeResult = Invoke-SqlTable -Query "USE [$databaseName]; SELECT COALESCE(SUM(reserved_page_count), 0) * 8192 AS DataBytes FROM sys.dm_db_partition_stats;"
    $dataBytes = [long]$dataSizeResult.Rows[0]['DataBytes']
    $compressionRatio = if ($dataBytes -gt 0) { [math]::Round(($backupBytes / [double]$dataBytes) * 100, 2) } else { 0 }

    $verification = Test-BackupFile -Path $backupPath
    $verifySeconds = $verification.DurationSeconds
    if (-not $verification.IsValid) {
        Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue
        $backupPath = $null
        throw 'RESTORE VERIFYONLY failed for the new backup.'
    }

    $packageStage = Join-Path $env:TEMP ('BackupRestoreConfig-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $packageStage -Force | Out-Null
    $secretsPath = Join-Path $env:APPDATA ('Microsoft\UserSecrets\{0}\secrets.json' -f $script:BackupRestoreConfig.UserSecretsId)
    if (-not (Test-Path -LiteralPath $secretsPath -PathType Leaf)) {
        Write-Log -Level WARN -Step $step -Message 'User secrets file not found; config packages will omit secrets.json.'
    }
    $fullZipPath = Join-Path $script:BackupRestoreConfig.ConfigDirectory ("config-backup-FULL_{0}.zip" -f $timestamp)
    $submitZipPath = Join-Path $script:BackupRestoreConfig.ConfigDirectory ("config-backup-SUBMIT_{0}.zip" -f $timestamp)
    $fullPackage = New-ConfigZip -Destination $fullZipPath -Redact $false
    $submitPackage = New-ConfigZip -Destination $submitZipPath -Redact $true

    $manifest = Get-Manifest
    $rowsByTable = @{}
    foreach ($row in $snapshot.Rows) {
        $rowsByTable[[string]$row.TableName] = [long]$row.TotalRows
    }
    $manifest.Backups = @($manifest.Backups | Where-Object { $_.FileName -ne [IO.Path]::GetFileName($backupPath) }) + @([pscustomobject]@{
        FileName = [IO.Path]::GetFileName($backupPath)
        CreatedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        SizeBytes = $backupBytes
        SHA256 = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
        BackupSeconds = $backupSeconds
        VerifySeconds = $verifySeconds
        RowsPerTable = $rowsByTable
    })
    $manifest.ConfigPackages = @($manifest.ConfigPackages | Where-Object { $_.FileName -notin @($fullPackage.FileName, $submitPackage.FileName) }) + @(
        [pscustomobject]@{ Type = 'FULL'; FileName = $fullPackage.FileName; CreatedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss'); SizeBytes = $fullPackage.SizeBytes; SHA256 = $fullPackage.SHA256 },
        [pscustomobject]@{ Type = 'SUBMIT'; FileName = $submitPackage.FileName; CreatedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss'); SizeBytes = $submitPackage.SizeBytes; SHA256 = $submitPackage.SHA256 }
    )

    $backupFiles = Get-BackupFiles
    foreach ($oldBackup in @($backupFiles | Select-Object -Skip 7)) {
        Remove-Item -LiteralPath $oldBackup.FullName -Force
        Write-Log -Level INFO -Step $step -Message "Deleted old backup: $($oldBackup.Name)"
        $manifest.Backups = @($manifest.Backups | Where-Object { $_.FileName -ne $oldBackup.Name })
    }
    Save-Manifest -Manifest $manifest
    Write-Log -Level INFO -Step $step -Message "Backup verified. Size=$backupBytes bytes; data size=$dataBytes bytes; backup/data ratio=$compressionRatio%; backup=$backupSeconds sec; verify=$verifySeconds sec."
    Write-Log -Level INFO -Step $step -Message "Config package hashes recorded: FULL=$($fullPackage.SHA256); SUBMIT=$($submitPackage.SHA256)."
    $result = 'PASS'
}
catch {
    if ($backupPath -and (Test-Path -LiteralPath $backupPath) -and $result -ne 'PASS') {
        Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue
    }
    if (Get-Command Write-Log -ErrorAction SilentlyContinue) {
        Write-Log -Level ERROR -Step $step -Message $_.Exception.Message
    }
    else {
        Write-Error $_.Exception.Message
    }
}
finally {
    $timer.Stop()
    if ($packageStage -and (Test-Path -LiteralPath $packageStage)) {
        Remove-Item -LiteralPath $packageStage -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Get-Command Add-Metric -ErrorAction SilentlyContinue) {
        $note = if ($backupPath) { "Backup=$([IO.Path]::GetFileName($backupPath)); verify=$verifySeconds sec" } else { 'Backup not completed' }
        Add-Metric -Step $step -StartTime $started -DurationSeconds $timer.Elapsed.TotalSeconds -SizeBytes $backupBytes -Result $result -Note $note
    }
}

if ($result -eq 'PASS') { exit 0 } else { exit 1 }
