# Datastream Books — Decision Log & Project Notes

> Living document. Update continuously as decisions are made and assumptions evolve.
> Last updated: May 22, 2026

---

## Project Identity

- **Internal name:** Datastream Books
- **Purpose:** Replace Macola accounting system with an internally-built finance application
- **Platform:** Microsoft Dataverse (model-driven app) + Azure SQL (hybrid)
- **Parallel system:** Datastream ERP (operations, already in production on Dataverse)
- **Strategic context:** Advances The Lighthouse IT modernization strategy
- **Authentication:** Microsoft Entra ID
- **Estimated user count:** 15 (5 accountants + 10 occasional contributors)
- **Tenant account:** ryanm@plastic-recycling.net
- **GitHub repo:** `datastream-books`
- **Finance System Owner:** Pam (controller-level accountant)
- **Executive Sponsor:** President

---

## Tenant Environment Inventory

| Environment | Type | URL | Purpose | Status |
|---|---|---|---|---|
| Plastic Recycling (default) | Default | orgd0b31c1b.crm.dynamics.com | Tenant default | Active |
| PRI-Datastream | Production | datastream.crm.dynamics.com | Datastream ERP | Active |
| PRI-Dev | Sandbox | pridev.crm.dynamics.com | ERP dev/test | Active |
| PRI-Sales | Production | prisales.crm.dynamics.com | Sales/CRM | Active |
| **PRI-Books** | **Production (Managed)** | **books.crm.dynamics.com** | **Datastream Books PROD** | **Deployed** |
| **PRI-Books-Dev** | **Sandbox (Unmanaged)** | **booksdev.crm.dynamics.com** | **Datastream Books DEV** | **Deployed & Authenticated** |

### Auth Profiles Established

```
pri-books          - PRI-Books production
pri-books-dev      - PRI-Books-Dev sandbox (primary dev target)
pri-datastream     - PRI-Datastream (ERP prod, for integration testing)
pri-dev            - PRI-Dev (ERP sandbox)
```

All four auth profiles confirmed active via `pac auth list`.

---

## Why We Are Building This

- Macola is being deprecated by the vendor in calendar year 2026
- Vendor ERP/finance replacements quoted at $300K–$1.7M, outside budget
- Business Central viable at ~$100/user/month + AP automation add-ons
- Internal Dataverse build leverages existing platform investment
- Datastream ERP build has proven the platform and team capability
- **Primary value driver:** AI-driven document discrepancy detection enables headcount reallocation
- **Secondary strategic benefit:** Cutover advances The Lighthouse (local DC + file server decom, full Entra migration)

---

## Decisions Made

