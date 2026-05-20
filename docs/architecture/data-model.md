# Datastream Books — Data Model (v1 Core)

> Source-of-truth design document for the v1 core data model.
> Updated whenever a table or relationship changes. Decisions affecting the
> data model also get an entry in `docs/decisions/datastream-books-decisions.md`.

## Scope of This Document

The **core v1 data model** — the smallest set of Dataverse tables and one
Azure SQL append-only ledger table that together let us post and report a
journal entry across multiple legal entities. Everything else (AP, AR, FA,
bank rec, approvals) builds on top of this core.

Tables described here:

| Table | Store | Purpose |
|---|---|---|
| `rm_entity` | Dataverse | Legal entity master — every transactional row carries `rm_entityid` |
| `rm_chartofaccount` | Dataverse | Chart of accounts (shared structure, per-entity activation) |
| `rm_fiscalperiod` | Dataverse | Open / Closed / Locked periods per entity |
| `rm_journalentry` | Dataverse | Header — workflow, approval, pre-post editing |
| `rm_journalentryline` | Dataverse | Lines — debits/credits, pre-post editing |
| `ledger.GeneralLedgerEntries` | Azure SQL | Posted lines — immutable, hash-chained |

Beyond v1 (intentionally out of scope here): Vendor, Customer, Bill, Invoice,
Receipt, BankAccount, BankTransaction, FixedAsset, ChangeRequest,
ApprovalRequest, ApprovalPolicy.

## Design Principles

1. **Multi-entity from row zero.** Every transactional row in every table
   includes `rm_entityid`. Reports and queries always pre-filter by entity.
2. **Pre-post editing in Dataverse; post-only ledger in Azure SQL.** Journal
   entries live in Dataverse until "Post" is invoked; the plugin then writes
   each line atomically to `ledger.GeneralLedgerEntries` and locks the JE.
3. **Denormalization at posting time.** Posted-ledger rows include the
   account code/name and posting-user UPN at the time of post. Renames or
   COA restructures do not retroactively alter prior periods.
4. **USD only in v1, currency-aware schema.** A `CurrencyCode` column is
   present everywhere it might matter; constraints today force `'USD'`.
   No code path relies on the absence of the column.
5. **Inter-company explicit.** When a JE crosses entities, the plugin
   generates the counter-entry automatically and links the two rows via
   `InterCompanyPairId`. There is no implicit IC behavior.

## Tables

### `rm_entity` (Dataverse)

Legal-entity master. One row per legal entity (operating company, real-estate
holding, etc.). See executive questionnaire §1 — exact list pending Pam +
executive input.

As built in Phase 3 (2026-05-19):

| Column | Type | Notes |
|---|---|---|
| `rm_entityid` | Unique Identifier (PK) | Used by every transactional table |
| `rm_entityname` | Single Line of Text (200) | Primary display name (ApplicationRequired) |
| `rm_entitycode` | Single Line of Text (20) | Short stable code for documents/reports (ApplicationRequired) |
| `rm_entityshort` | Single Line of Text (30) | Abbreviated label for compact UI |
| `rm_entitydesc` | Multiple Lines of Text (1000) | Long description |
| `rm_ein` | Single Line of Text (20) | Federal EIN — field-level security applied via role definitions (see security-model.md). Not column-encrypted in Phase 3 build; revisit before adding any real entity data. |
| `rm_entitytype` | Choice | Operating / Real Estate / Holding / Other (ApplicationRequired) |
| `rm_entitytypename` | Virtual | Auto-generated label companion |
| `rm_stateregistration` | Single Line of Text (2) | Two-letter US state code |
| `rm_fiscalyearendmonth` | Whole Number (1–12) | Pairs with `rm_fiscalyearendday` to express month-day |
| `rm_fiscalyearendday` | Whole Number (1–31) | Pairs with `rm_fiscalyearendmonth` |
| `rm_isactive` | Yes/No (default Yes) | Separates active legal entities from historical ones |
| `rm_isactivename` | Virtual | Auto-generated label companion |

**Out of Phase 3 scope (deferred):** `rm_basecurrency`, `rm_isconsolidationtarget`, `rm_legalname` — these were on the original v1 wish list but were not part of the Phase 3 build. v1 is USD-only per the decision log so `rm_basecurrency` can wait; the others are nice-to-haves.

