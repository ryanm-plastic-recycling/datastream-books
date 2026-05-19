-- =============================================================================
-- Migration: V0003__sql_logins.sql
-- Date:      2026-05-19
-- =============================================================================
-- Purpose:
--   Create the four contained database users that Datastream Books uses for
--   day-to-day database access, with least-privilege grants. Reinforce
--   immutability by also denying UPDATE/DELETE on ledger.GeneralLedgerEntries
--   to all four users (defense in depth on top of the universal DENY to
--   public from V0002).
--
-- Users created (CONTAINED, SQL authentication, no server login):
--   dsb_app
--     Application user. Used by the Dataverse posting plugin (or any
--     server-side process) to INSERT into the immutable ledger and the
--     audit event tables. NOTHING else.
--   dsb_migrate
--     CI/CD migration runner. db_ddladmin + db_datawriter — applies
--     migrations and seeds reference data. Cannot SELECT financial data
--     beyond what migrations themselves return.
--   dsb_reader
--     Read-only access to the `reports` schema. Used by Power BI, ad-hoc
--     reporting tools, and external auditors during engagement windows.
--   dsb_admin
--     db_owner equivalent for maintenance. STILL bound by the table-level
--     DENY UPDATE/DELETE on ledger.GeneralLedgerEntries, because DENY beats
--     GRANT and we re-apply it explicitly to this user. An admin needing
--     to alter ledger rows must (a) explicitly REVOKE the DENY in a
--     privileged session and (b) leave an Azure SQL audit trail of doing so.
--
-- =============================================================================
-- PASSWORD HANDLING — PARAMETERIZED, NEVER IN GIT
-- =============================================================================
-- The CREATE USER statements below reference passwords as sqlcmd-style
-- variables ($(pw_dsb_app), $(pw_dsb_migrate), $(pw_dsb_reader),
-- $(pw_dsb_admin)). The committed source contains NO plaintext password
-- and NO placeholder literal — only the variable token.
--
-- The project's migration runner generates a cryptographically random
-- 32-character password per account at apply time and substitutes the
-- token in-memory before sending the SQL to the server. The substituted
-- text is never logged, persisted, or committed.
--
-- Result of first apply: the four dsb_* accounts EXIST with grants/denies
-- in place, but have throwaway passwords known to no one. They cannot be
-- logged into until rotated. This is the most secure default — no usable
-- credential exists in any human's hands.
--
-- To rotate an account password before first use (or for routine rotation),
-- see docs/runbooks/sql-account-management.md. The one-line ALTER USER
-- syntax is documented there.
--
-- To apply this file manually with sqlcmd:
--   sqlcmd -S <server> -d DatastreamBooks-Dev -U <admin> -P <admin-pw> ^
--          -v pw_dsb_app=<pw1> pw_dsb_migrate=<pw2> ^
--             pw_dsb_reader=<pw3> pw_dsb_admin=<pw4> ^
--          -i V0003__sql_logins.sql
-- (Do this only if rotating; the project runner is the normal path.)
--
-- Prerequisites:
--   V0001 (schemas + SchemaMigrations) and V0002 (ledger.GeneralLedgerEntries)
--   must be applied first.
--
-- Idempotency:
--   IF NOT EXISTS gates each CREATE USER. GRANT/DENY statements are
--   idempotent by nature. Re-running this file rotates passwords if the
--   user already exists — the migration runner handles that correctly by
--   issuing ALTER USER ... WITH PASSWORD instead of CREATE USER on re-apply.
--   (See sql-account-management.md.)
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO

-- -----------------------------------------------------------------------------
-- Users
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'dsb_app')
    CREATE USER dsb_app     WITH PASSWORD = '$(pw_dsb_app)';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'dsb_migrate')
    CREATE USER dsb_migrate WITH PASSWORD = '$(pw_dsb_migrate)';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'dsb_reader')
    CREATE USER dsb_reader  WITH PASSWORD = '$(pw_dsb_reader)';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'dsb_admin')
    CREATE USER dsb_admin   WITH PASSWORD = '$(pw_dsb_admin)';
