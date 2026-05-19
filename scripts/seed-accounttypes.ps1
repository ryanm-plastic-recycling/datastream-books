<#
.SYNOPSIS
    Seed the five canonical Account Type rows (Asset, Liability, Equity,
    Revenue, Expense) into a Datastream Books Dataverse environment.

.DESCRIPTION
    Reference data that every environment needs. The rows are tied to the
    Account Type choice values defined in the rm_accounttype.rm_normalbalance
    column (Debit=261910000, Credit=261910001).

    Idempotent: existing rows (matched by rm_accounttypename) are skipped.

    Authentication: relies on `az account get-access-token` against the
    target environment URL. Run `pac auth select` first to set the
    profile, then this script uses az to mint a Dataverse-scoped token
    on the active Azure identity.

.PARAMETER EnvironmentUrl
    Dataverse environment URL. Defaults to PRI-Books-Dev.

.EXAMPLE
    ./scripts/seed-accounttypes.ps1
    Seed PRI-Books-Dev.

.EXAMPLE
    ./scripts/seed-accounttypes.ps1 -EnvironmentUrl 'https://books.crm.dynamics.com/'
    Seed production (only if/when authorized via Change Request).

.NOTES
    Per AGENTS.md: no secrets in this script. Auth via az + the active
    Azure identity's token; no client secrets, no passwords.

    Choice values 261910000 / 261910001 are the option-set values for
    Debit / Credit on rm_accounttype.rm_normalbalance. If those values
    are ever changed in a future migration, update this script in lockstep.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateNotNullOrEmpty()]
    [string] $EnvironmentUrl = 'https://booksdev.crm.dynamics.com/'
)

$ErrorActionPreference = 'Stop'

# Standard Phase 3 path lookup for az.cmd on the dev machine. Override via
# $env:AZ_CLI if you have it on PATH.
$azCli = if ($env:AZ_CLI) { $env:AZ_CLI } elseif (Get-Command az -ErrorAction SilentlyContinue) { 'az' } else { 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd' }

Write-Host ("Target environment: " + $EnvironmentUrl) -ForegroundColor Cyan
$tok = & $azCli account get-access-token --resource $EnvironmentUrl --query accessToken -o tsv
if ($LASTEXITCODE -ne 0 -or -not $tok) { throw "Failed to obtain Dataverse access token via az." }

$api = $EnvironmentUrl + 'api/data/v9.2/'
$headers = @{
    Authorization      = "Bearer $tok"
    Accept             = 'application/json'
    'OData-Version'    = '4.0'
    'OData-MaxVersion' = '4.0'
    'Content-Type'     = 'application/json; charset=utf-8'
}

# Canonical seed rows. Value 261910000 = Debit, 261910001 = Credit.
$rows = @(
    @{ Name = 'Asset';     Code = 'ASSET'; Short = 'Asset';  Desc = 'Assets - resources controlled by the entity.'; NormalBalance = 261910000 }
    @{ Name = 'Liability'; Code = 'LIAB';  Short = 'Liab';   Desc = 'Liabilities - obligations of the entity.';     NormalBalance = 261910001 }
    @{ Name = 'Equity';    Code = 'EQTY';  Short = 'Equity'; Desc = 'Equity - residual interest in the assets.';    NormalBalance = 261910001 }
    @{ Name = 'Revenue';   Code = 'REV';   Short = 'Rev';    Desc = 'Revenue - increases in economic benefits.';    NormalBalance = 261910001 }
    @{ Name = 'Expense';   Code = 'EXP';   Short = 'Exp';    Desc = 'Expenses - decreases in economic benefits.';   NormalBalance = 261910000 }
)

$existing = Invoke-RestMethod -Uri ($api + 'rm_accounttypes?$select=rm_accounttypename') -Headers $headers
$existingNames = @{}
foreach ($e in $existing.value) { $existingNames[$e.rm_accounttypename] = $true }

$inserted = 0
$skipped  = 0
foreach ($r in $rows) {
    if ($existingNames.ContainsKey($r.Name)) {
        Write-Host ("[skip] " + $r.Name) -ForegroundColor DarkGray
        $skipped++
        continue
    }
    if (-not $PSCmdlet.ShouldProcess($r.Name, 'POST rm_accounttypes')) { continue }
    $body = @{
        rm_accounttypename  = $r.Name
        rm_accounttypecode  = $r.Code
        rm_accounttypeshort = $r.Short
        rm_accounttypedesc  = $r.Desc
        rm_normalbalance    = $r.NormalBalance
    } | ConvertTo-Json -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
    Invoke-RestMethod -Uri ($api + 'rm_accounttypes') -Method POST -Headers $headers -Body $bytes | Out-Null
    Write-Host ("[+]    " + $r.Name + ' (NormalBalance=' + $(if ($r.NormalBalance -eq 261910000) { 'Debit' } else { 'Credit' }) + ')') -ForegroundColor Cyan
    $inserted++
}

Write-Host ""
Write-Host ("Inserted: " + $inserted + ', Skipped (already present): ' + $skipped) -ForegroundColor Green
