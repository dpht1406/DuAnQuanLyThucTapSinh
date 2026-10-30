$ErrorActionPreference = 'Stop'

$script:BackupRestoreConfig = @{
    DatabaseName = 'StudentInternshipMgmtDb'
    WorkspaceRoot = 'C:\BackupDemo'
    BackupDirectory = 'C:\BackupDemo\backups'
    ConfigDirectory = 'C:\BackupDemo\config'
    LogDirectory = 'C:\BackupDemo\logs'
    EvidenceDirectory = 'C:\BackupDemo\evidence'
    LogFile = 'C:\BackupDemo\logs\backup-restore.log'
    MetricsFile = 'C:\BackupDemo\evidence\metrics.csv'
    ManifestFile = 'C:\BackupDemo\backups\manifest.json'
    UserSecretsId = '172049aa-b3b1-4c2f-a2f3-538028103edf'
    ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
    AppSettingsPath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path 'backend\src\StudentInternshipMgmt.Api\appsettings.json')
}

function Initialize-BackupRestoreDirectories {
    foreach ($directory in @(
        $script:BackupRestoreConfig.BackupDirectory,
        $script:BackupRestoreConfig.ConfigDirectory,
        $script:BackupRestoreConfig.LogDirectory,
        $script:BackupRestoreConfig.EvidenceDirectory
    )) {
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
            New-Item -ItemType Directory -Path $directory -Force | Out-Null
        }
    }
}

function Write-Log {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('INFO', 'WARN', 'ERROR')]
        [string]$Level,
        [Parameter(Mandatory = $true)]
        [string]$Step,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Initialize-BackupRestoreDirectories
    $line = '{0} [{1}] [{2}] {3}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level, $Step, $Message
    Write-Host $line
    Add-Content -LiteralPath $script:BackupRestoreConfig.LogFile -Value $line -Encoding UTF8
}

function Get-ConnectionString {
    if (-not (Test-Path -LiteralPath $script:BackupRestoreConfig.AppSettingsPath -PathType Leaf)) {
        throw "appsettings.json not found: $($script:BackupRestoreConfig.AppSettingsPath)"
    }

    $settings = Get-Content -LiteralPath $script:BackupRestoreConfig.AppSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $connectionValue = $settings.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace([string]$connectionValue)) {
        throw 'ConnectionStrings:DefaultConnection is missing or empty.'
    }

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder -ArgumentList ([string]$connectionValue)
    if ($builder.InitialCatalog -cne $script:BackupRestoreConfig.DatabaseName) {
        throw "Refusing to run: configured database must be '$($script:BackupRestoreConfig.DatabaseName)'."
    }
    if (-not $builder.IntegratedSecurity -or -not [string]::IsNullOrEmpty($builder.UserID) -or -not [string]::IsNullOrEmpty($builder.Password)) {
        throw 'Refusing to run: Windows Authentication is required.'
    }

    $dataSource = $builder.DataSource.Trim()
    $hostPart = $dataSource -replace ',\d+$', ''
    if ($hostPart.Contains('\')) {
        $hostPart = ($hostPart -split '\\', 2)[0]
    }
    $localNames = @('.', '(local)', 'localhost', '127.0.0.1', '::1', [Environment]::MachineName)
    if (-not ($localNames | Where-Object { $_ -ieq $hostPart })) {
        throw "Refusing to run: SQL Server '$dataSource' is not a recognized local server."
    }

    $builder['Initial Catalog'] = 'master'
    return $builder.ConnectionString
}

function New-DatabaseConnection {
    $connection = New-Object System.Data.SqlClient.SqlConnection -ArgumentList (Get-ConnectionString)
    $connection.add_InfoMessage({
        param($sender, $eventArgs)
        foreach ($sqlMessage in $eventArgs.Errors) {
            if ($sqlMessage.Message -notmatch '^Changed database context to ') {
                Write-Log -Level INFO -Step 'SQL' -Message $sqlMessage.Message
            }
        }
    })
    $connection.FireInfoMessageEventOnUserErrors = $false
    $connection.Open()
    return $connection
}

