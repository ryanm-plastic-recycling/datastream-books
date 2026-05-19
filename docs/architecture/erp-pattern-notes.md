# ERP Pattern Notes — Conventions We Inherit from PRI-Datastream

> Patterns observed in the live Datastream ERP solution that Datastream Books
> should follow for consistency. The ERP metadata snapshot these patterns
> were derived from is at
> [`../reference/erp-metadata/`](../reference/erp-metadata/) (REFERENCE ONLY).

## Why This Document Exists

Books shares the `rm` publisher with Datastream ERP. Users, operations, and
the eventual auditor will see one logical system whose tables happen to be
spread across two solutions. The faster Books adopts ERP's conventions, the
less cognitive load there is for everyone — including for queries that join
across both solutions later (e.g., AR pulling shared `rm_customer` records).

These are **conventions**, not laws. Where a Books-specific concern argues
for diverging, divergence is fine — but document the rationale in the
decision log and in the table-specific design.

## Headline Stats (ERP solution)

| Stat | Value | Source |
|---|---|---|
| Custom (`rm_`) tables in ERP | **~47** (review summary); 46 visible in this export | upstream review; `columns.csv` distinct table count |
| Median column count per custom table | **~10** | upstream review |
| Total relationships in ERP | 2,266 | `relationships.csv` |
| Total columns across all visible tables | 3,300 | `columns.csv` |

The "median 10 columns" is striking: ERP optimizes for many small, focused
tables rather than a few wide ones. Books should follow the same instinct
— if a Books table is creeping past 15 columns, that's a signal to ask
whether two tables would be clearer.

## Pattern 1 — The "4-column master data" shape

Most reference / master tables in ERP follow this exact column set:

| Column | Type | Role |
|---|---|---|
| `rm_<entity>name` | Single Line of Text | The primary name (also the table's `PrimaryName`). What users see in lookups. |
| `rm_<entity>code` | Single Line of Text | The stable business identifier. What appears on documents, labels, integrations. Unique. |
| `rm_<entity>short` | Single Line of Text | A short form for compact UI (grids, drop-downs in tight spaces). |
| `rm_<entity>description` | Multiple Lines of Text | Free-form description. Optional. |

**Examples in ERP:** `rm_color`, `rm_format`, `rm_machine`, `rm_plant`,
`rm_process`, `rm_qualitystatus`, `rm_storagerow`, `rm_transporttype`
(see `columns.csv`).

**Books should follow this for any of its own master data:**
- `rm_chartofaccount` already uses `rm_accountname` + `rm_accountcode` —
  we're naturally compatible. Adding `rm_accountshort` (for compact COA
  pickers) and the existing `rm_accountname`/`rm_accountcode` keeps us
  aligned with the convention without disrupting the data model already
  documented in [`data-model.md`](data-model.md).
- `rm_entity` (legal entities) should pick up `rm_entitycode` and
  `rm_entityshort` if not already present.

**Naming rule:** Primary name field is `rm_<entity>name` (full singular noun
of the table after the prefix), *not* `rm_name`. ERP makes the entity
explicit so a query like `SELECT rm_machinename FROM ...` reads
unambiguously next to other tables in the same query.

## Pattern 2 — Picklist + Virtual companion

Every choice/picklist column in ERP is paired with a `*Virtual` column
of the same base name plus a name-suffix. The platform auto-generates the
virtual column to hold the *label text* (not the value) for reporting.

**Example from `columns.csv`:**
```
activitytypecode      EntityName     <- the actual choice value
activitytypecodename  Virtual        <- the auto-generated label companion
```

**Why this matters for Books:**
- Reports that need the *label* (e.g., "Account Type = Asset" in a balance
  sheet column header) should pull from the `*name` virtual column, not
  from an option-set value join. The platform keeps it current.
- Plugins reading choice values should still use the canonical column;
  only reporting and UI should pull from the virtual.
- Do NOT manually create the virtual column — Dataverse creates it
  automatically when you create the choice column. Just be aware it
  exists.

## Pattern 3 — `rm_customer` is the shared customer master

ERP defines `rm_customer` with its own customer-master semantics (sites,
contacts, etc.). **Books AR should reference `rm_customer` by lookup
rather than create a parallel customer table.** The same goes for
related ERP tables:

| ERP table | Books should reference, not duplicate |
|---|---|
| `rm_customer` | Yes — AR invoices, receipts, agings all point here |
| `rm_customercontact` | Yes — AR communications |
| `rm_sitecustomer` | Yes — site-level customer relationship |
| `rm_pricontact` | Maybe — depends on whether the contact carries finance-relevant fields |

What this avoids:
- Two customer masters drifting out of sync
- Reconciliation work between AR and ops every month
- Auditor questions like "which one is the real customer record?"

What this requires:
- A clear ownership boundary: ERP owns the master; Books reads. Edits to
  customer-master fields happen in ERP UI only.
