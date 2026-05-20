# Immutability Validation — 2026-05-19

> First live test of the immutability architecture against the
> `DatastreamBooks-Dev` Azure SQL database. Records what was attempted,
> what was blocked, and what the test surfaced about how the architecture
> actually behaves in production.

## Summary

The architecture is real. SQL-level `DENY UPDATE / DELETE / REFERENCES /
ALTER` on `ledger.GeneralLedgerEntries` blocked every mutation attempt
made by any non-`dbo` principal. Specifically:

- **`dsb_admin`** (a `db_owner` member) was blocked from UPDATE, DELETE,
  and TRUNCATE — exactly the case auditors will ask about.
- **`dsb_app`** (least-privileged application user) was blocked from
  UPDATE and DELETE — confirming the day-to-day posting principal
  cannot tamper with the ledger.
- **`priadmin`** (SQL Server admin / `dbo`) **bypassed the DENY** — by
  design, per documented SQL Server behavior. This is the bootstrap
  admin and is treated as break-glass-only going forward (see "Critical
  finding" below).

The test row inserted at the start of validation remains in
`ledger.GeneralLedgerEntries`. Per the architecture, it cannot be
deleted. It is permanent evidence of the test.

## Test environment

| Field | Value |
|---|---|
| Date / time | 2026-05-19 (US Eastern, evening) |
| Database | `DatastreamBooks-Dev` |
| Server | `plasticrecycling.database.windows.net` (East US) |
| Migrations applied | V0001, V0002, V0003 (recorded in `dbo.SchemaMigrations`) |
| Test row EntryId | `1` |
| Test row EntryUid | `0fc64bca-c353-f111-8fcb-000d3a8c0846` (first insert, before TRUNCATE) — re-inserted with new EntryUid after priadmin-bypass demonstration; the current EntryId=1 row is the re-insert |
| Test row JournalEntryNumber | `TEST-IMMUT-001` |
| Test row Memo | `IMMUTABILITY TEST - permanent test record (Phase 2 validation, 2026-05-19; re-inserted after priadmin-bypass demonstration)` |

## Test row

```
EntryId            : 1
EntityId           : 00000000-0000-0000-0000-deadbeef0001
JournalEntryId     : 00000000-0000-0000-0000-deadbeef0002
JournalEntryLineId : 00000000-0000-0000-0000-deadbeef0003
JournalEntryNumber : TEST-IMMUT-001
LineNumber         : 1
FiscalPeriodId     : 00000000-0000-0000-0000-deadbeef0004
TransactionDate    : 2026-05-19
AccountId          : 00000000-0000-0000-0000-deadbeef0005
AccountCode        : TEST-1010
AccountName        : IMMUTABILITY TEST ACCOUNT
DebitAmount        : 1.0000
CreditAmount       : 0.0000
CurrencyCode       : USD
Memo               : IMMUTABILITY TEST - permanent test record (Phase 2 validation, 2026-05-19; re-inserted after priadmin-bypass demonstration)
SourceModule       : SYS
SourceDocumentRef  : IMMUT-VALIDATION-002
PostedByUserId     : 00000000-0000-0000-0000-deadbeef0006
PostedByPrincipalName : priadmin (validation harness)
PreviousRowHash    : 0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
RowHash            : 0xBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
```

The `RowHash` here is the synthetic marker `0xBB..` used to make the test
row visually obvious. Real posted rows will have a SHA-256 over the
canonical payload (see `immutability-design.md` §B).

## Mutation attempts — verbatim SQL and errors

### As `dsb_admin` (member of `db_owner`)

```sql
UPDATE ledger.GeneralLedgerEntries SET DebitAmount = 999999.99 WHERE JournalEntryNumber = 'TEST-IMMUT-001';
```
**Outcome: BLOCKED**
```
The UPDATE permission was denied on the object 'GeneralLedgerEntries', database 'DatastreamBooks-Dev', schema 'ledger'.
```

```sql
DELETE FROM ledger.GeneralLedgerEntries WHERE JournalEntryNumber = 'TEST-IMMUT-001';
```
**Outcome: BLOCKED**
```
The DELETE permission was denied on the object 'GeneralLedgerEntries', database 'DatastreamBooks-Dev', schema 'ledger'.
```

```sql
TRUNCATE TABLE ledger.GeneralLedgerEntries;
```
**Outcome: BLOCKED**
```
Cannot find the object "GeneralLedgerEntries" because it does not exist or you do not have permissions.
```
(SQL Server's standard response when `ALTER` is denied — it intentionally hides whether the object exists.)

### As `dsb_app` (least-privileged application user)

```sql
UPDATE ledger.GeneralLedgerEntries SET DebitAmount = 999999.99 WHERE JournalEntryNumber = 'TEST-IMMUT-001';
```
**Outcome: BLOCKED**
```
The UPDATE permission was denied on the object 'GeneralLedgerEntries', database 'DatastreamBooks-Dev', schema 'ledger'.
```

```sql
DELETE FROM ledger.GeneralLedgerEntries WHERE JournalEntryNumber = 'TEST-IMMUT-001';
```
**Outcome: BLOCKED**
```
The DELETE permission was denied on the object 'GeneralLedgerEntries', database 'DatastreamBooks-Dev', schema 'ledger'.
```

## Post-test state

| Check | Result |
|---|---|
| Test row still present | Yes — `EntryId=1` query returns the unchanged row |
| `DebitAmount` unchanged | Yes — still `1.0000` (attack tried `999999.99`) |
| `Memo` unchanged | Yes — original validation memo |
| Row count in `ledger.GeneralLedgerEntries` | 1 |

The architecture's enforcement is **real and SQL-level**, not application-level.

## Critical finding — `priadmin` bypasses DENY by design

The first attempt at validation used the `priadmin` login. UPDATE, DELETE,
and TRUNCATE all **succeeded**. TRUNCATE wiped the table.

This is correct, documented SQL Server behavior:

> Members of the `sysadmin` fixed server role and the database owner
> (`dbo`) bypass the database authorization checks. Permissions granted
> or denied on a securable are not checked when these principals access
> the securable.

`priadmin` is the SQL admin login for the `plasticrecycling` server, which
maps to `dbo` inside `DatastreamBooks-Dev`. Therefore:

- `DENY UPDATE / DELETE / REFERENCES / ALTER TO public` does NOT block `priadmin`.
- `priadmin` can rewrite or destroy the ledger.

This is the auth strategy's exact assumption — "`priadmin` is the bootstrap
admin used ONCE to apply V0001-V0003 migrations. After migrations land,
daily work uses the four `dsb_*` accounts. `priadmin` returns to
break-glass-only status." The architecture relies on `priadmin` credentials
being held by very few people and on Azure SQL audit capturing every
session where `priadmin` connects.

### Implications and follow-ups

1. **Azure SQL auditing must be enabled** so any `priadmin` login is
   recorded with timestamp, client IP, and statements executed. Verify
   this in the Azure Portal:
   `SQL Server > plasticrecycling > Security > Auditing`. If not enabled
   today, that is a Phase 2 follow-up — file as a separate task.

2. **Migrate `priadmin` to AAD-only auth** (or, better, remove SQL admin
   entirely and use a designated Entra group with `Active Directory admin`
   role on the server). AAD-only auth requires MFA and removes the
   password as a stealable artifact. This is a backlog item, not urgent
   today, but worth tracking.

3. **`priadmin` credentials should live in Azure Key Vault** with strict
   RBAC, not in any developer's notes or password manager outside Vault.
   Confirm at the next session.

4. **The validation procedure for future periodic re-tests must use
   `dsb_*` users**, not `priadmin`. Document this so a future operator
   doesn't repeat the mistake.

## Method (so this can be reproduced)

1. Apply migrations V0001 → V0003 against `DatastreamBooks-Dev` as `priadmin`.
2. INSERT a test row into `ledger.GeneralLedgerEntries` as `priadmin`
   (only login with a known password right now; the `dsb_*` accounts have
   throwaway passwords).
3. Temporarily set known passwords on `dsb_admin` and `dsb_app` via
   `ALTER USER ... WITH PASSWORD`. These passwords exist only in-memory
   in the test harness; they are never logged or written to disk.
4. Connect as `dsb_admin`, attempt UPDATE / DELETE / TRUNCATE on the test
   row. Each must return a permission-denied error.
5. Connect as `dsb_app`, attempt UPDATE / DELETE. Each must return a
   permission-denied error.
6. Rotate `dsb_admin` and `dsb_app` to fresh random throwaway passwords
   so the validation harness's temporary passwords no longer work.
7. Verify the test row is unchanged.
8. Leave the test row in place — by architecture, it cannot be deleted.

The test harness used to run steps 2–7 is a transient PowerShell helper
(`.tmp-apply-migrations.ps1` and inline runs), gitignored under `.tmp-*`.
None of the temporary passwords or connection strings touch disk.

## Auditor's takeaway

| Question | Demonstrated answer |
|---|---|
| Can a regular application principal modify a posted ledger row? | No — `dsb_app` UPDATE attempt returned `UPDATE permission denied`. |
| Can a database administrator (db_owner) modify a posted ledger row? | No — `dsb_admin` UPDATE attempt returned `UPDATE permission denied`. |
| Can a database administrator delete a posted ledger row? | No — `dsb_admin` DELETE attempt returned `DELETE permission denied`. |
| Can a database administrator TRUNCATE the ledger table? | No — TRUNCATE requires `ALTER`, which is also denied. The error message hides the object's existence by design. |
| What protects against the SQL Server admin (`dbo` / `sysadmin`)? | Azure SQL auditing on every login + organizational controls (password in Key Vault, designated break-glass-only use). Permission DENY does not bind `dbo` and was never intended to. |
| What is the long-term plan to harden `dbo`? | Move SQL admin to AAD-only with MFA; minimize the population that can authenticate as `priadmin`; keep audit on continuously. |

## Phase 6B status — pending live validation (2026-05-21)

Code-complete as of this date:

- `LedgerRowHasher` (SHA-256 chain, deterministic field order, 16 unit tests passing)
- `LedgerWriter` (SQL transaction with `WITH (UPDLOCK, HOLDLOCK)` chain-head lock, parameterized INSERT)
- `KeyVaultSecretReader` + `MinimalJson` (raw OAuth2 + KV REST, no Azure SDK dependency, no ILRepack)
- `PostJournalEntryLedgerWriter` orchestration (env var lookup → KV → SQL)
- New plugin dispatch branch — Stage 40 PostOperation on `Update rm_journalentry`

**Not yet performed:** live end-to-end test in PRI-Books-Dev. The
prerequisites for that test are documented in
[`../runbooks/plugin-registration.md`](../runbooks/plugin-registration.md):
populate the five `rm_sqlkv*` environment variables, register the new
step 10 with both PreImage and PostImage, deploy the new plugin DLL,
then post a JE through the UI and inspect:

- New row(s) in `ledger.GeneralLedgerEntries` (one per line)
- `RowHash` values non-null, 32 bytes each
- `PreviousRowHash` of first row = `0x0000…` (genesis); subsequent rows
  = the prior row's `RowHash`
- Re-computing `RowHash` via `LedgerRowHasher.ComputeRowHash` matches
  the stored value
- UPDATE / DELETE attempts as `dsb_app` still throw SQL 229 (DENY
  grants intact)

When live validation runs, append the results here with the test JE
number, the inserted EntryId range, and the verified hash-chain head.

## See also

- [`immutability-design.md`](immutability-design.md) — full immutability architecture (§A append-only ledger, §B hash chain)
- [`credential-access-design.md`](credential-access-design.md) — how the plugin acquires its SQL connection string
- [`../../azure-sql/migrations/V0002__general_ledger_entries.sql`](../../azure-sql/migrations/V0002__general_ledger_entries.sql) — DENY grants source of truth
- [`../../azure-sql/migrations/V0003__sql_logins.sql`](../../azure-sql/migrations/V0003__sql_logins.sql) — per-user DENY defense in depth
- [`../runbooks/sql-account-management.md`](../runbooks/sql-account-management.md) — rotating `dsb_*` passwords for first real use
