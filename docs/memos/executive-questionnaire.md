# Datastream Books — Executive Questionnaire

**Purpose:** Items requiring executive input before design can be finalized. Please respond inline where possible.

**Format note:** Reply inline under each question. If a question is not applicable or needs delegation, mark accordingly.

---

## 1. Legal Entity Structure

Datastream Books needs to be built with multi-entity support from day one. We need a clear picture of the legal structure.

**1.1.** Please list all legal entities under our corporate umbrella. For each, provide:
- Legal name
- EIN
- Fiscal year-end
- Entity type (operating company / real estate / holding / other)
- State of registration
- Currently a separate set of books in Macola? (yes / no)

**1.2.** Describe the parent/subsidiary relationships between entities.

**1.3.** Inter-company transactions — how frequent are they between entities, and what types? (e.g., expense allocations, lease payments between operating and real estate entities, loans, capital transfers)

**1.4.** Do we need to produce consolidated financial statements (combining all entities and eliminating inter-company transactions), or do we report only on individual entity statements?

**1.5.** Is there an audit at the entity level, consolidated level, or none?

---

## 2. Currency

**2.1.** Do we have any transactions, vendors, customers, or bank accounts in non-USD currency?

**2.2.** Do we anticipate any in the next 3–5 years?

**2.3.** *If yes to either:* Confirm whether foreign currency handling is required in v1 or can be deferred.

---

## 3. Approval Thresholds

We need dollar thresholds for system-enforced approvals.

**3.1.** Bills above what dollar amount require supervisor approval?

**3.2.** Journal entries above what dollar amount require a second reviewer?

**3.3.** Write-offs (bad debt, AP forgiveness) above what dollar amount require Controller approval?

**3.4.** Should ALL wire transfers require dual approval regardless of amount? (Recommended: yes)

**3.5.** Do we want approval workflows for:
- New vendor setup (recommended: yes — fraud prevention)
- Vendor bank info changes (recommended: yes — known fraud vector)
- Period reopening (recommended: yes — Controller + executive)
- Manual JE posting to bank accounts (recommended: yes — Controller)
- Recurring JE setup or modification (recommended: yes — Controller)

---

## 4. Data Retention and Physical Documents

**4.1.** Do we have an existing data retention policy? Default proposal is 7 years for tax records and supporting documents.

**4.2.** Do we have any requirement to retain physical paper documents going forward, or can we go fully digital (SharePoint)?

**4.3.** The physical banker boxes in our basement — what is the plan for those? (Destroy after digitizing? Retain? Audit-driven decision?)

---

## 5. External Audit

**5.1.** Do we engage an external auditor today? If yes, what is the scope (full audit / review / compilation / agreed-upon procedures)?

**5.2.** Do any of our customers, banks, or insurers require SOC 1 or SOC 2 reports?

**5.3.** Should we proactively engage our auditor before finalizing the Datastream Books control architecture, or wait until our next audit cycle?

---

## 6. Banking and Payments

The current Macola system uses an add-on service called "Leahy" for ACH payments. This service is tied to Macola's SQL Server and will not be available after Macola is retired. We need a replacement.

**6.1.** Which banks do we currently use? Please list all bank accounts by entity.

**6.2.** Do any of our banks offer direct API integration for ACH origination? (If yes, this is the cleanest path.)

**6.3.** Are we open to evaluating Bill.com, Ramp, or similar AP automation services for Phase 2?

**6.4.** For v1 (cutover), we propose generating NACHA-format ACH files in Datastream Books for manual upload to the bank portal. Acceptable?

---

## 7. Sales Tax

**7.1.** Do we currently collect sales tax on any transactions?

**7.2.** If yes, how is it handled today, and does it need to be in Datastream Books v1?

---

## 8. Credit Management

**8.1.** Do we extend credit to customers (Net 30 / Net 60 / etc.)?

**8.2.** Do we currently manage customer credit limits in any system?

