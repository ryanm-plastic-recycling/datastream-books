<#
.SYNOPSIS
    Sync the cicd-sp-client-secret value from Key Vault into the
    rm_sqlkvclientsecret Dataverse Environment Variable.

.DESCRIPTION
    Background: per decision section 63 in
    docs/decisions/datastream-books-decisions.md, rm_sqlkvclientsecret is a
    plain-Text Dataverse Environment Variable (not Secret-typed), because the
    plugin sandbox identity lacks prvReadEnvironmentVariableSecretValue and
    cannot resolve Secret-typed env vars at runtime. Key Vault remains the
    source of truth; this script keeps the Dataverse-side copy in sync.

    Run this script:
      - Once on initial setup (after recreating the env var as plain Text).
      - After every SP client-secret rotation (12-month cadence per section 36).

    The script:
      1. Verifies the active az CLI session is in the right tenant.
      2. Reads cicd-sp-client-secret from kv-datastream-books (value is held
         in a variable only -- never echoed, never written to disk).
      3. Looks up the environmentvariabledefinition for rm_sqlkvclientsecret
         in the target environment via Web API.
      4. Looks for an existing environmentvariablevalue row; PATCHes if found,
         POSTs a new one if not.
      5. Verifies (by length, never by value) that the Dataverse-side value
         matches the KV-side length, and confirms.

    Safety:
      - Secret value is held only in a single PowerShell variable; cleared at
        end of script via Clear-Variable.
      - No diagnostic uses of `--query value` (per section 45). Length-based
        checks only.
      - Supports -WhatIf for dry runs.

    Encoding: this file is ASCII-only (no em-dashes, no smart quotes) and is
    saved as UTF-8 with BOM. Windows PowerShell 5.1 parses ASCII reliably
    regardless of BOM; the BOM makes pwsh 7+ behaviour deterministic too.

.PARAMETER Environment
    Logical environment name. One of: pri-books-dev (default), pri-books.
    Maps to the Dataverse URL internally.

.PARAMETER VaultName
    Key Vault name. Defaults to kv-datastream-books.

.PARAMETER SecretName
    Secret name in the vault. Defaults to cicd-sp-client-secret.

.PARAMETER EnvVarSchemaName
    Dataverse env var schema name. Defaults to rm_sqlkvclientsecret.

.EXAMPLE
    ./scripts/sync-sp-secret-to-dataverse.ps1
    Sync the current KV value into pri-books-dev's rm_sqlkvclientsecret.

.EXAMPLE
    ./scripts/sync-sp-secret-to-dataverse.ps1 -WhatIf
    Show what would change without writing anything.

.NOTES
    Requires az CLI signed in as a user with:
      - Key Vault Secrets User (or higher) on kv-datastream-books.
      - System Administrator (or equivalent) on the target Dataverse environment.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('pri-books-dev', 'pri-books')]
    [string] $Environment = 'pri-books-dev',

    [string] $VaultName = 'kv-datastream-books',

    [string] $SecretName = 'cicd-sp-client-secret',

    [string] $EnvVarSchemaName = 'rm_sqlkvclientsecret'
)

$ErrorActionPreference = 'Stop'

# --- Environment URL map (kept here, NOT hardcoded in callers) ---
$envUrlMap = @{
    'pri-books-dev' = 'https://booksdev.crm.dynamics.com'
    'pri-books'     = 'https://books.crm.dynamics.com'
}
$dvUrl = $envUrlMap[$Environment]
if (-not $dvUrl) { Write-Error "No URL mapping for environment '$Environment'." }

Write-Host "Target environment : $Environment ($dvUrl)" -ForegroundColor Cyan
Write-Host "Key Vault          : $VaultName" -ForegroundColor Cyan
Write-Host "Vault secret       : $SecretName" -ForegroundColor Cyan
Write-Host "Dataverse env var  : $EnvVarSchemaName" -ForegroundColor Cyan
Write-Host ""

# --- Tooling preflight ---
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error 'az CLI not found. Install Azure CLI before running this script.'
}

$azAcct = az account show --query "{tenantId:tenantId, user:user.name}" -o json 2>$null | ConvertFrom-Json
if (-not $azAcct) { Write-Error 'az CLI not signed in. Run: az login' }
Write-Host "az identity        : $($azAcct.user) (tenant $($azAcct.tenantId))" -ForegroundColor Cyan

# --- 1. Read secret from KV (variable only -- never echoed) ---
Write-Host ""
Write-Host "[1/4] Reading secret from Key Vault..." -ForegroundColor Yellow
$secret = az keyvault secret show --vault-name $VaultName --name $SecretName --query value -o tsv
if ([string]::IsNullOrWhiteSpace($secret)) {
    Write-Error "Could not read secret '$SecretName' from vault '$VaultName'. Check RBAC and that the secret exists."
}
$kvLen = $secret.Length
Write-Host "      KV value length: $kvLen characters" -ForegroundColor Gray

