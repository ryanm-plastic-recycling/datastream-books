# Datastream Books -- Executive Questionnaire

**Purpose:** Items requiring executive input before design can be finalized. Please respond inline where possible.

**Format note:** Reply inline under each question. If a question is not applicable or needs delegation, mark accordingly.

**Governance framing note (added 2026-05-22 per [§71](../decisions/datastream-books-decisions.md)):** Some items historically framed in this questionnaire as "for the Finance Lead to decide" are architectural decisions that IT owns under §71 (IT-decides / Finance Lead-consults / COO-concurs on cross-domain impact). The substantive content of those items is unchanged; the framing of *who authorizes the decision* is now explicit. The Finance Lead-consult portion (field lists, intake workflow, finance-domain detail) remains a Finance Lead item. Items affected by this reframing carry an inline note where it changes the response expected from the Finance Lead -- see §17 below for the worked example. The master decisions sheet [`./decisions-required-master-list.md`](./decisions-required-master-list.md) now categorizes every item as IT-decides / Finance Lead-decides / Exec-decides; consult it for the authoritative who-decides view.

**Status flag legend (added 2026-05-21 per audit):**

- **[Active]** -- still needs an answer; downstream work depends on it.
- **[Pending Finance Lead]** -- specific to the Finance Lead role; on the Finance Lead agenda (once named).
- **[Confirmed]** -- answered in a decision or other doc; retained for traceability with a citation.
- **[Archived]** -- moved to the "Archived Questions" section near the end of this doc.

---

## Recommended Finance Lead-facing conversation agenda (added 2026-05-21 per audit)

Three questions block the largest amount of downstream work and could be batched into a single 30-45 minute Finance Lead conversation (once named):

1. **§1 Legal Entity Structure** -- unblocks `rm_entity` real seeding; blocks Phase 10 cutover.
2. **§3 Approval Thresholds** -- unblocks `rm_approvalpolicy` row authoring; blocks Phase 8 approval workflows.
3. **§11 Chart of Accounts** -- confirms ownership of the 54-row seed already loaded in PRI-Books-Dev; blocks COA review acceptance.

Recommend scheduling this conversation after the Finance Lead is named by the CFO (Fred). §1 and §11 are also relevant to the Finance Lead framing -- both are deliverables where the Finance Lead's name lands on the artifact once named (per the Ownership Artifacts table in [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) "Finance Lead -- Role Definition" section).

Out-of-agenda follow-ups (kept separate so they do not bloat the core conversation):

- **§6 Banking and Payments** -- requires a banking-specific deep dive; blocks Phase 8 NACHA decisions but is not Finance Lead-only (involves IT + treasury function).
- **§12 Reporting Requirements** -- affects Phase 7C report scope; can be a written response since the question is descriptive ("what reports do we currently rely on").
- **§17 Vendor Master Scope** [Confirmed: §70 -- 2026-05-22] -- architectural decision resolved (Books is system of record; ERP receives Books-mastered field projection via plugin-driven push; ERP retains write authority on operations-only fields). Finance Lead-consult portion (field list, intake workflow, 1099 rules, approval routing) now lives in the Finance Lead conversation as a field-and-workflow item, not as an ownership decision. See §17 below for the decision summary and pointer to [§70](../decisions/datastream-books-decisions.md) + [`./decisions-required-master-list.md`](./decisions-required-master-list.md) entry #3.

---

## 1. Legal Entity Structure [Active] [Pending Finance Lead + Executives]

Datastream Books needs to be built with multi-entity support from day one. We need a clear picture of the legal structure.

**1.1.** Please list all legal entities under our corporate umbrella. For each, provide:
- Legal name
- EIN
- Fiscal year-end
- Entity type (operating company / real estate / holding / other)
- State of registration
- Currently a separate set of books in Macola? (yes / no)

**1.2.** Describe the parent/subsidiary relationships between entities.

**1.3.** Inter-company transactions -- how frequent are they between entities, and what types? (e.g., expense allocations, lease payments between operating and real estate entities, loans, capital transfers)

**1.4.** Do we need to produce consolidated financial statements (combining all entities and eliminating inter-company transactions), or do we report only on individual entity statements?

**1.5.** Is there an audit at the entity level, consolidated level, or none?

---

## 2. Currency [Active] [Pending Finance Lead]

**2.1.** Do we have any transactions, vendors, customers, or bank accounts in non-USD currency?

**2.2.** Do we anticipate any in the next 3-5 years?