function Invoke-SqlNonQuery {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Query,
        [System.Data.SqlClient.SqlConnection]$Connection,
        [System.Data.SqlClient.SqlTransaction]$Transaction
    )

    $ownsConnection = $null -eq $Connection
    if ($ownsConnection) {
        $Connection = New-DatabaseConnection
    }

    try {
        $command = $Connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = 0
        if ($null -ne $Transaction) {
            $command.Transaction = $Transaction
        }
        return $command.ExecuteNonQuery()
    }
    finally {
        if ($ownsConnection -and $null -ne $Connection) {
            $Connection.Dispose()
        }
    }
}

function Invoke-SqlTable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    $connection = New-DatabaseConnection
    try {
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = 0
        $reader = $command.ExecuteReader()
        try {
            $table = New-Object System.Data.DataTable
            $table.Load($reader)
            return ,$table
        }
        finally {
            $reader.Dispose()
            $command.Dispose()
        }
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlScalar {
    param(
        [Parameter(Mandatory = $true)][string]$Query,
        [System.Data.SqlClient.SqlConnection]$Connection,
        [System.Data.SqlClient.SqlTransaction]$Transaction
    )

    $ownsConnection = $null -eq $Connection
    if ($ownsConnection) {
        $Connection = New-DatabaseConnection
    }
    try {
        $command = $Connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = 0
        if ($null -ne $Transaction) {
            $command.Transaction = $Transaction
        }
        return $command.ExecuteScalar()
    }
    finally {
        if ($ownsConnection -and $null -ne $Connection) {
            $Connection.Dispose()
        }
    }
}

function Invoke-BackupCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    try {
        Invoke-SqlNonQuery -Query $Query | Out-Null
    }
    catch {
        if ($_.Exception.Message -match '(?i)3201|Operating system error 5') {
            Write-BackupPermissionGuidance
        }
        throw
    }
}

