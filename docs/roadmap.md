# Datastream Books — Roadmap

> Single source of truth for project state and direction.
> Updated at the end of every session per AGENTS.md "Roadmap Maintenance".

## Current Phase

**Single track active as of 2026-05-22 (strict sequential per §58, reaffirmed by §67).**

### Phase 7 (Backend Track) -- Vendor / Customer Integration with ERP

Phase 6B closed on 2026-05-21 (see Completed Phases). Architectural ownership question for vendor / customer master closed by §70 on 2026-05-22: Books is system of record; ERP receives a downstream projection of Books-mastered fields via plugin-driven push; ERP retains write authority on operations-only fields (site locations, transportation, operational flags). Same record, two writers, field-level lanes.

Next backend focus:

- **Pam conversation (week of 2026-05-25)** -- consult on the Books-mastered field list, new-vendor intake workflow, 1099 rules, and approval routing per [`memos/pam-conversation-prep-2026-05-w22.md`](memos/pam-conversation-prep-2026-05-w22.md) Item 1. Architectural ownership is settled by §70; this conversation finalizes the Pam-consult inputs that Phase 8 AP scoping depends on.
- **Books AR references `rm_customer` from PRI-Datastream** rather than duplicating it. Cross-solution read-only lookup; pattern reference: [`architecture/erp-pattern-notes.md`](architecture/erp-pattern-notes.md) Pattern 3. Per §70, customer master ownership now mirrors vendor master (Books is system of record; ERP read-only on Books-mastered fields).
- **Document the ownership boundary** in the data-model file so future contributors do not write to the ERP-owned (operations-only) side of the record. Tracked as BL-38 in [`backlog.md`](backlog.md).
- **Backlog items opened under §70:** BL-52 (Books -> ERP push plugin design), BL-53 (ERP-side write-permission lockdown), BL-54 (cutover-day reconciliation of existing ERP records with Macola-migrated Books data; P1 Phase 10, Pam owns the dedup review). See [`backlog.md`](backlog.md) for the full descriptions.

Phase 8 (AP / AR Core) picks up after backend Phase 7 lands. The UI track (Phase 7A S4 onward and Phase 7B-7F) remains deferred per §67 -- revisit is a fresh decision once Backend Track A lands and the [`memos/executive-questionnaire.md`](memos/executive-questionnaire.md) §17 Pam-consult portion has been answered (the architectural §17 question is already closed by §70).

## Completed Phases

> Newest first.

### Governance + role-definition session (2026-05-22 afternoon)

**Focus:** Define the Technical Strategic Lead role and stand up the concurrence-log structure so cross-domain decisions made under §71 authority have a defined stakeholder visibility + concern-raising channel.

**Outcome:**
- **§72 -- Technical Strategic Lead role definition.** Ryan M designated Technical Strategic Lead for Datastream Books -- a combined engineering + strategy + maintenance + architectural-authority role, distinct from a traditional IT support role. Read-this-as-that rule: references to "IT" in earlier decisions read as "Technical Strategic Lead" going forward. President is the executive reporting line; CFO sponsors the project per §32 but does not direct technical decisions.
- **§71 lead sentence rephrased in place** to "Architectural decisions sit with the Technical Strategic Lead (engineering + strategy); Pam consults; COO concurs on cross-domain impact." Body retained; no other prior decisions retroactively rewritten.
- **New file [`decisions/concurrence-log.md`](decisions/concurrence-log.md)** -- captures the 5-business-day stakeholder concern window for decisions made under §71 authority. Three active entries opened (§70 / §71 / §72), all windows close 2026-05-29; notification statuses currently "pending" (Friday status update + exec rollout meeting not yet sent).
- **Master decisions sheet categorization labels updated** from "IT-decides" to "Technical Strategic Lead-decides"; §72 row added to Status Tracking Table.
- **Cross-doc references added:** README.md Project Identity bullet for Technical Strategic Lead; AGENTS.md "Key Principles to Remember" gains principles #8 and #9 (architectural authority + role definition); master decisions sheet "How To Use" gains a "Raising concerns about a Section 1 decision" paragraph pointing at the concurrence log.

**Decisions made:** §72.

**Next:** Phase 7 (Backend Track) -- Pam conversation week of 2026-05-25 (consult portion of §17, per [`memos/pam-conversation-prep-2026-05-w22.md`](memos/pam-conversation-prep-2026-05-w22.md)).

### Governance + ownership session (2026-05-22 early afternoon)

**Focus:** Close the latent §22 ambiguity around vendor / customer master ownership and codify the governance pattern that should have framed it.

**Outcome:**
- **§70 -- Vendor / customer master ownership architecture.** Books is system of record; ERP receives a downstream projection of Books-mastered fields via plugin-driven push (legal name, EIN, tax classification, W-9 status, 1099 reportable flag, banking / NACHA, payment terms, hold-payment flag, credit terms, approval status, AP / AR routing). ERP retains write authority on operations-only fields (site locations and shipping points, transportation routing, operational approval flags for PO eligibility, operational notes, preferred-vendor flags, operational status). Same record, two writers, field-level lanes -- not table-level read-only. Resolves [`memos/executive-questionnaire.md`](memos/executive-questionnaire.md) §17 architectural question.
- **§71 -- Governance principle:** Architectural decisions sit with the Technical Strategic Lead (lead phrasing was originally "IT" -- updated in the afternoon session under §72); Pam consults on finance-domain detail; COO concurs on cross-domain impact. Retroactively reframes §67 as a Technical Strategic Lead decision with Pam consult; §67's substantive outcome (return to §58 strict sequential) is not disturbed. §30 (Pam as Finance System Owner) stands unchanged.
- **Master decisions sheet [`memos/decisions-required-master-list.md`](memos/decisions-required-master-list.md) restructured** into three primary sections (Technical Strategic Lead-decides / Pam-decides / Exec-decides) plus Section 4 for items mostly settled. Entry #3 (vendor master ownership) moved from Pam-decides to Technical Strategic Lead-decides and marked [Confirmed: §70].
- **Executive questionnaire [`memos/executive-questionnaire.md`](memos/executive-questionnaire.md) §17** marked Resolved with citation to §70; top-of-file governance framing note added explaining §71's reframing of some historical "Pam decides X" items as Technical Strategic Lead decisions with Pam consult.
- **Pam conversation prep [`memos/pam-conversation-prep-2026-05-w22.md`](memos/pam-conversation-prep-2026-05-w22.md) Item 1 rewritten** from "decide who owns vendor records" to "consult on field list, intake workflow, 1099 rules, approval routing." Item-1 time budget reduced from ~10 to ~7 minutes.
- **Three backlog items opened** under §70: BL-52 (push plugin design, P2 Phase 8), BL-53 (ERP write-permission lockdown, P2 Phase 8), BL-54 (cutover-day reconciliation, P1 Phase 10, Pam-input-needed Y).