**2.3.** *If yes to either:* Confirm whether foreign currency handling is required in v1 or can be deferred.

*Implicit answer pending confirmation:* §24 of the decision log defers FX to post-v1; v1 schema is USD-only with currency-aware columns retained for future extension.

---

## 3. Approval Thresholds [Active] [Pending Finance Lead]

We need dollar thresholds for system-enforced approvals.

**3.1.** Bills above what dollar amount require supervisor approval?

**3.2.** Journal entries above what dollar amount require a second reviewer?

**3.3.** Write-offs (bad debt, AP forgiveness) above what dollar amount require Controller approval?

**3.4.** Should ALL wire transfers require dual approval regardless of amount? (Recommended: yes)

**3.5.** Do we want approval workflows for:
- New vendor setup (recommended: yes -- fraud prevention)
- Vendor bank info changes (recommended: yes -- known fraud vector)
- Period reopening (recommended: yes -- Controller + executive)
- Manual JE posting to bank accounts (recommended: yes -- Controller)
- Recurring JE setup or modification (recommended: yes -- Controller)

---

## 4. Data Retention and Physical Documents [Active] [Pending Finance Lead]

**4.1.** Do we have an existing data retention policy? Default proposal is 7 years for tax records and supporting documents.

**4.2.** Do we have any requirement to retain physical paper documents going forward, or can we go fully digital (SharePoint)?

**4.3.** The physical banker boxes in our basement -- what is the plan for those? (Destroy after digitizing? Retain? Audit-driven decision?)

---

## 5. External Audit [Active] [Pending Finance Lead + Executives]

**5.1.** Do we engage an external auditor today? If yes, what is the scope (full audit / review / compilation / agreed-upon procedures)?

**5.2.** Do any of our customers, banks, or insurers require SOC 1 or SOC 2 reports?

**5.3.** Should we proactively engage our auditor before finalizing the Datastream Books control architecture, or wait until our next audit cycle?

---

## 6. Banking and Payments [Active] [Pending Finance Lead + Treasury]

The current Macola system uses an add-on service called "Unknown Leahy Product Name" for ACH payments. This service is tied to Macola's SQL Server and will not be available after Macola is retired. We need a replacement.

**6.1.** Which banks do we currently use? Please list all bank accounts by entity.

**6.2.** Do any of our banks offer direct API integration for ACH origination? (If yes, this is the cleanest path.)

**6.3.** Are we open to evaluating Bill.com, Ramp, or similar AP automation services for Phase 2?

**6.4.** For v1 (cutover), we propose generating NACHA-format ACH files in Datastream Books for manual upload to the bank portal. Acceptable?

---

## 7. Sales Tax [Active] [Pending Finance Lead]

**7.1.** Do we currently collect sales tax on any transactions?

**7.2.** If yes, how is it handled today, and does it need to be in Datastream Books v1?

---

## 8. Credit Management [Archived 2026-05-21]

Archived because §17 of the decision log explicitly defers credit limit management and enforcement to Phase 2. See "Archived Questions" section near the end of this doc for the full text.

---

## 9. Insurance and Compliance [Active] [Pending Finance Lead + IT]

**9.1.** Does our cyber insurance policy specify any controls or requirements for our financial systems?

**9.2.** Has our insurer been informed about the planned migration from Macola to Datastream Books?

**9.3.** Are there any other compliance regimes we need to consider (industry-specific, customer contract requirements)?

---

## 10. Document Processing AI [Archived 2026-05-21]

Archived because §12 of the decision log explicitly identifies Claude API as the Phase 2 document AI path, and the president-memo.md confirms the strategic value of AI-driven discrepancy detection. See "Archived Questions" section near the end of this doc for the full text.

---

## 11. Chart of Accounts [Active] [Pending Finance Lead]

**11.1.** Is the current Macola chart of accounts considered fit-for-purpose, or do we have known issues we want to fix during migration?

**11.2.** We propose pre-populating Datastream Books with a standard chart of accounts (covering operating companies and real estate entities) and having the finance team modify it. Acceptable approach?

*Implicit answer pending confirmation:* §23 of the decision log adopted the pre-populate approach; the 54-row standard COA was seeded into PRI-Books-Dev under "Default Operating Entity" during Phase 4 (2026-05-19). The Finance Lead's role (once named) is to review and approve.

**11.3.** Who from the finance team will own COA review?

*Implicit answer pending confirmation:* the Finance Lead, once named (per the Ownership Artifacts table in [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md)).

---

