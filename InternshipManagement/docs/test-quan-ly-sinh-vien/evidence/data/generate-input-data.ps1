$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression

$outputDirectory = $PSScriptRoot
$random = [Random]::new(20261012)
$headers = @("StudentCode", "FullName", "Email", "PhoneNumber", "Major", "ClassName")

function ConvertTo-CsvField([string] $Value) {
    if ($Value.Contains(",") -or $Value.Contains('"') -or $Value.Contains("`n") -or $Value.Contains("`r")) {
        return '"' + $Value.Replace('"', '""') + '"'
    }

    return $Value
}

function Write-CsvFile([string] $Path, [string[]] $Columns, [object[]] $Rows) {
    $utf8 = [Text.UTF8Encoding]::new($true)
    $writer = [IO.StreamWriter]::new($Path, $false, $utf8)
    try {
        $writer.WriteLine(($Columns | ForEach-Object { ConvertTo-CsvField $_ }) -join ",")
        foreach ($row in $Rows) {
            $values = foreach ($column in $Columns) {
                ConvertTo-CsvField ([string]$row.$column)
            }
            $writer.WriteLine($values -join ",")
        }
    }
    finally {
        $writer.Dispose()
    }
}

function ConvertTo-ColumnName([int] $Number) {
    $name = ""
    while ($Number -gt 0) {
        $Number--
        $name = [char](65 + ($Number % 26)) + $name
        $Number = [Math]::Floor($Number / 26)
    }
    return $name
}

