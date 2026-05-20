using DatastreamBooks.Plugins.Posting;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Enums;
using FakeXrmEasy.Middleware;
using FakeXrmEasy.Middleware.Crud;
using FakeXrmEasy.Middleware.Messages;
using FakeXrmEasy.Plugins;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using Xunit;

namespace DatastreamBooks.Plugins.Tests.PostingTests
{
    // Phase 6A unit tests.  One test per documented validation rule plus a
    // few interaction checks (totals after multiple line writes, sequence
    // per-entity).
    public class PostJournalEntryPluginTests
    {
        // ---------- shared fixture helpers ----------
        private static readonly Guid CreatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ApproverId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid CallerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private static IXrmFakedContext NewContext(Guid? callingUser = null)
        {
            // FXE 2.x requires the middleware-built context for CRUD to function.
            // Internal-only project — NonCommercial covers the licensing requirement.
            var ctx = MiddlewareBuilder
                .New()
                .AddCrud()
                .AddFakeMessageExecutors()
                .UseCrud()
                .UseMessages()
                .SetLicense(FakeXrmEasyLicense.NonCommercial)
                .Build();
            ctx.CallerProperties.CallerId = new EntityReference("systemuser", callingUser ?? CallerId);
            return ctx;
        }

        private static Entity Entity(string code, Guid? id = null)
        {
            return new Entity("rm_entity", id ?? Guid.NewGuid())
            {
                ["rm_entitycode"] = code,
                ["rm_entityname"] = $"Entity {code}",
            };
        }

        private static Entity FiscalPeriod(int status = PostJournalEntryPlugin.FiscalPeriodOpen, string name = "2026-05", Guid? id = null)
        {
            return new Entity("rm_fiscalperiod", id ?? Guid.NewGuid())
            {
                ["rm_status"] = new OptionSetValue(status),
                ["rm_periodname"] = name,
            };
        }

        private static Entity Header(
            Guid id,
            Guid entityId,
            int status,
            Guid? createdBy = null,
            Guid? approvedBy = null,
            decimal totalDebit = 0m,
            decimal totalCredit = 0m,
            Guid? fiscalPeriodId = null,
            string number = null)
        {
            var h = new Entity("rm_journalentry", id)
            {
                ["rm_entity"] = new EntityReference("rm_entity", entityId),
                ["rm_status"] = new OptionSetValue(status),
                ["rm_totaldebit"] = totalDebit,
                ["rm_totalcredit"] = totalCredit,
            };
            if (createdBy.HasValue) h["rm_createdby_user"] = new EntityReference("systemuser", createdBy.Value);
            if (approvedBy.HasValue) h["rm_approvedby_user"] = new EntityReference("systemuser", approvedBy.Value);
            if (fiscalPeriodId.HasValue) h["rm_fiscalperiod"] = new EntityReference("rm_fiscalperiod", fiscalPeriodId.Value);
            if (number != null) h["rm_journalentrynumber"] = number;
            return h;
        }

        private static Entity Line(Guid headerId, Guid entityId, decimal debit, decimal credit, Guid? id = null)
        {
            return new Entity("rm_journalentryline", id ?? Guid.NewGuid())
            {
                ["rm_journalentry"] = new EntityReference("rm_journalentry", headerId),
                ["rm_entity"] = new EntityReference("rm_entity", entityId),
                ["rm_debit"] = debit,
                ["rm_credit"] = credit,
            };
        }

        // ---------- 2. Header-totals maintenance ----------
        [Fact]
        public void HeaderTotals_Recompute_FromExistingLines()
        {
            var ctx = NewContext();
            var entity = Entity("DEFAULT");
            var fp = FiscalPeriod();
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusDraft, fiscalPeriodId: fp.Id);
            var existingLine = Line(header.Id, entity.Id, 100m, 0m);
            var newLine = Line(header.Id, entity.Id, 0m, 100m);
            ctx.Initialize(new Entity[] { entity, fp, header, existingLine, newLine });

            ExecuteLinePostOp(ctx, "Create", newLine);

            var reloaded = ctx.GetOrganizationService().Retrieve("rm_journalentry", header.Id,
                new Microsoft.Xrm.Sdk.Query.ColumnSet("rm_totaldebit", "rm_totalcredit"));
            reloaded.GetAttributeValue<decimal>("rm_totaldebit").Should().Be(100m);
            reloaded.GetAttributeValue<decimal>("rm_totalcredit").Should().Be(100m);
        }

