using System;

namespace DatastreamBooks.Plugins.Immutability
{
    // DTO representing one row destined for ledger.GeneralLedgerEntries.
    // Field order in this class is documentary; the canonical hash order
    // is enforced by LedgerRowHasher, not by reflection over this type.
    // Adding a field here without updating LedgerRowHasher will NOT change
    // the chain — by design. Hash format changes require a verification
    // migration; see docs/architecture/immutability-design.md §B.
    public sealed class LedgerRow
    {
        public Guid EntryUid { get; set; }
        public Guid EntityId { get; set; }
        public Guid JournalEntryId { get; set; }
        public string JournalEntryNumber { get; set; }   // required by schema; NOT in hash chain
        public Guid JournalEntryLineId { get; set; }
        public int LineNumber { get; set; }
        public Guid FiscalPeriodId { get; set; }
        public DateTime TransactionDate { get; set; }   // date-only
        public DateTime PostedAtUtc { get; set; }       // UTC timestamp
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string CurrencyCode { get; set; }
        public string Memo { get; set; }                 // nullable
        public string SourceModule { get; set; }
        public string SourceDocumentRef { get; set; }    // nullable
        public Guid? InterCompanyPairId { get; set; }
        public Guid? InterCompanyEntityId { get; set; }
        public long? ReversesEntryId { get; set; }
        public Guid PostedByUserId { get; set; }
        public string PostedByPrincipalName { get; set; }
        public Guid? ApprovedByUserId { get; set; }
        public string ApprovedByPrincipalName { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
    }
}
