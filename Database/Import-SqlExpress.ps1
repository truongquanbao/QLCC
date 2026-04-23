param(
    [string]$ServerInstance = ".\SQLEXPRESS",
    [string]$DatabaseName = "ApartmentManagerDB"
)

$ErrorActionPreference = "Stop"

function Invoke-SqlBatch {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Batch,
        [string]$Label
    )

    $trimmed = $Batch.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        return
    }

    Write-Host "Running $Label..."
    $command = $Connection.CreateCommand()
    $command.CommandTimeout = 0
    $command.CommandText = $trimmed
    [void]$command.ExecuteNonQuery()
}

function Invoke-SqlFile {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $lines = $content -split "`r?`n"
    $builder = New-Object System.Text.StringBuilder
    $batchIndex = 1

    foreach ($line in $lines) {
        if ($line.Trim().ToUpperInvariant() -eq "GO") {
            Invoke-SqlBatch -Connection $Connection -Batch $builder.ToString() -Label "$([System.IO.Path]::GetFileName($Path)) batch $batchIndex"
            $builder.Clear() | Out-Null
            $batchIndex++
            continue
        }

        [void]$builder.AppendLine($line)
    }

    Invoke-SqlBatch -Connection $Connection -Batch $builder.ToString() -Label "$([System.IO.Path]::GetFileName($Path)) batch $batchIndex"
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$createPath = Join-Path $scriptRoot "01_CreateTables.sql"
$seedPath = Join-Path $scriptRoot "02_SeedData.sql"
$verifyPath = Join-Path $scriptRoot "03_VerifySetup.sql"

$connectionString = "Server=$ServerInstance;Database=master;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"

Write-Host "Connecting to $ServerInstance..."
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()

try {
    Invoke-SqlFile -Connection $connection -Path $createPath
    Invoke-SqlFile -Connection $connection -Path $seedPath
    Invoke-SqlFile -Connection $connection -Path $verifyPath
    Write-Host "Import completed successfully."
}
finally {
    $connection.Close()
}
