# Datastream Books -- Decisions Required to Complete the Project

> **Purpose:** Master list of decisions that must be made before Datastream Books can complete. Each decision unlocks a phase of the build. Most are decided once; a few get refined over time.
>
> **Who decides each item (per [§71](../decisions/datastream-books-decisions.md) + [§72](../decisions/datastream-books-decisions.md)):**
> - **Technical Strategic Lead-decides (architectural):** cross-system data flow, schema ownership, integration patterns, system-of-record boundaries. The Finance Lead consults; COO concurs on cross-domain impact. The Technical Strategic Lead role (Ryan M) is defined in §72 -- a combined engineering + strategy + maintenance function, distinct from a traditional IT support role.
> - **Finance Lead-decides (operational):** finance / accounting workflows, field lists, approval rules, COA, reporting, period-close mechanics.
> - **Exec-decides (strategic):** scope, budget, timeline, external audit engagement, compliance.
>
> **How this gets used:** Reference this sheet before any Finance Lead or executive meeting. Mark items `[Confirmed: §XX]` once decided. Items not yet decided remain in [`./executive-questionnaire.md`](./executive-questionnaire.md) and [`../backlog.md`](../backlog.md).

---

## Section 1 -- Decisions the Technical Strategic Lead Owns (Architectural)

Per [§71](../decisions/datastream-books-decisions.md) + [§72](../decisions/datastream-books-decisions.md): the Technical Strategic Lead proposes, The Finance Lead consults on finance-domain detail, COO concurs on cross-domain impact. These are not Finance Lead-authorize items even when finance touches them. Stakeholders raise concerns about pending decisions in this section through the concurrence log ([`../decisions/concurrence-log.md`](../decisions/concurrence-log.md)) within a 5-business-day window before each decision locks.

| # | Decision | What the Technical Strategic Lead proposes / has decided | Who consults / concurs | Unlocks |
|---|---|---|---|---|
| 3 | **Vendor / customer master ownership architecture** [Confirmed: §70 -- 2026-05-22; concurrence window closes 2026-05-29] | Books is system of record for vendor / customer entity records. ERP receives a downstream projection of Books-mastered fields via plugin-driven push. ERP retains write authority on operations-only fields (site locations, transportation, operational flags). Same record, two writers, field-level lanes -- not table-level read-only. See §70 for the field list. | The Finance Lead consults on Books-mastered field list and new-vendor intake workflow. COO concurs on operations impact (the two dual-role operations users shift their "add new" pattern to Books). Confirmation expected in the upcoming exec rollout meeting. | Backend Track A (vendor / customer integration), Phase 8 (AP core), 1099 reporting |

---

## Section 2 -- Decisions the Finance Lead Owns (Finance / Accounting)

Operational details of the finance system: workflows, field lists, approval rules, COA, reporting, period-close mechanics. The Finance Lead authorizes; IT supports.

| # | Decision | What we need from the Finance Lead | Unlocks |
|---|---|---|---|
| 1 | **Legal entity structure** | Full list of legal entities with EINs, fiscal year-ends, parent/sub relationships, and whether we need consolidated financial statements or only per-entity. | Multi-entity ledger seeding (Phase 10) |
| 2 | **Chart of Accounts** | Review and sign off on the standard COA loaded in dev. Add/modify/remove accounts as needed. Confirm ongoing ownership. | GL posting, AP/AR account routing, all reporting |
| 4 | **Approval thresholds** | Dollar amounts for: bills requiring supervisor approval; JEs requiring second reviewer; write-offs requiring controller approval. | Approval workflows; AP/AR processing |
| 5 | **Approval scope confirmations** | Confirm dual-approval on all wires; approval required on new vendor setup, vendor bank info changes, period reopening, manual JE to bank accounts, recurring JE setup. | Same as above |
| 6 | **Period close process** | Confirm month-end and year-end close steps as the Finance Lead runs them today. Who closes? What gets locked? What reports get pinned? | Period locks; close runbook; year-end close runbook |
| 7 | **Bank list and ACH approach** | List of bank accounts by entity. Whether banks offer ACH API integration. Confirm NACHA file generation in v1 is acceptable. | AP payment execution; bank reconciliation |
| 8 | **Reports that must exist at cutover** | Beyond standard financial statements, which Macola reports do users rely on daily/weekly? Which reports do they wish they had but can't get from Macola? | Reporting build prioritization |
| 9 | **Macola pain points to fix** | What's broken or annoying in current Macola accounting workflows that we should explicitly solve in Books? | Workflow design choices; phase prioritization |
| 10 | **Macola archive scope** | What Macola data must remain accessible post-cutover, for how long, and to whom? | Archive design; Macola decommission plan |

---

## Section 3 -- Decisions the Executive Team Owns (Strategy / Scope / Budget)

Strategic scope, budget, timeline, external audit engagement, compliance. Exec authorizes; IT and the Finance Lead inform.

