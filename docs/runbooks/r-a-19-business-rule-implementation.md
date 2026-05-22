# Runbook: R-A-19 Business Rule Implementation

> Operator handoff for closing risk-register entry R-A-19 (form-level
> read-only fields not enforced on the JE main form). Design rationale
> and option analysis live in [`../architecture/form-readonly-enforcement.md`](../architecture/form-readonly-enforcement.md);
> this runbook is the click-path. Drafted 2026-05-22 in the continuation
> session immediately after decision §68 codified the operating
> principles. Single operator session, 30-45 minutes elapsed.

## Goal

Author a Dataverse Business Rule on the `rm_journalentry` main form
that locks five stamp fields when `rm_status = Posted`. Closes
BL-47 in the backlog; closes R-A-19 in the risk register; satisfies
the precondition for any Finance Lead-facing demo of Phase 7A or Phase 7A.5
artifacts.

## Pre-flight checklist

Before touching the maker portal:

1. **Verify operator auth profile.**
   ```
   pac auth list
   ```
   Confirm `pri-books-dev` is the **active** profile (asterisk). If
   the active profile is different (commonly `pri-datastream` for
   ERP work):
   ```
   pac auth select --index <N>
   ```
   where `<N>` is the index column from `pac auth list` for the
   `pri-books-dev` row.
2. **Verify `rm_status` is present on the JE main form.** The
   business rule condition reads `rm_status`. The field is the
   status picklist and should already be on the form. If it is not,
   stop and surface -- the form is not in the expected state and
   the design assumed it was.
3. **Verify the five target fields are present and currently
   editable** on the JE main form:
   - `rm_postedby_user` (lookup)
   - `rm_journalentrynumber` (text)
   - `rm_totaldebit` (decimal)
   - `rm_totalcredit` (decimal)
   - `rm_posteddatetime` (datetime)

   Open the form designer for FormId
   `91037b32-197b-4ec0-9451-13fe65f36634` and confirm each field is
   present and not already locked by some other mechanism. If any
   is already locked, stop and surface -- the design doc is
   over-scoped and the rule should narrow to the unlocked subset.

## Maker portal click-path

1. Navigate to the PRI-Books-Dev maker portal:
   `https://make.powerapps.com/environments/<PRI-Books-Dev-env-id>/solutions`
2. Open the **DatastreamBooks** solution.
3. **Tables** -> **rm_journalentry** -> **Business rules** tab.
4. Click **+ New business rule**.
   - **Scope:** Form
   - **Form selection:** Information (the main form -- FormId
     `91037b32-197b-4ec0-9451-13fe65f36634`)
5. Rule properties:
   - **Display name:** `JE Posted -- Lock Stamp Fields`
   - **Name:** `rm_je_posted_lock_stamp_fields` (auto-generated;
     edit if Dataverse picks something else)
   - **Description:** `Locks the five plugin-stamped fields when
     rm_status = Posted. R-A-19 mitigation. See
     docs/architecture/form-readonly-enforcement.md.`
6. Add a **Condition** on the canvas:
   - **Source:** Entity
   - **Field:** `rm_status` (Status)
   - **Operator:** Equals
   - **Type:** Value
   - **Value:** `Posted`
7. On the **Posted** branch (the true branch of the condition), add
   five **Lock Field** actions, one per field. For each:
   - Drag a `Lock / Unlock Field` action onto the canvas
   - **Field:** select the target column
   - **Status:** **Lock**

   Targets, in this order:
   - `rm_postedby_user`
   - `rm_journalentrynumber`
   - `rm_totaldebit`
   - `rm_totalcredit`
   - `rm_posteddatetime`

   **Lookup-field caveat:** `rm_postedby_user` is a lookup, not a
   simple-type field. Dataverse business rule Lock Field on lookups
   has historically had inconsistent behavior across form versions
   (modern UI vs classic, web vs mobile). The verification step
   below tests this specifically. If `rm_postedby_user` does not
   lock under verification, do not adjust the rule -- the fallback
   path is Option B (form scripting) per
   [`../architecture/form-readonly-enforcement.md`](../architecture/form-readonly-enforcement.md);
   signpost the failure and stop here.
8. **Save.**
9. **Activate.**

## Live verification

### Verification 1 -- Posted JE-2026-001005

1. Navigate to **PRI-Books-Dev** -> Tables -> rm_journalentry ->
   **Data** -> Active Journal Entries.
2. Open `JE-2026-001005` (rm_status = Posted).
3. On the main form, confirm each of the five fields renders
   **disabled** (greyed out / non-interactive):

   | Field | Expected state | Notes |
   |---|---|---|
   | `rm_postedby_user` | Locked | Lookup field -- lookup chooser hidden or disabled |
   | `rm_journalentrynumber` | Locked | Text input disabled |
   | `rm_totaldebit` | Locked | Decimal input disabled |
   | `rm_totalcredit` | Locked | Decimal input disabled |
   | `rm_posteddatetime` | Locked | Datetime input disabled |

4. Try to type into each field. The form should reject input on
   all five.

**If any field renders editable:** stop. Record which field(s)
failed in the runbook log section at the bottom. If only
`rm_postedby_user` failed, fall back to Option B per the design
doc. If multiple fields failed, the business rule did not activate
correctly -- re-verify in the maker portal that the rule status is
**Activated**.

### Verification 2 -- Fresh Draft JE

