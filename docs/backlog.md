# Datastream Books -- Backlog

> Consolidated backlog of work items deferred / planned across the
> project. Each item has a description, a source reference (where the
> TODO / "deferred to" / "planned" marker lives in the repo today), a
> suggested target phase, a priority field, and a Pam-input-needed
> flag.
>
> Compiled 2026-05-21 from the documentation audit at
> [`audits/audit-2026-05-21-evening.md`](audits/audit-2026-05-21-evening.md)
> §E. Priority Index added 2026-05-22 in the doc-only session
> following decision [§67](decisions/datastream-books-decisions.md).
> Updated as items land or new items are discovered.
>
> **How to use this doc:**
>
> - Start at the **Priority Index** section below for the ranked view.
> - When picking up work, scan the relevant target-phase section for
>   the full item details.
> - When a backlog item is started, add a row to the change log at the
>   bottom of this doc with the BL-id, the date, and the session that
>   picked it up.
> - When a backlog item is completed, mark it `[Done YYYY-MM-DD]` in
>   place + add a change-log row. Do **not** delete the item -- the
>   audit trail is more valuable than the cleanup.
> - When a new item is discovered, add it to the appropriate target
>   phase section with the next BL-id (sequentially -- gaps OK), then
>   add a row to the Priority Index.

## Status legend

- **[Open]** -- not yet started.
- **[In progress]** -- started but not yet complete; include a "Started: YYYY-MM-DD" note.
- **[Done YYYY-MM-DD]** -- completed; retain for traceability.
- **[Deferred]** -- explicitly deferred to a later phase or post-v1.

---

## Priority Index

> Ranking applied 2026-05-22 in the doc-only session following decision
> [§67](decisions/datastream-books-decisions.md). **P0** = blocking
> Backend Track A (critical path). **P1** = Pam-input required or
> Pam-demo-blocker. **P2** = phase-tied technical work, including
> R-A-17 / R-A-18 / R-A-19 mitigations. **P3** = polish,
> deferred-per-§67, maintenance-window, or long-term. Sorted by
> priority, then by phase.