| # | Date | Decision | Rationale |
|---|---|---|---|
| 1 | 2026-05-19 | Project name: Datastream Books | Matches accounting vernacular |
| 2 | 2026-05-19 | Skip local prototype | Team already knows Dataverse |
| 3 | 2026-05-19 | Separate Dataverse environment | Audit isolation, security boundary |
| 4 | 2026-05-19 | Hybrid data: Dataverse + Azure SQL | Dataverse for master/UX; Azure SQL for immutable ledger |
| 5 | 2026-05-19 | Microsoft Entra ID authentication | Consistency with Datastream ERP |
| 6 | 2026-05-19 | Payroll out of scope (Paylocity stays) | Permanent decision |
| 7 | 2026-05-19 | BC POC waived per executive direction | Build approach committed |
| 8 | 2026-05-19 | CI/CD via GitHub + Power Platform Build Tools | Native ALM |
| 9 | 2026-05-19 | Strategy in Claude.ai Project; execution in Claude Code | Repo is source of truth |
| 10 | 2026-05-19 | Per-app licensing (managed manually by IT) | Cost flexibility |
| 11 | 2026-05-19 | SharePoint for document storage | Native, audit-friendly |
| 12 | 2026-05-19 | Document AI long-term: Claude API | Phase 2 |
| 13 | 2026-05-19 | Track1099 for 1099 generation and W-9 collection | Compliant, low-friction |
| 14 | 2026-05-19 | Microsoft Graph API for customer/vendor email | Native, audit trail |
| 15 | 2026-05-19 | Native reporting v1; Power BI Phase 2 | Reduce dependency |
| 16 | 2026-05-19 | NACHA file generation in v1 for ACH | Removes Leahy dependency |
| 17 | 2026-05-19 | Credit limit management deferred to Phase 2 | Not used today |
| 18 | 2026-05-19 | Server-side plugins preferred over Power Automate | Atomicity, audit defensibility |
| 19 | 2026-05-19 | Change Management built into the app | Audit-friendly history |
| 20 | 2026-05-19 | Environment names: PRI-Books / PRI-Books-Dev | Matches PRI-* pattern |
| 21 | 2026-05-19 | PRI-Books-Dev sandbox as unmanaged dev source | Required by managed prod env |
| 22 | 2026-05-19 | Vendors/customers added as needed | Natural data hygiene |
| 23 | 2026-05-19 | Pre-populate standard COA; finance modifies | "React to draft" pattern |
| 24 | 2026-05-19 | Foreign currency NOT in v1; schema future-proofed | USD only |
| 25 | 2026-05-19 | Multi-entity REQUIRED in v1 | Multiple legal entities |
| 26 | 2026-05-19 | Cutover at fiscal period boundary; user-driven green light | Risk minimization |
| 27 | 2026-05-19 | TalentLMS + Scribe for training | Existing pattern |
| 28 | 2026-05-19 | PRI-Books environment deployed and authenticated | Build infrastructure ready |
| 29 | 2026-05-19 | PRI-Books-Dev sandbox deployed and authenticated | Dev environment ready |
| 30 | 2026-05-19 | **Pam designated as Finance System OWNER (not consultant)** | **Mirrors Datastream ERP departmental owner pattern** |
| 31 | 2026-05-19 | **ChangeRequest must support multi-image attachments** | **Concrete evidence beats verbal description** |
| 32 | 2026-05-19 | **President is executive sponsor; rollout meeting with President and COO** | **Cascades ownership framing from top** |
| 33 | 2026-05-19 | **Phase 3 complete. 5 foundational tables in PRI-Books-Dev (rm_accounttype, rm_accountcategory, rm_fiscalyear, rm_fiscalperiod, rm_entity). Seed data loaded. Full CI/CD pipeline operational end-to-end including pac federated identity (--githubFederated) and Unmanaged solution deployment to dev. Dev environment uses unmanaged solutions; future prod workflow will use managed.** |
| 34 | 2026-05-19 | **Phase 4 complete. rm_chartofaccount built in PRI-Books-Dev (14 user-defined columns + lookups to rm_accounttype, rm_accountcategory, rm_entity, self-referencing rm_parentaccount; alternate key on (rm_entity, rm_accountnumber) verified to reject duplicates). Standard 54-row Chart of Accounts seeded under placeholder "Default Operating Entity" (rm_entitycode=DEFAULT) per decision §23. Per-entity COA chosen over globally-shared COA (matches Macola, simpler than entityactivation intersect). rm_accountnumberingscheme deferred — would be soft-validation data with no immediate consumer. Two committed idempotent seed scripts: seed-default-entity.ps1 + seed-rm_chartofaccount.ps1.** |
| 45 | 2026-05-20 | **SP `cicd-sp-client-secret` rotated — inadvertent terminal exposure.** A diagnostic command using `--query value -o tsv` was executed against the Vault, exposing the SP client secret to terminal output (which is captured in tool/agent logs). Treated as exposure. New credential created on the `datastream-books-cicd` app reg (keyId `987e5ce6-934e-48db-a3d0-8972e98c7d63`, 2-year expiry, expires 2028-05-20). The exposed credential was deleted from the app reg, replaced in `kv-datastream-books / cicd-sp-client-secret`, and the GitHub Actions repository secret `AZURE_CLIENT_SECRET` refreshed to match. Safe-diagnostic patterns added to [`key-vault-management.md`](../runbooks/key-vault-management.md) — never use `--query value` for presence checks; use `--query "length(value) > 0"` instead, which returns a boolean and never echoes the value. |
| 44 | 2026-05-20 | **Both Dataverse-related SPs hold `Key Vault Secrets User` at vault scope.** During Power Platform Key Vault integration troubleshooting it became clear that two SPs participate in Secret-type Dataverse Environment Variable resolution: the standard Dataverse SP (object id `567ae524-268d-4de9-8054-7e26da9fa7f0`, granted first as a guess) and the Dataverse Resource Provider SP (object id `4f026a85-a88e-4674-baf4-45833854f411`, which is the one that actually authenticates during the maker-portal save). Both retained — the Resource Provider grant is the one Power Platform documentation references; the standard SP grant is retained as defense-in-depth. Documented as the integration prerequisite in [`key-vault-management.md`](../runbooks/key-vault-management.md) and [`credential-access-design.md`](../architecture/credential-access-design.md). |
| 43 | 2026-05-20 | **Key Vault firewall (dev) changed to `defaultAction = Allow` with `bypass = AzureServices`.** Power Platform's native Key Vault integration (the path that backs Secret-type Dataverse Environment Variables pointing at the Vault) resolves secrets from a fleet of Microsoft-managed IPs that change over time and cannot be pre-pinned in an allow-list. Under the original `defaultAction = Deny` + IP-allow-list policy, every Secret-type env var save attempt failed 403 because the resolver's source IP was unknown. Two viable resolutions: (a) `Allow + RBAC` (chosen for dev — simple, documented Microsoft path), or (b) Private Endpoint with VNet-integrated Dataverse enterprise policy (planned for prod). Bypass `AzureServices` is retained so Microsoft-internal services keep functioning; RBAC remains the actual access control. IP allow-list (`12.201.35.226/32`) retained as informational. Prod will NOT use this shortcut — Private Endpoint is the prod plan. |
| 38 | 2026-05-21 | **Phase 6B credential access pattern: plugin reads connection string from Key Vault at runtime via SP client secret stored in Dataverse Environment Variable.** Considered alternatives: (a) connection string stored directly in a Secret env var populated from KV at deploy time, and (b) managed identity on the Dataverse environment. Chose runtime-fetch pattern after weighing the trade-off — KV remains single source of truth, rotations propagate within the 5-minute TTL without redeploys. MI is not currently supported by the Dataverse plugin sandbox. To avoid ILRepacking the Azure SDK chain (Azure.Identity + Azure.Security.KeyVault.Secrets + Azure.Core + System.Memory + Newtonsoft.Json + …) into the signed plugin DLL, the implementation uses raw `HttpClient` + manual OAuth2 client-credentials + KV REST API + a tiny single-field JSON extractor (~200 LoC, fully unit-tested). Sandbox needs outbound HTTPS:443 to `login.microsoftonline.com` and `*.vault.azure.net`. Bearer token and secret value both cached in process-static fields (TTLs 55 min and 5 min respectively). |
| 39 | 2026-05-21 | **Phase 6B hash byte layout finalized.** Per-field length-prefixed UTF-8 (4-byte big-endian length, then UTF-8 bytes; nulls = 4-byte sentinel `0xFFFFFFFF`). Previous row hash appended raw (32 bytes, NOT length-prefixed). SHA-256 over the whole sequence. Per-field formatters: Guid → `ToString("D")` lowercase; int/long → invariant culture; decimal → `F4` invariant (always 4 decimals); date → `yyyy-MM-dd`; timestamp → UTC + `yyyy-MM-ddTHH:mm:ss.fffZ`. Genesis = 32 zero bytes per EntityId. Pinned by 16 unit tests in `LedgerRowHasherTests.cs` — failing any of these is treated as a chain-breaking event. |
| 40 | 2026-05-21 | **Phase 6B concurrency strategy: per-entity chain head locked via `WITH (UPDLOCK, HOLDLOCK)`.** Before computing a new row's hash, the writer reads `SELECT TOP 1 RowHash … WHERE EntityId = @id ORDER BY EntryId DESC` with update + hold locks inside its SQL transaction. A concurrent writer for the same EntityId blocks on the SELECT until the first writer commits, then reads the now-updated chain head. Different EntityIds do not contend. Trade-off accepted: per-entity serialization on posting throughput, but solo-dev concurrency keeps this irrelevant; under multi-user write pressure the next move is to chunk the chain or split entity scope, not to weaken the lock. |
| 41 | 2026-05-21 | **Phase 6B failure semantics: rollback-and-throw, not "PostFailed" status.** On any SQL or KV failure during the dual-write, the plugin throws `InvalidPluginExecutionException`. Dataverse rolls back its transaction (including the Approved→Posted flip and the `rm_postedby_user` / `rm_posteddatetime` stamps), leaving the JE at `Approved`. The user retries once the underlying issue is fixed. We did NOT add a `PostFailed` status value: doing so would require a schema change, and the rollback-and-throw pattern delivers the same audit story without one. |
| 42 | 2026-05-21 | **Phase 6B code-complete; live e2e validation deferred.** All code, tests (41 total, 25 new, all green), and documentation landed in this session. The first-real-JE end-to-end test in PRI-Books-Dev requires three out-of-band actions before it can run: populate the five `rm_sqlkv*` Dataverse Environment Variables in the maker portal; register Step 10 (Stage 40 PostOperation on Update rm_journalentry, with PreImage + PostImage) via the Plugin Registration Tool; deploy the new plugin DLL via CI. The runbook section in `plugin-registration.md` documents the steps. Validation result will be appended to `immutability-validation.md` once the test runs. |
| 37 | 2026-05-20 | **Key Vault SP credential corrected.** Session-generated credential (keyId `efb08aa2-a161-4f54-92b9-011197d07c88`) was replaced by user-generated credential (keyId `3eaf8a5a-1cf8-4d5c-89e5-e322567abf4a`, 2-year expiry, expires 2028-05-20) to align with rotation standard. Old credential deleted from app registration. Key Vault secret `cicd-sp-client-secret` now contains the correct value. GitHub Actions `AZURE_CLIENT_SECRET` aligned. Closes the "unknown credential" open item from the prior session — origin was user-side, not anomalous. |
| 36 | 2026-05-20 | **Key Vault `kv-datastream-books` provisioned in shared Datastream RG (East US, standard tier, RBAC mode).** Soft-delete 90 days + purge protection on (irreversible). Network: public access enabled but firewall = Deny default with explicit allow for ryanm dev IP only; `bypass = None` (no AllowAzureServices — explicit grants only). Diagnostic logging enabled day one (`AuditEvent` + `AllMetrics`) to existing Log Analytics workspace `7d6df7e7-d474-4284-be38-ba20eec9ef7f-Datastream-EUS`. RBAC at vault scope: ryanm = Key Vault Administrator (setup/break-glass); `datastream-books-cicd` SP = Key Vault Secrets User (Get-only — Phase 6B plugin runtime read path). `dsb_app` SQL password rotated and verified live: positive (SELECT @@VERSION, SELECT TOP 1, INSERT-then-ROLLBACK) all OK; negative (UPDATE/DELETE both throw SQL 229 permission denied) confirms the DENY architecture survives the rotation. dsb_app connection string stored as `dsb-app-connection-string`. Fresh 12-month client secret generated for the cicd SP and stored as `cicd-sp-client-secret` (federated identity remains the primary auth path for GitHub Actions; the client secret is break-glass only). Phase 6B is now unblocked — the plugin runtime can authenticate to Azure SQL as `dsb_app` via Key Vault, never as `priadmin`. New runbook `docs/runbooks/key-vault-management.md` is the single source of truth for Vault operations (RBAC, secret inventory, rotation, break-glass). Cross-references added in `sql-account-management.md`, `security-model.md`, and `cicd-setup.md`. **Risk surfaced:** an unrecognized client-credential ("secret", keyId `3eaf8a5a-1cf8-4d5c-89e5-e322567abf4a`, created today at 15:46:33Z, ~8 minutes before the Key Vault session began) was found on the `datastream-books-cicd` app reg during this session and was **NOT** deleted; its origin is unknown and needs investigation before disposition. |
| 35 | 2026-05-19 | **Phase 5 complete. rm_journalentry built in PRI-Books-Dev: primary `rm_journalentrynumber` (string 50, ApplicationRequired); 12 user-defined attributes total; 7 OneToMany lookups (required: rm_entity, rm_fiscalperiod; optional systemuser triple for SoD: rm_createdby_user / rm_approvedby_user / rm_postedby_user; two self-referencing reversal lookups rm_reversesje / rm_reversedbyje); status picklist Draft/PendingApproval/Approved/Posted/Reversed/Voided; JE type picklist Standard/Recurring/Adjusting/Closing/IntercompanyClearing/Reversal. rm_journalentryline built: primary rm_journalentrylinename, 7 user-defined attributes, 3 lookups (required parent rm_journalentry with Cascade All; required rm_account → rm_chartofaccount; required rm_entity denormalized from header). Both organization-owned with audit ON. Five Phase 5 design decisions documented in data-model.md: (1) autonumber deferred to Phase 6 plugin (Dataverse native autonumber can't easily do entity-prefixed numbering across tables); (2) totals as plain decimal columns (not roll-up — staleness; not calculated — can't cross transactional boundaries); (3) rm_createdby_user as a deliberate immutable user lookup distinct from Dataverse system createdby (which is reassignable); (4) Cascade All on header→lines (Draft cleanup; Posted-JE immutability is the plugin's job at the status level, not the relationship); (5) rm_entity denormalized onto lines for query performance + entity-scoped RLS. New transient-helper lesson: 0x80040216 / "An unexpected error occurred" on the first attribute POST immediately after CreateEntity is a metadata-cache lag and is reliably resolved by retrying after a few seconds — folded into the same poll-on-lock retry loop already in place for 0x80071151 and 429.** |
| 46 | 2026-05-20 | **Phase 7 (UI Track) UI architecture: Dataverse model-driven app + custom React pages for high-value screens.** Default Dataverse UI is sufficient for list views, simple forms, and admin screens. Custom React pages reserved for screens where the default UI is genuinely painful: JE entry (hybrid grid + form), financial reports with drill-down, multi-entity dashboard. Hybrid keeps Microsoft platform investment paying off where it can, and concentrates custom build effort where it produces real UX value. See [`phase-7-ui-design.md`](phase-7-ui-design.md). |
| 47 | 2026-05-20 | **Phase 7 primary user personas: Controller, AP Clerk, AR Clerk, Approver, Casual Contributor (5 base finance personas).** UI is role-aware: same shared dashboard, but the widgets shown and the default views differ by role. Approver sees a queue widget; AP Clerk sees recent bills + open vendor work; Controller sees period-close status and exception reports. Multi-persona without building separate apps per persona. |
| 48 | 2026-05-20 | **Phase 7 v1 screens: all 8 v1-priority screens included.** (1) JE create/edit/post, (2) AP bill entry + approvals, (3) AR invoice + receipts, (4) period close + TB, (5) multi-entity dashboard, (6) change requests, (7) financial reports (BS/P&L/CF), (8) vendor/customer master. Finance cannot cut over without any of these — partial cutover is not viable. |
| 49 | 2026-05-20 | **Phase 7 visual identity: match Datastream ERP color palette (predominantly blue) and corner logo placement.** Sister-app visual consistency is the only hard constraint. Otherwise IT decides pragmatically. Pattern inspiration from competitor finance UIs (Business Central, NetSuite, QuickBooks, Sage Intacct) where useful. Prioritize functional design over brand expression. **[Amended 2026-05-21 -- Phase 7A Session S2 visual identity extraction found that PRI-Datastream ERP has no custom theme; its app module sets only `AppChannel=1` and the three legacy `theme` records are all stock Microsoft. "Match ERP color palette" is therefore not literally implementable. Operative rule per [`../architecture/ui-styling.md`](../architecture/ui-styling.md): Books defines its own minimal theme via CSS variables (editable by accounting team without code changes), preserves logo continuity with ERP (sharing the PRI corporate logo `rm_DatastreamBooksLogo`, sourced from ERP's `rm_PRILogoCircle20211027`), and uses Fluent UI v9 defaults that ERP also surfaces. The sister-app visual consistency intent of §49 is preserved via logo + Fluent UI baseline.]** |
| 50 | 2026-05-20 | **Phase 7 JE entry: hybrid mode — Excel-like grid for power users, simpler form mode for clerks.** Toggle on the same page, not two separate screens. Pam's Macola muscle memory is Excel-style entry; clerks need form-mode guard rails. Both modes write to the same Dataverse entity through the same `PostJournalEntryPlugin` path. Hybrid is the single most complex Phase 7 screen and will be the long pole of Phase 7B. |
| 51 | 2026-05-20 | **Phase 7 mobile/tablet out of scope for v1; desktop only.** Finance team works at desks. Building responsive layouts would add 2-3 weeks without changing v1 utility. Mobile-optimized UI was already listed in Phase 2+ work — this re-confirms it. |
| 52 | 2026-05-20 | **Phase 7 homepage: single shared dashboard with role-aware widgets within.** Not separate dashboards per persona. Finance users already use ERP dashboards built on the same model; familiar concept reduces learning curve. Visual consistency with ERP wins. |
| 53 | 2026-05-20 | **Phase 7 notifications: in-app only for v1.** Email and Teams notifications deferred to Phase 8+. Reduces v1 dependencies — in-app notifications are zero-dependency; email and Teams require Graph API plumbing and Teams app registration that would expand scope. Can be added later without UI rework because the notification surface is decoupled. |
| 54 | 2026-05-20 | **Phase 7 reports: all three formats equally important — on-screen rendering, Excel export, PDF export.** Finance lives in Excel for analysis; on-screen for quick checks; PDF for external sharing (auditors, banks, lenders). Implies real investment in all three pipelines, not "on-screen plus a CSV button". Library choices and pipeline architecture decided at Phase 7C kickoff. |
| 55 | 2026-05-20 | **Phase 7 report drill-down: universal — every figure in BS, P&L, Cash Flow is clickable, drills to underlying transactions. For all roles.** Power feature that elevates Books beyond Macola and BC defaults. **Architectural implication:** reports must be live queries against `ledger.GeneralLedgerEntries` retaining row-level transaction provenance, NOT pre-aggregated snapshots. Closed-period reports still use `ReportSnapshots` for the figures themselves (per [`../architecture/immutability-design.md`](../architecture/immutability-design.md) §G), but drill-down on a closed period reads through the snapshot to the underlying ledger rows. |
| 56 | 2026-05-20 | **Phase 7 save semantics: explicit Save button with explicit draft state. No auto-save.** Matches Macola mental model (users expect to control when work persists). Aligns with audit-defensibility — discrete save events with clear audit trail are easier to explain to an auditor than a stream of auto-saves. Supports approval workflow integrity: Submit-for-Approval is a distinct event, not "the last edit before someone clicked Approve". |
| 57 | 2026-05-20 | **Phase 7 Pam validation model: NO initial design sign-off.** IT decides direction. Pam exercises ownership via the in-app Change Request system after pages land in dev. **Why this works:** Pam reacts better in REACT mode than PLAN mode (validated through prior project history); the CR system with multi-image attachments (decision §31) is the existing, working ownership mechanism. Compatible with the Finance System Owner framing — owners describe pain on real artifacts, not on mockups. Risk: Pam may file heavy CR volume in the first weeks of Phase 7B; mitigated by the Phase 7E CR burn-down window. |
| 58 | 2026-05-20 | **Phase 7 front-end timing: STRICT SEQUENTIAL — front-end build does not begin until all backend phases (Phase 6B through the expected Phase 11/12+ backend items) are complete.** Pam first sees UI ~7 months from project start. Risks accepted explicitly: stakeholder confidence during the invisible period, late discovery of UX issues that would have surfaced earlier in a parallel build, late visibility into integration issues with the live ledger. Trade-off rationale: sequencing simplifies coordination (one front-end build against a stable backend), concentrates front-end attention when it happens, and avoids the cost of rebuilding UI against backend that is still in flux. |
| 59 | 2026-05-20 | **Phase 7 reference material: Datastream ERP for color palette + logo placement; competitor finance UIs (Business Central, NetSuite, QuickBooks, Sage Intacct) for pattern inspiration where useful; otherwise design fresh for finance-specific patterns.** Provides bounded creative freedom — avoids designer-in-cul-de-sac syndrome where every design choice gets relitigated. Reduces churn by giving the team a small set of "look at how X does it" references. |
| 60 | 2026-05-20 | **Phase 7 design system: Fluent UI v9 defaults; minimal custom design system layer.** Document component conventions as encountered in code, no upfront design system documentation effort. Avoids weeks of design-system setup before any user-visible progress. Leverages Microsoft's ongoing investment in Fluent UI. Lowers bus factor — the design system is "Fluent UI" plus a small documented delta, not a custom artifact only Ryan understands. |
| 61 | 2026-05-20 | **Phase 7 security roles: finance-specific only — Controller, AP Clerk, AR Clerk, Approver, Casual Contributor (5 base roles), plus System Admin and Read-Only Auditor (7 total).** NOT aligned to the ERP role structure. Finance personas (Controller, AP, AR, approver) don't map to ERP personas (warehouse, transportation, ops, sales) — overlaying would force one team's mental model on the other. Clean separation simplifies SoD audits (each app's roles are reviewable in isolation). Detailed permissions per role are populated during Phase 7A as pages get attached. Read-Only Auditor is a deliberate addition for external auditors. See [`../architecture/security-model.md`](../architecture/security-model.md). |
| 62 | 2026-05-20 | **Pam's biggest Macola UI pain point: navigation (hard to find things).** Drives top-priority UX investments in Phase 7A: global search PCF control (searches across JEs, bills, invoices, customers, vendors, accounts), breadcrumbs on every page, recent items widget, thoughtful sitemap design grouped by user mental model rather than by Dataverse table structure. Validated as the single highest-leverage UX investment for this project. |
| 63 | 2026-05-21 | **Plain-Text env var for SP client secret (supersedes the Secret-type half of §38 for this specific variable).** Phase 6B validation against PRI-Books-Dev with plugin assemblies 1.0.0.2 and 1.0.0.3 both surfaced `0x80040256 Access Denied` when `DataverseEnvironmentVariables.GetValue` reached the `RetrieveEnvironmentVariableSecretValue` call. 1.0.0.2 had the wrong parameter shape (`environmentVariableDefinitionId` Guid instead of `EnvironmentVariableName` String — caught by Web API `$metadata`). 1.0.0.3 corrected the parameter and still failed with the identical code at 47 ms after entering `WriteToLedgerIfPosted`, proving the failure is not a payload bug. The Dataverse plugin sandbox identity does not hold `prvReadEnvironmentVariableSecretValue` even when impersonating the SYSTEM user via `OrgSvcFactory.CreateOrganizationService(null)` — the privilege gate is enforced at the message dispatcher and the sandbox's effective identity sits outside it. A user calling the same action via the Web API gets the same error (verified live), confirming the gate is identity-based, not payload-based. **Pivot:** `rm_sqlkvclientsecret` is converted to a plain Text env var (type code `100000000`). Key Vault `kv-datastream-books` REMAINS the source of truth for the underlying SP client secret — only the Dataverse-side representation changes. The new `scripts/sync-sp-secret-to-dataverse.ps1` deploy script reads `cicd-sp-client-secret` from KV and writes the value into the Dataverse env var; it is re-run after every SP credential rotation, and is the only sanctioned writer for that field. `DataverseEnvironmentVariables.GetValue` is simplified to a single code path (no Secret-type branch) — a regression test now asserts the function never invokes `Execute`, so reintroducing the Secret path requires deleting that test. **Risk accepted:** the SP client secret is held in plaintext inside `environmentvariablevalue.value` in Dataverse. **Mitigations:** (a) Dataverse audit log is enabled on `environmentvariabledefinition` and `environmentvariablevalue`, so reads/writes are recorded; (b) edit privilege on the variable is restricted to the System Administrator role; (c) the SP credential is itself rotated every 12 months per §36, limiting blast radius; (d) the underlying credential remains in Key Vault — Dataverse holds a working copy, not the original. The other four KV-related env vars (`rm_sqlkvtenantid`, `rm_sqlkvclientid`, `rm_sqlkvurl`, `rm_sqlkvsecretname`) were already plain Text and are unaffected. The broader Phase 6B credential-access pattern from §38 (KV as source of truth, plugin reads SQL connection string from KV at runtime via SP client-credentials flow, cached with TTL) is unchanged — only the *Dataverse-side delivery of the client secret* changes from "Secret-typed KV reference" to "plain Text mirror refreshed by deploy script". |
| 64 | 2026-05-21 | **PowerShell scripts in this repo are ASCII-only, saved as UTF-8 with BOM.** Surfaced during Phase 6B validation when `scripts/sync-sp-secret-to-dataverse.ps1` was first authored with em-dashes and section signs in inline comments. The Write tool saves UTF-8 *without* BOM by default; Windows PowerShell 5.1 reads non-BOM files using the legacy code page (Windows-1252), turning every UTF-8 em-dash byte sequence into mojibake (`â€"`) and refusing to parse the file. Two safeguards together prevent the failure mode: (a) restrict the source to printable ASCII (`em-dash` → `--`, `section` sign written as the word `section`, no smart quotes); (b) save with the UTF-8 BOM so even a PS 5.1 reader picks up the encoding deterministically. AGENTS.md updated with the rule under the PowerShell Scripts convention block. Verified by parsing and `-WhatIf` running the fixed script under `powershell.exe` (PS 5.1). |
| 65 | 2026-05-21 | **Phase 6B end-to-end validated against PRI-Books-Dev on JE-2026-001005.** First real JE posted through the full plugin path. Dataverse side: `rm_status` flipped to Posted (126190003), `rm_postedby_user` = ryanm@plastic-recycling.net, `rm_posteddatetime` = 2026-05-21T18:11:21Z. Azure SQL `ledger.GeneralLedgerEntries` side: two rows landed, one debit and one credit, total $75/$75 matching the JE header. **Hash chain verified live:** EntryId 3 (Cash debit $75, account 10100) has `PreviousRowHash` = 32 zero bytes (the documented genesis sentinel for the entity's first row, per §39). EntryId 4 (AR credit $75, account 11000) has `PreviousRowHash = 0x5E08EF14028496CB3C694C028B53140F9C34C4880B7512A6EADB906289DE344B`, which is byte-for-byte EntryId 3's `RowHash`. The chain links correctly across rows from the very first post. **Atomicity verified:** Dataverse and SQL show consistent posted state with no one-sided commit, confirming the rollback-and-throw pattern (§41) holds in practice. The complete dual-write path designed in Phase 6B — sandbox plugin → Dataverse Update → KV credential read → Azure SQL transaction with `WITH (UPDLOCK, HOLDLOCK)` chain-head lock → per-entity SHA-256 hash chain → commit-or-throw — works end-to-end in the target environment. Phase 6B is closed. Validation result also appended to `architecture/immutability-validation.md`. |
| 66 | 2026-05-21 | **Phase 7A (UI Foundation) runs in parallel with Backend Track -- scope-bounded and provisional.** Supersedes the strict-sequential mandate of §58 *only* for documentation / research items in Phase 7A Sessions S1-S3: credential cleanup (S1), visual identity extraction (S2), sitemap design (S3). **No build work, no app module construction, no PCF authoring, no security role scaffolding, no Dataverse schema changes happen under this decision.** Any work past S3 (S4 app module + theme + logo packaging, S5+ component scaffolds, dashboard, PCFs) requires a **separate, explicit operator confirmation in a new session** -- it is not pre-approved here. Artifacts produced by S1-S3: [`../architecture/ui-styling.md`](../architecture/ui-styling.md) (CSS-variable-based minimal theme; status pill palette; logo asset plan), [`../architecture/ui-sitemap.md`](../architecture/ui-sitemap.md) (accounting-workflow-first sitemap; persona visibility deferred to 7B), and a follow-up note in [`../architecture/immutability-validation.md`](../architecture/immutability-validation.md) confirming the §45-era credential cleanup state is live. **R-7-01 risk profile shifts:** the originally-mandated "7+ month UI invisibility" interval reduces by however much shell-only demo material lands during the parallel-track window, which makes the *new* risk "shell-without-data Pam-side dissatisfaction" rather than absence of UI. **Mitigation:** when Pam first encounters the Books app shell (currently planned for end of S4 in a subsequent session), the framing is explicit -- "shell only, no transactions yet; the pages you see will populate as backend phases land." This framing is mandatory in any demo conversation; it is not optional. **Why provisional:** the parallel-track override was made on-the-fly at Phase 7A kickoff to capitalize on a clean Phase 6B closure state. Committing now to the *full* Phase 7A build runtime would re-litigate §58 without daylight; deferring that decision keeps the option open. If, after S3 lands, the operator chooses not to continue parallel, S1-S3 artifacts remain valid (they describe state that will be needed whenever Phase 7A actually starts). |
| 67 | 2026-05-22 | **§66 reaffirmation gate resolved -- Phase 7A S4-S11 deferred; return to §58 strict sequential.** §66 made Phase 7A S1-S3 explicitly provisional and required a fresh operator confirmation before any work past S3 (S4 app module + theme + logo packaging, S5+ component scaffolds, dashboards, PCFs, security role scaffolding). The reaffirmation gate this morning resolved in favor of Option B from the handoff ([`../handoffs/handoff-2026-05-22-morning.md`](../handoffs/handoff-2026-05-22-morning.md) §G1) -- return to the strict-sequential ordering of §58 for all work past S3. **Rationale:** (a) Backend Track A (vendor / customer integration with ERP) is the current backend phase and is blocked on the [`../memos/executive-questionnaire.md`](../memos/executive-questionnaire.md) §17 vendor master scope question, which is Pam-bound and unanswered; (b) UI scaffolding built before the vendor/customer schema settles risks rework -- the hybrid JE entry grid (§50), the multi-entity dashboard, and the AP/AR bill/invoice screens (§48) all touch vendor or customer lookups that depend on the §17 answer; (c) the R-A-19 form-level read-only mitigation that §66 required before any Pam-facing shell demo remains a precondition for any future demo, so the §66 "shell-only demo" benefit was always gated behind that work even if S4 had landed; (d) the parallel-track context-switching cost between Backend Track A and UI shell work is real and was the original cost §58 sought to avoid. **What stays:** Phase 7A S1-S3 artifacts ([`../architecture/ui-styling.md`](../architecture/ui-styling.md), [`../architecture/ui-sitemap.md`](../architecture/ui-sitemap.md), the [`../architecture/immutability-validation.md`](../architecture/immutability-validation.md) follow-up confirming §45-era credential cleanup state) remain valid -- they describe state that will be needed whenever full Phase 7A actually starts. The 17 Phase 7 UX decisions (§46-§62) remain in force; they are not contingent on §66 or §67. The Phase 7A 11-session work breakdown remains valid as a planning artifact for when the phase resumes. **What does not happen under this decision:** no app module construction, no PCF authoring, no security role scaffolding, no Dataverse schema changes, no theme records, no logo packaging. The Phase 7A.5 candidate (R-A-19 mitigation as a small UI-flavored task that could unlock a Pam demo regardless of S4-S11 status) is captured separately in [`../architecture/form-readonly-enforcement.md`](../architecture/form-readonly-enforcement.md) (drafted same session) and remains independently considerable. **Risk profile reset:** R-7-01 returns toward its original framing -- the parallel-track shortened-invisibility benefit of §66 only materializes through S3, which is a small fraction of total Phase 7 UI surface; the "shell-without-data Pam dissatisfaction" sub-risk that §66 introduced is discharged because no shell beyond S1-S3 documentation will exist. R-7-05 likewise returns to its original framing: backend slippage compounds into 7B+ slip with no §66 parallel-track buffer past S3. The risk register entries for R-7-01 and R-7-05 already encoded the §66 reaffirmation as a conditional, so no separate amendment is required -- the conditional resolves to the "not reaffirmed" branch. **Revisit condition:** Phase 7A S4-S11 will be reconsidered once Backend Track A lands AND [`../memos/executive-questionnaire.md`](../memos/executive-questionnaire.md) §17 has a Pam answer. Revisit is a fresh §-numbered decision, not an automatic resumption. **[Annotation 2026-05-22 per §68]:** in retrospect, the handoff §G1 Option C (continue S4 only, then defer S5-S11) had a stronger case than the morning reaffirmation gate credited. Option C would have landed S4 (app module + theme + logo packaging) -- a concrete artifact Pam could react to in the upcoming conversation -- while still honoring the §17-gated halt on S5+ work. The §68 principle that concrete artifacts produce better accounting-team feedback than design documents argues for Option C's visible-artifact bias over Option B's strict-sequential purity. §67 stands as written and is not reversed by this annotation; the annotation makes the trade-off honest in the record. Future reaffirmation gates should weight "concrete artifact for the accounting team" more heavily than today's call did, and should treat Option C-style hybrid paths as first-class candidates rather than splitting-the-difference compromises. |
| 68 | 2026-05-22 | **Operating principles for Claude Code sessions on this project codified in CLAUDE.md.** Process / engagement-model meta-decision, not architectural or technical. Captures five principles that govern how documentation-heavy and other routine work is sequenced and how concerns are surfaced: **(a) Progress is the default** -- when the operator says "move forward," execute; surface concerns once, briefly, then proceed; do not re-raise procedural concerns across turns. Real concerns (failed verification, ambiguous decision, unexpected file in a diff, scope expansion request) get surfaced; procedural concerns ("should I commit?", "ready to move to item 3?") do not. **(b) Concrete artifacts beat design documents for accounting-team feedback** -- Pam reacts better to something she can point at and complain about than to something she has to imagine. When the choice is between shipping 50% of a feature that is visible and 10% of a feature that is fully decided but invisible, prefer the visible 50%. Bias sequencing toward visible artifacts. **(c) Approve-in-batches for routine work; step-by-step for high-stakes.** Routine = doc updates, design docs, ranking passes, runbook updates, decision-log entries; complete the full pass, then surface a single end-of-session review. High-stakes work uses step-by-step approval with explicit per-step verification; the definition is: plugin C# code, SQL migrations (especially DENY grants, append-only constraints), production deploys to PRI-Books, schema changes touching posted-ledger tables, anything touching the hash chain, anything that could affect JE-2026-001005 or future audit-trail anchors, anything that would reverse or substantially modify a prior decision-log row. **(d) Scope discipline** -- surface adjacent finds in the end-of-session summary; do not silently expand scope; the operator decides whether to address now or add to the backlog. **(e) Operator-driven hours** -- when the operator wants to keep working, support that; do not push for stopping unless something concrete makes stopping the right call (failed verification, ambiguous decision, scope expansion). **Why now:** the morning execution session (commits `deb5e4e` through `0b5ea3a`) ran on per-step approval for every item; the operator concluded the pattern over-weights low-stakes work and slows progress on documentation-heavy passes. Codifying the distinction lets Claude Code recognize routine work and execute it as a batch without procedural friction. **What changes:** CLAUDE.md gains an "Operating Principles" section between "Communication Style" and "When to Stop and Ask"; the new principles supersede the implicit per-step approval pattern for routine work but leave the existing "When to Stop and Ask" guidance intact for high-stakes scenarios -- in fact "When to Stop and Ask" is reframed as the high-stakes subset of these principles. **What does not change:** the high-stakes list (plugin code, SQL migrations, prod deploys, schema, hash chain, JE-2026-001005, audit-trail anchors, prior decisions) continues to require step-by-step approval. AGENTS.md is not touched -- AGENTS.md is the universal default and remains the primary directive per CLAUDE.md "Primary Directive". The annotation appended to §67 records this principle's first retrospective application: the morning's §66 reaffirmation choice was made under the old per-step pattern and would have benefited from the new principle-(b) emphasis on concrete artifacts. |

---

## Development Environment Strategy

### ALM Flow

```
PRI-Books-Dev (sandbox, unmanaged)        ← Active development with Claude Code
        ↓
   Solution exported as managed
        ↓
   GitHub Actions deploys managed solution
        ↓
   PRI-Books (production, managed)        ← Production destination
```

### When PRI-Books-Test Sandbox Is Added

Before finance team UAT begins. Three-environment ALM:

```
PRI-Books-Dev → PRI-Books-Test → PRI-Books
```

### Managed Environment Implications

PRI-Books is managed:
- Solution Checker mandatory before import
- Deployment from solutions only (no direct unmanaged customization)
- Pipelines feature enforced
- Stricter maker controls

PRI-Books-Dev as unmanaged source is the correct ALM pattern.

---

## Finance System Owner — Pam

### Critical Framing: Pam OWNS This System

Mirroring the Datastream ERP departmental ownership pattern:

| App / Area | Owner |
|---|---|
| Datastream ERP — Operations | [Ops lead] |
| Datastream ERP — Transportation | [Transportation lead] |
| Datastream ERP — [Other] | [Other leads] |
| **Datastream Books — Finance** | **Pam** |

**Pam is the business owner of the finance system. IT builds the system for her. She is responsible for whether it works.**

This is not a consultative role. This is ownership.

### Why This Framing Is Critical

The historical pattern of "complain, then escalate" exists because the person feels powerless and external to the system. **Ownership removes powerlessness.**

Without ownership: "IT didn't listen to us" (victim framing, escalation incentive)
With ownership: "I haven't worked through that workflow yet" (owner framing, problem-solving incentive)

When Pam escalates to the President (Executive Sponsor), the President must reinforce ownership: "Pam, you own this. What do you need from IT to make it work?" — not "IT, why isn't this working for Pam?"

This conversation must happen with the President during the rollout meeting.

### Ownership Artifacts (Pam's Name On Them)

| Deliverable | Pam's Role |
|---|---|
| Chart of Accounts | Approves and signs off |
| Approval threshold definition | Sets the dollar amounts |
| Standard report layouts | Reviews and approves each |
| Period close runbook | Authors (with IT support) |
| User Acceptance Testing | Owns test forms and signoff |
| User documentation | Reviews and approves |
| Training rollout | Leads |
| Change Request triage (finance-side) | Triages and prioritizes |
| Vendor master cleanup approach | Reviews and signs off |

### Ownership Language

- "Pam's accounting system" — not "the new accounting system"
- "What does Pam need it to do?" — not "what do we think they need?"
- In meetings: "Pam, when do you want to deploy this?" — not "we plan to deploy this when..."

### Visible Ownership

- Pam's name in repo README as Finance System Owner
- Pam's name on the executive memo as project co-lead
- Pam's email in the user-facing app footer as the "Report an Issue" contact
- Pam mentioned by name in leadership status updates as the owner

### Engagement Structure

**Weekly 30-minute check-in:**
- Same time every week
- Agenda sent day before
- Outcomes captured in repo or CR system
- No "while I'm thinking about it" requests — redirect to CR system

**All complaints/feedback route through Change Request system:**
- Not email, not conversation, not escalation
- Multi-image attachment support required (see ChangeRequest design)
- IT commits to triage within 48 hours

**Pam triages and prioritizes** finance-side CRs herself. She closes them out when satisfied. This is part of ownership.

### Test Acceptance Form Structure

For each feature ready for finance review:

1. Feature description (provided by IT)
2. What I tested (specific scenarios)
3. What worked
4. What didn't work (specific, reproducible)
5. Severity (Blocker / Major / Minor / Cosmetic)
6. Recommendation (Accept / Accept with conditions / Reject)
7. Date and signature

### Rollout Meeting: President and COO

Before kickoff, IT meets with the President (Executive Sponsor) and COO to:

1. Confirm Datastream Books project direction (per president memo)
2. **Confirm Pam as Finance System Owner**, mirroring ERP ownership pattern
3. Confirm the President will introduce the ownership role to Pam directly
4. Confirm leadership response to escalations: reinforce ownership, not rescue
5. Review executive questionnaire and assignment of answers
6. Confirm budget envelope and timeline expectations

The President introducing ownership to Pam is critical — not IT. Endorsement from above lands differently than a tap from IT.

### Failure Mode to Watch

If Pam cannot or will not accept ownership — insisting on critic role rather than owner — this is a serious early signal. Surface to the President (Executive Sponsor) at month 1, not month 6.

### Escalation Protocol

If, after 2 months of disciplined engagement, the escalation pattern persists:

1. Document specific instances (dates, what was bypassed, where it should have gone)
2. Raise with the President (Executive Sponsor) as a process risk, not a personality complaint
3. Project should not bear the cost of an unworkable dynamic
4. Becomes an HR/management issue, not a project issue

---

## Immutability Architecture

### A. Append-Only Transaction Ledger
- Dedicated Azure SQL table: `GeneralLedgerEntries`
- `DENY UPDATE, DELETE` at SQL role level for all accounts including app service principal
- Corrections via reversing entries only

### B. Hash-Chained Records
- Every ledger row: `RowHash` = SHA-256 of (row contents + `PreviousRowHash`)
- Periodic snapshots in `LedgerIntegrityCheckpoints`
- Nightly verification job with alerting

### C. Server-Side Posting Enforcement
- All journal posting through Dataverse plugin
- Validates: debits = credits, period open, account active, post role, SoD
- Dual write to Dataverse + Azure SQL in single transaction

### D. Period Locks at Data Layer
- `FiscalPeriod` table with Status (Open / Closed / Locked)
- Plugin rejects postings to Closed or Locked periods
- Period close writes hash to `PeriodCloseAttestation`
- Reopen requires elevated role + audit event
- Locked periods cannot be reopened

### E. Segregation of Duties
- Roles: `JE Entry`, `JE Approve`, `JE Post`, `JE Void`, `Period Close`, `Period Reopen`, `Vendor Setup`, `Vendor Bank Change`, `Wire Initiate`, `Wire Approve`
- Plugin enforces `CreatedBy != ApprovedBy` for sensitive operations
- System-enforced, not honor system
- SoD matrix version-controlled in repo

### F. Comprehensive Audit Trail
- Dataverse audit log on all financial tables, long-term retention
- Redundant `AuditEvents` table in Azure SQL — append-only

### G. Time-Bound, Signed Reports
- Closed-period reports hashed and stored at close
- `ReportSnapshots` preserves figures as-of close

### H. Dev/Prod Separation
- Developers have zero direct production database access
- All production changes via approved deployment pipeline
- Deployments approved, logged, version-tagged

### I. Backup and Recovery
- Dataverse: native Microsoft backups + solution exports in git
- Azure SQL: PITR + long-term retention (7+ years)
- Recovery procedures documented and tested annually

### J. Change Management Built In
- `ChangeRequest` table within the app
- Required: business reason, desired outcome, acceptance criteria, risk assessment, rollback plan
- **Multi-image attachment support** via `ChangeRequestAttachment` related table
- **Timeline / Notes** enabled for ad-hoc files
- Plugin enforces SoD: `ApprovedBy != RequestedBy != AssignedTo`
- Permanent record

### K. AI's Role
- Generates plugin code for posting enforcement and hash chaining
- Generates test cases proving each control works
- Generates audit reports and reconciliation queries
- Reviews changes for SoD violations

---

## ChangeRequest Design — Specific Requirements

### Core Table

`ChangeRequest` with standard workflow fields (per prior spec).

### Attachment Strategy

**Hybrid approach:**

1. **`ChangeRequestAttachment` related table** (primary mechanism)
   - `Image` column (Dataverse native type)
   - `Description` column (text)
   - `CapturedDate` (auto-populated)
   - Unlimited attachments per CR
   - Use case: screenshots showing what's wrong, "before" / "after" comparisons, error messages

2. **Timeline / Notes** (catch-all)
   - Enabled on ChangeRequest table
   - For ad-hoc files: auditor letters, vendor emails forwarded, PDFs, etc.

### User Experience Requirements

- Drag-drop image attachment in form
- Paste-from-clipboard support (screenshot → paste directly)
- Mobile capture: take photo with phone camera
- Multiple images per CR
- Thumbnail previews in form
- Original-size view on click
- File size limit: ~30MB per attachment (Dataverse default); larger files go to SharePoint

### Why This Matters

"It's broken" with no evidence is unactionable.
"It's broken — here's a screenshot of the actual issue" is a real problem to solve.

Forcing concrete evidence shifts complaints into problem statements.

---

## Scope

### In Scope (v1 MVP)
- General Ledger (COA, journal entries, recurring journals, period close)
- Multi-entity ledger with inter-company transaction support
- Accounts Payable (vendors, bills, payments via NACHA generation)
- Accounts Receivable (customers, invoices, receipts, aging)
- Bank reconciliation (manual statement import)
- Fixed Assets (acquisition, depreciation, disposal)
- Financial Reporting (native: TB, BS, P&L, Cash Flow, agings)
- 1099 generation via Track1099 integration
- W-9 collection via Track1099
- Audit log / immutable transaction history
- Document attachments via SharePoint
- Change Management workflow (built into app, with image attachments)
- Approval workflows
- Integration with Datastream ERP (shared customer/vendor masters)
- Email generation via Microsoft Graph API

### Phase 2+
- AI-driven document extraction and discrepancy detection (Claude API)
- Power BI reporting
- Bill.com / Ramp / Bank API for AP payment execution
- Credit limit management and enforcement
- Customer credit risk scoring
- Mobile-optimized UI
- Limble PO replacement
- Sales tax engine (if needed)

### Out of Scope (v1)
- Budgeting & forecasting, Inventory, Project accounting, Multi-currency, Sales tax engine, Mobile data entry, Foreign currency

### Out of Scope (Ever)
- Payroll — Paylocity, permanent

---

## Architecture Decisions

### Data Storage Strategy

| Data Type | Store | Rationale |
|---|---|---|
| Chart of Accounts | Dataverse | Master data |
| Vendors / Customers | Dataverse (synced with ERP) | Shared master data |
| Entities / Companies | Dataverse | Multi-entity master |
| Journal Entry headers | Dataverse | Workflow, approval |
| Journal Entry lines (working) | Dataverse | Pre-post editing |
| Posted ledger entries | Azure SQL (append-only) | Immutability + reporting |
| Period close attestations | Azure SQL | Cryptographic hash |
| Ledger integrity checkpoints | Azure SQL | Hash-chain verification |
| Audit events | Azure SQL (append-only) | Redundant audit |
| Historical Macola data | Azure SQL | Read-only archive |
| Reporting data mart | Azure SQL | Fast aggregations |
| Report snapshots | Azure SQL | Time-bound signed reports |
| Change Requests | Dataverse | App-internal workflow |
| Change Request attachments | Dataverse (Image columns) + SharePoint (large files) | Native attachment support |
| Documents | SharePoint | Native |
| Approval requests | Dataverse | Workflow integration |

### Application Layer

- **UI:** Model-driven app + custom pages (React/Fluent UI v9) + PCF controls
- **Business logic:** Dataverse plugins (C#) — strong preference over Power Automate
- **Workflows:** Power Automate only for simple notifications and scheduled jobs outside transaction context
- **Email:** Microsoft Graph API
- **Documents:** SharePoint
- **Document AI (Phase 2):** Claude API
- **ACH:** NACHA file generation in v1

### Multi-Entity Architecture

- `Entity` master table: name, EIN, fiscal year, base currency, type
- Every transactional table includes `EntityId`
- Shared COA structure with entity-specific account activation
- Inter-company JE pairs auto-generated by plugin
- Inter-company elimination accounts on each entity's COA
- Consolidation process nets inter-company balances
- Every report includes Entity selector

---

## Approval Workflows (v1)

| Approval Type | Trigger | Approver(s) |
|---|---|---|
| Bills | Amount > $X (TBD by Pam) | Direct supervisor / designated approver |
| Journal entries | Amount > $Y (TBD by Pam) | Second reviewer (≠ creator) |
| Wire transfers | All wires | Dual approval (≠ each other) |
| New vendor setup | All new vendors | AP Manager + Controller |
| Vendor bank info changes | All changes | AP Manager + Controller + out-of-band verification |
| Period reopening | All reopens | Controller + designated executive |
| Manual JE to bank accounts | All such JEs | Controller |
| Write-offs | Amount > $Z (TBD by Pam) | Controller |
| Recurring JE setup/modification | All | Controller |

Generic `ApprovalRequest` + `ApprovalPolicy` configuration tables. Thresholds adjustable by admin.

---

## DevOps / ALM Architecture

### CI/CD Pipeline

```
Claude Code edits files in PRI-Books-Dev
         ↓
   git push to GitHub
         ↓
   GitHub Actions triggers
         ↓
   pac CLI exports solution as managed
         ↓
   Deploy managed solution to PRI-Books
```

### Required Local Installs

- Power Platform CLI: INSTALLED — v1.51.1
- All four auth profiles established and active

---

## Document AI Strategy

### Long-Term Goal
- Claude API for document extraction
- Document type detection, structured data extraction
- Two-way write: SharePoint columns + Dataverse records
- Discrepancy detection layer
- Outcome: headcount reallocation from manual validation to higher-value work

### V1 Pattern
- SharePoint stores documents (current AI Builder pattern)
- Manual entry into Datastream Books
- Sufficient for cutover

### Phase 2
- Claude API integration evaluated and built

---

## Reporting Strategy

### V1 — Native Model-Driven App

Reports: Trial Balance (per-entity + consolidated), GL Detail by Account, Balance Sheet, Income Statement, Cash Flow Statement, AR Aging, AP Aging, Cash Position, Fixed Asset Register & Depreciation, Bank Reconciliation Summary, Journal Entry Audit Trail, Vendor 1099 Summary, Change Request Log.

### Phase 2 — Power BI

Paginated reports for formal financial statements, dashboards, cross-entity analytics, comparative period analysis.

---

## Pre-Migration Projects (Parallel With Build)

1. **President + COO + IT rollout meeting:** Confirm ownership framing, Pam as System Owner, escalation handling
2. **President (Executive Sponsor) introduces ownership role to Pam directly** (not IT)
3. **COA Pre-Population:** IT proposes draft; Pam reviews and signs off
4. **Vendor Master Strategy:** "Add as needed" — Pam reviews approach
5. **Entity Documentation:** Pam coordinates with executive team
6. **Approval Threshold Definition:** Pam sets dollar amounts
7. **Macola Archive Plan:** Retention duration and access pattern
8. **Pam engagement cadence:** Weekly check-ins begin

---

## Open Questions

See `executive-questionnaire.md`.

---

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Auditor rejects custom system | Medium | Document immutability architecture; proactive disclosure at next audit |
| Build timeline slips | Medium | Hard MVP scope, no scope creep, fixed cutover date |
| Dataverse capacity costs higher than expected | Medium | Monitor capacity during build |
| Key-person dependency | Medium | Documentation standards, AI-assisted handoff |
| Macola data quality during migration | High | Extract early, profile data |
| Period close logic bugs | High | Heavy test coverage, parallel run |
| AI-generated code introduces subtle bugs | Medium | Mandatory human review of financial logic |
| Hash-chain verification missed corruption | Low | Nightly verification + alerting |
| SoD bypassed by privileged user | Medium | Enforce in plugin code, quarterly role audit |
| Cutover failure | High | Parallel run mandatory; penny-perfect reconciliation |
| Document AI accuracy insufficient for headcount story | Medium | Phase 2 only; pilot before committing |
| Leahy ACH service unavailable post-Macola | High | NACHA file generation built in v1 |
| Change management not actually used | Medium | Built into workflow such that changes can't happen without it |
| **Pam refuses ownership role** | **High** | **President (Executive Sponsor) introduces role, reinforces in escalations. Surface at month 1 if Pam resists. Not a project issue if persistent — becomes HR.** |
| **Leadership rescues Pam from ownership during escalations** | **High** | **President + COO rollout meeting sets escalation handling protocol upfront** |
| Managed env constraints surprise developer | Mitigated | PRI-Books-Dev sandbox established as proper source |

---

## AI Usage Guidelines

- AI designs the immutability architecture
- AI generates code for the controls and test cases proving them
- AI assists with: schema design, plugin code, test cases, audit narratives, anomaly detection, document extraction (Phase 2)
- AI does NOT make the system immutable — architecture does
- All financial posting logic requires human review before merge
- AI-generated SQL/plugin code requires test coverage before production

---

## Cost Comparison

Assumptions: 15 users, 5-year horizon, existing M365 base licensing.

### Business Central (Reference)
- Licensing: ~$90K (5-yr)
- Implementation: $50K–$150K
- Internal effort: $30K–$50K
- AP automation add-on: $30K–$100K
- **5-year total: $200K–$390K**

### Datastream Books
- Power Apps per-app: managed by IT
- Dataverse capacity: $5K–$10K (5-yr)
- Azure SQL: $3K–$6K (5-yr)
- Build effort: $80K–$150K
- Ongoing maintenance: $30K–$60K
- Track1099: ~$5K (5-yr)
- Claude API (Phase 2): $8K–$20K (5-yr)
- **5-year total: $130K–$255K**

### Strategic Benefits Beyond Cost
- Headcount reallocation via AI discrepancy detection (Phase 2)
- IT modernization (Lighthouse)
- Unified ERP + Finance data model

---

## Timeline (Working Estimate)

> Note: This high-level grouping (Phase 0-5) is the original strategy-level
> estimate. The live phase-by-phase plan is in
> [`../roadmap.md`](../roadmap.md), where backend work is tracked as
> detailed Phases 1-11+ and the front-end build as Phase 7 (UI Track) with
> sub-phases 7A-7F.

| Phase | Duration | Notes |
|---|---|---|
| Phase 0: Strategy + decisions | Complete | Files generated, environments ready, rollout meeting scheduled |
| Phase 1: Design sprint + repo setup + CI/CD + Pam onboarding | 4–6 weeks | Data model, security, controls, ALM pipeline |
| Phase 2: MVP backend build (GL + AP + AR + Multi-entity) | 14–18 weeks | Core posting, immutability, multi-entity, reporting backend, change mgmt |
| Phase 3: Extended backend (FA, bank rec, NACHA, approvals, ERP integration) | 8–10 weeks | |
| Phase 4: Data migration + parallel run | 8–12 weeks | Minimum 1 full close cycle parallel |
| Phase 5: Cutover + hypercare | 4 weeks | At fiscal period boundary |
| **Phase 7 (UI Track): UI/UX front-end build + UAT** | **16–20 weeks** | **Strict sequential — starts after all backend complete (decision §58). Six sub-phases 7A-7F. See [`phase-7-ui-design.md`](phase-7-ui-design.md).** |
| **Total** | **14–18 months** | Multi-entity adds ~4 weeks vs. single-entity; Phase 7 (UI Track) adds the 16–20 weeks of front-end build + UAT on top of the original 10–14 month backend estimate. |

---

## Naming / Glossary

- **Datastream ERP** — existing operations model-driven app
- **Datastream Books** — this project
- **Macola** — legacy on-prem accounting being replaced
- **Leahy** — ACH service tied to Macola
- **Lighthouse** — internal IT strategic plan
- **PRI-Books** — Dataverse production environment (managed)
- **PRI-Books-Dev** — Dataverse dev sandbox (unmanaged)
- **Pam** — Finance System Owner
- **President** — Executive Sponsor
- **MVP** — General Ledger, AP, AR, basic reporting, multi-entity
- **SoD** — Segregation of Duties
- **CR** — Change Request
- **ALM** — Application Lifecycle Management
- **pac** — Power Platform CLI
- **PCF** — Power Apps Component Framework
- **COA** — Chart of Accounts
- **NACHA** — National Automated Clearing House Association
- **PITR** — Point-in-Time Restore
- **SME** — Subject Matter Expert (note: Pam is OWNER, not SME)

---

## Change Log

| Date | Change |
|---|---|
| 2026-05-19 | Document created |
| 2026-05-19 | Immutability architecture, CI/CD, cost numbers, DevOps section |
| 2026-05-19 | 20 additional considerations, per-app licensing, Claude.ai Project confirmed |
| 2026-05-19 | Document AI strategy, Track1099, Graph API, NACHA in v1, multi-entity confirmed |
| 2026-05-19 | PRI-Books env deployed and authenticated |
| 2026-05-19 | PRI-Books-Dev sandbox added; finance SME engagement protocol established |
| 2026-05-19 | **PRI-Books-Dev authenticated. Pam reframed from SME to System OWNER (mirrors ERP pattern). ChangeRequest design includes multi-image attachment support. Executive rollout meeting scheduled with President and COO. President confirmed as executive sponsor. Strategy phase complete.** |
| 2026-05-19 | **Phase 1 begun. Repo skeleton built (.gitkeep placeholders for all empty folders per repo-structure.md). Dataverse solution initialized with `Ryan McCauley` / `rm` publisher (verified against PRI-Datastream ERP via solution export from pri-dev; option-value prefix `12619` matched to ERP for cross-solution consistency). Plugin projects scaffolded (`DatastreamBooks.Plugins` net462 + `DatastreamBooks.Plugins.Tests` net48 with xUnit + FluentAssertions + FakeXrmEasy). First SQL migrations drafted: V0001 (initial schema placeholder), V0002 (`ledger.GeneralLedgerEntries` with DENY UPDATE/DELETE, hash-chain columns, indexes, and full rollback notes). First GitHub Actions workflow `deploy-dev.yml` stubbed with required secrets documented. Foundational PowerShell scripts created: setup-dev, auth-env, pull-solution, push-solution, run-sql-migration. Architecture docs authored: data-model, security-model, immutability-design. Controls docs authored: sod-matrix, approval-policies, audit-controls. All remaining CFO references in this decision log corrected to President (Executive Sponsor) following the recent sponsor change. New principle added to AGENTS.md: "Verification is mandatory, not optional" (codified after the publisher prefix had to be reverted from a guessed value to the verified ERP-matching value).** |
| 2026-05-19 | **Phase 1 closed; housekeeping pass. Branching Policy formalized in AGENTS.md (solo-dev, work directly on main, no branches, no worktrees) after merge drama between local commits and the user's direct PC edits. ERP metadata reference snapshot imported to `docs/reference/erp-metadata/` (tables.csv, columns.csv, relationships.csv exported 2026-04-24 + dataverse_metadata_rm.sqlite from 2026-01-16) with a REFERENCE-ONLY README — these files describe PRI-Datastream, not Books. New architecture note `docs/architecture/erp-pattern-notes.md` captures six patterns Books will follow (4-column master-data shape, picklist+virtual companion, `rm_customer` as shared customer master that Books AR should reference rather than duplicate, lowercase singular table naming, table-prefixed column naming, many-small-tables instinct — ERP has ~47 custom tables with median 10 columns). AGENTS.md gained a Reference Data section pointing at this folder and stating the read-only rules. First session log file authored at `docs/session-logs/2026-05-19-phase-1-foundation.md`. README Project Status updated: Phase 1 complete, Phase 2 next focus = Azure SQL provisioning + first real Dataverse tables.** |
| 2026-05-19 | **Phase 2 complete: Azure SQL provisioning + CI/CD foundation. docs/session-logs/ replaced by living docs/roadmap.md (single source of truth for state and direction); AGENTS.md gained Pull-Before-Work and Roadmap-Maintenance mandatory principles. SQL migrations V0001-V0003 applied to DatastreamBooks-Dev: ledger/audit/reports/archive schemas, ledger.GeneralLedgerEntries (append-only, hash-chained, 30 cols, 5 indexes, universal DENY UPDATE/DELETE/REFERENCES/ALTER on public), and four contained users (dsb_app, dsb_migrate, dsb_reader, dsb_admin) with least-privilege grants and per-user DENY. V0003 password handling: committed source has zero plaintext — passwords are sqlcmd-style $(pw_dsb_*) tokens, substituted in-memory with cryptographically random 32-char values at apply time, never logged or persisted. Immutability validated live: dsb_admin (db_owner) and dsb_app both blocked from UPDATE/DELETE/TRUNCATE with exact error messages captured in docs/architecture/immutability-validation.md. Critical finding documented: priadmin (SQL admin -> dbo) bypasses DENY by documented SQL Server design — protection is organizational + audit-based, not permission-based (auditing-enabled check + AAD-only-priadmin migration filed as Phase 3 backlog). CI/CD wired: Entra app datastream-books-cicd (appId a58747ee...) with service principal granted System Administrator on PRI-Books-Dev via pac admin assign-user; federated credential github-main bound to repo:.../datastream-books:ref:refs/heads/main; deploy-dev.yml rewritten for OIDC via azure/login@v2 with no client-secret references anywhere. New runbooks: cicd-setup.md (reproducible) and sql-account-management.md (rotation procedure + priadmin governance). Four GitHub Secrets added to repo via web UI (gh CLI not installed on dev machine). First workflow run pending user verification — pipeline expected to authenticate cleanly and exit gracefully because solution/src/Entities is still empty.** |
| 2026-05-20 | **Key Vault provisioning + credential rotation session.** Provisioned `kv-datastream-books` in the shared Datastream RG (East US, standard tier, Azure RBAC, soft-delete 90 days, purge protection on, public network with firewall `Deny`-default + ryanm-IP allow, `bypass=None`). Diagnostic logging on day one (`AuditEvent` + `AllMetrics` → existing Log Analytics workspace `…-Datastream-EUS`). RBAC at Vault scope: ryanm = Key Vault Administrator, `datastream-books-cicd` SP = Key Vault Secrets User. Rotated `dsb_app` SQL password (32 chars, alphanumeric + safe symbols, never logged) and stored full ADO.NET connection string as `dsb-app-connection-string`; rotation verified live with positive tests (SELECT @@VERSION, SELECT TOP 1, INSERT-in-tran-then-ROLLBACK) and negative tests (UPDATE/DELETE both throw SQL 229 permission-denied — the DENY architecture survives the rotation). Generated fresh 12-month client secret for the cicd SP and stored as `cicd-sp-client-secret` (federated identity remains the primary auth path; client secret is break-glass fallback). One mid-session glitch: first attempt to capture the credential JSON failed parsing on a CLI warning line — the unrecoverable credential was identified and deleted, replaced cleanly with the current `efb08aa2-…` keyId. New runbook `docs/runbooks/key-vault-management.md` authored as the single source of truth (RBAC, secret inventory, rotation, break-glass); cross-references added in `sql-account-management.md`, `security-model.md`, and `cicd-setup.md`. **Surfaced finding (open, not yet acted on):** an unrecognized credential named "secret" (keyId `3eaf8a5a-1cf8-4d5c-89e5-e322567abf4a`, created 2026-05-20 15:46:33Z — about 8 minutes before this session began) is present on the `datastream-books-cicd` app reg with origin unknown; left intact pending investigation. Phase 6B is now unblocked — the `PostJournalEntryPlugin` runtime can authenticate to Azure SQL as `dsb_app` (not `priadmin`), preserving the immutability architecture. |
| 2026-05-19 | **Phase 5 complete: Journal Entry tables.** `rm_journalentry` and `rm_journalentryline` both built in PRI-Books-Dev. Header has 12 user-defined attributes and 7 lookups (required `rm_entity`, `rm_fiscalperiod`; three SoD `systemuser` lookups `rm_createdby_user` / `rm_approvedby_user` / `rm_postedby_user`; two self-referencing reversal lookups `rm_reversesje` / `rm_reversedbyje`). Lines have 7 user-defined attributes and 3 lookups (parent `rm_journalentry` with Cascade All; `rm_account` → `rm_chartofaccount`; `rm_entity` denormalized from header). Local picklists `rm_status` and `rm_jetype` on the header; lines kept lean. No plugins yet — `PostJournalEntryPlugin` (Phase 6) owns auto-numbering, totals, SoD enforcement, period-lock check, and dual-write Dataverse + Azure SQL with hash-chain. Five design decisions documented in [`data-model.md`](../architecture/data-model.md) §`rm_journalentry` / §`rm_journalentryline`. Decision row §35 logged. New build-helper lesson: `0x80040216 An unexpected error occurred` on the first attribute POST right after `CreateEntity` is a metadata-cache lag — folded into the existing poll-on-lock retry loop alongside `0x80071151` (concurrent solution import) and 429 throttles. |
| 2026-05-19 | **Phase 4 complete: Chart of Accounts. `rm_chartofaccount` built in PRI-Books-Dev with 14 user-defined columns: primary `rm_chartofaccountname` (200), `rm_accountnumber` (50, app-required, unique within entity), `rm_accountshort`, `rm_accountdesc`, `rm_accountlevel`, `rm_displayorder`, `rm_isactive` (default Yes), `rm_iscashbankaccount` (default No), `rm_normalbalance` (optional Debit/Credit override of the linked account type's normal balance), `rm_currency` (default 'USD' set by seed scripts at insert time — Dataverse Web API does not expose a metadata-level default for StringAttributeMetadata, a fact added to the helper script for future authors), `rm_externalsystemid` (for migration), plus four lookups (`rm_accounttype` required, `rm_accountcategory`, `rm_entity` required, `rm_parentaccount` self-referencing). Alternate key `rm_coa_entity_number_key` on (`rm_entity`, `rm_accountnumber`) verified live to reject duplicate (entity, account number) inserts. Per-entity COA chosen over globally-shared COA: matches Macola behavior, simpler than the originally-drafted `entityactivation` intersect table; the data-model.md draft was updated to reflect the as-built shape including the deliberate divergences from the Phase-1 draft. Placeholder `Default Operating Entity` (rm_entitycode=DEFAULT, type=Operating, fiscal year-end 12/31) seeded into rm_entity. Standard 54-row Chart of Accounts (20 parent + 34 child rows, 1xxxx Assets, 2xxxx Liabilities, 3xxxx Equity, 4xxxx Revenue, 5xxxx-7xxxx Expenses) loaded under DEFAULT entity via two new committed idempotent seed scripts (`scripts/seed-default-entity.ps1`, `scripts/seed-rm_chartofaccount.ps1`). Both scripts resolve FKs by stable code (rm_entitycode, rm_accounttypecode) not GUIDs, so they are portable across environments — finance will run them against PRI-Books-Test once that env exists. Contra-accounts (Allowance for Doubtful Accounts, Accumulated Depreciation, Sales Returns & Allowances, Owner Distributions) carry an explicit `rm_normalbalance` override; non-contras leave it null to inherit by convention. The optional `rm_accountnumberingscheme` table was deliberately skipped — would be soft-validation documentation data with no immediate consumer; introduce when the COA validation plugin lands. Build process surfaced one new lesson learned: concurrent solution-import / publish operations from the CI workflow lock the metadata layer with a 0x80071151 / "another [Import] running" error; the mitigation is to wrap idempotent metadata builds in a poll-on-429 retry loop until the lock clears, rather than ordering work to avoid the lock entirely (which is fragile across multiple Claude Code sessions and manual edits).** |
| 2026-05-20 | **Phase 7 (UI Track) planning session — documentation only, no code.** Captured 17 UX decisions (§46-§62) for the front-end build phase: Dataverse model-driven + custom React hybrid architecture, 5 finance personas, all 8 v1 screens included, Datastream ERP visual identity match, hybrid Excel-grid + form-mode JE entry, desktop only (no mobile/tablet v1), shared role-aware dashboard homepage, in-app notifications only (email + Teams deferred to Phase 8), all three report formats equally weighted (on-screen + Excel + PDF), universal report drill-down to underlying transactions (architectural implication: reports are live queries retaining row-level provenance, not pre-aggregated snapshots), explicit Save with explicit draft state (no auto-save), no initial Pam design sign-off (CR-based ownership instead — consistent with Owner framing), strict sequential timing (UI dormant until backend Phase 11/12+ completes; Pam first sees UI ~7 months from project start; risks accepted explicitly), Datastream ERP + competitor finance UIs as reference material, Fluent UI v9 defaults with minimal custom layer (no upfront design system documentation), 7 finance-specific security roles distinct from ERP role structure, and Pam's biggest Macola pain point identified as navigation (drives Phase 7A investments in global search PCF, breadcrumbs, recent items widget, sitemap design). Created [`phase-7-ui-design.md`](phase-7-ui-design.md) capturing the full Phase 7 plan including six sub-phases 7A-7F (Foundation 3wk, Core Transactional 4-5wk, Reports 3-4wk, Specialty 2-3wk, CR Burn-down 2-3wk, UAT 2-3wk — 16-20 weeks total + UAT). Created [`../risk-register.md`](../risk-register.md) as the live risk register, with 5 Phase 7-specific risks (R-7-01 through R-7-05) plus the pre-existing risks carried over from this decision log. Created [`../runbooks/phase-7a-foundation-prompt.md`](../runbooks/phase-7a-foundation-prompt.md) — a DRAFT Claude Code prompt for the first Phase 7 (UI Track) kickoff session, marked DRAFT until backend Phase 11/12+ completes. Updated [`../architecture/security-model.md`](../architecture/security-model.md) with the 7 finance-specific persona-level roles (Controller, AP Clerk, AR Clerk, Approver, Casual Contributor, System Admin, Read-Only Auditor) distinguished from the 10 enforcement-level SoD roles already documented. Updated [`../roadmap.md`](../roadmap.md) to insert Phase 7 (UI Track) at the end of Future Phases with explicit dual-track numbering note (backend "Phase 7" and UI "Phase 7" coexist with track qualifiers); added Phase 11/12+ placeholder; tagged Phase 8 with the email + Teams notification deferral per decision §53. No code, no Dataverse changes, no Azure changes — pure documentation session. Phase 6B validation continues in a separate paused session and is unaffected.** |
| 2026-05-21 | **Phase 6B validation pivot — `rm_sqlkvclientsecret` flipped from Secret to plain Text env var (decision §63).** Plugin assemblies 1.0.0.2 and 1.0.0.3 both hit `0x80040256 Access Denied` from `RetrieveEnvironmentVariableSecretValue` against PRI-Books-Dev. 1.0.0.2 had a wrong payload parameter name (`environmentVariableDefinitionId` Guid instead of `EnvironmentVariableName` String — pinned by Web API `$metadata`). 1.0.0.3 corrected the parameter and reproduced the same error at 47 ms after entering `WriteToLedgerIfPosted`, proving the sandbox identity lacks `prvReadEnvironmentVariableSecretValue` regardless of `CreateOrganizationService(null)` impersonation. The same action via Web API as a System Administrator user also returned 0x80040256 for both real and bogus env var names, confirming the gate is identity-based at the message dispatcher. **Changes landed:** (a) `plugins/.../KeyVault/DataverseEnvironmentVariables.cs` simplified to one code path — Secret-type branch removed entirely, ColumnSet trimmed to (schemaname, defaultvalue), no `Execute` call anywhere; (b) `solution/src/environmentvariabledefinitions/rm_sqlkvclientsecret/environmentvariabledefinition.xml` `<type>` flipped from `100000005` → `100000000`; (c) regression test added asserting `GetValue` never invokes `Execute` and never emits `RetrieveEnvironmentVariableSecretValue` — reintroducing the Secret-type branch requires deleting this test; (d) `scripts/sync-sp-secret-to-dataverse.ps1` drafted as the only sanctioned writer of the env var value, pulling `cicd-sp-client-secret` from `kv-datastream-books` after each SP credential rotation; (e) plugin assembly bumped to 1.0.0.4. All 53 plugin tests pass. Key Vault remains the source of truth for the SP client secret — only the Dataverse-side delivery mechanism changes. Manual maker-portal steps required before redeploy: re-create `rm_sqlkvclientsecret` env var as Text type with the actual SP client secret VALUE (the type field is not editable in place; the env var definition must be deleted and recreated). |
| 2026-05-21 | **Phase 6B CLOSED — first real JE posted end-to-end against PRI-Books-Dev.** JE-2026-001005 (Cash debit $75 / AR credit $75, single entity) flowed through the complete Phase 6B path: Dataverse `Approved` → plugin Stage 40 PostOperation → KV credential resolution (plain-Text env var per §63) → Azure SQL transaction with per-entity `WITH (UPDLOCK, HOLDLOCK)` chain-head lock → SHA-256 hash chain on `ledger.GeneralLedgerEntries` → commit on both stores. Verified live: Dataverse status flipped to Posted (126190003), `rm_postedby_user` stamped, `rm_posteddatetime` = 2026-05-21T18:11:21Z; SQL EntryId 3 holds the entity's genesis-prefixed hash, EntryId 4's `PreviousRowHash` is byte-for-byte EntryId 3's `RowHash`. Hash chain works as designed (§39, §40). No one-sided commit. Decision §65 captures the validation result with the exact hash byte sequence. **Other closure work landed in this commit:** decision §64 codifies the ASCII-only + UTF-8-BOM rule for repo PowerShell scripts (surfaced when `scripts/sync-sp-secret-to-dataverse.ps1` first contained em-dashes and section signs that Windows PowerShell 5.1 mojibaked); the rule is mirrored into AGENTS.md → Code Conventions → PowerShell Scripts. `docs/architecture/immutability-validation.md` updated with the live first-real-JE validation result. `docs/roadmap.md` updated: Phase 6B moved from Current Phase into Completed Phases; Phase 7 (Vendor / Customer integration with ERP) becomes Current Phase. Plugin assembly 1.0.0.4 stays as the production-ready artifact for Phase 6B; subsequent phases will bump from there. Phase 6B was the architectural keystone of the immutability story — closing it means the audit-defensibility promise has a working implementation, not just a design doc. |
| 2026-05-22 | **§66 reaffirmation gate resolved (decision §67) -- return to §58 strict sequential.** Morning execution session opened with the §66 reaffirmation gate framed in last night's handoff (§G1 -- three options). Operator selected Option B (strict sequential per §58); Phase 7A S4-S11 deferred until Backend Track A (vendor/customer integration with ERP) lands and `executive-questionnaire.md` §17 (vendor master scope) has a Pam answer. S1-S3 research artifacts remain valid; the 17 Phase 7 UX decisions (§46-§62) remain in force. No code, schema, plugin, solution, or environment changes under this decision -- pure documentation gate close. R-7-01 and R-7-05 conditional language resolves to the "not reaffirmed" branch; no separate risk-register amendments required. |
| 2026-05-22 | **Operating principles codified (decision §68) -- continuation session after morning batch.** Five principles captured in CLAUDE.md governing routine vs high-stakes work, surfacing of concerns, scope discipline, and the bias toward concrete artifacts for accounting-team feedback. §67 was annotated retrospectively to acknowledge that handoff Option C had a stronger concrete-artifact case than the morning gate credited; §67 itself stands as written. |
