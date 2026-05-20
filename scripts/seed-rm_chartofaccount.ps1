<#
.SYNOPSIS
    Seed the standard ~50-account starter Chart of Accounts into rm_chartofaccount.

.DESCRIPTION
    Loads a conventional five-section COA (Assets 1xxxx, Liabilities 2xxxx,
    Equity 3xxxx, Revenue 4xxxx, Expenses 5xxxx-7xxxx) hung off the placeholder
    "Default Operating Entity" (rm_entitycode = 'DEFAULT'). Pam will modify
    in place per decision log §23 ("Pre-populate standard COA; finance modifies").

    Two-pass insert:
        Pass 1 — parent accounts (no rm_parentaccount)
        Pass 2 — child accounts, linking rm_parentaccount to the parent's GUID

    Idempotent: each row is matched by (rm_entity = DEFAULT entity id,
    rm_accountnumber) and skipped if already present. Account types and
    categories are resolved by their stable code columns (rm_accounttypecode,
    rm_accountcategorycode), not by GUID — so this script is portable across
    environments.

    Normal balance: the column on rm_chartofaccount is an OPTIONAL override
    of the account type's normal balance. Only contra-accounts (Allowance
    for Doubtful Accounts, Accumulated Depreciation, Sales Returns &
    Allowances) and the bank-account-flagged rows have a value here in the
    starter set. All other rows inherit by convention from their account
    type at report time.

    Account-type choice values (mirror rm_accounttype.rm_normalbalance):
        Debit  = 261910000
        Credit = 261910001

.PARAMETER EnvironmentUrl
    Dataverse environment URL. Defaults to PRI-Books-Dev.

.PARAMETER EntityCode
    The rm_entitycode of the entity to attach the COA to. Defaults to
    'DEFAULT' (the placeholder seeded by seed-default-entity.ps1).

.EXAMPLE
    ./scripts/seed-rm_chartofaccount.ps1
    Seed standard COA into the DEFAULT entity in PRI-Books-Dev.

.NOTES
    Per AGENTS.md: no secrets in this script. Auth via az + the active
    Azure identity's token; no client secrets, no passwords.

    To re-seed cleanly, drop the rm_chartofaccount rows first (an inactive
    flag is preferred over delete in production, but in dev you may
    delete via the Web API). This script does NOT delete; re-running just
    skips existing rows.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateNotNullOrEmpty()]
    [string] $EnvironmentUrl = 'https://booksdev.crm.dynamics.com/',
    [ValidateNotNullOrEmpty()]
    [string] $EntityCode = 'DEFAULT'
)

$ErrorActionPreference = 'Stop'

