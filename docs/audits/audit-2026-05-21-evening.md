# Datastream Books -- Documentation Audit, 2026-05-21 (evening)

> Autonomous documentation audit run during operator gap window after
> Phase 7A S1-S3 closed and pushed (commit `a7cde3b`). Read-only audit
> -- nothing is fixed here; tomorrow operator decides what to act on.
>
> Sections: A decision conflicts, B roadmap drift, C executive
> questionnaire status, D risk register reconciliation, E backlog
> consolidation, F documentation hygiene, G open questions for
> overnight rest.

## Inputs read

| File / Folder | Read in full | Notes |
|---|---|---|
| `docs/decisions/datastream-books-decisions.md` | Yes (§1-§66 + supporting sections) | Most recent edits tonight (S2/S3 commit). |
| `docs/roadmap.md` | Yes | Includes tonight's Track A/Track B restructure. |
| `docs/risk-register.md` | Yes | R-7-01 through R-7-05 + R-A-01 through R-A-15 + R-A-16 closed. |
| `docs/memos/executive-questionnaire.md` | Yes | No responses recorded inline yet. |
| `docs/memos/president-memo.md` | Yes | Strategy memo, May 19 2026. |
| `docs/architecture/immutability-validation.md` | Yes | Includes tonight's S1 follow-up section. |
| `docs/architecture/ui-styling.md` | Yes (authored tonight). | |
| `docs/architecture/ui-sitemap.md` | Yes (authored tonight). | |
| `docs/architecture/immutability-design.md` | Yes | |
| `docs/architecture/security-model.md` | Yes | |
| `docs/architecture/data-model.md` | Yes | |
| `docs/architecture/erp-pattern-notes.md` | Yes | |
| `docs/architecture/credential-access-design.md` | Yes | |
| `docs/controls/sod-matrix.md` | Yes | |
| `docs/controls/approval-policies.md` | Yes | |
| `docs/controls/audit-controls.md` | Yes | |
| `docs/runbooks/plugin-registration.md` | Yes | Multiple staleness items. |
| `docs/runbooks/cicd-setup.md` | Yes | |
| `docs/runbooks/key-vault-management.md` | Yes | |
| `docs/runbooks/sql-account-management.md` | Yes | |
| `docs/runbooks/phase-7a-foundation-prompt.md` | Yes | Pre-dates §66; pre-flight checklist now wrong. |
| `docs/decisions/phase-7-ui-design.md` | Yes | Pre-dates §66; needs amendment note. |
| `docs/reference/seed-data.md` | Yes | |
| `docs/repo-structure.md` | Yes | |
| `AGENTS.md` | Yes | |
| `CLAUDE.md` | In context. | |

---

## SECTION A -- Decision conflicts

### A1. §49 vs §66 visual-identity amendment -- partial propagation

- **Decisions involved:** §49 (match ERP color palette), §59 (ERP as primary visual reference), §66 (parallel-track + ui-styling reframe), tonight's in-place amendment of §49.
- **Conflict:** §49 has been amended in place to point at `ui-styling.md` for the operative state (ERP has no custom palette, Books defines its own minimal CSS-variable theme). But two downstream artifacts still reference §49 without acknowledgment:
  - `docs/decisions/phase-7-ui-design.md` line 37 cross-reference table for §49 says "Match Datastream ERP color palette (blue) + corner logo; competitor finance UIs for pattern inspiration" -- no amendment note.
  - `docs/runbooks/phase-7a-foundation-prompt.md` references §49 + the 17 UX decisions as a single set; pre-dates §66 entirely.
- **Proposed resolution:** Update `phase-7-ui-design.md` cross-reference table to note "§49 amended 2026-05-21 per ui-styling.md." Update `phase-7a-foundation-prompt.md` to acknowledge it pre-dates §66 (most efficient: add a top-of-file banner; do not rewrite the prompt itself, since it remains a useful reference for a future "full" 7A kickoff). Both fixes are doc-only edits.

### A2. §58 vs §66 sequential vs parallel -- handled explicitly, but downstream docs still claim sequential

- **Decisions involved:** §58 (strict sequential, UI dormant), §66 (parallel-track provisional).
- **Conflict:** §66 explicitly supersedes §58 for S1-S3 only. But several docs still describe the model as strict-sequential:
  - `phase-7-ui-design.md` "Total timeline" section says "Starts only after all backend phases ... are complete" with no §66 mention.
  - `phase-7-ui-design.md` "Sub-phases" Phase 7A description does not flag that S1-S3 ran early.
  - `risk-register.md` R-7-01 mitigation describes the strict-sequential context; R-7-01 risk profile shift noted in §66 has not been reflected in the risk row.
  - `phase-7a-foundation-prompt.md` pre-flight checklist requires Backend Phase 8 / 9 / 10 / 11+ complete -- now factually unmet for S1-S3.
- **Proposed resolution:** Edit `phase-7-ui-design.md` and `risk-register.md` to reference §66 with the "provisional through S3" caveat. Add a banner to `phase-7a-foundation-prompt.md`. Doc-only edits.

### A3. §38 / §63 -- env var pattern transition not propagated to runbooks