## 12. Reporting Requirements [Active] [Pending Finance Lead]

**12.1.** Beyond standard financial statements (Balance Sheet, P&L, Cash Flow, Trial Balance, Aging reports), are there specific reports the finance team or executives currently rely on that we need to replicate?

**12.2.** Who currently produces management reports (board reports, KPI dashboards, etc.) and how?

**12.3.** Are there reports we wish we had but don't, because Macola couldn't produce them?

---

## 13. Project Sponsorship and Resources [Archived 2026-05-21]

Archived because the Sponsorship and Owner roles are now confirmed in the decision log (§30 Finance Lead role definition, §32 President (Brandon) as Executive Sponsor) and documented in both README and AGENTS.md. See "Archived Questions" section near the end of this doc for the full text.

---

## 14. Cutover Timing [Active] [Pending Finance Lead + Executives]

**14.1.** What is the ideal target cutover date? (Recommendation: fiscal year-end for clean accounting break.)

**14.2.** What dates absolutely must be avoided (tax deadlines, audit periods, year-end close, peak business cycles)?

**14.3.** How long are we willing to run Macola and Datastream Books in parallel? (Recommendation: minimum one full close cycle, ideally two.)

*Implicit answer pending confirmation:* §26 of the decision log adopted the fiscal-period-boundary + user-driven green light pattern. Specific date is still open.

---

## 15. Macola Decommissioning [Active] [Pending Finance Lead + IT]

**15.1.** How long must we retain access to the Macola archive after cutover? (Default proposal: 7 years read-only, then archive to cold storage.)

**15.2.** Is anyone outside of accounting currently dependent on Macola data or reports? (Other departments, external accountants, etc.)

**15.3.** Are we aware of any Macola integrations or jobs that may break if Macola is unavailable? (Beyond Leahy, which we know about.)

---

## 16. Lighthouse Alignment [Archived 2026-05-21]

Archived because §32 of the decision log captures the cascading Lighthouse benefits (Brandon + Marco endorsed the framing during the executive working session per the decision log), and the president-memo.md "Secondary Strategic Benefit: The Lighthouse" section documents the downstream IT modernization scope. See "Archived Questions" section near the end of this doc for the full text.

---

## 17. Vendor Master Scope [Confirmed: §70 -- 2026-05-22] [Finance Lead-consult on field list and workflow]

**Decision summary (per [§70](../decisions/datastream-books-decisions.md)):** Books is the system of record for vendor and customer entity records. ERP receives a downstream projection of Books-mastered fields via plugin-driven push. ERP retains write authority on operations-only fields (site locations, transportation routing, operational status flags). Same record, two writers, field-level lanes -- not table-level read-only.

- **Books-mastered fields** (read-only in ERP after sync): legal name, EIN, tax classification, W-9 status, 1099 reportable flag, banking / NACHA details, payment terms, hold-payment flag, credit terms (customers), approval status, AP / AR routing.
- **ERP-mastered fields** (writable in ERP only): site locations and shipping points, transportation routing preferences, operational approval flags for PO eligibility, operational notes, preferred-vendor flags by product, operational status.

**Who decided and how (per [§71](../decisions/datastream-books-decisions.md)):** this is an IT architectural decision. The Finance Lead (once named) consults on the Books-mastered field list and new-vendor intake workflow. The COO (Marco) concurs on operations impact (the two dual-role operations users currently authorized to add vendors / customers on the operations side shift their "add new" pattern to Books going forward). Confirmation expected in the upcoming executive working session.

**What still needs Finance Lead input (consult, not authorize):**
- The exact field list above -- is anything missing? anything Books shouldn't own?
- New-vendor intake workflow design (who fills in what, in what order, with what approvals)
- 1099 rules (which vendors are 1099-reportable; default behavior; override path)
- Approval routing for new vendor setup -- which roles approve, single or dual approval

**Push pattern (deferred to Phase 8 scoping, not specified here):** plugin on Books vendor / customer entity Update message fires a downstream push to ERP. Async, retry on failure, reconcile via verification job. See backlog BL-52 (push plugin design), BL-53 (ERP-side write-permission lockdown), BL-54 (cutover-day reconciliation of existing ERP vendor / customer records with Books-migrated Macola data).

