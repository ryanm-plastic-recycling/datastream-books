# Runbook — SQL Account Management

> Day-to-day management of the four contained database users created by
> `V0003__sql_logins.sql` in `DatastreamBooks-Dev` (and, later,
> `DatastreamBooks` prod). Covers password rotation, first-use unlock,
> and the relationship between `priadmin` and the `dsb_*` accounts.

## The accounts

| Account | Role | Day-to-day purpose | Permissions on `ledger.GeneralLedgerEntries` |
|---|---|---|---|
| `dsb_app` | _(none — explicit grants only)_ | Application user — the Dataverse posting plugin / server-side process that writes to the immutable ledger. | GRANT INSERT, SELECT. DENY UPDATE, DELETE. |
| `dsb_migrate` | `db_ddladmin` + `db_datawriter` | CI/CD migration runner. Applies SQL migrations and seeds reference data. | GRANT SELECT. DENY UPDATE, DELETE. |
| `dsb_reader` | _(none — schema-scoped grant only)_ | Read-only access to `reports` schema. Used by Power BI, ad-hoc reporting, and external auditors during engagement windows. | None. (Reads ledger data via views in `reports` via ownership chaining.) |
| `dsb_admin` | `db_owner` | Maintenance work — schema changes, troubleshooting, role assignments. **Cannot mutate the ledger** despite `db_owner` membership, because per-user DENY beats per-user GRANT. | GRANT SELECT. DENY UPDATE, DELETE. |

Plus the bootstrap admin:

| Account | Role | Day-to-day purpose | Permissions on `ledger.GeneralLedgerEntries` |
|---|---|---|---|
| `priadmin` | SQL Server admin → maps to `dbo` | **Break-glass only.** Originally used to apply V0001–V0003. Bypasses all permission checks (documented SQL Server behavior). Never use for routine work. | Bypasses DENY. Can do anything. |

## Current state (as of last validation)

- All four `dsb_*` accounts EXIST in `DatastreamBooks-Dev` with the grants
  and per-user DENY rows listed above. See `sys.database_principals` and
  `sys.database_permissions` to verify.
- `dsb_app` — **rotated 2026-05-20 during the Key Vault provisioning
  session.** Connection string (including password) stored as
  `dsb-app-connection-string` in `kv-datastream-books`. Positive +
  negative permissions verified at rotation time. See
  [`key-vault-management.md`](key-vault-management.md) for the secret
  inventory, rotation procedure, and break-glass paths.
- `dsb_migrate`, `dsb_reader`, `dsb_admin` — still hold **throwaway
  passwords known to no one** (generated in-memory by the migration
  runner at apply time, never logged or persisted, then discarded). The
  accounts cannot be logged into until intentionally rotated. Rotate
  via the procedure below the first time a real consumer needs them.

## Rotating an account password (the one-line command)

To rotate, log in as `dsb_admin` (or `priadmin` if `dsb_admin` is locked out)
and run:

```sql
ALTER USER dsb_app     WITH PASSWORD = '<new-strong-password-here>';
-- or
ALTER USER dsb_migrate WITH PASSWORD = '<new-strong-password-here>';
-- or
ALTER USER dsb_reader  WITH PASSWORD = '<new-strong-password-here>';
-- or
ALTER USER dsb_admin   WITH PASSWORD = '<new-strong-password-here>';
```

That is the entire SQL needed.

### Where the new password should come from

**Preferred path (production):** Azure Key Vault.

1. Create the secret in Key Vault first (Portal, CLI, or `New-AzKeyVaultSecret`),
   e.g., `kv-datastream-books/secrets/sql-dsb-app`.
2. Read the secret value into a local variable in a privileged session.
3. Pass it to the `ALTER USER` statement.
4. Confirm the secret in Key Vault matches.

`az` example (does not echo the secret to the shell history if you assign to a variable first):

```powershell
$pw = az keyvault secret show --vault-name kv-datastream-books --name sql-dsb-app --query value -o tsv
# Apply via sqlcmd or Invoke-Sqlcmd with $pw substituted in-memory
```

**Acceptable path (dev only):** A user-scope environment variable, the same
pattern as `DATASTREAM_BOOKS_DEV_CONN`. Never commit. Never echo. Remove
from the variable scope when finished.

**Not acceptable:** Storing the password in `appsettings.json`, in any repo
file (even `.gitignored`), in a Microsoft Teams DM, in Notepad on the
desktop, or in the body of an email.

### Rotation cadence

| Account | Cadence |
|---|---|
| `dsb_app` | Quarterly; or immediately if the plugin's SP key is compromised. |
| `dsb_migrate` | Quarterly; or immediately after any CI/CD pipeline access change. |
| `dsb_reader` | Quarterly; or immediately at the end of each external auditor engagement. |
| `dsb_admin` | Quarterly; or immediately if `dsb_admin` credentials may have been exposed. |
| `priadmin` | Annually + on any suspected exposure. Held in Key Vault only; access reviewed quarterly. |

