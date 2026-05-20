using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DatastreamBooks.Plugins.Posting
{
    // Single plugin class dispatches across the steps registered against it:
    //   1. Pre-Op  Update rm_journalentry          -> ValidateHeaderUpdate (immutability, SoD, totals, period)
    //   2. Pre-Op  Create/Update/Delete rm_journalentryline -> GuardLineAgainstLockedHeader
    //   3. Post-Op Create/Update/Delete rm_journalentryline -> RecomputeHeaderTotals
    //   4. Post-Op Update rm_journalentry          -> WriteToLedgerIfPosted (Phase 6B: Azure SQL dual-write + hash chain)
    //
    // Steps that need pre-state must register a PreImage named "PreImage".
    // The Phase 6B post-op step additionally requires a PostImage named "PostImage"
    // including rm_status, rm_entity, rm_journalentrynumber, rm_postedby_user, rm_posteddatetime.
    //
    // Phase 6B (2026-05-21): the post-op handler dual-writes Posted JEs to
    // ledger.GeneralLedgerEntries via PostJournalEntryLedgerWriter, with a
    // SHA-256 hash chain per EntityId. Any SQL failure throws
    // InvalidPluginExecutionException, which rolls back the Dataverse
    // transaction — keeping the two stores atomic.
    //
    // Note: rm_journalentrynumber is now a Dataverse native Auto Number column,
    // so the Create step on the header has been removed from the plugin.
    public class PostJournalEntryPlugin : PluginBase
    {
        public const int StatusDraft = 126190000;
        public const int StatusPendingApproval = 126190001;
        public const int StatusApproved = 126190002;
        public const int StatusPosted = 126190003;
        public const int StatusReversed = 126190004;
        public const int StatusVoided = 126190005;

        public const int FiscalPeriodOpen = 261910000;

        public const string HeaderEntity = "rm_journalentry";
        public const string LineEntity = "rm_journalentryline";

        public const string PreImageName = "PreImage";
        public const string PostImageName = "PostImage";

        // Optional override for tests; in production, the default writer is constructed inline.
        public PostJournalEntryLedgerWriter LedgerWriter { get; set; }

        public PostJournalEntryPlugin() : base(typeof(PostJournalEntryPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext ctx)
        {
            var ec = ctx.PluginExecutionContext;
            var svc = ctx.PluginUserService;
            var stage = ec.Stage;
            var msg = ec.MessageName;
            var entity = ec.PrimaryEntityName;

            ctx.Trace($"Dispatch: msg={msg} entity={entity} stage={stage}");

            if (entity == HeaderEntity)
            {
                if (stage == 20 && msg == "Update")
                {
                    ValidateHeaderUpdate(ctx, svc);
                    return;
                }
                if (stage == 40 && msg == "Update")
                {
                    WriteToLedgerIfPosted(ctx);
                    return;
                }
            }
            else if (entity == LineEntity)
            {
                if (stage == 20 && (msg == "Create" || msg == "Update" || msg == "Delete"))
                {
                    GuardLineAgainstLockedHeader(ctx, svc);
                    return;
                }
                if (stage == 40 && (msg == "Create" || msg == "Update" || msg == "Delete"))
                {
                    RecomputeHeaderTotals(ctx, svc);
                    return;
                }
            }
        }

        // ---------- 2. Header update: immutability + transition guards ----------
        internal void ValidateHeaderUpdate(ILocalPluginContext ctx, IOrganizationService svc)
        {
            var ec = ctx.PluginExecutionContext;
            var target = (Entity)ec.InputParameters["Target"];

            var pre = ec.PreEntityImages.Contains(PreImageName)
                ? ec.PreEntityImages[PreImageName]
                : svc.Retrieve(HeaderEntity, target.Id, AllHeaderColumns());

            var oldStatus = pre.GetAttributeValue<OptionSetValue>("rm_status")?.Value;
            var newStatus = target.Contains("rm_status")
                ? target.GetAttributeValue<OptionSetValue>("rm_status")?.Value
                : oldStatus;

            // 6. Locked statuses cannot be modified, except Posted -> Reversed.
            if (oldStatus == StatusPosted || oldStatus == StatusReversed || oldStatus == StatusVoided)
            {
                var allowed = oldStatus == StatusPosted && newStatus == StatusReversed;
                if (!allowed)
                {
                    throw new InvalidPluginExecutionException(
                        $"Journal Entry is locked (status={StatusLabel(oldStatus)}). " +
                        $"Posted JEs may only transition to Reversed; Reversed and Voided JEs are immutable.");
                }
            }

            // No status transition? Nothing more to validate.
            if (oldStatus == newStatus) return;

            if (newStatus == StatusApproved)
            {
                StampApproverIfMissing(target, pre, ec);
                ValidateSegregationOfDuties(target, pre);
                ValidateBalanced(target, pre);
                target["rm_approveddatetime"] = DateTime.UtcNow;
            }
            else if (newStatus == StatusPosted)
            {
                ValidateFiscalPeriodOpen(svc, target, pre);
                target["rm_postedby_user"] = new EntityReference("systemuser", ec.UserId);
                target["rm_posteddatetime"] = DateTime.UtcNow;
            }
        }

        // ---------- 2b. Phase 6B post-op dual-write to Azure SQL ledger ----------
        internal void WriteToLedgerIfPosted(ILocalPluginContext ctx)
        {
            var ec = ctx.PluginExecutionContext;

            // Detect transition: old status (PreImage) -> new status (Target or PostImage).
            int? oldStatus = null;
            if (ec.PreEntityImages.Contains(PreImageName))
            {
                oldStatus = ec.PreEntityImages[PreImageName].GetAttributeValue<OptionSetValue>("rm_status")?.Value;
            }

            int? newStatus = null;
            if (ec.InputParameters.Contains("Target") && ec.InputParameters["Target"] is Entity tgt && tgt.Contains("rm_status"))
            {
                newStatus = tgt.GetAttributeValue<OptionSetValue>("rm_status")?.Value;
            }
            if (newStatus == null && ec.PostEntityImages.Contains(PostImageName))
            {
                newStatus = ec.PostEntityImages[PostImageName].GetAttributeValue<OptionSetValue>("rm_status")?.Value;
            }

            // Only fire on transitions INTO Posted from a non-Posted state.
            if (newStatus != StatusPosted) return;
            if (oldStatus == StatusPosted) return;

            ctx.Trace($"Phase 6B: Approved->Posted transition detected for {ec.PrimaryEntityId}; starting ledger dual-write.");

            var writer = LedgerWriter ?? new PostJournalEntryLedgerWriter();
            writer.Execute(ctx, ec.PrimaryEntityId);
        }

        // ---------- 3. SoD ----------
        internal static void StampApproverIfMissing(Entity target, Entity pre, IPluginExecutionContext ec)
        {
            var approver = target.GetAttributeValue<EntityReference>("rm_approvedby_user")
                ?? pre.GetAttributeValue<EntityReference>("rm_approvedby_user");
            if (approver == null)
            {
                target["rm_approvedby_user"] = new EntityReference("systemuser", ec.UserId);
            }
        }

        internal static void ValidateSegregationOfDuties(Entity target, Entity pre)
        {
            var creator = target.GetAttributeValue<EntityReference>("rm_createdby_user")
                ?? pre.GetAttributeValue<EntityReference>("rm_createdby_user");
            var approver = target.GetAttributeValue<EntityReference>("rm_approvedby_user")
                ?? pre.GetAttributeValue<EntityReference>("rm_approvedby_user");

            if (creator == null)
            {
                throw new InvalidPluginExecutionException(
                    "Journal Entry has no rm_createdby_user. The originator must be recorded before approval.");
            }
            if (approver == null)
            {
                throw new InvalidPluginExecutionException(
                    "Journal Entry has no rm_approvedby_user. Approver must be set before status can reach Approved.");
            }
            if (creator.Id == approver.Id)
            {
                throw new InvalidPluginExecutionException(
                    "Segregation-of-duties violation: the user who created this Journal Entry cannot also approve it. " +
                    "rm_createdby_user and rm_approvedby_user must be different users.");
            }
        }

        // ---------- 4. Balanced totals ----------
        internal static void ValidateBalanced(Entity target, Entity pre)
        {
            var debit = target.Contains("rm_totaldebit")
                ? target.GetAttributeValue<decimal?>("rm_totaldebit") ?? 0m
                : pre.GetAttributeValue<decimal?>("rm_totaldebit") ?? 0m;
            var credit = target.Contains("rm_totalcredit")
                ? target.GetAttributeValue<decimal?>("rm_totalcredit") ?? 0m
                : pre.GetAttributeValue<decimal?>("rm_totalcredit") ?? 0m;

            if (debit != credit)
            {
                throw new InvalidPluginExecutionException(
                    $"Journal Entry is out of balance and cannot be Approved. " +
                    $"Total debits = {debit:N2}, total credits = {credit:N2}, difference = {(debit - credit):N2}.");
            }
            if (debit == 0m && credit == 0m)
            {
                throw new InvalidPluginExecutionException(
                    "Journal Entry has no debit or credit amounts. Add at least one balanced line pair before approving.");
            }
        }

        // ---------- 5. Fiscal period open ----------
        internal static void ValidateFiscalPeriodOpen(IOrganizationService svc, Entity target, Entity pre)
        {
            var fpRef = target.GetAttributeValue<EntityReference>("rm_fiscalperiod")
                ?? pre.GetAttributeValue<EntityReference>("rm_fiscalperiod");
            if (fpRef == null)
            {
                throw new InvalidPluginExecutionException(
                    "Journal Entry has no rm_fiscalperiod. The target period must be set before posting.");
            }

            var fp = svc.Retrieve("rm_fiscalperiod", fpRef.Id, new ColumnSet(true));
            var status = fp.GetAttributeValue<OptionSetValue>("rm_status")?.Value;
            if (status != FiscalPeriodOpen)
            {
                var name = fp.GetAttributeValue<string>("rm_periodname") ?? fpRef.Id.ToString();
                throw new InvalidPluginExecutionException(
                    $"Fiscal period '{name}' is not Open (status={FiscalPeriodStatusLabel(status)}). " +
                    $"Posting is blocked until the period is reopened or the JE is retargeted to an Open period.");
            }
        }

        // ---------- 6. Line guard: block writes against locked headers ----------
        internal void GuardLineAgainstLockedHeader(ILocalPluginContext ctx, IOrganizationService svc)
        {
            var ec = ctx.PluginExecutionContext;
            EntityReference parentRef = null;

            if (ec.MessageName == "Create" || ec.MessageName == "Update")
            {
                var target = ec.InputParameters["Target"] as Entity;
                if (target != null && target.Contains("rm_journalentry"))
                    parentRef = target.GetAttributeValue<EntityReference>("rm_journalentry");
            }

            if (parentRef == null && ec.PreEntityImages.Contains(PreImageName))
            {
                parentRef = ec.PreEntityImages[PreImageName].GetAttributeValue<EntityReference>("rm_journalentry");
            }

            if (parentRef == null)
            {
                // For Update/Delete without an image or input parent, fall back to retrieve.
                if (ec.MessageName == "Delete")
                {
                    var lineRef = (EntityReference)ec.InputParameters["Target"];
                    var line = svc.Retrieve(LineEntity, lineRef.Id, new ColumnSet(true));
                    parentRef = line.GetAttributeValue<EntityReference>("rm_journalentry");
                }
                else if (ec.MessageName == "Update")
                {
                    var line = svc.Retrieve(LineEntity, ec.PrimaryEntityId, new ColumnSet(true));
                    parentRef = line.GetAttributeValue<EntityReference>("rm_journalentry");
                }
            }

            if (parentRef == null) return; // no parent yet (shouldn't happen — required field)

            var parent = svc.Retrieve(HeaderEntity, parentRef.Id, new ColumnSet(true));
            var status = parent.GetAttributeValue<OptionSetValue>("rm_status")?.Value;
            if (status == StatusPosted || status == StatusReversed || status == StatusVoided)
            {
                var num = parent.GetAttributeValue<string>("rm_journalentrynumber") ?? parentRef.Id.ToString();
                throw new InvalidPluginExecutionException(
                    $"Cannot modify lines of Journal Entry '{num}' — its status is {StatusLabel(status)}. " +
                    $"Posted JEs are immutable; corrections must be made via a reversing JE.");
            }
        }

        // ---------- 7. Recompute header totals ----------
        internal void RecomputeHeaderTotals(ILocalPluginContext ctx, IOrganizationService svc)
        {
            var ec = ctx.PluginExecutionContext;
            EntityReference parentRef = null;

            if (ec.MessageName == "Create" || ec.MessageName == "Update")
            {
                var target = ec.InputParameters["Target"] as Entity;
                if (target != null && target.Contains("rm_journalentry"))
                    parentRef = target.GetAttributeValue<EntityReference>("rm_journalentry");
            }
            if (parentRef == null && ec.PreEntityImages.Contains(PreImageName))
            {
                parentRef = ec.PreEntityImages[PreImageName].GetAttributeValue<EntityReference>("rm_journalentry");
            }
            if (parentRef == null) return;

            // Sum live lines.  Note: on Delete the row is already gone in post-op,
            // which is exactly what we want.
            var query = new QueryExpression(LineEntity)
            {
                ColumnSet = new ColumnSet(true),
                NoLock = true,
            };
            query.Criteria.AddCondition("rm_journalentry", ConditionOperator.Equal, parentRef.Id);
            var lines = svc.RetrieveMultiple(query).Entities;

            decimal totalDebit = 0m;
            decimal totalCredit = 0m;
            foreach (var line in lines)
            {
                totalDebit += line.GetAttributeValue<decimal?>("rm_debit") ?? 0m;
                totalCredit += line.GetAttributeValue<decimal?>("rm_credit") ?? 0m;
            }

            var update = new Entity(HeaderEntity, parentRef.Id)
            {
                ["rm_totaldebit"] = totalDebit,
                ["rm_totalcredit"] = totalCredit,
            };
            ctx.Trace($"RecomputeHeaderTotals: parent={parentRef.Id} debit={totalDebit:N2} credit={totalCredit:N2} lines={lines.Count}");
            svc.Update(update);
        }

        // ---------- helpers ----------
        private static ColumnSet AllHeaderColumns() => new ColumnSet(true);

        private static string StatusLabel(int? status)
        {
            if (status == null) return "(unset)";
            switch (status.Value)
            {
                case StatusDraft: return "Draft";
                case StatusPendingApproval: return "PendingApproval";
                case StatusApproved: return "Approved";
                case StatusPosted: return "Posted";
                case StatusReversed: return "Reversed";
                case StatusVoided: return "Voided";
                default: return status.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string FiscalPeriodStatusLabel(int? status)
        {
            if (status == null) return "(unset)";
            switch (status.Value)
            {
                case FiscalPeriodOpen: return "Open";
                case 261910001: return "Closed";
                case 261910002: return "Locked";
                default: return status.Value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