**Decisions made:** §70, §71.

**Next:** Continuation session same afternoon -- §72 + concurrence log (above).

### Governance / process session (2026-05-22 morning)

**Focus:** Close out the §66 reaffirmation gate from yesterday's handoff, codify operating principles for routine vs high-stakes work, and ship the CI deploy-dev fix that surfaced overnight.

**Outcome:**
- **§67 -- §66 reaffirmation gate resolved.** Phase 7A S4-S11 deferred until Backend Track A lands AND executive-questionnaire §17 (vendor master) has a Pam answer. Phase 7A S1-S3 artifacts remain valid; the 17 Phase 7 UX decisions (§46-§62) remain in force. Per the §68 annotation appended to §67 the same day, in retrospect Option C (continue S4 only, then defer) had a stronger concrete-artifact case than Option B (strict sequential) credited; §67 stands but future reaffirmation gates should weight visible-artifact bias more heavily.
- **§68 -- Operating principles codified in CLAUDE.md.** Five principles governing routine vs high-stakes work, surfacing of concerns, scope discipline, bias toward concrete artifacts for accounting-team feedback, and operator-driven hours. High-stakes work (plugin code, SQL migrations, prod deploys, schema, hash chain, JE-2026-001005, audit-trail anchors, prior decisions) continues to require step-by-step approval; routine work executes as a batch with single end-of-session review.
- **§69 -- CI workflow async-import pattern adopted as default.** Four consecutive deploy-dev runs had reported import failures while Dataverse Solution History showed actual success -- root cause was `pac solution import` synchronous mode connection dropping at the Dataverse gateway's ~5-minute timeout. Fixed by switching to `--async --max-async-wait-time 30`. Post-fix run (097da24, GitHub run #39) completed successfully in 13m 35s with the import step taking 10m 31s.
- **R-A-19 mitigation design** committed at [`architecture/form-readonly-enforcement.md`](architecture/form-readonly-enforcement.md); implementation runbook at [`runbooks/r-a-19-business-rule-implementation.md`](runbooks/r-a-19-business-rule-implementation.md) (BL-47 status: Ready -- operator handoff for a 30-45 min maker-portal session).
- **AGENTS.md "What NOT to Do" row** added for PRT-vs-CI/CD (R-A-20 mitigation; BL-50 closed).
- **Backlog Priority Index** added to [`backlog.md`](backlog.md) (50 items ranked P0-P3; later updated to 53 items in the afternoon §70 session).

**Decisions made:** §67, §68, §69.

**Next:** Same-day afternoon §70 + §71 session (above).

### Phase 7A Sessions S1-S3 (research artifacts only; S4-S11 deferred per §67) -- completed 2026-05-21

**Focus:** Three documentation / research items in Phase 7A that ran in parallel with backend under the §66 provisional override. S1 (credential cleanup), S2 (visual identity extraction), S3 (sitemap design). All three landed as documentation artifacts only -- no app module, no PCF, no security role scaffolding, no Dataverse schema changes.

**Outcome:**
- **S1 -- Credential cleanup verification** ([`architecture/immutability-validation.md`](architecture/immutability-validation.md) 2026-05-21 follow-up section). Confirmed live state of `datastream-books-cicd` app reg matches the §45-era cleanup -- one password credential (`987e5ce6-934e-48db-a3d0-8972e98c7d63`, expires 2028-05-19), one federated credential (`github-main`); the unrecognized "secret" credential surfaced under §36 has been removed.
- **S2 -- Visual identity extraction** ([`architecture/ui-styling.md`](architecture/ui-styling.md)). Finding: PRI-Datastream ERP has no custom theme; the three legacy `theme` records are stock Microsoft. Books defines its own minimal CSS-variable-based theme; logo continuity preserved via the shared PRI corporate logo. §49 amended in place.
- **S3 -- Sitemap design** ([`architecture/ui-sitemap.md`](architecture/ui-sitemap.md)). Accounting-workflow-first 8-group structure; persona visibility deferred to 7B.
- **Autonomous documentation audit** ([`audits/audit-2026-05-21-evening.md`](audits/audit-2026-05-21-evening.md)) during 2-hour operator gap window. 46-item backlog consolidated; risk register R-A-17 through R-A-20 added.

**Deferred per §67 (2026-05-22 morning):** Phase 7A S4 (app module + theme + logo packaging), S5-S10 sessions, status pill placement, Innovation Team logo binary. Captured as BL-26 through BL-29 in [`backlog.md`](backlog.md) at P3 priority. Revisit is a fresh decision once Backend Track A lands and exec questionnaire §17 has a Pam answer (consult portion -- architectural portion already closed by §70).

**Decisions made:** §66 (provisional parallel-track override -- subsequently closed by §67).

**Next:** Phase 7A S1-S3 outputs feed forward to whenever full Phase 7A actually resumes. In the interim, Backend Track A continues as the single active phase.

### Phase 6B (validation closed) — first real JE posted end-to-end (2026-05-21)

**Focus:** Take the Phase 6B code-complete plugin from "all tests
green locally" to "first real JE flows through the live PRI-Books-Dev
environment without one-sided commits". This phase is the proof that
the immutability architecture works in production, not just in test
harnesses.

**Outcome:**
- JE-2026-001005 (Cash debit $75, AR credit $75, single entity)
  posted successfully through the full stack.
- Dataverse side: `rm_status` = Posted (126190003),
  `rm_postedby_user` = ryanm@plastic-recycling.net,
  `rm_posteddatetime` = 2026-05-21T18:11:21Z.
- Azure SQL `ledger.GeneralLedgerEntries` side: EntryId 3 (Cash debit
  $75, account 10100) — `PreviousRowHash` = 32 zero bytes (genesis,
  per §39); EntryId 4 (AR credit $75, account 11000) —
  `PreviousRowHash` = `0x5E08EF14028496CB3C694C028B53140F9C34C4880B7512A6EADB906289DE344B`,
  byte-for-byte EntryId 3's `RowHash`. Hash chain works as designed.
- Atomicity verified: Dataverse and SQL show consistent posted state
  with no one-sided commit. Rollback-and-throw (§41) holds under
  real conditions.
- Plugin assembly 1.0.0.4 is the production-ready artifact. 1.0.0.2
  and 1.0.0.3 are retired (both failed validation; see Decisions
  below).
- `architecture/immutability-validation.md` updated with the live
  first-real-JE validation result, including the exact hash bytes.