A rotation event always:
1. Writes a row to `audit.AuditEvents` (once that table exists in V0005)
   describing which account, when, by whom (the human running the ALTER),
   and a Change Request reference.
2. Updates the secret in Key Vault.
3. Notifies any system using the credential — primarily the GitHub Actions
   workflow secrets (for `dsb_migrate`) and the Dataverse plugin
   configuration (for `dsb_app`).

## First-use unlock procedure

Each `dsb_*` account currently has a throwaway password no one knows.
Before first use, rotate to a real password using the procedure above.
Use this sequence when wiring a real consumer:

1. Generate a strong random password (or pull from Key Vault if pre-staged).
2. As `dsb_admin` (after rotating its own password first) or `priadmin`,
   run the appropriate `ALTER USER` from above.
3. Store the new password in Key Vault under
   `kv-datastream-books/secrets/sql-<account>`.
4. Configure the consumer (CI workflow, Dataverse plugin config, Power BI
   data source) to read from Key Vault.
5. Verify the consumer can connect and perform its expected operations
   (e.g., `dsb_migrate` should apply a no-op migration; `dsb_app` should
   INSERT and then fail an UPDATE attempt).
6. Record the first-use date in the Change Request that authorized it.

## Bootstrap admin (`priadmin`) governance

- **Status:** break-glass only.
- **Where the credential lives:** Azure Key Vault (`kv-datastream-books/secrets/sql-priadmin`).
- **Who can read the secret:** designated humans (named in the Key Vault
  access policy). Reviewed quarterly.
- **Auditing requirement:** Azure SQL audit must be enabled on the
  `plasticrecycling` server with retention sufficient to cover any audit
  cycle (currently target = 7 years). Every `priadmin` login is captured
  with timestamp, client IP, and statements executed.
- **Long-term plan:** Migrate the bootstrap admin to AAD-only auth (Entra
  group + Active Directory admin on the server), removing the password
  as a stealable artifact and requiring MFA. Tracked as a Phase 2 follow-up.

### Why `priadmin` is not constrained by `DENY`

SQL Server bypasses permission checks for the `sysadmin` server role and
the `dbo` database principal. `priadmin` is mapped to `dbo` inside
`DatastreamBooks-Dev`, so `DENY UPDATE/DELETE ON ledger.GeneralLedgerEntries
TO public` does not block it. This was demonstrated in the 2026-05-19
validation run (see [`../architecture/immutability-validation.md`](../architecture/immutability-validation.md)).

The architecture's protection against `priadmin` abuse is therefore
**organizational + audit-based**, not permission-based:

- Few humans hold `priadmin`.
- Every `priadmin` session is audited at the Azure SQL layer.
- A break-glass usage triggers a Change Request review after the fact.
- Periodic re-tests of immutability (using `dsb_*` users) confirm no one
  has quietly REVOKEd the DENY grants.

## Verification queries

To confirm the current grant/deny state at any time:

```sql
SELECT pr.name AS Grantee, perm.permission_name, perm.state_desc
FROM   sys.database_permissions perm
JOIN   sys.database_principals  pr ON pr.principal_id = perm.grantee_principal_id
WHERE  perm.major_id = OBJECT_ID('ledger.GeneralLedgerEntries')
ORDER  BY pr.name, perm.permission_name;
```

Expected: 4 DENY rows on `public` (UPDATE / DELETE / REFERENCES / ALTER)
plus 8 DENY rows across the four `dsb_*` users (UPDATE + DELETE each),
plus the GRANTs for INSERT/SELECT on the relevant `dsb_*` users.

To list which accounts exist and how they authenticate:

```sql
SELECT name, type_desc, authentication_type_desc
FROM   sys.database_principals
WHERE  name LIKE 'dsb_%' OR name = 'dbo'
ORDER  BY name;
```

## See also

- [`key-vault-management.md`](key-vault-management.md) — the Key Vault
  that stores rotated `dsb_*` credentials, secret inventory, and
  rotation procedure (which calls back into this runbook)
- [`../architecture/immutability-design.md`](../architecture/immutability-design.md) — what these accounts are protecting
- [`../architecture/immutability-validation.md`](../architecture/immutability-validation.md) — the live test that confirmed DENY blocks `dsb_admin` and `dsb_app`
- [`../../azure-sql/migrations/V0003__sql_logins.sql`](../../azure-sql/migrations/V0003__sql_logins.sql) — account definitions and grants source of truth
- [`cicd-setup.md`](cicd-setup.md) — when `dsb_migrate` and federated identity get wired into GitHub Actions
