# Form-Level Read-Only Enforcement on Posted Journal Entries

> Mitigation design for R-A-19 (form-level read-only fields not
> enforced in the maker-portal UI; the plugin layer enforces them
> server-side). Drafted 2026-05-22 in the documentation session
> following decision [§67](../decisions/datastream-books-decisions.md)
> (Phase 7A S4-S11 deferred). Design only -- no implementation under
> this document. Surfaced as a Phase 7A.5 candidate: small UI-flavored
> task independent of S4-S11 that closes the R-A-19 precondition for
> any future Pam-facing shell demo.

## The Gap

The Phase 6B [`PostJournalEntryPlugin`](../../plugins/DatastreamBooks.Plugins/Posting/PostJournalEntryPlugin.cs)
enforces immutability of four stamped fields on a Posted journal
entry:

| Field | What it holds | Stamped by |
|---|---|---|
| `rm_journalentrynumber` | The autonumber assigned at first save | Plugin (Phase 6A) |
| `rm_postedby_user` | systemuser lookup, set during Approved->Posted transition | Plugin (Phase 6B) |
| `rm_totaldebit` | Sum of line debits, computed at post time | Plugin (Phase 6B) |
| `rm_totalcredit` | Sum of line credits, computed at post time | Plugin (Phase 6B) |

Plugin enforcement is authoritative: any Update message that tries to
change one of these fields on a JE with `rm_status = Posted` throws
`InvalidPluginExecutionException`, and the Dataverse transaction
rolls back per decision [§41](../decisions/datastream-books-decisions.md).

The gap is purely UX: the maker-portal main form (FormId
`91037b32-197b-4ec0-9451-13fe65f36634`) renders these four fields as
editable controls regardless of `rm_status`. A user (Pam, an
auditor, an admin) opening Posted JE-2026-001005 sees the post-stamp
fields as editable; their cursor lands inside the value; they can
type. The save throws -- but the in-form experience suggests the edit
was accepted up until the save attempt. The mismatch creates
confusion ("I thought I changed it -- why did it not save?") and
weakens the audit-defensibility narrative when an auditor performs the
same experiment.

The mitigation must change the in-form experience so the fields
appear locked when `rm_status = Posted`, before any save attempt.
The server-side enforcement remains the authoritative gate; the
client-side change is a UX disclosure of the server gate.

## Mitigation Options

Five Dataverse mechanisms can disable a field in a model-driven form
conditionally on another field's value. They differ in cost,
maintenance, and where the rule lives.

### Option A -- Business Rule

Dataverse Business Rules are declarative, in-solution rules that
fire client-side on form load and on field change. They have a
**Lock Field** action that takes a target field + a boolean expression
over other fields on the same record.

A single business rule scoped to `rm_journalentry` main form, with a
condition `rm_status equals Posted` and four Lock Field actions
(one per target field), covers the entire R-A-19 surface.

**Pros:**
- Zero code. Authored entirely in the maker portal designer.
- Lives in the solution as XML; flows through `pull-solution.ps1` +
  CI/CD with no special handling.
- Visible to Pam and any future maintainer in the form designer --
  the rule appears as a labeled item alongside the form itself.
- Runs client-side; the user sees the lock immediately on form load.
- If Phase 7B replaces the main form with a custom page, the business
  rule becomes inert (custom pages don't honor classic business
  rules) but causes no harm and can be removed cleanly.

**Cons:**
- Limited expressiveness. Business rules can compare field values
  but cannot call functions, read the user's role, or check related
  records. The R-A-19 condition (`rm_status = Posted`) is exactly
  inside the supported envelope, so this is theoretical, not actual.
- Defense-in-depth gap if buggy: a malformed business rule could
  leave the form looking editable when the plugin would reject. The
  plugin remains the authoritative gate, but the user experience
  would regress to the current state.

### Option B -- Model-Driven Form Scripting (JavaScript Web Resource)

A JavaScript web resource bound to the form `OnLoad` event reads
`formContext.getAttribute("rm_status").getValue()` and calls
`formContext.getControl("rm_postedby_user").setDisabled(true)` (etc.)
on the four target controls when status is Posted. A second handler
on `rm_status` `OnChange` reruns the same logic if status flips
during the form session.

