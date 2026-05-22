# Credential Access Design — Plugin → Key Vault → Azure SQL

> How `PostJournalEntryPlugin` acquires its Azure SQL connection string
> at runtime. Phase 6B (2026-05-21).
>
> **AMENDED 2026-05-21 per decision §63.** The SP client secret is now
> delivered to the plugin via a **plain-Text** Dataverse Environment
> Variable (`rm_sqlkvclientsecret`), NOT a Secret-type env var as this
> document originally described. Reason: the Dataverse plugin sandbox
> identity does not hold `prvReadEnvironmentVariableSecretValue`, so
> Secret-type resolution returns `0x80040256 Access Denied` regardless
> of payload shape (pinned by plugin assemblies 1.0.0.2 and 1.0.0.3
> failed validations). Key Vault remains the source of truth for the
> underlying SP client secret -- only the Dataverse-side delivery
> mechanism changed. The script
> [`../../scripts/sync-sp-secret-to-dataverse.ps1`](../../scripts/sync-sp-secret-to-dataverse.ps1)
> is the only sanctioned writer of the `rm_sqlkvclientsecret` Text
> value, run after each Vault rotation. See §63 of the decision log
> for full rationale. Sections below are annotated with **[§63]** where
> the original Secret-type narrative has been superseded.

## Chosen pattern

**Plugin reads the connection string from Azure Key Vault at runtime,
authenticating as the `datastream-books-cicd` service principal.**

The SP's client secret is stored as a **plain-Text Dataverse
Environment Variable [§63 -- originally Secret type, see amendment
banner above]**; the tenant id, client id, vault URL, and secret name
are also plain-text Dataverse Environment Variables. The plugin reads
all five, exchanges them for an Azure AD bearer token, calls the Key
Vault REST API for the connection string, and caches the result in a
process-static field with a 5-minute TTL.

## Why this pattern (and not the alternatives)

Three viable paths were considered. Two were rejected.

| Option | Verdict | Why |
|---|---|---|
| **A. Plugin → KV via SP client secret (chosen)** | Adopted | Key Vault remains single source of truth; rotations in KV propagate to plugins within the 5-minute TTL; no out-of-band deploy script needed. |
| **B. Encrypted Dataverse env var holds the conn string directly, populated from KV at deploy time** | Rejected (this session) | Simpler runtime (no outbound HTTPS, no JSON parsing in plugin) but requires a manual deploy step on every rotation, splitting source of truth between Vault and Dataverse. User chose the runtime-fetch pattern after weighing the trade-off. |
| **C. Managed Identity on the Dataverse environment** | Not available | Dataverse plugin sandbox does not expose managed-identity acquisition today. Would be the preferred long-term option once Microsoft ships support. |

The major implementation trade-off accepted for Option A was the **ILRepack vs. raw REST** sub-decision (see next section).

## Implementation: raw REST, not Azure SDK

The Dataverse plugin sandbox loads only the single registered plugin
assembly — no transitive dependencies, no GAC reach. Two ways forward:

1. **ILRepack** Azure.Identity + Azure.Security.KeyVault.Secrets +
   Azure.Core + System.Memory + System.Buffers +
   Microsoft.Bcl.AsyncInterfaces + Newtonsoft.Json + … into the signed
   plugin DLL. Adds ~5–8 MB to the assembly, requires ILRepack tooling
   and careful handling of signing.
2. **Hand-roll OAuth2 client-credentials + KV REST** over `HttpClient`
   and the BCL. Zero new package references, no ILRepack, signed DLL
   stays small (~50 KB delta).

We chose **(2)**. Cost: ~120 lines of code in
`KeyVault/KeyVaultSecretReader.cs` + 80 lines in `KeyVault/MinimalJson.cs`,
both fully covered by unit tests. Benefit: deployment story is unchanged;
no new build complexity; no risk of ILRepack producing a non-loadable
sandbox assembly.

The two REST flows used:

| Endpoint | Method | Purpose |
|---|---|---|
| `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token` | POST | Exchange SP credentials for a bearer token (scope `https://vault.azure.net/.default`) |
| `https://{vault}.vault.azure.net/secrets/{name}?api-version=7.4` | GET | Retrieve the secret value with the bearer token |

Sandbox network requirement: outbound HTTPS:443 to both hostnames.
Dataverse Online sandboxes have permitted external HTTPS calls since
~2019; this has not been blocked in our tenant as of the last verified
check (E2E validation is the next step).

## Dataverse Environment Variables