**Cross-references:**
- [§70](../decisions/datastream-books-decisions.md) -- full decision text with rationale
- [`./decisions-required-master-list.md`](./decisions-required-master-list.md) entry #3 -- the master decisions sheet view (IT-decides / Finance Lead-consult / COO-concur)
- [`./pam-conversation-prep-2026-05-w22.md`](./pam-conversation-prep-2026-05-w22.md) Item 1 -- the rewritten Finance Lead-consult agenda (field list, intake workflow, 1099 rules, approval routing)
- [`../architecture/erp-pattern-notes.md`](../architecture/erp-pattern-notes.md) §3 -- the `rm_customer` cross-solution pattern that the push plugin will inform / diverge from

**Original questions (retained for traceability):**

- **17.1 (original):** When a bill arrives from a new vendor, who creates the vendor record? *Resolved by §70: AP in Books. The dual-role operations users shift their pattern.*
- **17.2 (original):** For vendors that already exist in PRI-Datastream ERP, is the canonical record the ERP one or the Books one? *Resolved by §70: Books is canonical for Books-mastered fields; ERP retains write authority on operations-only fields on the same record. Cutover-day reconciliation of existing ERP records with Macola-migrated Books data is BL-54.*
- **17.3 (original):** What vendor master fields does Books require beyond ERP's existing schema? *Resolved by §70's Books-mastered field list above; specific column-level design is a Finance Lead-consult item ahead of Phase 8 AP scoping.*

---

## Archived Questions

Questions retained here for traceability after their substantive content has been answered elsewhere. Archive reason cited per question.

### §8 Credit Management [Archived 2026-05-21]

**Archive reason:** Resolved by §17 of the decision log (credit limit management and enforcement deferred to Phase 2; not in v1 scope). The Finance Lead may revisit at Phase 2 kickoff if customer credit risk becomes material.

Original questions:

**8.1.** Do we extend credit to customers (Net 30 / Net 60 / etc.)?

**8.2.** Do we currently manage customer credit limits in any system?

**8.3.** Is credit limit management and enforcement required in v1, or can it be deferred to Phase 2?

### §10 Document Processing AI [Archived 2026-05-21]

**Archive reason:** Resolved by §12 of the decision log (Claude API selected as Phase 2 document AI; AI Builder is interim v1 pattern). President-memo.md "The Strategic Value Driver: AI-Driven Document Processing" section documents the strategic case and the headcount-reallocation outcome. Executives have signed off on the direction; Finance Lead concurrence will follow naming.

Original questions:

**10.1.** Confirm that the strategic value of AI-driven invoice/PO discrepancy detection is recognized -- meaning, if AI can flag only the discrepancies, we can reallocate accounting headcount from manual validation to higher-value work.

**10.2.** Are we open to evaluating Anthropic Claude API (or similar) as a Phase 2 replacement for AI Builder, if cost and accuracy comparisons favor it?

**10.3.** Any concerns about sending document content to a third-party AI service for processing? (Note: Claude API offers zero data retention and enterprise data protections.)

### §13 Project Sponsorship and Resources [Archived 2026-05-21]

**Archive reason:** Sponsorship and ownership are confirmed:

- Executive Sponsor: President (Brandon) (§32)
- Finance Lead: TBD — to be named by CFO (Fred) post executive working session (§30, §32a)
- Strategic Lead / IT: Ryan McCauley (AGENTS.md, README.md)
- Budget envelope: implied by the cost comparison in president-memo.md ($130K-$255K 5-year total accepted vs $200K-$390K BC alternative)

Original questions:

**13.1.** Confirm executive sponsor for the Datastream Books project.

**13.2.** Confirm finance lead (the SME who will own requirements, testing, and signoff on the finance side).

**13.3.** Confirm IT lead (build and architecture owner).

**13.4.** Confirm acceptable internal budget envelope for build effort and ongoing operations.

### §16 Lighthouse Alignment [Archived 2026-05-21]

**Archive reason:** Lighthouse alignment is captured in §32 of the decision log and the president-memo.md "Secondary Strategic Benefit: The Lighthouse" section. President (Brandon) + COO (Marco) endorsed the framing during the executive working session (per the decision log Finance Lead section). Downstream parallel work streams (Entra migration, file server retirement) sequence after Macola cutover.

Original questions:

**16.1.** Confirm that these downstream benefits are recognized and in scope for the overall initiative.

**16.2.** Should we begin planning the parallel work streams now (Entra migration, file server retirement), or sequence after Macola cutover?

---

## Response

Please return this document with responses inline. For questions requiring delegation, please indicate who should answer and we will follow up directly.

For any question that cannot be answered immediately, please respond "TBD" and we will track for follow-up.

---

**Contact for clarification:** Strategic Lead
**Target response date:** [Date]
