using DatastreamBooks.Plugins.Immutability;
using FluentAssertions;
using System;
using System.Globalization;
using Xunit;

namespace DatastreamBooks.Plugins.Tests.ImmutabilityTests
{
    // Tests pin down the deterministic byte layout of the hash chain.
    // Failing these tests usually means a chain-breaking format change.
    // Read docs/architecture/immutability-design.md §B before "fixing"
    // any of them — the right fix is usually to revert the source change.
    public class LedgerRowHasherTests
    {
        private static LedgerRow SampleRow()
        {
            return new LedgerRow
            {
                EntryUid = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
                EntityId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                JournalEntryId = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
                JournalEntryLineId = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD"),
                LineNumber = 1,
                FiscalPeriodId = Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE"),
                TransactionDate = new DateTime(2026, 5, 20),
                PostedAtUtc = new DateTime(2026, 5, 20, 16, 30, 45, 123, DateTimeKind.Utc),
                AccountId = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
                AccountCode = "1010",
                AccountName = "Cash and Equivalents",
                DebitAmount = 100m,
                CreditAmount = 0m,
                CurrencyCode = "USD",
                Memo = "test memo",
                SourceModule = "GL",
                SourceDocumentRef = null,
                InterCompanyPairId = null,
                InterCompanyEntityId = null,
                ReversesEntryId = null,
                PostedByUserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PostedByPrincipalName = "ryanm@plastic-recycling.net",
                ApprovedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ApprovedByPrincipalName = "pam@plastic-recycling.net",
                ApprovedAtUtc = new DateTime(2026, 5, 19, 22, 0, 0, DateTimeKind.Utc),
            };
        }

        // ---------- output shape ----------
        [Fact]
        public void Genesis_Is32ZeroBytes()
        {
            var g = LedgerRowHasher.Genesis();
            g.Should().HaveCount(32);
            g.Should().OnlyContain(b => b == 0);
        }

        [Fact]
        public void ComputeRowHash_Returns32Bytes()
        {
            var h = LedgerRowHasher.ComputeRowHash(SampleRow(), LedgerRowHasher.Genesis());
            h.Should().HaveCount(32);
        }

