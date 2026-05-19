-- =============================================================================
-- Migration: V0001__initial_schema.sql
-- Date:      2026-05-19
-- =============================================================================
-- Purpose:
--   Initialize the Datastream Books Azure SQL database with the schemas every
--   later migration depends on, plus the dbo.SchemaMigrations metadata table.
--   No tables outside dbo.SchemaMigrations are created here — those belong
--   to V0002+.
--
-- Schemas created:
--   - ledger   : append-only financial ledger objects (V0002 creates the first table)
--   - audit    : redundant audit-event tables and integrity checkpoints
--   - reports  : report snapshots and reporting marts
--   - archive  : historical Macola data (read-only after migration cutover)
--
-- Metadata table:
--   dbo.SchemaMigrations(Version int PK, AppliedAt datetime2, AppliedBy nvarchar(128),
--                        Description nvarchar(500))
--
-- Idempotency:
--   Every object is guarded by an existence check. Safe to re-run.
--
-- Apply order:
--   First migration. No prerequisites.
--
-- Auth context at first apply:
--   priadmin (SQL bootstrap admin). After V0003 lands, daily migration apply
--   moves to dsb_migrate; priadmin becomes break-glass-only.
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO

-- -----------------------------------------------------------------------------
-- Schemas
-- -----------------------------------------------------------------------------
-- CREATE SCHEMA must be the only statement in its batch in SQL Server, hence
-- the dynamic SQL pattern with existence guards.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'ledger')
    EXEC(N'CREATE SCHEMA ledger AUTHORIZATION dbo');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'audit')
    EXEC(N'CREATE SCHEMA [audit] AUTHORIZATION dbo');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'reports')
    EXEC(N'CREATE SCHEMA reports AUTHORIZATION dbo');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'archive')
    EXEC(N'CREATE SCHEMA [archive] AUTHORIZATION dbo');
GO

-- -----------------------------------------------------------------------------
-- Metadata table
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.name = N'SchemaMigrations'
)
BEGIN
    CREATE TABLE dbo.SchemaMigrations
    (
        [Version]     INT             NOT NULL,
        AppliedAt     DATETIME2(3)    NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAt DEFAULT SYSUTCDATETIME(),
        AppliedBy     NVARCHAR(128)   NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedBy DEFAULT SUSER_SNAME(),
        [Description] NVARCHAR(500)   NULL,
        CONSTRAINT PK_SchemaMigrations PRIMARY KEY CLUSTERED ([Version])
    );
END
GO

-- -----------------------------------------------------------------------------
-- Record this migration
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE [Version] = 1)
BEGIN
    INSERT INTO dbo.SchemaMigrations ([Version], [Description])
    VALUES (1, N'V0001 - Create ledger/audit/reports/archive schemas and SchemaMigrations metadata table.');
END
GO

-- =============================================================================
-- ROLLBACK NOTES
-- =============================================================================
-- Pre-data rollback (only safe before V0002 creates any tables in `ledger`):
--   DROP TABLE IF EXISTS dbo.SchemaMigrations;
--   DROP SCHEMA IF EXISTS [archive];
--   DROP SCHEMA IF EXISTS reports;
--   DROP SCHEMA IF EXISTS [audit];
--   DROP SCHEMA IF EXISTS ledger;
--
-- Once V0002+ have created objects inside ledger/audit/reports/archive, those
-- objects must be dropped FIRST (per the rollback notes in each later
-- migration) before the parent schemas can go. Post-data rollback against
-- a populated ledger should be treated as a destructive operation requiring
-- a Change Request and a PITR plan.
-- =============================================================================