| BL | Subject (short) | Priority | Target phase | Pam-input |
|---|---|---|---|---|
| 38 | Vendor / customer integration design + ownership boundary doc | **P0** | 7-Backend Track A | Y (gated by §17) |
| 46 | Explicit decision row for §22 vendors (ERP-synced vs Books-owned) | **P0** | 7-Backend Track A | Y (gated by §17) |
| 01 | Approval threshold values $X / $Y / $Z | **P1** | Before Phase 8 | Y |
| 04 | `rm_basecurrency` / `rm_isconsolidationtarget` / `rm_legalname` on `rm_entity` | **P1** | Phase 10 (entity seeding) | Y (gated by §1) |
| 22 | `runbooks/change-management.md` | **P1** | Pre-cutover | Y (Pam triages CRs) |
| 23 | `runbooks/period-close.md` | **P1** | Phase 9 | Y (Pam authors) |
| 24 | `runbooks/year-end-close.md` | **P1** | Phase 10 | Y (Pam authors) |
| 34 | Posted-ledger field-level security on restricted accounts | **P1** | Phase 8 | Y (driven by approval policy) |
| 37 | Field-level security on `rm_entity.rm_ein` | **P1** | Phase 10 (before real entity seeding) | Y (gated by §1) |
| 47 | Form-level read-only on JE form (R-A-19 mitigation) | **P1** | Phase 7A.5 (any session) | N (Pam-demo-blocker) |
| 02 | `rm_isintercompany` flag on `rm_chartofaccount` | P2 | Phase 8 | N |
| 06 | `ApproveJournalEntryPlugin` (SoD-enforced) | P2 | Phase 7B / 8 | N |
| 07 | `ClosePeriodPlugin` + `ReopenPeriodPlugin` | P2 | Phase 9 | N |
| 08 | `ApproveVendorBankChangePlugin` | P2 | Phase 8 | N |
| 09 | `ApproveWirePlugin` | P2 | Phase 8 | N |
| 10 | `ChangeRequestApprovalPlugin` | P2 | Phase 7D (or earlier) | N |
| 11 | V0004 migration: `audit.PeriodCloseAttestation` | P2 | Phase 9 | N |
| 12 | V0005 migration: `audit.AuditEvents` | P2 | Phase 8 / 9 | N |
| 13 | V0006 migration: `ReportSnapshots` | P2 | Phase 9 | N |
| 14 | `scripts/Rotate-DsbAppPassword.ps1` | P2 | Pre-cutover | N |
| 15 | `verify-integrity.ps1` (on-demand hash chain verification) | P2 | Phase 8 / 9 | N |
| 16 | Nightly hash chain verification job | P2 | Phase 9 | N |
| 17 | Re-hash EntryIds 3 and 4 offline (Phase 6B post-check) | P2 | Phase 8 (any session) | N |
| 18 | Re-run negative tests against newly-written EntryIds | P2 | Phase 8 (any session) | N |
| 20 | `runbooks/disaster-recovery.md` | P2 | Pre-cutover | N |
| 21 | `runbooks/data-recovery.md` | P2 | Pre-cutover | N |
| 25 | `user-guides/` (ap-clerk, ar-clerk, controller, admin) | P2 | Phase 7E (can start at 7B) | Y (Pam reviews + approves) |
| 30 | AAD-only auth for `priadmin` | P2 | Pre-cutover | N |
| 31 | Quarterly access review owner + cadence + first review | P2 | Phase 7B | N (structure IT-built; Pam joins review later) |
| 32 | PIM workflow design | P2 | Pre-cutover | N |
| 33 | Vendor banking field-level security | P2 | Phase 8 | N |
| 36 | Single rotation script (KV + GH Actions + Dataverse) | P2 | Pre-cutover | N |
| 45 | Explicit decision row for Phase 8 email + Teams Graph API scope | P2 | Phase 8 kickoff | N |
| 48 | Per-JE line numbering plugin Option B (R-A-18 mitigation) | P2 | Phase 7B | N |
| 49 | User-guide docs of "Saving in Progress" hiccup (R-A-17 mitigation) | P2 | Phase 7B | N |
| 03 | Memo-only JE lines | P3 | Phase 8+ (when use case surfaces) | N |
| 05 | `rm_accountnumberingscheme` table | P3 | When COA validation plugin lands | N |
| 19 | POST-branch test of `sync-sp-secret-to-dataverse.ps1` | P3 | Maintenance window | N |
| 26 | Innovation Team logo binary | P3 | Deferred per §67 (was Phase 7A S4) | Y (Pam or exec sources) |
| 27 | Status pill placement decision | P3 | Deferred per §67 (was Phase 7A S10) | N |
| 28 | App module + theme application + logo packaging (S4) | P3 | Deferred per §67 | N |
| 29 | Phase 7A S5-S10 sessions | P3 | Deferred per §67 | N |
| 35 | Migrate plugin runtime credential to managed identity | P3 | Long-term (watch MS roadmap) | N |
| 50 | AGENTS.md What NOT to Do row for PRT-vs-CI/CD | **[Done 2026-05-22]** | -- | -- |
| 39 | §63 propagation | [Done 2026-05-21] | -- | -- |
| 40 | seed-data.md history fill | [Done 2026-05-21] | -- | -- |
| 41 | repo-structure.md branching strategy alignment | [Done 2026-05-21] | -- | -- |
| 42 | phase-7-ui-design.md update for §66 + §49 amendment | [Done 2026-05-21] | -- | -- |
| 43 | phase-7a-foundation-prompt.md amendment banner | [Done 2026-05-21] | -- | -- |
| 44 | risk-register.md update | [Done 2026-05-21] | -- | -- |

**Bucket totals:** P0 = 2, P1 = 8, P2 = 25, P3 = 8, Done = 7. Total = 50.

### Next Pam Conversation Agenda

The Pam-input items gating the upcoming Pam conversation (week of
2026-05-25). Consolidated prep at
[`memos/pam-conversation-prep-2026-05-w22.md`](memos/pam-conversation-prep-2026-05-w22.md)
(drafted in the same 2026-05-22 session as this Priority Index).

| Agenda item | Backlog row | Exec questionnaire § |
|---|---|---|
| Vendor master scope decision (ERP-synced vs Books-owned) | BL-46, then BL-38 | §17 |
| Approval threshold values $X / $Y / $Z | BL-01 | §3 |
| Legal entity full list | (no BL-id) | §1 |
| COA review and sign-off | (no BL-id) | §11 |

---

## Phase 7-Backend (Current Track A)

### BL-38 -- Vendor / customer integration design + ownership boundary doc