function Write-BackupPermissionGuidance {
    try {
        $serviceRows = Invoke-SqlTable -Query "SELECT service_account FROM sys.dm_server_services WHERE servicename LIKE 'SQL Server (%'"
        $accounts = @($serviceRows.Rows | ForEach-Object { [string]$_.service_account } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
        if ($accounts.Count -eq 0) {
            Write-Log -Level ERROR -Step 'PERMISSIONS' -Message 'Khong doc duoc tai khoan dich vu SQL Server; hay kiem tra DMV sys.dm_server_services.'
            return
        }
        foreach ($account in $accounts) {
            $command = 'icacls "{0}" /grant "{1}:(OI)(CI)M"' -f $script:BackupRestoreConfig.BackupDirectory, $account
            Write-Log -Level ERROR -Step 'PERMISSIONS' -Message "SQL Server service account: $account. Tu chay lenh bang quyen Administrator: $command"
        }
    }
    catch {
        Write-Log -Level ERROR -Step 'PERMISSIONS' -Message "Khong truy van duoc tai khoan dich vu SQL Server: $($_.Exception.Message)"
    }
}

function Get-DbSnapshot {
    $tableList = Invoke-SqlTable -Query @"
USE [$($script:BackupRestoreConfig.DatabaseName)];
SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;
"@
    $spaceTable = Invoke-SqlTable -Query "USE [$($script:BackupRestoreConfig.DatabaseName)]; EXEC sp_spaceused"
    if ($spaceTable.Rows.Count -lt 1) {
        throw 'sp_spaceused did not return database size information.'
    }

    $databaseSizeText = [string]$spaceTable.Rows[0]['database_size']
    $unallocatedText = [string]$spaceTable.Rows[0]['unallocated space']
    $sizeMatch = [regex]::Match($databaseSizeText, '^\s*([\d.,]+)\s*MB', 'IgnoreCase')
    $unallocatedMatch = [regex]::Match($unallocatedText, '^\s*([\d.,]+)\s*MB', 'IgnoreCase')
    $databaseSizeMb = $null
    $unallocatedMb = $null
    if ($sizeMatch.Success) {
        $databaseSizeMb = [decimal]::Parse($sizeMatch.Groups[1].Value, [Globalization.NumberStyles]::Number, [Globalization.CultureInfo]::InvariantCulture)
    }
    if ($unallocatedMatch.Success) {
        $unallocatedMb = [decimal]::Parse($unallocatedMatch.Groups[1].Value, [Globalization.NumberStyles]::Number, [Globalization.CultureInfo]::InvariantCulture)
    }

    $snapshot = New-Object System.Data.DataTable
    [void]$snapshot.Columns.Add('TableName', [string])
    [void]$snapshot.Columns.Add('TotalRows', [long])
    [void]$snapshot.Columns.Add('DataChecksum', [string])
    [void]$snapshot.Columns.Add('DatabaseSizeMB', [string])
    [void]$snapshot.Columns.Add('UnallocatedSpaceMB', [string])

    foreach ($tableInfo in $tableList.Rows) {
        $schema = ([string]$tableInfo.SchemaName).Replace(']', ']]')
        $name = ([string]$tableInfo.TableName).Replace(']', ']]')
        $qualifiedName = "[$schema].[$name]"
        $rowResult = Invoke-SqlTable -Query "USE [$($script:BackupRestoreConfig.DatabaseName)]; SELECT COUNT_BIG(1) AS TotalRows FROM $qualifiedName"
        $totalRows = [long]$rowResult.Rows[0]['TotalRows']
        $checksum = 'N/A'
        try {
            $checksumResult = Invoke-SqlTable -Query "USE [$($script:BackupRestoreConfig.DatabaseName)]; SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) AS DataChecksum FROM $qualifiedName"
            if ($checksumResult.Rows.Count -gt 0 -and $checksumResult.Rows[0]['DataChecksum'] -ne [DBNull]::Value) {
                $checksum = [string]$checksumResult.Rows[0]['DataChecksum']
            }
            elseif ($totalRows -eq 0) {
                $checksum = 'EMPTY'
            }
        }
        catch {
            Write-Log -Level WARN -Step 'SNAPSHOT' -Message "Checksum unavailable for $qualifiedName; row count retained. $($_.Exception.Message)"
        }

        $record = $snapshot.NewRow()
        $record.TableName = "$schema.$name"
        $record.TotalRows = $totalRows
        $record.DataChecksum = $checksum
        $record.DatabaseSizeMB = if ($null -ne $databaseSizeMb) { [string]$databaseSizeMb } else { $databaseSizeText }
        $record.UnallocatedSpaceMB = if ($null -ne $unallocatedMb) { [string]$unallocatedMb } else { $unallocatedText }
        [void]$snapshot.Rows.Add($record)
    }
    $snapshot.ExtendedProperties['DatabaseSizeMB'] = $databaseSizeMb
    $snapshot.ExtendedProperties['DatabaseSizeText'] = $databaseSizeText
    return ,$snapshot
}

function Test-BackupFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
            return [pscustomobject]@{ IsValid = $false; DurationSeconds = 0.0; Path = $Path }
        }
        $escapedPath = $Path.Replace("'", "''")
        Invoke-SqlNonQuery -Query "RESTORE VERIFYONLY FROM DISK = N'$escapedPath' WITH CHECKSUM" | Out-Null
        $header = Invoke-SqlTable -Query "RESTORE HEADERONLY FROM DISK = N'$escapedPath'"
        if ($header.Rows.Count -lt 1 -or [string]$header.Rows[0]['DatabaseName'] -cne $script:BackupRestoreConfig.DatabaseName) {
            $timer.Stop()
            Write-Log -Level WARN -Step 'VERIFY' -Message "Backup does not contain database '$($script:BackupRestoreConfig.DatabaseName)': '$Path'."
            return [pscustomobject]@{ IsValid = $false; DurationSeconds = [math]::Round($timer.Elapsed.TotalSeconds, 3); Path = $Path }
        }
        $timer.Stop()
        return [pscustomobject]@{ IsValid = $true; DurationSeconds = [math]::Round($timer.Elapsed.TotalSeconds, 3); Path = $Path }
    }
    catch {
        $timer.Stop()
        Write-Log -Level WARN -Step 'VERIFY' -Message "Backup verify failed for '$Path': $($_.Exception.Message)"
        return [pscustomobject]@{ IsValid = $false; DurationSeconds = [math]::Round($timer.Elapsed.TotalSeconds, 3); Path = $Path }
    }
    finally {
        if ($timer.IsRunning) {
            $timer.Stop()
        }
    }
}

