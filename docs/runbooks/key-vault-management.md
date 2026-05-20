# Runbook — Key Vault Management

> Single source of truth for `kv-datastream-books`. Covers the Vault's
> configuration, RBAC, diagnostic logging, secret inventory, rotation
> procedures, and break-glass recovery.

## Vault

| Field | Value |
|---|---|
| Vault name | `kv-datastream-books` |
| Resource ID | `/subscriptions/7d6df7e7-d474-4284-be38-ba20eec9ef7f/resourceGroups/Datastream/providers/Microsoft.KeyVault/vaults/kv-datastream-books` |
| Vault URI | `https://kv-datastream-books.vault.azure.net/` |
| Subscription | Azure Subscription PRI (`7d6df7e7-d474-4284-be38-ba20eec9ef7f`) |
| Tenant | Plastic Recycling (`ca800f2c-47b3-4400-8eb1-fb1db2a39a1e`) |
| Resource group | `Datastream` (shared with ERP infrastructure) |
| Region | East US |
| SKU | standard |
| Permission model | Azure RBAC (not legacy access policies) |
| Soft-delete | Enabled, 90-day retention |
| Purge protection | Enabled (irreversible) |
| Public network access | Enabled |
| Network default action | **Allow** (changed 2026-05-20 from Deny — see Firewall configuration below) |
| Network bypass | `AzureServices` (retained as defense-in-depth) |
| Network IP allow-list | `12.201.35.226/32` (ryanm dev — retained but informational while defaultAction=Allow) |
| Provisioned | 2026-05-20, by `ryanm@plastic-recycling.net` |

### Configuration decisions

- **Azure RBAC, not access policies.** RBAC is the modern Microsoft-recommended
  permission model — assignments flow through the same role-assignment
  surface as the rest of Azure, and least-privilege roles like
  `Key Vault Secrets User` (read-only on secrets) don't exist in the legacy
  access-policy model.
- **Purge protection enabled.** Cannot be turned off later. Mitigates an
  attacker (or an accidental destructive operation) deleting the Vault and
  then immediately purging it before the 90-day soft-delete window can save
  us. The trade-off is that the Vault and any deleted secrets cannot be
  fully removed until the soft-delete retention elapses.

### Firewall configuration (dev) — `defaultAction = Allow`

**As of 2026-05-20 the dev Vault firewall is set to `defaultAction = Allow`
with `bypass = AzureServices`.** The IP allow-list (`12.201.35.226/32`) is
retained but is informational while `defaultAction = Allow`.

**Why we moved off `Deny + IP allow-list` in dev:**

Power Platform's native Key Vault integration (the path that backs
Secret-type Dataverse Environment Variables) resolves the secret on
behalf of the Dataverse environment. The resolver runs from a fleet of
Microsoft-managed IPs that change over time and are not exposed as a
stable allow-list we can pin. With `defaultAction = Deny`, every attempt
to read a Secret-type env var failed with HTTP 403 (Forbidden) because
the resolver's source IP was not in our allow-list.

Two viable resolutions:
1. **Allow + RBAC** (chosen for dev) — flip `defaultAction` to `Allow`,
   keep `bypass = AzureServices`, and rely on Azure RBAC at vault scope
   as the sole access-control surface. This is the documented Microsoft
   path for Power Platform KV integration in non-prod environments.
2. **Private Endpoint** (planned for production) — replace public access
   entirely with a VNet-integrated private endpoint that the Dataverse
   environment joins via Power Platform's enterprise integration features.
   Heavier setup; right answer for prod.

`bypass = AzureServices` is kept so Microsoft-internal services (the
Power Platform resolver, audit logging pipelines, Defender for Cloud)
continue to function. RBAC remains the actual access control: even with
the firewall open, principals without `Key Vault Secrets User` (or
higher) at vault scope are still denied by RBAC.

**Production guidance.** The prod Vault (when provisioned at cutover)
will use Private Endpoint, NOT the dev `Allow + RBAC` shortcut. The
prod runbook must document the VNet, subnet, and Dataverse
enterprise-policy linkage. Note this as a deliberate dev/prod divergence
in `security-model.md`.

**Verification commands:**

```powershell
az keyvault show --name kv-datastream-books `
  --query "{defaultAction:properties.networkAcls.defaultAction, bypass:properties.networkAcls.bypass, ipRules:properties.networkAcls.ipRules}" -o json
