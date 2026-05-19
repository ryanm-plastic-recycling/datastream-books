# ⛔ REFERENCE ONLY — NOT DATASTREAM BOOKS

This folder contains metadata from the **Datastream ERP** environment
(PRI-Datastream / pri-dev). It is committed here as a reference for
naming conventions, column patterns, and shared-master-data tables.

**These files do NOT describe the Datastream Books schema.**
**Do NOT implement these tables in Books.**
**Do NOT update or modify these files except to refresh from the
source ERP environment.**

---

## Why this is here

Datastream Books shares the `rm` publisher with Datastream ERP. That means
both solutions can reference each other's tables and the same `rm_` prefix
appears in both. Looking at how ERP tables are shaped — naming, column
count, picklist patterns, shared masters like `rm_customer` — is the
fastest way to keep Books consistent with the conventions Pam and the
operations team are already used to seeing.

For the patterns extracted from these files, see
[`../../architecture/erp-pattern-notes.md`](../../architecture/erp-pattern-notes.md).

## Files in this folder

| File | Source | Rows | Purpose |
|---|---|---|---|
| `tables.csv` | ERP metadata export, 2026-04-24 | 56 (incl. header) | All tables visible to the export account in PRI-Datastream (system + custom). |
| `columns.csv` | ERP metadata export, 2026-04-24 | 3,301 (incl. header) | Every column on every table above. Has `IsCustomAttribute` so custom columns can be filtered. |
| `relationships.csv` | ERP metadata export, 2026-04-24 | 2,267 (incl. header) | Lookup relationships (FK direction). Has `IsCustomRelationship`. |
| `dataverse_metadata_rm.sqlite` | Metadata snapshot, 2026-01-16 | n/a | SQLite snapshot of the same metadata. Convenient for SQL queries (`sqlite3 dataverse_metadata_rm.sqlite ".tables"`). Older than the CSVs by a quarter — refresh if you need it current. |

### About the row counts
- `tables.csv` includes ~10 system tables (activitypointer, annotation, etc.) plus all `rm_`-prefixed custom tables. A rough count of distinct `rm_`-prefixed tables in `columns.csv` is **46**; the upstream review noted **47** custom tables. The one-table delta likely reflects a config or definitionless table that exports no columns — confirm against the sqlite snapshot if exact accounting matters.

## Export source

- **Environment:** PRI-Datastream (production) — same publisher and prefix as `pri-dev`.
- **Tool:** `pac data export` + custom metadata serializer (the script is on a personal dev box, not in this repo).
- **Account:** ryanm@plastic-recycling.net at export time.

## Refresh policy

**As-needed, not scheduled.** Refresh this folder when:

1. The Books data model is materially evolving and you want to recheck ERP patterns
2. A new ERP table or column is added that Books will reference (e.g., a new
   field added to `rm_customer` that Books wants to read via Dataverse lookup)
3. Before any cross-solution integration work begins
4. Annually as part of the audit-prep cycle so the reference doesn't drift
   too far from production

The exporter command lives in the same private dev-box script. When this folder
is refreshed, **add a new dated entry to the table above** rather than silently
overwriting — that preserves the per-export history for anyone reading.

## What NOT to do with these files

- ❌ Do not copy table or column definitions into `solution/src/`. Books tables
  live in `solution/src/Entities/...` and get their own design pass.
- ❌ Do not run any SQL or `pac` command that mutates ERP based on what you
  see here. The CSVs are a snapshot, not a live source.
- ❌ Do not commit additional ERP exports without a date suffix or table-row
  count update — every refresh needs to be auditable.

## See also

- [`../../architecture/erp-pattern-notes.md`](../../architecture/erp-pattern-notes.md) — patterns extracted from these files
- [`../../decisions/datastream-books-decisions.md`](../../decisions/datastream-books-decisions.md) — why we share the `rm` publisher
- [`../../../AGENTS.md`](../../../AGENTS.md) §Reference Data — operating rules for this folder
