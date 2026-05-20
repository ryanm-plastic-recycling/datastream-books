using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DatastreamBooks.Plugins.Immutability
{
    // SHA-256 hash chain for ledger.GeneralLedgerEntries.
    //
    // Algorithm:
    //   RowHash = SHA256( canonical_payload || PreviousRowHash )
    //
    // canonical_payload is the concatenation of length-prefixed UTF-8
    // serializations of each field in CANONICAL_FIELD_ORDER. Each field
    // is written as:
    //
    //   For null     : 4 bytes  0xFFFFFFFF  (sentinel)
    //   For value    : 4 bytes  big-endian length, then UTF-8 bytes
    //
    // PreviousRowHash is always 32 raw bytes appended after the payload.
    // The genesis row for a given EntityId uses 32 zero bytes.
    //
    // Field-level serialization rules:
    //   Guid          -> ToString("D") lowercase invariant
    //   int / long    -> ToString(InvariantCulture) (no thousands separator)
    //   decimal       -> ToString("F4", InvariantCulture) (always 4 decimals)
    //   DateTime date -> "yyyy-MM-dd"
    //   DateTime ts   -> ToUniversalTime() + "yyyy-MM-ddTHH:mm:ss.fffZ"
    //   string        -> as-is (length-prefix makes escaping unnecessary)
    //
    // Field order is frozen here. Changing it is a chain-breaking event:
    // a verification migration must re-hash existing rows under the new
    // format and prove continuity, OR a new chain must start. Do not
    // reorder lightly. See docs/architecture/immutability-design.md §B.
    public static class LedgerRowHasher
    {
        // 32 zero bytes — the PreviousRowHash for the very first row of
        // any given EntityId chain.
        public static byte[] Genesis()
        {
            return new byte[32];
        }

        public static byte[] ComputeRowHash(LedgerRow row, byte[] previousRowHash)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (previousRowHash == null) throw new ArgumentNullException(nameof(previousRowHash));
            if (previousRowHash.Length != 32)
                throw new ArgumentException(
                    "PreviousRowHash must be exactly 32 bytes (SHA-256 output size).",
                    nameof(previousRowHash));

            using (var stream = new MemoryStream())
            {
                // CANONICAL FIELD ORDER — DO NOT REORDER.
                WriteField(stream, FormatGuid(row.EntryUid));
                WriteField(stream, FormatGuid(row.EntityId));
                WriteField(stream, FormatGuid(row.JournalEntryId));
                WriteField(stream, FormatGuid(row.JournalEntryLineId));
                WriteField(stream, FormatInt(row.LineNumber));
                WriteField(stream, FormatGuid(row.FiscalPeriodId));
                WriteField(stream, FormatDate(row.TransactionDate));
                WriteField(stream, FormatTimestamp(row.PostedAtUtc));
                WriteField(stream, FormatGuid(row.AccountId));
                WriteField(stream, row.AccountCode);
                WriteField(stream, row.AccountName);
                WriteField(stream, FormatDecimal(row.DebitAmount));
                WriteField(stream, FormatDecimal(row.CreditAmount));
                WriteField(stream, row.CurrencyCode);
                WriteField(stream, row.Memo);                              // nullable
                WriteField(stream, row.SourceModule);
                WriteField(stream, row.SourceDocumentRef);                  // nullable
                WriteField(stream, FormatGuidNullable(row.InterCompanyPairId));
                WriteField(stream, FormatGuidNullable(row.InterCompanyEntityId));
                WriteField(stream, FormatLongNullable(row.ReversesEntryId));
                WriteField(stream, FormatGuid(row.PostedByUserId));
                WriteField(stream, row.PostedByPrincipalName);
                WriteField(stream, FormatGuidNullable(row.ApprovedByUserId));
                WriteField(stream, row.ApprovedByPrincipalName);            // nullable
                WriteField(stream, FormatTimestampNullable(row.ApprovedAtUtc));

                // Trailing previous-row-hash (raw bytes, NOT length-prefixed).
                stream.Write(previousRowHash, 0, 32);

                stream.Position = 0;
                using (var sha = SHA256.Create())
                {
                    return sha.ComputeHash(stream);
                }
            }
        }

        // --------- field writer ---------
        private static readonly byte[] NullSentinel = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        private static void WriteField(Stream stream, string value)
        {
            if (value == null)
            {
                stream.Write(NullSentinel, 0, 4);
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteUInt32Be(stream, (uint)bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteUInt32Be(Stream stream, uint value)
        {
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }

        // --------- canonical field formatters ---------
        // Each formatter has exactly one canonical string form per input;
        // tests assert this property. Adding overloads or culture-sensitive
        // formatting here is a chain-breaking change.

        public static string FormatGuid(Guid g)
        {
            return g.ToString("D", CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        public static string FormatGuidNullable(Guid? g)
        {
            return g.HasValue ? FormatGuid(g.Value) : null;
        }

        public static string FormatInt(int i)
        {
            return i.ToString(CultureInfo.InvariantCulture);
        }

        public static string FormatLongNullable(long? l)
        {
            return l.HasValue ? l.Value.ToString(CultureInfo.InvariantCulture) : null;
        }

        public static string FormatDecimal(decimal d)
        {
            return d.ToString("F4", CultureInfo.InvariantCulture);
        }

        public static string FormatDate(DateTime d)
        {
            // Date-only; ignore time component if any.
            return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static string FormatTimestamp(DateTime d)
        {
            var utc = d.Kind == DateTimeKind.Utc ? d : d.ToUniversalTime();
            return utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        public static string FormatTimestampNullable(DateTime? d)
        {
            return d.HasValue ? FormatTimestamp(d.Value) : null;
        }

        // --------- convenience for callers that work in hex ---------
        public static string ToHex(byte[] bytes)
        {
            if (bytes == null) return null;
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", bytes[i]);
            return sb.ToString();
        }
    }
}
