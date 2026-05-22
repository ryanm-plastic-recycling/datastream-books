# DRAFT — Phase 7A Foundation Claude Code Prompt

> **AMENDED 2026-05-21 per decision §66.** Phase 7A Sessions S1-S3
> (research / documentation items: credential cleanup, visual identity
> extraction, sitemap design) ran in parallel with the Backend Track
> under the provisional override in
> [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md)
> §66. The pre-flight checklist below was written *before* §66 and
> requires Backend Phase 8 / 9 / 10 / 11+ complete -- those items were
> **not** complete when S1-S3 ran, and that gap was intentional and
> documented in §66. This prompt now describes the *eventual* full
> Phase 7A kickoff (S4 onward); if you are resuming Phase 7A after
> §66 reaffirmation, re-read §66 and treat the pre-flight checklist
> as advisory rather than blocking. Artifacts produced by S1-S3:
> [`../architecture/ui-styling.md`](../architecture/ui-styling.md),
> [`../architecture/ui-sitemap.md`](../architecture/ui-sitemap.md),
> and a follow-up section in
> [`../architecture/immutability-validation.md`](../architecture/immutability-validation.md).
>
> **DRAFT.** Original framing follows. Original purpose was a kickoff
> prompt for a Phase 7A session that opens *after* all backend phases
> complete. Retained for the eventual full-kickoff use case.

## Before running this prompt — pre-flight checklist

Confirm **all** of the following before launching this session:

- [ ] Backend Phase 6B end-to-end validation completed and result appended to [`../architecture/immutability-validation.md`](../architecture/immutability-validation.md)
- [ ] Backend Phase 7 (Vendor/Customer Integration with ERP) complete — `rm_customer` cross-solution lookup pattern in place and exercised
- [ ] Backend Phase 8 (AP/AR Core) complete — bill/invoice/receipt entities and posting plugins in PRI-Books-Dev
- [ ] Backend Phase 9 (Period Close + Reporting) complete — `ClosePeriodPlugin`, `ReopenPeriodPlugin`, `ReportSnapshots`, nightly hash-chain verification job all in place
- [ ] Backend Phase 10 (Macola Data Migration + Cutover) complete — at least one full close cycle reconciled penny-perfect against Macola
- [ ] Any Phase 11/12+ backend items (whatever they end up being — see `roadmap.md` for the current state) complete
- [ ] PRI-Books-Test sandbox provisioned (per [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) — three-environment ALM before UAT)
- [ ] Pam aware that Phase 7 is opening and the CR-based ownership flow is about to activate (per decision §57)
- [ ] All 17 Phase 7 UX decisions (§46-§62) reviewed and still current — if any have shifted since 2026-05-20, update the decision log first

## What this session will do

Phase 7A is the **3-week Foundation** sub-phase. It builds the shell, the
navigation, the homepage, and the security role scaffolding — none of the
v1 transactional screens. Exit criteria are listed in
[`../decisions/phase-7-ui-design.md`](../decisions/phase-7-ui-design.md) §Phase 7A.

The prompt below is intentionally a **session-kickoff prompt** — it asks
Claude Code to read context, confirm understanding, and propose a
sequenced task list. Implementation prompts for each Phase 7A deliverable
follow as subsequent sessions.

## The prompt

Copy the contents of the fenced block below into Claude Code when ready
to open Phase 7A.

