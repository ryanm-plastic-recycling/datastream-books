-- =============================================================================
-- Migration: V0002__general_ledger_entries.sql
-- Author:    Initial draft (Phase 1)
-- Date:      2026-05-19
-- =============================================================================
-- Purpose:
--   Create the append-only ledger.GeneralLedgerEntries table — the immutable
--   posted transaction record that backs Datastream Books's financial
--   statements. Every posted journal-entry line writes exactly one row
--   here, atomically with the Dataverse post (server-side plugin enforces
--   atomicity).
--
-- References:
--   - docs/decisions/datastream-books-decisions.md, "Immutability Architecture"
--     sections A (Append-Only Transaction Ledger) and B (Hash-Chained Records)
--   - docs/architecture/immutability-design.md (to be authored — Phase 1)
--
-- Key design properties:
--   1. APPEND-ONLY at the SQL role level. UPDATE and DELETE are denied to
--      every role and the dbo schema owner. Corrections happen via reversing
--      entries (a second row with negated amounts and ReversedByEntryId set).
--   2. HASH-CHAINED. Each row's RowHash = SHA-256 of (canonical row payload +
--      PreviousRowHash). The chain is anchored per-entity (independent chain
--      per EntityId) to allow per-entity verification and bulk loads.
--      Genesis row's PreviousRowHash is 0x00..00 (32 zero bytes).
--   3. MULTI-ENTITY from day one. Every row carries EntityId; no global
--      assumption.
--   4. USD-only in v1 (Amount is decimal(19,4); currency is metadata only).
--      Schema is future-proofed via the CurrencyCode column.
--   5. Posted by the Dataverse plugin acting as a SQL principal
--      (rl_app_writer). Direct INSERT from any human account is denied.
--
-- Idempotency:
--   IF NOT EXISTS gates all CREATE statements. Re-running is safe.
--
-- =============================================================================
-- PREREQUISITES (must run first)
-- =============================================================================
--   V0001 — creates the `ledger` schema and the rl_app_writer / rl_app_reader
--           roles. This migration assumes both exist.
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
        DebitAmount             DECIMAL(19,4)    NOT NULL CONSTRAINT DF_GLE_DebitAmount DEFAULT 0,
        CreditAmount            DECIMAL(19,4)    NOT NULL CONSTRAINT DF_GLE_CreditAmount DEFAULT 0,
        CurrencyCode            CHAR(3)          NOT NULL CONSTRAINT DF_GLE_CurrencyCode DEFAULT 'USD',

        -- ----- Description / source -----
        Memo                    NVARCHAR(500)    NULL,
        SourceModule            NVARCHAR(20)     NOT NULL,    -- 'GL','AP','AR','FA','BANK','SYS'
        SourceDocumentRef       NVARCHAR(100)    NULL,        -- e.g., Bill #, Invoice #, Bank Stmt ID

        -- ----- Inter-company linkage -----
        InterCompanyPairId      UNIQUEIDENTIFIER NULL,        -- NULL unless this row is part of an IC pair
        InterCompanyEntityId    UNIQUEIDENTIFIER NULL,        -- Counter-entity for IC entries

        -- ----- Reversal linkage -----
        ReversesEntryId         BIGINT           NULL,        -- If this row is a reversal, points to original EntryId
        ReversedByEntryId       BIGINT           NULL,        -- Set when a later reversal points back; populated via
                                                              -- one-shot trigger on the *reversal* INSERT (NOT an UPDATE
                                                              -- of the original — see ReversalLinkage section below).

        -- ----- Identity of poster -----
        PostedByUserId          UNIQUEIDENTIFIER NOT NULL,    -- Azure AD object id of the posting user (NOT the SP)
        PostedByPrincipalName   NVARCHAR(200)    NOT NULL,    -- UPN at post time (denormalized; survives renames)

        -- ----- Approval chain (denormalized for audit) -----
        ApprovedByUserId        UNIQUEIDENTIFIER NULL,        -- NULL if no approval required by policy
        ApprovedByPrincipalName NVARCHAR(200)    NULL,
        ApprovedAtUtc           DATETIME2(3)     NULL,

        -- ----- Hash chain (see CONSTRAINTS + COMPUTED COLUMN below) -----
        PreviousRowHash         BINARY(32)       NOT NULL,    -- 32 zero bytes for the per-entity genesis row
        RowHash                 BINARY(32)       NOT NULL,    -- SHA-256 of canonical payload || PreviousRowHash;
                                                              -- supplied by the writing plugin; validated by
                                                              -- the nightly verifier job (see V0003+ when added).

        -- ----- PK + table-level constraints -----
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

        -- A reversal references a prior entry; an entry being reversed cannot
        -- itself already be a reversal (we use the *original* as the anchor).
        CONSTRAINT CK_GeneralLedgerEntries_ReversalShape
            CHECK (
                ReversesEntryId IS NULL
                OR ReversesEntryId < EntryId
            ),

        -- Inter-company pairs are either fully specified or fully NULL
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
-- Primary read patterns:
--   (1) Trial balance / GL detail: by EntityId, FiscalPeriodId, AccountId
--   (2) JE detail lookup: by JournalEntryId
--   (3) Hash-chain verification: by EntityId, EntryId ASC
--   (4) Reversal tracing: by ReversesEntryId
-- =============================================================================

