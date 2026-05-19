<#
.SYNOPSIS
    Switch the active pac CLI auth profile, defaulting to pri-books-dev.

.DESCRIPTION
    Per AGENTS.md, pri-books-dev is the default development target. This script
    selects an existing pac auth profile and verifies the resulting connection
    with `pac org who`. It does NOT create profiles unless -Create is set.

    Use this whenever you switch between Datastream Books work, ERP work,
    or production-read tasks.

.PARAMETER Profile
    Name of the pac auth profile to activate. One of:
      pri-books-dev   (default — Datastream Books dev sandbox)
      pri-books       (Datastream Books production — managed; READ-ONLY by convention)
      pri-datastream  (ERP prod, integration testing only)
      pri-dev         (ERP sandbox)

.PARAMETER Create
    Create the named profile if it does not exist. Interactive — opens the browser
    for sign-in. Cannot be combined with -WhatIf in a meaningful way (the create
    itself is interactive).

.EXAMPLE
    ./scripts/auth-env.ps1
    Activate pri-books-dev.

.EXAMPLE
    ./scripts/auth-env.ps1 -Profile pri-books
    Switch to prod (read-only by convention — see AGENTS.md).

.EXAMPLE
    ./scripts/auth-env.ps1 -Profile pri-books-dev -Create
    Create the profile if missing, then activate it.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('pri-books-dev', 'pri-books', 'pri-datastream', 'pri-dev')]
    [string] $Profile = 'pri-books-dev',

    [switch] $Create
)

$ErrorActionPreference = 'Stop'

# Map profile name -> environment URL (kept here, NOT hardcoded in callers)
$script:EnvironmentUrls = @{
    'pri-books-dev'  = 'https://booksdev.crm.dynamics.com/'
    'pri-books'      = 'https://books.crm.dynamics.com/'
    'pri-datastream' = 'https://datastream.crm.dynamics.com/'
    'pri-dev'        = 'https://pridev.crm.dynamics.com/'
}

function Test-PacInstalled {
    $cmd = Get-Command pac -ErrorAction SilentlyContinue
    if (-not $cmd) {
        Write-Error 'pac CLI not found. Run scripts/setup-dev.ps1 -Install first.'
    }
}

function Get-PacProfileExists {
    param([string] $Name)
    $list = pac auth list 2>&1 | Out-String
    return ($list -match "\b$([regex]::Escape($Name))\b")
}

Test-PacInstalled

if (-not (Get-PacProfileExists -Name $Profile)) {
    if (-not $Create) {
        Write-Error "Auth profile '$Profile' does not exist. Re-run with -Create to create it interactively."
    }

    $url = $script:EnvironmentUrls[$Profile]
    if (-not $url) {
        Write-Error "No environment URL mapping for '$Profile'."
    }

    if ($PSCmdlet.ShouldProcess($Profile, "pac auth create (interactive sign-in to $url)")) {
        Write-Host "Creating auth profile '$Profile' targeting $url ..." -ForegroundColor Yellow
        pac auth create --name $Profile --environment $url
    } else {
        return
    }
}

if ($PSCmdlet.ShouldProcess($Profile, 'pac auth select')) {
    pac auth select --name $Profile | Out-Null
    Write-Host ''
    Write-Host "Active profile: $Profile" -ForegroundColor Cyan
    pac org who
}