$azCli = if ($env:AZ_CLI) { $env:AZ_CLI } elseif (Get-Command az -ErrorAction SilentlyContinue) { 'az' } else { 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd' }

Write-Host ("Target environment: " + $EnvironmentUrl) -ForegroundColor Cyan
Write-Host ("Target entity code: " + $EntityCode)     -ForegroundColor Cyan
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

# --- Resolve foreign keys by stable codes ---

$ent = Invoke-RestMethod -Uri ($api + "rm_entities?`$filter=rm_entitycode eq '$EntityCode'&`$select=rm_entityid,rm_entityname") -Headers $headers
if (-not $ent.value -or $ent.value.Count -eq 0) { throw "Entity with code '$EntityCode' not found. Run seed-default-entity.ps1 first." }
$entityId = $ent.value[0].rm_entityid
Write-Host ("Entity resolved: " + $ent.value[0].rm_entityname + " (id=" + $entityId + ")") -ForegroundColor Cyan

$types = Invoke-RestMethod -Uri ($api + 'rm_accounttypes?$select=rm_accounttypeid,rm_accounttypecode') -Headers $headers
$typeIdByCode = @{}
foreach ($t in $types.value) { $typeIdByCode[$t.rm_accounttypecode] = $t.rm_accounttypeid }
foreach ($code in @('ASSET','LIAB','EQTY','REV','EXP')) {
    if (-not $typeIdByCode.ContainsKey($code)) { throw "rm_accounttype code '$code' not found. Run seed-accounttypes.ps1 first." }
}
Write-Host ("Account types resolved: " + ($typeIdByCode.Keys -join ', ')) -ForegroundColor Cyan

# --- COA definition ---
# Normal balance: $null = inherit from type. Use the option-set value
# (261910000=Debit, 261910001=Credit) only for contra-accounts and where
# query-side efficiency justifies storing it locally.

$DEBIT  = 261910000
$CREDIT = 261910001

$coa = @(
    # ----- ASSETS (1xxxx) -----
    @{ Number='10000'; Name='Cash and Cash Equivalents';        Short='Cash & Equiv';    Type='ASSET'; Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for all cash and cash-equivalent accounts.' }
    @{ Number='10100'; Name='Cash - Operating';                 Short='Cash Op';         Type='ASSET'; Parent='10000';  IsCashBank=$true;  NormalBalance=$DEBIT; Desc='Primary operating bank account.' }
    @{ Number='10200'; Name='Cash - Savings';                   Short='Cash Sav';        Type='ASSET'; Parent='10000';  IsCashBank=$true;  NormalBalance=$DEBIT; Desc='Savings / reserve bank account.' }
    @{ Number='10300'; Name='Petty Cash';                       Short='Petty Cash';      Type='ASSET'; Parent='10000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='On-hand petty cash; not a bank account.' }

    @{ Number='11000'; Name='Accounts Receivable';              Short='AR';              Type='ASSET'; Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for AR balances.' }
    @{ Number='11100'; Name='Accounts Receivable - Trade';      Short='AR Trade';        Type='ASSET'; Parent='11000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Customer trade receivables.' }
    @{ Number='11200'; Name='Allowance for Doubtful Accounts';  Short='ADA';             Type='ASSET'; Parent='11000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Contra-asset to AR Trade for estimated uncollectibles.' }

    @{ Number='12000'; Name='Inventory';                        Short='Inv';             Type='ASSET'; Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for inventory.' }
    @{ Number='12100'; Name='Raw Materials Inventory';          Short='Raw Mat';         Type='ASSET'; Parent='12000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Raw-material inventory at cost.' }
    @{ Number='12200'; Name='Work in Progress';                 Short='WIP';             Type='ASSET'; Parent='12000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Work-in-progress inventory.' }
    @{ Number='12300'; Name='Finished Goods';                   Short='FG';              Type='ASSET'; Parent='12000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Finished-goods inventory.' }

    @{ Number='13000'; Name='Prepaid Expenses';                 Short='Prepaid';         Type='ASSET'; Parent=$null;    IsCashBank=$false; NormalBalance=$DEBIT; Desc='Prepaid insurance, rent, subscriptions, etc.' }

    @{ Number='14000'; Name='Fixed Assets';                     Short='FA';              Type='ASSET'; Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for property, plant & equipment.' }
    @{ Number='14100'; Name='Land';                             Short='Land';            Type='ASSET'; Parent='14000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Land; not depreciated.' }
    @{ Number='14200'; Name='Buildings';                        Short='Bldgs';           Type='ASSET'; Parent='14000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Buildings; depreciated.' }
    @{ Number='14300'; Name='Machinery & Equipment';            Short='M&E';             Type='ASSET'; Parent='14000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Plant machinery and equipment.' }
    @{ Number='14400'; Name='Vehicles';                         Short='Veh';             Type='ASSET'; Parent='14000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Fleet vehicles.' }
    @{ Number='14500'; Name='Furniture & Fixtures';             Short='F&F';             Type='ASSET'; Parent='14000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Office furniture and fixtures.' }
    @{ Number='14900'; Name='Accumulated Depreciation';         Short='Accum Depr';      Type='ASSET'; Parent='14000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Contra-asset to fixed assets; accumulates depreciation taken to date.' }

    @{ Number='15000'; Name='Other Assets';                     Short='Other Asset';     Type='ASSET'; Parent=$null;    IsCashBank=$false; NormalBalance=$DEBIT; Desc='Catch-all for assets not classified elsewhere.' }

    # ----- LIABILITIES (2xxxx) -----
    @{ Number='20000'; Name='Accounts Payable';                 Short='AP';              Type='LIAB';  Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for AP balances.' }
    @{ Number='20100'; Name='Accounts Payable - Trade';         Short='AP Trade';        Type='LIAB';  Parent='20000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Vendor trade payables.' }
    @{ Number='20200'; Name='Accrued Expenses';                 Short='Accrued Exp';     Type='LIAB';  Parent='20000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Expenses incurred but not yet invoiced.' }

    @{ Number='21000'; Name='Accrued Liabilities';              Short='Accrued Liab';    Type='LIAB';  Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for accrued liabilities.' }
    @{ Number='21100'; Name='Payroll Liabilities';              Short='Payroll Liab';    Type='LIAB';  Parent='21000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Wages payable plus employee/employer payroll-tax withholdings. Detail expansion will be defined by Pam.' }
    @{ Number='21200'; Name='Sales Tax Payable';                Short='Sales Tax Pay';   Type='LIAB';  Parent='21000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Sales tax collected but not yet remitted.' }

    @{ Number='22000'; Name='Short-term Debt';                  Short='ST Debt';         Type='LIAB';  Parent=$null;    IsCashBank=$false; NormalBalance=$CREDIT;Desc='Debt due within twelve months.' }

    @{ Number='23000'; Name='Long-term Debt';                   Short='LT Debt';         Type='LIAB';  Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for long-term debt.' }
    @{ Number='23100'; Name='Mortgage Payable';                 Short='Mortgage';        Type='LIAB';  Parent='23000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Real-estate mortgage liabilities.' }
    @{ Number='23200'; Name='Notes Payable';                    Short='Notes Pay';       Type='LIAB';  Parent='23000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Long-term notes payable.' }

    # ----- EQUITY (3xxxx) -----
    @{ Number='30000'; Name="Owner's Equity / Common Stock";    Short='Equity';          Type='EQTY';  Parent=$null;    IsCashBank=$false; NormalBalance=$CREDIT;Desc='Owner contributions / common stock.' }
    @{ Number='31000'; Name='Retained Earnings';                Short='RE';              Type='EQTY';  Parent=$null;    IsCashBank=$false; NormalBalance=$CREDIT;Desc='Cumulative net income from prior years.' }
    @{ Number='32000'; Name='Current Year Earnings';            Short='CY Earn';         Type='EQTY';  Parent=$null;    IsCashBank=$false; NormalBalance=$CREDIT;Desc='Clearing account for year-end close (closes into Retained Earnings).' }
    @{ Number='33000'; Name='Distributions / Dividends';        Short='Distrib';         Type='EQTY';  Parent=$null;    IsCashBank=$false; NormalBalance=$DEBIT; Desc='Owner distributions / dividends paid (contra-equity).' }

    # ----- REVENUE (4xxxx) -----
    @{ Number='40000'; Name='Operating Revenue';                Short='Op Rev';          Type='REV';   Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for operating revenue.' }
    @{ Number='40100'; Name='Product Sales';                    Short='Prod Sales';      Type='REV';   Parent='40000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Revenue from product sales.' }
    @{ Number='40200'; Name='Service Revenue';                  Short='Svc Rev';         Type='REV';   Parent='40000';  IsCashBank=$false; NormalBalance=$CREDIT;Desc='Revenue from services rendered.' }

    @{ Number='41000'; Name='Other Income';                     Short='Other Inc';       Type='REV';   Parent=$null;    IsCashBank=$false; NormalBalance=$CREDIT;Desc='Non-operating income (interest, gains on disposal, etc.).' }
    @{ Number='49000'; Name='Sales Returns and Allowances';     Short='Sales Ret';       Type='REV';   Parent=$null;    IsCashBank=$false; NormalBalance=$DEBIT; Desc='Contra-revenue for returns and allowances.' }

    # ----- EXPENSES (5xxxx-7xxxx) -----
    @{ Number='50000'; Name='Cost of Goods Sold';               Short='COGS';            Type='EXP';   Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for cost of goods sold.' }
    @{ Number='50100'; Name='COGS - Materials';                 Short='COGS Mat';        Type='EXP';   Parent='50000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Direct material cost of goods sold.' }
    @{ Number='50200'; Name='COGS - Labor';                     Short='COGS Lab';        Type='EXP';   Parent='50000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Direct labor cost of goods sold.' }
    @{ Number='50300'; Name='COGS - Overhead';                  Short='COGS OH';         Type='EXP';   Parent='50000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Allocated overhead cost of goods sold.' }

    @{ Number='60000'; Name='Operating Expenses';               Short='Op Exp';          Type='EXP';   Parent=$null;    IsCashBank=$false; NormalBalance=$null;  Desc='Parent rollup for operating expenses.' }
    @{ Number='60100'; Name='Salaries and Wages';               Short='Salaries';        Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Office and admin salaries and wages.' }
    @{ Number='60200'; Name='Rent Expense';                     Short='Rent';            Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Facilities rent expense.' }
    @{ Number='60300'; Name='Utilities';                        Short='Utilities';       Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Electricity, gas, water, telecom.' }
    @{ Number='60400'; Name='Insurance';                        Short='Insurance';       Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Insurance premiums expensed in period.' }
    @{ Number='60500'; Name='Office Supplies';                  Short='Office Sup';      Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='General office consumables.' }
    @{ Number='60600'; Name='Professional Services';            Short='Prof Svcs';       Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Legal, accounting, consulting fees.' }
    @{ Number='60700'; Name='Travel';                           Short='Travel';          Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Business travel and lodging.' }
    @{ Number='60800'; Name='Depreciation Expense';             Short='Depr Exp';        Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Depreciation expense for the period.' }
    @{ Number='60900'; Name='Bad Debt Expense';                 Short='Bad Debt';        Type='EXP';   Parent='60000';  IsCashBank=$false; NormalBalance=$DEBIT; Desc='Provision for uncollectible receivables.' }

    @{ Number='70000'; Name='Other Expenses';                   Short='Other Exp';       Type='EXP';   Parent=$null;    IsCashBank=$false; NormalBalance=$DEBIT; Desc='Non-operating expenses (interest, losses, etc.).' }
)

Write-Host ("COA rows defined: " + $coa.Count) -ForegroundColor Cyan

# --- Load existing accounts for this entity (for idempotency) ---
$existingAccts = @{}
$page = Invoke-RestMethod -Uri ($api + "rm_chartofaccounts?`$filter=_rm_entity_value eq $entityId&`$select=rm_chartofaccountid,rm_accountnumber") -Headers $headers
foreach ($a in $page.value) { $existingAccts[$a.rm_accountnumber] = $a.rm_chartofaccountid }
Write-Host ("Existing COA rows for this entity: " + $existingAccts.Count) -ForegroundColor Cyan

# --- Two-pass insert (parents first, then children) ---
function Send-Coa-Row {
    param([hashtable] $Row, [string] $ParentId)

    $body = [ordered]@{
        rm_chartofaccountname               = $Row.Name
        rm_accountnumber                    = $Row.Number
        rm_accountshort                     = $Row.Short
        rm_accountdesc                      = $Row.Desc
        'rm_accounttype@odata.bind'         = "/rm_accounttypes($($typeIdByCode[$Row.Type]))"
        'rm_entity@odata.bind'              = "/rm_entities($entityId)"
        rm_isactive                         = $true
        rm_iscashbankaccount                = [bool]$Row.IsCashBank
        rm_currency                         = 'USD'
        rm_accountlevel                     = $(if ($Row.Parent) { 1 } else { 0 })
    }
    if ($null -ne $Row.NormalBalance) { $body.rm_normalbalance = [int]$Row.NormalBalance }
    if ($ParentId)                    { $body.'rm_parentaccount@odata.bind' = "/rm_chartofaccounts($ParentId)" }

    $json = $body | ConvertTo-Json -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $created = Invoke-RestMethod -Uri ($api + 'rm_chartofaccounts') -Method POST -Headers $headers -Body $bytes
    return $created.rm_chartofaccountid
}

$inserted = 0
$skipped  = 0

# Pass 1: parent accounts (no Parent)
Write-Host "Pass 1: parent accounts" -ForegroundColor Yellow
foreach ($r in ($coa | Where-Object { -not $_.Parent })) {
    if ($existingAccts.ContainsKey($r.Number)) {
        Write-Host ("[skip] " + $r.Number + ' ' + $r.Name) -ForegroundColor DarkGray
        $skipped++; continue
    }
    if (-not $PSCmdlet.ShouldProcess($r.Number + ' ' + $r.Name, 'POST rm_chartofaccounts')) { continue }
    $id = Send-Coa-Row -Row $r -ParentId $null
    $existingAccts[$r.Number] = $id
    Write-Host ("[+]    " + $r.Number + ' ' + $r.Name) -ForegroundColor Cyan
    $inserted++
}

# Pass 2: child accounts
Write-Host "Pass 2: child accounts" -ForegroundColor Yellow
foreach ($r in ($coa | Where-Object { $_.Parent })) {
    if ($existingAccts.ContainsKey($r.Number)) {
        Write-Host ("[skip] " + $r.Number + ' ' + $r.Name) -ForegroundColor DarkGray
        $skipped++; continue
    }
    $parentId = $existingAccts[$r.Parent]
    if (-not $parentId) { Write-Warning ("Parent " + $r.Parent + " for " + $r.Number + " not found; skipping."); continue }
    if (-not $PSCmdlet.ShouldProcess($r.Number + ' ' + $r.Name, 'POST rm_chartofaccounts')) { continue }
    $id = Send-Coa-Row -Row $r -ParentId $parentId
    $existingAccts[$r.Number] = $id
    Write-Host ("[+]    " + $r.Number + ' ' + $r.Name + ' (parent ' + $r.Parent + ')') -ForegroundColor Cyan
    $inserted++
}

Write-Host ""
Write-Host ("Inserted: " + $inserted + ', Skipped (already present): ' + $skipped + ', Total defined: ' + $coa.Count) -ForegroundColor Green