| # | Decision | What we need from execs | Unlocks |
|---|---|---|---|
| 11 | **Cutover target date** | Specific fiscal period boundary for go-live. Dates to avoid (tax deadlines, audit, peak business). | Project schedule; parallel run window |
| 12 | **Parallel run duration** | How many close cycles do we run Macola and Books in parallel? (Recommended: minimum one, ideally two.) | Cutover planning; resource planning |
| 13 | **Budget envelope confirmation** | Confirm total build budget and ongoing operations budget for next 5 years. | Build scope discipline; scope creep prevention |
| 14 | **External auditor engagement** | Engage auditor now to review control architecture, or wait until next regular audit cycle? | Audit readiness work; control documentation timing |
| 15 | **AI document processing commitment (Phase 2)** | Confirm strategic value of AI-driven invoice/PO discrepancy detection for headcount reallocation. Approve evaluation of Claude API as the document AI engine. | Phase 2 design; headcount-reallocation business case |
| 16 | **Lighthouse alignment confirmation** | Confirm post-cutover IT modernization is in scope (local domain controller decommission, file server decommission, full Entra ID migration). | Adjacent IT work streams |
| 17 | **Customer-credit handling scope** | Confirm credit limit management is deferred to Phase 2 (or pull it into v1 if executive priority). | AR scope; customer master design |
| 18 | **Sales tax handling scope** | Confirm whether we collect sales tax on any transactions. If yes, scope for v1 vs Phase 2. | AR scope; tax engine decisions |
| 19 | **Project sponsorship + ownership confirmation** | Confirm President (Brandon) as sponsor, CFO (Fred) as Finance Lead namer, the Finance Lead once named, Strategic Lead. Confirm escalation handling (reinforce ownership, not rescue). | Engagement model; CR triage authority |
| 20 | **Insurance/compliance review** | Confirm cyber insurance policy requirements and whether insurer has been informed of migration. Confirm any industry-specific compliance regimes. | Control architecture confirmation; risk register |

---

## Section 4 -- Decisions That Are Mostly Settled But Need One-Time Confirmation

These have working answers in the decision log; just need explicit sign-off rather than fresh debate.

| # | Decision | Working answer | Sign-off needed from |
|---|---|---|---|
| 21 | **Foreign currency** | Not in v1; schema future-proofed. | Finance Lead |
| 22 | **Payroll** | Permanently out of scope (Paylocity stays). | Executive (one-time confirm) |
| 23 | **Data retention policy** | 7 years default for tax records and supporting documents. | Finance Lead |
| 24 | **Physical document policy** | Going fully digital (SharePoint) post-cutover. | Finance Lead |
| 25 | **Training approach** | TalentLMS + Scribe (existing pattern). | Finance Lead |

---

## How To Use This Sheet

**Categorization rule (per [§71](../decisions/datastream-books-decisions.md) + [§72](../decisions/datastream-books-decisions.md)):** Architectural items live in Section 1 (Technical Strategic Lead-decides). Operational finance items live in Section 2 (Finance Lead-decides). Strategic / scope / budget items live in Section 3 (Exec-decides). Settled items live in Section 4. When a new decision arises, classify before adding -- the classification determines which conversation owns the answer.

**Before any Finance Lead meeting:** confirm which Finance Lead-owned items (Section 2) are on the agenda. Architectural items from Section 1 may also appear, framed as "here is what we are proposing; what operational details do you own?" -- not as "you decide who owns vendor records." The upcoming conversation covers #1, #2, #4, and the Finance Lead-consult portion of #3 -- the architectural decision on #3 is already made.