**Decisions made (logged in
[`decisions/datastream-books-decisions.md`](decisions/datastream-books-decisions.md)
§63-§65):**
- **§63 — Plain-Text env var for `rm_sqlkvclientsecret`.** The Dataverse
  plugin sandbox identity does not hold
  `prvReadEnvironmentVariableSecretValue` even when impersonating the
  SYSTEM user via `OrgSvcFactory.CreateOrganizationService(null)`.
  `RetrieveEnvironmentVariableSecretValue` returns `0x80040256 Access
  Denied` from inside the sandbox, regardless of payload shape — pinned
  by two failed deploys (1.0.0.2 with the wrong parameter name,
  1.0.0.3 with the correct parameter name). Same action returns the
  same error to a System Administrator user calling via Web API.
  Pivot: convert the variable to plain Text (type 100000000); a deploy
  script syncs the value from Key Vault after each SP rotation.
  `DataverseEnvironmentVariables.GetValue` simplified to a single
  code path; regression test enforces "no Execute calls". Key Vault
  remains the source of truth for the underlying secret.
- **§64 — ASCII-only PowerShell + UTF-8 with BOM.** Surfaced when the
  first cut of `scripts/sync-sp-secret-to-dataverse.ps1` contained
  em-dashes and section signs that Windows PowerShell 5.1 mojibaked
  into `â€"` (UTF-8 read as Windows-1252). Codified in AGENTS.md under
  PowerShell Scripts conventions.
- **§65 — Phase 6B end-to-end validated** with the specific live evidence
  recorded above.

**Issues encountered (resolved):**
- 1.0.0.2 sent `environmentVariableDefinitionId` (Guid) as the action
  parameter. Web API `$metadata` confirmed the action's only parameter
  is `EnvironmentVariableName` (Edm.String). Fixed in 1.0.0.3 — still
  failed (sandbox privilege gate, not payload). Drove the §63 pivot.
- Sync-script encoding bug: em-dashes in the first cut produced
  mojibake in PS 5.1; rewriting as ASCII-only + UTF-8-with-BOM
  resolved it and drove §64.

**New artifacts in this phase:**
- `scripts/sync-sp-secret-to-dataverse.ps1` — KV-to-Dataverse value
  sync; the only sanctioned writer of the `rm_sqlkvclientsecret`
  value. Supports `-WhatIf`; verifies by length only (per §45);
  Clear-Variable on completion.
- `plugins/DatastreamBooks.Plugins.Tests/KeyVaultTests/DataverseEnvironmentVariablesTests.cs`
  — 9 tests, including the regression guard that fails if anyone
  reintroduces the Secret-type branch.

**Next:** Phase 7 (Backend Track) — Vendor / Customer Integration with
ERP. See Current Phase above.

---

### Phase 6B (code) — PostJournalEntryPlugin Azure SQL dual-write + hash chain (code-complete 2026-05-21)

**Focus:** The architectural keystone of the immutability story. When a
JE transitions Approved→Posted, the plugin writes one row per line into
Azure SQL `ledger.GeneralLedgerEntries`, computes per-entity SHA-256
hash chain values, and commits both stores atomically. Any SQL failure
throws `InvalidPluginExecutionException`, which rolls back the Dataverse
transaction — keeping the two stores consistent.

**Outcome:**
- New plugin code in `plugins/DatastreamBooks.Plugins/`:
  - `Immutability/LedgerRow.cs` — ledger row DTO
  - `Immutability/LedgerRowHasher.cs` — SHA-256 hash chain, exact byte
    layout pinned by tests
  - `Immutability/LedgerWriter.cs` — SQL transaction with
    `WITH (UPDLOCK, HOLDLOCK)` chain-head lock, parameterized INSERT
  - `KeyVault/KeyVaultSecretReader.cs` — raw OAuth2 client-credentials
    + Key Vault REST GET over HttpClient (no Azure SDK, no ILRepack)
  - `KeyVault/MinimalJson.cs` — tiny single-field JSON extractor
  - `KeyVault/DataverseEnvironmentVariables.cs` — env-var read helper
  - `Posting/PostJournalEntryLedgerWriter.cs` — orchestration
  - `Posting/PostJournalEntryPlugin.cs` — new Stage 40 PostOperation
    dispatch branch, fires only on Approved→Posted transition
- 25 new tests, all passing (16 hash-chain + 5 writer-arg + 8 JSON +
  4 existing); total suite 41 tests green.
- New runbook section in
  [`runbooks/plugin-registration.md`](runbooks/plugin-registration.md)
  for Step 10 registration (PreImage + PostImage) and the five Dataverse
  Environment Variable definitions that gate the run.
- New architecture doc
  [`architecture/credential-access-design.md`](architecture/credential-access-design.md)
  capturing the chosen pattern + rejected alternatives.
- `architecture/immutability-design.md` §B updated with the exact
  byte layout, per-field formatters, genesis hash, and chain-head
  locking strategy.

**Decisions made:**
- **Plugin reads connection string from Key Vault at runtime** (Option
  A), not "env var holds conn string directly" (Option B). User chose
  Option A after weighing the trade-off — KV remains the single source
  of truth, rotations propagate within the 5-minute TTL without a
  re-deploy. Documented in `credential-access-design.md`.
- **Raw HttpClient + OAuth + KV REST** instead of Azure.Identity +
  Azure.Security.KeyVault.Secrets. Avoids ILRepacking 5–8 MB of
  transitive dependencies into the signed plugin DLL. ~200 lines of
  hand-rolled code, fully unit-tested.
- **Hash byte layout:** each field is length-prefixed (4-byte big-endian
  length + UTF-8) or null sentinel (4 bytes `0xFFFFFFFF`); previous row
  hash appended raw (32 bytes); SHA-256 over the whole sequence. Pinned
  by 16 tests in `LedgerRowHasherTests.cs`.
- **`decimal` canonical form is `F4` (4 decimals, invariant culture)**
  even though the schema is `DECIMAL(19,4)`. Future schema change to
  more precision would be chain-breaking; documented.
- **Per-entity chain head locked with `WITH (UPDLOCK, HOLDLOCK)`** in
  the SQL transaction. Concurrent writers serialize at the SELECT.
- **Rollback-and-throw, not "PostFailed" status**, on SQL failure. We
  do not have a `PostFailed` status value defined on `rm_status`; the
  cleanest atomicity story is "Dataverse rolls back the
  Approved→Posted flip when the plugin throws". JE remains at
  `Approved`, retryable.
- **5-minute Key Vault secret TTL, ~55-minute token TTL.** Caches in
  process-static fields.
- **Connection timeout = 60s** (plugin runtime; conn string in Vault
  uses standard 30s). Mitigates Azure SQL serverless cold-start without
  inflating all consumers.
- **SP client secret stored as Secret-type Dataverse env var.** Plugin
  reads it via the elevated organization service
  (`OrgSvcFactory.CreateOrganizationService(null)`) so the secret
  decrypts. Tenant id, client id, vault URL, secret name are plain
  string env vars (not secrets).

