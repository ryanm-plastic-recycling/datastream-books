using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DatastreamBooks.Plugins.Immutability
{
    // Writes a batch of ledger rows to Azure SQL ledger.GeneralLedgerEntries
    // as one transaction, chained by SHA-256 RowHash per EntityId.
    //
    // Locking strategy:
    //   For each unique EntityId in the batch, we SELECT the current chain
    //   head WITH (UPDLOCK, HOLDLOCK). This takes an update lock on the
    //   most recent row (if any) and holds it for the duration of the
    //   transaction. A second concurrent writer for the same EntityId
    //   will block on the SELECT until we COMMIT, then read the updated
    //   chain head — preventing divergent chains.
    //
    // Failure semantics:
    //   Any SqlException during the transaction triggers a rollback and is
    //   wrapped in a LedgerWriteException so the caller (the plugin) can
    //   surface it as a posting failure. The Dataverse transaction the
    //   plugin lives inside will then roll back too, keeping the JE at
    //   its prior status. Retry-friendly.
    //
    // Caller responsibility:
    //   - All rows in `batch` must share the same EntityId (defensive check enforces).
    //   - Order rows by LineNumber before passing in (hash chain is order-sensitive).
    //   - EntryUid must be unset (Guid.Empty) — this class generates it.
    public class LedgerWriter
    {
        public const int CommandTimeoutSeconds = 60;

        public class WrittenRow
        {
            public Guid EntryUid { get; set; }
            public int LineNumber { get; set; }
            public byte[] RowHash { get; set; }
            public byte[] PreviousRowHash { get; set; }
        }

        public List<WrittenRow> WriteBatch(string connectionString, IList<LedgerRow> batch)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString is required (empty or null).", nameof(connectionString));
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (batch.Count == 0) return new List<WrittenRow>();

            var entityIds = batch.Select(r => r.EntityId).Distinct().ToList();
            if (entityIds.Count != 1)
            {
                throw new ArgumentException(
                    "All rows in a batch must share one EntityId; got " + entityIds.Count + " distinct values.",
                    nameof(batch));
            }
            var entityId = entityIds[0];

            // Stamp EntryUid for each row in-memory so the hash is computed
            // over a known value. The DB column has a default, but we
            // override so the hash and the persisted row agree.
            foreach (var row in batch)
            {
                if (row.EntryUid == Guid.Empty) row.EntryUid = Guid.NewGuid();
            }

            var results = new List<WrittenRow>(batch.Count);
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var tran = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        var prevHash = ReadChainHeadWithLock(cn, tran, entityId);

                        foreach (var row in batch)
                        {
                            row.EntryUid = row.EntryUid == Guid.Empty ? Guid.NewGuid() : row.EntryUid;
                            var rowHash = LedgerRowHasher.ComputeRowHash(row, prevHash);

                            InsertRow(cn, tran, row, prevHash, rowHash);

                            results.Add(new WrittenRow
                            {
                                EntryUid = row.EntryUid,
                                LineNumber = row.LineNumber,
                                RowHash = rowHash,
                                PreviousRowHash = prevHash,
                            });
                            prevHash = rowHash;
                        }

                        tran.Commit();
                    }
                    catch (SqlException ex)
                    {
                        SafeRollback(tran);
                        throw new LedgerWriteException(
                            "SQL failure during ledger dual-write for EntityId " + entityId + ": " + ex.Message, ex);
                    }
                    catch (Exception ex)
                    {
                        SafeRollback(tran);
                        throw new LedgerWriteException(
                            "Unexpected failure during ledger dual-write for EntityId " + entityId + ": " + ex.Message, ex);
                    }
                }
            }
            return results;
        }

        private static void SafeRollback(SqlTransaction tran)
        {
            try { tran.Rollback(); } catch { /* connection may be already dead */ }
        }

        private static byte[] ReadChainHeadWithLock(SqlConnection cn, SqlTransaction tran, Guid entityId)
        {
            const string sql = @"
SELECT TOP 1 RowHash
FROM   ledger.GeneralLedgerEntries WITH (UPDLOCK, HOLDLOCK)
WHERE  EntityId = @EntityId
ORDER  BY EntryId DESC;";

            using (var cmd = new SqlCommand(sql, cn, tran))
            {
                cmd.CommandTimeout = CommandTimeoutSeconds;
                cmd.Parameters.Add("@EntityId", SqlDbType.UniqueIdentifier).Value = entityId;
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return LedgerRowHasher.Genesis();
                }
                var bytes = (byte[])result;
                if (bytes.Length != 32)
                {
                    throw new LedgerWriteException(
                        "Chain head row for EntityId " + entityId + " has RowHash of length " + bytes.Length +
                        " (expected 32). Possible chain corruption — refusing to write.");
                }
                return bytes;
            }
        }

        private static void InsertRow(
            SqlConnection cn,
            SqlTransaction tran,
            LedgerRow row,
            byte[] prevHash,
            byte[] rowHash)
        {
            const string sql = @"
INSERT INTO ledger.GeneralLedgerEntries
  (EntryUid, EntityId, JournalEntryId, JournalEntryLineId, JournalEntryNumber, LineNumber,
   FiscalPeriodId, TransactionDate, PostedAtUtc, AccountId, AccountCode, AccountName,
   DebitAmount, CreditAmount, CurrencyCode, Memo, SourceModule, SourceDocumentRef,
   InterCompanyPairId, InterCompanyEntityId, ReversesEntryId,
   PostedByUserId, PostedByPrincipalName,
   ApprovedByUserId, ApprovedByPrincipalName, ApprovedAtUtc,
   PreviousRowHash, RowHash)
VALUES
  (@EntryUid, @EntityId, @JournalEntryId, @JournalEntryLineId, @JournalEntryNumber, @LineNumber,
   @FiscalPeriodId, @TransactionDate, @PostedAtUtc, @AccountId, @AccountCode, @AccountName,
   @DebitAmount, @CreditAmount, @CurrencyCode, @Memo, @SourceModule, @SourceDocumentRef,
   @InterCompanyPairId, @InterCompanyEntityId, @ReversesEntryId,
   @PostedByUserId, @PostedByPrincipalName,
   @ApprovedByUserId, @ApprovedByPrincipalName, @ApprovedAtUtc,
   @PreviousRowHash, @RowHash);";

            using (var cmd = new SqlCommand(sql, cn, tran))
            {
                cmd.CommandTimeout = CommandTimeoutSeconds;
                cmd.Parameters.Add("@EntryUid", SqlDbType.UniqueIdentifier).Value = row.EntryUid;
                cmd.Parameters.Add("@EntityId", SqlDbType.UniqueIdentifier).Value = row.EntityId;
                cmd.Parameters.Add("@JournalEntryId", SqlDbType.UniqueIdentifier).Value = row.JournalEntryId;
                cmd.Parameters.Add("@JournalEntryLineId", SqlDbType.UniqueIdentifier).Value = row.JournalEntryLineId;
                cmd.Parameters.Add("@JournalEntryNumber", SqlDbType.NVarChar, 32).Value = row.JournalEntryNumber ?? (object)DBNull.Value;
                cmd.Parameters.Add("@LineNumber", SqlDbType.Int).Value = row.LineNumber;
                cmd.Parameters.Add("@FiscalPeriodId", SqlDbType.UniqueIdentifier).Value = row.FiscalPeriodId;
                cmd.Parameters.Add("@TransactionDate", SqlDbType.Date).Value = row.TransactionDate.Date;
                cmd.Parameters.Add("@PostedAtUtc", SqlDbType.DateTime2, 3).Value = row.PostedAtUtc;
                cmd.Parameters.Add("@AccountId", SqlDbType.UniqueIdentifier).Value = row.AccountId;
                cmd.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 32).Value = (object)row.AccountCode ?? DBNull.Value;
                cmd.Parameters.Add("@AccountName", SqlDbType.NVarChar, 200).Value = (object)row.AccountName ?? DBNull.Value;
                cmd.Parameters.Add("@DebitAmount", SqlDbType.Decimal).Value = row.DebitAmount;
                cmd.Parameters[cmd.Parameters.Count - 1].Precision = 19;
                cmd.Parameters[cmd.Parameters.Count - 1].Scale = 4;
                cmd.Parameters.Add("@CreditAmount", SqlDbType.Decimal).Value = row.CreditAmount;
                cmd.Parameters[cmd.Parameters.Count - 1].Precision = 19;
                cmd.Parameters[cmd.Parameters.Count - 1].Scale = 4;
                cmd.Parameters.Add("@CurrencyCode", SqlDbType.Char, 3).Value = (object)row.CurrencyCode ?? DBNull.Value;
                cmd.Parameters.Add("@Memo", SqlDbType.NVarChar, 500).Value = (object)row.Memo ?? DBNull.Value;
                cmd.Parameters.Add("@SourceModule", SqlDbType.NVarChar, 20).Value = (object)row.SourceModule ?? DBNull.Value;
                cmd.Parameters.Add("@SourceDocumentRef", SqlDbType.NVarChar, 100).Value = (object)row.SourceDocumentRef ?? DBNull.Value;
                cmd.Parameters.Add("@InterCompanyPairId", SqlDbType.UniqueIdentifier).Value = (object)row.InterCompanyPairId ?? DBNull.Value;
                cmd.Parameters.Add("@InterCompanyEntityId", SqlDbType.UniqueIdentifier).Value = (object)row.InterCompanyEntityId ?? DBNull.Value;
                cmd.Parameters.Add("@ReversesEntryId", SqlDbType.BigInt).Value = (object)row.ReversesEntryId ?? DBNull.Value;
                cmd.Parameters.Add("@PostedByUserId", SqlDbType.UniqueIdentifier).Value = row.PostedByUserId;
                cmd.Parameters.Add("@PostedByPrincipalName", SqlDbType.NVarChar, 200).Value = (object)row.PostedByPrincipalName ?? DBNull.Value;
                cmd.Parameters.Add("@ApprovedByUserId", SqlDbType.UniqueIdentifier).Value = (object)row.ApprovedByUserId ?? DBNull.Value;
                cmd.Parameters.Add("@ApprovedByPrincipalName", SqlDbType.NVarChar, 200).Value = (object)row.ApprovedByPrincipalName ?? DBNull.Value;
                cmd.Parameters.Add("@ApprovedAtUtc", SqlDbType.DateTime2, 3).Value = (object)row.ApprovedAtUtc ?? DBNull.Value;
                cmd.Parameters.Add("@PreviousRowHash", SqlDbType.Binary, 32).Value = prevHash;
                cmd.Parameters.Add("@RowHash", SqlDbType.Binary, 32).Value = rowHash;

                cmd.ExecuteNonQuery();
            }
        }
    }

    public class LedgerWriteException : Exception
    {
        public LedgerWriteException(string message) : base(message) { }
        public LedgerWriteException(string message, Exception inner) : base(message, inner) { }
    }
}
