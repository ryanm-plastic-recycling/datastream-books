# Datastream Books — Immutability Design

> Formal write-up of the immutability architecture. Cross-references the
> Decision Log "Immutability Architecture" sections A–K, expanded with the
> implementation specifics that exist now that we are in code.

## Why Immutability Is Architectural, Not Procedural

External auditors and our cyber-insurance posture both rely on the
financial record being **tamper-evident and tamper-resistant by
construction**, not by promise. The cost of "we promise not to edit the
ledger" is near-zero to audit. The cost of "every posted row is hash-
chained, the ledger table denies UPDATE/DELETE at the SQL role level, and
the writing principal is the only one granted INSERT" is a real control
auditors can test.

This document specifies *how* each immutability promise is enforced — what
code, what schema, what role.

---

## A. Append-Only Transaction Ledger

**Where it lives:** Azure SQL, `ledger.GeneralLedgerEntries`
([`../../azure-sql/migrations/V0002__general_ledger_entries.sql`](../../azure-sql/migrations/V0002__general_ledger_entries.sql)).

**How it is enforced:**

1. **Object-scoped DENY** on the table:
   ```sql
   DENY UPDATE     ON ledger.GeneralLedgerEntries TO public;
   DENY DELETE     ON ledger.GeneralLedgerEntries TO public;
   DENY REFERENCES ON ledger.GeneralLedgerEntries TO public;
   DENY ALTER      ON ledger.GeneralLedgerEntries TO public;
   ```
   DENY beats GRANT in SQL Server / Azure SQL. Even `db_owner` cannot
   UPDATE without first REVOKEing the DENY, which is itself a DDL event
   captured by Azure SQL auditing.

2. **INSERT is granted to a single role**, `rl_app_writer`. This role is
   held only by the Dataverse plugin's SQL service principal. Humans —
   including DBAs — do not hold `rl_app_writer`.

3. **Corrections are reversing entries, not edits.** The
   `PostJournalEntryPlugin` writes a new row whose `ReversesEntryId` points
   to the original. The original row is never modified.

**Testable by an auditor:** They run `SELECT name FROM sys.database_principals
WHERE name = 'rl_app_writer';` and confirm exactly the plugin SP is a member.
They attempt `UPDATE ledger.GeneralLedgerEntries SET DebitAmount = 0 WHERE 1=0;`
and see permission denied. They examine the deny grants via
`sys.database_permissions`.

---

## B. Hash-Chained Records

**Algorithm:** SHA-256 over a canonical, deterministic byte sequence:

```
RowHash = SHA256(canonical_payload || PreviousRowHash)
```

The **canonical_payload** is the row's content serialized as
length-prefixed UTF-8 fields in a fixed order. The order is frozen in the
plugin's `LedgerRowHasher` class — never change it without writing a
verification migration that proves chain continuity over a deterministic
re-hash of all prior rows.

**Field order:**
```
EntryUid, EntityId, JournalEntryId, JournalEntryLineId, LineNumber,
FiscalPeriodId, TransactionDate, PostedAtUtc, AccountId, AccountCode,
AccountName, DebitAmount, CreditAmount, CurrencyCode, Memo, SourceModule,
SourceDocumentRef, InterCompanyPairId, InterCompanyEntityId,
ReversesEntryId, PostedByUserId, PostedByPrincipalName, ApprovedByUserId,
ApprovedByPrincipalName, ApprovedAtUtc
```

**Genesis row:** The first row for a given `EntityId` has
`PreviousRowHash = 0x00...00` (32 zero bytes).

**Chain scope:** **Per-entity.** Each `EntityId` has its own independent
hash chain. We chose per-entity over a single global chain because:
- It allows bulk loading of historical Macola data per-entity without
  serializing all entities into a single insertion order.
- A corrupted or contested period in one entity does not invalidate the
  audit position of other entities.
- It still gives auditors a single, unbroken proof per legal entity, which
  is the audit boundary that matters.

**Verification:**
- Live: a nightly verification job (to be authored in V0003+) walks each
  entity's chain in `(EntityId, EntryId)` order and re-computes each
  `RowHash`. Mismatch triggers an alert and writes an integrity-check row
  to `audit.LedgerIntegrityCheckpoints`.
