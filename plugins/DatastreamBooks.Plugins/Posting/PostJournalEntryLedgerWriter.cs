using DatastreamBooks.Plugins.Immutability;
using DatastreamBooks.Plugins.KeyVault;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DatastreamBooks.Plugins.Posting
{
    // Phase 6B: orchestrates the Approved→Posted post-op dual-write into
    // Azure SQL ledger.GeneralLedgerEntries.
    //
    // Triggered by PostJournalEntryPlugin's Stage 40 (PostOperation)
    // handler on Update of rm_journalentry, only when the status transitions
    // into Posted. The Dataverse transaction we run inside is committed
    // ONLY if this method returns without throwing — so a SQL failure
    // here rolls back the rm_status flip via plugin exception, keeping
    // Dataverse and the ledger consistent.
    //
    // Connection string acquisition:
    //   1. Read 5 Dataverse Environment Variables (tenant, client id,
    //      client secret, vault URL, secret name) via the elevated
    //      organization service (system user, so Secret env vars decrypt).
    //   2. KeyVaultSecretReader fetches the SQL connection string from
    //      Key Vault using the SP credentials; result is cached in a
    //      process-static field with a 5-minute TTL.
    //
    // Hash chain:
    //   LedgerWriter takes care of locking the per-entity chain head and
    //   computing each row's hash; see Immutability/LedgerWriter.cs.
    public class PostJournalEntryLedgerWriter
    {
        // Default factory: production code path. Tests can substitute
        // a fake by passing their own writer + connection-string source.
        public Func<IServiceProvider, IOrganizationService> ElevatedServiceFactory { get; set; }
        public Func<IOrganizationService, string> ConnectionStringSource { get; set; }
        public LedgerWriter Writer { get; set; } = new LedgerWriter();

        public static string DefaultConnectionStringSource(IOrganizationService systemSvc)
        {
            var tenantId = DataverseEnvironmentVariables.GetValue(systemSvc, DataverseEnvironmentVariables.Schema.KvTenantId);
            var clientId = DataverseEnvironmentVariables.GetValue(systemSvc, DataverseEnvironmentVariables.Schema.KvClientId);
            var clientSecret = DataverseEnvironmentVariables.GetValue(systemSvc, DataverseEnvironmentVariables.Schema.KvClientSecret);
            var vaultUrl = DataverseEnvironmentVariables.GetValue(systemSvc, DataverseEnvironmentVariables.Schema.KvUrl);
            var secretName = DataverseEnvironmentVariables.GetValue(systemSvc, DataverseEnvironmentVariables.Schema.KvSecretName);
            return KeyVaultSecretReader.GetSecret(tenantId, clientId, clientSecret, vaultUrl, secretName);
        }

        public List<LedgerWriter.WrittenRow> Execute(ILocalPluginContext ctx, Guid headerId)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var ec = ctx.PluginExecutionContext;

            // Resolve services.
            // Use OrgSvcFactory(null) → system-user-context service. Required
            // for reading Secret-type Environment Variables and for ensuring
            // we can retrieve lines regardless of the calling user's row-level
            // security on rm_journalentryline.
            var systemSvc = ctx.OrgSvcFactory.CreateOrganizationService(null);

            var connStr = (ConnectionStringSource ?? DefaultConnectionStringSource)(systemSvc);
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new InvalidPluginExecutionException(
                    "PostJournalEntryLedgerWriter: connection string resolved from Key Vault is empty. " +
                    "Cannot proceed with ledger dual-write.");
            }

            // Retrieve header with everything we need for the ledger payload.
            var header = systemSvc.Retrieve("rm_journalentry", headerId, new ColumnSet(
                "rm_journalentryid",
                "rm_journalentrynumber",
                "rm_journaldescription",
                "rm_entity",
                "rm_fiscalperiod",
                "rm_postingdate",
                "rm_status",
                "rm_referencenumber",
                "rm_sourcedocument",
                "rm_createdby_user",
                "rm_approvedby_user",
                "rm_postedby_user",
                "rm_approveddatetime",
                "rm_posteddatetime",
                "rm_reversesje"));

            var entityRef = header.GetAttributeValue<EntityReference>("rm_entity");
            var fpRef = header.GetAttributeValue<EntityReference>("rm_fiscalperiod");
            var postingDate = header.GetAttributeValue<DateTime>("rm_postingdate");
            var postedAt = header.GetAttributeValue<DateTime?>("rm_posteddatetime") ?? DateTime.UtcNow;
            var approvedAt = header.GetAttributeValue<DateTime?>("rm_approveddatetime");
            var jeNumber = header.GetAttributeValue<string>("rm_journalentrynumber");
            var memo = header.GetAttributeValue<string>("rm_journaldescription");
            var sourceRef = header.GetAttributeValue<string>("rm_referencenumber");
            var postedByRef = header.GetAttributeValue<EntityReference>("rm_postedby_user");
            var approvedByRef = header.GetAttributeValue<EntityReference>("rm_approvedby_user");

            if (entityRef == null)
                throw new InvalidPluginExecutionException("rm_journalentry " + headerId + " is missing rm_entity — cannot dual-write.");
            if (fpRef == null)
                throw new InvalidPluginExecutionException("rm_journalentry " + headerId + " is missing rm_fiscalperiod — cannot dual-write.");
            if (postedByRef == null)
                throw new InvalidPluginExecutionException("rm_journalentry " + headerId + " is missing rm_postedby_user — cannot dual-write.");

            // Look up the poster's UPN (and approver's, if any).
            var postedByUpn = LookupUserUpn(systemSvc, postedByRef.Id);
            var approvedByUpn = approvedByRef != null ? LookupUserUpn(systemSvc, approvedByRef.Id) : null;

            // Pull lines with COA fields linked in one shot.
            var lineQuery = new QueryExpression("rm_journalentryline")
            {
                ColumnSet = new ColumnSet(
                    "rm_journalentrylineid",
                    "rm_linenumber",
                    "rm_account",
                    "rm_debit",
                    "rm_credit",
                    "rm_linedescription",
                    "rm_externalrefnumber"),
                NoLock = false,
            };
            lineQuery.Criteria.AddCondition("rm_journalentry", ConditionOperator.Equal, headerId);
            lineQuery.AddOrder("rm_linenumber", OrderType.Ascending);

            var coaLink = lineQuery.AddLink(
                "rm_chartofaccount",
                "rm_account",
                "rm_chartofaccountid",
                JoinOperator.Inner);
            coaLink.EntityAlias = "coa";
            coaLink.Columns = new ColumnSet("rm_accountnumber", "rm_chartofaccountname");

            var lines = systemSvc.RetrieveMultiple(lineQuery).Entities;
            if (lines.Count == 0)
            {
                throw new InvalidPluginExecutionException(
                    "rm_journalentry " + jeNumber + " has no rm_journalentryline rows — cannot post empty JE.");
            }

            // Build the batch.
            var batch = new List<LedgerRow>(lines.Count);
            foreach (var line in lines)
            {
                var accountRef = line.GetAttributeValue<EntityReference>("rm_account");
                var accountCode = line.GetAttributeValue<AliasedValue>("coa.rm_accountnumber")?.Value as string;
                var accountName = line.GetAttributeValue<AliasedValue>("coa.rm_chartofaccountname")?.Value as string;
                var debit = line.GetAttributeValue<decimal?>("rm_debit") ?? 0m;
                var credit = line.GetAttributeValue<decimal?>("rm_credit") ?? 0m;
                var lineNo = line.GetAttributeValue<int?>("rm_linenumber") ?? 0;
                var lineMemo = line.GetAttributeValue<string>("rm_linedescription");
                var lineExtRef = line.GetAttributeValue<string>("rm_externalrefnumber");

                if (accountRef == null)
                    throw new InvalidPluginExecutionException("rm_journalentryline " + line.Id + " is missing rm_account.");
                if (string.IsNullOrEmpty(accountCode))
                    throw new InvalidPluginExecutionException("rm_journalentryline " + line.Id + " could not resolve account code.");
                if (lineNo <= 0)
                    throw new InvalidPluginExecutionException("rm_journalentryline " + line.Id + " has invalid rm_linenumber " + lineNo + ".");

                batch.Add(new LedgerRow
                {
                    EntryUid = Guid.NewGuid(),
                    EntityId = entityRef.Id,
                    JournalEntryId = headerId,
                    JournalEntryNumber = jeNumber,
                    JournalEntryLineId = line.Id,
                    LineNumber = lineNo,
                    FiscalPeriodId = fpRef.Id,
                    TransactionDate = postingDate.Date,
                    PostedAtUtc = postedAt.Kind == DateTimeKind.Utc ? postedAt : postedAt.ToUniversalTime(),
                    AccountId = accountRef.Id,
                    AccountCode = accountCode,
                    AccountName = accountName ?? string.Empty,
                    DebitAmount = debit,
                    CreditAmount = credit,
                    CurrencyCode = "USD", // v1 USD-only; multi-currency is post-cutover
                    Memo = string.IsNullOrEmpty(lineMemo) ? memo : lineMemo,
                    SourceModule = "GL",
                    SourceDocumentRef = string.IsNullOrEmpty(lineExtRef) ? sourceRef : lineExtRef,
                    InterCompanyPairId = null,
                    InterCompanyEntityId = null,
                    ReversesEntryId = null, // linkage preserved at JE level (rm_reversesje); ledger linkage in a later phase
                    PostedByUserId = postedByRef.Id,
                    PostedByPrincipalName = postedByUpn ?? postedByRef.Id.ToString(),
                    ApprovedByUserId = approvedByRef?.Id,
                    ApprovedByPrincipalName = approvedByUpn,
                    ApprovedAtUtc = approvedAt.HasValue
                        ? (approvedAt.Value.Kind == DateTimeKind.Utc ? approvedAt.Value : approvedAt.Value.ToUniversalTime())
                        : (DateTime?)null,
                });
            }

            ctx.Trace(string.Format(CultureInfo.InvariantCulture,
                "Phase6B dual-write: JE={0} EntityId={1} lines={2}",
                jeNumber, entityRef.Id, batch.Count));

            try
            {
                var written = Writer.WriteBatch(connStr, batch);
                ctx.Trace(string.Format(CultureInfo.InvariantCulture,
                    "Phase6B dual-write complete: {0} ledger rows inserted; head hash={1}",
                    written.Count,
                    written.Count > 0 ? LedgerRowHasher.ToHex(written[written.Count - 1].RowHash).Substring(0, 16) + "..." : "n/a"));
                return written;
            }
            catch (LedgerWriteException ex)
            {
                throw new InvalidPluginExecutionException(
                    "Azure SQL ledger dual-write failed for JE " + jeNumber + ": " + ex.Message +
                    " — Dataverse transaction will roll back; the JE remains at its prior status. Retry once the underlying issue is fixed.",
                    ex);
            }
            catch (KeyVaultSecretReaderException ex)
            {
                throw new InvalidPluginExecutionException(
                    "Could not read SQL connection string from Key Vault for JE " + jeNumber + ": " + ex.Message +
                    " — verify the cicd SP credential and Key Vault RBAC.",
                    ex);
            }
        }

        private static string LookupUserUpn(IOrganizationService svc, Guid userId)
        {
            try
            {
                var user = svc.Retrieve("systemuser", userId, new ColumnSet("domainname", "fullname"));
                var upn = user.GetAttributeValue<string>("domainname");
                if (!string.IsNullOrEmpty(upn)) return upn;
                return user.GetAttributeValue<string>("fullname");
            }
            catch
            {
                // Non-fatal: fall back to GUID-as-string in caller.
                return null;
            }
        }
    }
}
