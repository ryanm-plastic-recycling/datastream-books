# Datastream Books — Roadmap

> Single source of truth for project state and direction.
> Updated at the end of every session per AGENTS.md "Roadmap Maintenance".

## Current Phase

**Phase 4: Chart of Accounts (this session)**

Build the Chart of Accounts master table (`rm_chartofaccount`) in PRI-Books-Dev, seed it with a standard ~50-account starter set hung off a placeholder `Default Operating Entity`, and prove the seed script is re-runnable against any environment. After this lands, the next session moves to Journal Entry tables (`rm_journalentry`, `rm_journalentryline`) followed by the first plugin (`PostJournalEntryPlugin`).

## Completed Phases

> Newest first.

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
> (Phase 4 has been promoted to Current Phase — see top of file.)

### Phase 5 — Journal Entry tables (`rm_journalentry`, `rm_journalentryline`)

Header + lines for pre-post journal entries in Dataverse. Auto-numbering on the header (`JE-{Entity short}-{YYYY}-{NNNNN}`). Lookups to `rm_entity`, `rm_fiscalperiod`, `rm_chartofaccount`. Status choice (Draft / PendingApproval / Approved / Posted / Reversed / Voided). No plugins yet — schema first.

### Phase 6 — First Posting Plugin (`PostJournalEntryPlugin`)

The two-phase Dataverse + Azure SQL commit. SoD enforcement (`ApprovedBy != PostedBy`). Period-lock check. Per-entity hash-chain computation. Unit tests via FakeXrmEasy.

### Phase 7 — Vendor / Customer Integration with ERP

Books AR references `rm_customer` from PRI-Datastream rather than duplicating. Cross-solution lookup design. Read-only relationship; ownership boundary documented per `erp-pattern-notes.md` Pattern 3. Books-owned vendor master decision per decision log §22 — confirm scope before AP design.

### Phase 8 — AP / AR Core

Bills, invoices, receipts, aging reports. NACHA file generation for ACH payments (replaces the Leahy dependency tied to Macola). Track1099 integration for 1099 generation and W-9 collection.

### Phase 9 — Period Close + Reporting

Native model-driven reports: Trial Balance, Balance Sheet, P&L, Cash Flow, AR/AP aging, JE audit trail, ChangeRequest log. Period close attestation flow. Hash-chain verification job.

### Phase 10 — Macola Data Migration + Cutover

Historical archive into `archive` schema. Parallel run with Macola for at least one full close cycle (ideally two). Penny-perfect reconciliation. User-driven green light. Cutover at fiscal-period boundary per decision log §26.

### Future / Phase 2+ Work (out of v1 cutover scope)

- **Document AI (Claude API replacement of AI Builder).** Phase 2 strategic value driver — AI-driven invoice/PO/receipt discrepancy detection enabling headcount reallocation from manual validation. Pattern: documents land in SharePoint → Claude API extracts structured data → matched against POs/receipts → clean matches auto-route, exceptions to human review.
- **Power BI reporting.** Paginated reports, dashboards, cross-entity analytics. Replaces or augments native model-driven reports.
- **Bill.com / Ramp / Bank API integration.** Direct AP payment execution beyond NACHA file generation.
- **Credit limit management and enforcement.** Customer credit limits + risk scoring (decision log §17 — deferred from v1).
- **Limble PO replacement.** Bring PO workflow under Datastream rather than a parallel system.
- **Mobile-optimized UI.** Out of v1 scope per decisions.

## See Also

- [`decisions/datastream-books-decisions.md`](decisions/datastream-books-decisions.md) — full decision log
- [`architecture/`](architecture/) — data model, security, immutability, ERP patterns
- [`controls/`](controls/) — SoD matrix, approval policies, audit controls
- [`reference/erp-metadata/`](reference/erp-metadata/) — ERP solution metadata snapshot (REFERENCE ONLY)
- [`../AGENTS.md`](../AGENTS.md) — operating instructions for AI coding agents