**Before any executive meeting:** confirm which Section 3 items are open. Some can be batched (#11, #12, #13 fit naturally in one conversation). Section 1 architectural items get COO concurrence in the same conversation -- not authorization, concurrence.

**Decision close-out discipline:** every answered item becomes a numbered decision in [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) within 48 hours of the conversation. No decision lives in someone's head.

**Raising concerns about a Section 1 (architectural) decision:** Section 1 items are decided by the Technical Strategic Lead under [§71](../decisions/datastream-books-decisions.md) authority. Stakeholders (Finance Lead, COO/Marco, President/Brandon, CFO/Fred) raise concerns through [`../decisions/concurrence-log.md`](../decisions/concurrence-log.md) -- each architectural decision opens with a 5-business-day concern window before it locks. Concerns can be raised verbally, by email, or in the upcoming exec rollout meeting; the Technical Strategic Lead captures them in the concurrence log and either resolves them in-place or opens a new formal decision that supersedes the original. This is the structural channel for cross-domain concurrence; ad-hoc objection in Slack or hallway conversation is welcome but should be captured in the concurrence log so the audit trail is complete.

**Status tracking:** items not yet decided remain in [`./executive-questionnaire.md`](./executive-questionnaire.md) and [`../backlog.md`](../backlog.md). Items decided get marked `[Confirmed: §XX]` with citation. Section 1 items additionally carry a concurrence-window close date; review [`../decisions/concurrence-log.md`](../decisions/concurrence-log.md) for the active window list and locked-entry archive.

---

## How This Gets Us to 80%

Of the work remaining in Datastream Books:

- **Items 1, 2, 4, 5 + the Finance Lead-consult on item 3** unlock Backend Track A and Phase 8 (AP + AR core) -- about 30% of remaining build
- **Items 6-10** unlock Phase 9 (period close, reporting, payment execution) -- about 20% of remaining build
- **Items 11-13** lock the schedule and budget envelope -- without these, every other decision floats
- **Items 14-20** resolve exec-level scope questions that otherwise come back as mid-project surprises

If items 1-13 are answered cleanly, the build path is fully scoped through cutover. The remaining 20% is execution variance and Phase 2 AI work -- neither of which is gated on a single decision.

---

## What's *Not* On This List (And Why)

- **Day-to-day workflow details** -- these get decided in build sessions with screen mockups in front of the Finance Lead, not in strategic conversations
- **Lower-level technical architecture choices** -- the Technical Strategic Lead owns these too, but they don't rise to the "decisions sheet" level (data model, plugin internals, SQL DDL, security role internals). The architectural items that *do* land here are the ones where another stakeholder needs to be informed or concur (e.g., vendor master ownership affects operations and finance both -- so it lands in Section 1 with COO concurrence and Finance Lead consult).
- **Specific column lists, field placements, report layouts** -- these emerge from user testing, not advance planning
- **Phase 2 detailed scope** -- by design, deferred until v1 stabilizes

The list above is *strategic* decisions only. Tactical decisions are intentionally absent -- they happen in the build, not in meetings.

---

## Status Tracking Table

> Update this table as decisions are made. Format: `[Open]` / `[Pending: <person/meeting>]` / `[Confirmed: §XX <date>]`

| # | Decision | Owner | Status |
|---|---|---|---|
| 3 | Vendor / customer master ownership architecture | Technical Strategic Lead (Finance Lead consult, COO concur) | **[Confirmed: §70 -- 2026-05-22; concurrence window closes 2026-05-29]** |
| -- | Governance principle (Technical Strategic Lead-decides / Finance Lead-decides / Exec-decides) | Technical Strategic Lead | **[Confirmed: §71 -- 2026-05-22; concurrence window closes 2026-05-29]** |
| -- | Technical Strategic Lead role definition (Ryan M) | Technical Strategic Lead (President-reporting) | **[Confirmed: §72 -- 2026-05-22; concurrence window closes 2026-05-29]** |
| 1 | Legal entity structure | Finance Lead (+ exec confirmation) | [Pending: Finance Lead, week of 2026-05-25] |
| 2 | Chart of Accounts | Finance Lead | [Pending: Finance Lead, week of 2026-05-25] |
| 4 | Approval thresholds | Finance Lead | [Pending: Finance Lead, week of 2026-05-25] |
| 5 | Approval scope confirmations | Finance Lead | [Open] |
| 6 | Period close process | Finance Lead | [Open] |
| 7 | Bank list and ACH approach | Finance Lead | [Open] |
| 8 | Reports that must exist at cutover | Finance Lead | [Open] |
| 9 | Macola pain points to fix | Finance Lead | [Open] |
| 10 | Macola archive scope | Finance Lead | [Open] |
| 11 | Cutover target date | Exec | [Open -- exec questionnaire §14] |
| 12 | Parallel run duration | Exec | [Open -- exec questionnaire §14] |
| 13 | Budget envelope confirmation | Exec | [Open -- exec questionnaire §13] |
| 14 | External auditor engagement | Exec | [Open -- exec questionnaire §5] |
| 15 | AI document processing commitment | Exec | [Open -- exec questionnaire §10] |
| 16 | Lighthouse alignment confirmation | Exec | [Open -- exec questionnaire §16] |
| 17 | Customer-credit handling scope | Exec | [Open -- exec questionnaire §8] |
| 18 | Sales tax handling scope | Exec | [Open -- exec questionnaire §7] |
| 19 | Project sponsorship + ownership confirmation | Exec | [Confirmed: §30 -- Finance Lead role; exec confirmation pending] |
| 20 | Insurance/compliance review | Exec | [Open -- exec questionnaire §9] |
| 21 | Foreign currency | Finance Lead | [Confirmed: §24 -- not in v1] |
| 22 | Payroll | Exec (one-time confirm) | [Confirmed: §6 -- permanently out of scope] |
| 23 | Data retention policy | Finance Lead | [Pending: Finance Lead] |
| 24 | Physical document policy | Finance Lead | [Pending: Finance Lead] |
| 25 | Training approach | Finance Lead | [Confirmed: §27] |

---

*Last updated: 2026-05-22 (afternoon). Restructured per [§71](../decisions/datastream-books-decisions.md) governance principle (Technical Strategic Lead-decides / Finance Lead-decides / Exec-decides categorization; original phrasing was "IT-decides", updated to "Technical Strategic Lead-decides" in the afternoon session per [§72](../decisions/datastream-books-decisions.md) role definition). Entry #3 moved from Finance Lead-decides to Technical Strategic Lead-decides and marked confirmed per [§70](../decisions/datastream-books-decisions.md). §72 role definition added to the Status Tracking Table with a concurrence-window close of 2026-05-29.*
