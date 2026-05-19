<#
.SYNOPSIS
    Export the DatastreamBooks solution from a Dataverse environment and unpack
    it into solution/src for commit.

.DESCRIPTION
    Default environment is pri-books-dev (the unmanaged dev sandbox per AGENTS.md).
    This script:
      1. Verifies pac is authenticated to the target profile
      2. Exports the solution as unmanaged (default) or managed
      3. Unpacks the zip into solution/src using `pac solution unpack`
      4. Cleans up the temporary zip

    The unpacked tree is what gets committed. We never commit the zip.

.PARAMETER Profile
    pac auth profile to use. Default: pri-books-dev.

.PARAMETER SolutionName
    Dataverse solution unique name to export. Default: DatastreamBooks.

.PARAMETER Managed
    Export the managed version. Default is unmanaged (for unpacked source).
    Managed export is only meaningful for verifying what gets shipped to prod
    — not for source control.

.EXAMPLE
    ./scripts/pull-solution.ps1
    Pull the unmanaged solution from pri-books-dev into solution/src.

.EXAMPLE
    ./scripts/pull-solution.ps1 -WhatIf
    Show what would be exported / unpacked without doing it.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('pri-books-dev', 'pri-books', 'pri-datastream', 'pri-dev')]
    [string] $Profile = 'pri-books-dev',

    [ValidateNotNullOrEmpty()]
    [string] $SolutionName = 'DatastreamBooks',

    [switch] $Managed
)

$ErrorActionPreference = 'Stop'

$repoRoot   = Resolve-Path (Join-Path $PSScriptRoot '..')
$solutionDir = Join-Path $repoRoot 'solution'
$srcDir      = Join-Path $solutionDir 'src'
$tempZip     = Join-Path $env:TEMP "${SolutionName}-pull.zip"

if (-not (Test-Path $solutionDir)) {
    Write-Error "Solution folder not found at $solutionDir. Run 'pac solution init' first."
}

# Activate the requested profile
& "$PSScriptRoot/auth-env.ps1" -Profile $Profile | Out-Null

if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

$exportArgs = @(
    '--name', $SolutionName,
    '--path', $tempZip,
    '--overwrite'
)
if ($Managed) { $exportArgs += '--managed' }

if ($PSCmdlet.ShouldProcess("$SolutionName from $Profile", "pac solution export")) {
    Write-Host "Exporting $SolutionName from $Profile ..." -ForegroundColor Cyan
    pac solution export @exportArgs
}

if (-not (Test-Path $tempZip)) {
    Write-Error "Export did not produce $tempZip"
}

if ($PSCmdlet.ShouldProcess("$tempZip -> $srcDir", "pac solution unpack")) {
    Write-Host "Unpacking into $srcDir ..." -ForegroundColor Cyan
    pac solution unpack `
        --zipfile $tempZip `
        --folder  $srcDir `
        --packagetype ($Managed ? 'Managed' : 'Unmanaged') `
        --allowDelete `
        --allowWrite

    Remove-Item $tempZip -Force
    Write-Host 'Pull complete. Review changes with `git status` before committing.' -ForegroundColor Green
}