Five definitions live in the `DatastreamBooks` solution. **All five
are plain-Text type [§63].** None is Secret-type today; the originally
designed Secret-type path for `rm_sqlkvclientsecret` was abandoned
after live validation surfaced an unfixable
`0x80040256 Access Denied` from the sandbox.

| Schema name | Type | Source | Purpose |
|---|---|---|---|
| `rm_sqlkvtenantid` | Text | Dataverse env var value | Entra tenant id -- `ca800f2c-47b3-4400-8eb1-fb1db2a39a1e` |
| `rm_sqlkvclientid` | Text | Dataverse env var value | `datastream-books-cicd` app id -- `a58747ee-f26f-4702-b911-044ee44df9a5` |
| `rm_sqlkvclientsecret` | **Text [§63 -- originally Secret]** | Dataverse env var value, mirrored from Key Vault `kv-datastream-books / cicd-sp-client-secret` by the sync script. **Only sanctioned writer:** [`../../scripts/sync-sp-secret-to-dataverse.ps1`](../../scripts/sync-sp-secret-to-dataverse.ps1). Re-run after every SP credential rotation. | SP client secret (working copy held in plaintext inside Dataverse). |
| `rm_sqlkvurl` | Text | Dataverse env var value | Vault URL -- `https://kv-datastream-books.vault.azure.net/` |
| `rm_sqlkvsecretname` | Text | Dataverse env var value | Secret name -- `dsb-app-connection-string` |

The plugin reads all five via the elevated organization service
(`IOrganizationServiceFactory.CreateOrganizationService(null)`).
`DataverseEnvironmentVariables.GetValue` has a single code path -- no
Secret-type branch. A regression test in
`plugins/DatastreamBooks.Plugins.Tests/KeyVaultTests/DataverseEnvironmentVariablesTests.cs`
asserts the function never invokes `Execute`, locking in the §63
design. Reintroducing a Secret-type branch requires deleting that
test.

Tenant, client id, vault URL, and secret name are not secrets -- they
are kept as env vars purely so the same plugin assembly is portable
across environments (dev / test / prod can each point at their own
Key Vault).

### Risk accepted: SP client secret held in plaintext inside Dataverse

The §63 pivot moves the SP client secret value into
`environmentvariablevalue.value` in Dataverse where it is unencrypted
at rest. Mitigations per §63:

- Dataverse audit log is enabled on `environmentvariabledefinition` and
  `environmentvariablevalue`; every read and write is recorded.
- Edit privilege on `rm_sqlkvclientsecret` is restricted to the System
  Administrator role.
- The underlying SP credential is rotated every 12 months per §36,
  limiting blast radius.
- Key Vault remains the source of truth for the underlying credential
  -- Dataverse holds a working copy, not the original.

### Historical: Power Platform Key Vault integration prerequisites

> **Historical -- not used by Books today per §63.** The narrative
> below was the original Phase 6B design for resolving Secret-type
> env vars via the Power Platform Key Vault integration. It is
> retained for reference (in case Microsoft fixes the sandbox
> `prvReadEnvironmentVariableSecretValue` gating in the future, or
> in case a different consumer needs this path).

For Secret-type env vars to save and resolve, the Power Platform
resolver pipeline needs all of:

1. **Vault firewall** permissive for the resolver:
   - Dev: `defaultAction = Allow` with `bypass = AzureServices`
   - Prod: Private Endpoint joined to a Dataverse enterprise policy
2. **Two SP grants at vault scope**:
   - Dataverse SP (object id `567ae524-268d-4de9-8054-7e26da9fa7f0`) --
     `Key Vault Secrets User`
   - Dataverse Resource Provider SP (object id
     `4f026a85-a88e-4674-baf4-45833854f411`) -- `Key Vault Secrets User`
     (this is the SP that actually authenticates during the save).
3. **The user creating the env var** holds `Key Vault Secrets User`
   (or higher) at the Vault.

Operational details in
[`../runbooks/key-vault-management.md`](../runbooks/key-vault-management.md)
under the same heading. These prerequisites are still satisfied in
the live environment (they cost nothing to retain) but the live env
var no longer depends on them.

## Caching

Two static caches in `KeyVaultSecretReader`:

| Cache | Key | TTL | Notes |
|---|---|---|---|
| Bearer token | `tenant|clientId` | Azure-issued `expires_in` minus 5 min safety | Refreshed once per ~55 minutes per AppDomain |
| Secret value | `vaultUrl|secretName` | 5 minutes | KV rotations propagate within TTL |