        [Fact]
        public void HeaderTotals_MultiLine_SumsCorrectly()
        {
            var ctx = NewContext();
            var entity = Entity("DEFAULT");
            var fp = FiscalPeriod();
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusDraft, fiscalPeriodId: fp.Id);
            var l1 = Line(header.Id, entity.Id, 250.50m, 0m);
            var l2 = Line(header.Id, entity.Id, 749.50m, 0m);
            var l3 = Line(header.Id, entity.Id, 0m, 500m);
            var l4 = Line(header.Id, entity.Id, 0m, 500m);
            ctx.Initialize(new Entity[] { entity, fp, header, l1, l2, l3, l4 });

            ExecuteLinePostOp(ctx, "Create", l4);

            var reloaded = ctx.GetOrganizationService().Retrieve("rm_journalentry", header.Id,
                new Microsoft.Xrm.Sdk.Query.ColumnSet("rm_totaldebit", "rm_totalcredit"));
            reloaded.GetAttributeValue<decimal>("rm_totaldebit").Should().Be(1000m);
            reloaded.GetAttributeValue<decimal>("rm_totalcredit").Should().Be(1000m);
        }

        // ---------- 3. Balanced check on Approved ----------
        [Fact]
        public void Approve_UnbalancedJE_Throws()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusDraft,
                createdBy: CreatorId, approvedBy: ApproverId,
                totalDebit: 100m, totalCredit: 95m);
            ctx.Initialize(new Entity[] { entity, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusApproved),
            };
            Action act = () => ExecuteHeaderUpdate(ctx, target, header);
            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*out of balance*100.00*95.00*");
        }

        [Fact]
        public void Approve_BalancedJE_StampsApprovedDateTime()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusDraft,
                createdBy: CreatorId, approvedBy: ApproverId,
                totalDebit: 100m, totalCredit: 100m);
            ctx.Initialize(new Entity[] { entity, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusApproved),
            };
            ExecuteHeaderUpdate(ctx, target, header);

            target.Contains("rm_approveddatetime").Should().BeTrue();
            target.GetAttributeValue<DateTime>("rm_approveddatetime").Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        // ---------- 4. SoD on Approved ----------
        [Fact]
        public void Approve_SameUserCreatorAndApprover_Throws()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusDraft,
                createdBy: CreatorId, approvedBy: CreatorId,
                totalDebit: 100m, totalCredit: 100m);
            ctx.Initialize(new Entity[] { entity, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusApproved),
            };

            Action act = () => ExecuteHeaderUpdate(ctx, target, header);
            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*Segregation-of-duties*");
        }

        [Fact]
        public void Approve_NullApprover_DefaultsToCurrentUser()
        {
            var ctx = NewContext(ApproverId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusDraft,
                createdBy: CreatorId,
                totalDebit: 100m, totalCredit: 100m);
            ctx.Initialize(new Entity[] { entity, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusApproved),
            };
            ExecuteHeaderUpdate(ctx, target, header);

            target.GetAttributeValue<EntityReference>("rm_approvedby_user").Id.Should().Be(ApproverId);
        }

        // ---------- 5. Fiscal-period open on Posted ----------
        [Fact]
        public void Post_ClosedPeriod_Throws()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var fp = FiscalPeriod(status: 261910001, name: "2025-12"); // Closed
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusApproved,
                createdBy: CreatorId, approvedBy: ApproverId,
                totalDebit: 100m, totalCredit: 100m,
                fiscalPeriodId: fp.Id);
            ctx.Initialize(new Entity[] { entity, fp, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusPosted),
            };

            Action act = () => ExecuteHeaderUpdate(ctx, target, header);
            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*2025-12*Closed*");
        }

        [Fact]
        public void Post_OpenPeriod_StampsPostedByAndDateTime()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var fp = FiscalPeriod();
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusApproved,
                createdBy: CreatorId, approvedBy: ApproverId,
                totalDebit: 100m, totalCredit: 100m,
                fiscalPeriodId: fp.Id);
            ctx.Initialize(new Entity[] { entity, fp, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusPosted),
            };
            ExecuteHeaderUpdate(ctx, target, header);

            target.GetAttributeValue<EntityReference>("rm_postedby_user").Id.Should().Be(CallerId);
            target.GetAttributeValue<DateTime>("rm_posteddatetime").Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        // ---------- 6. Immutability of Posted / Reversed / Voided ----------
        [Fact]
        public void Update_PostedJE_BlockedExceptToReversed()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusPosted,
                createdBy: CreatorId, approvedBy: ApproverId,
                totalDebit: 100m, totalCredit: 100m);
            ctx.Initialize(new Entity[] { entity, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_journaldescription"] = "Edited after post",
            };

            Action act = () => ExecuteHeaderUpdate(ctx, target, header);
            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*locked*Posted*");
        }

        [Fact]
        public void Update_PostedJE_ToReversed_Allowed()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusPosted,
                createdBy: CreatorId, approvedBy: ApproverId,
                totalDebit: 100m, totalCredit: 100m);
            ctx.Initialize(new Entity[] { entity, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusReversed),
            };
            Action act = () => ExecuteHeaderUpdate(ctx, target, header);
            act.Should().NotThrow();
        }

        [Fact]
        public void Update_VoidedJE_AlwaysBlocked()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusVoided,
                createdBy: CreatorId, approvedBy: ApproverId);
            ctx.Initialize(new Entity[] { entity, header });

            var target = new Entity("rm_journalentry", header.Id)
            {
                ["rm_status"] = new OptionSetValue(PostJournalEntryPlugin.StatusDraft),
            };
            Action act = () => ExecuteHeaderUpdate(ctx, target, header);
            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*Voided*");
        }

        [Fact]
        public void LineWrite_AgainstPostedHeader_Throws()
        {
            var ctx = NewContext(CallerId);
            var entity = Entity("DEFAULT");
            var header = Header(Guid.NewGuid(), entity.Id, PostJournalEntryPlugin.StatusPosted,
                createdBy: CreatorId, approvedBy: ApproverId,
                totalDebit: 100m, totalCredit: 100m,
                number: "JE-DEFAULT-000001");
            ctx.Initialize(new Entity[] { entity, header });

            var newLine = Line(header.Id, entity.Id, 50m, 0m);

            Action act = () => ExecuteLinePreOp(ctx, "Create", newLine);
            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*JE-DEFAULT-000001*Posted*");
        }

        // ---------- pipeline harness ----------
        private static void ExecuteCreate(IXrmFakedContext ctx, Entity target)
        {
            var pluginCtx = ctx.GetDefaultPluginContext();
            pluginCtx.MessageName = "Create";
            pluginCtx.Stage = 20;
            pluginCtx.PrimaryEntityName = "rm_journalentry";
            pluginCtx.InputParameters = new ParameterCollection { { "Target", target } };
            ctx.ExecutePluginWith<PostJournalEntryPlugin>(pluginCtx);
        }

        private static void ExecuteHeaderUpdate(IXrmFakedContext ctx, Entity target, Entity preImage)
        {
            var pluginCtx = ctx.GetDefaultPluginContext();
            pluginCtx.MessageName = "Update";
            pluginCtx.Stage = 20;
            pluginCtx.PrimaryEntityName = "rm_journalentry";
            pluginCtx.PrimaryEntityId = target.Id;
            pluginCtx.InputParameters = new ParameterCollection { { "Target", target } };
            pluginCtx.PreEntityImages = new EntityImageCollection
            {
                { PostJournalEntryPlugin.PreImageName, preImage }
            };
            ctx.ExecutePluginWith<PostJournalEntryPlugin>(pluginCtx);
        }

        private static void ExecuteLinePreOp(IXrmFakedContext ctx, string message, Entity line)
        {
            var pluginCtx = ctx.GetDefaultPluginContext();
            pluginCtx.MessageName = message;
            pluginCtx.Stage = 20;
            pluginCtx.PrimaryEntityName = "rm_journalentryline";
            pluginCtx.PrimaryEntityId = line.Id;
            pluginCtx.InputParameters = new ParameterCollection { { "Target", line } };
            ctx.ExecutePluginWith<PostJournalEntryPlugin>(pluginCtx);
        }

        private static void ExecuteLinePostOp(IXrmFakedContext ctx, string message, Entity line)
        {
            var pluginCtx = ctx.GetDefaultPluginContext();
            pluginCtx.MessageName = message;
            pluginCtx.Stage = 40;
            pluginCtx.PrimaryEntityName = "rm_journalentryline";
            pluginCtx.PrimaryEntityId = line.Id;
            pluginCtx.InputParameters = new ParameterCollection { { "Target", line } };
            pluginCtx.PreEntityImages = new EntityImageCollection
            {
                { PostJournalEntryPlugin.PreImageName, line }
            };
            ctx.ExecutePluginWith<PostJournalEntryPlugin>(pluginCtx);
        }
    }
}