- On-demand: `verify-integrity.ps1` (planned) calls the same procedure.

**Where the hash is computed:** In the writing plugin, NOT in SQL. We
compute in C# so the same hashing code runs in unit tests against
synthetic rows — auditors can be shown that the verifier and the writer
agree on the canonical form.

---

## C. Server-Side Posting Enforcement

**Where it lives:** `DatastreamBooks.Plugins/Posting/PostJournalEntryPlugin.cs`
(to be implemented). Triggered by the `Post` custom action on `rm_journalentry`.

**What it validates before writing:**
- Source state is `Approved`
- Debits = Credits across all lines (within tolerance of 0.0001 — guards
  against floating-point if any code path uses doubles)
- All accounts are `Active` for the entity
- The fiscal period the transaction date resolves to is `Open` (not
  `Closed` or `Locked`) — checked for BOTH entities in an IC pair
- The user holds the `JE Post` role
- `ApprovedBy != PostedBy` (SoD)
- For wire JEs (any line touching a cash account flagged as wire-eligible):
  `WireInitiate != WireApprove`

**What it does atomically:**
1. Open a Dataverse transaction
2. Open a SQL transaction against `ledger.GeneralLedgerEntries`
3. For each line, compute `RowHash` chained on `PreviousRowHash` (last row
   for this EntityId)
4. INSERT each row into Azure SQL
5. Flip `rm_journalentry.rm_status` to `Posted`, stamp `rm_postedbyuserid`
   and `rm_postedatutc`
6. Commit both transactions; on any failure, both roll back

The plugin uses `IOrganizationService` for Dataverse and a managed-identity
or service-principal-bound `SqlConnection` for Azure SQL. The two-phase
nature is the reason we cannot use Power Automate for this — Flow does not
give us atomicity across the two stores.

---

## D. Period Locks at the Data Layer

**Where it lives:** `rm_fiscalperiod.rm_status` (Dataverse) +
`PostJournalEntryPlugin` (refuses to post to non-Open) +
`ClosePeriodPlugin` / `ReopenPeriodPlugin` (state transitions).

**State machine:**
```
   (new period)
        |
        v
       Open  ----close---->  Closed  ----lock---->  Locked  (terminal)
                                ^                  |
                                +---reopen---------+
                                  (Period Reopen role
                                   + audit event)
```

**Close attestation:** `ClosePeriodPlugin` computes a SHA-256 over a
canonical payload of `(EntityId, FiscalPeriodId, EndingBalances per
account)`, writes it to `rm_fiscalperiod.rm_closehashbinary`, and inserts
a row into `audit.PeriodCloseAttestation` (V0004 — planned) with the same
hash. This pins the financial statement state at the moment of close.

**Reopen audit trail:** Every reopen writes an explicit
`audit.AuditEvents` row (V0005 — planned) including the requester, the
elevated-role approver, and the business reason supplied. Reopens are
also bounded by a configured tenure (e.g., cannot reopen periods more
than 90 days old without a second approval).

---

## E. Segregation of Duties

See [`security-model.md`](security-model.md) and
[`../controls/sod-matrix.md`](../controls/sod-matrix.md).

The SoD invariants enforced in plugin code are listed in §C above. None
are honor-code; all are GUID comparisons inside the plugin.

---

## F. Comprehensive Audit Trail

Two redundant trails:

1. **Dataverse audit log** — enabled on every financial table, retention
   set to the maximum allowed by the environment SKU. The Dataverse audit
   log captures CRUD events at the platform layer.