- **Decisions involved:** §38 (Plugin reads conn string from KV at runtime via SP secret stored as Secret-type env var), §63 (rm_sqlkvclientsecret converted from Secret to plain Text; only Dataverse-side delivery changes).
- **Conflict:** §63 explicitly supersedes the Secret-type half of §38 for `rm_sqlkvclientsecret`. The architecture doc and three runbooks still describe the Secret-type path as canonical:
  - `architecture/credential-access-design.md` lines 11, 64-89, 167, 170 describe `rm_sqlkvclientsecret` as Secret type. Banner says "Phase 6B (2026-05-21)" but predates §63 (also 2026-05-21).
  - `runbooks/plugin-registration.md` §"Phase 6B prerequisites -- Dataverse Environment Variables" (lines 130-198) walks the operator through creating `rm_sqlkvclientsecret` as Secret type, with elaborate Vault firewall + dual-SP RBAC prerequisites that are no longer needed for the env var. Includes a verification block that flags `rm_sqlkvclientsecret [String]` as "a hard stop" -- which is now exactly the correct state.
  - `runbooks/key-vault-management.md` lines 50-54, 110-130, 297-302 describe the Secret-type pattern and "consumer fan-out" reminder #3 (Dataverse env var updates automatically when Vault secret updates) which §63 explicitly broke (the new sync script is the only writer now).
- **Proposed resolution:** Sweep these three files in a focused doc-hygiene commit. Each file gets an amendment note at the top pointing to §63 + the section(s) below pointing to `scripts/sync-sp-secret-to-dataverse.ps1`. The full Secret-type narrative can stay as historical context (delete the "do not create as String" warning, flip to "the env var IS String per §63" instruction).

### A4. §57 vs §66 -- "no design review" vs "shell-only demo framing"

- **Decisions involved:** §57 (no initial Finance Lead design sign-off; CR-based ownership after pages land), §66 (Finance Lead first encounter must be framed "shell only, no transactions yet").
- **Conflict:** These do not strictly contradict but they create ambiguity. §57 says the Finance Lead exercises ownership via CR after pages land in dev. §66 says the first Finance Lead encounter with the 7A shell needs explicit framing. Question: does the framed-as-shell-only demo count as "pages landed in dev" under §57 (and therefore unlock CR-based ownership), or is it pre-§57 because there are no transactional pages?
- **Proposed resolution:** Clarify in §66's body language (or a §67 follow-up) that the shell-only demo is **not** the start of §57 CR-based ownership; it is a status update. the Finance Lead's CR window opens when 7B transactional pages land. Phrasing matters for the project narrative.

### A5. §50 hybrid JE entry vs ui-sitemap.md "Live in 7A shell" wording

- **Decisions involved:** §50 (hybrid JE entry built in 7B), ui-sitemap.md row "Journal Entries -- Live in 7A shell".
- **Conflict:** Not really a conflict, but the sitemap's "Live in 7A shell" wording for JE could imply the hybrid grid+form is live in 7A. The intended meaning is "the Dataverse default list view works today because the table exists from Phase 5; the §50 hybrid mode is a 7B build." The conflict is presentation, not substance.
- **Proposed resolution:** Sharpen the ui-sitemap.md text. One-line clarification under the GL section noting that the default Dataverse view is live; the §50 hybrid form is a 7B deliverable. Doc-only edit.

### A6. AGENTS.md branching policy vs repo-structure.md

- **Decisions involved:** AGENTS.md Branching Policy (solo-dev, work directly on main, no branches), repo-structure.md "Branching Strategy" section (lists `develop`, `feature/*`, `hotfix/*` branches).
- **Conflict:** Direct contradiction. AGENTS.md is authoritative ("solo-developer project. Do not create branches.") -- repo-structure.md describes a multi-branch workflow that does not apply.
- **Proposed resolution:** Update repo-structure.md "Branching Strategy" section to reflect the AGENTS.md rule. Note solo-dev, single-branch, no feature/hotfix branches. Doc-only edit.

---

## SECTION B -- Roadmap drift

For each phase in `roadmap.md` -- which decisions back it, where the gaps are.

### Completed phases (chronological)

| Phase | Decision backing | Gaps / drift |
|---|---|---|
| Phase 0 (strategy) | §1-§32, §41-§42 (assorted strategy + identity decisions), president-memo.md | Strong backing. |
| Phase 1 (repo foundation + design sprint) | §28-§29 (environments), §72 setup notes in change log, decisions §A-§K | Strong backing. |
| Phase 2 (Azure SQL + CI/CD) | §H (dev/prod separation), §A (append-only ledger), §C (server-side posting), §38 (credential pattern) | Strong backing. |
| Phase 3 (foundational tables) | §25 (multi-entity), §F (audit), data-model.md §rm_entity | Strong backing. |
| Phase 4 (COA) | §23 (pre-populate standard COA, Finance Lead reviews), §25 (multi-entity), §34 (per-entity COA decision) | Strong backing. Seed-data.md does not record the 54-row seed run -- minor omission. |
| Phase 5 (JE tables) | §35 (per-table decisions), §E (SoD), §C (server-side posting) | Strong backing. |
| Phase 6A (plugin Dataverse-only) | §35 + plugin spec in immutability-design.md §C | Strong backing. |
| Phase 6B (KV provisioning) | §36, §37, §43-§45 (KV/firewall/credential rotations) | Strong backing. |
| Phase 6B (code) | §38-§42 (credential pattern + hash + concurrency + failure semantics) | Strong backing. |
| Phase 6B (validation closed) | §63-§65 (env var pivot + ASCII + live validation) | Strong backing. |
| Phase 7A S1-S3 (tonight) | §66 (provisional parallel-track) | Strong backing. |

### Current phase

| Track | Decision backing | Gaps |
|---|---|---|
| Track A: Phase 7 Backend (Vendor / Customer integration) | §22 (vendors as needed), erp-pattern-notes.md §3 (cross-solution rm_customer) | Decision §22 says "as needed" which is wider than the Track A scope. Worth a clarifying decision before AP design (do we sync vendors from ERP at all, or are Books vendors fully Books-owned?). |
| Track B: Phase 7A UI Foundation S1-S3 (research only) | §66 | Strong backing. |