The 5-minute secret TTL is the compromise between rotation latency and
network cost. With ~5,000 posting plugin calls per business day at
steady state, a 5-minute TTL gives us ~96 KV calls per day — cheap and
slow enough to never throttle. If we ever need faster rotation
propagation, lower the TTL; or invalidate manually via
`KeyVaultSecretReader.InvalidateCaches()` from an admin tool.

Static caches survive across plugin invocations within the same
Dataverse sandbox AppDomain. AppDomain recycling will clear them, which
is harmless — the first call after a recycle pays one cold-start cost
(typically 200–800 ms for token + secret round trip).

## What "the plugin" does at posting time

1. Stage 40 (PostOperation) of `Update rm_journalentry` fires.
2. Plugin detects status transition into `Posted`. Other transitions
   return immediately.
3. Plugin acquires the elevated org service via
   `OrgSvcFactory.CreateOrganizationService(null)`.
4. Plugin reads the 5 env vars.
5. Plugin calls `KeyVaultSecretReader.GetSecret(...)` — token cached
   miss + KV cached miss = ~500ms on first call after AppDomain
   restart; cache hit = sub-millisecond on subsequent calls.
6. Plugin opens a `SqlConnection` with the returned ADO.NET string,
   runs the hash-chain transaction (`LedgerWriter.WriteBatch`), commits
   or rolls back.
7. On any failure (KV unreachable, SQL unreachable, hash mismatch
   refusal), the plugin throws `InvalidPluginExecutionException`,
   which rolls back the Dataverse transaction containing the
   Approved→Posted flip. The JE remains at `Approved`, retryable once
   the underlying issue is fixed.

## Failure modes and what each looks like

| Failure | What the user sees | What the operator does |
|---|---|---|
| Env var `rm_sqlkvclientsecret` missing/empty | "Dataverse Environment Variable 'rm_sqlkvclientsecret' is defined but has neither a value nor a defaultvalue." | Run [`../../scripts/sync-sp-secret-to-dataverse.ps1`](../../scripts/sync-sp-secret-to-dataverse.ps1) to populate from Key Vault. The script is the only sanctioned writer of this Text-type env var [§63]. |
| Env var definition missing entirely | "Dataverse Environment Variable definition 'rm_sqlkv*' was not found." | Reimport the DatastreamBooks solution; the env var definition is part of it. |
| KV unreachable / network failure | "Could not read SQL connection string from Key Vault for JE …: Network failure …" | Check Azure status, plugin sandbox egress, vault firewall. |
| Bad SP credential / wrong RBAC | "… Azure AD token endpoint returned 401 …" or "… Key Vault GET … returned 403 …" | Verify cicd SP secret matches the value in `rm_sqlkvclientsecret`; verify `Key Vault Secrets User` role still present on the vault for the SP. |
| SQL unreachable / serverless cold-start beyond 60s | "… SQL failure during ledger dual-write … timeout expired …" | Retry — usually wakes the serverless instance on the second attempt. |
| Hash chain corruption detected | "Chain head row for EntityId … has RowHash of length N (expected 32). Possible chain corruption — refusing to write." | Stop. Investigate via verify-integrity job. Do NOT manually fix. |

## Open items / future work

- **Verify sandbox egress to KV before going live.** Verified live by
  the first successful end-to-end post (JE-2026-001005 per §65);
  recorded in `immutability-validation.md`.
- **Plugin SP retrieval test of `cicd-sp-client-secret`.** Verified
  implicitly by §65.
- **Migrate to managed identity** when Dataverse plugin sandbox
  supports it. Will eliminate the SP secret from the env-var surface
  and remove the plaintext-in-Dataverse risk accepted under §63.
- **Single rotation script that touches all three consumers** (Key
  Vault, Dataverse env var, GitHub Actions secret) in one pass.
  Currently the Dataverse half is automated by
  [`../../scripts/sync-sp-secret-to-dataverse.ps1`](../../scripts/sync-sp-secret-to-dataverse.ps1);
  the GitHub Actions secret refresh is still manual.

## See also

- [`../runbooks/key-vault-management.md`](../runbooks/key-vault-management.md) — Vault config + rotation procedures
- [`../runbooks/sql-account-management.md`](../runbooks/sql-account-management.md) — `dsb_app` SQL user backing the conn string
- [`immutability-design.md`](immutability-design.md) §B — hash chain spec
- [`security-model.md`](security-model.md) — credential storage layer in the overall arch
- `plugins/DatastreamBooks.Plugins/KeyVault/KeyVaultSecretReader.cs`
- `plugins/DatastreamBooks.Plugins/KeyVault/DataverseEnvironmentVariables.cs`
- `plugins/DatastreamBooks.Plugins/Posting/PostJournalEntryLedgerWriter.cs`