        [Fact]
        public void ComputeRowHash_NullRow_Throws()
        {
            Action act = () => LedgerRowHasher.ComputeRowHash(null, LedgerRowHasher.Genesis());
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ComputeRowHash_WrongPrevHashSize_Throws()
        {
            Action act = () => LedgerRowHasher.ComputeRowHash(SampleRow(), new byte[16]);
            act.Should().Throw<ArgumentException>().WithMessage("*32 bytes*");
        }

        [Fact]
        public void ComputeRowHash_NullPrevHash_Throws()
        {
            Action act = () => LedgerRowHasher.ComputeRowHash(SampleRow(), null);
            act.Should().Throw<ArgumentNullException>();
        }

        // ---------- determinism ----------
        [Fact]
        public void ComputeRowHash_SameInputs_SameOutput()
        {
            var prev = LedgerRowHasher.Genesis();
            var h1 = LedgerRowHasher.ComputeRowHash(SampleRow(), prev);
            var h2 = LedgerRowHasher.ComputeRowHash(SampleRow(), prev);
            h1.Should().BeEquivalentTo(h2);
        }

        [Fact]
        public void ComputeRowHash_DifferentEntryUid_DifferentOutput()
        {
            var prev = LedgerRowHasher.Genesis();
            var r1 = SampleRow();
            var r2 = SampleRow();
            r2.EntryUid = Guid.Parse("99999999-9999-9999-9999-999999999999");
            LedgerRowHasher.ComputeRowHash(r1, prev).Should().NotBeEquivalentTo(LedgerRowHasher.ComputeRowHash(r2, prev));
        }

        [Fact]
        public void ComputeRowHash_DifferentPrevHash_DifferentOutput()
        {
            var row = SampleRow();
            var prev1 = LedgerRowHasher.Genesis();
            var prev2 = new byte[32]; prev2[0] = 0x01;
            LedgerRowHasher.ComputeRowHash(row, prev1).Should().NotBeEquivalentTo(LedgerRowHasher.ComputeRowHash(row, prev2));
        }

        [Fact]
        public void ComputeRowHash_NullableFieldsNullOrSet_DifferentOutput()
        {
            // Memo null vs Memo "" should differ — null sentinel vs zero-length payload.
            var r1 = SampleRow(); r1.Memo = null;
            var r2 = SampleRow(); r2.Memo = "";
            var prev = LedgerRowHasher.Genesis();
            LedgerRowHasher.ComputeRowHash(r1, prev).Should().NotBeEquivalentTo(LedgerRowHasher.ComputeRowHash(r2, prev));
        }

        // ---------- chain continuity ----------
        [Fact]
        public void Chain_TwoRows_LinkViaPreviousHash()
        {
            // Row 2's hash is influenced by row 1's hash.
            // Mutating row 1 in any way changes row 2's hash too.
            var row1 = SampleRow();
            var row2 = SampleRow();
            row2.EntryUid = Guid.NewGuid();
            row2.LineNumber = 2;

            var h1_a = LedgerRowHasher.ComputeRowHash(row1, LedgerRowHasher.Genesis());
            var h2_a = LedgerRowHasher.ComputeRowHash(row2, h1_a);

            // Change row 1's debit amount — row 1's hash changes, so row 2's must too.
            row1.DebitAmount = 100.0001m;
            var h1_b = LedgerRowHasher.ComputeRowHash(row1, LedgerRowHasher.Genesis());
            var h2_b = LedgerRowHasher.ComputeRowHash(row2, h1_b);

            h1_a.Should().NotBeEquivalentTo(h1_b);
            h2_a.Should().NotBeEquivalentTo(h2_b);
        }

        // ---------- canonical-form formatters ----------
        [Fact]
        public void FormatGuid_IsLowercaseInvariant()
        {
            // The class uses Guid.ToString("D") which is already lowercase
            // hex on .NET, but explicit ToLowerInvariant guards against
            // future framework changes.
            var s = LedgerRowHasher.FormatGuid(Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
            s.Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        }

        [Fact]
        public void FormatDecimal_Always4Decimals()
        {
            LedgerRowHasher.FormatDecimal(1m).Should().Be("1.0000");
            LedgerRowHasher.FormatDecimal(1.5m).Should().Be("1.5000");
            LedgerRowHasher.FormatDecimal(1.23456m).Should().Be("1.2346"); // banker-rounded by F4
            LedgerRowHasher.FormatDecimal(-1m).Should().Be("-1.0000");
        }

        [Fact]
        public void FormatDecimal_IsInvariantCulture()
        {
            // Even if a build machine is set to de-DE, the format must use
            // '.' as decimal separator. This is implicit in InvariantCulture
            // but the test pins the contract.
            var previous = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                LedgerRowHasher.FormatDecimal(1234.5m).Should().Be("1234.5000");
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Fact]
        public void FormatTimestamp_LocalConvertedToUtc()
        {
            // Local-kind DateTime: the formatter must convert to UTC before
            // stringifying so machines in different time zones produce the
            // same canonical form for the same instant.
            var instantUtc = new DateTime(2026, 5, 20, 16, 30, 45, 123, DateTimeKind.Utc);
            var asLocal = instantUtc.ToLocalTime();
            LedgerRowHasher.FormatTimestamp(asLocal).Should().Be("2026-05-20T16:30:45.123Z");
        }

        [Fact]
        public void FormatTimestamp_AlreadyUtc_RoundTrips()
        {
            LedgerRowHasher.FormatTimestamp(new DateTime(2026, 5, 20, 16, 30, 45, 123, DateTimeKind.Utc))
                .Should().Be("2026-05-20T16:30:45.123Z");
        }

        [Fact]
        public void FormatDate_DateOnly_NoTimePart()
        {
            LedgerRowHasher.FormatDate(new DateTime(2026, 5, 20, 17, 0, 0)).Should().Be("2026-05-20");
        }
    }
}