**Rationale:**
- **EIN storage.** `rm_ein` is a plain Single Line of Text in the Phase 3 build, not a column-encrypted attribute. Dataverse column encryption requires Premium tier and an explicit enablement step we have not yet done. Until that's set up, no real EIN values should be entered. Field-level security via the security role definitions provides read-restriction at the principal level.
- **Fiscal year end as month + day, not as a date.** Dataverse has no month-day-only column type. The only date types (DateOnly / DateTime) carry a calendar year. A sentinel-year approach (e.g., always store as 2024-12-31) requires every consumer to know the convention and ignore the year — easy to forget, and year-bound comparisons go subtly wrong. Two integers are unambiguous. Combination validation (e.g., 2-30 is invalid; 2-29 only valid in leap years) is enforced by a plugin in Phase 4, not by the column itself.

**Relationships:**
- `rm_entity` 1—* `rm_fiscalperiod`
- `rm_entity` 1—* `rm_journalentry`
- `rm_entity` 1—* `ledger.GeneralLedgerEntries` (logical FK from SQL to Dataverse)

### `rm_chartofaccount` (Dataverse)

Per-entity COA. Account numbers are **unique within an entity** (enforced
by an alternate key on `rm_entity` + `rm_accountnumber`) but not across
entities. Account categories and types are factored to their own master
tables (`rm_accounttype`, `rm_accountcategory`) for reporting symmetry
and to let auditors see "what is an Asset" as a first-class lookup.

As built in Phase 4 (2026-05-19):

| Column | Type | Notes |
|---|---|---|
| `rm_chartofaccountid` | Unique Identifier (PK) | |
| `rm_chartofaccountname` | Single Line of Text (200) | Primary display name — e.g., `Cash - Operating - Bank of Indiana` (ApplicationRequired) |
| `rm_accountnumber` | Single Line of Text (50) | Account number in the entity's numbering scheme. Searchable. Unique within an entity via alternate key (ApplicationRequired) |
| `rm_accountshort` | Single Line of Text (50) | Short display name for reports and compact UI |
| `rm_accountdesc` | Multiple Lines of Text (1000) | Long description |
| `rm_accounttype` | Lookup (rm_accounttype) | Asset / Liability / Equity / Revenue / Expense (ApplicationRequired) |
| `rm_accountcategory` | Lookup (rm_accountcategory) | Sub-category within the type (optional) |
| `rm_entity` | Lookup (rm_entity) | Owning legal entity (ApplicationRequired) |
| `rm_parentaccount` | Lookup (rm_chartofaccount) | Self-reference for hierarchical COA. NULL = top-level. |
| `rm_accountlevel` | Whole Number (0–10) | Depth in COA hierarchy. Maintained by plugin in a later phase; seeded with 0 for top-level, 1 for sub. |
| `rm_displayorder` | Whole Number (0–999999) | Optional sort key for COA display in reports. |
| `rm_isactive` | Yes/No (default Yes) | Soft-delete flag. Inactive accounts cannot be posted to but are retained for financial-history preservation. |
| `rm_iscashbankaccount` | Yes/No (default No) | Flags accounts representing physical cash or bank accounts; drives reconciliation routing and dual-approval policy on JEs to these accounts. |
| `rm_normalbalance` | Choice (Debit / Credit) | OPTIONAL override of the linked `rm_accounttype.rm_normalbalance`, stored locally for query efficiency. Set only for contra-accounts in the starter seed. Option values mirror `rm_accounttype.rm_normalbalance`: Debit=261910000, Credit=261910001. |
| `rm_normalbalancename` | Virtual | Auto-generated label companion for `rm_normalbalance` |
| `rm_currency` | Single Line of Text (3) | ISO 4217 code. USD only in v1; column present so multi-currency can be added without a schema change. Seed scripts populate 'USD' at insert time (Dataverse string columns do not accept a metadata-level default). |
| `rm_externalsystemid` | Single Line of Text (100) | Identifier from the source system during migration (e.g., the Macola account number) |

**Settings:**
- Ownership: Organization
- Audit: ON
- Alternate key: `rm_coa_entity_number_key` on (`rm_entity`, `rm_accountnumber`) — enforces account-number uniqueness within an entity