**Pending live validation:** the one-time deployment plus first-JE
verification in PRI-Books-Dev — listed under Current Phase above.

### Key Vault provisioning + credential rotation (completed 2026-05-20)

**Focus:** Stand up `kv-datastream-books` as the credential store for
Datastream Books, rotate `dsb_app`'s SQL password into a known value
stored in Vault, generate a fresh 12-month client secret for the
`datastream-books-cicd` SP and store that in Vault too, enable diagnostic
logging from day one, and grant the SP `Key Vault Secrets User` so the
Phase 6B plugin runtime can authenticate to Azure SQL as `dsb_app` —
*not* `priadmin` (the immutability architecture depends on this).

**Outcome:**
- `kv-datastream-books` provisioned in the shared Datastream RG (East US,
  standard, Azure RBAC, soft-delete 90 days + purge protection on,
  public network with firewall `Deny` default + only ryanm dev IP
  `12.201.35.226/32` in the allow-list, `bypass=None`).
- Diagnostic setting `kv-datastream-books-diagnostics` routes
  `AuditEvent` logs and `AllMetrics` to the existing Log Analytics
  workspace `7d6df7e7-d474-4284-be38-ba20eec9ef7f-Datastream-EUS` in the
  Datastream RG (reused, not re-created).
- RBAC at Vault scope: ryanm = `Key Vault Administrator` (setup +
  break-glass), `datastream-books-cicd` SP =
  `Key Vault Secrets User` (Get-only).
- `dsb_app` SQL password rotated (32-char, alphanumeric + safe symbols
  `! @ # $ % ^ & * - _ +`, never logged or echoed). Verified live as
  `dsb_app`: positive tests (SELECT @@VERSION, SELECT TOP 1 from
  ledger, INSERT-in-transaction-then-ROLLBACK) all pass; negative tests
  (UPDATE / DELETE) both throw SQL 229 *permission denied*, confirming
  the V0002 DENY architecture survives the rotation.
- ADO.NET connection string stored as `dsb-app-connection-string`
  (content-type `text/plain;charset=utf-8`; tags `purpose=plugin-runtime,
  environment=dev, target=datastream-books-dev`).
- Fresh 12-month client secret generated on the `datastream-books-cicd`
  app reg (display name "Key Vault and CI/CD authentication", keyId
  `efb08aa2-a161-4f54-92b9-011197d07c88`, expires 2027-05-20). Stored
  as `cicd-sp-client-secret`. Federated identity (OIDC) remains the
  primary auth path for GitHub Actions — this secret is break-glass
  fallback only.
- New runbook
  [`runbooks/key-vault-management.md`](runbooks/key-vault-management.md)
  authored as the single source of truth (configuration, RBAC, diagnostic
  logging, secret inventory, rotation procedures, break-glass for SP
  credential loss / Vault lockout / RBAC mis-grant / accidental Vault
  delete). Cross-references added in `sql-account-management.md`,
  `security-model.md`, and `cicd-setup.md`.

**Decisions made:**
- **Azure RBAC, not legacy access policies.** Modern recommendation;
  enables least-privilege roles like `Key Vault Secrets User` (read-only
  on secrets) that don't exist in the access-policy model.
- **Diagnostic logging on at provisioning.** Auditor-defensibility lives
  or dies on continuous audit. Day-zero coverage means there is no
  "what happened before logging was enabled" gap.
- **Purge protection on, irreversibly.** Trade-off accepted: cannot
  fully purge the Vault for 90 days post-deletion, but cannot be
  silently destroyed by an attacker or accident either.
- **Firewall = explicit allow only, `bypass=None`.** No
  `AllowAzureServices` — that bypass is functionally "trust arbitrary
  Microsoft-hosted code". Phase 6B revisits when plugin egress IPs are
  known.
- **Reuse the existing shared Log Analytics workspace** in the
  Datastream RG rather than create a new one. Same audit horizon as
  the rest of the PRI Azure footprint; one place to query.
- **Service principal client secret stays as a break-glass path,
  not the primary auth.** GitHub Actions continues to use federated
  identity (OIDC) — no secret in CI logs or in GitHub Secrets storage.
- **Decline to delete the unidentified pre-session credential.** An
  unrecognized credential named "secret" (keyId
  `3eaf8a5a-1cf8-4d5c-89e5-e322567abf4a`) was found on the cicd app reg
  with a creation timestamp ~8 minutes before this session started. Its
  origin is unknown; deleting it without confirmation could disable
  some other consumer. Surfaced to the user; left intact pending
  investigation.

**Issues encountered (resolved):**
- The `Microsoft.KeyVault` resource provider was not registered on the
  PRI subscription — first-time use. Registered explicitly with
  `az provider register --namespace Microsoft.KeyVault --wait`, then
  Vault create succeeded.
- Azure SQL serverless cold-start: `DatastreamBooks-Dev` (GP_S_Gen5_1,
  60-minute auto-pause) was paused at the start of the rotation. First
  connection timed out in post-login at the default 30-second
  threshold. Resolved by bumping `Connect Timeout` to 120s for the
  rotation-and-verification connections specifically. The Vault-stored
  connection string keeps the standard 30s timeout — runtime callers
  pay the wake-up cost once per pause cycle and recover on retry.
- `az ad app credential reset` JSON-parse failure on a non-suppressed
  CLI warning broke the first secret-capture attempt; the resulting
  credential was unrecoverable. Deleted by keyId, then retried with
  `--only-show-errors 2>$null` for a clean stdout, succeeded.
- `az ad app credential reset --output json` returned an empty `keyId`
  in the response object even though Azure created the credential with
  a valid `keyId`. Cross-referenced via `az ad app credential list`
  after creation and patched the Vault tag via
  `az keyvault secret set-attributes`. Quirk documented in
  `key-vault-management.md` so the next rotation doesn't trip on it.

**Next:** Phase 6B — Azure SQL dual-write + hash chain in
`PostJournalEntryPlugin`. All Vault-side dependencies satisfied.

### Phase 6A: PostJournalEntryPlugin — Dataverse-only validations (completed 2026-05-19)

**Focus:** Stand up the first server-side posting plugin with the six
Dataverse-side validations the audit defensibility story relies on,
without yet touching Azure SQL. Azure SQL dual-write + the hash chain
are deferred to Phase 6B (after the Key Vault session) by design — the
prod plugin SP cannot yet read the SQL connection string until Key
Vault is wired.