function Add-Metric {
    param(
        [Parameter(Mandatory = $true)][string]$Step,
        [Parameter(Mandatory = $true)][datetime]$StartTime,
        [Parameter(Mandatory = $true)][double]$DurationSeconds,
        [Parameter(Mandatory = $true)][long]$SizeBytes,
        [Parameter(Mandatory = $true)][string]$Result,
        [string]$Note = ''
    )

    Initialize-BackupRestoreDirectories
    $metric = [pscustomobject]@{
        Step = $Step
        StartTime = $StartTime.ToString('yyyy-MM-dd HH:mm:ss')
        DurationSeconds = [math]::Round($DurationSeconds, 3)
        SizeBytes = $SizeBytes
        Result = $Result
        Note = $Note
    }
    if (-not (Test-Path -LiteralPath $script:BackupRestoreConfig.MetricsFile -PathType Leaf)) {
        $metric | Export-Csv -LiteralPath $script:BackupRestoreConfig.MetricsFile -NoTypeInformation -Encoding UTF8
    }
    else {
        $metric | Export-Csv -LiteralPath $script:BackupRestoreConfig.MetricsFile -NoTypeInformation -Append -Encoding UTF8
    }
}

function Write-SnapshotCsv {
    param(
        [Parameter(Mandatory = $true)][System.Data.DataTable]$Snapshot,
        [Parameter(Mandatory = $true)][string]$Path
    )
    $Snapshot | Export-Csv -LiteralPath $Path -NoTypeInformation -Encoding UTF8
}

function Get-Manifest {
    $manifestPath = $script:BackupRestoreConfig.ManifestFile
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        return [pscustomobject]@{ Backups = @(); ConfigPackages = @() }
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -eq $manifest.Backups) { $manifest | Add-Member -NotePropertyName Backups -NotePropertyValue @() }
    if ($null -eq $manifest.ConfigPackages) { $manifest | Add-Member -NotePropertyName ConfigPackages -NotePropertyValue @() }
    return $manifest
}

function Save-Manifest {
    param([Parameter(Mandatory = $true)]$Manifest)
    $Manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $script:BackupRestoreConfig.ManifestFile -Encoding UTF8
}

function Get-BackupFiles {
    if (-not (Test-Path -LiteralPath $script:BackupRestoreConfig.BackupDirectory -PathType Container)) {
        return @()
    }
    return @(Get-ChildItem -LiteralPath $script:BackupRestoreConfig.BackupDirectory -File -Filter 'StudentInternshipMgmtDb_FULL_*.bak' |
        Sort-Object LastWriteTimeUtc -Descending)
}

function Invoke-ChildPowerShell {
    param(
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [string[]]$Arguments = @()
    )
    $quotedArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"{0}"' -f $ScriptPath))
    foreach ($argument in $Arguments) {
        $quotedArgs += ('"{0}"' -f ($argument -replace '"', '\"'))
    }
    $output = & "$PSHOME\powershell.exe" @quotedArgs 2>&1
    $exitCode = $LASTEXITCODE
    return [pscustomobject]@{ ExitCode = $exitCode; Output = ($output | Out-String) }
}
