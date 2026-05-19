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

| Column | Type | Notes |
|---|---|---|
| `rm_entityid` | Unique Identifier (PK) | Used by every transactional table |
| `rm_name` | Single Line of Text | Display name |
| `rm_legalname` | Single Line of Text | Legal name as registered |
| `rm_ein` | Single Line of Text (encrypted) | Federal EIN — encrypted at rest |
| `rm_entitytype` | Choice | Operating / RealEstate / Holding / Other |
| `rm_stateofregistration` | Choice | US state |
| `rm_fiscalyearendmonth` | Whole Number | 1–12 |
| `rm_basecurrency` | Single Line of Text | ISO 4217; v1 = 'USD' |
| `rm_status` | Choice | Active / Inactive |
| `rm_isconsolidationtarget` | Yes/No | Eligible for the consolidation tool |

**Rationale:**
- `rm_ein` is encrypted because EINs are sensitive identifiers under our
  insurance and audit posture. Display-level masking is also enforced by
  field-level security in the role definitions.
- Fiscal year end is stored as a *month* (1–12) so we can compute period
  boundaries without storing a parallel calendar.

**Relationships:**
- `rm_entity` 1—* `rm_fiscalperiod`
- `rm_entity` 1—* `rm_journalentry`
- `rm_entity` 1—* `ledger.GeneralLedgerEntries` (logical FK from SQL to Dataverse)

### `rm_chartofaccount` (Dataverse)

Single COA structure shared across all entities. Per-entity activation via
`rm_chartofaccount_entityactivation` (intersect table) so each entity can
exclude accounts it does not use without forking the COA.

| Column | Type | Notes |
|---|---|---|
| `rm_chartofaccountid` | Unique Identifier (PK) | |
| `rm_accountcode` | Single Line of Text | Stable identifier visible to users (e.g., `1010`) |
| `rm_accountname` | Single Line of Text | Display name |
| `rm_accounttype` | Choice | Asset / Liability / Equity / Revenue / Expense |
| `rm_normalbalance` | Choice | Debit / Credit |
| `rm_iscashaccount` | Yes/No | Flagged to invoke wire-transfer SoD on postings |
| `rm_isintercompany` | Yes/No | Account used as IC clearing |
| `rm_iscontrolaccount` | Yes/No | Auto-managed by sub-ledger (AP, AR, FA) — no manual JE |
| `rm_status` | Choice | Active / Inactive |
| `rm_parentaccountid` | Lookup (rm_chartofaccount) | Optional rollup parent |

**Rationale:**
- `rm_iscashaccount`, `rm_isintercompany`, `rm_iscontrolaccount` are
  promoted to first-class flags because plugins make routing decisions
  based on them (e.g., manual JE to a cash account triggers Controller
  approval per the decision log §Approval Workflows).
- A hierarchical `rm_parentaccountid` supports financial-statement
  rollups without storing the rollup in a separate table.

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
                  ----< rm_chartofaccount_entityactivation (N) >---- rm_entity (1)
```

## Inter-Company JE Mechanics

When a user submits a JE flagged inter-company (`rm_intercompanyentityid` set):

1. The originating JE references the counter-entity.
2. On approval, the IC-pairing plugin generates a matched companion JE in
   the counter-entity. The two share an `rm_intercompanypairid` GUID.
3. Both JEs must use a configured IC clearing account (`rm_isintercompany = true`).
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