function Add-ZipTextEntry($Archive, [string] $Name, [string] $Content) {
    $entry = $Archive.CreateEntry($Name, [IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    try {
        $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
        $writer.Write($Content)
        $writer.Dispose()
    }
    finally {
        $stream.Dispose()
    }
}

function Write-XlsxFile([string] $Path, [string[]] $Columns, [object[]] $Rows) {
    $sheetRows = [Text.StringBuilder]::new()
    [void]$sheetRows.Append('<row r="1">')
    for ($columnIndex = 0; $columnIndex -lt $Columns.Count; $columnIndex++) {
        $reference = "$(ConvertTo-ColumnName ($columnIndex + 1))1"
        $safeValue = [Security.SecurityElement]::Escape($Columns[$columnIndex])
        [void]$sheetRows.Append("<c r=`"$reference`" t=`"inlineStr`"><is><t xml:space=`"preserve`">$safeValue</t></is></c>")
    }
    [void]$sheetRows.Append('</row>')

    for ($rowIndex = 0; $rowIndex -lt $Rows.Count; $rowIndex++) {
        $excelRow = $rowIndex + 2
        [void]$sheetRows.Append("<row r=`"$excelRow`">")
        for ($columnIndex = 0; $columnIndex -lt $Columns.Count; $columnIndex++) {
            $reference = "$(ConvertTo-ColumnName ($columnIndex + 1))$excelRow"
            $rawValue = [string]$Rows[$rowIndex].$($Columns[$columnIndex])
            $safeValue = [Security.SecurityElement]::Escape($rawValue)
            [void]$sheetRows.Append("<c r=`"$reference`" t=`"inlineStr`"><is><t xml:space=`"preserve`">$safeValue</t></is></c>")
        }
        [void]$sheetRows.Append('</row>')
    }

    $sheetXml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
        '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">' +
        '<sheetData>' + $sheetRows.ToString() + '</sheetData></worksheet>'
    $workbookXml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
        '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">' +
        '<sheets><sheet name="Students" sheetId="1" r:id="rId1"/></sheets></workbook>'
    $rootRels = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' +
        '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>'
    $workbookRels = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' +
        '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>'
    $contentTypes = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">' +
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>' +
        '<Default Extension="xml" ContentType="application/xml"/>' +
        '<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>' +
        '<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>'

    $fileStream = [IO.File]::Open($Path, [IO.FileMode]::Create)
    try {
        $archive = [IO.Compression.ZipArchive]::new($fileStream, [IO.Compression.ZipArchiveMode]::Create)
        Add-ZipTextEntry $archive "[Content_Types].xml" $contentTypes
        Add-ZipTextEntry $archive "_rels/.rels" $rootRels
        Add-ZipTextEntry $archive "xl/workbook.xml" $workbookXml
        Add-ZipTextEntry $archive "xl/_rels/workbook.xml.rels" $workbookRels
        Add-ZipTextEntry $archive "xl/worksheets/sheet1.xml" $sheetXml
        $archive.Dispose()
    }
    finally {
        $fileStream.Dispose()
    }
}

$validRows = for ($index = 1; $index -le 20; $index++) {
    [pscustomobject]@{
        StudentCode = "TST{0:D4}" -f $index
        FullName = "Nguyễn Thị Sinh viên {0:D2}" -f $index
        Email = "tst{0:D4}@example.edu.vn" -f $index
        PhoneNumber = "09{0:D8}" -f $index
        Major = "Công nghệ thông tin"
        ClassName = "TST-CNTT-01"
    }
}
Write-CsvFile (Join-Path $outputDirectory "students-valid-20.csv") $headers $validRows

$mixedRows = @(
    [pscustomobject]@{ StudentCode = "TST9001"; FullName = "Nguyễn Ánh"; Email = "anh@example.edu.vn"; PhoneNumber = "0900009001"; Major = "CNTT"; ClassName = "SE01" },
    [pscustomobject]@{ StudentCode = "TST9002"; FullName = "Thiếu họ tên"; Email = "tst9002@example.edu.vn"; PhoneNumber = "0900009002"; Major = "CNTT"; ClassName = "" },
    [pscustomobject]@{ StudentCode = "TST9003"; FullName = "Sai email"; Email = "not-an-email"; PhoneNumber = "0900009003"; Major = "CNTT"; ClassName = "SE01" },
    [pscustomobject]@{ StudentCode = "TST9004"; FullName = "Sai điện thoại"; Email = "tst9004@example.edu.vn"; PhoneNumber = "123456789"; Major = "CNTT"; ClassName = "SE01" },
    [pscustomobject]@{ StudentCode = "TST9005"; FullName = "Mã trùng lần đầu"; Email = "tst9005@example.edu.vn"; PhoneNumber = "0900009005"; Major = "CNTT"; ClassName = "SE01" },
    [pscustomobject]@{ StudentCode = "tst9005"; FullName = "Mã trùng khác hoa thường"; Email = "tst9006@example.edu.vn"; PhoneNumber = "0900009006"; Major = "CNTT"; ClassName = "SE01" },
    [pscustomobject]@{ StudentCode = "TSTDB0001"; FullName = "MSSV cần seed vào DB test"; Email = "db@example.edu.vn"; PhoneNumber = "0900009007"; Major = "CNTT"; ClassName = "SE01" },
    [pscustomobject]@{ StudentCode = "TST9008"; FullName = "=1+1"; Email = "safe@example.edu.vn"; PhoneNumber = "0900009008"; Major = "CNTT"; ClassName = "SE01" },
    [pscustomobject]@{ StudentCode = "TST9009"; FullName = "Trần, ""Bình"""; Email = "tst9009@example.edu.vn"; PhoneNumber = "0900009009"; Major = "Khoa học dữ liệu"; ClassName = "SE02" }
)
Write-CsvFile (Join-Path $outputDirectory "students-mixed-errors.csv") $headers $mixedRows

$missingHeaderColumns = @("StudentCode", "FullName", "Email", "PhoneNumber", "Major")
Write-CsvFile (Join-Path $outputDirectory "students-missing-column.csv") $missingHeaderColumns @(
    [pscustomobject]@{ StudentCode = "TST9101"; FullName = "Thiếu cột"; Email = "tst9101@example.edu.vn"; PhoneNumber = "0900009101"; Major = "CNTT" }
)

foreach ($count in @(1000, 5000)) {
    $rows = for ($index = 1; $index -le $count; $index++) {
        $code = "TST{0:D5}" -f $index
        $phoneSuffix = $index.ToString("D8")
        $middle = @("Minh", "Hà", "Lan", "Quang", "Thảo")[$random.Next(0, 5)]
        [pscustomobject]@{
            StudentCode = $code
            FullName = "Nguyễn $middle Test $index"
            Email = "bulk$index@example.edu.vn"
            PhoneNumber = "09$phoneSuffix"
            Major = @("Công nghệ thông tin", "Khoa học dữ liệu", "Kế toán")[$random.Next(0, 3)]
            ClassName = "TST-CNTT-{0:D2}" -f (($index % 30) + 1)
        }
    }
    Write-CsvFile (Join-Path $outputDirectory "students-large-$count.csv") $headers $rows
    if ($count -eq 5000) {
        Write-XlsxFile (Join-Path $outputDirectory "students-large-5000.xlsx") $headers $rows
    }
}

[IO.File]::WriteAllText((Join-Path $outputDirectory "students-invalid-extension.txt"), "This file is intentionally not a supported import format.`r`n")
[IO.File]::WriteAllBytes((Join-Path $outputDirectory "students-empty.csv"), [byte[]]@())

Write-Output "Seed=20261012; generated 20 valid, 9 mixed, 1 missing-header, 1,000 and 5,000 row CSVs, plus 5,000 row XLSX, invalid-extension, and empty fixtures."