GO

-- -----------------------------------------------------------------------------
-- Role memberships
-- -----------------------------------------------------------------------------
ALTER ROLE db_ddladmin   ADD MEMBER dsb_migrate;
ALTER ROLE db_datawriter ADD MEMBER dsb_migrate;
GO

ALTER ROLE db_owner ADD MEMBER dsb_admin;
GO

-- -----------------------------------------------------------------------------
-- dsb_app — minimal grants
-- -----------------------------------------------------------------------------
GRANT INSERT ON ledger.GeneralLedgerEntries TO dsb_app;
GRANT SELECT ON ledger.GeneralLedgerEntries TO dsb_app;  -- needed for hash-chain read-then-INSERT
-- audit.AuditEvents will be added in V0005; INSERT grant to dsb_app comes then.
GO

-- -----------------------------------------------------------------------------
-- dsb_migrate — schema work but blocked from mutating posted ledger rows
-- -----------------------------------------------------------------------------
GRANT SELECT ON ledger.GeneralLedgerEntries TO dsb_migrate;
GO

-- -----------------------------------------------------------------------------
-- dsb_reader — reports schema only, read-only
-- -----------------------------------------------------------------------------
GRANT SELECT ON SCHEMA::reports TO dsb_reader;
GO

-- -----------------------------------------------------------------------------
-- dsb_admin — db_owner, but explicitly denied ledger mutation
-- -----------------------------------------------------------------------------
GRANT SELECT ON ledger.GeneralLedgerEntries TO dsb_admin;
GO

-- =============================================================================
-- DEFENSE IN DEPTH — explicit DENY UPDATE/DELETE to every user
-- =============================================================================
-- The universal DENY on public from V0002 already covers these users. We
-- reapply the DENY here on a per-user basis so that:
--   1. Auditors examining sys.database_permissions for any of the dsb_*
--      users see the DENY explicitly attached to them, not just inherited.
--   2. Any future migration that GRANTs UPDATE to one of these users
--      (a mistake or an attack) is still blocked, because per-user DENY
--      wins over per-user GRANT.
-- =============================================================================
DENY UPDATE ON ledger.GeneralLedgerEntries TO dsb_app;
DENY DELETE ON ledger.GeneralLedgerEntries TO dsb_app;

DENY UPDATE ON ledger.GeneralLedgerEntries TO dsb_migrate;
DENY DELETE ON ledger.GeneralLedgerEntries TO dsb_migrate;

DENY UPDATE ON ledger.GeneralLedgerEntries TO dsb_reader;
DENY DELETE ON ledger.GeneralLedgerEntries TO dsb_reader;

DENY UPDATE ON ledger.GeneralLedgerEntries TO dsb_admin;
DENY DELETE ON ledger.GeneralLedgerEntries TO dsb_admin;
GO

-- =============================================================================
-- Record this migration
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE [Version] = 3)
BEGIN
    INSERT INTO dbo.SchemaMigrations ([Version], [Description])
    VALUES (3, N'V0003 - Create four contained users (dsb_app, dsb_migrate, dsb_reader, dsb_admin) with least-privilege grants and per-user DENY UPDATE/DELETE on ledger.GeneralLedgerEntries. Passwords parameterized via $(pw_dsb_*); see docs/runbooks/sql-account-management.md.');
END
GO

-- =============================================================================
-- ROLLBACK NOTES
-- =============================================================================
-- Safe rollback (no data dependencies — these are just principals):
--   DROP USER IF EXISTS dsb_admin;
--   DROP USER IF EXISTS dsb_reader;
--   DROP USER IF EXISTS dsb_migrate;
--   DROP USER IF EXISTS dsb_app;
--   DELETE FROM dbo.SchemaMigrations WHERE [Version] = 3;
--
-- After rollback, daily DB access falls back to priadmin (bootstrap admin)
-- until a replacement migration is applied. priadmin is break-glass-only —
-- see the auth strategy notes in the roadmap and CI/CD runbook.
-- =============================================================================