**8.3.** Is credit limit management and enforcement required in v1, or can it be deferred to Phase 2?

---

## 9. Insurance and Compliance

**9.1.** Does our cyber insurance policy specify any controls or requirements for our financial systems?

**9.2.** Has our insurer been informed about the planned migration from Macola to Datastream Books?

**9.3.** Are there any other compliance regimes we need to consider (industry-specific, customer contract requirements)?

---

## 10. Document Processing AI

We currently use Microsoft AI Builder in SharePoint to extract data from documents (invoices, POs, etc.). The accuracy is uneven, particularly for variable document formats.

**10.1.** Confirm that the strategic value of AI-driven invoice/PO discrepancy detection is recognized — meaning, if AI can flag only the discrepancies, we can reallocate accounting headcount from manual validation to higher-value work.

**10.2.** Are we open to evaluating Anthropic Claude API (or similar) as a Phase 2 replacement for AI Builder, if cost and accuracy comparisons favor it?

**10.3.** Any concerns about sending document content to a third-party AI service for processing? (Note: Claude API offers zero data retention and enterprise data protections.)

---

## 11. Chart of Accounts

**11.1.** Is the current Macola chart of accounts considered fit-for-purpose, or do we have known issues we want to fix during migration?

**11.2.** We propose pre-populating Datastream Books with a standard chart of accounts (covering operating companies and real estate entities) and having the finance team modify it. Acceptable approach?

**11.3.** Who from the finance team will own COA review?

---

## 12. Reporting Requirements

**12.1.** Beyond standard financial statements (Balance Sheet, P&L, Cash Flow, Trial Balance, Aging reports), are there specific reports the finance team or executives currently rely on that we need to replicate?

**12.2.** Who currently produces management reports (board reports, KPI dashboards, etc.) and how?

**12.3.** Are there reports we wish we had but don't, because Macola couldn't produce them?

---

## 13. Project Sponsorship and Resources

**13.1.** Confirm executive sponsor for the Datastream Books project.

**13.2.** Confirm finance lead (the SME who will own requirements, testing, and signoff on the finance side).

**13.3.** Confirm IT lead (build and architecture owner).

**13.4.** Confirm acceptable internal budget envelope for build effort and ongoing operations.

---

## 14. Cutover Timing

**14.1.** What is the ideal target cutover date? (Recommendation: fiscal year-end for clean accounting break.)

**14.2.** What dates absolutely must be avoided (tax deadlines, audit periods, year-end close, peak business cycles)?

**14.3.** How long are we willing to run Macola and Datastream Books in parallel? (Recommendation: minimum one full close cycle, ideally two.)

---

## 15. Macola Decommissioning

**15.1.** How long must we retain access to the Macola archive after cutover? (Default proposal: 7 years read-only, then archive to cold storage.)

**15.2.** Is anyone outside of accounting currently dependent on Macola data or reports? (Other departments, external accountants, etc.)

**15.3.** Are we aware of any Macola integrations or jobs that may break if Macola is unavailable? (Beyond Leahy, which we know about.)

---

## 16. Lighthouse Alignment

The successful cutover of Datastream Books enables broader IT modernization aligned with The Lighthouse strategy:

- Decommission of local domain controller (full Entra ID migration)
- Decommission of local file server (all documents in SharePoint)
- Reduction of on-premise infrastructure
- Users no longer required to be physically present at HQ to access systems

**16.1.** Confirm that these downstream benefits are recognized and in scope for the overall initiative.

**16.2.** Should we begin planning the parallel work streams now (Entra migration, file server retirement), or sequence after Macola cutover?

---

## Response

Please return this document with responses inline. For questions requiring delegation, please indicate who should answer and we will follow up directly.

For any question that cannot be answered immediately, please respond "TBD" and we will track for follow-up.

---

**Contact for clarification:** [Your Name]
**Target response date:** [Date]