- A read-tolerant relationship: a Books AR record cannot break if ERP
  reorganizes the customer master schema. Use lookups, not denormalized
  copies of customer fields (with the exception of denormalization at
  ledger post-time, which is intentional for audit immutability — see
  [`immutability-design.md`](immutability-design.md) §B).

There may be **no analogous shared vendor master** in ERP today — vendors
are tracked operationally elsewhere. Books will likely need to own the
vendor master (per `docs/decisions/datastream-books-decisions.md` §22:
"Vendors/customers added as needed"). Confirm before AP design lands.

## Pattern 4 — Table naming: singular nouns, lowercase, no separators

ERP table logical names (from `tables.csv`):

```
rm_color, rm_machine, rm_plant, rm_process, rm_customer, rm_lot,
rm_productionrun, rm_qualitycontrol, rm_runinput, rm_runoutput,
rm_transportation, rm_storagerow, rm_motivevehicle
```

Rules in evidence:
- **Singular**, not plural. `rm_color`, not `rm_colors`. The EntitySet
  (plural collection name) is what gets pluralized — that's a separate field.
- **Lowercase only.** `rm_productionrun`, not `rm_ProductionRun`. (This
  matches the Dataverse storage convention and AGENTS.md naming guidance.)
- **No underscores, hyphens, or camelCase between words.** Multi-word
  table names just concatenate: `rm_productionrun`, `rm_storagerow`,
  `rm_motivevehicle`. The SchemaName casing column shows that Display
  Names use word breaks ("Production Run") but the logical name does not.

Books has been following this already (`rm_journalentry`,
`rm_journalentryline`, `rm_chartofaccount`, `rm_fiscalperiod`). Keep it.

## Pattern 5 — Column naming follows the table prefix

Every custom column in ERP is `rm_<table-stem><columnname>`. The table-stem
prefix on the column makes it readable even outside its table context:

```
rm_machinename       <- on rm_machine
rm_machinecode       <- on rm_machine
rm_productionrunlot  <- on rm_productionrun (FK to rm_lot)
rm_customercontactid <- primary key of rm_customercontact
```

This is **stricter than the default Dataverse convention** (which would
allow `rm_name` on every table). The ERP team chose the more verbose
pattern; Books should match.

## Pattern 6 — Many small tables over a few wide ones

The median 10-column count means ERP splits concepts aggressively. Some
examples worth modeling on:

- `rm_format`, `rm_formatsub`, `rm_formattype` — three tables representing
  a hierarchy that could have been one table with two enum columns.
- `rm_color`, `rm_colorsub` — same.
- `rm_lot`, `rm_lotgenealogy` — separating the "what" from the "how it
  was made" history.

For Books, this argues:
- Don't put approval workflow state on `rm_journalentry` itself — already
  factored out as `rm_approvalrequest`/`rm_approvalpolicy` per the
  decision log.
- Don't put report-snapshot data on `rm_fiscalperiod` — factor to
  `ReportSnapshots` (already planned for V0006).
- Whenever you find yourself adding a 6th boolean flag to a row, ask if
  the row should split.

## What Books Does Differently (and Why)

Worth documenting up front so we don't get accused of inconsistency later:

| Books does | ERP does | Why |
|---|---|---|
| Stores posted ledger rows in Azure SQL with hash chain | All data in Dataverse | Audit-defensible immutability requires DENY UPDATE/DELETE at the SQL role level — Dataverse alone doesn't give us that |
| Two-phase commit Dataverse + Azure SQL on Post | Single-store transactions | Same reason — Books is hybrid by design |
| Denormalizes account code/name onto posted ledger rows | Lookups for everything | Posted rows must survive renames of the master record (audit boundary) |
| Per-entity hash chains | n/a | Books-specific to immutability |

These are deliberate divergences, not pattern violations.

## Open Questions Affecting Pattern Adoption

1. **`rm_<entity>short` adoption in Books:** Worth adding to existing
   Books tables (`rm_chartofaccount`, `rm_entity`)? Recommend yes; cost
   is low and grid UI benefits.
2. **Vendor master ownership:** Does ERP own it, or does Books? Today the
   answer is Books (per decision §22), but if any ERP-side vendor metadata
   exists we should resolve before AP design.
3. **`rm_customer` field-level security:** Books reading customer
   financial data (credit limits, payment terms) — are those columns
   exposed in ERP today, or do we need to add them to the ERP solution
   first (with their owner's approval)?

## See also

- [`../reference/erp-metadata/`](../reference/erp-metadata/) — the raw export this is based on
- [`../reference/erp-metadata/README.md`](../reference/erp-metadata/README.md) — refresh policy, file inventory
- [`data-model.md`](data-model.md) — Books v1 core data model
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) — decision context
- [`../../AGENTS.md`](../../AGENTS.md) — naming conventions Books follows