**Outcome:**
- Single `PostJournalEntryPlugin` class in
  `plugins/DatastreamBooks.Plugins/Posting/PostJournalEntryPlugin.cs`
  dispatches across 8 message-processing steps. Six validations:
  1. **Auto-number** `rm_journalentrynumber` on Create as
     `JE-{entitycode}-{NNNNNN}`, per-entity sequence (max + 1 read at
     write time — acceptable at solo-dev concurrency; documented
     upgrade path to a counter table if needed).
  2. **Maintain `rm_totaldebit` / `rm_totalcredit`** on the header
     on every line write (post-op Create / Update / Delete), by
     re-summing all live lines for the parent.
  3. **Reject Approved transition when debits ≠ credits** — pre-op
     header Update; error message includes both totals and the diff.
  4. **SoD on Approved** — `rm_createdby_user` ≠ `rm_approvedby_user`;
     plugin defaults a null approver to the calling user before the
     check, so the comparison can never silently pass.
  5. **Reject Posted transition when fiscal period is not Open** —
     reads `rm_fiscalperiod.rm_status`; stamps `rm_postedby_user` =
     calling user and `rm_posteddatetime` = UTC now on success.
  6. **Immutability of Posted / Reversed / Voided headers and their
     lines** — one allowed exception: Posted → Reversed (the lawful
     one-way transition for reversing entries).
- 16 xUnit tests in
  `plugins/DatastreamBooks.Plugins.Tests/PostingTests/PostJournalEntryPluginTests.cs`,
  one per rule plus interaction cases (per-entity sequences, multi-line
  totals, SoD null-approver default, Voided always blocked, line writes
  against a Posted header). All passing locally and in CI.
- FakeXrmEasy.v9 2.x wired up via `MiddlewareBuilder.New().AddCrud()
  .AddFakeMessageExecutors().UseCrud().UseMessages().SetLicense(NonCommercial)
  .Build()` — the obsolete `XrmFakedContext()` default constructor
  doesn't ship CRUD plumbing by default in 2.x.
- New runbook
  [`docs/runbooks/plugin-registration.md`](runbooks/plugin-registration.md)
  documents the one-time PRT registration flow (assembly + 8 step rows
  + PreImages), plus the smoke-test sequence and the
  pull-solution-and-commit close-out. Subsequent plugin code changes
  flow through the existing CI workflow without manual PRT touches.

**Decisions made:**
- **Single dispatching plugin class**, not one per step.
  `PostJournalEntryPlugin` switches on `Message + PrimaryEntityName +
  Stage` to pick which private method runs. Keeps related logic in one
  file at the expense of a small dispatch block; matches what a future
  auditor will want to read.
- **Auto-numbering: read-max-and-increment** rather than a counter
  table. At solo-dev concurrency the race window is irrelevant; if
  multi-user write pressure arrives we replace the helper with a
  counter-table lookup behind the same method signature.
- **`ColumnSet(true)` on the single-row Retrieve calls** inside the
  plugin. The retrievals touch 1 row each (entity, fiscal period,
  parent header) — the bandwidth cost is negligible and the plugin
  becomes robust against future field additions without code changes.
- **Reversal exception coded as the single allowed transition** out of
  Posted. Reversed and Voided are terminal — no escape hatch in code.
- **Stamping the approver to current user when null** rather than
  rejecting at the gate. Matches the maker-portal UX (a one-click
  "Approve" button doesn't require the user to first set their own
  GUID into the approver lookup).

**Deferred to Phase 6B (post Key Vault session):**
- Azure SQL `ledger.GeneralLedgerEntries` writes on Posted transition
- Per-entity SHA-256 hash chain computation
- The atomic two-phase commit across Dataverse + Azure SQL
- `LedgerRowHasher` in `plugins/DatastreamBooks.Plugins/Immutability/`
- Reading the SQL connection string from Key Vault

**Issues encountered (resolved):**
- FakeXrmEasy.v9 2.x default `XrmFakedContext()` constructor is
  deprecated and ships without CRUD middleware wired up — Retrieve
  silently returns entities with no attributes. Migrated to
  `MiddlewareBuilder` per the deprecation warning.
- FakeXrmEasy 2.x requires explicit acceptance of a license enum
  (`FakeXrmEasyLicense.NonCommercial` is appropriate for an internal,
  closed-source project).
- `ExecutePluginWith<T>` returns a Castle DynamicProxy of `T`, not the
  real instance — cannot be cast back to the concrete plugin type.
  Tests now assert on the mutated Target entity rather than capturing
  the plugin reference.

**Next:** Phase 6B — Azure SQL dual-write + hash chain. Blocked on the
Key Vault session (Pam-scheduled 8:30 PM reminder tomorrow per session
brief).

### Phase 5: Journal Entry tables (completed 2026-05-19)

**Focus:** Build the `rm_journalentry` (header) + `rm_journalentryline` (lines) tables in PRI-Books-Dev so Phase 6 has somewhere to post against.

**Outcome:**
- `rm_journalentry` created with 12 user-defined attributes + 7 lookups: required `rm_entity` and `rm_fiscalperiod`, three optional `systemuser` lookups (`rm_createdby_user`, `rm_approvedby_user`, `rm_postedby_user`) for SoD, and two self-referencing reversal lookups (`rm_reversesje`, `rm_reversedbyje`). Primary name attribute is `rm_journalentrynumber`. Workflow status (Draft / PendingApproval / Approved / Posted / Reversed / Voided) and JE type (Standard / Recurring / Adjusting / Closing / IntercompanyClearing / Reversal) are local picklists.
- `rm_journalentryline` created with 7 user-defined attributes + 3 lookups: required parent `rm_journalentry` (Cascade All), required `rm_account` (to `rm_chartofaccount`), required `rm_entity` (denormalized from header). Primary name attribute is `rm_journalentrylinename` (manually entered in Phase 5; plugin will compute later).
- Both tables organization-owned with audit ON.
- `docs/architecture/data-model.md` updated to reflect the as-built shape — every column, every lookup, every cascade behavior, with the rationale for each Phase 5 decision.

**Decisions made:**
- **Autonumber deferred to Phase 6.** Dataverse's native autonumber column type doesn't easily support entity-prefixed numbering across tables. `rm_journalentrynumber` is a plain ApplicationRequired string column in Phase 5; the PostJournalEntryPlugin will generate `JE-{entitycode}-{NNNNNN}` and enforce uniqueness server-side. Simpler and more flexible.
- **Totals as plain decimal columns**, not roll-up or calculated. Roll-up columns refresh on a cadence and can be stale at validation time. Plain decimals updated by the plugin are atomic with line writes and always current.
- **`rm_createdby_user` is a deliberate, immutable user lookup**, separate from Dataverse's reassignable system `createdby`. Set once by plugin and never updated. SoD reads from this field.
- **Cascade All on the parent relationship** (header → lines). Posted-JE immutability is enforced at the status check level inside the plugin, not at the relationship level — so cascade only matters for Draft JEs, where the UI needs to be able to discard a Draft cleanly.
- **`rm_entity` denormalized onto lines** for query performance and entity-scoped row-level security. Plugin enforces `header.rm_entity = line.rm_entity` invariant.
- **`rm_costcenter` and `rm_project` as free-form strings** in Phase 5. They get masters / lookups when project accounting or cost-center reporting becomes a real requirement.
- **Account lookup named `rm_account`** (not `rm_chartofaccount`) for cleaner field references in forms and reports. Relationship still resolves to the COA table.

