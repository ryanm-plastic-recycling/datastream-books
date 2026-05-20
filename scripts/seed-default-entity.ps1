<#
.SYNOPSIS
    Seed the placeholder "Default Operating Entity" row in rm_entity.

.DESCRIPTION
    Pre-Phase-4 placeholder entity that lets the standard Chart of Accounts
    hang off something real until finance defines the actual legal-entity
    inventory (executive questionnaire §1).

    The row uses Code = 'DEFAULT' so it is visibly distinct from a real
    finance-blessed entity. When the real entity list lands, finance can
    either rename this row in place or re-parent the COA rows to a new
    rm_entity record and inactivate this one.

    Idempotent: existing rows (matched by rm_entitycode = 'DEFAULT') are
    skipped.

    Choice values for rm_entitytype (from solution/src/Entities/rm_entity/Entity.xml):
        Operating   = 261910000
        Real Estate = 261910001
        Holding     = 261910002
        Other       = 261910003

.PARAMETER EnvironmentUrl
    Dataverse environment URL. Defaults to PRI-Books-Dev.

.EXAMPLE
    ./scripts/seed-default-entity.ps1
    Seed PRI-Books-Dev.

.NOTES
    Per AGENTS.md: no secrets in this script. Auth via az + the active
    Azure identity's token; no client secrets, no passwords.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateNotNullOrEmpty()]
    [string] $EnvironmentUrl = 'https://booksdev.crm.dynamics.com/'
)

$ErrorActionPreference = 'Stop'

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
    Prefer             = 'return=representation'
}

# Idempotency check by rm_entitycode (the stable business identifier)
$existing = Invoke-RestMethod -Uri ($api + "rm_entities?`$filter=rm_entitycode eq 'DEFAULT'&`$select=rm_entityid,rm_entityname,rm_entitycode") -Headers $headers
if ($existing.value -and $existing.value.Count -gt 0) {
    $r = $existing.value[0]
    Write-Host ("[skip] DEFAULT already present (" + $r.rm_entityname + ", id=" + $r.rm_entityid + ")") -ForegroundColor DarkGray
    return
}

if (-not $PSCmdlet.ShouldProcess('Default Operating Entity', 'POST rm_entities')) { return }

$body = @{
    rm_entityname          = 'Default Operating Entity'
    rm_entitycode          = 'DEFAULT'
    rm_entityshort         = 'Default'
    rm_entitydesc          = 'Placeholder entity created before the real legal-entity inventory is finalized. Finance will either rename this in place or re-parent the COA to the real entity rows and inactivate this one.'
    rm_entitytype          = 261910000   # Operating
    rm_isactive            = $true
    rm_fiscalyearendmonth  = 12
    rm_fiscalyearendday    = 31
} | ConvertTo-Json -Compress
$bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
$created = Invoke-RestMethod -Uri ($api + 'rm_entities') -Method POST -Headers $headers -Body $bytes
Write-Host ("[+] Default Operating Entity created (id=" + $created.rm_entityid + ")") -ForegroundColor Cyan
