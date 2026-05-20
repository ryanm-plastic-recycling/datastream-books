using DatastreamBooks.Plugins.Immutability;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace DatastreamBooks.Plugins.Tests.ImmutabilityTests
{
    // These tests do NOT touch SQL — they verify the argument-validation
    // and pre-write defensive checks in LedgerWriter. Full integration
    // testing against ledger.GeneralLedgerEntries is performed manually
    // during Phase 6B end-to-end validation (a real JE posted in
    // PRI-Books-Dev), documented in immutability-validation.md.
    public class LedgerWriterArgValidationTests
    {
        [Fact]
        public void WriteBatch_NullConnectionString_Throws()
        {
            var w = new LedgerWriter();
            Action act = () => w.WriteBatch(null, new[] { Sample() });
            act.Should().Throw<ArgumentException>()
                .WithMessage("*connectionString*");
        }

        [Fact]
        public void WriteBatch_EmptyConnectionString_Throws()
        {
            var w = new LedgerWriter();
            Action act = () => w.WriteBatch("   ", new[] { Sample() });
            act.Should().Throw<ArgumentException>()
                .WithMessage("*connectionString*");
        }

        [Fact]
        public void WriteBatch_NullBatch_Throws()
        {
            var w = new LedgerWriter();
            Action act = () => w.WriteBatch("Server=x", null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteBatch_EmptyBatch_ReturnsEmpty_NoConnectionAttempt()
        {
            // An empty batch is a no-op — we don't even open a connection,
            // so the (junk) connection string never matters.
            var w = new LedgerWriter();
            var result = w.WriteBatch("Server=junk;Connection Timeout=1", new List<LedgerRow>());
            result.Should().BeEmpty();
        }

        [Fact]
        public void WriteBatch_MultipleEntityIds_Throws()
        {
            // All rows in one batch must share an EntityId so the per-entity
            // hash chain is well-defined. Mixing breaks the locking semantics.
            var w = new LedgerWriter();
            var rows = new[]
            {
                Sample(Guid.NewGuid()),
                Sample(Guid.NewGuid()),
            };
            Action act = () => w.WriteBatch("Server=junk;Connection Timeout=1", rows);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*EntityId*2 distinct*");
        }

        // --- helper ---
        private static LedgerRow Sample(Guid? entityId = null)
        {
            return new LedgerRow
            {
                EntryUid = Guid.NewGuid(),
                EntityId = entityId ?? Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                JournalEntryId = Guid.NewGuid(),
                JournalEntryNumber = "JE-DEFAULT-000001",
                JournalEntryLineId = Guid.NewGuid(),
                LineNumber = 1,
                FiscalPeriodId = Guid.NewGuid(),
                TransactionDate = new DateTime(2026, 5, 20),
                PostedAtUtc = new DateTime(2026, 5, 20, 16, 0, 0, DateTimeKind.Utc),
                AccountId = Guid.NewGuid(),
                AccountCode = "1010",
                AccountName = "Cash",
                DebitAmount = 100m,
                CreditAmount = 0m,
                CurrencyCode = "USD",
                SourceModule = "GL",
                PostedByUserId = Guid.NewGuid(),
                PostedByPrincipalName = "test@example.com",
            };
        }
    }
}
