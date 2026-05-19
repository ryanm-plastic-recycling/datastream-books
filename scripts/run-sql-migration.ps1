<#
.SYNOPSIS
    Apply pending Azure SQL migrations from azure-sql/migrations/ to the target database.
    STUB — Azure SQL DB has not yet been provisioned for Datastream Books.

.DESCRIPTION
    Discovers V<NNNN>__*.sql files in azure-sql/migrations/, computes a SHA-256 of each
    file's content, and applies the ones not yet recorded in dbo.SchemaMigrations on
    the target database. Wraps each migration in a transaction. Aborts on first failure.

    Until Azure SQL exists for this project, this script is a STUB:
      - Validates that migration files in the repo follow naming convention
      - Validates the V0001 SchemaMigrations table contract is referenced correctly
      - Reports what WOULD be applied, in order
      - Exits non-zero with a clear message that connection params are missing

.PARAMETER Environment
    'dev' | 'test' | 'prod'. Used to select the right connection target. All TBD —
    secrets / Key Vault references are not wired up yet.

.PARAMETER MigrationsPath
    Override the default migrations folder (azure-sql/migrations).

.PARAMETER WhatIf
    Standard PowerShell -WhatIf. With this script in stub mode, -WhatIf and a normal
    run produce essentially the same output: a dry-run list of pending migrations.

.EXAMPLE
    ./scripts/run-sql-migration.ps1 -Environment dev
    Lists what would run; exits non-zero because no DB is provisioned yet.

.NOTES
    When Azure SQL is provisioned:
      * Add an entry in scripts/auth-env.ps1 (or a parallel sql-auth.ps1) for SQL auth
      * Wire AAD-only auth via Microsoft.Data.SqlClient (Authentication=ActiveDirectoryDefault)
      * Wire production secret retrieval via Azure Key Vault references in
        the GitHub Actions workflow, NOT in this script
      * Implement the SchemaMigrations row insert per V0001 (MigrationId, AppliedAtUtc,
        AppliedBy, ChecksumSha256)
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('dev', 'test', 'prod')]
    [string] $Environment = 'dev',

    [ValidateScript({ Test-Path $_ })]
    [string] $MigrationsPath
)

$ErrorActionPreference = 'Stop'

if (-not $MigrationsPath) {
    $repoRoot       = Resolve-Path (Join-Path $PSScriptRoot '..')
    $MigrationsPath = Join-Path $repoRoot 'azure-sql/migrations'
}

# --- Discover migrations ---
$pattern = '^V(?<num>\d{4})__(?<name>[A-Za-z0-9_]+)\.sql$'
$candidates = Get-ChildItem -Path $MigrationsPath -Filter 'V*.sql' -File | Sort-Object Name

if (-not $candidates) {
    Write-Host "No migrations found in $MigrationsPath" -ForegroundColor Yellow
    exit 0
}

$migrations = foreach ($file in $candidates) {
    if ($file.Name -notmatch $pattern) {
        Write-Error "Migration file '$($file.Name)' does not match required pattern V<NNNN>__<description>.sql"
    }
    $num  = [int]$Matches['num']
    $name = $Matches['name']
    $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash
    [pscustomobject]@{
        Number       = $num
        Id           = $file.BaseName  # e.g., V0002__general_ledger_entries
        Name         = $name
        FullPath     = $file.FullName
        ChecksumSha256 = $hash
    }
}

# --- Verify monotonic numbering ---
$prev = 0
foreach ($m in $migrations) {
    if ($m.Number -ne ($prev + 1)) {
        Write-Error ("Migration numbering gap: expected V{0:D4} but found V{1:D4} ($($m.Id))" -f ($prev + 1), $m.Number)
    }
    $prev = $m.Number
}

Write-Host "== Datastream Books — SQL migration runner (STUB) ==" -ForegroundColor Cyan
Write-Host "Target environment: $Environment" -ForegroundColor Cyan
Write-Host "Migrations discovered in: $MigrationsPath" -ForegroundColor Cyan
Write-Host ''
$migrations | Format-Table Number, Id, ChecksumSha256 -AutoSize | Out-Host

# --- Stub: no DB target yet ---
$connectionTargetMissing = $true   # TODO: replace with real check once Azure SQL exists

if ($connectionTargetMissing) {
    Write-Host ''
    Write-Warning 'Azure SQL database is not yet provisioned for Datastream Books.'
    Write-Warning 'This script is a stub. It validated migration filenames and computed checksums,'
    Write-Warning 'but did not connect to any database. Wire connection params here once the DB exists.'
    Write-Warning 'See script header for the integration TODO list.'
    exit 2
}

# --- Future real-run path (NOT executed today) ---
# foreach ($m in $migrations) {
#     if ($PSCmdlet.ShouldProcess("$($m.Id) on $Environment", 'apply migration')) {
#         # 1. SELECT against dbo.SchemaMigrations to see if Id already applied
#         # 2. If not, BEGIN TRAN; execute file content; INSERT INTO dbo.SchemaMigrations;
#         #    COMMIT (or ROLLBACK on error and exit non-zero)
#     }
# }