**Rationale for shape changes vs. earlier (Phase 1) draft:**
- The earlier draft used choice columns for `rm_accounttype` and `rm_status` and split `rm_iscashaccount`/`rm_isintercompany`/`rm_iscontrolaccount` as separate booleans. The as-built v2 promotes `rm_accounttype` and `rm_accountcategory` to lookups against their own master tables for reporting and audit symmetry, collapses status to a single `rm_isactive` boolean (Phase 1 ledger status is governed by `rm_fiscalperiod.rm_status` and JE-level state, not by COA status), and renames `rm_iscashaccount` to the more explicit `rm_iscashbankaccount`. `rm_isintercompany` and `rm_iscontrolaccount` are intentionally deferred until the AP/AR sub-ledgers land and we have concrete plugin behaviors to attach to those flags — premature flags rot.
- An explicit per-entity COA (account number unique within an entity) rather than a globally-shared COA: this matches Macola's behavior, lets entities maintain different numbering ranges, and removes the need for a separate `entityactivation` intersect table. The trade-off is that consolidation reports must group by account-category or type rather than by account number. Acceptable; the category/type lookup gives us the grouping key.
- `rm_normalbalance` stored locally on the row (rather than always joined through `rm_accounttype`) is a denormalization for query efficiency — financial reports filter by Debit-vs-Credit constantly. The value defaults to the type's normal balance at row creation, with the seed script explicitly setting it for contra-accounts (Accumulated Depreciation, Allowance for Doubtful Accounts, Sales Returns).
- `rm_accountnumberingscheme` (proposed as an optional companion table for documenting per-entity number ranges) was **deliberately skipped** this phase. It would be soft-validation data with no immediate consumer; adding it now creates a table with one row that nothing reads. It will be introduced when the COA validation plugin lands (likely a later phase), at which point the plugin is the natural consumer of the range definitions.

### `rm_fiscalperiod` (Dataverse)

One row per (Entity, Period). Period = a month for most entities; quarterly
or 13-period schedules are supported by setting `rm_periodlength`.

| Column | Type | Notes |
|---|---|---|
| `rm_fiscalperiodid` | Unique Identifier (PK) | |
| `rm_entityid` | Lookup (rm_entity) | Required |
| `rm_periodname` | Single Line of Text | e.g., `2026-05`, `FY26 Q2` |
| `rm_startdate` | Date | Inclusive |
| `rm_enddate` | Date | Inclusive |
| `rm_status` | Choice | Open / Closed / Locked |
| `rm_closedbyuserid` | Lookup (systemuser) | Set by the period-close plugin |
| `rm_closedatutc` | Date and Time | Set by the period-close plugin |
| `rm_closehashbinary` | File | SHA-256 of close attestation payload (see immutability-design.md §G) |

**Rationale for `rm_status` as a choice with three values:**
- `Open` — postings allowed
- `Closed` — postings denied; reopening allowed via elevated role + audit event
- `Locked` — postings denied; reopening NOT allowed (terminal state, e.g.,
  after annual audit signoff)

The period-close plugin transitions `Open` → `Closed` and writes the close
hash. A separate "lock" action transitions `Closed` → `Locked` and is
irreversible.

### `rm_journalentry` (Dataverse)

JE header. Holds workflow state (Draft, PendingApproval, Approved, Posted,
Reversed, Voided).

| Column | Type | Notes |
|---|---|---|
| `rm_journalentryid` | Unique Identifier (PK) | |
| `rm_entityid` | Lookup (rm_entity) | Required |
| `rm_journalentrynumber` | Single Line of Text | Auto-numbered: `JE-{Entity short code}-{YYYY}-{NNNNN}` |
| `rm_transactiondate` | Date | Business effective date |
| `rm_fiscalperiodid` | Lookup (rm_fiscalperiod) | Computed from `rm_transactiondate` + entity |
| `rm_memo` | Multiple Lines of Text | Header memo |
| `rm_sourcemodule` | Choice | GL / AP / AR / FA / BANK / SYS |
| `rm_status` | Choice | Draft / PendingApproval / Approved / Posted / Reversed / Voided |
| `rm_createdbyuserid` | Lookup (systemuser) | Set on create |
| `rm_approvedbyuserid` | Lookup (systemuser) | SoD: must differ from creator if approval required |
| `rm_postedbyuserid` | Lookup (systemuser) | SoD: must differ from approver |
| `rm_postedatutc` | Date and Time | Set by posting plugin |
| `rm_reversesjournalentryid` | Lookup (rm_journalentry) | If this JE is a reversal |
| `rm_intercompanyentityid` | Lookup (rm_entity) | If inter-company; identifies the counter-entity |