**Issues encountered (resolved):**
- Metadata cache lag after entity creation: first attribute POST returned `0x80040216 An unexpected error occurred`. Retry succeeded a few seconds later. Mitigation added to the .tmp-build helper: `0x80040216` and 500-class responses now classified as transient and folded into the same poll-on-lock retry loop already in place for `0x80071151` (concurrent solution import) and 429 throttles.
- The `RequiredLevel=ApplicationRequired` setting takes a publish-customizations pass before Dataverse honors it in the forms UI. Build script publishes per-table at the end; no manual step.

**Next:** Phase 6 — PostJournalEntryPlugin.

### Phase 4: Chart of Accounts (completed 2026-05-19)

**Focus:** Stand up the per-entity `rm_chartofaccount` master and a runnable standard COA seed so the next phases (Journal Entries, posting plugin) have account rows to point at.

**Outcome:**
- `rm_chartofaccount` table created in PRI-Books-Dev (14 user-defined columns + system audit columns)
- Four lookups in place: `rm_accounttype` (required), `rm_accountcategory`, `rm_entity` (required), `rm_parentaccount` (self, for hierarchical COA)
- Alternate key `rm_coa_entity_number_key` on (`rm_entity`, `rm_accountnumber`) — verified live to reject duplicate `(entity, account number)` inserts
- Placeholder `Default Operating Entity` (rm_entitycode=`DEFAULT`) seeded into `rm_entity`
- Standard 54-row COA loaded into PRI-Books-Dev under the DEFAULT entity: 20 parent + 34 child rows, including contra-account `rm_normalbalance` overrides on Allowance for Doubtful Accounts, Accumulated Depreciation, Sales Returns & Allowances, and Owner Distributions
- Two new committed seed scripts (`scripts/seed-default-entity.ps1`, `scripts/seed-rm_chartofaccount.ps1`) — both idempotent, environment-portable (FK resolution via stable codes, not GUIDs), and `-WhatIf`-aware
- `docs/architecture/data-model.md` updated to reflect the as-built shape — per-entity COA, lookups for type/category/entity/parent, single `rm_isactive` boolean instead of the Phase-1 draft's `rm_status` choice, and the rationale for each divergence

**Decisions made:**
- **Skipped `rm_accountnumberingscheme`** for this phase. It would be soft-validation data with no immediate consumer. Will be added when the COA validation plugin lands (the natural consumer of the range definitions).
- **`rm_normalbalance` as an optional local override** of the linked `rm_accounttype.rm_normalbalance`, with explicit values written only for contra-accounts. The denormalization is intentional — financial reports filter by Debit-vs-Credit constantly.
- **String columns do not get metadata-level defaults**. Dataverse Web API rejects `DefaultValue` on `StringAttributeMetadata`. `rm_currency`='USD' is set by the seed scripts at insert time; the value will also be enforced by a Phase 6+ validation plugin once multi-currency becomes a real concern.
- **Per-entity COA** (account number unique within an entity, not globally) — matches Macola behavior, removes need for an `entityactivation` intersect table.

**Issues encountered (resolved):**
- Solution-import / publish lock contention from the concurrent CI run — solved by wrapping the idempotent metadata build in a poll-on-429 loop so each step retries until the lock clears
- `StringAttributeMetadata.DefaultValue` is not a valid property on the Dataverse Web API — removed the default-value path on string columns and pushed the convention to the seed scripts; helper helper script comment now warns future authors
- Metadata cache lag on read-after-write for the choice column briefly caused `Get-DvAttribute` to return null and re-attempt a duplicate POST; mitigated by the retry loop and idempotency checks on the server side (existing-attribute errors are silently absorbed)

**Next:** Phase 5 — Journal Entry tables.

### Phase 3: First Real Dataverse Tables (completed 2026-05-19)

**Focus:** Create the first five foundational tables in PRI-Books-Dev and prove the CI/CD pipeline end-to-end.

**Outcome:**
- 5 tables created in PRI-Books-Dev: `rm_accounttype`, `rm_accountcategory`, `rm_fiscalyear`, `rm_fiscalperiod`, `rm_entity`
- 5 seed rows in `rm_accounttype` (Asset / Liability / Equity / Revenue / Expense with correct `NormalBalance`)
- Solution shell bootstrap to PRI-Books-Dev — solution didn't exist in dev until the first GitHub Actions run pushed an empty unmanaged shell
- Pack-as-Unmanaged workflow pattern established for dev environment (managed/unmanaged target is workflow-controlled, not source-controlled)
- pac CLI federated identity proven working via `--githubFederated` (note: not `--federatedToken` as some docs suggest)
- Full CI/CD pipeline now operational end-to-end: source push → GitHub Actions → OIDC auth → pac solution pack → import to PRI-Books-Dev

**Issues encountered (resolved):**
- PowerShell 5 vs 7 syntax differences in helper scripts (`@{}` splatting, `Set-Variable -Force` semantics) — kept scripts PS7-clean
- Solution shell didn't exist in PRI-Books-Dev until first bootstrap import — the workflow's first run created it
- Choice column default value bug (PowerShell `[int]` defaults to 0, which can collide with a real option value) — `DefaultFormValue` only emitted when a non-zero default is intended
- Metadata cache hiccup on `rm_entity` creation; recovered on retry
- Workflow initially packed as Managed; changed to Unmanaged for dev — managed dev imports lock the table for further customization
- `pac` CLI flag is `--githubFederated`, not `--federatedToken`

**Next:** Phase 4 — Chart of Accounts.

### Phase 2 — Azure SQL provisioning + CI/CD foundation (2026-05-19)

- Three SQL migrations applied to `DatastreamBooks-Dev`, all recorded in `dbo.SchemaMigrations`:
  - V0001: `ledger` / `audit` / `reports` / `archive` schemas + `dbo.SchemaMigrations` metadata
  - V0002: `ledger.GeneralLedgerEntries` (30 cols, 5 indexes, universal `DENY UPDATE / DELETE / REFERENCES / ALTER` on `public`)
  - V0003: four contained SQL users (`dsb_app`, `dsb_migrate`, `dsb_reader`, `dsb_admin`) with least-privilege grants and explicit per-user `DENY UPDATE / DELETE` on the ledger. Passwords parameterized via sqlcmd-style `$(pw_dsb_*)` tokens — committed source contains no plaintext; the apply runner generates cryptographically random 32-char passwords in-memory.
