# Session Log — 2026-05-19 — Phase 1 Foundation Closed

> Working notes for the day Phase 1 was completed and Phase 2 prep began.
> Session log format: what got done, what went wrong, what we learned, what's next.

## Summary

Phase 1 (design sprint + repo setup) is **complete**. The repository now
contains a working Dataverse solution scaffold with the verified ERP-matching
publisher, plugin and test project scaffolding, the first two SQL migrations
(including the immutability backbone `ledger.GeneralLedgerEntries`), a
deploy-to-dev workflow stub, foundational PowerShell scripts, and a full
architecture/controls doc set. A second housekeeping pass today imported
ERP metadata as reference material, captured the patterns Books will follow,
and updated AGENTS.md with a Reference Data section.

Phase 2 begins next session: Azure SQL provisioning and the first real
Dataverse tables informed by the ERP patterns we just documented.

## What Was Completed (Phase 1 + today's housekeeping)

### Earlier in Phase 1 (squashed into 5b25157 / bfde747 on main)

- Repo skeleton: every folder from `docs/repo-structure.md` exists,
  empty ones have `.gitkeep`.
- Dataverse solution initialized:
  `Datastream Books` (display) / `DatastreamBooks` (unique) under publisher
  `Ryan McCauley` / `RyanMcCauley` with prefix `rm` and option-value
  prefix `12619` — all values **verified** against the live PRI-Datastream
  solution by exporting it from pri-dev and reading Solution.xml.
- Plugin projects scaffolded: `DatastreamBooks.Plugins` (net462) and
  `DatastreamBooks.Plugins.Tests` (net48, xUnit + FluentAssertions +
  FakeXrmEasy v2). Subfolders mirror the planned plugin domains
  (Posting, Validation, PeriodLock, Immutability, etc.). No plugin
  business logic yet — scaffolding only.
- SQL migrations:
  - `V0001__initial_schema.sql` — header-only placeholder for schemas/roles
  - `V0002__general_ledger_entries.sql` — full draft of the append-only
    hash-chained ledger table, including `DENY UPDATE/DELETE/REFERENCES/ALTER`
    on `public`, per-entity hash chain columns, indexes, extended properties,
    and a full rollback-notes block
- GitHub Actions `deploy-dev.yml` stubbed: build + test plugin, pack + import
  solution to PRI-Books-Dev, all required secrets and variables documented
  in the workflow header.
- PowerShell scripts: `setup-dev.ps1`, `auth-env.ps1`, `pull-solution.ps1`,
  `push-solution.ps1`, `run-sql-migration.ps1` (stub).
- Architecture docs: `data-model.md`, `security-model.md`, `immutability-design.md`.
- Controls docs: `sod-matrix.md`, `approval-policies.md`, `audit-controls.md`.
- Decision-log sweep: every CFO reference replaced with President /
  Executive Sponsor (President).
- New principle added to AGENTS.md: **"Verification is mandatory, not
  optional"** — codified after a real session incident where assumed
  publisher values (`dsb` prefix) had to be reverted to the verified
  ERP-matching values (`rm` prefix).

### Today's housekeeping (this commit batch)

- Created `docs/session-logs/` and authored this file.
- Updated README Project Status: Phase 1 complete; Phase 2 focus = Azure SQL
  provisioning + first real Dataverse tables.
- Added a decision-log change-log entry for today's events.
- Created `docs/reference/erp-metadata/` and imported four files from the
  Downloads staging area: `tables.csv`, `columns.csv`, `relationships.csv`,
  `dataverse_metadata_rm.sqlite`.
- Wrote `docs/reference/erp-metadata/README.md` (REFERENCE ONLY warning,
  export date, refresh policy, file inventory). The user pre-staged a
  minimal version; this expanded it.
- Wrote `docs/architecture/erp-pattern-notes.md` capturing six patterns
  Books should follow from ERP (4-column master data, picklist+virtual,
  `rm_customer` shared master, table naming, column naming, many-small
  tables).
- Added a Reference Data section to AGENTS.md pointing at the new folder
  and the rules for using it.

## The Merge Drama (and how it resolved)

After Phase 1's local commits, things got tangled. Two commits with the
same title appear on main:

```
030a5d0  Add branching policy to AGENTS.md
235589d  Add branching policy to AGENTS.md
```

What actually happened:

1. **235589d** added the "Branching Policy" section at the *bottom* of
   AGENTS.md (after Key Principles to Remember).
2. **030a5d0** moved that same section to the *top* of AGENTS.md (right
   after Project Context). The diff shows 10 insertions + 10 deletions —
   pure reorder, no content change.
3. **699a67d** is a merge commit reconciling local main with remote main
   that had diverged.

Net result: the branching policy sits where it should (near the top,
visible to anyone reading AGENTS.md for the first time), but the git log
looks like there are duplicate commits. Functionally fine; cosmetically
confusing.

The squash of Phase 1 work into a single commit (`bfde747`) was also part
of this — the 10-commit working sequence from the worktree session was
combined into one summary commit on main. That's appropriate for a solo
project but loses some of the per-step granularity.

### Branching Policy Decision (formalized today, codified at commit 030a5d0)

**Solo-developer project. Work directly on main. No feature branches. No
git worktrees.** Commit in logical chunks; push to main; multiple commits
per session is fine. The policy is now in AGENTS.md right after Project
Context so it can't be missed.

Why this policy now:
- The Phase 1 worktree session generated 10 commits on a feature branch
  that then needed to be reconciled with main edits the user made directly
  on the PC. The reconciliation was the source of the "merge drama."