```

Expected:
```json
{
  "bypass": "AzureServices",
  "defaultAction": "Allow",
  "ipRules": [ { "value": "12.201.35.226/32" } ]
}
```

## RBAC

| Principal | Role | Scope | Purpose |
|---|---|---|---|
| `ryanm@plastic-recycling.net` (object id `46ae0175-c6d7-473b-8d6d-1e90b99c194b`) | `Key Vault Administrator` | Vault | Full control during setup, rotation, and break-glass. Reviewed quarterly. |
| `datastream-books-cicd` SP (object id `510f68ee-1d89-46b1-bc4b-9f127d8e9f62`, app id `a58747ee-f26f-4702-b911-044ee44df9a5`) | `Key Vault Secrets User` | Vault | `Get` permission on secrets only. Used by the Phase 6B plugin runtime (and CI/CD) to read the dsb_app connection string at runtime. |
| **Dataverse SP** (object id `567ae524-268d-4de9-8054-7e26da9fa7f0`) | `Key Vault Secrets User` | Vault | Granted as part of Power Platform Key Vault integration troubleshooting. May not be strictly necessary — the Resource Provider SP below is the one that actually resolves Secret-type env vars — but is retained as defense-in-depth. |
| **Dataverse Resource Provider SP** (object id `4f026a85-a88e-4674-baf4-45833854f411`) | `Key Vault Secrets User` | Vault | **The SP that actually handles Secret-type Dataverse Environment Variable resolution.** Without this grant, attempts to save a Secret-type env var pointing at a Key Vault secret fail with HTTP 403. Documented Microsoft prerequisite for Power Platform → Key Vault integration. |

No standing grants beyond the four above. RBAC propagation typically takes
5–10 minutes after assignment; do not perform retrieval tests immediately
after an assignment changes.

### Dataverse → Key Vault integration prerequisites

For Secret-type Dataverse Environment Variables that reference a Key
Vault secret to save and resolve correctly:

1. **Vault firewall** must allow the Dataverse resolver's traffic. In
   dev this is achieved via `defaultAction = Allow` (see Firewall
   section above). In prod, via Private Endpoint joined to a Dataverse
   enterprise policy.
2. **Both Dataverse SPs** (the standard Dataverse SP `567ae524-…` and
   the Dataverse Resource Provider SP `4f026a85-…`) must hold
   `Key Vault Secrets User` (or higher) at vault scope. In practice the
   Resource Provider SP is the one that authenticates when the maker
   portal saves a Secret-type env var; granting only the standard
   Dataverse SP is **not** sufficient.
3. The user **creating** the env var must hold a role that allows
   reading the underlying Key Vault secret (e.g. `Key Vault Secrets
   User` or `Key Vault Administrator`). Without this, the maker portal
   fails the save with a confusing 403 attributed to the *user*, not
   the Dataverse SP.

If any of the three is missing, the symptom is the same: maker-portal
"Save" of a Secret-type env var pointing at the Vault returns 403 and
the env var either does not get created or gets created as a String
type with no value.

Verify the current state:

```powershell
$vaultId = "/subscriptions/7d6df7e7-d474-4284-be38-ba20eec9ef7f/resourceGroups/Datastream/providers/Microsoft.KeyVault/vaults/kv-datastream-books"
az role assignment list --scope $vaultId --query "[].{principal:principalName, type:principalType, role:roleDefinitionName}" -o table
```

## Diagnostic logging

| Field | Value |
|---|---|
| Diagnostic setting name | `kv-datastream-books-diagnostics` |
| Destination | Log Analytics workspace `7d6df7e7-d474-4284-be38-ba20eec9ef7f-Datastream-EUS` (Datastream RG, East US) |
| Workspace retention | 30 days (dev; raise to ≥365 days before prod) |
| Logs routed | `AuditEvent` |
| Metrics routed | `AllMetrics` |

Every secret read, write, list, delete, RBAC change, and access denial
lands in `AzureDiagnostics` in the workspace. Auditor-defensibility relies
on this being on continuously — verify periodically.

Verify:

```powershell
az monitor diagnostic-settings list --resource $vaultId -o table
```

Quick sanity query (after a few minutes of activity):

```kql
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.KEYVAULT"
| where Resource =~ "kv-datastream-books"
| project TimeGenerated, OperationName, identity_claim_upn_s, requestUri_s, ResultSignature, CallerIPAddress
| order by TimeGenerated desc
| take 50
```

## Secret inventory

| Name | Content type | Purpose | Owner | Rotation cadence | Consumed by |
|---|---|---|---|---|---|
| `dsb-app-connection-string` | `text/plain;charset=utf-8` | Azure SQL connection string for `dsb_app` (least-privileged ledger writer) | Ryan | Quarterly; or immediately on any suspected exposure / plugin SP compromise | Phase 6B `PostJournalEntryPlugin` (will read at runtime via managed identity once that role is in place; for now SP-based) |
| `cicd-sp-client-secret` | `text/plain;charset=utf-8` | Client secret for the `datastream-books-cicd` Entra app (app id `a58747ee-f26f-4702-b911-044ee44df9a5`). Current credential keyId `987e5ce6-934e-48db-a3d0-8972e98c7d63`, expires **2028-05-20**. | Ryan | 24 months (current credential) — calendar reminder 2028-05-06. Immediate rotation on any suspected exposure. | (1) Phase 6B `PostJournalEntryPlugin` via `rm_sqlkvclientsecret` env var → KV resolution; (2) GitHub Actions `AZURE_CLIENT_SECRET` (break-glass fallback to federated identity). Both consumers must be refreshed on rotation. |

Verify the current inventory:

```powershell
az keyvault secret list --vault-name kv-datastream-books --query "[].{name:name, enabled:attributes.enabled, contentType:contentType, tags:tags}" -o table
```

### Tags convention

| Tag | Required? | Allowed values |
|---|---|---|
| `purpose` | Yes | Short identifier, e.g. `plugin-runtime`, `service-principal-auth`, `migration` |
| `environment` | Yes | `dev`, `test`, `prod` |
| `target` | When relevant | The system the secret unlocks, e.g. `datastream-books-dev` |
| `app-id` | For SP secrets | The Entra application (client) id the secret belongs to |
| `key-id` | For SP secrets | The credential `keyId` on the app reg, so we can cross-reference Azure-side records |

## Rotation procedures

### Rotate `dsb-app-connection-string`

Single PowerShell session — the password lives in script scope only and
never lands on disk or stdout. Connection string for `priadmin` is read
from the user-scope env var `DATASTREAM_BOOKS_DEV_CONN`.

```powershell
# 1. Generate a 32-char password (alphanumeric + ! @ # $ % ^ & * - _ +)
#    — see scripts/Rotate-DsbAppPassword.ps1 (TBD) or inline the generator.

