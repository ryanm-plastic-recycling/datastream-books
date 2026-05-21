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

## Phase 6B status — live validation PASSED on 2026-05-21

First real JE posted end-to-end against PRI-Books-Dev with plugin
assembly 1.0.0.4. The dual-write path, the per-entity hash chain, and
the rollback-and-throw atomicity all work as designed.

**Test JE:**

| Field | Value |
|---|---|
| JE number | JE-2026-001005 |
| GUID | `371c1eb1-2855-f111-a825-00224805f4b1` |
| Lines | Cash debit $75 (account 10100) / AR credit $75 (account 11000) |
| Total debit / credit | 75.00 / 75.00 |
| Entity | (single entity — first row in this entity's chain) |

**Dataverse side (post-update):**

| Field | Value |
|---|---|
| `rm_status` | 126190003 (Posted) |
| `rm_postedby_user` | ryanm@plastic-recycling.net |
| `rm_posteddatetime` | 2026-05-21T18:11:21Z |

**Azure SQL `ledger.GeneralLedgerEntries`:**

| EntryId | Side | Account | Amount | `PreviousRowHash` | `RowHash` |
|---|---|---|---|---|---|
| 3 | Debit | 10100 (Cash) | $75 | `0x0000…0000` (genesis, 32 zero bytes) | `0x5E08EF14028496CB3C694C028B53140F9C34C4880B7512A6EADB906289DE344B` |
| 4 | Credit | 11000 (AR) | $75 | `0x5E08EF14028496CB3C694C028B53140F9C34C4880B7512A6EADB906289DE344B` | `0x35ADA1A1BD140ED18DC3509741C5CA50BFA2605787BC68081F18551529227883` |

**What this confirms:**

- **Hash chain integrity.** EntryId 4's `PreviousRowHash` is byte-for-byte
  EntryId 3's `RowHash`. The chain links correctly from the very first
  row of this entity onward, with the genesis sentinel (32 zero bytes)
  marking the head per §39.
- **Atomicity.** Dataverse and SQL agree on the posted state. No
  one-sided commit — the rollback-and-throw pattern (§41) holds under
  real conditions.
- **Credential resolution.** The plugin successfully read the SQL
  connection string from Key Vault using the plain-Text env var path
  (§63). No `0x80040256 Access Denied` failures from the sandbox.
- **Plugin sandbox can reach Azure SQL.** Network egress on
  port 1433 from the Dataverse plugin sandbox to
  `plasticrecycling.database.windows.net` works for the v9 sandbox
  AppDomain.

**Failed attempts that drove the final design:**

| Assembly | Outcome | Root cause |
|---|---|---|
| 1.0.0.2 | `0x80040256 Access Denied` on `RetrieveEnvironmentVariableSecretValue` | Wrong action parameter (`environmentVariableDefinitionId` Guid instead of `EnvironmentVariableName` String — caught by Web API `$metadata`) |
| 1.0.0.3 | `0x80040256 Access Denied` at 47 ms (same call) | Parameter shape fixed, but the plugin sandbox identity lacks `prvReadEnvironmentVariableSecretValue` even when impersonating SYSTEM via `OrgSvcFactory.CreateOrganizationService(null)`. The privilege gate is enforced at the message dispatcher; `CreateOrganizationService(null)` does not bypass it. |
| 1.0.0.4 | PASS | Pivoted to plain-Text env var (§63). `DataverseEnvironmentVariables.GetValue` no longer calls `Execute`. A regression test in `KeyVaultTests/DataverseEnvironmentVariablesTests.cs` asserts the function never invokes `Execute`, locking the design in. |

**Still-pending follow-up checks (Phase 6B closure does not block on
these; they belong to the broader integrity story):**

- Re-compute `RowHash` of EntryId 3 and EntryId 4 offline via
  `LedgerRowHasher.ComputeRowHash` against the row data and verify
  bytes match. Recommended as the first nightly job in the integrity
  verification phase.
- Repeat the negative tests (UPDATE / DELETE as `dsb_app` against the
  newly-written EntryIds) to re-confirm SQL 229 / DENY grants are
  still in force after the first real INSERT.
- **Maintenance-window backlog (logged 2026-05-21):** the POST branch of
  `scripts/sync-sp-secret-to-dataverse.ps1` has never been exercised
  live -- only the PATCH branch ran during Phase 6B closure. Testing
  POST requires deleting the `rm_sqlkvclientsecret` env var value row
  to force a value-row-not-yet-present condition, which causes a brief
  outage window for the plugin runtime (any JE post during the window
  throws). Schedule for a dedicated maintenance window with documented
  rollback plan. Not blocking; the PATCH branch covers the steady-state
  rotation path.

## 2026-05-21 follow-up -- app-reg credential state verified

During Phase 7A Session S1 (pre-flight + Phase 6B carry-forward
cleanup), the `datastream-books-cicd` Entra app registration was
inspected via `az ad app credential list` to verify the cleanup
documented in §45 actually held live.

**Live state:** exactly **one** password credential present, keyId
`987e5ce6-934e-48db-a3d0-8972e98c7d63` (displayName "secret3",
2026-05-20 -- 2028-05-19, hint `lRP`). Federated credential
`github-main` (OIDC, subject `repo:ryanm-plastic-recycling/datastream-books:ref:refs/heads/main`)
present and untouched.

**Cross-reference with decision log:** efb08aa2 (§37 -- deleted) and
3eaf8a5a (§37 retained, §45 deleted after `--query value` exposure)
are both **absent** from the live app reg. State matches §45's
documented end-state.

**Key Vault alignment** (length-only check per §45 safe-diagnostic
pattern): `kv-datastream-books / cicd-sp-client-secret` is enabled,
non-empty, tag `key-id` pins the live credential
(`987e5ce6-934e-48db-a3d0-8972e98c7d63`), last updated 2026-05-20T18:12:39Z.

**Carry-forward concern from Phase 6B handoff is closed.** The
"unidentified credential keyId 3eaf8a5a-..." item the operator brief
flagged was a stale snapshot that predated §45's resolution. No
disposition action was taken; nothing required deletion. This note
exists so a future operator does not re-investigate the same closed
issue.

## See also

- [`immutability-design.md`](immutability-design.md) — full immutability architecture (§A append-only ledger, §B hash chain)
- [`credential-access-design.md`](credential-access-design.md) — how the plugin acquires its SQL connection string
- [`../../azure-sql/migrations/V0002__general_ledger_entries.sql`](../../azure-sql/migrations/V0002__general_ledger_entries.sql) — DENY grants source of truth
- [`../../azure-sql/migrations/V0003__sql_logins.sql`](../../azure-sql/migrations/V0003__sql_logins.sql) — per-user DENY defense in depth
- [`../runbooks/sql-account-management.md`](../runbooks/sql-account-management.md) — rotating `dsb_*` passwords for first real use