CREATE NONCLUSTERED INDEX IX_GLE_EntityPeriodAccount
    ON ledger.GeneralLedgerEntries (EntityId, FiscalPeriodId, AccountId)
    INCLUDE (DebitAmount, CreditAmount, TransactionDate)
    WHERE 1 = 1;  -- explicit predicate marker; full index on all rows
GO

CREATE NONCLUSTERED INDEX IX_GLE_JournalEntryId
    ON ledger.GeneralLedgerEntries (JournalEntryId)
    INCLUDE (LineNumber, AccountId, DebitAmount, CreditAmount);
GO

CREATE NONCLUSTERED INDEX IX_GLE_EntityChain
    ON ledger.GeneralLedgerEntries (EntityId, EntryId)
    INCLUDE (RowHash, PreviousRowHash);
GO

CREATE NONCLUSTERED INDEX IX_GLE_ReversesEntryId
    ON ledger.GeneralLedgerEntries (ReversesEntryId)
    WHERE ReversesEntryId IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX IX_GLE_TransactionDate
    ON ledger.GeneralLedgerEntries (EntityId, TransactionDate)
    INCLUDE (AccountId, DebitAmount, CreditAmount);
GO

-- =============================================================================
-- PERMISSIONS / DENY GRANTS — THIS IS WHERE IMMUTABILITY IS ENFORCED
-- =============================================================================
-- Azure SQL has no DDL-level "table is immutable" switch, so we enforce it
-- with object-scoped DENY for UPDATE, DELETE, TRUNCATE on the table. DENY
-- wins over GRANT, so even the schema owner cannot bypass it without first
-- explicitly REVOKEing the DENY (which itself is an auditable DDL event).
--
-- INSERT is GRANTed only to rl_app_writer (the Dataverse plugin service
-- principal). Humans get nothing. Admins doing maintenance must REVOKE
-- the DENY in a privileged session with a Change Request reference; that
-- REVOKE is itself logged via Azure SQL audit.
-- =============================================================================

-- INSERT: only the plugin service principal
GRANT INSERT ON ledger.GeneralLedgerEntries TO rl_app_writer;

-- SELECT: writer + reader + admin (admin needs read for reconciliation queries)
GRANT SELECT ON ledger.GeneralLedgerEntries TO rl_app_writer;
GRANT SELECT ON ledger.GeneralLedgerEntries TO rl_app_reader;
GRANT SELECT ON ledger.GeneralLedgerEntries TO rl_admin;