- **Description:** Books AR references `rm_customer` from PRI-Datastream rather than duplicating; vendors TBD per §22 / §17 of executive-questionnaire. Document the ownership boundary in `data-model.md` so future contributors do not write to the ERP-owned side.
- **Source:** `roadmap.md` Track A; `erp-pattern-notes.md` §3.
- **Target phase:** Phase 7-Backend (Track A).
- **Priority:** P0 (blocking Backend Track A) -- follows BL-46.
- **Pam-input-needed:** Y -- gated by executive-questionnaire §17 (vendor master scope).
- **Status:** [Open]

### BL-46 -- Explicit confirmation row for §22 vendors

- **Description:** Decision §22 says "Vendors/customers added as needed" with ambiguous "as needed" semantics. Need an explicit decision (likely §68+ in the decision log) capturing whether vendors are ERP-synced or Books-owned for Phase 8 AP design.
- **Source:** Audit Section A6, Section B; executive-questionnaire.md §17 (new).
- **Target phase:** Phase 7-Backend (Track A) before AP design.
- **Priority:** P0 (blocking Backend Track A) -- precedes BL-38.
- **Pam-input-needed:** Y -- gated by executive-questionnaire §17 (vendor master scope).
- **Status:** [Open]

---

## Phase 7A S4+ (deferred per §67)

> All items in this section were originally gated by the §66
> reaffirmation. Decision §67 (2026-05-22) deferred Phase 7A S4-S11
> until Backend Track A lands and §17 has a Pam answer. Items below
> remain captured for the eventual revisit decision but are not
> scheduled.

### BL-26 -- Innovation Team logo binary

- **Description:** Source the Innovation Team logo binary; package as web resource `rm_InnovationTeamLogo` in the Books solution. Source + placement (footer / corner watermark / welcome card) decided in S4.
- **Source:** `ui-styling.md` O4, O5.
- **Target phase:** Deferred per §67 (was Phase 7A S4).
- **Priority:** P3 (deferred + dependent on external asset).
- **Pam-input-needed:** Y -- Pam (or executive team) sources the logo binary.
- **Status:** [Open]

### BL-27 -- Status pill placement decision (which forms / which views)

- **Description:** Decide where the `StatusPill` PCF surfaces (rm_journalentry main form, JE list view, future bill/invoice forms). Captured during S10 implementation.
- **Source:** `ui-sitemap.md`, `ui-styling.md`.
- **Target phase:** Deferred per §67 (was Phase 7A S10).
- **Priority:** P3 (deferred).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-28 -- App module + theme application + logo packaging (S4)

- **Description:** Create the Datastream Books model-driven app module in PRI-Books-Dev; apply theme JSON binding the CSS variables from `ui-styling.md`; package both logos as web resources; configure sitemap per `ui-sitemap.md`. `pull-solution.ps1` + commit.
- **Source:** `ui-styling.md` "Books Theme Record" section; `ui-sitemap.md`.
- **Target phase:** Deferred per §67 (was Phase 7A S4).
- **Priority:** P3 (deferred).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-29 -- Phase 7A S5-S10 sessions (breadcrumb / recent / dashboard / patterns / toolchain / status pill)

- **Description:** Subsequent Phase 7A sessions per the work breakdown approved 2026-05-21 evening kickoff. Several have been descoped or deferred (breadcrumb tentatively dropped; recent items leans on platform built-in).
- **Source:** `ui-styling.md`, `ui-sitemap.md`, S0 kickoff conversation.
- **Target phase:** Deferred per §67.
- **Priority:** P3 (deferred).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-47 -- (NEW per audit risk R-A-19) Form-level read-only fields on JE form