# 2. ALTER USER dsb_app WITH PASSWORD = '<pw>'
#    Connect with priadmin (env var); use System.Data.SqlClient.

# 3. Positive verification (must succeed as dsb_app):
#    - SELECT @@VERSION
#    - SELECT TOP 1 EntryId FROM ledger.GeneralLedgerEntries
#    - BEGIN TRAN; INSERT … ; ROLLBACK
#    Confirm 0 KV-PROV-TEST (or rotation-marker) rows remain after rollback.

# 4. Negative verification (must fail with SQL 229: permission denied):
#    - UPDATE ledger.GeneralLedgerEntries SET Memo = N'x' WHERE 1=0
#    - DELETE FROM ledger.GeneralLedgerEntries WHERE 1=0

# 5. If all positive pass AND both negative throw SQL 229:
#    Build ADO.NET connection string with new password,
#    write to a temp file in $env:TEMP (random name),
#    az keyvault secret set --file $tmpFile --content-type text/plain;charset=utf-8
#    delete temp file (overwrite-then-Remove-Item).

# 6. Clear the password variable, [System.GC]::Collect().

# 7. Update audit.AuditEvents (Phase 6+ when that table exists) with the
#    rotation event and a Change Request reference.
```

If any positive verification fails, or any negative verification
*succeeds*, STOP — do not store the new connection string in Vault. The
previous Vault version remains the source of truth until the rotation
is proven clean.

### Rotate `cicd-sp-client-secret`

```powershell
# 1. Append a fresh credential on the app reg (does not invalidate existing creds)
$cred = az ad app credential reset `
    --id a58747ee-f26f-4702-b911-044ee44df9a5 `
    --append `
    --display-name "Key Vault and CI/CD authentication" `
    --years 1 `
    --only-show-errors `
    --output json 2>$null | Out-String | ConvertFrom-Json

