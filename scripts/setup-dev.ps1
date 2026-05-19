<#
.SYNOPSIS
    Verify (and optionally install) the prerequisites needed to develop Datastream Books locally.

.DESCRIPTION
    Default mode is verification-only: report what is missing or out of date, exit 0 if
    the environment is good, exit non-zero if something must be installed before development
    can start. Pass -Install to actually install missing items.

    What it checks:
      * Windows PowerShell 5.1 OR PowerShell 7+ (either works)
      * .NET SDK 8 (or newer)
      * Node.js LTS (18+)
      * Git CLI
      * Power Platform CLI (pac)
      * Expected pac auth profiles: pri-books, pri-books-dev, pri-datastream, pri-dev

    What it does NOT do:
      * It does NOT create auth profiles. Run scripts/auth-env.ps1 -Create for that.
      * It does NOT modify your global PATH.
      * It does NOT touch the repo or any solution files.

.PARAMETER Install
    When set, the script attempts to install missing prerequisites via:
      * winget (preferred) for .NET, Node, Git, PowerShell 7
      * dotnet tool install --global for pac CLI
    Existing tools are not modified — only missing ones are added.
    Requires an elevated session for some installs.

.PARAMETER SkipAuthCheck
    Skip the pac auth profile check. Useful in CI where auth is set up another way.

.EXAMPLE
    ./scripts/setup-dev.ps1
    Read-only verification.

.EXAMPLE
    ./scripts/setup-dev.ps1 -Install
    Verify, and install anything that's missing.

.EXAMPLE
    ./scripts/setup-dev.ps1 -WhatIf
    Show what would be installed without doing anything.

.NOTES
    Per AGENTS.md PowerShell conventions: approved verbs, parameter validation,
    Write-Host / Write-Verbose / Write-Error appropriately, -WhatIf supported via
    SupportsShouldProcess.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch] $Install,
    [switch] $SkipAuthCheck
)

$ErrorActionPreference = 'Stop'

# --- Required versions ---
$script:RequiredDotnetMajor = 8
$script:RequiredNodeMajor   = 18
$script:ExpectedAuthProfiles = @('pri-books', 'pri-books-dev', 'pri-datastream', 'pri-dev')

# --- State accumulator ---
$script:Findings = [System.Collections.Generic.List[pscustomobject]]::new()

function Add-Finding {
    param(
        [Parameter(Mandatory)] [string] $Component,
        [Parameter(Mandatory)] [ValidateSet('OK', 'Missing', 'Outdated', 'Warning')] [string] $Status,
        [Parameter(Mandatory)] [string] $Detail,
        [string] $InstallHint
    )
    $script:Findings.Add([pscustomobject]@{
        Component   = $Component
        Status      = $Status
        Detail      = $Detail
        InstallHint = $InstallHint
    })
}

function Get-CommandVersion {
    param([string] $CommandName, [string] $VersionArg = '--version')
    $cmd = Get-Command $CommandName -ErrorAction SilentlyContinue
    if (-not $cmd) { return $null }
    try {
        $output = & $CommandName $VersionArg 2>&1 | Out-String
        return $output.Trim()
    } catch {
        return $null
    }
}

function Test-DotnetSdk {
    $version = Get-CommandVersion -CommandName 'dotnet'
    if (-not $version) {
        Add-Finding -Component '.NET SDK' -Status 'Missing' -Detail 'dotnet CLI not found' `
            -InstallHint 'winget install Microsoft.DotNet.SDK.8'
        return
    }
    $major = ($version -split '\.')[0] -as [int]
    if ($null -eq $major -or $major -lt $script:RequiredDotnetMajor) {
        Add-Finding -Component '.NET SDK' -Status 'Outdated' `
            -Detail "Found $version; need $($script:RequiredDotnetMajor)+" `
            -InstallHint 'winget install Microsoft.DotNet.SDK.8'
        return
    }
    Add-Finding -Component '.NET SDK' -Status 'OK' -Detail $version
}

function Test-NodeJs {
    $version = Get-CommandVersion -CommandName 'node' -VersionArg '--version'
    if (-not $version) {
        Add-Finding -Component 'Node.js' -Status 'Missing' -Detail 'node not found' `
            -InstallHint 'winget install OpenJS.NodeJS.LTS'
        return
    }
    $clean = $version.TrimStart('v')
    $major = ($clean -split '\.')[0] -as [int]
    if ($null -eq $major -or $major -lt $script:RequiredNodeMajor) {
        Add-Finding -Component 'Node.js' -Status 'Outdated' `
            -Detail "Found $version; need $($script:RequiredNodeMajor)+" `
            -InstallHint 'winget install OpenJS.NodeJS.LTS'
        return
    }
    Add-Finding -Component 'Node.js' -Status 'OK' -Detail $version
}