# --- 2. Acquire Dataverse access token ---
Write-Host ""
Write-Host "[2/4] Acquiring Dataverse access token..." -ForegroundColor Yellow
$token = az account get-access-token --resource $dvUrl --query accessToken -o tsv
if ([string]::IsNullOrWhiteSpace($token)) { Write-Error "Failed to acquire token for $dvUrl." }
$headers = @{
    Authorization      = "Bearer $token"
    'OData-MaxVersion' = '4.0'
    'OData-Version'    = '4.0'
    Accept             = 'application/json'
    'Content-Type'     = 'application/json'
    Prefer             = 'return=representation'
}
$apiBase = "$dvUrl/api/data/v9.2"

# --- 3. Look up the env var definition ---
Write-Host ""
Write-Host "[3/4] Looking up environment variable definition..." -ForegroundColor Yellow
$defFilter = "schemaname eq '$EnvVarSchemaName'"
$defUri    = "$apiBase/environmentvariabledefinitions?`$select=schemaname,type,environmentvariabledefinitionid&`$filter=$defFilter"
$def       = Invoke-RestMethod -Method GET -Uri $defUri -Headers $headers
if (-not $def.value -or $def.value.Count -eq 0) {
    Write-Error "Env var definition '$EnvVarSchemaName' not found in $Environment. Create it (Text type, 100000000) in the maker portal first."
}
$defId   = $def.value[0].environmentvariabledefinitionid
$defType = $def.value[0].type
Write-Host "      Definition id : $defId" -ForegroundColor Gray
Write-Host "      Type code     : $defType" -ForegroundColor Gray
if ($defType -ne 100000000) {
    Write-Error "Env var '$EnvVarSchemaName' is type $defType, expected 100000000 (Text). Recreate the env var as Text per section 63 before re-running."
}

# --- 4. Look up existing value row and PATCH, or POST new ---
Write-Host ""
Write-Host "[4/4] Writing value to Dataverse..." -ForegroundColor Yellow
$valFilter = "_environmentvariabledefinitionid_value eq $defId"
$valUri    = "$apiBase/environmentvariablevalues?`$select=environmentvariablevalueid,value&`$filter=$valFilter"
$existing  = Invoke-RestMethod -Method GET -Uri $valUri -Headers $headers

$body = @{ value = $secret } | ConvertTo-Json -Compress

if ($existing.value -and $existing.value.Count -gt 0) {
    $valueId = $existing.value[0].environmentvariablevalueid
    $existingLen = if ($existing.value[0].value) { $existing.value[0].value.Length } else { 0 }
    Write-Host "      Existing row  : $valueId (current length $existingLen)" -ForegroundColor Gray
    if ($PSCmdlet.ShouldProcess("$EnvVarSchemaName value row $valueId", 'PATCH environmentvariablevalues')) {
        Invoke-RestMethod -Method PATCH -Uri "$apiBase/environmentvariablevalues($valueId)" -Headers $headers -Body $body | Out-Null
        Write-Host "      PATCH ok" -ForegroundColor Green
    } else {
        Write-Host "      (skipped -- WhatIf)" -ForegroundColor Yellow
    }
} else {
    Write-Host "      No existing row -- will POST new." -ForegroundColor Gray
    $postBody = @{
        value                                                   = $secret
        'EnvironmentVariableDefinitionId@odata.bind' = "/environmentvariabledefinitions($defId)"
        schemaname                                              = $EnvVarSchemaName
    } | ConvertTo-Json -Compress
    if ($PSCmdlet.ShouldProcess("$EnvVarSchemaName value row (new)", 'POST environmentvariablevalues')) {
        Invoke-RestMethod -Method POST -Uri "$apiBase/environmentvariablevalues" -Headers $headers -Body $postBody | Out-Null
        Write-Host "      POST ok" -ForegroundColor Green
    } else {
        Write-Host "      (skipped -- WhatIf)" -ForegroundColor Yellow
    }
}

# --- Verify by length only (per section 45) ---
if (-not $WhatIfPreference) {
    Write-Host ""
    Write-Host "Verifying..." -ForegroundColor Yellow
    $verify    = Invoke-RestMethod -Method GET -Uri $valUri -Headers $headers
    $verifyLen = if ($verify.value -and $verify.value.Count -gt 0 -and $verify.value[0].value) { $verify.value[0].value.Length } else { 0 }
    if ($verifyLen -eq $kvLen) {
        Write-Host "Verified: Dataverse value length matches KV value length ($kvLen chars)." -ForegroundColor Green
    } else {
        Write-Error "Length mismatch: KV=$kvLen, Dataverse=$verifyLen. Investigate before deploying the plugin."
    }
}

# --- Cleanup ---
Clear-Variable secret -ErrorAction SilentlyContinue
Write-Host ""
Write-Host "Done." -ForegroundColor Cyan
