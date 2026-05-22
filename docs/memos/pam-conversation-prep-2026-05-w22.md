# Pam Conversation Prep -- Week of 2026-05-25

> Consolidated agenda for the upcoming Datastream Books conversation
> with Pam. Drafted 2026-05-22 per audit Section C recommendation and
> decision §67's call for §17 to land alongside the existing §1 / §3
> / §11 agenda. One-page format: pre-read, agenda, what we need,
> what she's signing off on, what she should bring.

## Meeting metadata

- **Audience:** Pam (Finance System Owner) + Ryan (IT)
- **Duration:** 45 minutes (30 minutes minimum; 60 if §17 generates discussion)
- **Format:** In person if possible; Teams as fallback
- **Decision authority:** Pam owns the finance answers. IT defers
  to her on accounting questions; she defers to IT on architecture.

## Pre-read (please review before the meeting)

1. The standard 54-row Chart of Accounts already loaded in
   PRI-Books-Dev under "Default Operating Entity"
   ([`../architecture/data-model.md`](../architecture/data-model.md)
   §`rm_chartofaccount` lists the structure). Pam should be ready
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
- A working opinion on whether the operations team or the AP team
  owns new vendor creation.

## Agenda

### Item 1 -- §17 Vendor Master Scope (NEW; ~10 min)

> Decision §22 of the decision log says "vendors/customers added as
> needed" -- broad enough that it leaves open who owns the master.
> This is the only agenda item that has not been previously
> discussed with you; it surfaced during the 2026-05-21 audit and
> blocks the current backend track.

- **What we need from you:**
  1. When a bill arrives from a new vendor, who creates the
     record -- AP clerk in Books, or operations in ERP first?
  2. For vendors already in PRI-Datastream (ERP), is the canonical
     record the ERP one or the Books one?
  3. What vendor fields does Books need beyond ERP's existing
     schema? (W-9 status, 1099 reportability, payment terms, NACHA
     banking, AP-specific approval routing.)
- **What we're proposing you sign off on:** the operating model
  for vendor records (one of three: Books-owned, ERP-owned with
  Books cross-reference, or split by vendor type). IT will write
  the formal decision row in the decision log.
- **What is downstream of this:** Phase 7-Backend Track A
  (vendor/customer integration) and all of Phase 8 (AP Core) wait
  on this answer.

### Item 2 -- §3 Approval Thresholds (~10 min)

> Pam-owned thresholds. We need working numbers, not perfect ones.
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
  (anticipated §68 vendor master scope, §69 approval thresholds,
  §70 entity structure if substantively different from current
  placeholder, §71 COA sign-off).
- Update the `executive-questionnaire.md` status flags from
  `[Active] [Pending Pam]` to `[Confirmed]` with citations.
- Mark the corresponding backlog items (BL-01, BL-04, BL-37, BL-38,
  BL-46) as actionable for the next IT-side work session.

If anything is left unresolved at the end of the conversation, IT
will track it in [`../backlog.md`](../backlog.md) under a new BL-id
and flag it for the next conversation rather than letting it drift.

## Linked artifacts

- [`./executive-questionnaire.md`](./executive-questionnaire.md) -- §1, §3, §11, §17 in full
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) -- §22 (vendors as needed), §23 (pre-populate COA), §30 (Pam as Finance System Owner), §67 (Phase 7A deferral that makes this conversation the immediate critical path)
- [`../backlog.md`](../backlog.md) -- Next Pam Conversation Agenda subsection
- [`../architecture/data-model.md`](../architecture/data-model.md) -- `rm_chartofaccount` structure for the COA review item
- [`../architecture/erp-pattern-notes.md`](../architecture/erp-pattern-notes.md) §3 -- the `rm_customer` cross-solution pattern that the §17 vendor decision will mirror or diverge from
