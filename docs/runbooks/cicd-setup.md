# Runbook — CI/CD Setup (Entra App + Federated Identity)

> Reproducible record of how the `datastream-books-cicd` Entra application,
> service principal, federated identity, Dataverse application user, and
> GitHub Secrets were set up. Run these steps to rebuild from scratch or to
> create a parallel set for a new environment.

## Inputs (constants)

| Value | What it is |
|---|---|
| Tenant ID | `ca800f2c-47b3-4400-8eb1-fb1db2a39a1e` |
| Subscription ID | `7d6df7e7-d474-4284-be38-ba20eec9ef7f` (Azure Subscription PRI) |
| Resource Group | `Datastream` (shared with ERP infrastructure) |
| GitHub repo | `ryanm-plastic-recycling/datastream-books` |
| Dataverse env (dev) | `https://booksdev.crm.dynamics.com/` (PRI-Books-Dev) |
| App display name | `datastream-books-cicd` |

## Step 1 — Azure CLI prerequisites

Make sure Azure CLI is installed and logged into the right tenant + subscription.

```powershell
az --version             # expect 2.85+
az login                 # if not already
az account set --subscription "7d6df7e7-d474-4284-be38-ba20eec9ef7f"
az account show --query "{name:name, id:id, tenantId:tenantId, user:user.name}" -o json
```

Expected output: subscription `Azure Subscription PRI`, tenant
`ca800f2c-47b3-4400-8eb1-fb1db2a39a1e`.

On this machine, `az.cmd` is at `C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd`.
The system PATH points at the x86 dir which is empty — invoke via full
path or fix PATH if `az` isn't found.

## Step 2 — Create the Entra application registration

```powershell
az ad app create --display-name "datastream-books-cicd"
```

Capture the `appId` and `id` (object ID) from the response. The first
created run produced:

| Field | Value |
|---|---|
| App (client) ID | `a58747ee-f26f-4702-b911-044ee44df9a5` |
| App Object ID | `4ebf7b8b-3803-49b8-bee0-362b6a7ce681` |

Idempotency: if the app already exists, `az ad app list --display-name
"datastream-books-cicd"` returns it. Don't create a second one with the
same display name.

## Step 3 — Create the service principal

```powershell
az ad sp create --id <app-client-id>
```

For this run, `<app-client-id>` = `a58747ee-f26f-4702-b911-044ee44df9a5`.

Produced:

| Field | Value |
|---|---|
| SP Object ID | `510f68ee-1d89-46b1-bc4b-9f127d8e9f62` |

## Step 4 — Grant the SP System Administrator in PRI-Books-Dev