### Future phases

| Phase | Decision backing | Gaps |
|---|---|---|
| Phase 8 (AP / AR Core) | §13 (Track1099), §14 (Graph API email), §16 (NACHA), §17 (credit limits deferred), §22 (vendors as needed), §53 (notifications: email + Teams added here per deferral). | Decision §53 spread Phase 8 scope to include email + Teams Graph-API wiring. That is not in §13/§14 directly. Worth a confirming row before 8 opens. |
| Phase 9 (Period Close + Reporting) | §D (period locks), §G (signed reports), §54 (three report formats), §55 (universal drill-down). | Decisions §54/§55 are UI-track decisions; the backend Phase 9 needs the snapshot data they sit on. No backend-side decision pins the snapshot schema yet -- immutability-design.md §G says "(V0006 -- planned)" but no decision row exists for that table's schema. |
| Phase 10 (Macola Data Migration + Cutover) | §26 (cutover at fiscal period), R-A-05 (Macola data quality) | Strong directional backing; tactical decisions (what to archive, what to migrate) not yet captured. |
| Phase 11/12+ ("Remaining backend items") | None | The roadmap explicitly notes this is a placeholder. The lack of decision backing is the point. |
| Phase 7 UI Track 7A S4-S11 (deferred) | §66 (gates S4 onward on fresh confirmation), §46-§62 | Strong backing for the deferred-state; §66 holds the gate. |
| Phase 7 UI Track 7B-7F | §46-§62 (17 UX decisions) | Strong backing. §50 (hybrid JE), §54 (reports), §55 (drill-down), §57 (CR ownership), §58 (sequential -- now §66-amended). |
| Phase 2+ Future Work | §12 (Doc AI -> Claude API), §15 (Power BI Phase 2), §17 (credit limits Phase 2), §51 (mobile out of scope v1, into Phase 2+) | Decision-backed. |

### Phases lacking decision backing

- **Phase 11/12+ "Remaining backend items"** -- by design, no specific decision; the placeholder framing is the decision.
- **Phase 8 email + Teams via Graph API** -- inherited from §53's "deferred to Phase 8+" without an explicit Phase-8-builds-this row. Recommend a future decision row when Phase 8 opens.

### Decisions that should appear in a phase but do not

- **§17 (credit limit management deferred to Phase 2)** -- referenced from `roadmap.md` Phase 2+ "Credit limit management and enforcement" item. Backed.
- **§22 (vendors / customers added as needed)** -- back-references Phase 7 (Backend Track) but the "as needed" semantics need tightening before AP design.
- **§55 (universal drill-down)** -- backend Phase 9 must produce a query path that supports row-level drill-down. No backend Phase 9 task currently captures this requirement.
- **§57 (no initial design sign-off)** -- referenced from 7B+ but not from Track B (S1-S3) where the operator brief talks about the Finance Lead's first encounter. §66 made this explicit; cleanup in §66 body language per A4 above.
- **§64 (ASCII-only PowerShell + UTF-8 BOM)** -- referenced in AGENTS.md, but no roadmap item; the rule is operational and ongoing.

---

## SECTION C -- Executive questionnaire status

`docs/memos/executive-questionnaire.md` is unanswered in-doc. Implicit answers exist in decisions or other docs for some items.

| Section | Has answer? | Where | Still relevant? | Propose |
|---|---|---|---|---|
| §1 Legal Entity Structure | **No** | -- | Yes -- blocks `rm_entity` real seeding (currently one placeholder "Default Operating Entity"). | **Keep + follow-up**. Finance Lead + executives; blocker for Phase 10 cutover. |
| §2 Currency | Implicit | §24 (no FX in v1; schema future-proofed) | Yes (need explicit confirmation) | **Keep**. Defer follow-up; risk is low. |
| §3 Approval Thresholds (3.1, 3.2, 3.3) | **No** | -- | Yes -- blocks ApprovalPolicy rows for Phase 8. | **Keep + follow-up**. Finance Lead-blocking. |
| §3.4 (all wires dual approval) | Implicit | sod-matrix.md (Wire Initiate vs Wire Approve), approval-policies.md row "Wire transfer / ALL wires" | Yes (confirm with the Finance Lead) | **Keep**. Low priority. |
| §3.5 (approval workflows for vendor / period / etc.) | Implicit | approval-policies.md, sod-matrix.md | Yes | **Keep**. Confirm with the Finance Lead at next session. |
| §4 Data Retention (4.1, 4.2, 4.3) | **No** | Default proposal: 7 years | Yes -- low risk but should be confirmed before audit cycle. | **Keep + follow-up**. |
| §5 External Audit (5.1, 5.2, 5.3) | **No** | -- | Yes -- affects audit defensibility narrative + Read-Only Auditor role activation. | **Keep + follow-up**. Significant. |
| §6 Banking and Payments (6.1, 6.2, 6.3, 6.4) | **No** | -- | Yes -- blocks Phase 8 NACHA decisions. | **Keep + follow-up**. Phase-8-blocker. |
| §7 Sales Tax (7.1, 7.2) | **No** | -- | Likely no impact for v1; need confirmation. | **Keep**. Low priority. |
| §8 Credit Management (8.1, 8.2, 8.3) | Implicit | §17 (deferred to Phase 2) | Yes | **Archive (mostly answered)** or **Keep + confirmation only**. |
| §9 Insurance and Compliance (9.1, 9.2, 9.3) | **No** | -- | Yes -- may bind us tighter (cyber insurance controls). | **Keep + follow-up**. |
| §10 Document AI (10.1, 10.2, 10.3) | Implicit | §12 (Claude API in Phase 2), president-memo.md | Yes (Phase 2 confirmation only) | **Archive (mostly answered)**. |
| §11 Chart of Accounts (11.1, 11.2, 11.3) | Implicit | §23 (pre-populate standard, Finance Lead reviews); Phase 4 has 54 rows live | Yes -- confirm Finance Lead owns COA review going forward. | **Keep + confirmation only**. |
| §12 Reporting Requirements (12.1, 12.2, 12.3) | **No** | -- | Yes -- affects Phase 7C report scope beyond the 9 listed in ui-sitemap.md. | **Keep + follow-up**. |
| §13 Project Sponsorship (13.1, 13.2, 13.3, 13.4) | Yes | §30 (Finance Lead role), §32 (President sponsor), repo README, AGENTS.md | Yes | **Archive**. |
| §14 Cutover Timing (14.1, 14.2, 14.3) | Implicit | §26 (fiscal period boundary, user-driven green light) | Yes -- confirm specific date. | **Keep + follow-up**. |
| §15 Macola Decommissioning (15.1, 15.2, 15.3) | **No** | -- | Yes | **Keep**. Lower priority. |
| §16 Lighthouse Alignment (16.1, 16.2) | Implicit | §32, president-memo.md | Yes | **Archive**. |

