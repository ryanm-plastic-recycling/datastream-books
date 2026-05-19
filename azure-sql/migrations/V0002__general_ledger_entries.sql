-- =============================================================================
-- Migration: V0002__general_ledger_entries.sql
-- Date:      2026-05-19
-- =============================================================================
-- Purpose:
--   Create ledger.GeneralLedgerEntries — the append-only, hash-chained posted
--   transaction record that backs Datastream Books's financial statements.
--   Every posted journal-entry line writes exactly one row here, atomically
--   with the Dataverse post (server-side plugin enforces atomicity).
--
-- References:
--   - docs/decisions/datastream-books-decisions.md "Immutability Architecture" A and B
--   - docs/architecture/immutability-design.md
--
-- Key design properties:
--   1. APPEND-ONLY: UPDATE/DELETE/REFERENCES/ALTER denied to public. DENY beats
--      GRANT, so even db_owner cannot UPDATE without first REVOKEing the DENY
--      (an auditable DDL event).
--   2. HASH-CHAINED per EntityId. RowHash = SHA-256(canonical payload || PreviousRowHash).
--   3. MULTI-ENTITY from row zero. EntityId on every row.
--   4. USD-only in v1 (CurrencyCode is metadata for forward compatibility).
--
-- GRANTs vs DENYs in this migration:
--   - DENY UPDATE/DELETE/REFERENCES/ALTER TO public is set here (universal).
--   - GRANT INSERT/SELECT to the four dsb_* users happens in V0003. We do NOT
--     reference any user/role that does not yet exist.
--
-- Idempotency:
--   IF NOT EXISTS gates the table, indexes, and extended property. Safe to re-run.
--
-- Prerequisites:
--   V0001 must have created the `ledger` schema and dbo.SchemaMigrations.
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO

-- =============================================================================
-- TABLE: ledger.GeneralLedgerEntries
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'ledger' AND t.name = N'GeneralLedgerEntries'
)
BEGIN
    CREATE TABLE ledger.GeneralLedgerEntries
    (
        -- ----- Identity / chain position -----
        EntryId                 BIGINT          IDENTITY(1,1) NOT NULL,
        EntryUid                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_GLE_EntryUid DEFAULT NEWSEQUENTIALID(),

        -- ----- Multi-entity scope -----
        EntityId                UNIQUEIDENTIFIER NOT NULL,    -- FK to Dataverse Entity record (rm_entity)

        -- ----- Posting metadata -----
        JournalEntryId          UNIQUEIDENTIFIER NOT NULL,    -- FK to Dataverse rm_journalentry (header)
        JournalEntryLineId      UNIQUEIDENTIFIER NOT NULL,    -- FK to Dataverse rm_journalentryline
        JournalEntryNumber      NVARCHAR(32)     NOT NULL,    -- Human-readable JE number (e.g., "JE-2026-00045")
        LineNumber              INT              NOT NULL,    -- Line within the JE (1-based)

        -- ----- Period / date -----
        FiscalPeriodId          UNIQUEIDENTIFIER NOT NULL,    -- FK to Dataverse rm_fiscalperiod
        TransactionDate         DATE             NOT NULL,    -- Business effective date
        PostedAtUtc             DATETIME2(3)     NOT NULL CONSTRAINT DF_GLE_PostedAtUtc DEFAULT SYSUTCDATETIME(),

        -- ----- Account -----
        AccountId               UNIQUEIDENTIFIER NOT NULL,    -- FK to Dataverse rm_chartofaccount
        AccountCode             NVARCHAR(32)     NOT NULL,    -- Denormalized for reporting (immutable at post time)
        AccountName             NVARCHAR(200)    NOT NULL,    -- Denormalized for reporting

        -- ----- Amount (USD only in v1; currency is metadata) -----
        DebitAmount             DECIMAL(19,4)    NOT NULL CONSTRAINT DF_GLE_DebitAmount  DEFAULT 0,
        CreditAmount            DECIMAL(19,4)    NOT NULL CONSTRAINT DF_GLE_CreditAmount DEFAULT 0,
        CurrencyCode            CHAR(3)          NOT NULL CONSTRAINT DF_GLE_CurrencyCode DEFAULT 'USD',

        -- ----- Description / source -----
        Memo                    NVARCHAR(500)    NULL,
        SourceModule            NVARCHAR(20)     NOT NULL,    -- 'GL','AP','AR','FA','BANK','SYS'
        SourceDocumentRef       NVARCHAR(100)    NULL,        -- e.g., Bill #, Invoice #, Bank Stmt ID

        -- ----- Inter-company linkage -----
        InterCompanyPairId      UNIQUEIDENTIFIER NULL,
        InterCompanyEntityId    UNIQUEIDENTIFIER NULL,

        -- ----- Reversal linkage -----
        ReversesEntryId         BIGINT           NULL,        -- Forward pointer (immutability-safe). Use a view to
                                                              -- expose "is this original reversed?" — see notes below.
        ReversedByEntryId       BIGINT           NULL,        -- Reserved column; never UPDATEd. Future view supersedes.

        -- ----- Identity of poster -----
        PostedByUserId          UNIQUEIDENTIFIER NOT NULL,
        PostedByPrincipalName   NVARCHAR(200)    NOT NULL,

        -- ----- Approval chain (denormalized for audit) -----
        ApprovedByUserId        UNIQUEIDENTIFIER NULL,
        ApprovedByPrincipalName NVARCHAR(200)    NULL,
        ApprovedAtUtc           DATETIME2(3)     NULL,

        -- ----- Hash chain -----
        PreviousRowHash         BINARY(32)       NOT NULL,    -- 32 zero bytes for the per-entity genesis row
        RowHash                 BINARY(32)       NOT NULL,    -- Computed by the writing plugin

        -- ----- Constraints -----
        CONSTRAINT PK_GeneralLedgerEntries
            PRIMARY KEY CLUSTERED (EntryId),

        CONSTRAINT UQ_GeneralLedgerEntries_EntryUid
            UNIQUE NONCLUSTERED (EntryUid),

        -- One-and-only-one of debit or credit must be non-zero (and non-negative)
        CONSTRAINT CK_GeneralLedgerEntries_DebitOrCredit
            CHECK (
                (DebitAmount >= 0 AND CreditAmount >= 0)
                AND (DebitAmount = 0 OR CreditAmount = 0)
                AND (DebitAmount > 0 OR CreditAmount > 0)
            ),

        CONSTRAINT CK_GeneralLedgerEntries_SourceModule
            CHECK (SourceModule IN ('GL','AP','AR','FA','BANK','SYS')),

        CONSTRAINT CK_GeneralLedgerEntries_ReversalShape
            CHECK (
                ReversesEntryId IS NULL
                OR ReversesEntryId < EntryId
            ),

        CONSTRAINT CK_GeneralLedgerEntries_InterCompanyShape
            CHECK (
                (InterCompanyPairId IS NULL AND InterCompanyEntityId IS NULL)
                OR (InterCompanyPairId IS NOT NULL AND InterCompanyEntityId IS NOT NULL)
            )
    );
END
GO

