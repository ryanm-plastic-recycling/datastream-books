# Finance Lead Conversation Prep -- Week of 2026-05-25

> Consolidated agenda for the upcoming Datastream Books conversation
> with the Finance Lead. Drafted 2026-05-22 per audit Section C recommendation and
> decision §67's call for §17 to land alongside the existing §1 / §3
> / §11 agenda. Revised 2026-05-22 in the §70 / §71 session: Item 1
> rewritten from "decide who owns vendor records" to "consult on field
> list, intake workflow, 1099 rules, approval routing" after the
> architectural decision was made under §70. One-page format:
> pre-read, agenda, what we need, what she's signing off on, what
> she should bring.

## Meeting metadata

- **Audience:** the Finance Lead (once named) + Ryan (IT)
- **Duration:** 42 minutes (30 minutes minimum; 50 if Item 3 entity
  list discussion runs long). Item 1 trimmed from ~10 min to ~7 min
  after §70 closed the architectural question.
- **Format:** In person if possible; Teams as fallback
- **Decision authority (per [§71](../decisions/datastream-books-decisions.md)):**
  The Finance Lead authorizes operational finance answers (workflows, field lists,
  thresholds, COA, period close mechanics). IT authorizes architecture
  (cross-system data flow, schema ownership, integration patterns,
  system-of-record boundaries). The Finance Lead consults on architectural items
  that touch finance; COO concurs on cross-domain impact. Item 1 of
  this agenda is now a Finance Lead-consult item (architecture decided under
  §70); Items 2-4 remain Finance Lead-authorize.

## Pre-read (please review before the meeting)

1. The standard 54-row Chart of Accounts already loaded in
   PRI-Books-Dev under "Default Operating Entity"
   ([`../architecture/data-model.md`](../architecture/data-model.md)
   §`rm_chartofaccount` lists the structure). The Finance Lead should be ready
   to react to it as a draft.
2. [`./executive-questionnaire.md`](./executive-questionnaire.md)
   §1, §3, §11, §17 -- the four agenda items below. Each section's
   sub-questions are reproduced inline here, but the questionnaire
   has the full context.

## What to bring

- A list (informal is fine) of all legal entities with their EINs,
  fiscal year-ends, and Macola separation status. We will turn this
  into the `rm_entity` seed.
- The approval thresholds you would set today if asked -- not the
  perfect answer, the working answer. We can refine later.
- Any pain points with the current Macola chart of accounts that
  should be fixed during migration.
- A working opinion on the Books-mastered field list for vendors and
  customers (per §70 the AP team owns new vendor creation in Books;
  the operations team retains write authority on operations-only
  fields on the same record -- the question for the Finance Lead is whether the
  field list is correct and complete).
- A working opinion on the new-vendor intake workflow (who fills in
  what, in what order, with what approvals).
- A working opinion on 1099 rules (default behavior, override path,
  which vendor types are always-1099 vs never-1099).

## Agenda

### Item 1 -- §17 Vendor Master Scope -- Finance Lead-consult portion (~7 min)

> **Architectural decision made under [§70](../decisions/datastream-books-decisions.md)
> on 2026-05-22 -- Books is system of record for vendor and customer
> entity records, ERP receives a downstream projection of
> Books-mastered fields via plugin-driven push, and ERP retains write
> authority on operations-only fields (site locations, transportation,
> operational flags). Same record, two writers, field-level lanes.**
> This Item 1 is the Finance Lead-consult portion: confirm the field list and
> nail down the operational details that IT needs from finance before
> Phase 8 AP scoping can finalize.

- **Architectural decision summary (for awareness; not asking for
  re-litigation):**
  - **Books-mastered fields** (read-only in ERP after sync): legal
    name, EIN, tax classification, W-9 status, 1099 reportable
    flag, banking / NACHA details, payment terms, hold-payment flag,
    credit terms (customers), approval status, AP / AR routing.
  - **ERP-mastered fields** (writable in ERP only): site locations
    and shipping points, transportation routing preferences,
    operational approval flags for PO eligibility, operational
    notes, preferred-vendor flags by product, operational status.
  - The two dual-role operations users who currently add vendors /
    customers on the ERP side shift their "add new" pattern to
    Books going forward.
- **What we need from you (consult, not authorize):**
  1. Is the Books-mastered field list above correct and complete?
     Anything missing? Anything Books shouldn't own?
  2. New-vendor intake workflow -- who fills in what, in what
     order, with what approvals? (AP clerk drafts, AP manager
     reviews, Controller approves? Or single-step for low-risk
     vendor types?)
  3. 1099 rules -- which vendor types are always 1099-reportable
     (LLC, sole prop, etc.); which are never (corporation);
     override path; default behavior when type is unclear.
  4. Approval routing for new vendor setup -- single or dual
     approval, what roles, what dollar / risk threshold (if any)
     for dual approval.
- **What we're proposing you sign off on (Finance Lead-consult outputs, not
  architecture):** the Books-mastered field list (confirmed or
  amended), the new-vendor intake workflow, the 1099 rules, the
  approval-routing rules for new vendor setup. IT will roll these
  into the Phase 8 AP scoping artifacts. The *architectural*
  ownership question is closed under §70 and is not a sign-off item
  here.