# 2. Store the new secret in Vault (overwrites the current version; old
#    version stays in the version history per Key Vault default).
$tmp = Join-Path $env:TEMP ("kvsec-{0}.tmp" -f ([Guid]::NewGuid().ToString('N')))
try {
    [System.IO.File]::WriteAllText($tmp, $cred.password, [System.Text.UTF8Encoding]::new($false))
    az keyvault secret set `
        --vault-name kv-datastream-books `
        --name cicd-sp-client-secret `
        --file $tmp `
        --content-type "text/plain;charset=utf-8" `
        --tags purpose=service-principal-auth environment=dev `
               app-id=a58747ee-f26f-4702-b911-044ee44df9a5 `
               key-id=$($cred.keyId) `
        --output none
} finally {
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
}

# 3. List all credentials and identify the old keyId to delete.
az ad app credential list --id a58747ee-f26f-4702-b911-044ee44df9a5 -o table

# 4. Delete the old credential by key id.
az ad app credential delete --id a58747ee-f26f-4702-b911-044ee44df9a5 --key-id <old-keyId>

# 5. Refresh the GitHub Actions secret AZURE_CLIENT_SECRET so the
#    repo's workflows pick up the new value. Without this step the SP
#    secret in Vault is "rotated" but CI continues using the old one.
#    Web UI: https://github.com/ryanm-plastic-recycling/datastream-books/settings/secrets/actions
#    -> AZURE_CLIENT_SECRET -> Update value.
#    gh CLI alternative (if installed):
#       gh secret set AZURE_CLIENT_SECRET --body "<new value>"

# 6. Validate that any consumer of this secret can still authenticate
#    (e.g. trigger a GitHub Actions workflow run, post a JE through
#    the maker portal so the plugin exercises the new secret).
```

> **Caveat — known JSON quirk:** `az ad app credential reset` returns
> `keyId` in its JSON response, but in past runs it has come back empty.
> Always cross-check the actual `keyId` via `az ad app credential list`
> after creation, and patch the Vault tag if needed via
> `az keyvault secret set-attributes`.

> **Consumer fan-out reminder.** The SP client secret has THREE
> consumers that must stay in sync on every rotation:
>
> 1. Key Vault secret `cicd-sp-client-secret` (this Vault).
> 2. GitHub Actions repository secret `AZURE_CLIENT_SECRET` (the
>    `datastream-books` repo).
> 3. Dataverse Environment Variable `rm_sqlkvclientsecret` — Secret type
>    pointing at the Key Vault secret above; updates automatically when
>    the Vault secret is updated, **provided the resolver SPs hold
>    `Key Vault Secrets User`** at vault scope (see Dataverse → Key
>    Vault integration prerequisites above).
>
> Skipping #2 is the most common error: the Vault gets the new secret
> but CI continues authenticating with the old one until the next time
> someone notices a 401 in a workflow run.

### Safe diagnostic queries — DO NOT log raw secret values

Common diagnostic question: "is the secret populated and reachable?"
The unsafe pattern is to fetch the value and echo it (which then sits
in shell history, terminal scrollback, and any captured-output
artifact). The safe pattern is a presence-check that never returns the
value to the caller.

**Safe — secret reachable / presence check:**

```powershell
# Returns "true" or fails — never echoes the value.
$ok = az keyvault secret show --vault-name kv-datastream-books `
    --name cicd-sp-client-secret `
    --query "length(value) > `0`" -o tsv 2>$null
Write-Host "cicd-sp-client-secret reachable + non-empty: $ok"
```

**Safe — secret metadata only:**

```powershell
az keyvault secret show --vault-name kv-datastream-books `
    --name cicd-sp-client-secret `
    --query "{name:name, contentType:contentType, enabled:attributes.enabled, updated:attributes.updated, tags:tags}" -o json
```

**Unsafe — DO NOT run interactively:**

```powershell
# This echoes the secret value to the terminal. Avoid except inside a
# scoped variable that you immediately consume and then clear.
az keyvault secret show --vault-name kv-datastream-books `
    --name cicd-sp-client-secret --query value -o tsv
```

If you must read the value into a variable for an immediate operation
(rotation, programmatic consumption), assign-and-consume in a single
PowerShell block and clear the variable at the end. **Never** paste
that command into a chat or commit it to a script.

> **Why this matters.** A real incident on 2026-05-20: a diagnostic
> `--query value` exposed the SP client secret in a captured tool
> output. The credential was rotated immediately (keyId
> `987e5ce6-934e-48db-a3d0-8972e98c7d63`, expires 2028-05-20); the
> exposed credential was revoked. Treat any execution of the unsafe
> pattern above as a rotation event.