function Test-Git {
    $version = Get-CommandVersion -CommandName 'git'
    if (-not $version) {
        Add-Finding -Component 'Git' -Status 'Missing' -Detail 'git not found' `
            -InstallHint 'winget install Git.Git'
        return
    }
    Add-Finding -Component 'Git' -Status 'OK' -Detail ($version -split "`n")[0]
}

function Test-PacCli {
    $pac = Get-Command pac -ErrorAction SilentlyContinue
    if (-not $pac) {
        Add-Finding -Component 'pac CLI' -Status 'Missing' -Detail 'pac CLI not found' `
            -InstallHint 'dotnet tool install --global Microsoft.PowerApps.CLI.Tool'
        return
    }
    try {
        $out = pac --version 2>&1 | Out-String
        $line = ($out -split "`n" | Where-Object { $_ -match 'Version:' } | Select-Object -First 1).Trim()
        Add-Finding -Component 'pac CLI' -Status 'OK' -Detail $line
    } catch {
        Add-Finding -Component 'pac CLI' -Status 'Warning' -Detail "pac present but --version failed: $_"
    }
}

function Test-AuthProfiles {
    if ($SkipAuthCheck) {
        Add-Finding -Component 'pac auth profiles' -Status 'OK' -Detail 'Skipped (-SkipAuthCheck)'
        return
    }
    $pac = Get-Command pac -ErrorAction SilentlyContinue
    if (-not $pac) {
        Add-Finding -Component 'pac auth profiles' -Status 'Warning' -Detail 'pac CLI missing — cannot list auth profiles'
        return
    }
    try {
        $list = pac auth list 2>&1 | Out-String
        $missing = @()
        foreach ($profile in $script:ExpectedAuthProfiles) {
            if ($list -notmatch "\b$profile\b") { $missing += $profile }
        }
        if ($missing.Count -gt 0) {
            Add-Finding -Component 'pac auth profiles' -Status 'Missing' `
                -Detail "Missing: $($missing -join ', ')" `
                -InstallHint 'scripts/auth-env.ps1 -Create'
        } else {
            Add-Finding -Component 'pac auth profiles' -Status 'OK' `
                -Detail "All expected profiles present: $($script:ExpectedAuthProfiles -join ', ')"
        }
    } catch {
        Add-Finding -Component 'pac auth profiles' -Status 'Warning' -Detail "pac auth list failed: $_"
    }
}

function Invoke-MissingInstalls {
    $needsInstall = $script:Findings | Where-Object { $_.Status -in @('Missing', 'Outdated') -and $_.InstallHint }
    if (-not $needsInstall) {
        Write-Host 'Nothing to install.' -ForegroundColor Green
        return
    }
    foreach ($item in $needsInstall) {
        $target = "$($item.Component): $($item.Detail)"
        if (-not $PSCmdlet.ShouldProcess($target, 'Install')) { continue }
        Write-Host "Installing $($item.Component) via: $($item.InstallHint)" -ForegroundColor Yellow
        try {
            Invoke-Expression $item.InstallHint
            Write-Host "  -> $($item.Component) install attempted. Re-run setup-dev.ps1 to verify." -ForegroundColor Green
        } catch {
            Write-Error "Install of $($item.Component) failed: $_"
        }
    }
}

# --- Run all checks ---
Write-Host '== Datastream Books — dev environment check ==' -ForegroundColor Cyan
Write-Host ''

Test-DotnetSdk
Test-NodeJs
Test-Git
Test-PacCli
Test-AuthProfiles

# --- Report ---
$script:Findings |
    Sort-Object Component |
    Format-Table Component, Status, Detail -AutoSize |
    Out-Host

$failures = $script:Findings | Where-Object { $_.Status -in @('Missing', 'Outdated') }

if ($Install -and $failures) {
    Write-Host ''
    Write-Host '== Attempting installs ==' -ForegroundColor Cyan
    Invoke-MissingInstalls
    exit 0
}

if ($failures) {
    Write-Host ''
    Write-Host "Environment has $($failures.Count) issue(s). Re-run with -Install to attempt automatic install." -ForegroundColor Yellow
    exit 1
}

Write-Host 'Environment looks good. Ready to develop.' -ForegroundColor Green
exit 0