**Summary:**
- **Answered or implicitly answered:** §8 (credit), §10 (Doc AI), §13 (sponsorship), §16 (Lighthouse) -- propose archive.
- **Highest-priority follow-ups (block downstream phases):** §1 (entities, blocks Phase 10), §3 (thresholds, blocks Phase 8), §6 (banking, blocks Phase 8), §12 (reporting, expands Phase 7C scope).
- **Lower priority:** §2, §4, §5, §7, §9, §11, §14, §15.

Most useful concrete next step: schedule a single Finance Lead-facing conversation covering §1, §3, and §11 specifically -- those three unlock the most downstream work.

---

## SECTION D -- Risk register reconciliation

`docs/risk-register.md` is the live register. Each row reviewed against current state.

### Phase 7 UI risks (R-7-01 through R-7-05)

| ID | Risk | Current state | Mitigation status | Propose |
|---|---|---|---|---|
| R-7-01 | Strict sequential creates 7+ month UI invisibility | **Profile shifted** -- §66 makes UI shell visible earlier (parallel research items already done; S4 may move shell into PRI-Books-Dev). New risk shape per §66: "shell-without-data Finance Lead-side dissatisfaction." | Mitigation should reference §66 + shell-only demo framing. | **Maintain + update**. Rewrite the row to acknowledge §66; original 7-month timeline assumption is now provisional. |
| R-7-02 | Heavy CR volume Phase 7B first weeks | Unchanged. Still future. | Phase 7E burn-down still planned per §57. | **Maintain**. |
| R-7-03 | Universal drill-down complexity | Unchanged; deferred to 7C kickoff. | Unchanged. | **Maintain**. |
| R-7-04 | Hybrid JE entry as long pole | Unchanged. | Unchanged. | **Maintain**. |
| R-7-05 | Cutover slips because UI starts too late | Shifted with §66 -- parallel-track research closes some of the gap; full UI build (7B-7F) timing still unaddressed. | Mention §66 in the mitigation text. | **Maintain + update**. |

### Pre-existing architectural risks (R-A-01 through R-A-15)

| ID | Risk | Current state | Propose |
|---|---|---|---|
| R-A-01 | Auditor rejects custom system | Mitigation reinforced -- §65 live validation produced empirical evidence. | **Downgrade to Low**. Mitigation is now demonstrated, not just designed. |
| R-A-02 | Build timeline slips | Phase 6B closed on schedule; no slippage to date. | **Maintain**. |
| R-A-03 | Dataverse capacity costs higher than expected | No new data. | **Maintain** (monitor). |
| R-A-04 | Key-person dependency | Documentation continues to expand; AI-assisted handoff working as designed. | **Maintain**. |
| R-A-05 | Macola data quality | Not exercised. | **Maintain**. |
| R-A-06 | Period close logic bugs | Plugin not yet authored. | **Maintain**. |
| R-A-07 | AI-generated code introduces subtle bugs | 53 plugin tests passing; live validation in §65; hash layout pinned by 16 tests. | **Downgrade to Low**. Mitigation has teeth now. |
| R-A-08 | Hash chain corruption missed | Mitigation = nightly verification job (not yet built). 16 LedgerRowHasher tests pin the byte layout. Live re-hash of EntryIds 3-4 still pending per immutability-validation.md "still-pending follow-up checks." | **Maintain**. Until the nightly job is real, this risk stays. |
| R-A-09 | SoD bypassed | Persona role mapping deferred to 7B; enforcement-level checks live in plugin code. | **Maintain**. |
| R-A-10 | Cutover failure | Not exercised. | **Maintain**. |
| R-A-11 | Document AI accuracy insufficient | Phase 2 risk. | **Maintain**. |
| R-A-12 | Leahy ACH unavailable post-Macola | Mitigation: NACHA in v1 per §16. Not yet built. | **Maintain**. |
| R-A-13 | Change management not actually used | Not yet exercised. | **Maintain**. |
| R-A-14 | Finance Lead refuses ownership (once named) | Not surfaced. | **Maintain** (monitor). |
| R-A-15 | Leadership rescues Finance Lead | Not surfaced. | **Maintain** (monitor). |

### New risks not currently in the register

Per operator brief, add these:

| Proposed ID | Risk | Severity | Owner | Mitigation | Status |
|---|---|---|---|---|---|
| R-A-17 | "Saving in Progress" UI hiccup on long plugin posts (>5 s). Long-running PostOperation plugins cause the maker-portal save spinner to hang visibly even though the underlying transaction is succeeding. Confuses users; risks duplicate-submission. | Low | IT | Phase 7B form-level UX: add explicit Save state + disable Save button during post + show progress message. Until then, document in user-guides/* that the spinner is normal during JE posting. | **Open**. New 2026-05-21. |
| R-A-18 | Per-JE line numbering currently uses a global autonumber pattern (line numbers monotonic across all JEs); Option B plugin (per-JE local numbering) is backlogged. JE audit-trail report may surface "line 437 of JE-2026-001005" which is confusing. | Medium | IT | Build Option B plugin before JE UI lands in Phase 7B. Numbers reset per JE; existing data does not need backfill if the plugin handles transition by computing display number on read. | **Open**. New 2026-05-21. |
| R-A-19 | Form-level read-only fields are only enforced at the plugin (server) layer. UI users can edit `rm_postedby_user`, `rm_journalentrynumber`, totals in the maker portal even though the plugin rejects the save. Finance Lead-discovery risk during shell-only demo. | Medium | IT | Phase 7A S4+ form scripts to make these fields read-only client-side. Documented in §46-style decision row when Phase 7A continues. | **Open**. New 2026-05-21. |
| R-A-20 | PRT-vs-CI/CD divergence: a routine plugin code change made via PRT (instead of CI/CD) would not flow into source control, breaking the audit-defensibility narrative ("every change has a CR + a Git commit"). Phase 6B used PRT for first-time registration only; future routine changes must use CI/CD. | Medium | IT | AGENTS.md is the authoritative rule (CI/CD for routine, PRT only for first registration). Reinforce in plugin-registration.md ("Why this is a runbook and not part of CI" section). Add a row to the AGENTS.md "What NOT to Do" list. | **Open**. New 2026-05-21. |

Closed risks: R-A-16 stays closed.

---

## SECTION E -- Backlog consolidation

Consolidated from grep across `docs/`, runbook "TBD" notes, "(planned)" markers, deferred design decisions, and tonight's S1 carry-forwards.

| # | Item | Source | Suggested target phase | Priority |
|---|---|---|---|---|
| BL-01 | Approval threshold values $X / $Y / $Z | exec-questionnaire §3.1, §3.2, §3.3; approval-policies.md; sod-matrix.md "Pending: Threshold Values" | Before Phase 8 | High |
| BL-02 | rm_isintercompany flag on `rm_chartofaccount` | data-model.md, decision log §34 | Phase 8 / AR / AP onset | Medium |
| BL-03 | Memo-only JE lines (neither debit nor credit) | data-model.md | Phase 8+ | Low |
| BL-04 | rm_basecurrency, rm_isconsolidationtarget, rm_legalname on `rm_entity` | data-model.md | Phase 10 (real entity seeding) | Medium |
| BL-05 | rm_accountnumberingscheme table | data-model.md, decision §34 | When COA validation plugin lands | Low |
| BL-06 | `ApproveJournalEntryPlugin` | sod-matrix.md, security-model.md | Phase 7B / 8 (when AP/AR approvals land) | High |
| BL-07 | `ClosePeriodPlugin` + `ReopenPeriodPlugin` | immutability-design.md §C, §D | Phase 9 | High |
| BL-08 | `ApproveVendorBankChangePlugin` | sod-matrix.md, approval-policies.md | Phase 8 | High |
| BL-09 | `ApproveWirePlugin` | sod-matrix.md | Phase 8 | Medium |
| BL-10 | `ChangeRequestApprovalPlugin` | immutability-design.md §J | Phase 7D | Medium |
| BL-11 | V0004 migration: `audit.PeriodCloseAttestation` | immutability-design.md §D | Phase 9 | High |
| BL-12 | V0005 migration: `audit.AuditEvents` | immutability-design.md §F | Phase 8 / 9 | High |
| BL-13 | V0006 migration: `ReportSnapshots` | immutability-design.md §G | Phase 9 | High |
| BL-14 | `scripts/Rotate-DsbAppPassword.ps1` | key-vault-management.md "TBD" | Pre-cutover | Medium |
| BL-15 | `verify-integrity.ps1` (on-demand hash chain verification) | immutability-design.md §B | Phase 8 / 9 | Medium |
| BL-16 | Nightly hash chain verification job (SQL Agent or Azure Function) | immutability-design.md §B | Phase 9 | High |
| BL-17 | Re-hash EntryId 3 and EntryId 4 offline to verify post-write integrity | immutability-validation.md "still-pending follow-up checks" | Before Phase 7 UI shell demo | Medium |
| BL-18 | Re-run negative tests (UPDATE/DELETE as `dsb_app`) against newly-written EntryIds | immutability-validation.md | Before Phase 7 UI shell demo | Medium |
| BL-19 | POST branch test of `sync-sp-secret-to-dataverse.ps1` | immutability-validation.md (logged tonight) | Dedicated maintenance window | Low |
| BL-20 | `runbooks/disaster-recovery.md` | immutability-design.md §I, repo-structure.md | Pre-cutover | Medium |
| BL-21 | `runbooks/data-recovery.md` | immutability-design.md §I | Pre-cutover | Medium |
| BL-22 | `runbooks/change-management.md` | security-model.md, repo-structure.md | Pre-cutover | Medium |
| BL-23 | `runbooks/period-close.md` | repo-structure.md | Phase 9 | High |
| BL-24 | `runbooks/year-end-close.md` | repo-structure.md | Phase 10 | Medium |
| BL-25 | `user-guides/ap-clerk.md`, `ar-clerk.md`, `controller.md`, `admin.md` | repo-structure.md | Phase 7E (TalentLMS modules) | Medium |
| BL-26 | Innovation Team logo binary (source, package, place) | ui-styling.md O4, O5 | Phase 7A S4 | Medium |
| BL-27 | Status pill placement -- which forms / which views | ui-sitemap.md, ui-styling.md | Phase 7A S10 | Medium |
| BL-28 | Phase 7A S4 (app module + theme application) | ui-styling.md "Books Theme Record" section | Phase 7A S4 (gated by §66 reaffirmation) | High |
| BL-29 | Phase 7A S5-S10 (breadcrumb / recent / dashboard / patterns / toolchain / status pill) | ui-styling.md, ui-sitemap.md | Phase 7A | Medium |
| BL-30 | AAD-only auth for `priadmin` (remove SQL password as stealable artifact) | sql-account-management.md, immutability-validation.md | Pre-cutover | Medium |
| BL-31 | Quarterly access review owner + cadence + first review | sod-matrix.md, security-model.md, key-vault-management.md "Quarterly review checklist" | Phase 7B | Low |
| BL-32 | PIM workflow design (no standing admin access to PRI-Books) | security-model.md | Pre-cutover | Medium |
| BL-33 | Vendor banking field-level security | security-model.md (TBD design) | Phase 8 | High |
| BL-34 | Posted-ledger amounts in restricted accounts -- field-level security | security-model.md | Phase 8 (when approval-policies row exists) | Medium |
| BL-35 | Migrate plugin runtime credential to managed identity (when Dataverse supports) | credential-access-design.md, security-model.md | Long-term -- watch Microsoft roadmap | Low |
| BL-36 | Single rotation script that hits Vault + Dataverse env var + GitHub Actions secret in one pass | credential-access-design.md "Open items"; `sync-sp-secret-to-dataverse.ps1` covers Dataverse side only | Pre-cutover | Medium |
| BL-37 | Field-level security on `rm_entity.rm_ein` (column encryption, premium tier) | data-model.md `rm_entity` rationale, security-model.md | Before real EIN values are entered (so, Phase 10 entity seeding) | High |
| BL-38 | Phase 7-Backend vendor / customer integration design + ownership boundary doc | roadmap.md Track A, erp-pattern-notes.md §3 | Phase 7-Backend (current) | High |
| BL-39 | Documentation hygiene sweep: §63 propagation into credential-access-design.md, plugin-registration.md, key-vault-management.md (see Section F) | This audit | Next available session | Medium |
| BL-40 | seed-data.md update -- record the 54-row COA seed run + the "Applied environments" table | seed-data.md, Phase 4 closure | Next available session | Low |
| BL-41 | repo-structure.md branching strategy alignment with AGENTS.md solo-dev rule | This audit, A6 | Next available session | Low |
| BL-42 | phase-7-ui-design.md update for §66 + §49 amendment | This audit, A1 + A2 | Next available session | Medium |
| BL-43 | phase-7a-foundation-prompt.md amendment banner | This audit, A2 | Next available session | Low |
| BL-44 | risk-register.md update for R-7-01, R-7-05 amendment + new R-A-17 through R-A-20 | This audit, Section D | Next available session | Medium |
| BL-45 | Explicit decision row for Phase 8 email + Teams Graph API scope | This audit, Section B | Phase 8 kickoff | Low |
| BL-46 | Explicit confirmation row for §22 vendors -- ERP-sync vs Books-owned for Phase 8 | This audit, Section B | Phase 7-Backend | High |

Total: 46 backlog items. Roughly bucketed:

- **High priority (12):** BL-01, BL-06, BL-07, BL-08, BL-11, BL-12, BL-13, BL-16, BL-23, BL-28, BL-33, BL-37, BL-38, BL-46 (rounded). All unblock specific downstream phases.
- **Medium priority (~22):** mostly tooling, runbooks, secondary plugins, hygiene work.
- **Low priority (~12):** future / Phase 2+ / nice-to-haves.

---

## SECTION F -- Documentation hygiene

Items flagged for cleanup. Priority is the priority of the cleanup itself, not the underlying work.

### F1. Stale Secret-type env var references (multiple files)

| File | Lines | Issue | Fix |
|---|---|---|---|
| `docs/architecture/credential-access-design.md` | 11, 64-89, 167, 170 | Documents `rm_sqlkvclientsecret` as Secret type per §38. §63 changed this 2026-05-21. | Add amendment banner at top; flip the schema row to "String / Text per §63"; remove "Do not create as String type with the raw secret pasted" warning (now exactly the correct action). Keep the historical "Power Platform Key Vault integration prerequisites" subsection but mark it "Historical (no longer required for `rm_sqlkvclientsecret` per §63; retained for the prod managed-identity story when that lands)." |
| `docs/runbooks/plugin-registration.md` | 130-198 | "Phase 6B prerequisites -- Dataverse Environment Variables" walks operators through creating `rm_sqlkvclientsecret` as Secret type with KV firewall + dual-SP RBAC prerequisites. §63 made all of this obsolete for the env var. Verification block at line 195 flags `rm_sqlkvclientsecret [String]` as "a hard stop" -- which is now the correct state. | Replace the entire Secret-type subsection with a Text-type instruction set: create env var of type Text, populate via `scripts/sync-sp-secret-to-dataverse.ps1`, verify type is `[String]`. Keep the firewall + RBAC narrative but move to "Historical -- pre-§63" appendix or delete entirely since the new path does not need it. |
| `docs/runbooks/key-vault-management.md` | 50-54, 100-130, 297-302 | Documents the Secret-type integration pattern for Dataverse <-> KV; "Consumer fan-out reminder" line 297-302 says rm_sqlkvclientsecret "updates automatically when the Vault secret is updated" -- which §63 broke. The new sync script is the only writer. | Update the fan-out reminder to flag that #3 requires manual rerun of `sync-sp-secret-to-dataverse.ps1` after every Vault rotation. Mark the Dataverse RBAC integration section "Historical / not used by Books today" but keep -- still useful context if Microsoft ever fixes the prvReadEnvironmentVariableSecretValue sandbox issue. |

Combined cleanup is moderate -- 20-30 minute edit across three files. **Recommend bundling as a single doc-hygiene commit with title "Propagate §63 to runbooks + credential-access-design."**

### F2. Stale §49 / §58 references (Phase 7 UI docs)

| File | Issue | Fix |
|---|---|---|
| `docs/decisions/phase-7-ui-design.md` | Cross-reference table for §49 says "Match Datastream ERP color palette (blue) + corner logo; competitor finance UIs for pattern inspiration" -- no amendment note. Total timeline section says "Starts only after all backend phases complete" -- no §66 mention. | Add amendment note to §49 cross-reference. Add §66 reference to the Total timeline section. Add "Phase 7A S1-S3 ran in parallel with Backend Track per §66" line to the Phase 7A description. |
| `docs/runbooks/phase-7a-foundation-prompt.md` | Pre-flight checklist requires Backend Phase 8 / 9 / 10 / 11+ complete. None met for S1-S3. Pre-dates §66 entirely. | Add top-of-file banner: "AMENDED 2026-05-21 -- §66 unbundled S1-S3 from full Phase 7A; this prompt now describes the *eventual* full kickoff. S1-S3 ran in parallel." |
| `docs/risk-register.md` R-7-01, R-7-05 | Both describe the strict-sequential context; §66 risk profile shift not reflected. | Rewrite mitigation columns to reference §66 + shell-only demo framing. |

### F3. Branching policy contradiction

| File | Issue | Fix |
|---|---|---|
| `docs/repo-structure.md` "Branching Strategy" section | Lists `develop`, `feature/*`, `hotfix/*` branches as the workflow. Contradicts AGENTS.md "Branching Policy" (solo-dev, work directly on main, no branches, no worktrees). | Rewrite the Branching Strategy section to match AGENTS.md. Single paragraph. |

### F4. Stale "(to be implemented)" / "(planned)" markers

| File | Marker | Status |
|---|---|---|
| `immutability-design.md` line 152 | "PostJournalEntryPlugin ... (to be implemented)" | **Done.** Phase 6B closed. Flip to "implemented in `plugins/DatastreamBooks.Plugins/Posting/PostJournalEntryPlugin.cs`." |
| `immutability-design.md` line 136 | "nightly verification job (to be authored in V0003+)" | V0003 is done (SQL logins). The verification job is BL-16, still pending. Flip to "(to be authored as Phase 9 backend item BL-16; V0003 itself ships the SQL logins behind the plugin runtime credential)." |
| `immutability-design.md` lines 204, 208, 232, 242 | "(V0004 -- planned)", "(V0005 -- planned)", "(V0006 -- planned)" | All still legitimately pending -- BL-11, BL-12, BL-13. Leave or annotate with the BL IDs from this audit. |
| `immutability-design.md` line 254 "deploy-prod workflow (to be authored)" | Still pending. | Leave. |
| `immutability-design.md` lines 270-274 | `runbooks/disaster-recovery.md`, `runbooks/data-recovery.md`, `runbooks/change-management.md` "(to be authored)" | Still pending. BL-20, BL-21, BL-22. Leave. |
| `immutability-design.md` line 327-331 Open Items | Lists items, several done in Phase 6B closure. | Refresh the list to reflect Phase 6B closure -- some items can move to a "done" sub-list. |
| `security-model.md` line 167 | "PIM workflow design (TBD)" | Still pending. BL-32. Leave. |
| `seed-data.md` "Future seed data" | Lists rm_chartofaccount as Phase 4 future work; Phase 4 is complete with 54 rows. | Move that line to "Applied environments" history; add the 54-row PRI-Books-Dev seed event. |

### F5. Other minor staleness

- `docs/architecture/security-model.md` references "the Phase 7A security role scaffolding session" but §66 moved security role scaffolding to 7B (per ui-styling.md "scope" and tonight's roadmap update). Update the section.
- `docs/runbooks/plugin-registration.md` Step 7 example commit message says "Phase 6A" but Phase 6B closed; if the example is used verbatim now, the commit history would say "Phase 6A" for a 6B-era change. Update example to a generic placeholder like "Phase 6: register PostJournalEntryPlugin steps..."
- `docs/runbooks/plugin-registration.md` row 9 description "(Phase 6A placeholder; see source for current intent)" is incomplete. Either fill in or remove.

### F6. Broken / suspect internal links

None found in this pass. All `[…](…)` style links within `docs/` resolved against existing files (with the noted exceptions being TBD-marked future files like `runbooks/disaster-recovery.md`).

---

## SECTION G -- Open questions for tomorrow-morning Ryan

Listed in rough priority for overnight reflection. Items where rest is more valuable than tonight's debate.

### G1. §66 reaffirmation -- the central question for tomorrow

§66 is provisional through S3 only. Three honest paths after S3:

1. **Continue parallel through Phase 7A S4-S11** -- build app module + theme + sitemap + status pill in PRI-Books-Dev while Backend Track A continues. Highest velocity for visible artifacts. Strongest test of §66's "shell-only demo framing" mitigation for R-7-01.
2. **Return to §58 strict sequential** -- park S4 onward; resume only after Backend Phase 8+ complete. Lowest risk of "shell-without-data Finance Lead dissatisfaction." Slowest visible progress.
3. **Hybrid -- continue S4 (app module + theme) only; defer S5-S11** -- get the shell visible without committing to all of Phase 7A. Splits the difference. Adds one more "provisional commit" cycle in a few weeks when S5-S11 come up.

No new info expected overnight; this is a judgment call. Worth thinking about with daylight.

### G2. §63 propagation -- when does it land?

Three files describe the Secret-type pattern as canonical despite §63 (Section F1 above). Risk: future operator (or future Claude session) reads `plugin-registration.md` and tries to recreate `rm_sqlkvclientsecret` as Secret, hitting the same 0x80040256 wall §63 already solved. Question for tomorrow: do we sweep tonight's audit's F1 in the next session, or do we let it ride until a fresh Phase 6B-style rotation surfaces it?

Recommendation if asked: **sweep early**. The risk of a future operator falling into the same hole is real; the cost of fixing it now is small (one focused commit).

### G3. The exec-questionnaire crystallization moment

Section C of this audit flags four high-priority unanswered exec questions: §1 (entities), §3 (thresholds), §6 (banking), §12 (reporting). §1 and §3 are the only two that block specific upcoming work (Phase 10 cutover and Phase 8 approvals, respectively). Question: is there an opportunity to compress these into a single Finance Lead-facing conversation in the next 2 weeks? If yes, the rest of the questionnaire follows behind. If no, what is the asynchronous path -- does the Finance Lead fill in the markdown?

Only the Finance Lead (or executives) can answer §1 / §3 / §6 / §11 / §12 -- DO NOT propose answers. Flagging only.

### G4. Finance Lead first encounter timing -- the shell demo

§66 says the first Finance Lead encounter with the 7A shell must be framed "shell only, no transactions yet." Questions:
- When does that first encounter happen?
- Is it triggered by an explicit operator action (Ryan invites the Finance Lead) or implicit (the Finance Lead happens to log into PRI-Books-Dev)?
- What is the standing instruction to the Finance Lead between Phase 6B closure and the first shell encounter -- does the Finance Lead know to NOT log into PRI-Books-Dev yet?

These are not blockers for tomorrow morning but they shape the §57 vs §66 interplay (Section A4).

### G5. The R-A-19 risk -- form-level read-only fields

Per operator brief: "Form-level read-only fields not enforced in UI (only at plugin)." The Finance Lead, during the shell demo, may try to edit `rm_journalentrynumber` on JE-2026-001005 and see the field accept the keystroke (even though the eventual save would fail). That's confusing.

Question: is this fixable cheaply by adding maker-portal form-level read-only attributes to a few columns, without needing the 7B form-script work? It might be a one-evening task. Worth scoping before the shell demo, not after. (Note: this is a pre-S4 decision -- whether to slip in a quick read-only pass on the JE form during S4 or to leave for full 7B treatment.)

### G6. §22 vendors-as-needed -- specifics

§22 says "Vendors/customers added as needed" -- a "natural data hygiene" framing. Track A is currently scoped to customer cross-solution lookup to ERP. Vendor scope is ambiguous:
- Are vendors created fresh in Books each time a bill arrives (no ERP touch)?
- Or are vendors synced from somewhere?
- erp-pattern-notes.md §3 says "Books will likely need to own the vendor master" -- confirm.

This is a decision point that lands cleanly into a new decision row when Track A's design solidifies. Flagging here for tomorrow's planning so it does not get lost.

### G7. Innovation Team logo -- source and approval

ui-styling.md O4 reserves `rm_InnovationTeamLogo` web resource. No source yet. Question: where does the binary come from? Is it a Finance Lead-approved or executive-approved asset, or an IT-internal brand artifact? S4 needs the binary in hand.

---

## Audit close-out

- All seven sections complete.
- No fixes made -- this is a read-only audit.
- 46 backlog items consolidated; 4 new risks proposed; 6 doc-hygiene items flagged; 7 open questions surfaced for overnight rest.
- Estimated act-on time: half of the items in Section F could be folded into a single hygiene commit (~45-60 min next session). The exec-questionnaire follow-up is the only item that requires Finance Lead time and cannot be handled by Ryan + Claude alone.

**Recommended next-session sequencing (if operator wants to act on this audit immediately):**

1. G1 (§66 reaffirmation) -- decision call.
2. F1 (§63 propagation sweep) -- doc commit.
3. F2 + F3 + F5 (other staleness) -- doc commit.
4. risk-register update (D + new risks) -- doc commit.
5. seed-data.md history fill -- doc commit.
6. phase-7-ui-design.md update -- doc commit.

That entire stack is a single 60-90 minute focused doc session. Yields a fully reconciled documentation state going into Phase 7A S4 (or wherever G1 lands).

## See also

- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) -- live decision log
- [`../roadmap.md`](../roadmap.md) -- live phase state
- [`../risk-register.md`](../risk-register.md) -- live risk register
- [`../memos/executive-questionnaire.md`](../memos/executive-questionnaire.md) -- pending exec input
- [`../architecture/ui-styling.md`](../architecture/ui-styling.md) -- Phase 7A S2 artifact
- [`../architecture/ui-sitemap.md`](../architecture/ui-sitemap.md) -- Phase 7A S3 artifact