**Rationale:**
- Three person-fields (`Created`, `Approved`, `Posted`) so the plugin can
  enforce strict SoD per the role list in the decision log.
- `rm_status` is the gate to ledger writes — only `Approved` → `Posted` is
  legal. The posting plugin rejects writes for any other source state.

### `rm_journalentryline` (Dataverse)

JE lines. Editable while the parent JE is `Draft` / `PendingApproval`; locked
once the JE is `Posted`.

| Column | Type | Notes |
|---|---|---|
| `rm_journalentrylineid` | Unique Identifier (PK) | |
| `rm_journalentryid` | Lookup (rm_journalentry) | Cascade-delete from header (only while Draft) |
| `rm_linenumber` | Whole Number | 1-based, contiguous (plugin enforces) |
| `rm_accountid` | Lookup (rm_chartofaccount) | |
| `rm_debitamount` | Currency | One of debit/credit is non-zero; both >= 0 |
| `rm_creditamount` | Currency | |
| `rm_memo` | Single Line of Text | Line-level memo |
| `rm_sourcedocumentref` | Single Line of Text | Free-form ref to source doc |

**Constraint enforced by plugin (not by Dataverse alone):**
- Sum of debits = sum of credits per JE
- Every line's account is `rm_status = Active` for the JE's entity
- Inter-company JEs have matched IC pair across entities

### `ledger.GeneralLedgerEntries` (Azure SQL)

The immutable posted ledger. Schema, indexes, and DENY grants live in
[`azure-sql/migrations/V0002__general_ledger_entries.sql`](../../azure-sql/migrations/V0002__general_ledger_entries.sql).
Summary here for the data model overview:

- One row per posted JE line
- Append-only (DENY UPDATE/DELETE/REFERENCES on `public`)
- Hash-chained per-entity (independent chain per `EntityId`)
- Denormalized: account code/name, posting-user UPN, approval metadata all
  captured at post time
- Reversal linkage via `ReversesEntryId` (forward pointer only — see V0002
  for the "no UPDATE of the original" treatment)

See [`immutability-design.md`](immutability-design.md) for the full
immutability specification.

## Relationship Diagram (text form)

```
rm_entity (1) ----< rm_fiscalperiod (N)
   |
   +---< rm_journalentry (N) ----< rm_journalentryline (N)
                |                          |
                +-- on Post -->            +-- on Post -->
                                           ledger.GeneralLedgerEntries (N)
                                                 |
                                                 +-- ReversesEntryId
                                                 |   (self-reference)
                                                 |
                                                 +-- per-entity hash chain
                                                     (RowHash, PreviousRowHash)

rm_chartofaccount (1) ----< rm_journalentryline (N)
                  ----< ledger.GeneralLedgerEntries (N)
```

## Inter-Company JE Mechanics

When a user submits a JE flagged inter-company (`rm_intercompanyentityid` set):

1. The originating JE references the counter-entity.
2. On approval, the IC-pairing plugin generates a matched companion JE in
   the counter-entity. The two share an `rm_intercompanypairid` GUID.
3. Both JEs must use a configured IC clearing account. IC clearing accounts will be identified by naming convention or a future flag (rm_isintercompany was deferred per Phase 4 — flag will be added when AP/AR sub-ledgers land and concrete IC plugin behavior is defined).
4. On Post, both ledger rows are written within the same transaction; they
   share `InterCompanyPairId`. Period-locks are checked for both entities
   before write — a closed counter-period blocks the original post.

Consolidation later nets out balances by `InterCompanyPairId`.

## Open Questions Affecting the Data Model

These are tracked in `docs/memos/executive-questionnaire.md` and must be
resolved before the data model can be considered final:

- §1.1–1.5: Legal entity list, EINs, parent/subsidiary structure, audit scope
- §3.1–3.3: Approval thresholds (drives `ApprovalPolicy` rows, not the schema)
- §11.1–11.3: COA structure and owner — affects the seed data, not the table shape
- §12.1–12.3: Reporting requirements that may force additional denormalized fields
  on posted ledger rows

## See Also

- [`security-model.md`](security-model.md) — roles, SoD matrix, field-level security
- [`immutability-design.md`](immutability-design.md) — full immutability architecture
- [`../controls/sod-matrix.md`](../controls/sod-matrix.md) — operational SoD matrix
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) — decision context