- For a solo project, branches add overhead without adding parallelism
  (there's no one else to coordinate with) and they create OneDrive
  conflict surface.
- Pull requests are still possible if we ever want one for an external
  audit reviewer to comment on — the policy doesn't forbid them, just
  argues against them as the default.

## OneDrive Lessons Learned

The repository lives inside a OneDrive-synced folder. That's a known
foot-gun and we tripped over it. Concrete lessons:

1. **OneDrive + active git operations = race conditions.** When git is
   writing to `.git/` while OneDrive is trying to sync, you can get
   half-uploaded files, file-locked errors, and the dreaded `filename (1)`
   duplicate pattern. The Downloads folder shown earlier in this session
   has multiple `001_create_table.sql.txt`, `... (1).txt`, `... (2).txt`
   files — that's the OneDrive symptom set.

2. **Pause OneDrive sync before any heavy git/repo work.** Today's
   housekeeping is happening with sync paused for 8 hours, and that's
   the right default. Resume after the session and let OneDrive catch
   up uninterrupted.

3. **Worktrees inside a OneDrive-synced repo amplify the problem.** The
   Phase 1 worktree at `.claude/worktrees/elastic-boyd-219f6c/` lived
   inside the synced tree. Every file written by the agent triggered a
   sync attempt, slowing the agent and creating partial-file states.
   Combined with the branching policy decision: **no worktrees from
   here on**.

4. **When OneDrive throws conflict files, resolve them at the OS level
   before continuing in git.** Don't try to `git add` your way through
   a `filename (1).cs` situation; delete the duplicates first.

5. **The repo location itself is on OneDrive and that's not changing
   today.** Long-term, moving the repo to a non-synced path
   (`C:\Code\datastream-books`) would eliminate the foot-gun entirely.
   Filed mentally as a future task; not urgent.

## What's Next

### Immediate (next session)

1. **Provision Azure SQL Dev database.**
   - Single Azure SQL Database, Basic or S0 tier (~$5–15/mo)
   - Dedicated resource group `rg-datastream-books-dev`
   - AAD-only auth
   - Enable LTR for the eventual prod move (dev doesn't need it strictly,
     but setting it now means the IaC template is real)
   - Configure secrets for the deploy-dev workflow once the DB exists

2. **First real Dataverse tables** (informed by ERP pattern notes):
   - `rm_entity` — legal entity master. Adopt the 4-column master pattern
     (`rm_entityname`, `rm_entitycode`, `rm_entityshort`, plus the EIN,
     fiscal-year-end-month, base-currency, status columns from `data-model.md`).
   - `rm_fiscalperiod` — period master with Open/Closed/Locked status.
   - Possibly `rm_chartofaccount` — but only after Pam reviews the
     starter COA proposal (executive questionnaire §11).

3. **Apply V0001 to the new Azure SQL DB once provisioned.** Will need
   to flesh out V0001 first (schemas, roles, SchemaMigrations metadata
   table) — today it's still a header-only placeholder.

### Soon (next 1–2 sessions)

4. **Set up the Entra app registration for GitHub Actions** and add
   federated identity for this repo. Flip `USE_OIDC=true` in the workflow.
5. **First plugin: `PostJournalEntryPlugin`.** Implements the two-phase
   Dataverse + Azure SQL commit and the per-entity hash chain. This is
   where the immutability architecture goes from doc to code.
6. **Pam's weekly check-in starts.** First agenda: legal entity list
   (executive questionnaire §1), starter COA review (§11).

### Open Questions Still Outstanding

- Legal entity inventory (exec questionnaire §1.1–1.5)
- Approval thresholds (§3.1–3.3) — must be set before approval-policy
  rows can be seeded
- COA owner / signoff path (§11.3)
- Cyber-insurance control requirements (§9.1) — may bind us tighter than
  the controls already designed
- Whether ERP's existing tables will need new columns to support Books
  AR/AP integration (e.g., credit-limit columns on `rm_customer`)

## Risks Discovered This Session

- **ERP metadata snapshot is from 2026-04-24** (CSVs) and 2026-01-16
  (sqlite). The CSVs are fresh enough but the sqlite is a quarter old —
  if any Books design decision hinges on the exact ERP schema today,
  refresh first. Refresh policy is documented in
  `docs/reference/erp-metadata/README.md`.
- **One-table discrepancy:** upstream review noted 47 custom `rm_` tables
  in ERP; this export shows 46 distinct in `columns.csv`. Probably a
  config table with no columns, but worth confirming if exact accounting
  matters.

## Decisions Captured

| # | Decision | Where logged |
|---|---|---|
| New | Solo-dev project; work on main; no branches; no worktrees | AGENTS.md §Branching Policy (committed 030a5d0) + this log |
| New | ERP metadata committed as reference under `docs/reference/erp-metadata/`, not implemented in Books | AGENTS.md §Reference Data + `erp-metadata/README.md` + this log |
| New | Books adopts ERP's 4-column master data pattern, picklist+virtual usage, table/column naming conventions where reasonable | `docs/architecture/erp-pattern-notes.md` + this log |
| New | Books AR references `rm_customer` from ERP; does not duplicate it | `erp-pattern-notes.md` Pattern 3 + this log |

## See Also

- `docs/decisions/datastream-books-decisions.md` — formal decision log (see today's change-log entry)
- `docs/architecture/erp-pattern-notes.md` — patterns ERP teaches us
- `docs/reference/erp-metadata/README.md` — what's in the ERP snapshot, how to refresh
- `AGENTS.md` §Reference Data, §Branching Policy
