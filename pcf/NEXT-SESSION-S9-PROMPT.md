# DRAFT -- Phase 7A Session S9 (PCF / Pages Toolchain Setup) Kickoff Prompt

> **DRAFT. Do not execute until decision §66 has been reaffirmed for S4+
> in a separate conversation.** This file captures the S9 kickoff prompt
> so it is ready when the operator clears the §66 gate. The prompt itself
> sits inside the fenced block below; copy it into Claude Code when ready.
>
> Authored 2026-05-21 evening as a stretch artifact during the autonomous
> audit window. See [`../docs/audits/audit-2026-05-21-evening.md`](../docs/audits/audit-2026-05-21-evening.md)
> §G1 for the §66 reaffirmation context.

## Why S9 is the right next PCF session (not S4)

Per the Phase 7A work breakdown approved in the 2026-05-21 evening
kickoff conversation, the post-S3 sessions are:

- S4 -- app module + theme application in PRI-Books-Dev
- S5 -- breadcrumb component (tentatively dropped per "Better Option" call)
- S6 -- recent items decision (likely lean on Dataverse built-in)
- S7 -- role-aware dashboard scaffold
- S8 -- common pattern docs (save semantics, notifications, status state list)
- S9 -- **PCF / pages toolchain setup** (this prompt's target)
- S10 -- example PCF: status pill
- S11 -- close-out

The sessions naturally split into two tracks:

- **Track A: maker-portal-side (S4, S6 if custom, S7).** Builds artifacts inside Dataverse using the maker portal + `pull-solution.ps1`.
- **Track B: build-tooling-side (S9, S10).** Builds artifacts in `pcf/` and `pages/` using `npm` + `pac pcf` tooling.

S9 is Track B's foundation. Until S9 lands, S10 (status pill) cannot
run. S5 (breadcrumb), if not dropped, also depends on S9. **S9 is the
right Track B kickoff because every subsequent PCF deliverable
inherits its toolchain choices.**

S9 can run **in parallel** with S4 in a real-calendar sense -- they
modify different parts of the repo (`pcf/` vs `solution/src/`). But
the operator's pacing has been one focused session at a time, so
realistically S9 follows S4 chronologically.

## Pre-flight checklist for S9

Confirm **all** of the following before invoking the prompt:

- [ ] Decision §66 reaffirmed for S4+ in a documented operator
      conversation (or §66 superseded by a new decision row that
      authorizes S4+ explicitly).
- [ ] S4 (app module + theme + logo packaging) is complete and
      committed -- or operator has explicitly chosen to run S9 ahead
      of S4 in parallel.
- [ ] `docs/architecture/ui-styling.md` CSS variable definitions are
      still the source of truth (not amended since Phase 7A S2).
- [ ] `git fetch origin && git pull origin main` runs clean.
- [ ] `pac auth select --name pri-books-dev` returns the expected
      Dev environment (booksdev.crm.dynamics.com).
- [ ] Node.js LTS installed (check: `node --version` >= 18.x).
- [ ] `pac --help` includes `pcf` subcommand (modern pipeline).
- [ ] `pcf/` directory exists; currently empty except for `.gitkeep`
      and this prompt file.

## What S9 will do

Per Q9 answer from the 2026-05-21 evening kickoff
([conversation reference in the decision log §66 context]):
**modern pipeline (`pac pcf init --framework react`), Fluent UI v9
as per-control imports.**

S9 builds the **toolchain scaffold** that every subsequent PCF
control will inherit. It does NOT build a real component. The
status pill (S10) is the first real component.

### Deliverables

1. **`pcf/StatusPill/` scaffold** -- created via `pac pcf init`,
   modern pipeline (`--framework react`). One control, named
   StatusPill (placeholder; S10 implements its logic). Verifies the
   toolchain end-to-end without committing to component behavior.

2. **Shared CSS variable file at `pcf/shared/css/tokens.css`** --
   the single source of truth for the CSS variables defined in
   `docs/architecture/ui-styling.md`. Every PCF imports this file (or
   inlines the relevant subset). When variables change in
   `ui-styling.md`, only this file needs to be edited.

3. **Shared TypeScript types at `pcf/shared/types/`** -- starter
   types for JE status enum (matching `data-model.md`
   `rm_journalentry.rm_status` option values 261910000-261910005),
   for the status pill consumer and any later component that needs
   the enum.

4. **`pcf/package.json` workspace skeleton** -- if needed for monorepo
   structure. Otherwise each control's own `package.json` is
   sufficient.

5. **Build verification** -- `npm install` + `npm run build` produce
   a valid `out/controls/StatusPill/` bundle. No deploy to
   PRI-Books-Dev yet -- that is S10's first commit.

### Not in S9 scope

- Status pill component logic (S10).
- Any other PCF control beyond the StatusPill placeholder.
- Custom page scaffold (`pages/` directory) -- defer to the
  custom-page-specific session if/when one is scheduled.
- Solution registration of the StatusPill assembly -- S10 handles.
- Any CI/CD changes -- the existing pipeline picks up the PCF
  artifacts when S10 commits them.

## The prompt

Copy the contents of the fenced block below into Claude Code when
ready to open S9.

```
SESSION TYPE: Phase 7A Session S9 -- PCF / Pages toolchain setup

OBJECTIVE:
Stand up the build-tooling scaffold every subsequent PCF deliverable
will inherit. Modern pipeline (pac pcf init --framework react), Fluent
UI v9 as per-control imports. One placeholder control (StatusPill --
S10 implements logic). One shared CSS tokens file matching
docs/architecture/ui-styling.md. One shared TypeScript types
directory. Build verification end-to-end (npm install + npm run
build).

PRE-FLIGHT (read in this order before any tool use):
1. README.md
2. AGENTS.md (note section 64 ASCII discipline + Branching Policy)
3. CLAUDE.md
4. docs/decisions/datastream-books-decisions.md sections 60, 61, 66
   (Fluent UI v9 + minimal custom layer; 7 finance-specific security
   roles; provisional parallel-track)
5. docs/architecture/ui-styling.md (CSS variables, status pill
   palette, typography)
6. docs/architecture/ui-sitemap.md (where the status pill will
   eventually surface)
7. docs/audits/audit-2026-05-21-evening.md sections E, G1 (backlog
   items affecting S9; G1 explains the parallel-track context)

Then run pre-flight commands:
- git fetch origin && git pull origin main
- pac auth select --name pri-books-dev
- pac auth list (verify pri-books-dev is active)
- node --version (verify >= 18.x)
- pac --help (verify pcf subcommand exists)

DELIVERABLES (in order):

D1. Create pcf/StatusPill/ via pac pcf init.
    - Modern pipeline, React framework, scope = field (drops into form
      columns; eventual host on rm_journalentry.rm_status and
      rm_journalentryline as needed).
    - Namespace: PlasticRecycling.DatastreamBooks (or shorter --
      operator confirms during session).
    - Control name: StatusPill.
    - Verify pac generates the standard skeleton (ControlManifest.Input.xml,
      index.ts, css/, etc.).

D2. Create pcf/shared/css/tokens.css.
    - Mirror the CSS variable definitions from
      docs/architecture/ui-styling.md exactly. Brand palette,
      neutrals, status pill palette, typography, density.
    - Add a header comment explaining: edit ui-styling.md FIRST,
      then sync changes here. ui-styling.md is the spec; tokens.css
      is the implementation.

D3. Create pcf/shared/types/jeStatus.ts.
    - Export const enum (or const object) with the 6 status values
      from data-model.md (Draft=261910000 through Voided=261910005).
    - Export a status-to-CSS-variable-name mapping (e.g.,
      JeStatusTokens[Draft] = '--status-draft-bg' + '--status-draft-fg').
    - Strict TypeScript per AGENTS.md.

D4. Wire StatusPill control to import from pcf/shared/.
    - StatusPill's package.json gets a relative dependency on
      pcf/shared/.
    - The StatusPill index.ts imports tokens.css and jeStatus.ts but
      does NOT implement the rendering yet -- a single placeholder
      div with text "StatusPill scaffold -- implemented in S10" is
      sufficient.

D5. Verify build end-to-end.
    - cd pcf/StatusPill
    - npm install (no warnings beyond informational; no errors)
    - npm run build
    - Confirm out/controls/StatusPill/ exists with bundle.js,
      ControlManifest.xml, css/StatusPill.css

D6. Commit (after operator approval).
    - Show diff to operator before staging.
    - Stage explicit paths (pcf/StatusPill/, pcf/shared/, any
      package.json modifications). Do not stage node_modules/ --
      verify .gitignore excludes it.
    - Commit message:
      "Phase 7A S9: PCF + pages toolchain scaffold

       Modern pipeline (pac pcf init --framework react). Fluent UI v9
       as per-control imports. StatusPill control scaffold created
       (logic implemented in S10). Shared CSS tokens mirror
       ui-styling.md spec. Shared jeStatus.ts types match
       data-model.md option values 261910000-261910005.
       Build verified: npm install + npm run build produce valid
       bundle.

       No deploy to PRI-Books-Dev (S10 first deploy). No app module
       changes. Toolchain scaffold only.
       "
    - Show commit message before committing.

D7. Push (after operator approval per usual cadence).

NOT IN SCOPE FOR S9:
- Status pill rendering logic (S10).
- Other PCF controls.
- Custom page scaffold (pages/).
- Solution registration of the PCF assembly.
- CI/CD pipeline changes.
- Dataverse schema changes.

CONSTRAINTS:
- ASCII-only PowerShell where any is authored.
- All file paths use forward slashes in commit messages (Windows OK
  in tool calls).
- One commit covers all S9 deliverables. Do not split.
- Show operator the diff before staging.

REPORTING:
- Brief output throughout.
- At end: confirm npm install + npm run build outputs; show pcf/
  tree; show commit message draft; await approval.

TIME BUDGET: 90-120 minutes including the initial reads. If pac pcf
init fails or npm install produces compilation errors that take more
than 15 minutes to debug, surface to operator and pause -- do not
chase Microsoft-side dependency issues alone.
```

## Subsequent S10 session

S10 implements StatusPill rendering. The S10 prompt is not drafted in
this artifact -- it will be written after S9 lands and S9's actual
output informs the S10 instruction.

Skeleton S10 spec for planning purposes:

- Read `rm_status` value from the bound column (the StatusPill is a
  field-scope PCF, so the value comes via the formatted-value
  context).
- Resolve to the 6 status labels (Draft / PendingApproval / Approved
  / Posted / Reversed / Voided).
- Render a Fluent UI v9 `Badge` (or equivalent) with background and
  foreground from the corresponding `--status-*-bg` / `--status-*-fg`
  CSS variables.
- Tabular numeric formatting NOT relevant (status is text, not
  numeric).
- Test plan: at least one render-per-status test (6 tests minimum)
  using Fluent UI's React testing utilities or
  @testing-library/react.
- Deployment: register the assembly with the DatastreamBooks
  solution; `pull-solution.ps1` to capture; commit + push triggers
  CI/CD deploy to PRI-Books-Dev.

S10 also tests the CI/CD pipeline's PCF-bundling path for the first
time. Worth allocating recovery time in the session budget for
pipeline-side surprises.

## Cross-references

- [`../docs/architecture/ui-styling.md`](../docs/architecture/ui-styling.md) -- CSS variable spec
- [`../docs/architecture/ui-sitemap.md`](../docs/architecture/ui-sitemap.md) -- where StatusPill will eventually surface
- [`../docs/architecture/data-model.md`](../docs/architecture/data-model.md) -- `rm_journalentry.rm_status` option values
- [`../docs/decisions/datastream-books-decisions.md`](../docs/decisions/datastream-books-decisions.md) section 60 (Fluent UI v9 + minimal custom layer)
- [`../docs/audits/audit-2026-05-21-evening.md`](../docs/audits/audit-2026-05-21-evening.md) -- audit context driving the §66 reaffirmation gate
- [`../AGENTS.md`](../AGENTS.md) Code Conventions TypeScript / React rules

## Change history

| Date | Change | Source |
|---|---|---|
| 2026-05-21 | Initial DRAFT during autonomous audit window. Gated by §66 reaffirmation for S4+. Modern pipeline + Fluent UI v9 per-control imports per Q9 answer from kickoff conversation. | Autonomous audit stretch artifact |