2. **`audit.AuditEvents`** — Azure SQL append-only table (V0005 — planned).
   Plugins write here too, with richer semantic events than the
   platform-level Dataverse audit (e.g., "PostRejected because
   ApprovedBy == PostedBy"). The redundancy is intentional: an attacker
   would need to compromise both stores.

---

## G. Time-Bound, Signed Reports

`ReportSnapshots` table (V0006 — planned) stores closed-period reports
(BS, P&L, CashFlow, TB) as JSON blobs along with a SHA-256 of the
blob. When the period is closed, a row is written for each standard
report. Subsequent re-runs of the same period regenerate from live data
and produce the same hash — divergence is an integrity alert.

---

## H. Dev/Prod Separation

- Developers have **zero** direct production database access.
- All production changes flow through GitHub Actions → managed-solution
  import → publish. The deploy-prod workflow (to be authored) requires
  a Required Reviewer approval gate in GitHub.
- The `rl_admin` role on Azure SQL prod is held by named individuals only,
  reviewed quarterly. Plugin service principals hold `rl_app_writer`
  only — not `rl_admin`.

---

## I. Backup and Recovery

| Store | Backup | Retention |
|---|---|---|
| Dataverse (PRI-Books) | Microsoft-native automated backups + solution exports in git | Vendor-managed + 7 yr in git |
| Azure SQL | Point-in-Time Restore + Long-Term Retention | 7+ years (drives SKU choice) |
| SharePoint (documents) | Microsoft 365 retention policies | 7+ years |

DR procedures: [`../runbooks/disaster-recovery.md`](../runbooks/disaster-recovery.md)
(to be authored).

Recovery is tested annually — see [`../runbooks/data-recovery.md`](../runbooks/data-recovery.md)
(to be authored).

---

## J. Change Management Built In

The `ChangeRequest` Dataverse table is part of the app. Plugins on the
`ChangeRequest` entity enforce:
- `RequestedBy != ApprovedBy != AssignedTo`
- Required fields: business reason, desired outcome, acceptance criteria,
  risk assessment, rollback plan
- Multi-image attachment support via `ChangeRequestAttachment` (decision
  log §J + ChangeRequest design section)

Every production-affecting change has a permanent ChangeRequest row.
The deploy-prod workflow validates a CR reference is supplied in the
commit message or PR body before allowing deploy.

---

## K. AI's Role

AI (Claude Code) writes the plugin code that implements these controls,
generates the test cases proving they work, and assists with audit
narratives. AI does NOT make the system immutable — the architecture
above does. AI-generated code touching financial logic requires human
review before merge per the AGENTS.md operating principles.

---

## Auditor's Cheat Sheet

Questions an auditor will ask and where the answer is:

| Question | Answer |
|---|---|
| "Can someone edit a posted entry?" | No — DENY UPDATE on `ledger.GeneralLedgerEntries`. Demo: attempt UPDATE, see error. |
| "Can someone delete a posted entry?" | No — DENY DELETE. Same demo. |
| "How do I detect tampering?" | Hash chain. Nightly verification job; on-demand `verify-integrity.ps1`. |
| "Who can write to the ledger?" | One role (`rl_app_writer`) held by one service principal. Demo: `sys.database_role_members`. |
| "How do you handle corrections?" | Reversing entries. `ReversesEntryId` column. No edits, ever. |
| "How do you prevent posting to a closed period?" | `PostJournalEntryPlugin` checks `rm_fiscalperiod.rm_status` before writing. Closed periods rejected at the plugin. |
| "Can a closed period be reopened?" | Yes, with elevated role + audit event. Demo: try as non-`Period Reopen` user → denied. |
| "Can a locked period be reopened?" | No. Terminal state. Demo: try as any role → denied. |
| "Who approved this JE?" | Denormalized on the ledger row: `ApprovedByPrincipalName`, `ApprovedAtUtc`. |
| "How do you ensure same person didn't create and approve?" | `ApproveJournalEntryPlugin` enforces `CreatedBy != ApprovedBy`. Demo: try, see rejection. |

---

## Open Items

- V0003+ migrations for `LedgerIntegrityCheckpoints`, `PeriodCloseAttestation`,
  `AuditEvents`, `ReportSnapshots`
- Plugin implementations: `PostJournalEntryPlugin`, `ApproveJournalEntryPlugin`,
  `ClosePeriodPlugin`, `ReopenPeriodPlugin`, `ChangeRequestApprovalPlugin`
- Nightly hash-chain verification job
- DR runbook + annual restore test

## See Also

- [`data-model.md`](data-model.md)
- [`security-model.md`](security-model.md)
- [`../controls/sod-matrix.md`](../controls/sod-matrix.md)
- [`../controls/audit-controls.md`](../controls/audit-controls.md)
- [`../../azure-sql/migrations/V0002__general_ledger_entries.sql`](../../azure-sql/migrations/V0002__general_ledger_entries.sql)
- Decision log §A–K