1. Create a new draft JE in PRI-Books-Dev (any test entity, any
   test amounts). Status will be Draft.
2. On the main form, confirm:
   - `rm_postedby_user`: editable (Draft -- not yet stamped)
   - `rm_journalentrynumber`: **may be read-only** -- the
     autonumber plugin stamps this field on first save; the form's
     perceived state during Draft is implementation-dependent.
     Note in the log and do not adjust the rule.
   - `rm_totaldebit`: editable (Draft -- still in progress)
   - `rm_totalcredit`: editable (Draft -- still in progress)
   - `rm_posteddatetime`: editable (Draft -- not yet stamped)
3. Save the draft JE. Do not approve. Do not post.

**If any field that should be editable in Draft is locked:** stop.
The business rule's Posted branch may be firing under the wrong
condition; re-verify in the maker portal that the condition reads
`rm_status equals Posted` and that the five Lock Field actions sit
on the Posted (true) branch, not the default (false) branch.

## Capture step

After both verifications pass:

```
pwsh scripts/pull-solution.ps1
git status
```

**Expected diff location:** somewhere under
`solution/src/Entities/rm_journalentry/`. The exact path Dataverse
uses for business rule XAML is to be discovered empirically during
this step -- the design doc lists this as an open question.
Likely candidates:

- `solution/src/Entities/rm_journalentry/Workflows/` (if treated as
  a workflow row, which business rules technically are)
- `solution/src/Entities/rm_journalentry/FormXml/` (less likely)
- A top-level `solution/src/Workflows/` directory (least likely;
  would surprise the design doc's assumption)

**If the diff lands outside `solution/src/Entities/rm_journalentry/`
or its sub-paths:** stop and surface. The design doc assumed
entity-scoped serialization; an unexpected location may indicate a
configuration issue or a Dataverse pattern change. Do not commit
until the location is understood.

If the diff lands in the expected location:

```
git add solution/src/Entities/rm_journalentry/
git commit -m "feat: business rule for JE Posted lock stamp fields (R-A-19 / BL-47)

Closes BL-47 in docs/backlog.md. Mitigates R-A-19 by locking the
five plugin-stamped fields on the rm_journalentry main form when
rm_status = Posted: rm_postedby_user, rm_journalentrynumber,
rm_totaldebit, rm_totalcredit, rm_posteddatetime. Authored in
PRI-Books-Dev maker portal per
docs/runbooks/r-a-19-business-rule-implementation.md, then
captured via pull-solution.ps1.
"
git push origin main
```

## Rollback

If verification fails or the rule causes unexpected behavior:

1. Maker portal -> rm_journalentry -> Business rules -> select the
   rule -> **Deactivate**.
2. (If already captured to source) optionally delete the rule from
   source:
   ```
   pwsh scripts/pull-solution.ps1
   git add solution/src/Entities/rm_journalentry/
   git commit -m "revert: deactivate JE Posted lock stamp fields business rule (R-A-19 / BL-47 rollback)"
   git push origin main
   ```
3. Record the rollback in the runbook log section below. Update
   BL-47 status to reflect the rollback condition (e.g.,
   `[In progress -- rolled back, fallback path TBD]`).

The plugin layer remains the authoritative immutability gate per
decision §41, so a rollback degrades UX but does not weaken the
audit-defensibility narrative.

## Post-implementation

1. Mark **BL-47 [Done YYYY-MM-DD]** in
   [`../backlog.md`](../backlog.md) (update the BL-47 inline entry
   Status field).
2. Update the **Priority Index** in `backlog.md`:
   - Move BL-47 to the Done section of the table
   - Bucket totals: P1: 8 -> 7, Done: 7 -> 8. Total stays 50.
3. Append a row to the backlog **Change log** at the bottom with
   the implementation commit hash and date.
4. Close the three **Open Questions** in
   [`../architecture/form-readonly-enforcement.md`](../architecture/form-readonly-enforcement.md)
   with their empirical answers:
   - Where does Dataverse serialize the business rule? (Whichever
     path it actually landed in.)
   - Is `rm_journalentrynumber` populated on the form before first
     save? (Whatever Verification 2 step 2 observed.)
   - Did `rm_posteddatetime` lock correctly? (Yes / no; if no,
     remove it from the rule and document the partial mitigation.)
5. Update **R-A-19 in [`../risk-register.md`](../risk-register.md)**:
   - If verification passed cleanly: status changes from
     `Open (added 2026-05-21)` to
     `Closed YYYY-MM-DD via BL-47 business rule`.
   - If verification partially failed and Option B fallback is
     pending: status stays Open with a note pointing to the new
     follow-up backlog item for the fallback work.

## Runbook log

Append observations from each execution here.

| Date | Operator | Result | Notes |
|---|---|---|---|
| _(awaiting first execution)_ | | | |

## See also

- [`../architecture/form-readonly-enforcement.md`](../architecture/form-readonly-enforcement.md) -- design rationale and option analysis
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) §41 (rollback-and-throw, the authoritative immutability gate this runbook supports), §67 (Phase 7A S4-S11 deferred), §68 (operating principles that justified the operator-handoff framing of this runbook)
- [`../risk-register.md`](../risk-register.md) -- R-A-19 entry
- [`../backlog.md`](../backlog.md) -- BL-47 entry
- [`./plugin-registration.md`](plugin-registration.md) -- the "Why this is a runbook" precedent (referenced by R-A-20)