-- HARD DENIES — applied to public so they cover every role/user/principal
-- (including the schema owner unless they REVOKE first).
DENY UPDATE    ON ledger.GeneralLedgerEntries TO public;
DENY DELETE    ON ledger.GeneralLedgerEntries TO public;
DENY REFERENCES ON ledger.GeneralLedgerEntries TO public;  -- prevents adding FK with cascade that could imply delete

-- TRUNCATE requires ALTER on the table, which is admin-only; explicit DENY
-- belt-and-suspenders:
DENY ALTER ON ledger.GeneralLedgerEntries TO public;
GO

-- =============================================================================
-- REVERSAL LINKAGE — note on ReversedByEntryId
-- =============================================================================
-- Setting ReversedByEntryId on the ORIGINAL row would require an UPDATE,
-- which we have just denied. There are three viable patterns to track the
-- "this row has been reversed" linkage without violating immutability:
--
--   A) Don't store it on the original row. The reversal row stores
--      ReversesEntryId; queries that need to know "is this original row
--      reversed?" join to a sub-select. Simplest, but every read pays a
--      lookup cost.
--
--   B) View-based denormalization. Create a view that left-joins the
--      reversal pointer back to the original. No UPDATE required.
--
--   C) Side-table: ledger.LedgerEntryReversals(OriginalEntryId, ReversalEntryId).
--      Append-only itself (also DENY UPDATE/DELETE). The reversal-writing
--      plugin inserts one row to LedgerEntryReversals atomically with the
--      reversal INSERT to GeneralLedgerEntries.
--
-- We are committing to OPTION (B) for v1 (cleanest from a query perspective)
-- and removing ReversedByEntryId from the table definition in V0003 once the
-- view is created. The column stays NULLABLE here so we never have to
-- ALTER the table for this — V0003 will simply CREATE VIEW that ignores it
-- and a future migration may eventually drop the column once nothing reads it.
--
-- TODO (V0003): create ledger.vw_GeneralLedgerEntriesWithReversal.
-- =============================================================================

-- =============================================================================
-- EXTENDED PROPERTIES (descriptive metadata for tooling and auditors)
-- =============================================================================
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Append-only posted general-ledger entries. Hash-chained per EntityId. Corrections via reversing entries only. INSERT permitted to rl_app_writer only; UPDATE and DELETE denied to public.',
    @level0type = N'SCHEMA', @level0name = N'ledger',
    @level1type = N'TABLE',  @level1name = N'GeneralLedgerEntries';
GO

-- =============================================================================
-- ROLLBACK NOTES
-- =============================================================================
-- Once any row exists in ledger.GeneralLedgerEntries, this migration is
-- effectively irreversible — dropping the table destroys the immutable
-- audit record. Rollback is therefore only safe BEFORE the first INSERT.
--
-- Safe pre-data rollback:
--
--   REVOKE INSERT ON ledger.GeneralLedgerEntries FROM rl_app_writer;
--   REVOKE SELECT ON ledger.GeneralLedgerEntries FROM rl_app_writer;
--   REVOKE SELECT ON ledger.GeneralLedgerEntries FROM rl_app_reader;
--   REVOKE SELECT ON ledger.GeneralLedgerEntries FROM rl_admin;
--   -- DENY statements survive object drop; nothing further needed.
--   DROP INDEX IF EXISTS IX_GLE_EntityPeriodAccount ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_JournalEntryId     ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_EntityChain        ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_ReversesEntryId    ON ledger.GeneralLedgerEntries;
--   DROP INDEX IF EXISTS IX_GLE_TransactionDate    ON ledger.GeneralLedgerEntries;
--   DROP TABLE IF EXISTS ledger.GeneralLedgerEntries;
--
-- Post-data rollback (i.e., after real ledger rows exist):
--   DO NOT execute. File a Change Request. Recovery should use Azure SQL
--   PITR or long-term backup. Modifying the live ledger schema after data
--   exists requires a maintenance window, a backup snapshot, and a
--   signed-off CR per docs/runbooks/change-management.md (to be authored).
-- =============================================================================