- **Description:** Make `rm_postedby_user`, `rm_journalentrynumber`, `rm_totaldebit`, `rm_totalcredit` read-only at the form level (maker portal property), not just at the plugin layer. Currently the maker portal accepts edits to these fields and the save throws -- confusing during Pam-facing demo. Design committed 2026-05-22 in [`architecture/form-readonly-enforcement.md`](architecture/form-readonly-enforcement.md) (Business Rule on the JE main form recommended). Implementation runbook committed 2026-05-22 in [`runbooks/r-a-19-business-rule-implementation.md`](runbooks/r-a-19-business-rule-implementation.md) (30-45 min operator-driven session; includes `rm_posteddatetime` as a fifth target field per the design doc's "default YES" recommendation).
- **Source:** Risk-register R-A-19 (added 2026-05-21). Audit Section G5.
- **Target phase:** Phase 7A.5 (any session) -- independent of S4-S11 deferral per §67.
- **Priority:** P1 (Pam-demo-blocker; precondition for any Pam-facing shell demo).
- **Pam-input-needed:** N -- IT-side technical fix; removes a blocker to showing Pam future artifacts rather than asking her for input.
- **Status:** [Ready -- operator handoff] -- runbook in [`runbooks/r-a-19-business-rule-implementation.md`](runbooks/r-a-19-business-rule-implementation.md) waiting for a 30-45 min operator-driven implementation session.

---

## Phase 7B (Core Transactional Screens)

### BL-06 -- `ApproveJournalEntryPlugin`

- **Description:** SoD-enforced approval plugin for JE state transitions into Approved. Implements `CreatedBy != ApprovedBy` and threshold-based routing per `approval-policies.md`.
- **Source:** `sod-matrix.md`, `security-model.md`.
- **Target phase:** Phase 7B (Core Transactional Screens) or Phase 8 (AP/AR Core), whichever comes first.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-48 -- (NEW per audit risk R-A-18) Per-JE line numbering plugin (Option B)

- **Description:** Build the Option B plugin that computes per-JE local line numbers on read so existing data does not need backfill. Resolves the "line 437 on a 2-line JE" confusion in audit reports.
- **Source:** Risk-register R-A-18 (added 2026-05-21). Carry-forward from Phase 6B closure conversation.
- **Target phase:** Phase 7B (before JE hybrid grid+form UI lands).
- **Priority:** P2 (R-A-18 mitigation; phase-tied).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-49 -- (NEW per audit risk R-A-17) User-guide documentation of "Saving in Progress" UI hiccup

- **Description:** Document in `user-guides/ap-clerk.md` / `ar-clerk.md` / `controller.md` that the maker-portal save spinner may hang visibly during JE posting due to Azure SQL serverless cold-start. Until Phase 7B form-level UX lands.
- **Source:** Risk-register R-A-17 (added 2026-05-21).
- **Target phase:** Phase 7B documentation; can also be addressed inline by the Phase 7B form-level UX work.
- **Priority:** P2 (R-A-17 mitigation; phase-tied).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-50 -- (NEW per audit risk R-A-20) AGENTS.md "What NOT to Do" row for PRT-vs-CI/CD

- **Description:** Add a row to the AGENTS.md "What NOT to Do" list explicitly forbidding routine plugin code changes via PRT. Reinforces the rule already articulated in the Code Conventions section and `plugin-registration.md`.
- **Source:** Risk-register R-A-20 (added 2026-05-21).
- **Target phase:** Any session (doc-only, low cost).
- **Priority:** _(N/A)_
- **Pam-input-needed:** _(N/A)_
- **Status:** [Done 2026-05-22] -- landed in commit `e3b3b91` (item 2 of the 2026-05-22 morning execution session).

### BL-31 -- Quarterly access review owner + cadence + first review

- **Description:** Assign owner (likely Pam + IT jointly per `sod-matrix.md`); establish cadence; run first access review covering all 7 finance-specific roles + the 4 SQL `dsb_*` users + the SP grants on KV.
- **Source:** `sod-matrix.md`, `security-model.md`, `key-vault-management.md` "Quarterly review checklist".
- **Target phase:** Phase 7B (when roles land + first wave of users get assigned).
- **Priority:** P2 (phase-tied; gated by 7B role assignment).
- **Pam-input-needed:** N -- structure IT-built; Pam joins the review activity later.
- **Status:** [Open]

---

## Phase 8 (AP / AR Core)

### BL-08 -- `ApproveVendorBankChangePlugin`

- **Description:** Dual-approval plugin for vendor banking info changes. SoD: `VendorSetupBy != BankChangedBy`; two approvers required; out-of-band confirmation note check per `approval-policies.md`.
- **Source:** `sod-matrix.md`, `approval-policies.md`.
- **Target phase:** Phase 8.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-09 -- `ApproveWirePlugin`

- **Description:** Wire-transfer approval plugin. SoD: `Initiator != Approver`.
- **Source:** `sod-matrix.md`.
- **Target phase:** Phase 8.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-12 -- V0005 migration: `audit.AuditEvents`

- **Description:** Azure SQL append-only audit table. Same DENY UPDATE/DELETE pattern as `ledger.GeneralLedgerEntries`. Semantic event log (PostJEAccepted / PostJERejected / SoDViolationAttempted / etc.).
- **Source:** `immutability-design.md` §F; `audit-controls.md`.
- **Target phase:** Phase 8 / 9.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-33 -- Vendor banking field-level security

- **Description:** Field-level security on vendor banking info columns. Visible only to `Vendor Bank Change` role. Specific column-level design TBD.
- **Source:** `security-model.md`.
- **Target phase:** Phase 8.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-34 -- Posted-ledger field-level security on restricted accounts

- **Description:** Field-level security on posted ledger amounts in restricted accounts (e.g., payroll suspense, M&A). Driven by approval policy.
- **Source:** `security-model.md`.
- **Target phase:** Phase 8 (once approval-policies rows exist).
- **Priority:** P1 (driven by approval policy which Pam owns).
- **Pam-input-needed:** Y -- Pam owns approval-policy definition which determines which accounts are restricted.
- **Status:** [Open]

### BL-45 -- Explicit decision row for Phase 8 email + Teams Graph API scope

- **Description:** Decision §53 deferred email + Teams "to Phase 8+" without an explicit row pinning Phase 8 scope. Confirm at Phase 8 kickoff whether Graph-API email + Teams app registration are Phase 8 deliverables.
- **Source:** Audit Section B; `roadmap.md` Phase 8 (newly annotated).
- **Target phase:** Phase 8 kickoff.
- **Priority:** P2 (phase-tied scoping decision).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-02 -- `rm_isintercompany` flag on `rm_chartofaccount`

- **Description:** Add the `rm_isintercompany` boolean flag deferred from Phase 4. Used by inter-company JE plugin to identify IC clearing accounts.
- **Source:** `data-model.md`; decision §34.
- **Target phase:** Phase 8 (when AR/AP land; IC plugin behavior defined).
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

---

## Phase 9 (Period Close + Reporting)

### BL-07 -- `ClosePeriodPlugin` + `ReopenPeriodPlugin`

- **Description:** Period close attestation flow (writes SHA-256 close hash to `rm_fiscalperiod.rm_closehashbinary` + row in `audit.PeriodCloseAttestation`); reopen workflow with elevated role + audit event.
- **Source:** `immutability-design.md` §C, §D.
- **Target phase:** Phase 9.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-11 -- V0004 migration: `audit.PeriodCloseAttestation`

- **Description:** Azure SQL append-only table holding close hashes per (entity, period).
- **Source:** `immutability-design.md` §D.
- **Target phase:** Phase 9.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-13 -- V0006 migration: `ReportSnapshots`

- **Description:** Azure SQL table holding closed-period report snapshots (BS / P&L / Cash Flow / TB) as JSON blobs with SHA-256 hashes. Drill-down on closed periods reads through snapshot to ledger (per decision §55).
- **Source:** `immutability-design.md` §G; decision §55.
- **Target phase:** Phase 9.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-15 -- `verify-integrity.ps1` (on-demand hash chain verification)

- **Description:** PowerShell script that walks each entity's chain in `(EntityId, EntryId)` order and re-computes `RowHash` offline to verify the chain has not been tampered with.
- **Source:** `immutability-design.md` §B.
- **Target phase:** Phase 8 / 9.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-16 -- Nightly hash chain verification job

- **Description:** SQL Agent or Azure Function that runs `verify-integrity.ps1`'s logic on a schedule (nightly) against `ledger.GeneralLedgerEntries`; writes results to `audit.LedgerIntegrityCheckpoints`; alerts on mismatch.
- **Source:** `immutability-design.md` §B; `audit-controls.md`.
- **Target phase:** Phase 9.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-17 -- Re-hash EntryIds 3 and 4 offline (Phase 6B integrity post-check)

- **Description:** Compute `RowHash` of EntryIds 3 and 4 offline via `LedgerRowHasher.ComputeRowHash` against the row data and verify bytes match the live values. First nightly job's expected behavior; running now confirms the chain held.
- **Source:** `immutability-validation.md` "Still-pending follow-up checks".
- **Target phase:** Phase 8 (any session) -- before any further posts that would extend the chain.
- **Priority:** P2 (phase-tied technical verification).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-18 -- Re-run negative tests against newly-written EntryIds

- **Description:** Confirm SQL 229 / DENY grants still in force after the first real INSERT (re-run UPDATE / DELETE as `dsb_app` against EntryIds 3 / 4 and verify rejection).
- **Source:** `immutability-validation.md` "Still-pending follow-up checks".
- **Target phase:** Phase 8 (any session).
- **Priority:** P2 (phase-tied technical verification).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-23 -- `runbooks/period-close.md`

- **Description:** Month-end close runbook. Authored by Pam (with IT support) per the Ownership Artifacts table.
- **Source:** `repo-structure.md`; ownership table.
- **Target phase:** Phase 9.
- **Priority:** P1 (Pam-input required; Pam authors per Ownership Artifacts table).
- **Pam-input-needed:** Y -- Pam authors with IT support.
- **Status:** [Open]

---

## Phase 10 (Macola Data Migration + Cutover)

### BL-04 -- `rm_basecurrency`, `rm_isconsolidationtarget`, `rm_legalname` on `rm_entity`

- **Description:** Three nice-to-have columns deferred from Phase 3. Add before real entity seeding.
- **Source:** `data-model.md` "Out of Phase 3 scope (deferred)".
- **Target phase:** Phase 10 (real entity seeding).
- **Priority:** P1 (gated by §1 entity definitions from Pam).
- **Pam-input-needed:** Y -- entity definitions come from executive-questionnaire §1 (Pam-bound).
- **Status:** [Open]

### BL-24 -- `runbooks/year-end-close.md`

- **Description:** Annual close runbook.
- **Source:** `repo-structure.md`.
- **Target phase:** Phase 10.
- **Priority:** P1 (Pam-input required; analogous to BL-23 period-close runbook).
- **Pam-input-needed:** Y -- Pam authors with IT support.
- **Status:** [Open]

### BL-37 -- Field-level security on `rm_entity.rm_ein`

- **Description:** Add column encryption (premium tier) or field-level security to `rm_entity.rm_ein`. Currently plain text per Phase 3 build; placeholder noted that no real EIN values should be entered until this is set up.
- **Source:** `data-model.md` `rm_entity` rationale; `security-model.md`.
- **Target phase:** Phase 10 (before real entity seeding -- §1 of exec questionnaire answer triggers).
- **Priority:** P1 (gated by §1 entity definitions from Pam).
- **Pam-input-needed:** Y -- triggered when real EIN values are entered, which is gated by executive-questionnaire §1.
- **Status:** [Open]

---

## Pre-cutover (any phase before Phase 10 closes)

### BL-14 -- `scripts/Rotate-DsbAppPassword.ps1`

- **Description:** PowerShell script to rotate `dsb_app` SQL password end-to-end: generate, ALTER USER, store in Vault, verify positive + negative tests.
- **Source:** `key-vault-management.md` rotation procedure (TBD marker on the script reference).
- **Target phase:** Pre-cutover.
- **Priority:** P2 (phase-tied operational work).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-20 -- `runbooks/disaster-recovery.md`

- **Description:** DR procedures for Dataverse + Azure SQL + SharePoint. Tested annually.
- **Source:** `immutability-design.md` §I; `repo-structure.md`.
- **Target phase:** Pre-cutover.
- **Priority:** P2 (phase-tied operational doc).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-21 -- `runbooks/data-recovery.md`

- **Description:** Restore procedures (Dataverse PITR, Azure SQL PITR + LTR, SharePoint recovery).
- **Source:** `immutability-design.md` §I; `repo-structure.md`.
- **Target phase:** Pre-cutover.
- **Priority:** P2 (phase-tied operational doc).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-22 -- `runbooks/change-management.md`

- **Description:** Operational change-management runbook documenting the in-app ChangeRequest flow + approval gates + the deploy-prod CR-reference requirement.
- **Source:** `security-model.md`; `repo-structure.md`.
- **Target phase:** Pre-cutover.
- **Priority:** P1 (Pam co-owns; she triages CRs per ownership table).
- **Pam-input-needed:** Y -- Pam owns CR triage and signs off on the operational flow.
- **Status:** [Open]

### BL-25 -- `user-guides/` (ap-clerk, ar-clerk, controller, admin)

- **Description:** End-user-facing documentation for each persona. Eventually published in TalentLMS per the training rollout decision (§27).
- **Source:** `repo-structure.md`; decision §27.
- **Target phase:** Phase 7E (TalentLMS modules), but can start as soon as 7B transactional screens land.
- **Priority:** P2 (phase-gated; cannot write AP clerk guide before AP screens exist).
- **Pam-input-needed:** Y -- Pam reviews and approves per Ownership Artifacts table.
- **Status:** [Open]

### BL-30 -- AAD-only auth for `priadmin`

- **Description:** Migrate `priadmin` (SQL Server bootstrap admin, bypasses DENY by `dbo` mapping) to AAD-only auth via Entra group + Active Directory admin on the server. Removes the SQL password as a stealable artifact.
- **Source:** `sql-account-management.md`; `immutability-validation.md` Critical Finding.
- **Target phase:** Pre-cutover.
- **Priority:** P2 (phase-tied security hardening).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-32 -- PIM workflow design

- **Description:** No standing admin access to PRI-Books production. Admin actions go via deployment pipeline OR time-bound elevation request through Entra PIM.
- **Source:** `security-model.md`.
- **Target phase:** Pre-cutover.
- **Priority:** P2 (phase-tied security hardening).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-36 -- Single rotation script that touches all three SP-secret consumers

- **Description:** End-to-end rotation: KV update + GitHub Actions `AZURE_CLIENT_SECRET` update + `sync-sp-secret-to-dataverse.ps1` run, in one pass. Currently only the Dataverse half is automated.
- **Source:** `credential-access-design.md` "Open items"; `key-vault-management.md` fan-out reminder.
- **Target phase:** Pre-cutover.
- **Priority:** P2 (phase-tied operational tooling).
- **Pam-input-needed:** N.
- **Status:** [Open]

---

## Maintenance window backlog (operator's discretion)

### BL-19 -- POST branch test of `sync-sp-secret-to-dataverse.ps1`

- **Description:** Test the POST branch of the sync script live. Requires deleting the `rm_sqlkvclientsecret` value row to force a value-row-not-present condition; brief plugin outage window during test.
- **Source:** `immutability-validation.md` (logged 2026-05-21 maintenance backlog).
- **Target phase:** Dedicated maintenance window with documented rollback plan.
- **Priority:** P3 (operator's discretion; non-blocking).
- **Pam-input-needed:** N.
- **Status:** [Open]

---

## Always-pending items needing operator input (cannot be self-served)

### BL-01 -- Approval threshold values $X / $Y / $Z

- **Description:** Pam-owned dollar thresholds for bills, JEs, write-offs. Blocks `rm_approvalpolicy` row authoring.
- **Source:** Executive questionnaire §3.1, §3.2, §3.3; `approval-policies.md`; `sod-matrix.md` "Pending: Threshold Values".
- **Target phase:** Before Phase 8.
- **Priority:** P1 (Pam-input required; conversation-imminent per Next Pam Conversation Agenda).
- **Pam-input-needed:** Y -- Pam owns threshold definition per Ownership Artifacts table.
- **Status:** [Open -- Pending Pam]

---

## Phase 2+ (post-v1, deliberate scope deferral)

### BL-35 -- Migrate plugin runtime credential to managed identity

- **Description:** When Dataverse plugin sandbox supports managed identity, migrate from SP-secret pattern. Eliminates the SP client secret from the env-var surface and removes the plaintext-in-Dataverse risk accepted under §63.
- **Source:** `credential-access-design.md`; `security-model.md`.
- **Target phase:** Long-term -- watch Microsoft roadmap.
- **Priority:** P3 (long-term; gated by external platform support).
- **Pam-input-needed:** N.
- **Status:** [Open]

### BL-03 -- Memo-only JE lines

- **Description:** Support JE lines that have neither debit nor credit (informational lines). Deferred from Phase 5.
- **Source:** `data-model.md`.
- **Target phase:** Phase 8+ when first concrete use case surfaces.
- **Priority:** P3 (deferred; gated by use-case surfacing).
- **Pam-input-needed:** N.
- **Status:** [Deferred]

### BL-05 -- `rm_accountnumberingscheme` table

- **Description:** Soft-validation table documenting per-entity account number ranges. Deferred from Phase 4 -- no immediate consumer.
- **Source:** `data-model.md`; decision §34.
- **Target phase:** When COA validation plugin lands.
- **Priority:** P3 (deferred; gated by COA validation plugin).
- **Pam-input-needed:** N.
- **Status:** [Deferred]

### BL-10 -- `ChangeRequestApprovalPlugin`

- **Description:** Enforce `RequestedBy != ApprovedBy != AssignedTo` on `rm_changerequest`. Multi-image attachment workflow per decision §31.
- **Source:** `immutability-design.md` §J; `security-model.md`.
- **Target phase:** Phase 7D (when ChangeRequest UI lands) or earlier if the table is wired up.
- **Priority:** P2 (phase-tied technical work).
- **Pam-input-needed:** N.
- **Status:** [Open]

---

## Doc hygiene (any session)

### BL-39 -- §63 propagation completed 2026-05-21

- **Description:** Three files updated to reflect §63 plain-Text env var reality: `credential-access-design.md`, `plugin-registration.md`, `key-vault-management.md`.
- **Source:** Audit Section F1.
- **Target phase:** Doc-only.
- **Priority:** _(N/A)_
- **Pam-input-needed:** _(N/A)_
- **Status:** [Done 2026-05-21]

### BL-40 -- seed-data.md history fill completed 2026-05-21

- **Description:** Recorded the Phase 4 54-row COA seed run in PRI-Books-Dev. Future-seed section trimmed accordingly.
- **Source:** Audit Section F.
- **Target phase:** Doc-only.
- **Priority:** _(N/A)_
- **Pam-input-needed:** _(N/A)_
- **Status:** [Done 2026-05-21]

### BL-41 -- repo-structure.md branching strategy alignment completed 2026-05-21

- **Description:** Replaced the four-branch table in `repo-structure.md` with a paragraph aligning with AGENTS.md solo-dev rule. Deleted the "Branch" row from the naming conventions table in AGENTS.md (which was internally inconsistent with its own Branching Policy).
- **Source:** Audit Section A6.
- **Target phase:** Doc-only.
- **Priority:** _(N/A)_
- **Pam-input-needed:** _(N/A)_
- **Status:** [Done 2026-05-21]

### BL-42 -- phase-7-ui-design.md update for §66 + §49 amendment

- **Description:** Cross-reference table for §49 annotated with the ui-styling.md amendment; Total Timeline + Phase 7A description updated for §66.
- **Source:** Audit Section A1, A2.
- **Target phase:** Doc-only.
- **Priority:** _(N/A)_
- **Pam-input-needed:** _(N/A)_
- **Status:** [Done 2026-05-21]

### BL-43 -- phase-7a-foundation-prompt.md amendment banner

- **Description:** Top-of-file banner explaining S1-S3 ran in parallel per §66; pre-flight checklist now advisory.
- **Source:** Audit Section A2.
- **Target phase:** Doc-only.
- **Priority:** _(N/A)_
- **Pam-input-needed:** _(N/A)_
- **Status:** [Done 2026-05-21]

### BL-44 -- risk-register.md update completed 2026-05-21

- **Description:** R-7-01 and R-7-05 amended for §66 framing. R-A-01 + R-A-07 downgraded to Low post-§65. R-A-17 through R-A-20 added.
- **Source:** Audit Section D.
- **Target phase:** Doc-only.
- **Priority:** _(N/A)_
- **Pam-input-needed:** _(N/A)_
- **Status:** [Done 2026-05-21]

---

## Change log

| Date | Backlog change | Session / commit |
|---|---|---|
| 2026-05-21 | Backlog file created from audit Section E. 46 items consolidated. Items BL-39 through BL-44 are doc-hygiene items completed during the same cleanup session. | Phase 7A audit cleanup (commit `09f833b`) |
| 2026-05-22 | Priority Index added (50 items ranked P0-P3). Next Pam Conversation Agenda sub-section added. Inline Priority + Pam-input-needed fields populated on all items. BL-50 marked Done (closed by AGENTS.md What NOT to Do entry committed earlier this session as `e3b3b91`). BL-26-BL-29 reclassified P3 + "deferred per §67" target phase per decision §67. | 2026-05-22 morning execution session (item 4) |
| 2026-05-22 | BL-47 status changed from `[Open]` to `[Ready -- operator handoff]`. Implementation runbook drafted at [`runbooks/r-a-19-business-rule-implementation.md`](runbooks/r-a-19-business-rule-implementation.md). `rm_posteddatetime` added as a fifth target field per the design doc's default-yes recommendation. Bucket totals unchanged (BL-47 not closing yet). | 2026-05-22 continuation session (item 4) |

## See also

- [`audits/audit-2026-05-21-evening.md`](audits/audit-2026-05-21-evening.md) -- the audit that produced this backlog
- [`roadmap.md`](roadmap.md) -- phase sequencing
- [`risk-register.md`](risk-register.md) -- some backlog items are risk mitigations
- [`decisions/datastream-books-decisions.md`](decisions/datastream-books-decisions.md) -- decisions that gate backlog items
- [`memos/pam-conversation-prep-2026-05-w22.md`](memos/pam-conversation-prep-2026-05-w22.md) -- consolidated prep for the upcoming Pam conversation (drafted in the 2026-05-22 morning session)