- Immutability validation against the live DB:
  - As `dsb_admin` (db_owner): UPDATE / DELETE / TRUNCATE all returned permission-denied errors.
  - As `dsb_app` (least privilege): UPDATE / DELETE returned permission-denied errors.
  - As `priadmin` (SQL admin / dbo): UPDATE / DELETE / TRUNCATE all succeeded — `dbo` bypasses DENY by documented SQL Server design. The auth strategy treats `priadmin` as break-glass-only; protection against its abuse is organizational + audit-based, not permission-based.
  - Test row `EntryId=1` (JournalEntryNumber `TEST-IMMUT-001`) remains in the ledger as permanent evidence. Full record at `docs/architecture/immutability-validation.md`.
- Entra app registration `datastream-books-cicd` created with service principal `510f68ee-1d89-46b1-bc4b-9f127d8e9f62`. Federated credential `github-main` bound to `repo:ryanm-plastic-recycling/datastream-books:ref:refs/heads/main`. SP granted System Administrator on PRI-Books-Dev via `pac admin assign-user --application-user`.
- `deploy-dev.yml` rewritten for OIDC; no client secrets stored anywhere. Workflow triggers on push to `main` (per solo-dev branching policy). First-run safety added: pack/import steps skip gracefully when `solution/src/Entities` is empty.
- Four GitHub Secrets added to the repo (AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_SUBSCRIPTION_ID, POWER_PLATFORM_ENVIRONMENT_URL) via the web UI — `gh` CLI is not installed on the dev machine.
- New runbooks: `docs/runbooks/cicd-setup.md` (reproducible Entra + workflow setup) and `docs/runbooks/sql-account-management.md` (one-line ALTER USER rotation, Key Vault pattern, `priadmin` governance).
- AGENTS.md gained Pull-Before-Work and Roadmap-Maintenance principles. Session-log file replaced by this living roadmap.

### Phase 1 — Repo foundation + design sprint (2026-05-19)

- Repo skeleton built (every folder from `docs/repo-structure.md`; empty ones use `.gitkeep`).
- Dataverse solution initialized: `DatastreamBooks` under publisher `Ryan McCauley` / `RyanMcCauley`, prefix `rm`, option-value prefix `12619`. Values **verified** by exporting `Datastream` from pri-dev and reading Solution.xml — caught and corrected a prior `dsb` guess; led to new "Verification is mandatory" principle in AGENTS.md.
- Plugin projects scaffolded: `DatastreamBooks.Plugins` (net462) + `DatastreamBooks.Plugins.Tests` (net48, xUnit + FluentAssertions + FakeXrmEasy v2). Domain subfolders (Posting, Validation, PeriodLock, etc.) with `.gitkeep`.
- SQL migration drafts: `V0001__initial_schema.sql` (placeholder) and `V0002__general_ledger_entries.sql` (full draft of the append-only hash-chained ledger with DENY grants and rollback notes).
- GitHub Actions `deploy-dev.yml` stubbed with all required secrets/variables documented.
- PowerShell scripts: `setup-dev`, `auth-env`, `pull-solution`, `push-solution`, `run-sql-migration` (stub).
- Architecture docs: `data-model.md`, `security-model.md`, `immutability-design.md`. Controls docs: `sod-matrix.md`, `approval-policies.md`, `audit-controls.md`.
- Decision-log sweep: every CFO reference replaced with President / Executive Sponsor.
- Branching Policy formalized after a merge-drama incident: solo-dev project, work directly on main, no branches, no worktrees inside the OneDrive-synced repo.
- ERP metadata reference snapshot imported at `docs/reference/erp-metadata/` (REFERENCE ONLY — describes PRI-Datastream ERP, not Books). Six patterns extracted to `docs/architecture/erp-pattern-notes.md` (4-column master-data shape; picklist + Virtual companion; `rm_customer` is the shared customer master Books AR should reference, not duplicate; ~47 custom tables in ERP with median ~10 columns).
- OneDrive lessons learned: pause sync before heavy git work; avoid worktrees inside the synced tree; resolve `(1)` duplicate conflict files at the OS level before continuing in git.

### Phase 0 — Strategy and pre-build (2026-05 and earlier)

- President memo signed off; Datastream Books selected over Business Central based on 5-year cost (~$70K–$135K savings), strategic AI-driven document discrepancy detection opportunity, and Lighthouse IT-modernization alignment.
- Tenant inventory captured; PRI-Books (managed prod) and PRI-Books-Dev (unmanaged sandbox) provisioned; all four `pac` auth profiles (`pri-books`, `pri-books-dev`, `pri-datastream`, `pri-dev`) active.
- Pam designated **Finance System Owner** (not consultant/SME), mirroring the Datastream ERP departmental ownership pattern. President confirmed as Executive Sponsor; rollout meeting planned with President + COO.
- Architectural pillars locked in: hybrid Dataverse + Azure SQL store with immutable hash-chained ledger; server-side plugins for financial logic (not Power Automate); multi-entity from day one; ChangeRequest workflow with multi-image attachment support built into the app.
- Decision log, executive questionnaire, president memo, repo-structure doc all authored. Executive questionnaire still has open items (legal entity inventory §1, approval thresholds §3, COA owner §11) — these block downstream work.

## Future Phases

> Placeholders. Order is approximate; reshuffles as priorities shift.
> (Phase 6 has been promoted to Current Phase — see top of file.)
>
> **Numbering note (added 2026-05-20):** The roadmap now uses two parallel
> "Phase 7" labels — one for the backend track (Vendor/Customer
> Integration, listed first below) and one for the UI track (UI/UX
> Front-End Build, listed last below as it is sequenced after **all**
> backend phases complete). The dual label preserves the user-facing
> phase names already in circulation while making chronology explicit.
> Backend track runs sequentially through Phase 11+; UI track is dormant
> until backend completes. See
> [`decisions/phase-7-ui-design.md`](decisions/phase-7-ui-design.md) for
> the UI-track planning artifact.

### Phase 7 (Backend Track) — Vendor / Customer Integration with ERP

> Promoted to Current Phase at the top of this file. Retained here for the Future-Phases numbering continuity. Architectural ownership question closed by §70 (Books is system of record; ERP receives Books-mastered field projection via plugin-driven push; ERP retains write authority on operations-only fields). Pam-consult portion (field list, intake workflow, 1099 rules, approval routing) lands in the week-of-2026-05-25 conversation. Books AR / vendor cross-solution lookup design follows; ownership boundary documented per `erp-pattern-notes.md` Pattern 3.