Done via `pac admin assign-user` (the `pac` CLI commands for Dataverse
admin work better than the Power Platform Admin Center web UI for this
because they're scriptable).

```powershell
pac auth select --name pri-books-dev
pac admin assign-user `
    --environment "https://booksdev.crm.dynamics.com/" `
    --user a58747ee-f26f-4702-b911-044ee44df9a5 `
    --application-user `
    --role "System Administrator"
```

Expected output:
```
Successfully assigned user a58747ee-f26f-4702-b911-044ee44df9a5 to environment
525fece6-185d-ecd9-b760-ae26444d7d07 with security role System Administrator
```

### If `pac admin assign-user` is unavailable (portal fallback)

1. Power Platform Admin Center → Environments → **PRI-Books-Dev**.
2. **Settings** (gear) → **Users + permissions** → **Application users**.
3. **+ New app user**.
4. **+ Add an app** → search for `datastream-books-cicd` → Add.
5. Business unit: leave as default (`PRI-Books-Dev`).
6. Security roles: **System Administrator**.
7. Create.

## Step 5 — Configure federated identity (no client secret)

Create a JSON file with the federated credential parameters:

```json
{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:ryanm-plastic-recycling/datastream-books:ref:refs/heads/main",
  "description": "GitHub Actions OIDC for main-branch deployments of Datastream Books",
  "audiences": ["api://AzureADTokenExchange"]
}
```

Apply:

```powershell
az ad app federated-credential create `
    --id a58747ee-f26f-4702-b911-044ee44df9a5 `
    --parameters @fc-main.json
```

Verify:

```powershell
az ad app federated-credential list --id a58747ee-f26f-4702-b911-044ee44df9a5 -o table
```

Produced credential id: `ee8f671d-2fe4-48c4-8f20-6e513091932d`.

### Adding more federated credentials later

For PR-triggered runs (eventually), add a credential with subject
`repo:ryanm-plastic-recycling/datastream-books:pull_request`. For other
branches, `repo:.../datastream-books:ref:refs/heads/<branch>`. Each gets
its own `name`.

## Step 6 — Add GitHub Secrets to the repo

**Status:** `gh` CLI is not installed on this machine. Set the secrets via
the GitHub web UI. (If you install `gh` later, the equivalent commands are
in the "gh CLI alternative" subsection at the end.)

### Web UI path

1. https://github.com/ryanm-plastic-recycling/datastream-books
2. **Settings** (top-right of the repo) → **Secrets and variables** (left nav) → **Actions**
3. Click **New repository secret** four times, adding each of:

   | Secret name | Value |
   |---|---|
   | `AZURE_TENANT_ID` | `ca800f2c-47b3-4400-8eb1-fb1db2a39a1e` |
   | `AZURE_CLIENT_ID` | `a58747ee-f26f-4702-b911-044ee44df9a5` |
   | `AZURE_SUBSCRIPTION_ID` | `7d6df7e7-d474-4284-be38-ba20eec9ef7f` |
   | `POWER_PLATFORM_ENVIRONMENT_URL` | `https://booksdev.crm.dynamics.com` |

4. Confirm all four appear in the list.

Repository **Variables** (not secrets) are not strictly required at the
moment. If you want to override the solution name later, add a variable
`POWER_PLATFORM_SOLUTION_NAME` = `DatastreamBooks`. The workflow falls
back to that string by default.

### gh CLI alternative

If `gh` is installed and authenticated (`gh auth status` is green):

```powershell
gh secret set AZURE_TENANT_ID            --body "ca800f2c-47b3-4400-8eb1-fb1db2a39a1e"
gh secret set AZURE_CLIENT_ID            --body "a58747ee-f26f-4702-b911-044ee44df9a5"
gh secret set AZURE_SUBSCRIPTION_ID      --body "7d6df7e7-d474-4284-be38-ba20eec9ef7f"
gh secret set POWER_PLATFORM_ENVIRONMENT_URL --body "https://booksdev.crm.dynamics.com"
```

## Step 7 — Wire the workflow to use OIDC

See `.github/workflows/deploy-dev.yml`. The relevant block:

```yaml
permissions:
  id-token: write    # required for OIDC
  contents: read

steps:
  - uses: azure/login@v2
    with:
      client-id:       ${{ secrets.AZURE_CLIENT_ID }}
      tenant-id:       ${{ secrets.AZURE_TENANT_ID }}
      subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

No client secret is referenced anywhere. The `id-token: write` permission
plus the federated credential we configured in Step 5 is what lets
`azure/login@v2` obtain a token from Entra without a stored secret.

## Step 8 — Re-enable the workflow (if disabled)

GitHub disables workflows that have been inactive for 60+ days or that
have never run successfully. If the workflow is disabled:

1. https://github.com/ryanm-plastic-recycling/datastream-books/actions
2. Left nav → **Deploy to PRI-Books-Dev**.
3. If a banner says "This scheduled workflow is disabled" or similar,
   click **Enable workflow** in the upper-right (`...` menu).
4. Optionally click **Run workflow** to trigger a manual run, or push a
   trivial commit to a `feature/*` or `main` branch.

## Step 9 — Test the workflow with a trivial commit

The first end-to-end run should:
- Authenticate via OIDC (no secret in logs)
- Build + test the plugin (passes — no business logic yet)
- Pack the solution from `solution/src` (warns: no entities, but doesn't fail)
- Authenticate `pac` to PRI-Books-Dev
- Attempt to import an empty solution (this may fail because there are no
  components — that's expected today; the goal of this test is auth, not deploy)

A clean PASS through the auth steps is success for today. The "no
components to deploy" failure surface is the next session's problem.

## Client secret (break-glass fallback to federated identity)

Federated identity (Step 5 above) is the primary auth path — no client
secret is required for the GitHub Actions OIDC flow. A client secret
exists as a break-glass path for the scenarios where federation is
unavailable (e.g., during the rotation of the federated credential
itself, or when authenticating from a context that cannot present an
OIDC token).

| Field | Value |
|---|---|
| Display name | `Key Vault and CI/CD authentication` |
| Created | 2026-05-20 |
| Expires | 2027-05-20 (12 months; calendar reminder to rotate ~2 weeks before) |
| Stored in | `kv-datastream-books` as secret `cicd-sp-client-secret` |
| Rotation procedure | See [`key-vault-management.md`](key-vault-management.md) §Rotation procedures |

The secret value lives in Key Vault only. It is never committed to the
repo, set as a GitHub secret, or stored in any developer workstation.
GitHub Actions does not consume this secret today — federated identity
covers the runner. If a future workflow needs to act outside the OIDC
context, it acquires this secret at runtime via Key Vault using its
own managed identity (TBD when that scenario arrives).

## Step 6 RBAC follow-up — Key Vault access for the SP

In addition to the Dataverse System Administrator role granted in
Step 4, the `datastream-books-cicd` SP holds **`Key Vault Secrets User`**
on `kv-datastream-books` (Vault scope). This grants `Get` on secrets
only — no `List`, no `Set`, no `Delete`, no key access. Required for
the Phase 6B `PostJournalEntryPlugin` runtime to read
`dsb-app-connection-string` at posting time.

Verify:

```powershell
$vaultId = "/subscriptions/7d6df7e7-d474-4284-be38-ba20eec9ef7f/resourceGroups/Datastream/providers/Microsoft.KeyVault/vaults/kv-datastream-books"
az role assignment list --assignee a58747ee-f26f-4702-b911-044ee44df9a5 --scope $vaultId -o table
```

## What this runbook produced (single-source-of-truth values)

| Field | Value |
|---|---|
| App display name | `datastream-books-cicd` |
| App (client) ID | `a58747ee-f26f-4702-b911-044ee44df9a5` |
| App object ID | `4ebf7b8b-3803-49b8-bee0-362b6a7ce681` |
| Service principal object ID | `510f68ee-1d89-46b1-bc4b-9f127d8e9f62` |
| Federated credential name | `github-main` |
| Federated credential subject | `repo:ryanm-plastic-recycling/datastream-books:ref:refs/heads/main` |
| Federated credential id | `ee8f671d-2fe4-48c4-8f20-6e513091932d` |
| Dataverse role granted | System Administrator on PRI-Books-Dev |
| Key Vault role granted | `Key Vault Secrets User` on `kv-datastream-books` (added 2026-05-20) |
| Client secret display name | `Key Vault and CI/CD authentication` |
| Client secret expires | 2027-05-20 (stored in Vault as `cicd-sp-client-secret`) |

## Teardown (to remove the SP and all related access)

```powershell
# Remove federated credentials
az ad app federated-credential list --id a58747ee-f26f-4702-b911-044ee44df9a5 --query "[].id" -o tsv | ForEach-Object {
    az ad app federated-credential delete --id a58747ee-f26f-4702-b911-044ee44df9a5 --federated-credential-id $_
}

# Delete the service principal (still leaves the app reg)
az ad sp delete --id 510f68ee-1d89-46b1-bc4b-9f127d8e9f62

# Delete the app registration
az ad app delete --id a58747ee-f26f-4702-b911-044ee44df9a5

# Remove the application user from Dataverse (via Admin Center; pac doesn't have a delete-app-user verb)
# Settings -> Users + permissions -> Application users -> select datastream-books-cicd -> Delete
```

## See also

- [`key-vault-management.md`](key-vault-management.md) — Vault that stores
  the SP client secret and the dsb_app connection string
- [`sql-account-management.md`](sql-account-management.md) — managing `dsb_*` accounts that eventually pair with this SP
- [`../../.github/workflows/deploy-dev.yml`](../../.github/workflows/deploy-dev.yml) — the workflow that uses these secrets
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) — auth strategy context