- **What is downstream of this:** Backend Track A
  (vendor/customer integration) unblocks on the field-list +
  intake-workflow input above, *not* on a fundamental ownership
  decision (that's already made). Phase 8 (AP Core) scoping
  finalizes once 1099 rules and approval routing are nailed down.
  The push plugin design itself (Books -> ERP) is a Phase 8 or
  earlier scoping item -- not asking for Finance Lead input on the plugin
  internals.

### Item 2 -- §3 Approval Thresholds (~10 min)

> Finance Lead-owned thresholds. We need working numbers, not perfect ones.
> They will be configurable and adjustable; the question is what to
> start with.

- **What we need from you:**
  1. Bills above what dollar amount require supervisor approval?
  2. Journal entries above what dollar amount require a second
     reviewer?
  3. Write-offs (bad debt, AP forgiveness) above what dollar amount
     require Controller approval?
  4. Wire transfers -- all dual-approval regardless of amount?
     (Our recommendation: yes.)
  5. Approval workflows for new vendor setup, vendor bank info
     changes, period reopening, manual JE to bank accounts,
     recurring JE setup -- all recommended yes.
- **What we're proposing you sign off on:** four dollar amounts
  ($X bills, $Y JEs, $Z write-offs, plus the recommended yes
  answers) which will be loaded into the `rm_approvalpolicy` table.
- **What is downstream of this:** Phase 8 approval workflows.

### Item 3 -- §1 Legal Entity Structure (~15 min)

> The most data-heavy item. We need the full picture of legal
> entities before we can seed real entity records and before
> Phase 10 cutover can proceed.

- **What we need from you:**
  1. The full list of legal entities, each with legal name, EIN,
     fiscal year-end, entity type (operating / real estate /
     holding), state of registration, and Macola separation status.
  2. Parent / subsidiary relationships.
  3. Inter-company transaction patterns and frequency.
  4. Consolidated financial statements -- required or per-entity
     only?
  5. Audit scope -- entity-level, consolidated, or none?
- **What we're proposing you sign off on:** the `rm_entity` seed
  list and the consolidation policy. IT will package these into
  a seed script committed to the repo.
- **What is downstream of this:** real entity seeding (currently
  a placeholder "Default Operating Entity" row), Phase 10 cutover,
  and inter-company JE plugin design.

### Item 4 -- §11 Chart of Accounts (~10 min)

> The 54-row standard COA is already loaded in PRI-Books-Dev. Your
> role is to react to it as a draft -- accept, modify, or reject
> per account. Marking up a printout (or a marked-up Excel export
> we can prepare for you) is the fastest path.

- **What we need from you:**
  1. Confirm the pre-populate approach is acceptable
     (recommendation per decision §23: yes; you'd react to a draft
     rather than build from scratch).
  2. Identify accounts to add, modify, or remove from the standard
     54-row seed.
  3. Confirm you own COA review going forward (per the Ownership
     Artifacts table).
- **What we're proposing you sign off on:** the final COA for the
  Default Operating Entity, and your ownership of per-entity COA
  customizations that follow.
- **What is downstream of this:** GL design completion and AR / AP
  posting paths that depend on stable account codes.

## After the meeting

IT will, within 48 hours:

- Write a decision-log row for each item you signed off on
  (anticipated §72+ approval thresholds, §73+ entity structure if
  substantively different from current placeholder, §74+ COA
  sign-off, plus a §-numbered row for the Item 1 Finance Lead-consult
  outputs -- field list, intake workflow, 1099 rules, approval
  routing for new vendor setup -- captured as one row separate
  from §70's architectural decision). Decision numbering will pick
  up from the latest committed row in the decision log; §70 / §71
  occupy the next two slots.
- Update the [`./executive-questionnaire.md`](./executive-questionnaire.md)
  status flags from `[Active] [Pending Finance Lead]` to `[Confirmed]` with
  citations.
- Mark the corresponding backlog items (BL-01, BL-04, BL-37, BL-38,
  BL-46) as actionable for the next IT-side work session. BL-46
  (explicit decision row for §22 vendors) is already closed by §70
  -- update its status accordingly during the same backlog pass.

If anything is left unresolved at the end of the conversation, IT
will track it in [`../backlog.md`](../backlog.md) under a new BL-id
and flag it for the next conversation rather than letting it drift.

## Linked artifacts

- [`./executive-questionnaire.md`](./executive-questionnaire.md) -- §1, §3, §11, §17 in full (§17 now confirmed under §70; consult content retained for traceability)
- [`./decisions-required-master-list.md`](./decisions-required-master-list.md) -- master decisions sheet (IT-decides / Finance Lead-decides / Exec-decides categorization per §71)
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) -- §22 (vendors as needed; preserved for "as-needed creation, no batch pre-load" intent), §23 (pre-populate COA), §30 (Finance Lead role), §67 (Phase 7A deferral), §70 (vendor / customer master ownership architecture -- closes Item 1's architectural question), §71 (governance principle -- IT-decides / Finance Lead-consults / COO-concurs on cross-domain impact)
- [`../backlog.md`](../backlog.md) -- Next Finance Lead Conversation Agenda subsection; BL-52 / BL-53 / BL-54 added for §70 follow-on plugin design, ERP write-permission lockdown, and cutover-day reconciliation
- [`../architecture/data-model.md`](../architecture/data-model.md) -- `rm_chartofaccount` structure for the COA review item
- [`../architecture/erp-pattern-notes.md`](../architecture/erp-pattern-notes.md) §3 -- the `rm_customer` cross-solution pattern; §70's push plugin design draws on or diverges from this in Phase 8 scoping
