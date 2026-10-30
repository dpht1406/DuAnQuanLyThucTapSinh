param(
    [switch]$IUnderstand
)

$ErrorActionPreference = 'Stop'
$step = '03-simulate-loss'
$connection = $null
$transaction = $null
$result = 'FAIL'
$started = Get-Date
$timer = [Diagnostics.Stopwatch]::StartNew()

try {
    . (Join-Path $PSScriptRoot '00-common.ps1')
    Initialize-BackupRestoreDirectories
    if (-not $IUnderstand) {
        throw 'Safety stop: rerun with -IUnderstand after reading HUONG-DAN.md.'
    }

    $cutoff = [DateTime]::UtcNow.AddHours(-24)
    $recentVerified = $false
    foreach ($candidate in (Get-BackupFiles)) {
        if ($candidate.LastWriteTimeUtc -lt $cutoff) { continue }
        $check = Test-BackupFile -Path $candidate.FullName
        if ($check.IsValid) {
            $recentVerified = $true
            break
        }
    }
    if (-not $recentVerified) {
        throw 'Safety stop: no verified backup exists within the last 24 hours.'
    }

    $confirmation = Read-Host 'To confirm this destructive data-loss simulation, type exactly: XOA DU LIEU'
    if ($confirmation -cne 'XOA DU LIEU') {
        throw 'Safety stop: confirmation phrase did not match.'
    }

    Write-Log -Level WARN -Step $step -Message 'Destructive data-loss simulation confirmed; transaction is starting.'
    $databaseName = $script:BackupRestoreConfig.DatabaseName
    $connection = New-DatabaseConnection
    Invoke-SqlNonQuery -Connection $connection -Query "USE [$databaseName]" | Out-Null

    $requestedNames = @('PlacementRequests', 'StatusHistories', 'Notifications', 'RefreshTokens', 'Users', 'Students')
    $tables = Invoke-SqlTable -Query @"
USE [$databaseName];
SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0;
"@
    $nodes = @{}
    foreach ($table in $tables.Rows) {
        if ($requestedNames -contains [string]$table.TableName) {
            $key = '{0}.{1}' -f [string]$table.SchemaName, [string]$table.TableName
            $nodes[$key] = [pscustomobject]@{
                Key = $key
                SchemaName = [string]$table.SchemaName
                TableName = [string]$table.TableName
            }
        }
    }
    foreach ($requiredName in $requestedNames) {
        if (-not ($nodes.Values | Where-Object { $_.TableName -eq $requiredName })) {
            Write-Log -Level WARN -Step $step -Message "Table not found and will be skipped: $requiredName"
        }
    }
    if ($nodes.Count -eq 0) {
        throw 'None of the expected tables exist; refusing to run.'
    }

    $foreignKeys = Invoke-SqlTable -Query @"
USE [$databaseName];
SELECT childSchema.name AS ChildSchema, childTable.name AS ChildTable,
       parentSchema.name AS ParentSchema, parentTable.name AS ParentTable
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS childTable ON childTable.object_id = fk.parent_object_id
INNER JOIN sys.schemas AS childSchema ON childSchema.schema_id = childTable.schema_id
INNER JOIN sys.tables AS parentTable ON parentTable.object_id = fk.referenced_object_id
INNER JOIN sys.schemas AS parentSchema ON parentSchema.schema_id = parentTable.schema_id;
"@
    $incomingCount = @{}
    $parentsByChild = @{}
    foreach ($key in $nodes.Keys) {
        $incomingCount[$key] = 0
        $parentsByChild[$key] = New-Object System.Collections.ArrayList
    }
    foreach ($foreignKey in $foreignKeys.Rows) {
        $childKey = '{0}.{1}' -f [string]$foreignKey.ChildSchema, [string]$foreignKey.ChildTable
        $parentKey = '{0}.{1}' -f [string]$foreignKey.ParentSchema, [string]$foreignKey.ParentTable
        if ($nodes.ContainsKey($childKey) -and $nodes.ContainsKey($parentKey)) {
            [void]$parentsByChild[$childKey].Add($parentKey)
            $incomingCount[$parentKey]++
        }
    }

    $ready = New-Object System.Collections.ArrayList
    foreach ($key in $nodes.Keys) {
        if ($incomingCount[$key] -eq 0) { [void]$ready.Add($key) }
    }
    $deleteOrder = New-Object System.Collections.ArrayList
    while ($ready.Count -gt 0) {
        $key = [string]$ready[0]
        $ready.RemoveAt(0)
        [void]$deleteOrder.Add($key)
        foreach ($parentKey in $parentsByChild[$key]) {
            $incomingCount[$parentKey]--
            if ($incomingCount[$parentKey] -eq 0) { [void]$ready.Add($parentKey) }
        }
    }
    if ($deleteOrder.Count -ne $nodes.Count) {
        throw 'Foreign-key dependency cycle found among target tables; refusing to disable constraints or delete.'
    }

    $transaction = $connection.BeginTransaction()
    $deletedCounts = New-Object System.Collections.ArrayList
    foreach ($key in $deleteOrder) {
        $tableInfo = $nodes[[string]$key]
        $schema = $tableInfo.SchemaName.Replace(']', ']]')
        $tableName = $tableInfo.TableName.Replace(']', ']]')
        if ($tableInfo.TableName -eq 'Users') {
            $deleteQuery = "DELETE FROM [$schema].[$tableName] WHERE [Role] <> N'Admin' OR [Role] IS NULL; SELECT @@ROWCOUNT;"
        }
        else {
            $deleteQuery = "DELETE FROM [$schema].[$tableName]; SELECT @@ROWCOUNT;"
        }
        $deleted = [long](Invoke-SqlScalar -Connection $connection -Transaction $transaction -Query $deleteQuery)
        [void]$deletedCounts.Add([pscustomobject]@{ TableName = $tableInfo.TableName; DeletedRows = $deleted })
    }
    $transaction.Commit()
    $transaction.Dispose()
    $transaction = $null

    $deletedCounts | Format-Table TableName, DeletedRows -AutoSize | Out-Host
    foreach ($item in $deletedCounts) {
        Write-Log -Level INFO -Step $step -Message "Deleted $($item.DeletedRows) rows from $($item.TableName)."
    }
    $snapshot = Get-DbSnapshot
    $afterLossPath = Join-Path $script:BackupRestoreConfig.EvidenceDirectory 'after-loss.csv'
    Write-SnapshotCsv -Snapshot $snapshot -Path $afterLossPath
    $snapshot | Format-Table TableName, TotalRows, DataChecksum -AutoSize | Out-Host
    Write-Log -Level INFO -Step $step -Message "After-loss snapshot saved: $afterLossPath"
    $result = 'PASS'
}
catch {
    if ($null -ne $transaction) {
        try {
            $transaction.Rollback()
        }
        catch {
            if (Get-Command Write-Log -ErrorAction SilentlyContinue) {
                Write-Log -Level ERROR -Step $step -Message "Transaction rollback also failed: $($_.Exception.Message)"
            }
        }
    }
    if (Get-Command Write-Log -ErrorAction SilentlyContinue) {
        Write-Log -Level ERROR -Step $step -Message $_.Exception.Message
    }
    else {
        Write-Error $_.Exception.Message
    }
}
finally {
    if ($null -ne $transaction) { $transaction.Dispose() }
    if ($null -ne $connection) { $connection.Dispose() }
    $timer.Stop()
    if (Get-Command Add-Metric -ErrorAction SilentlyContinue) {
        Add-Metric -Step $step -StartTime $started -DurationSeconds $timer.Elapsed.TotalSeconds -SizeBytes 0 -Result $result -Note 'Transaction-based simulated data loss'
    }
}

if ($result -eq 'PASS') { exit 0 } else { exit 1 }
