# Datastream Books — Roadmap

> Single source of truth for project state and direction.
> Updated at the end of every session per AGENTS.md "Roadmap Maintenance".

## Current Phase

### Phase 2 — Azure SQL provisioning + CI/CD foundation

**Goal:** Stand up the Azure SQL Dev database with real schemas/users, prove the immutability architecture against the live database, and wire GitHub Actions to Dataverse with federated identity (no client secrets).

**Active work:**
- V0001 schemas (`ledger`, `audit`, `reports`, `archive`) + `dbo.SchemaMigrations` metadata table
- V0002 `ledger.GeneralLedgerEntries` finalized (in `ledger` schema, with SchemaMigrations row)
- V0003 four contained SQL users (`dsb_app`, `dsb_migrate`, `dsb_reader`, `dsb_admin`) with least-privilege grants and explicit `DENY UPDATE, DELETE` on the ledger
- Immutability validation against the live DB (INSERT, then attempt UPDATE/DELETE — both must be blocked at SQL level)
- Entra app registration `datastream-books-cicd` + federated identity for GitHub Actions
- `deploy-dev.yml` rewritten for OIDC; first end-to-end CI run

**Auth strategy:** `priadmin` (SQL bootstrap admin) is used **once** to apply V0001–V0003. After migrations land, all daily work uses the `dsb_*` accounts. `priadmin` returns to break-glass-only status.

## Completed Phases

> Newest first.

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

### Phase 3 — First Real Dataverse Tables

`rm_entity`, `rm_fiscalperiod`, possibly `rm_chartofaccount`. Apply the 4-column master-data pattern from `erp-pattern-notes.md`. First solution components committed to `solution/src`.

### Phase 4 — First Posting Plugin (`PostJournalEntryPlugin`)

The two-phase Dataverse + Azure SQL commit. SoD enforcement (`ApprovedBy != PostedBy`). Period-lock check. Per-entity hash-chain computation. Unit tests via FakeXrmEasy.

### Phase 5 — Vendor / Customer Integration with ERP

Books AR references `rm_customer` from PRI-Datastream rather than duplicating. Cross-solution lookup design. Read-only relationship; ownership boundary documented per `erp-pattern-notes.md` Pattern 3. Books-owned vendor master decision per decision log §22 — confirm scope before AP design.

### Phase 6 — AP / AR Core

Bills, invoices, receipts, aging reports. NACHA file generation for ACH payments (replaces the Leahy dependency tied to Macola). Track1099 integration for 1099 generation and W-9 collection.

### Phase 7 — Period Close + Reporting

Native model-driven reports: Trial Balance, Balance Sheet, P&L, Cash Flow, AR/AP aging, JE audit trail, ChangeRequest log. Period close attestation flow. Hash-chain verification job.

### Phase 8 — Macola Data Migration + Cutover

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