**Pros:**
- Full expressiveness. Future logic ("lock unless current user is
  in System Administrator role and period is open") fits cleanly.
- Familiar pattern from many existing Dataverse customizations.
- Same delivery channel as a business rule (solution-resident,
  CI/CD-deployable).

**Cons:**
- Code. Requires a JavaScript file in the solution, a test harness
  for the logic (or living with no tests), and a maintenance burden.
- More moving parts than the R-A-19 condition requires.
- YAGNI: the future logic that would justify scripting does not
  exist today and may never exist if Phase 7B replaces the form.

### Option C -- PCF Field-Level Lock

A custom PCF (PowerApps Component Framework) control replaces the
default field renderer with one that consults `rm_status` and
renders read-only when Posted.

**Pros:**
- Maximum control over presentation.
- Reusable across many fields if a project-wide "conditional lock"
  PCF is built once.

**Cons:**
- Significantly higher effort: PCF build pipeline, manifest, npm
  toolchain, deployment, version management.
- Overkill for "make field read-only based on a sibling field's
  value" -- a problem Dataverse already solves with two simpler
  mechanisms.
- One PCF per field type (text, lookup, decimal, picklist) unless
  generalized, multiplying the work.

### Option D -- Custom Page Guard

Replace the model-driven main form with a custom page (canvas-style
page hosted in the model-driven app) and implement conditional
disabling in the page's own logic.

**Pros:**
- Total control over UX. This is the eventual Phase 7B JE entry
  approach for the hybrid grid + form mode (decision [§50](../decisions/datastream-books-decisions.md)).
- Solves R-A-19 incidentally as a side effect of the rewrite.

**Cons:**
- Massive effort relative to R-A-19. Replicating the whole form's
  field layout, validation, and lookups in a custom page is weeks
  of work, not a single session.
- Couples R-A-19 mitigation to Phase 7B scope -- exactly what
  decision [§67](../decisions/datastream-books-decisions.md)
  deferred.
- The R-A-19 mitigation deadline is "before any Pam-facing shell
  demo", which can land before Phase 7B; coupling them violates the
  Phase 7A.5 framing.

### Option E -- Form Property Static Read-Only

Set the field's static `IsReadOnly` property to `true` in the form
designer.

**Pros:**
- Zero code, zero rules, single property toggle.

**Cons:**
- **Disqualifying.** Static read-only locks the field for every
  record at every status. Draft JEs (where the four fields are
  legitimately editable -- well, three of them; `rm_postedby_user`
  is never user-editable) would also be locked. The Draft creation
  flow breaks. Listed for completeness; not viable.

## Recommendation

**Option A (Business Rule).**

### Reasoning

1. **The condition is inside the business-rule expressiveness
   envelope.** `rm_status equals Posted` is the exact shape business
   rules were designed to handle. Choosing scripting (B) or PCF (C)
   for this would buy expressiveness we do not need.
2. **Zero code respects the §60 design-system principle.** Decision
   [§60](../decisions/datastream-books-decisions.md) commits the
   project to "Fluent UI v9 defaults with minimal custom layer." A
   business rule honors that; a JavaScript web resource bends it.
3. **Pam can audit the rule visually.** Dataverse renders business
   rules as a labeled flowchart in the maker portal. An auditor or
   Pam herself can open the rule, read the condition, and verify
   coverage in seconds -- no source-code review required.
4. **Deployment is identical to existing artifacts.** The business
   rule ships as solution XML under
   `solution/src/Entities/rm_journalentry/`, is captured by
   `pull-solution.ps1`, and rides the same CI/CD pipeline as every
   other Phase 1-6 artifact. No new tooling, no new patterns.
5. **Disposable cleanly.** When Phase 7B replaces the form with a
   custom page, the business rule becomes inert and can be deleted
   in the same commit that retires the form.

### Counter to the "defense-in-depth" objection

A buggy business rule could mask the server enforcement. Two
safeguards:

- The plugin is the authoritative gate per decision [§41](../decisions/datastream-books-decisions.md);
  a buggy business rule degrades UX but cannot bypass the immutability
  guarantee.
- A live verification step (see "Before Implementation") catches the
  bug class before merge.

## Effort Estimate

Single session, 45-60 minutes elapsed:

| Step | Estimate | Notes |
|---|---|---|
| Author business rule in maker portal (PRI-Books-Dev) | 15-25 min | One condition + four Lock Field actions; activate. |
| Live verify against JE-2026-001005 (Posted) | 5-10 min | Open form; confirm 4 fields render disabled. |
| Live verify against a Draft JE | 5-10 min | Open a Draft record; confirm fields editable (esp. `rm_journalentrynumber`, which is the autonumber but is user-visible during draft). |
| `pull-solution.ps1`, review diff | 5-10 min | The new rule appears under FormXml or as a separate business rule XML; confirm it's captured. |
| Commit + push; CI deploy and re-verify | 5-10 min | Standard ALM flow. |

No new entities, no schema changes, no plugin changes, no SQL
migration.

## Phase 7A.5 Framing

This work is **not** part of Phase 7A S4-S11 (deferred by decision
[§67](../decisions/datastream-books-decisions.md)). It is a one-off
form-metadata change that:

- Closes the R-A-19 precondition for any Pam-facing demo of any
  Phase 7A artifact (current or future).
- Does not depend on the §17 vendor master scope answer.
- Does not depend on Backend Track A.
- Does not depend on Phase 7A S4-S11 reaffirmation.
- Can land in a single dedicated session whenever convenient.
- Is independently considerable -- choosing to schedule it does
  not commit the project to anything else.

Recommended as the "Phase 7A.5" candidate: a small, isolated
UI-flavored task that unlocks future demo optionality without
crossing the §67 deferral boundary.

## Before Implementation

The canonical implementation path is the runbook at
[`../runbooks/r-a-19-business-rule-implementation.md`](../runbooks/r-a-19-business-rule-implementation.md)
(drafted 2026-05-22), which includes the maker-portal click-path,
the pre-flight checklist, the live verification checklist against
JE-2026-001005 and a fresh Draft JE, the capture step with the
expected diff location, the rollback procedure, and the
post-implementation backlog / risk-register / design-doc updates.
The conceptual pre-flight checklist below is retained for design
traceability; the runbook expands each item into operational steps.

A separate implementation session should:

1. **Verify the current form state.** Open the JE main form in the
   PRI-Books-Dev maker portal. Confirm `rm_postedby_user`,
   `rm_journalentrynumber`, `rm_totaldebit`, `rm_totalcredit`, and
   `rm_posteddatetime` are present and currently editable. If any
   field is already locked by some other mechanism, the design
   here is over-scoped and should narrow.
2. **Verify `rm_status` is on the form.** The business rule
   condition reads `rm_status`. If the field is not on the form
   (it should be -- it's the status picklist) the rule can still
   evaluate, but defensive: confirm.
3. **Decide on rule scope.** Default to "Form scope" (rule applies
   only to the main form). "Entity scope" applies the rule
   everywhere the entity is rendered, including views' inline-edit
   experience -- not strictly needed for R-A-19 but harmless.
   Recommend Form scope for minimum blast radius.
4. **Document the rule in source control.** After
   `pull-solution.ps1`, confirm the rule appears in the staged
   diff under `solution/src/Entities/rm_journalentry/` (likely as
   a new file under a `BusinessRules/` or `WorkflowXaml/` path
   depending on how Dataverse serializes it -- to be verified
   empirically).

## Open Questions

- **Does Dataverse serialize the business rule as part of the
  entity bundle or as a separate workflow row?** Answer impacts
  the `pull-solution.ps1` diff review step. Verifiable in the
  implementation session by running pull-solution and inspecting
  the diff.
- **Is `rm_journalentrynumber` populated by the time the form
  first renders for a brand-new Draft JE?** Affects whether the
  field is meaningfully editable during Draft. If the autonumber
  is plugin-stamped at first save, the answer is "not until first
  save" -- not a blocker for the business rule, but worth knowing
  for the Draft-state verification step.
- **Do we want the same lock on `rm_posteddatetime`?** Not
  currently in scope per R-A-19, but symmetrical. Recommendation:
  yes, include it in the same business rule -- one more Lock Field
  action, zero additional cost. To be confirmed by operator before
  authoring.

## See Also

- [`../runbooks/r-a-19-business-rule-implementation.md`](../runbooks/r-a-19-business-rule-implementation.md) -- the implementation runbook (operator-driven session, 30-45 min)
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) -- decisions §41 (rollback-and-throw), §50 (hybrid JE entry, Phase 7B), §60 (Fluent UI v9 defaults), §66 (Phase 7A parallel-track, provisional), §67 (S4-S11 deferred), §68 (operating principles).
- [`../risk-register.md`](../risk-register.md) -- R-A-19 (the risk this doc mitigates).
- [`../backlog.md`](../backlog.md) -- BL-47 (R-A-19 mitigation tracking item).
- [`./immutability-design.md`](./immutability-design.md) -- the broader audit-defensibility narrative this mitigation supports.
- [`../../plugins/DatastreamBooks.Plugins/Posting/PostJournalEntryPlugin.cs`](../../plugins/DatastreamBooks.Plugins/Posting/PostJournalEntryPlugin.cs) -- the server-side enforcement that remains the authoritative gate.