-- =============================================================================
-- INDEXES
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GLE_EntityPeriodAccount' AND object_id = OBJECT_ID(N'ledger.GeneralLedgerEntries'))
    CREATE NONCLUSTERED INDEX IX_GLE_EntityPeriodAccount
        ON ledger.GeneralLedgerEntries (EntityId, FiscalPeriodId, AccountId)
        INCLUDE (DebitAmount, CreditAmount, TransactionDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GLE_JournalEntryId' AND object_id = OBJECT_ID(N'ledger.GeneralLedgerEntries'))
    CREATE NONCLUSTERED INDEX IX_GLE_JournalEntryId
        ON ledger.GeneralLedgerEntries (JournalEntryId)
        INCLUDE (LineNumber, AccountId, DebitAmount, CreditAmount);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GLE_EntityChain' AND object_id = OBJECT_ID(N'ledger.GeneralLedgerEntries'))
    CREATE NONCLUSTERED INDEX IX_GLE_EntityChain
        ON ledger.GeneralLedgerEntries (EntityId, EntryId)
        INCLUDE (RowHash, PreviousRowHash);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GLE_ReversesEntryId' AND object_id = OBJECT_ID(N'ledger.GeneralLedgerEntries'))
    CREATE NONCLUSTERED INDEX IX_GLE_ReversesEntryId
        ON ledger.GeneralLedgerEntries (ReversesEntryId)
        WHERE ReversesEntryId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GLE_TransactionDate' AND object_id = OBJECT_ID(N'ledger.GeneralLedgerEntries'))
    CREATE NONCLUSTERED INDEX IX_GLE_TransactionDate
        ON ledger.GeneralLedgerEntries (EntityId, TransactionDate)
        INCLUDE (AccountId, DebitAmount, CreditAmount);
GO

-- =============================================================================
-- PERMISSIONS — UNIVERSAL DENY (immutability enforcement)
-- =============================================================================
-- DENY beats GRANT. Applied to public so it covers every role/user/principal,
-- including future db_owner members. To perform a privileged maintenance
-- UPDATE/DELETE, an admin must REVOKE the DENY in a privileged session —
-- which is itself a DDL event captured by Azure SQL audit.
--
-- GRANT INSERT/SELECT to the dsb_* users lives in V0003 (where the users
-- are created). This migration deliberately does NOT reference any user or
-- role that doesn't exist yet.
-- =============================================================================
DENY UPDATE     ON ledger.GeneralLedgerEntries TO public;
DENY DELETE     ON ledger.GeneralLedgerEntries TO public;
DENY REFERENCES ON ledger.GeneralLedgerEntries TO public;
DENY ALTER      ON ledger.GeneralLedgerEntries TO public;
GO

-- =============================================================================
-- EXTENDED PROPERTIES
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties
    WHERE major_id = OBJECT_ID(N'ledger.GeneralLedgerEntries')
      AND minor_id = 0
      AND class    = 1
      AND name     = N'MS_Description'
)
EXEC sys.sp_addextendedproperty
    @name       = N'MS_Description',
    @value      = N'Append-only posted general-ledger entries. Hash-chained per EntityId. Corrections via reversing entries only. INSERT permitted only to dsb_app (granted in V0003); UPDATE/DELETE/REFERENCES/ALTER denied to public.',
    @level0type = N'SCHEMA', @level0name = N'ledger',
    @level1type = N'TABLE',  @level1name = N'GeneralLedgerEntries';
GO

-- =============================================================================
-- REVERSAL LINKAGE — design note
-- =============================================================================
-- Setting ReversedByEntryId on the ORIGINAL row would require UPDATE, which
-- we have just denied. v1 uses a view (ledger.vw_GeneralLedgerEntriesWithReversal,
-- to be created in V0004+) that left-joins the reversal pointer back to the
-- original. The ReversedByEntryId column above is reserved and never written.
-- =============================================================================

-- =============================================================================
-- Record this migration
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE [Version] = 2)
BEGIN
    INSERT INTO dbo.SchemaMigrations ([Version], [Description])
    VALUES (2, N'V0002 - Create ledger.GeneralLedgerEntries (append-only, hash-chained, multi-entity) with indexes, CHECKs, and universal DENY UPDATE/DELETE/REFERENCES/ALTER.');
END
GO

-- =============================================================================
-- ROLLBACK NOTES
-- =============================================================================
-- Once any row exists in ledger.GeneralLedgerEntries, this migration is
-- effectively irreversible — dropping the table destroys the immutable
-- audit record. Rollback is safe only BEFORE the first INSERT.
--
-- Safe pre-data rollback:
--   DROP INDEX IF EXISTS IX_GLE_EntityPeriodAccount ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_JournalEntryId      ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_EntityChain         ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_ReversesEntryId     ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_TransactionDate     ON ledger.GeneralLedgerEntries;
--   DROP TABLE IF EXISTS ledger.GeneralLedgerEntries;  -- DENYs are dropped with the object
--   DELETE FROM dbo.SchemaMigrations WHERE [Version] = 2;
--
-- Post-data rollback: DO NOT execute. File a Change Request and use
-- Azure SQL PITR/LTR. Modifying the live ledger schema after data exists
-- is a maintenance-window event.
-- =============================================================================