## Break-glass — recovery when secrets / SP credentials are lost

Scenarios and the recovery path for each. Order matters.

### A) SP client secret is lost (cannot retrieve from Vault, cannot authenticate)

Federated identity (OIDC from GitHub Actions on `refs/heads/main`) is the
primary auth path and does not depend on `cicd-sp-client-secret`. If
federation still works:

1. Use a federated-credential workflow run to regenerate the client
   secret via the rotation procedure above.
2. Confirm new secret is stored in Vault.
3. Delete the lost credential by `keyId` (visible in `az ad app credential list`).

If federation is *also* broken (e.g. the federated credential was
deleted by mistake):

1. Sign into Azure Portal as a tenant admin (Application Administrator
   role on the app reg, or higher).
2. Portal → Microsoft Entra ID → App registrations → `datastream-books-cicd`
   → Certificates & secrets → New client secret.
3. Copy the value once; immediately store via the rotation procedure.
4. Re-create the federated credential per [`cicd-setup.md`](cicd-setup.md)
   §Step 5.

### B) Vault itself is unreachable (firewall lockout)

1. Confirm with `az account show` that az CLI auth still works (control
   plane is independent of the data plane).
2. Get current public IP: `curl https://api.ipify.org`.
3. Add IP to the Vault allow-list (control-plane op, not blocked by
   firewall):
   ```powershell
   az keyvault network-rule add --name kv-datastream-books `
       --ip-address $(curl -s https://api.ipify.org)/32
   ```
4. Retry data-plane operation.

### C) RBAC mis-grant — locked out of own Vault

The Vault is in the `Datastream` resource group inside the PRI subscription.
ryanm holds **Owner** at subscription scope and **User Access Administrator**
at root scope, which grants the right to write RBAC assignments at any
scope including this Vault — even if the `Key Vault Administrator`
assignment is somehow lost. To recover:

```powershell
az role assignment create `
  --assignee-object-id 46ae0175-c6d7-473b-8d6d-1e90b99c194b `
  --assignee-principal-type User `
  --role "Key Vault Administrator" `
  --scope /subscriptions/7d6df7e7-d474-4284-be38-ba20eec9ef7f/resourceGroups/Datastream/providers/Microsoft.KeyVault/vaults/kv-datastream-books
```

If subscription Owner is also lost (genuine break-glass), escalate to
the tenant Global Administrator — that role can grant itself
`User Access Administrator` at tenant root scope and re-establish the
chain.

### D) Vault is accidentally deleted

Soft-delete is on with 90-day retention. Recovery:

```powershell
az keyvault recover --name kv-datastream-books --location eastus
```

This restores the Vault and *all* secrets within it. Confirm the secret
inventory matches the documented table above after recovery.

If the Vault is "purged" — soft-deleted *and* the deletion is finalized
within the 90-day window — purge protection should have prevented it. If
somehow it didn't, the secrets are irrecoverable from Azure; rotate the
underlying credentials at their source systems and re-provision.

## Quarterly review checklist

Add to the operations calendar — last business day of each calendar quarter:

- [ ] RBAC assignments at Vault scope match this runbook
- [ ] Diagnostic setting still active and routing to the documented LAW
- [ ] LAW retention sufficient for current audit horizon (≥ 90 days dev,
      ≥ 365 days prod-once-cutover)
- [ ] Soft-delete + purge protection still on (`az keyvault show`)
- [ ] Firewall IP allow-list reflects current consumers (no stale entries)
- [ ] No standing RBAC grant beyond the documented two
- [ ] Each secret's tags accurate; rotation cadence not breached
- [ ] No secret is past its rotation due date — refer to the inventory
      table above

## See also

- [`sql-account-management.md`](sql-account-management.md) — the `dsb_*`
  SQL users whose credentials live in this Vault
- [`cicd-setup.md`](cicd-setup.md) — the Entra app reg + federated
  identity that the SP secret backs up
- [`../architecture/security-model.md`](../architecture/security-model.md)
  — where Key Vault sits in the overall credential-flow architecture
- [`../architecture/immutability-validation.md`](../architecture/immutability-validation.md)
  — the priadmin bypass finding that drove the dsb_app-as-runtime-identity
  decision
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md)
  — decision record for this Vault's creation and configuration choices