### Phase 8 — AP / AR Core

Bills, invoices, receipts, aging reports. NACHA file generation for ACH payments (replaces the Leahy dependency tied to Macola). Track1099 integration for 1099 generation and W-9 collection. **Email + Teams notifications** (deferred from Phase 7 UI track per decision §53 — in-app notifications only in v1 UI; email and Teams added in this phase via Microsoft Graph API and Teams app registration respectively).

**Scope question open (flagged 2026-05-21 per audit):** decision §53 deferred email + Teams "to Phase 8+" without an explicit row pinning the Phase 8 scope. Tagged for the Phase 8 design conversation -- confirm Graph-API email + Teams app registration are Phase 8 deliverables (vs. Phase 9 or later) when Phase 8 opens.

### Phase 9 — Period Close + Reporting

Native model-driven reports: Trial Balance, Balance Sheet, P&L, Cash Flow, AR/AP aging, JE audit trail, ChangeRequest log. Period close attestation flow. Hash-chain verification job. Note: report **rendering** (on-screen, Excel, PDF) and **drill-down** are part of Phase 7 (UI Track) Phase 7C — this phase delivers the backend report queries, snapshot writes, and aggregation logic the UI sits on top of.

### Phase 10 — Macola Data Migration + Cutover

Historical archive into `archive` schema. Parallel run with Macola for at least one full close cycle (ideally two). Penny-perfect reconciliation. User-driven green light. Cutover at fiscal-period boundary per decision log §26.

### Phase 11/12+ — Remaining backend items

Placeholder for any backend work that surfaces between now and the close of Phase 10 — likely candidates include a deeper PCF reporting layer, additional posting-plugin coverage as edge cases emerge, and the nightly hash-chain verification job promotion to production. Items here block Phase 7 (UI Track) start.

**Intentionally backing-free per audit 2026-05-21:** this phase has no specific decision row driving its scope; the placeholder framing is the decision. Items land here as they surface during Phase 6B-10 execution.

### Phase 7 (UI Track) — UI/UX Front-End Build

> **Dormant until backend completes.** Planning artifacts live in
> [`decisions/phase-7-ui-design.md`](decisions/phase-7-ui-design.md). The
> first session prompt is drafted at
> [`runbooks/phase-7a-foundation-prompt.md`](runbooks/phase-7a-foundation-prompt.md)
> and marked DRAFT until backend Phase 11/12+ completes.

**16-20 weeks total (build + UAT).** Six sub-phases:

- **Phase 7A — Foundation (3 weeks).** Sitemap, global search PCF, breadcrumbs, recent items widget, role-aware homepage shell, Datastream ERP visual styling extraction, finance-specific security role scaffolding (7 roles per decision §61).
- **Phase 7B — Core Transactional Screens (4-5 weeks).** Hybrid JE entry (Excel-grid + form on same screen), AP bill entry + approvals queue, AR invoice + receipt entry, approval queue widget.
- **Phase 7C — Reports (3-4 weeks).** Balance Sheet, Income Statement, Cash Flow, Trial Balance, aging reports — all three formats (on-screen, Excel export, PDF export) with universal drill-down per decision §55.
- **Phase 7D — Specialty Screens (2-3 weeks).** Period close + TB review, multi-entity dashboard, Change Request management, vendor/customer master maintenance.
- **Phase 7E — Refinement and CR Burn-down (2-3 weeks).** Dedicated CR triage and implementation window; no new feature scope.
- **Phase 7F — UAT (2-3 weeks calendar).** Persona-specific UAT scripts; Pam signs off.

UX direction is owned by IT; Pam exercises ownership via the in-app Change Request system once pages land in dev (per decision §57 — no initial design sign-off; CR-based ownership consistent with the existing CR design and Owner framing). All 17 UX decisions are captured as §46-§62 in [`decisions/datastream-books-decisions.md`](decisions/datastream-books-decisions.md).

### Future / Phase 2+ Work (out of v1 cutover scope)

- **Document AI (Claude API replacement of AI Builder).** Phase 2 strategic value driver — AI-driven invoice/PO/receipt discrepancy detection enabling headcount reallocation from manual validation. Pattern: documents land in SharePoint → Claude API extracts structured data → matched against POs/receipts → clean matches auto-route, exceptions to human review.
- **Power BI reporting.** Paginated reports, dashboards, cross-entity analytics. Replaces or augments native model-driven reports.
- **Bill.com / Ramp / Bank API integration.** Direct AP payment execution beyond NACHA file generation.
- **Credit limit management and enforcement.** Customer credit limits + risk scoring (decision log §17 — deferred from v1).
- **Limble PO replacement.** Bring PO workflow under Datastream rather than a parallel system.
- **Mobile-optimized UI.** Out of v1 scope per decisions §51.

## See Also

- [`decisions/datastream-books-decisions.md`](decisions/datastream-books-decisions.md) — full decision log
- [`decisions/concurrence-log.md`](decisions/concurrence-log.md) — stakeholder visibility + 5-business-day concern window for decisions made under §71 authority (Technical Strategic Lead architecture)
- [`decisions/phase-7-ui-design.md`](decisions/phase-7-ui-design.md) — Phase 7 (UI Track) planning artifact
- [`memos/decisions-required-master-list.md`](memos/decisions-required-master-list.md) — master decisions sheet with Technical Strategic Lead-decides / Pam-decides / Exec-decides categorization
- [`memos/pam-conversation-prep-2026-05-w22.md`](memos/pam-conversation-prep-2026-05-w22.md) — consolidated agenda for the upcoming Pam conversation (week of 2026-05-25)
- [`backlog.md`](backlog.md) — 53-item backlog with Priority Index (P0-P3)
- [`risk-register.md`](risk-register.md) — live risk register (Phase 7 risks R-7-01 through R-7-05; audit risks R-A-17 through R-A-20)
- [`runbooks/phase-7a-foundation-prompt.md`](runbooks/phase-7a-foundation-prompt.md) — DRAFT prompt for the first Phase 7 (UI Track) session (deferred per §67)
- [`runbooks/r-a-19-business-rule-implementation.md`](runbooks/r-a-19-business-rule-implementation.md) — BL-47 maker-portal handoff for R-A-19 mitigation (Pam-demo-blocker)
- [`architecture/`](architecture/) — data model, security, immutability, ERP patterns
- [`controls/`](controls/) — SoD matrix, approval policies, audit controls
- [`reference/erp-metadata/`](reference/erp-metadata/) — ERP solution metadata snapshot (REFERENCE ONLY)
- [`../AGENTS.md`](../AGENTS.md) — operating instructions for AI coding agents
- [`../CLAUDE.md`](../CLAUDE.md) — Claude-specific extensions including the Operating Principles section codified under §68