```
SESSION TYPE: Phase 7A (Foundation) kickoff — 3-week sub-phase, first session

OBJECTIVE:
Open the Phase 7 (UI Track) build. Read the planning artifacts, confirm
the 17 UX decisions (§46-§62 in the decision log) are still current,
propose a sequenced task list for Phase 7A, and surface any open
architectural questions that need an answer before code is written.
No code in this session — orientation only.

PRE-FLIGHT:
- OneDrive sync paused for the duration of work
- Solo-dev project, work directly on main, no branches
- Before any work: git fetch origin && git pull origin main
- Verify pac auth: pac auth list, pac auth select --name pri-books-dev
- Confirm the "Before running this prompt" checklist in
  docs/runbooks/phase-7a-foundation-prompt.md is fully satisfied. STOP
  and ask if any item is unchecked.

CONTEXT (read these first, in this order):
1. README.md
2. AGENTS.md
3. CLAUDE.md
4. docs/roadmap.md — confirm we are entering Phase 7 (UI Track)
5. docs/decisions/phase-7-ui-design.md — the Phase 7 plan document
6. docs/decisions/datastream-books-decisions.md §46-§62 — the 17 UX
   decisions that scope this phase
7. docs/architecture/security-model.md — the 7 finance-specific roles
   (decision §61) that Phase 7A will scaffold
8. docs/architecture/immutability-design.md §B and §G — drill-down must
   preserve transaction provenance per decision §55
9. docs/risk-register.md — the 5 Phase 7 risks (R-7-01 through R-7-05)
10. AGENTS.md §Code Conventions (TypeScript / React) — Fluent UI v9,
    strict TS, no <form> tags in PCF, no localStorage/sessionStorage
11. docs/reference/erp-metadata/ README and erp-pattern-notes.md — for
    cross-solution lookup conventions and visual palette extraction

TODAY'S SCOPE (orientation only — no code):

1. Confirm pre-flight checklist is satisfied. If anything is unchecked,
   STOP and surface to the operator before reading further.

2. Read the context files in the order listed above. After each, write
   one sentence confirming what is current and what (if anything) looks
   stale.

3. Confirm the 17 UX decisions (§46-§62) are still the operating model.
   For any decision where the project state has shifted since
   2026-05-20, flag it explicitly. Examples of potential drift:
   - Have personas (§47) changed since cutover? Did UAT surface a
     persona that wasn't in the original 5?
   - Is the v1 screen list (§48) still all 8 — or has anything been
     deferred / merged / split?
   - Is the strict-sequential timing (§58) still in force, or did some
     parallel work happen during the backend phases?
   - Is Pam's biggest pain point still navigation (§62), or did
     parallel-run / UAT surface a different top pain?

4. Propose a sequenced task list for the 3-week Phase 7A sub-phase:
   - Sitemap design grouped by user mental model
   - Global search PCF control
   - Breadcrumb component
   - Recent items widget
   - Role-aware homepage shell
   - Datastream ERP visual styling extraction (color hex values, font,
     logo placement) — captured as a styling note
   - Finance-specific security role scaffolding (the 7 Dataverse roles
     per decision §61, created in PRI-Books-Dev with empty privileges
     populated as later sub-phases attach pages)
   Each task should include: estimated time, dependencies, exit
   criteria, and which decision(s) it implements.

5. Surface open architectural questions that need an answer before code
   is written. Likely candidates:
   - Global search PCF — does it search Azure SQL (`ledger.*`,
     `audit.*`) or only Dataverse? Same question as Phase 7C drill-down
     architecture (decision §55 implies live ledger access; the global
     search may share that infrastructure).
   - Custom React page hosting — single React shell containing all
     custom pages, or one Power Pages custom-page per screen?
   - Authentication for React pages — implicit via Power Pages session
     or explicit Entra MSAL flow?
   - PCF vs. custom page boundary — at what point does a "PCF control"
     become a "custom page"? (Influences how the global search and the
     recent items widget are packaged.)

6. Propose how Pam's first encounter with the UI will happen. Per
   decision §57, no design review during construction — Pam first sees
   pages when they land in dev. The sequence matters: Phase 7A produces
   shell + homepage + navigation but no transactional screens, so
   "landing in dev" is a meaningful but limited experience. Should we
   invite Pam in at end of Phase 7A or wait until first transactional
   screen lands in Phase 7B?

REPORTING:
- Brief output throughout
- At session end, hand off with:
  - Confirmed task list with estimates
  - List of open architectural questions, each with a proposed default
    answer and a flag for which need explicit operator input
  - Proposed Phase 7A timeline against the calendar
  - Any decisions from §46-§62 that look stale and need updating

CRITICAL CONSTRAINTS:
- NO code is written this session
- NO solution components are modified
- NO Dataverse changes
- NO Azure changes
- This is the orientation session for the Phase 7 build

TIME BUDGET: 60 minutes for orientation. If exceeded, the proposed task
list is the safe place to leave partial work; the open-architectural-
questions list must be complete for the session to be considered closed.
```

## Subsequent Phase 7A sessions

After the kickoff session above, expect roughly the following follow-on
sessions through the 3-week Phase 7A. Each is its own Claude Code
session, prompted from the task list produced by the kickoff:

1. **Sitemap + visual style extraction session** — design sitemap
   grouped by user mental model; extract Datastream ERP color hex
   values, font choices, logo placement; capture as a styling note
   (decision §49, §59).
2. **Global search PCF session** — build the PCF control; decide live
   ledger access vs. Dataverse-only based on the architectural answer
   from kickoff. Addresses decision §62 (Pam's navigation pain).
3. **Breadcrumb + recent items session** — both are small enough to
   share a session; both feed the navigation story.
4. **Role-aware homepage session** — single shared dashboard, widgets
   filtered by current user's role(s) (decision §52, §47).
5. **Security role scaffolding session** — create the 7 finance-
   specific Dataverse security roles per decision §61 in PRI-Books-Dev
   with empty privileges. Privileges populate as Phase 7B-7D pages
   attach to roles.

## Cross-references

- [`../decisions/phase-7-ui-design.md`](../decisions/phase-7-ui-design.md) — the Phase 7 plan
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) §46-§62 — the 17 UX decisions
- [`../risk-register.md`](../risk-register.md) — R-7-01 through R-7-05
- [`../architecture/security-model.md`](../architecture/security-model.md) — the 7 finance-specific roles
- [`../architecture/immutability-design.md`](../architecture/immutability-design.md) §B and §G — drill-down provenance constraints
- [`../roadmap.md`](../roadmap.md) — phase sequencing
- [`../../AGENTS.md`](../../AGENTS.md) §Code Conventions — TypeScript / React rules
