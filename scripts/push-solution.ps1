<#
.SYNOPSIS
    Pack solution/src and import the resulting zip into a Dataverse environment.

.DESCRIPTION
    Default target is pri-books-dev. NEVER targets pri-books from this script
    (production deployment must go through GitHub Actions, per AGENTS.md).

    Steps:
      1. Activate the requested pac auth profile
      2. Pack solution/src into a managed or unmanaged zip
      3. Import the zip into the target Dataverse environment
      4. Optionally publish customizations after import

.PARAMETER Profile
    pac auth profile. Allowed: pri-books-dev, pri-datastream, pri-dev.
    pri-books is intentionally not allowed — use GitHub Actions for prod.

.PARAMETER SolutionName
    Dataverse solution unique name. Default: DatastreamBooks.

.PARAMETER Managed
    Pack as managed. Default is unmanaged (the right form for dev pushes).

.PARAMETER Publish
    Run `pac solution publish` after import. Default: $true. Disable with
    -Publish:$false if you want to inspect before publishing.

.EXAMPLE
    ./scripts/push-solution.ps1
    Pack + import as unmanaged into pri-books-dev, then publish.

.EXAMPLE
    ./scripts/push-solution.ps1 -WhatIf
    Show what would happen.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('pri-books-dev', 'pri-datastream', 'pri-dev')]
    [string] $Profile = 'pri-books-dev',

    [ValidateNotNullOrEmpty()]
    [string] $SolutionName = 'DatastreamBooks',

    [switch] $Managed,

    [bool] $Publish = $true
)

$ErrorActionPreference = 'Stop'

if ($Profile -eq 'pri-books') {
    Write-Error 'Direct push to pri-books is not allowed. Production deployments go through GitHub Actions (see .github/workflows/deploy-prod.yml).'
}

$repoRoot   = Resolve-Path (Join-Path $PSScriptRoot '..')
$srcDir     = Join-Path $repoRoot 'solution/src'
$tempZip    = Join-Path $env:TEMP "${SolutionName}-push.zip"

if (-not (Test-Path $srcDir)) {
    Write-Error "Unpacked solution not found at $srcDir. Run scripts/pull-solution.ps1 first or initialize the solution."
}

& "$PSScriptRoot/auth-env.ps1" -Profile $Profile | Out-Null

if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

$packType = if ($Managed) { 'Managed' } else { 'Unmanaged' }

if ($PSCmdlet.ShouldProcess("$srcDir -> $tempZip ($packType)", "pac solution pack")) {
    Write-Host "Packing $packType solution ..." -ForegroundColor Cyan
    pac solution pack `
        --folder $srcDir `
        --zipfile $tempZip `
        --packagetype $packType
}

if ($PSCmdlet.ShouldProcess("$tempZip -> $Profile", "pac solution import")) {
    Write-Host "Importing to $Profile ..." -ForegroundColor Cyan
    pac solution import `
        --path $tempZip `
        --force-overwrite `
        --activate-plugins
}

if ($Publish -and $PSCmdlet.ShouldProcess($Profile, 'pac solution publish')) {
    Write-Host 'Publishing customizations ...' -ForegroundColor Cyan
    pac solution publish
}

if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

Write-Host 'Push complete.' -ForegroundColor Green
